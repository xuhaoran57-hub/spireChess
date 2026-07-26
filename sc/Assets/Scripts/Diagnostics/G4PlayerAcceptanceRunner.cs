using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SpireChess.App;
using SpireChess.Battle;
using SpireChess.Run;
using SpireChess.Save;
using SpireChess.Shop;
using SpireChess.Simulation;
using SpireChess.UI;
using SpireChess.UI.Battle;
using SpireChess.UI.Common;
using SpireChess.UI.MainMenu;
using SpireChess.UI.Run;
using SpireChess.UI.Shop;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SpireChess.Diagnostics
{
    [DefaultExecutionOrder(-800)]
    public sealed class G4PlayerAcceptanceRunner : MonoBehaviour
    {
        private enum StressPlaybackMode
        {
            Normal,
            Accelerated,
            Skip
        }

        private const float SceneTimeoutSeconds = 20f;
        private const float ScreenshotTimeoutSeconds = 10f;
        private const float CheckpointSettleSeconds = 0.65f;

        private static G4PlayerAcceptanceRunner instance;
        private string evidenceDirectory;
        private int screenshotSequence;
        private readonly HashSet<int> screenshotOrdinals =
            new HashSet<int>();
        private bool failed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (!G4RuntimeArguments.IsAcceptanceRequested || instance != null)
            {
                return;
            }

            var root = new GameObject(nameof(G4PlayerAcceptanceRunner));
            DontDestroyOnLoad(root);
            root.AddComponent<G4PlayerAcceptanceRunner>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator Start()
        {
            if (G4RuntimeArguments.IsFrozenVisualRequested &&
                G4RuntimeArguments.IsStressRequested)
            {
                Fail(
                    $"{G4RuntimeArguments.FrozenVisualFlag} and " +
                    $"{G4RuntimeArguments.StressFlag} are mutually exclusive.");
                yield break;
            }

            var saveRoot = G4RuntimeArguments.Read(
                G4RuntimeArguments.SaveRootArgument);
            if (string.IsNullOrWhiteSpace(saveRoot) ||
                !Path.IsPathRooted(saveRoot))
            {
                Fail(
                    "G4 acceptance refused to start without an isolated " +
                    $"absolute {G4RuntimeArguments.SaveRootArgument}.");
                yield break;
            }

            if (!G4RuntimeArguments.HasFlag(
                    G4RuntimeArguments.NoScreenshotsFlag))
            {
                var requestedEvidence = G4RuntimeArguments.Read(
                    G4RuntimeArguments.EvidenceOutputArgument);
                if (string.IsNullOrWhiteSpace(requestedEvidence) ||
                    !Path.IsPathRooted(requestedEvidence))
                {
                    Fail(
                        "G4 acceptance screenshots require an absolute " +
                        $"{G4RuntimeArguments.EvidenceOutputArgument}.");
                    yield break;
                }

                evidenceDirectory = Path.GetFullPath(requestedEvidence);
                Directory.CreateDirectory(evidenceDirectory);
            }

            yield return RunAcceptanceFlow();
        }

        private IEnumerator RunAcceptanceFlow()
        {
            yield return WaitForCondition(
                () =>
                {
                    if (!string.IsNullOrWhiteSpace(
                            GameApp.InitializationFailure))
                    {
                        throw new InvalidOperationException(
                            "GameApp initialization failed: " +
                            GameApp.InitializationFailure);
                    }

                    var app = GameApp.Instance;
                    return app != null &&
                           app.Configs != null &&
                           app.RunSaves != null &&
                           app.Persistence != null &&
                           app.Router != null &&
                           !string.IsNullOrWhiteSpace(app.SaveRootPath);
                },
                "GameApp initialization");
            if (failed)
            {
                yield break;
            }

            if (!string.Equals(
                    Path.GetFullPath(GameApp.Instance.SaveRootPath),
                    Path.GetFullPath(G4RuntimeArguments.RequireAbsolutePath(
                        G4RuntimeArguments.SaveRootArgument)),
                    StringComparison.OrdinalIgnoreCase))
            {
                Fail("GameApp did not adopt the injected isolated save root.");
                yield break;
            }

            if (G4RuntimeArguments.IsStressRequested)
            {
                var stressSeed = G4RuntimeArguments.ReadInt(
                    G4RuntimeArguments.AcceptanceSeedArgument,
                    940401,
                    1,
                    int.MaxValue);
                yield return RunStressAcceptanceFlow(stressSeed);
                yield break;
            }

            yield return WaitForScene<MainMenuController>(
                GameSceneNames.MainMenu);
            if (failed)
            {
                yield break;
            }

            GameApp.Instance.AbandonRun();
            yield return CaptureCheckpoint(
                G4RuntimeArguments.IsFrozenVisualRequested
                    ? "main-menu-new-run"
                    : "main-menu",
                "Fresh isolated save root; no continue slot.",
                fileOrdinal: G4RuntimeArguments.IsFrozenVisualRequested
                    ? 1
                    : 0);
            if (failed)
            {
                yield break;
            }

            var seed = G4RuntimeArguments.ReadInt(
                G4RuntimeArguments.AcceptanceSeedArgument,
                G4RuntimeArguments.IsFrozenVisualRequested ? 78 : 940101,
                1,
                int.MaxValue);
            var mainMenuController =
                Object.FindObjectOfType<MainMenuController>();
            if (!ExecuteStep(
                    "start deterministic run through NewGameButton",
                    () => InvokeNamedButton(
                        mainMenuController?.ScreenView,
                        "NewGameButton")))
            {
                yield break;
            }

            yield return WaitForScene<RunTestController>(GameSceneNames.Run);
            if (failed)
            {
                yield break;
            }
            if (G4RuntimeArguments.IsFrozenVisualRequested)
            {
                yield return RunFrozenVisualAcceptanceFlow(seed);
                yield break;
            }
            yield return CaptureCheckpoint(
                "run-map",
                $"Deterministic run seed {seed}; formal map scene initialized.");
            if (failed)
            {
                yield break;
            }

            var runController = Object.FindObjectOfType<RunTestController>();
            if (!ExecuteStep(
                    "enter f1_shop_start through its map node button",
                    () => InvokeMapNode(runController, "f1_shop_start")))
            {
                yield break;
            }

            yield return WaitForScene<ShopTestController>(GameSceneNames.Shop);
            if (failed)
            {
                yield break;
            }
            yield return CaptureCheckpoint(
                "shop",
                "Formal shop loaded from the reachable map node.");
            if (failed)
            {
                yield break;
            }

            var shopController = Object.FindObjectOfType<ShopTestController>();
            var firstOfferIndex = FindFirstMinionOfferIndex(shopController);
            var purchasedArtId = firstOfferIndex < 0
                ? string.Empty
                : shopController.Session.MinionOffers[firstOfferIndex].ArtId;
            if (!ExecuteStep(
                    "buy first non-null shop minion through ShopCardView",
                    () =>
                {
                    if (shopController == null || firstOfferIndex < 0)
                    {
                        return false;
                    }

                    var offer = FindShopCard(
                        ShopCardZone.MinionOffer,
                        firstOfferIndex);
                    return InvokePointerClick(offer) &&
                           shopController.LastOperationResult?.Success == true;
                }))
            {
                yield break;
            }
            yield return null;

            var purchasedBenchIndex = FindFirstOccupiedBenchIndex(
                shopController);
            if (!ExecuteStep(
                    "play purchased minion through bench card and battle slot",
                    () =>
                {
                    var bench = FindShopCard(
                        ShopCardZone.Bench,
                        purchasedBenchIndex);
                    var battleSlot = FindShopSlot(
                        ShopCardZone.Battle,
                        0);
                    if (bench == null || battleSlot == null)
                    {
                        return false;
                    }

                    return InvokePointerClick(bench) &&
                           InvokePointerClick(battleSlot) &&
                           shopController.Session.Collection.Battle[0] != null &&
                           shopController.Session.Collection
                               .Bench[purchasedBenchIndex] == null;
                }))
            {
                yield break;
            }
            yield return null;
            yield return CaptureCheckpoint(
                "shop-buy-play",
                $"Bought offer {firstOfferIndex} ({purchasedArtId}) into " +
                $"bench {purchasedBenchIndex} and played it to battle slot 0.");
            if (failed)
            {
                yield break;
            }

            if (!ExecuteStep(
                    "freeze shop through FreezeButton",
                    () => InvokeNamedButton(
                              shopController?.FormalScreenView,
                              "FreezeButton") &&
                          shopController.Session.IsFrozen))
            {
                yield break;
            }
            yield return CaptureCheckpoint(
                "shop-frozen",
                "FreezeButton wiring succeeded and the shop is visibly frozen.");
            if (failed)
            {
                yield break;
            }

            if (!ExecuteStep(
                    "unfreeze shop through FreezeButton",
                    () => InvokeNamedButton(
                              shopController?.FormalScreenView,
                              "FreezeButton") &&
                          !shopController.Session.IsFrozen))
            {
                yield break;
            }
            yield return CaptureCheckpoint(
                "shop-unfrozen",
                "FreezeButton wiring succeeded and the shop is visibly unfrozen.");
            if (failed)
            {
                yield break;
            }

            if (!ExecuteStep(
                    "complete map shop through EndButton",
                    () => InvokeNamedButton(
                        shopController?.FormalScreenView,
                        "EndButton")))
            {
                yield break;
            }

            yield return WaitForScene<RunTestController>(GameSceneNames.Run);
            if (failed)
            {
                yield break;
            }
            yield return CaptureCheckpoint(
                "run-after-shop",
                "Returned through the formal Run scene after completing shop.");
            if (failed)
            {
                yield break;
            }

            runController = Object.FindObjectOfType<RunTestController>();
            if (!ExecuteStep(
                    "enter f1_opening_normal through its map node button",
                    () => InvokeMapNode(
                        runController,
                        "f1_opening_normal")))
            {
                yield break;
            }

            yield return WaitForScene<BattleTestController>(
                GameSceneNames.Battle);
            if (failed)
            {
                yield break;
            }
            yield return CaptureCheckpoint(
                "battle-ready",
                "Formal battle loaded from the reachable opening encounter.");
            if (failed)
            {
                yield break;
            }

            var battleController =
                Object.FindObjectOfType<BattleTestController>();
            if (!ExecuteStep("start 2x battle playback", () =>
                {
                    var screen =
                        Object.FindObjectOfType<BattleScreenView>();
                    if (battleController == null || screen == null)
                    {
                        return false;
                    }

                    if (battleController.PlaybackSpeed <= 1f)
                    {
                        if (!InvokeNamedButton(screen, "Speed"))
                        {
                            return false;
                        }
                    }
                    return InvokeNamedButton(screen, "Start") &&
                           battleController.PlaybackSpeed == 2f;
                }))
            {
                yield break;
            }

            var battleScreen =
                Object.FindObjectOfType<BattleScreenView>();
            yield return WaitForBattleFeedback(
                battleController,
                battleScreen,
                "battle_summon",
                "opening battle deathrattle summon");
            if (failed)
            {
                yield break;
            }
            yield return CaptureCheckpoint(
                "battle-death-summon",
                "The legal opening battle reached the explicit summon " +
                "feedback after the purchased minion's deathrattle.",
                0f,
                true);
            if (failed)
            {
                yield break;
            }
            if (!ExecuteStep(
                    "skip battle playback through Skip button",
                    () => InvokeNamedButton(
                        Object.FindObjectOfType<BattleScreenView>(),
                        "Skip")))
            {
                yield break;
            }
            yield return WaitForCondition(
                () => battleController.LastResult != null &&
                      !battleController.IsAttackAnimationPlaying,
                "2x/skip battle completion");
            if (failed)
            {
                yield break;
            }
            yield return CaptureCheckpoint(
                "battle-result",
                "2x playback was skipped; domain settlement and result UI completed.");
            if (failed)
            {
                yield break;
            }

            if (!ExecuteStep("return battle result to run", () =>
                {
                    return InvokeNamedButton(
                        Object.FindObjectOfType<BattleScreenView>(),
                        "Return");
                }))
            {
                yield break;
            }
            yield return WaitForScene<RunTestController>(GameSceneNames.Run);
            if (failed)
            {
                yield break;
            }
            yield return CaptureCheckpoint(
                "run-return",
                "Formal Run scene restored after battle settlement.");
            if (failed)
            {
                yield break;
            }

            runController = Object.FindObjectOfType<RunTestController>();
            if (GameApp.Instance.Run.State.Phase == RunPhase.RewardChoice)
            {
                yield return CaptureCheckpoint(
                    "run-reward",
                    "Formal reward choice is visible before the legal skip.");
                if (failed)
                {
                    yield break;
                }
                if (!ExecuteStep(
                        "skip opening battle reward through choice button",
                        () => InvokeNamedButton(
                            runController?.FormalScreenView,
                            "Choice_SkipReward")))
                {
                    yield break;
                }
                yield return null;
            }

            if (!ExecuteStep(
                    "continue battle result through ActionButton",
                    () => GameApp.Instance.Run.State.Phase ==
                          RunPhase.BattleResult &&
                          InvokeNamedButton(
                              runController?.FormalScreenView,
                              "ActionButton") &&
                          GameApp.Instance.Run.State.Phase ==
                          RunPhase.MapSelection))
            {
                yield break;
            }
            yield return null;
            yield return CaptureCheckpoint(
                "run-map-after-battle",
                "Battle result was acknowledged and phase is MapSelection.");
            if (failed)
            {
                yield break;
            }

            var mapper = new RunSnapshotMapper(GameApp.Instance.Configs);
            var expectedFingerprint = RunStateFingerprint.Compute(
                mapper.Capture(GameApp.Instance.Run));
            var expectedPhase = GameApp.Instance.Run.State.Phase;
            var systemMenu =
                Object.FindObjectOfType<RunSystemMenuView>();
            if (!ExecuteStep(
                    "open run system menu through MenuButton",
                    () => InvokeNamedButton(systemMenu, "MenuButton") &&
                          systemMenu.IsOpen))
            {
                yield break;
            }
            yield return CaptureCheckpoint(
                "run-system-menu",
                "System menu opened through its real button wiring.");
            if (failed)
            {
                yield break;
            }
            if (!ExecuteStep(
                    "open audio settings through AudioSettingsButton",
                    () => InvokeNamedButton(
                              systemMenu,
                              "AudioSettingsButton") &&
                          systemMenu.SettingsOpen))
            {
                yield break;
            }
            yield return CaptureCheckpoint(
                "run-audio-settings",
                "Four-channel local audio settings panel is visible.");
            if (failed)
            {
                yield break;
            }
            if (!ExecuteStep(
                    "close audio settings through CloseButton",
                    () => InvokeNamedButton(systemMenu, "CloseButton") &&
                          !systemMenu.SettingsOpen))
            {
                yield break;
            }
            if (!ExecuteStep(
                    "save and return through SaveReturnButton",
                    () => InvokeNamedButton(
                        systemMenu,
                        "SaveReturnButton")))
            {
                yield break;
            }

            yield return WaitForScene<MainMenuController>(
                GameSceneNames.MainMenu);
            if (failed)
            {
                yield break;
            }
            mainMenuController = Object.FindObjectOfType<MainMenuController>();
            if (mainMenuController?.ScreenView == null ||
                !mainMenuController.ScreenView.ContinueInteractable)
            {
                Fail("Main menu did not expose a valid Continue action.");
                yield break;
            }
            yield return CaptureCheckpoint(
                "main-menu-continue",
                "Saved isolated run is visible and Continue is interactable.");
            if (failed)
            {
                yield break;
            }

            if (!ExecuteStep("continue isolated run", () =>
                {
                    return InvokeNamedButton(
                        mainMenuController.ScreenView,
                        "ContinueButton");
                }))
            {
                yield break;
            }
            yield return WaitForScene<RunTestController>(GameSceneNames.Run);
            if (failed)
            {
                yield break;
            }

            var resumedRun = GameApp.Instance.Run;
            if (resumedRun == null ||
                resumedRun.State.Phase != expectedPhase ||
                !string.Equals(
                    RunStateFingerprint.Compute(mapper.Capture(resumedRun)),
                    expectedFingerprint,
                    StringComparison.Ordinal))
            {
                Fail(
                    "Continued run does not match the saved domain fingerprint.");
                yield break;
            }
            yield return CaptureCheckpoint(
                "continued-run",
                $"Continue restored phase {expectedPhase} with an exact fingerprint.");
            if (failed)
            {
                yield break;
            }

            yield return CompleteAcceptance(
                "Formal MainMenu -> Run -> Shop -> Run -> Battle -> Run -> " +
                "MainMenu -> Continue chain completed with isolated " +
                "persistence.");
        }

        private IEnumerator RunFrozenVisualAcceptanceFlow(int seed)
        {
            const int frozenVisualSeed = 78;
            if (seed != frozenVisualSeed)
            {
                Fail(
                    "The frozen visual chain is a deterministic production " +
                    $"contract and requires seed {frozenVisualSeed}, got {seed}.");
                yield break;
            }

            yield return WaitForRunPresentationReady(
                "frozen visual initial map");
            if (failed)
            {
                yield break;
            }

            var run = GameApp.Instance.Run;
            var runController =
                Object.FindObjectOfType<RunTestController>();
            var runView = runController?.FormalScreenView;
            if (run == null ||
                runView == null ||
                run.State.Phase != RunPhase.MapSelection ||
                run.State.CurrentMap?.Nodes == null ||
                run.State.CurrentMap.Nodes.Count != 19 ||
                runView.IsChoiceVisible)
            {
                Fail(
                    "Frozen visual acceptance did not start on the formal " +
                    "19-node map without a choice overlay.");
                yield break;
            }

            var expectedNodeIds = new HashSet<string>(
                run.State.CurrentMap.Nodes.Select(node => node.Id),
                StringComparer.Ordinal);
            var coveredNodeIds =
                new HashSet<string>(StringComparer.Ordinal);
            var mapSegmentSnapshots =
                new Dictionary<
                    RunMapViewportSegment,
                    RunMapViewportSnapshot>();
            var mapSegments = new[]
            {
                new
                {
                    Segment = RunMapViewportSegment.Left,
                    Checkpoint = "run-map-left",
                    Ordinal = 2
                },
                new
                {
                    Segment = RunMapViewportSegment.Center,
                    Checkpoint = "run-map-center",
                    Ordinal = 3
                },
                new
                {
                    Segment = RunMapViewportSegment.Right,
                    Checkpoint = "run-map-right",
                    Ordinal = 4
                }
            };
            foreach (var entry in mapSegments)
            {
                var snapshot = runView.SetMapViewportSegment(entry.Segment);
                mapSegmentSnapshots[entry.Segment] = snapshot;
                foreach (var nodeId in snapshot.FullyVisibleNodeIds)
                {
                    coveredNodeIds.Add(nodeId);
                }

                if (runView.IsChoiceVisible ||
                    snapshot.FullyVisibleNodeIds.Count == 0)
                {
                    Fail(
                        $"Map segment {entry.Segment} did not expose a " +
                        "non-empty unobstructed node set.");
                    yield break;
                }

                yield return CaptureCheckpoint(
                    entry.Checkpoint,
                    $"Seed {seed}; segment={entry.Segment}; fullyVisible=" +
                    string.Join(",", snapshot.FullyVisibleNodeIds) +
                    "; intersecting=" +
                    string.Join(",", snapshot.IntersectingNodeIds) + ".",
                    fileOrdinal: entry.Ordinal);
                if (failed)
                {
                    yield break;
                }
            }

            var leftMap =
                mapSegmentSnapshots[RunMapViewportSegment.Left];
            var centerMap =
                mapSegmentSnapshots[RunMapViewportSegment.Center];
            var rightMap =
                mapSegmentSnapshots[RunMapViewportSegment.Right];
            var leftNodeIds = new HashSet<string>(
                leftMap.FullyVisibleNodeIds,
                StringComparer.Ordinal);
            var rightNodeIds = new HashSet<string>(
                rightMap.FullyVisibleNodeIds,
                StringComparer.Ordinal);
            if (Mathf.Abs(leftMap.HorizontalNormalizedPosition) > 0.01f ||
                Mathf.Abs(
                    centerMap.HorizontalNormalizedPosition - 0.5f) > 0.01f ||
                Mathf.Abs(
                    rightMap.HorizontalNormalizedPosition - 1f) > 0.01f ||
                leftMap.ContentBoundsInViewport.width <=
                leftMap.ViewportBounds.width ||
                leftNodeIds.SetEquals(rightNodeIds) ||
                !leftNodeIds.Contains("f1_shop_start") ||
                !rightNodeIds.Contains("f1_boss") ||
                !leftNodeIds.Except(rightNodeIds).Any() ||
                !rightNodeIds.Except(leftNodeIds).Any())
            {
                Fail(
                    "The map segment evidence did not prove a wider-than-" +
                    "viewport scroll range with distinct left/center/right " +
                    "positions and endpoint-exclusive nodes.");
                yield break;
            }

            if (!coveredNodeIds.SetEquals(expectedNodeIds))
            {
                var missing = expectedNodeIds
                    .Except(coveredNodeIds)
                    .OrderBy(value => value, StringComparer.Ordinal);
                Fail(
                    "The left/center/right map evidence did not fully cover " +
                    "the production 19-node map. Missing: " +
                    string.Join(", ", missing) + ".");
                yield break;
            }

            if (!ExecuteStep(
                    "enter f1_shop_start through the visible map node",
                    () => InvokeMapNodeInVisibleSegment(
                        runController,
                        "f1_shop_start")))
            {
                yield break;
            }
            yield return WaitForScene<ShopTestController>(
                GameSceneNames.Shop);
            if (failed)
            {
                yield break;
            }
            yield return WaitForShopPresentationReady(
                "round-1 shop entry");
            if (failed)
            {
                yield break;
            }

            var shopController =
                Object.FindObjectOfType<ShopTestController>();
            if (!ValidateFrozenShop(
                    shopController,
                    1,
                    3,
                    "moss_mark_seedling"))
            {
                yield break;
            }
            yield return CaptureCheckpoint(
                "shop-entry",
                "Round 1 formal shop entered from f1_shop_start with the " +
                "seed-78 production offer set.",
                fileOrdinal: 5);
            if (failed)
            {
                yield break;
            }

            if (!ExecuteStep(
                    "buy round-1 moss_mark_seedling through its offer card",
                    () => BuyConfiguredMinionThroughUi(
                        shopController,
                        "moss_mark_seedling")))
            {
                yield break;
            }
            yield return null;
            if (!ExecuteStep(
                    "play round-1 moss_mark_seedling into battle slot 0",
                    () => PlayConfiguredBenchMinionThroughUi(
                        shopController,
                        "moss_mark_seedling",
                        0)))
            {
                yield break;
            }
            yield return null;
            if (!ExecuteStep(
                    "end round-1 shop through EndButton",
                    () => InvokeNamedButton(
                        shopController.FormalScreenView,
                        "EndButton")))
            {
                yield break;
            }

            yield return WaitForScene<RunTestController>(
                GameSceneNames.Run);
            if (failed)
            {
                yield break;
            }
            yield return WaitForRunPresentationReady(
                "round-1 shop return");
            if (failed)
            {
                yield break;
            }
            runController = Object.FindObjectOfType<RunTestController>();
            if (!ExecuteStep(
                    "enter f1_opening_normal through the map",
                    () => InvokeMapNodeInVisibleSegment(
                        runController,
                        "f1_opening_normal")))
            {
                yield break;
            }
            yield return WaitForScene<BattleTestController>(
                GameSceneNames.Battle);
            if (failed)
            {
                yield break;
            }
            yield return ResolveFrozenBattleAndReturn(
                "opening battle",
                BattleSide.Player);
            if (failed)
            {
                yield break;
            }

            runController = Object.FindObjectOfType<RunTestController>();
            if (!ExecuteStep(
                    "enter f1_shop_2 through the map",
                    () => InvokeMapNodeInVisibleSegment(
                        runController,
                        "f1_shop_2")))
            {
                yield break;
            }
            yield return WaitForScene<ShopTestController>(
                GameSceneNames.Shop);
            if (failed)
            {
                yield break;
            }
            yield return WaitForShopPresentationReady(
                "round-2 shop entry");
            if (failed)
            {
                yield break;
            }
            shopController = Object.FindObjectOfType<ShopTestController>();
            if (!ValidateFrozenShop(shopController, 2, 5))
            {
                yield break;
            }

            if (!ExecuteStep("refresh round-2 shop through RefreshButton", () =>
                {
                    var beforeGold = shopController.Session.Gold;
                    var beforeRefreshes =
                        shopController.Session.RefreshCount;
                    return InvokeNamedButton(
                               shopController.FormalScreenView,
                               "RefreshButton") &&
                           shopController.LastOperationResult?.Success == true &&
                           shopController.Session.Gold == beforeGold - 1 &&
                           shopController.Session.RefreshCount ==
                           beforeRefreshes + 1 &&
                           shopController.Session.MinionOffers.Count(
                               offer => offer?.Id ==
                                        "hearth_core_spark") == 2 &&
                           shopController.Session.SpellOffer?.Id ==
                           "temporary_ward";
                }))
            {
                yield break;
            }
            yield return null;
            yield return CaptureCheckpoint(
                "shop-refresh",
                "RefreshButton spent one gold and produced two " +
                "hearth_core_spark offers plus temporary_ward.",
                fileOrdinal: 6);
            if (failed)
            {
                yield break;
            }

            if (!ExecuteStep(
                    "buy round-2 hearth_core_spark through its offer card",
                    () => BuyConfiguredMinionThroughUi(
                        shopController,
                        "hearth_core_spark")))
            {
                yield break;
            }
            yield return null;
            if (!ExecuteStep(
                    "play round-2 hearth_core_spark into battle slot 1",
                    () => PlayConfiguredBenchMinionThroughUi(
                        shopController,
                        "hearth_core_spark",
                        1)))
            {
                yield break;
            }
            yield return null;
            yield return CaptureCheckpoint(
                "shop-buy-play",
                "The refreshed hearth_core_spark was bought through its " +
                "offer, placed on the bench, then played into battle slot 1.",
                fileOrdinal: 7);
            if (failed)
            {
                yield break;
            }

            if (!ExecuteStep(
                    "buy temporary_ward through the spell offer",
                    () => BuyConfiguredSpellThroughUi(
                        shopController,
                        "temporary_ward")))
            {
                yield break;
            }
            yield return null;
            var wardBenchIndex = FindBenchCardIndex(
                shopController,
                "temporary_ward");
            if (!ExecuteStep(
                    "select temporary_ward and expose its target state",
                    () =>
                {
                    var spellCard = FindShopCard(
                        ShopCardZone.Bench,
                        wardBenchIndex);
                    return wardBenchIndex >= 0 &&
                           InvokePointerClick(spellCard) &&
                           shopController.SelectedBenchIndex ==
                           wardBenchIndex &&
                           shopController.LastOperationResult?.Error ==
                           ShopOperationError.InvalidTarget;
                }))
            {
                yield break;
            }
            yield return null;
            yield return CaptureCheckpoint(
                "shop-target-or-warcry",
                "temporary_ward is selected through its Compact hand card " +
                "and the formal shop is visibly requesting a battle target.",
                fileOrdinal: 8);
            if (failed)
            {
                yield break;
            }
            if (!ExecuteStep(
                    "target battle slot 0 with temporary_ward",
                    () => TargetSelectedSpellThroughUi(
                        shopController,
                        wardBenchIndex,
                        0,
                        "temporary_ward")))
            {
                yield break;
            }
            yield return null;

            if (!ExecuteStep(
                    "freeze round-2 shop through FreezeButton",
                    () => InvokeNamedButton(
                              shopController.FormalScreenView,
                              "FreezeButton") &&
                          shopController.Session.IsFrozen))
            {
                yield break;
            }
            yield return CaptureCheckpoint(
                "shop-frozen",
                "FreezeButton entered the visible frozen state.",
                fileOrdinal: 9);
            if (failed)
            {
                yield break;
            }
            if (!ExecuteStep(
                    "unfreeze round-2 shop through FreezeButton",
                    () => InvokeNamedButton(
                              shopController.FormalScreenView,
                              "FreezeButton") &&
                          !shopController.Session.IsFrozen))
            {
                yield break;
            }
            yield return CaptureCheckpoint(
                "shop-unfrozen",
                "FreezeButton returned the same shop to its unfrozen state.",
                fileOrdinal: 10);
            if (failed)
            {
                yield break;
            }
            if (!ExecuteStep(
                    "end round-2 shop through EndButton",
                    () => InvokeNamedButton(
                        shopController.FormalScreenView,
                        "EndButton")))
            {
                yield break;
            }

            yield return WaitForScene<RunTestController>(
                GameSceneNames.Run);
            if (failed)
            {
                yield break;
            }
            yield return WaitForRunPresentationReady(
                "round-2 shop return");
            if (failed)
            {
                yield break;
            }
            runController = Object.FindObjectOfType<RunTestController>();
            if (!ExecuteStep(
                    "enter f1_safe_normal through the map",
                    () => InvokeMapNodeInVisibleSegment(
                        runController,
                        "f1_safe_normal")))
            {
                yield break;
            }
            yield return WaitForScene<BattleTestController>(
                GameSceneNames.Battle);
            if (failed)
            {
                yield break;
            }
            yield return ResolveFrozenBattleAndReturn(
                "safe battle",
                BattleSide.Player);
            if (failed)
            {
                yield break;
            }

            runController = Object.FindObjectOfType<RunTestController>();
            if (!ExecuteStep(
                    "enter f1_shop_3 through the map",
                    () => InvokeMapNodeInVisibleSegment(
                        runController,
                        "f1_shop_3")))
            {
                yield break;
            }
            yield return WaitForScene<ShopTestController>(
                GameSceneNames.Shop);
            if (failed)
            {
                yield break;
            }
            yield return WaitForShopPresentationReady(
                "round-3 shop entry",
                "ClaimButton");
            if (failed)
            {
                yield break;
            }
            shopController = Object.FindObjectOfType<ShopTestController>();
            if (!ValidateFrozenShop(shopController, 3, 5) ||
                run.State.PendingCardRewards.Count != 1 ||
                run.State.PendingCardRewards[0].ConfigId !=
                "minor_tempering" ||
                !shopController.RewardModalVisible)
            {
                Fail(
                    "Round-3 shop did not expose the deterministic " +
                    "minor_tempering pending reward.");
                yield break;
            }
            if (!ExecuteStep(
                    "claim minor_tempering through ClaimButton",
                    () => InvokeNamedButton(
                              shopController.FormalScreenView,
                              "ClaimButton") &&
                          run.State.PendingCardRewards.Count == 0))
            {
                yield break;
            }
            yield return null;

            var temperingBenchIndex = FindBenchCardIndex(
                shopController,
                "minor_tempering");
            if (!ExecuteStep(
                    "select minor_tempering through its bench card",
                    () =>
                {
                    var spellCard = FindShopCard(
                        ShopCardZone.Bench,
                        temperingBenchIndex);
                    return temperingBenchIndex >= 0 &&
                           InvokePointerClick(spellCard) &&
                           shopController.SelectedBenchIndex ==
                           temperingBenchIndex &&
                           shopController.LastOperationResult?.Error ==
                           ShopOperationError.InvalidTarget;
                }))
            {
                yield break;
            }
            yield return null;
            if (!ExecuteStep(
                    "target battle slot 0 with minor_tempering",
                    () => TargetSelectedSpellThroughUi(
                        shopController,
                        temperingBenchIndex,
                        0,
                        "minor_tempering")))
            {
                yield break;
            }
            yield return null;

            if (!ExecuteStep(
                    "buy delayed_supply through the spell offer",
                    () => BuyConfiguredSpellThroughUi(
                        shopController,
                        "delayed_supply")))
            {
                yield break;
            }
            yield return null;
            var supplyBenchIndex = FindBenchCardIndex(
                shopController,
                "delayed_supply");
            if (!ExecuteStep(
                    "use delayed_supply through its bench card",
                    () =>
                {
                    var spellCard = FindShopCard(
                        ShopCardZone.Bench,
                        supplyBenchIndex);
                    return supplyBenchIndex >= 0 &&
                           InvokePointerClick(spellCard) &&
                           shopController.LastOperationResult?.Success == true &&
                           shopController.Session.ScheduledGold == 2 &&
                           FindBenchCardIndex(
                               shopController,
                               "delayed_supply") < 0;
                }))
            {
                yield break;
            }
            yield return null;

            if (!ExecuteStep(
                    "buy round-3 hearth_core_spark through its offer card",
                    () => BuyConfiguredMinionThroughUi(
                        shopController,
                        "hearth_core_spark")))
            {
                yield break;
            }
            yield return null;
            if (!ExecuteStep(
                    "play round-3 hearth_core_spark into battle slot 2",
                    () => PlayConfiguredBenchMinionThroughUi(
                        shopController,
                        "hearth_core_spark",
                        2)))
            {
                yield break;
            }
            yield return null;
            if (!ExecuteStep(
                    "end round-3 shop through EndButton",
                    () => InvokeNamedButton(
                        shopController.FormalScreenView,
                        "EndButton")))
            {
                yield break;
            }

            yield return WaitForScene<RunTestController>(
                GameSceneNames.Run);
            if (failed)
            {
                yield break;
            }
            yield return WaitForRunPresentationReady(
                "round-3 shop return");
            if (failed)
            {
                yield break;
            }
            runController = Object.FindObjectOfType<RunTestController>();
            if (!ExecuteStep(
                    "enter f1_mid_mechanic through the map",
                    () => InvokeMapNodeInVisibleSegment(
                        runController,
                        "f1_mid_mechanic")))
            {
                yield break;
            }
            yield return WaitForScene<BattleTestController>(
                GameSceneNames.Battle);
            if (failed)
            {
                yield break;
            }
            yield return ResolveFrozenBattleAndReturn(
                "mid-mechanic battle",
                null);
            if (failed)
            {
                yield break;
            }
            if (run.State.Health != 19 ||
                run.State.PendingCardRewards.Count != 0)
            {
                Fail(
                    "The deterministic round-3 draw did not settle to " +
                    "19 health with no pending card reward.");
                yield break;
            }

            runController = Object.FindObjectOfType<RunTestController>();
            if (!ExecuteStep(
                    "enter f1_shop_4 through the map",
                    () => InvokeMapNodeInVisibleSegment(
                        runController,
                        "f1_shop_4")))
            {
                yield break;
            }
            yield return WaitForScene<ShopTestController>(
                GameSceneNames.Shop);
            if (failed)
            {
                yield break;
            }
            yield return WaitForShopPresentationReady(
                "round-4 shop entry");
            if (failed)
            {
                yield break;
            }
            shopController = Object.FindObjectOfType<ShopTestController>();
            if (!ValidateFrozenShop(
                    shopController,
                    4,
                    8,
                    "moss_mark_seedling",
                    "young_deer_spirit"))
            {
                yield break;
            }

            if (!ExecuteStep(
                    "buy round-4 moss_mark_seedling through its offer card",
                    () => BuyConfiguredMinionThroughUi(
                        shopController,
                        "moss_mark_seedling")))
            {
                yield break;
            }
            yield return null;
            if (!ExecuteStep(
                    "play round-4 moss_mark_seedling into battle slot 3",
                    () => PlayConfiguredBenchMinionThroughUi(
                        shopController,
                        "moss_mark_seedling",
                        3)))
            {
                yield break;
            }
            yield return null;
            if (!ExecuteStep(
                    "buy round-4 young_deer_spirit through its offer card",
                    () => BuyConfiguredMinionThroughUi(
                        shopController,
                        "young_deer_spirit")))
            {
                yield break;
            }
            yield return null;
            if (!ExecuteStep(
                    "play round-4 young_deer_spirit into battle slot 4",
                    () => PlayConfiguredBenchMinionThroughUi(
                        shopController,
                        "young_deer_spirit",
                        4)))
            {
                yield break;
            }
            yield return null;

            if (!ExecuteStep("upgrade round-4 tavern through UpgradeButton", () =>
                {
                    var beforeTier = shopController.Session.TavernTier;
                    return shopController.Session.Gold == 2 &&
                           shopController.Session.CurrentUpgradeCost == 2 &&
                           InvokeNamedButton(
                               shopController.FormalScreenView,
                               "UpgradeButton") &&
                           shopController.LastOperationResult?.Success == true &&
                           shopController.Session.TavernTier ==
                           beforeTier + 1 &&
                           shopController.Session.Gold == 0;
                }))
            {
                yield break;
            }
            yield return null;
            yield return CaptureCheckpoint(
                "shop-upgrade",
                "Round 4 spent its final two legal gold through " +
                "UpgradeButton and advanced the tavern from tier 1 to tier 2.",
                fileOrdinal: 11);
            if (failed)
            {
                yield break;
            }
            if (!ExecuteStep(
                    "end round-4 shop through EndButton",
                    () => InvokeNamedButton(
                        shopController.FormalScreenView,
                        "EndButton")))
            {
                yield break;
            }

            yield return WaitForScene<RunTestController>(
                GameSceneNames.Run);
            if (failed)
            {
                yield break;
            }
            yield return WaitForRunPresentationReady(
                "round-4 shop return");
            if (failed)
            {
                yield break;
            }
            runController = Object.FindObjectOfType<RunTestController>();
            if (!ExecuteStep(
                    "enter f1_elite_wall through the map",
                    () => InvokeMapNodeInVisibleSegment(
                        runController,
                        "f1_elite_wall")))
            {
                yield break;
            }
            yield return WaitForScene<BattleTestController>(
                GameSceneNames.Battle);
            if (failed)
            {
                yield break;
            }
            yield return WaitForBattlePresentationReady(
                "elite battle entry");
            if (failed)
            {
                yield break;
            }

            var battleController =
                Object.FindObjectOfType<BattleTestController>();
            var battleScreen =
                Object.FindObjectOfType<BattleScreenView>();
            var eliteStandeeDetails = string.Empty;
            if (battleController == null ||
                battleScreen == null ||
                !battleController.IsRunBattle ||
                !battleController.UsesFormalView ||
                !battleScreen.HasCompleteBindings ||
                !TryValidateBattleStandees(
                    battleController,
                    battleScreen,
                    5,
                    3,
                    out eliteStandeeDetails))
            {
                Fail(
                    "The elite battle did not render its expected formal " +
                    "production standees. " + eliteStandeeDetails);
                yield break;
            }
            yield return CaptureCheckpoint(
                "battle-start",
                "The legal seed-78 elite battle is ready with the five-card " +
                "player formation built through four formal shops.",
                fileOrdinal: 12);
            if (failed)
            {
                yield break;
            }
            var eliteSettlementsBefore = SettlementCount(run);
            if (!ExecuteStep(
                    "start elite battle through Start button",
                    () => InvokeNamedButton(battleScreen, "Start")))
            {
                yield break;
            }
            yield return WaitForBattleFeedback(
                battleController,
                battleScreen,
                "battle_shield_break",
                "elite shield break",
                () => TryValidateActiveBattleStandees(
                    battleScreen,
                    out _));
            if (failed)
            {
                yield break;
            }
            yield return CaptureCheckpoint(
                "battle-attack-shield",
                "The elite battle reached the explicit shield-break feedback.",
                0f,
                true,
                13);
            if (failed)
            {
                yield break;
            }
            yield return WaitForBattleFeedback(
                battleController,
                battleScreen,
                "battle_summon",
                "elite young-deer deathrattle summon",
                () => HasRenderedBattleStandee(
                    battleScreen,
                    "token_young_spirit"));
            if (failed)
            {
                yield break;
            }
            yield return CaptureCheckpoint(
                "battle-death-summon",
                "The elite battle reached the explicit deathrattle summon " +
                "feedback after the shield-break state.",
                0f,
                true,
                14);
            if (failed)
            {
                yield break;
            }
            yield return WaitForCondition(
                () => battleController.LastResult != null &&
                      !battleScreen.IsAnimationPlaying &&
                      battleScreen.ActiveFeedbackFxCount == 0 &&
                      !HasActiveNonLoopingAudio() &&
                      !HasActiveNonLoopingAnimation() &&
                      battleScreen.IsResultVisible &&
                      IsNamedButtonReady(battleScreen, "Return"),
                "elite battle completion and transient cleanup",
                45f);
            if (failed)
            {
                yield break;
            }
            if (battleController.LastResult.Winner != BattleSide.Player)
            {
                Fail(
                    "The deterministic elite battle did not settle as a " +
                    "Player victory.");
                yield break;
            }
            if (!ReferenceEquals(
                    run.LastBattleResult,
                    battleController.LastResult) ||
                SettlementCount(run) != eliteSettlementsBefore + 1)
            {
                Fail(
                    "The elite battle did not settle its RunSession exactly " +
                    "once.");
                yield break;
            }
            yield return CaptureCheckpoint(
                "battle-result",
                "The legal elite battle settled as a Player victory with " +
                "the formal result overlay visible.",
                fileOrdinal: 15);
            if (failed)
            {
                yield break;
            }

            if (!ExecuteStep(
                    "return elite result through Return button",
                    () => InvokeNamedButton(battleScreen, "Return")))
            {
                yield break;
            }
            yield return WaitForScene<RunTestController>(
                GameSceneNames.Run);
            if (failed)
            {
                yield break;
            }
            yield return WaitForRunPresentationReady(
                "elite reward return");
            if (failed)
            {
                yield break;
            }
            runController = Object.FindObjectOfType<RunTestController>();
            var rewardChoice = run.State.PendingRewardChoice;
            var renderedRewardOptions =
                Object.FindObjectsOfType<RunChoiceOptionView>()
                    .Where(option =>
                        option != null &&
                        option.gameObject.activeInHierarchy)
                    .ToArray();
            var occupiedRewardTargets = run.Shop.Collection.Battle.Count(
                card => card != null);
            var expectedRenderedRewardOptions =
                rewardChoice?.Candidates.Sum(candidate =>
                    candidate.RequiresOwnedMinionTarget
                        ? occupiedRewardTargets
                        : 1) + 1 ?? 0;
            var renderedCandidateIds = new HashSet<string>(
                renderedRewardOptions
                    .Where(option =>
                        option.Action == RunUiActionType.SelectReward)
                    .Select(option => option.PrimaryId),
                StringComparer.Ordinal);
            if (run.State.Phase != RunPhase.RewardChoice ||
                rewardChoice == null ||
                rewardChoice.Candidates.Count != 3 ||
                !rewardChoice.AllowSkip ||
                runController?.FormalScreenView == null ||
                !runController.FormalScreenView.IsChoiceVisible ||
                runController.FormalScreenView.RenderedChoiceCount !=
                expectedRenderedRewardOptions ||
                renderedRewardOptions.Length !=
                expectedRenderedRewardOptions ||
                renderedRewardOptions.Count(option =>
                    option.Action == RunUiActionType.SkipReward) != 1 ||
                renderedRewardOptions.Any(option =>
                    !option.IsInteractable) ||
                !renderedCandidateIds.SetEquals(
                    rewardChoice.Candidates.Select(
                        candidate => candidate.CandidateId)))
            {
                Fail(
                    "The elite victory did not expose the strict three-card " +
                    "reward choice, all owned-minion target rows, and its " +
                    "single legal Skip action.");
                yield break;
            }
            yield return CaptureCheckpoint(
                "run-reward",
                "Elite victory returned to a strict RewardChoice containing " +
                $"three candidates, {expectedRenderedRewardOptions - 1} " +
                "candidate/target rows, and one legal Skip action.",
                fileOrdinal: 16);
            if (failed)
            {
                yield break;
            }
            runController.FormalScreenView
                .SetChoiceViewportNormalizedPosition(0f);
            yield return WaitForCondition(
                () => IsNamedButtonReady(
                    runController.FormalScreenView,
                    "Choice_SkipReward"),
                "scrolled elite reward Skip action");
            if (failed)
            {
                yield break;
            }
            if (!ExecuteStep(
                    "skip elite reward through Choice_SkipReward",
                    () => InvokeNamedButton(
                              runController.FormalScreenView,
                              "Choice_SkipReward") &&
                          run.State.Phase == RunPhase.BattleResult))
            {
                yield break;
            }
            yield return null;
            if (!ExecuteStep(
                    "acknowledge elite result through ActionButton",
                    () => InvokeNamedButton(
                              runController.FormalScreenView,
                              "ActionButton") &&
                          run.State.Phase == RunPhase.MapSelection))
            {
                yield break;
            }
            yield return null;
            var returnedMap = runController.FormalScreenView
                .SetMapViewportSegment(RunMapViewportSegment.Center);
            yield return CaptureCheckpoint(
                "run-returned-map",
                "Reward skip and BattleResult acknowledgement restored the " +
                "formal map. Fully visible: " +
                string.Join(",", returnedMap.FullyVisibleNodeIds) + ".",
                fileOrdinal: 17);
            if (failed)
            {
                yield break;
            }

            var mapper = new RunSnapshotMapper(GameApp.Instance.Configs);
            var expectedFingerprint = RunStateFingerprint.Compute(
                mapper.Capture(run));
            var expectedPhase = run.State.Phase;
            var systemMenu =
                Object.FindObjectOfType<RunSystemMenuView>();
            if (!ExecuteStep(
                    "open run system menu through MenuButton",
                    () => InvokeNamedButton(systemMenu, "MenuButton") &&
                          systemMenu.IsOpen))
            {
                yield break;
            }
            yield return CaptureCheckpoint(
                "run-system-menu",
                "The system menu opened through its real button wiring.",
                fileOrdinal: 18);
            if (failed)
            {
                yield break;
            }
            if (!ExecuteStep(
                    "open audio settings through AudioSettingsButton",
                    () => InvokeNamedButton(
                              systemMenu,
                              "AudioSettingsButton") &&
                          systemMenu.SettingsOpen))
            {
                yield break;
            }
            yield return CaptureCheckpoint(
                "run-audio-settings",
                "The four-channel local audio settings panel is visible.",
                fileOrdinal: 19);
            if (failed)
            {
                yield break;
            }
            if (!ExecuteStep(
                    "close audio settings through CloseButton",
                    () => InvokeNamedButton(systemMenu, "CloseButton") &&
                          !systemMenu.SettingsOpen) ||
                !ExecuteStep(
                    "save and return through SaveReturnButton",
                    () => InvokeNamedButton(
                        systemMenu,
                        "SaveReturnButton")))
            {
                yield break;
            }

            yield return WaitForScene<MainMenuController>(
                GameSceneNames.MainMenu);
            if (failed)
            {
                yield break;
            }
            yield return WaitForMainMenuPresentationReady(
                "saved-run Continue action");
            if (failed)
            {
                yield break;
            }
            var mainMenuController =
                Object.FindObjectOfType<MainMenuController>();
            if (mainMenuController?.ScreenView == null ||
                !mainMenuController.ScreenView.ContinueInteractable)
            {
                Fail(
                    "Saved seed-78 run did not expose an interactable " +
                    "Continue action.");
                yield break;
            }
            yield return CaptureCheckpoint(
                "main-menu-saved-run",
                "The saved seed-78 map run is visible on the main menu.",
                fileOrdinal: 20);
            if (failed)
            {
                yield break;
            }
            if (!ExecuteStep(
                    "continue saved seed-78 run through ContinueButton",
                    () => InvokeNamedButton(
                        mainMenuController.ScreenView,
                        "ContinueButton")))
            {
                yield break;
            }
            yield return WaitForScene<RunTestController>(
                GameSceneNames.Run);
            if (failed)
            {
                yield break;
            }
            yield return WaitForRunPresentationReady(
                "continued run restoration");
            if (failed)
            {
                yield break;
            }

            var resumedRun = GameApp.Instance.Run;
            if (resumedRun == null ||
                resumedRun.State.Phase != expectedPhase ||
                !string.Equals(
                    RunStateFingerprint.Compute(mapper.Capture(resumedRun)),
                    expectedFingerprint,
                    StringComparison.Ordinal))
            {
                Fail(
                    "Continue did not restore the frozen visual chain's " +
                    "exact saved domain fingerprint.");
                yield break;
            }
            yield return CaptureCheckpoint(
                "continued-run",
                $"Continue restored phase {expectedPhase} with an exact " +
                "domain fingerprint.",
                fileOrdinal: 21);
            if (failed)
            {
                yield break;
            }

            yield return CompleteAcceptance(
                "Frozen visual acceptance completed the legal seed-78 " +
                "four-shop/four-battle elite chain, 19-node segmented map, " +
                "strict reward, and exact save/continue restoration.");
        }

        private IEnumerator WaitForRunPresentationReady(string description)
        {
            yield return WaitForCondition(
                () =>
                {
                    var controller =
                        Object.FindObjectOfType<RunTestController>();
                    var activeRun = GameApp.Instance?.Run;
                    var expectedNodes =
                        activeRun?.State?.CurrentMap?.Nodes?.Count ?? 0;
                    return controller != null &&
                           controller.IsInitialized &&
                           controller.FormalScreenView != null &&
                           controller.FormalScreenView.HasCompleteBindings &&
                           expectedNodes > 0 &&
                           controller.NodeButtonCount == expectedNodes;
                },
                "formal Run presentation after " + description);
        }

        private IEnumerator WaitForMainMenuPresentationReady(
            string description)
        {
            yield return WaitForCondition(
                () =>
                {
                    var controller =
                        Object.FindObjectOfType<MainMenuController>();
                    return controller?.ScreenView != null &&
                           controller.ScreenView.ContinueInteractable &&
                           IsNamedButtonReady(
                               controller.ScreenView,
                               "ContinueButton");
                },
                "formal MainMenu presentation after " + description);
        }

        private IEnumerator WaitForShopPresentationReady(
            string description,
            string expectedButtonName = "RefreshButton")
        {
            yield return WaitForCondition(
                () =>
                {
                    var controller =
                        Object.FindObjectOfType<ShopTestController>();
                    return controller != null &&
                           controller.IsInitialized &&
                           controller.IsUsingFormalView &&
                           controller.FormalScreenView != null &&
                           controller.FormalScreenView.HasCompleteBindings &&
                           IsNamedButtonReady(
                               controller.FormalScreenView,
                               expectedButtonName);
                },
                "formal Shop presentation after " + description);
        }

        private IEnumerator WaitForBattlePresentationReady(
            string description)
        {
            yield return WaitForCondition(
                () =>
                {
                    var controller =
                        Object.FindObjectOfType<BattleTestController>();
                    var screen =
                        Object.FindObjectOfType<BattleScreenView>();
                    return controller != null &&
                           controller.IsRunBattle &&
                           controller.UsesFormalView &&
                           controller.SetupState != null &&
                           screen != null &&
                           screen.HasCompleteBindings &&
                           TryValidateActiveBattleStandees(
                               screen,
                               out _) &&
                           IsNamedButtonReady(screen, "Start");
                },
                "formal Battle presentation after " + description);
        }

        private IEnumerator ResolveFrozenBattleAndReturn(
            string description,
            BattleSide? expectedWinner)
        {
            yield return WaitForBattlePresentationReady(description);
            if (failed)
            {
                yield break;
            }

            var battleController =
                Object.FindObjectOfType<BattleTestController>();
            var battleScreen =
                Object.FindObjectOfType<BattleScreenView>();
            var standeeDetails = string.Empty;
            if (battleController == null ||
                battleScreen == null ||
                !battleController.IsRunBattle ||
                !battleController.UsesFormalView ||
                !battleScreen.HasCompleteBindings ||
                !TryValidateBattleStandees(
                    battleController,
                    battleScreen,
                    -1,
                    -1,
                    out standeeDetails))
            {
                Fail(
                    $"The {description} did not render its formal battle " +
                    $"standees. {standeeDetails}");
                yield break;
            }

            var activeRun = GameApp.Instance.Run;
            if (activeRun == null)
            {
                Fail($"The {description} has no active RunSession.");
                yield break;
            }
            var settlementsBefore = SettlementCount(activeRun);
            if (!ExecuteStep(
                    $"start {description} through Start button",
                    () => InvokeNamedButton(battleScreen, "Start")))
            {
                yield break;
            }
            yield return null;
            if (!ExecuteStep(
                    $"skip {description} playback through Skip button",
                    () => InvokeNamedButton(battleScreen, "Skip")))
            {
                yield break;
            }
            yield return WaitForCondition(
                () => battleController.LastResult != null &&
                      !battleScreen.IsAnimationPlaying &&
                      battleScreen.ActiveFeedbackFxCount == 0 &&
                      !HasActiveNonLoopingAudio() &&
                      !HasActiveNonLoopingAnimation() &&
                      battleScreen.IsResultVisible &&
                      IsNamedButtonReady(battleScreen, "Return"),
                $"{description} settlement");
            if (failed)
            {
                yield break;
            }
            if (!ReferenceEquals(
                    activeRun.LastBattleResult,
                    battleController.LastResult) ||
                SettlementCount(activeRun) != settlementsBefore + 1)
            {
                Fail(
                    $"The {description} did not settle its RunSession " +
                    "exactly once.");
                yield break;
            }
            if (battleController.LastResult.Winner != expectedWinner)
            {
                Fail(
                    $"The deterministic {description} winner was " +
                    $"{battleController.LastResult.Winner?.ToString() ?? "Draw"}, " +
                    "expected " +
                    $"{expectedWinner?.ToString() ?? "Draw"}.");
                yield break;
            }

            if (!ExecuteStep(
                    $"return {description} through Return button",
                    () => InvokeNamedButton(battleScreen, "Return")))
            {
                yield break;
            }
            yield return WaitForScene<RunTestController>(
                GameSceneNames.Run);
            if (failed)
            {
                yield break;
            }
            yield return WaitForRunPresentationReady(
                description + " result return");
            if (failed)
            {
                yield break;
            }

            var runController =
                Object.FindObjectOfType<RunTestController>();
            if (!ExecuteStep(
                    $"acknowledge {description} through ActionButton",
                    () => GameApp.Instance.Run.State.Phase ==
                          RunPhase.BattleResult &&
                          InvokeNamedButton(
                              runController?.FormalScreenView,
                              "ActionButton") &&
                          GameApp.Instance.Run.State.Phase ==
                          RunPhase.MapSelection))
            {
                yield break;
            }
            yield return null;
        }

        private bool ValidateFrozenShop(
            ShopTestController controller,
            int expectedRound,
            int expectedGold,
            params string[] requiredMinionOfferIds)
        {
            var session = controller?.Session;
            if (controller?.FormalScreenView == null ||
                session == null ||
                !controller.IsUsingFormalView ||
                !session.IsShopOpen ||
                session.Round != expectedRound ||
                session.Gold != expectedGold ||
                requiredMinionOfferIds.Any(requiredId =>
                    session.MinionOffers.All(offer =>
                        offer?.Id != requiredId)))
            {
                Fail(
                    $"Round-{expectedRound} deterministic shop contract " +
                    $"failed; expected gold={expectedGold}, offers=" +
                    string.Join(",", requiredMinionOfferIds) + ".");
                return false;
            }

            return true;
        }

        private static bool BuyConfiguredMinionThroughUi(
            ShopTestController controller,
            string configId)
        {
            var session = controller?.Session;
            if (session == null)
            {
                return false;
            }

            var offerIndex = Enumerable
                .Range(0, session.MinionOffers.Count)
                .Where(index =>
                    session.MinionOffers[index]?.Id == configId)
                .DefaultIfEmpty(-1)
                .First();
            var offerCard = FindShopCard(
                ShopCardZone.MinionOffer,
                offerIndex);
            return offerIndex >= 0 &&
                   InvokePointerClick(offerCard) &&
                   controller.LastOperationResult?.Success == true &&
                   FindBenchCardIndex(controller, configId) >= 0;
        }

        private static bool PlayConfiguredBenchMinionThroughUi(
            ShopTestController controller,
            string configId,
            int battleIndex)
        {
            var benchIndex = FindBenchCardIndex(controller, configId);
            var benchCard = FindShopCard(
                ShopCardZone.Bench,
                benchIndex);
            var battleSlot = FindShopSlot(
                ShopCardZone.Battle,
                battleIndex);
            return benchIndex >= 0 &&
                   benchCard != null &&
                   battleSlot != null &&
                   InvokePointerClick(benchCard) &&
                   InvokePointerClick(battleSlot) &&
                   controller.Session.Collection.Bench[benchIndex] == null &&
                   controller.Session.Collection.Battle[battleIndex]
                       ?.ConfigId == configId &&
                   controller.LastOperationResult?.Success == true;
        }

        private static bool BuyConfiguredSpellThroughUi(
            ShopTestController controller,
            string configId)
        {
            if (controller?.Session?.SpellOffer?.Id != configId)
            {
                return false;
            }

            var spellOffer = FindShopCard(
                ShopCardZone.SpellOffer,
                0);
            return InvokePointerClick(spellOffer) &&
                   controller.LastOperationResult?.Success == true &&
                   controller.Session.SpellOffer == null &&
                   FindBenchCardIndex(controller, configId) >= 0;
        }

        private static bool TargetSelectedSpellThroughUi(
            ShopTestController controller,
            int benchIndex,
            int battleIndex,
            string configId)
        {
            var battleCard = FindShopCard(
                ShopCardZone.Battle,
                battleIndex);
            var battleSlot = FindShopSlot(
                ShopCardZone.Battle,
                battleIndex);
            var invoked = battleCard != null
                ? InvokePointerClick(battleCard)
                : InvokePointerClick(battleSlot);
            return invoked &&
                   controller.LastOperationResult?.Success == true &&
                   controller.Session.Collection.Bench[benchIndex] == null &&
                   FindBenchCardIndex(controller, configId) < 0;
        }

        private static int FindBenchCardIndex(
            ShopTestController controller,
            string configId)
        {
            var bench = controller?.Session?.Collection?.Bench;
            if (bench == null)
            {
                return -1;
            }

            return Enumerable
                .Range(0, bench.Count)
                .Where(index => bench[index]?.ConfigId == configId)
                .DefaultIfEmpty(-1)
                .First();
        }

        private static bool InvokeMapNodeInVisibleSegment(
            RunTestController controller,
            string nodeId)
        {
            var view = controller?.FormalScreenView;
            if (view == null)
            {
                return false;
            }

            foreach (var segment in new[]
                     {
                         RunMapViewportSegment.Left,
                         RunMapViewportSegment.Center,
                         RunMapViewportSegment.Right
                     })
            {
                var snapshot = view.SetMapViewportSegment(segment);
                if (snapshot.FullyVisibleNodeIds.Contains(nodeId) &&
                    InvokeMapNode(controller, nodeId))
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerator RunStressAcceptanceFlow(int seed)
        {
            GameApp.Instance.AbandonRun();
            GameApp.Instance.StartNewRun(seed);
            if (GameApp.Instance.Run == null)
            {
                Fail("G4 stress run could not create its isolated session.");
                yield break;
            }

            G4SceneLoadDiagnostics.NotifySceneLoadRequested(
                GameSceneNames.Shop);
            SceneManager.LoadScene(GameSceneNames.Shop);
            yield return WaitForScene<ShopTestController>(
                GameSceneNames.Shop);
            if (failed)
            {
                yield break;
            }
            yield return WaitForShopPresentationReady(
                "stress shop entry");
            if (failed)
            {
                yield break;
            }

            var shopController =
                Object.FindObjectOfType<ShopTestController>();
            if (!ExecuteStep(
                    "build ten-card compact shop through legal economy",
                    () => BuildTenCardStressShop(
                        shopController,
                        out _)))
            {
                yield break;
            }
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();

            var activeCards = Object.FindObjectsOfType<CardView>()
                .Where(card =>
                    card != null &&
                    card.gameObject.activeInHierarchy)
                .ToArray();
            var routedCards = activeCards
                .Select(card => new
                {
                    Card = card,
                    Route = card.GetComponent<ShopCardView>()
                })
                .ToArray();
            if (shopController?.FormalScreenView == null ||
                shopController.FormalScreenView.RenderedCardCount != 13 ||
                activeCards.Length != 13 ||
                activeCards.Any(card =>
                    !card.HasCompleteBindings ||
                    !card.transform.IsChildOf(
                        shopController.FormalScreenView.transform)) ||
                routedCards.Any(value => value.Route == null) ||
                routedCards.Count(value =>
                    value.Route.Zone == ShopCardZone.Battle &&
                    value.Card.CurrentDisplayMode ==
                    CardDisplayMode.Compact) != 5 ||
                routedCards.Count(value =>
                    value.Route.Zone == ShopCardZone.Bench &&
                    value.Card.CurrentDisplayMode ==
                    CardDisplayMode.Compact) != 5 ||
                routedCards.Count(value =>
                    value.Route.Zone == ShopCardZone.MinionOffer &&
                    value.Card.CurrentDisplayMode !=
                    CardDisplayMode.Compact) != 2 ||
                routedCards.Count(value =>
                    value.Route.Zone == ShopCardZone.SpellOffer &&
                    value.Card.CurrentDisplayMode !=
                    CardDisplayMode.Compact) != 1)
            {
                Fail(
                    "G4 compact-card stress fixture did not render exactly " +
                    "five Compact battle cards, five Compact bench cards, " +
                    "two full minion offers, and one full spell offer inside " +
                    "the formal shop.");
                yield break;
            }
            yield return CaptureCheckpoint(
                "stress-shop-ten-compact",
                "Formal shop rendered five battle cards plus five hand cards " +
                "in Compact mode, alongside two minion offers and one spell " +
                "offer, after legal economy operations.");
            if (failed)
            {
                yield break;
            }

            var playbackModes = new[]
            {
                StressPlaybackMode.Normal,
                StressPlaybackMode.Accelerated,
                StressPlaybackMode.Skip,
                StressPlaybackMode.Accelerated,
                StressPlaybackMode.Skip
            };
            var mapper = new RunSnapshotMapper(GameApp.Instance.Configs);
            string expectedBattleHash = null;
            string expectedRunFingerprint = null;
            var nestedDetails = string.Empty;
            for (var playbackIndex = 0;
                 playbackIndex < playbackModes.Length;
                 playbackIndex++)
            {
                var playbackMode = playbackModes[playbackIndex];
                var run = PrepareNestedStressRun(seed);
                if (run == null)
                {
                    Fail(
                        "G4 nested-summon stress fixture could not reach a " +
                        "legal pending battle.");
                    yield break;
                }

                var settlementsBefore = SettlementCount(run);
                G4SceneLoadDiagnostics.NotifySceneLoadRequested(
                    GameSceneNames.Battle);
                SceneManager.LoadScene(GameSceneNames.Battle);
                // A same-scene reload leaves the previous controller visible
                // until the next frame. Wait it out before checking readiness.
                yield return null;
                yield return null;
                yield return WaitForScene<BattleTestController>(
                    GameSceneNames.Battle);
                if (failed)
                {
                    yield break;
                }
                yield return WaitForBattlePresentationReady(
                    $"stress battle {playbackMode} entry");
                if (failed)
                {
                    yield break;
                }

                var battleController =
                    Object.FindObjectOfType<BattleTestController>();
                var battleScreen =
                    Object.FindObjectOfType<BattleScreenView>();
                var standeeDetails = string.Empty;
                if (battleController == null ||
                    battleScreen == null ||
                    !battleController.IsRunBattle ||
                    !battleController.UsesFormalView ||
                    !battleScreen.HasCompleteBindings ||
                    !TryValidateBattleStandees(
                        battleController,
                        battleScreen,
                        3,
                        1,
                        out standeeDetails))
                {
                    Fail(
                        "G4 nested-summon stress fixture did not render its " +
                        "four expected formal battle standees. " +
                        standeeDetails);
                    yield break;
                }

                if (playbackIndex == 0)
                {
                    yield return CaptureCheckpoint(
                        "stress-battle-nested-ready",
                        "Formal battle rendered three deterministic player " +
                        "standees and one enemy standee before playback.");
                    if (failed)
                    {
                        yield break;
                    }
                }

                switch (playbackMode)
                {
                    case StressPlaybackMode.Normal:
                        battleController.StartBattle();
                        break;
                    case StressPlaybackMode.Accelerated:
                        battleController.TogglePlaybackSpeed();
                        if (Mathf.Abs(
                                battleController.PlaybackSpeed - 2f) > 0.01f)
                        {
                            Fail(
                                "Nested stress battle did not enter 2x " +
                                "playback mode.");
                            yield break;
                        }
                        battleController.StartBattle();
                        break;
                    case StressPlaybackMode.Skip:
                        battleController.StartBattle();
                        yield return null;
                        battleController.SkipPlayback();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (playbackIndex == 0)
                {
                    yield return WaitForBattleFeedback(
                        battleController,
                        battleScreen,
                        "battle_stats",
                        "tomb-guardian group stat presentation",
                        () => HasRenderedBattleStandeeId(
                                  battleScreen,
                                  "g4-stress:fox-matriarch") &&
                              HasRenderedBattleStandeeId(
                                  battleScreen,
                                  "g4-stress:young-deer"));
                    if (failed)
                    {
                        yield break;
                    }
                }

                yield return WaitForCondition(
                    () => battleController.LastResult != null &&
                          !battleScreen.IsAnimationPlaying &&
                          battleScreen.ActiveFeedbackFxCount == 0 &&
                          !HasActiveNonLoopingAudio() &&
                          !HasActiveNonLoopingAnimation() &&
                          battleScreen.IsResultVisible &&
                          IsNamedButtonReady(battleScreen, "Return"),
                    $"nested deathrattle {playbackMode} settlement, result " +
                    "overlay, and transient cleanup",
                    90f);
                if (failed)
                {
                    yield break;
                }

                var result = battleController.LastResult;
                if (!ValidateNestedDeathrattleStressResult(
                        result,
                        out nestedDetails))
                {
                    Fail(nestedDetails);
                    yield break;
                }
                if (!ReferenceEquals(run.LastBattleResult, result) ||
                    SettlementCount(run) != settlementsBefore + 1)
                {
                    Fail(
                        $"Nested stress {playbackMode} playback did not " +
                        "settle the RunSession exactly once.");
                    yield break;
                }

                var battleHash =
                    BattleDeterminismHasher.Compute(result);
                var runFingerprint =
                    ComparableRunFingerprint(mapper, run);
                if (expectedBattleHash == null)
                {
                    expectedBattleHash = battleHash;
                    expectedRunFingerprint = runFingerprint;
                }
                else if (!string.Equals(
                             expectedBattleHash,
                             battleHash,
                             StringComparison.Ordinal) ||
                         !string.Equals(
                             expectedRunFingerprint,
                             runFingerprint,
                             StringComparison.Ordinal))
                {
                    Fail(
                        $"Nested stress {playbackMode} playback diverged " +
                        "from the 1x domain result or settled run snapshot.");
                    yield break;
                }

                var settledFingerprint =
                    ComparableRunFingerprint(mapper, run);
                var settledCount = SettlementCount(run);
                battleController.SkipPlayback();
                battleController.StartBattle();
                var reenteredResult =
                    battleController.ResolveImmediately();
                yield return null;
                var reentryFingerprint =
                    ComparableRunFingerprint(mapper, run);
                if (!ReferenceEquals(reenteredResult, result) ||
                    !ReferenceEquals(run.LastBattleResult, result) ||
                    SettlementCount(run) != settledCount ||
                    !string.Equals(
                        settledFingerprint,
                        reentryFingerprint,
                        StringComparison.Ordinal) ||
                    battleScreen.IsAnimationPlaying ||
                    battleScreen.ActiveFeedbackFxCount != 0 ||
                    HasActiveNonLoopingAudio() ||
                    HasActiveNonLoopingAnimation())
                {
                    Fail(
                        $"Nested stress {playbackMode} playback changed its " +
                        "settled state after public re-entry operations.");
                    yield break;
                }

                if (playbackIndex == playbackModes.Length - 1)
                {
                    yield return CaptureCheckpoint(
                        "stress-battle-nested-result",
                        nestedDetails + " Five in-process rounds covering " +
                        "1x, 2x, and Skip produced identical battle hashes " +
                        "and normalized Run fingerprints.");
                    if (failed)
                    {
                        yield break;
                    }
                }
            }

            yield return new WaitForSecondsRealtime(30f);
            var finalBattleScreen =
                Object.FindObjectOfType<BattleScreenView>();
            if (finalBattleScreen == null ||
                finalBattleScreen.IsAnimationPlaying ||
                finalBattleScreen.ActiveFeedbackFxCount != 0 ||
                HasActiveNonLoopingAudio() ||
                HasActiveNonLoopingAnimation())
            {
                Fail(
                    "Stress presentation transients did not remain at zero " +
                    "after the five-round 30-second stabilization window.");
                yield break;
            }

            yield return CompleteAcceptance(
                "Formal ten-card Compact and nested deathrattle stress " +
                "fixtures completed across five in-process rounds with " +
                "1x/2x/Skip equivalence, exactly-once settlement, and a " +
                "30-second zero-transient stabilization window.");
        }

        private static bool BuildTenCardStressShop(
            ShopTestController controller,
            out string details)
        {
            details = string.Empty;
            var session = controller?.Session;
            if (controller?.FormalScreenView == null ||
                session == null)
            {
                details = "Formal shop controller is unavailable.";
                return false;
            }

            for (var round = 1; round <= 8; round++)
            {
                if (session.Round != round ||
                    !session.IsShopOpen ||
                    session.Gold !=
                    ShopEconomyRules.GetRoundBudget(round))
                {
                    details =
                        $"Stress shop round {round} did not start with the " +
                        "expected legal economy.";
                    return false;
                }

                switch (round)
                {
                    case 1:
                    case 3:
                    case 7:
                        if (!BuyStressMinions(
                                controller,
                                1,
                                true))
                        {
                            details =
                                $"Stress shop round {round} could not buy " +
                                "one non-tripling minion.";
                            return false;
                        }
                        break;
                    case 5:
                        if (!BuyStressMinions(
                                controller,
                                2,
                                true))
                        {
                            details =
                                "Stress shop round 5 could not buy two " +
                                "non-tripling minions.";
                            return false;
                        }
                        break;
                    case 2:
                    case 4:
                    case 6:
                    case 8:
                        if (!controller.UpgradeTavern().Success)
                        {
                            details =
                                $"Stress shop round {round} could not apply " +
                                "its legal discounted tavern upgrade.";
                            return false;
                        }
                        if (round == 8 &&
                            !controller.RefreshShop().Success)
                        {
                            details =
                                "Stress shop round 8 could not spend its " +
                                "remaining legal gold on the tier-5 refresh.";
                            return false;
                        }
                        break;
                }

                if (round == 8)
                {
                    break;
                }

                if (!session.EndRound().Success ||
                    !session.StartNextRound().Success)
                {
                    details =
                        $"Stress shop could not advance legally after " +
                        $"round {round}.";
                    return false;
                }
            }

            if (!PlayAllStressBenchMinions(controller))
            {
                details =
                    "Stress shop could not legally fill all five battle slots.";
                return false;
            }
            if (!session.EndRound().Success ||
                !session.StartNextRound().Success ||
                session.Round != 9 ||
                !BuyStressMinions(controller, 3, false) ||
                !session.EndRound().Success ||
                !session.StartNextRound().Success ||
                session.Round != 10 ||
                !BuyStressMinions(controller, 2, false))
            {
                details =
                    "Stress shop could not legally fill all five hand slots " +
                    "during rounds 9 and 10.";
                return false;
            }

            var owned = session.Collection.Bench
                .Concat(session.Collection.Battle)
                .Where(card => card != null)
                .ToArray();
            var offeredMinions =
                session.MinionOffers.Count(offer => offer != null);
            if (session.TavernTier !=
                    ShopEconomyRules.MaximumTavernTier ||
                session.Collection.Bench.Count(card => card != null) != 5 ||
                session.Collection.Battle.Count(card => card != null) != 5 ||
                owned.Length != 10 ||
                owned.GroupBy(card => card.ConfigId)
                    .Any(group => group.Count() >= 3) ||
                offeredMinions != 2 ||
                session.SpellOffer == null)
            {
                details =
                    "Stress shop did not reach tier 5 with five battle cards, " +
                    "five hand cards, and three offers.";
                return false;
            }

            details =
                "tier=5, battle=5, hand=5, minionOffers=2, spellOffers=1";
            return true;
        }

        private static bool BuyStressMinions(
            ShopTestController controller,
            int count,
            bool requireChoiceFreePlay)
        {
            var session = controller.Session;
            for (var purchaseNumber = 0;
                 purchaseNumber < count;
                 purchaseNumber++)
            {
                var ownedCounts = session.Collection.Bench
                    .Concat(session.Collection.Battle)
                    .Where(card => card != null)
                    .GroupBy(card => card.ConfigId)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Count());
                var offerIndex = Enumerable
                    .Range(0, session.MinionOffers.Count)
                    .Where(index =>
                    {
                        var offer = session.MinionOffers[index];
                        return offer != null &&
                               (!requireChoiceFreePlay ||
                                offer.Effects == null ||
                                offer.Effects.All(effect =>
                                    effect == null ||
                                    effect.Trigger != "OnPlay")) &&
                               (!ownedCounts.TryGetValue(
                                    offer.Id,
                                    out var owned) ||
                                owned < 2);
                    })
                    .DefaultIfEmpty(-1)
                    .First();
                if (offerIndex < 0 ||
                    !controller.BuyMinionAt(offerIndex).Success)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool PlayAllStressBenchMinions(
            ShopTestController controller)
        {
            var session = controller.Session;
            for (var battleIndex = 0;
                 battleIndex < ShopEconomyRules.BattleSlotCount;
                 battleIndex++)
            {
                var benchIndex = Enumerable
                    .Range(0, ShopEconomyRules.BenchSlotCount)
                    .Where(index =>
                        session.Collection.Bench[index] != null)
                    .DefaultIfEmpty(-1)
                    .First();
                if (benchIndex < 0 ||
                    !controller.PlayBenchMinion(
                        benchIndex,
                        battleIndex).Success ||
                    session.PendingDiscover != null ||
                    session.PendingChoice != null)
                {
                    return false;
                }
            }

            return true;
        }

        private static RunSession PrepareNestedStressRun(int seed)
        {
            GameApp.Instance.StartNewRun(seed);
            var run = GameApp.Instance.Run;
            if (run == null ||
                !run.EnterNode("f1_shop_start").Success ||
                !run.EndShopAndPrepareBattle(GameSceneNames.Run).Success ||
                !run.EnterNode("f1_opening_normal").Success ||
                run.PendingBattle == null)
            {
                return null;
            }

            var originalContext = run.PendingBattle;
            run.PrepareBattle(new BattleContext(
                BuildNestedDeathrattleStressBoard(),
                "G4 nested deathrattle stress fixture",
                GameSceneNames.Run,
                originalContext.NodeAttemptId,
                originalContext.EncounterId,
                seed));
            return run;
        }

        private static int SettlementCount(RunSession run)
        {
            return run.State.Statistics.BattlesWon +
                   run.State.Statistics.BattlesNotWon;
        }

        private static string ComparableRunFingerprint(
            RunSnapshotMapper mapper,
            RunSession run)
        {
            var payload = mapper.Capture(run);
            payload.RunState.Statistics.StartedAtUtc = default(DateTime);
            payload.RunState.Statistics.CompletedAtUtc = null;
            return RunStateFingerprint.Compute(payload);
        }

        private static bool TryValidateBattleStandees(
            BattleTestController controller,
            BattleScreenView screen,
            int expectedPlayerCount,
            int expectedEnemyCount,
            out string details)
        {
            details = string.Empty;
            var setup = controller?.SetupState;
            if (setup == null || screen == null)
            {
                details = "Battle controller, setup, or screen is missing.";
                return false;
            }

            var playerCount =
                setup.Player.Count(value => value != null && value.IsAlive);
            var enemyCount =
                setup.Enemy.Count(value => value != null && value.IsAlive);
            var expected = setup.Player
                .Select((value, index) => new { value, index })
                .Where(entry =>
                    entry.value != null && entry.value.IsAlive)
                .Select(entry => new
                {
                    RuntimeInstanceId = ResolveExpectedBattleInstanceId(
                        entry.value,
                        BattleSide.Player,
                        entry.index),
                    Side = BattleSide.Player
                })
                .Concat(setup.Enemy
                    .Select((value, index) => new { value, index })
                    .Where(entry =>
                        entry.value != null && entry.value.IsAlive)
                    .Select(entry => new
                    {
                        RuntimeInstanceId = ResolveExpectedBattleInstanceId(
                            entry.value,
                            BattleSide.Enemy,
                            entry.index),
                        Side = BattleSide.Enemy
                    }))
                .ToArray();
            var active = Object.FindObjectsOfType<BattleStandeeView>()
                .Where(value =>
                    value != null &&
                    value.gameObject.activeInHierarchy)
                .ToArray();
            if ((expectedPlayerCount >= 0 &&
                 playerCount != expectedPlayerCount) ||
                enemyCount <= 0 ||
                (expectedEnemyCount >= 0 &&
                 enemyCount != expectedEnemyCount) ||
                expected.Length == 0 ||
                expected.Any(value =>
                    string.IsNullOrWhiteSpace(value.RuntimeInstanceId)) ||
                expected.Select(value => value.RuntimeInstanceId)
                    .Distinct(StringComparer.Ordinal)
                    .Count() != expected.Length ||
                screen.RenderedCardCount != expected.Length ||
                active.Length != expected.Length ||
                active.Any(value =>
                    !value.HasCompleteBindings ||
                    value.Model == null ||
                    string.IsNullOrWhiteSpace(value.InstanceId) ||
                    !value.transform.IsChildOf(screen.transform)))
            {
                details =
                    $"setup={expected.Length}, player={playerCount}, " +
                    $"enemy={enemyCount}, " +
                    $"rendered={screen.RenderedCardCount}, " +
                    $"active={active.Length}.";
                return false;
            }

            foreach (var expectedStandee in expected)
            {
                var matches = active.Where(value =>
                        string.Equals(
                            value.InstanceId,
                            expectedStandee.RuntimeInstanceId,
                            StringComparison.Ordinal) &&
                        value.Side == expectedStandee.Side)
                    .ToArray();
                if (matches.Length != 1)
                {
                    details =
                        $"Runtime standee '{expectedStandee.RuntimeInstanceId}' " +
                        $"on {expectedStandee.Side} rendered {matches.Length} " +
                        "times.";
                    return false;
                }
            }

            details =
                $"Validated {playerCount} player and " +
                $"{expected.Length - playerCount} enemy formal standees.";
            return true;
        }

        private static string ResolveExpectedBattleInstanceId(
            BattleMinionRuntime minion,
            BattleSide side,
            int slotIndex)
        {
            return !string.IsNullOrWhiteSpace(minion.RuntimeInstanceId)
                ? minion.RuntimeInstanceId
                : $"{side}:{slotIndex}:{minion.Id}";
        }

        private static bool TryValidateActiveBattleStandees(
            BattleScreenView screen,
            out string details)
        {
            details = string.Empty;
            if (screen == null)
            {
                details = "Battle screen is missing.";
                return false;
            }

            var active = Object.FindObjectsOfType<BattleStandeeView>()
                .Where(value =>
                    value != null &&
                    value.gameObject.activeInHierarchy)
                .ToArray();
            if (active.Length == 0 ||
                active.Length != screen.RenderedCardCount ||
                active.Any(value =>
                    !value.HasCompleteBindings ||
                    value.Model == null ||
                    string.IsNullOrWhiteSpace(value.InstanceId) ||
                    !value.transform.IsChildOf(screen.transform)))
            {
                details =
                    $"rendered={screen.RenderedCardCount}, " +
                    $"active={active.Length}.";
                return false;
            }

            details = $"Validated {active.Length} active formal standees.";
            return true;
        }

        private static bool HasRenderedBattleStandee(
            BattleScreenView screen,
            string runtimeIdSuffix)
        {
            return TryValidateActiveBattleStandees(screen, out _) &&
                   Object.FindObjectsOfType<BattleStandeeView>().Any(value =>
                       value != null &&
                       value.gameObject.activeInHierarchy &&
                       value.transform.IsChildOf(screen.transform) &&
                       EndsWithRuntimeId(
                           value.InstanceId,
                           runtimeIdSuffix));
        }

        private static bool HasRenderedBattleStandeeId(
            BattleScreenView screen,
            string runtimeInstanceId)
        {
            return TryValidateActiveBattleStandees(screen, out _) &&
                   Object.FindObjectsOfType<BattleStandeeView>().Any(value =>
                       value != null &&
                       value.gameObject.activeInHierarchy &&
                       value.transform.IsChildOf(screen.transform) &&
                       string.Equals(
                           value.InstanceId,
                           runtimeInstanceId,
                           StringComparison.Ordinal));
        }

        private static BattleBoardState BuildNestedDeathrattleStressBoard()
        {
            var configs = GameApp.Instance.Configs;
            var board = new BattleBoardState();
            board.Player[0] = new BattleMinionRuntime(
                configs.MinionsById["fox_den_matriarch"],
                isGolden: true,
                initialHealth: 1,
                sourceInstanceId: "g4-stress:fox-matriarch",
                permanentKeywords: new[] { "Shield" },
                runtimeInstanceId: "g4-stress:fox-matriarch");
            board.Player[1] = new BattleMinionRuntime(
                configs.MinionsById["young_deer_spirit"],
                initialHealth: 1,
                sourceInstanceId: "g4-stress:young-deer",
                permanentKeywords: new[] { "Shield" },
                runtimeInstanceId: "g4-stress:young-deer");
            board.Player[2] = new BattleMinionRuntime(
                configs.MinionsById["thousand_ring_tomb_guardian"],
                initialHealth: 1,
                sourceInstanceId: "g4-stress:tomb-guardian",
                runtimeInstanceId: "g4-stress:tomb-guardian");
            board.Enemy[0] = new BattleMinionRuntime(
                configs.MinionsById["mirrorsteel_duelist"],
                initialAttack: 100,
                initialHealth: 300,
                runtimeInstanceId: "g4-stress:enemy");
            return board;
        }

        private static bool ValidateNestedDeathrattleStressResult(
            BattleSimulationResult result,
            out string details)
        {
            if (result == null)
            {
                details = "Nested deathrattle stress battle has no result.";
                return false;
            }
            if (result.Diagnostics.HitEffectLimit ||
                result.Diagnostics.Player.SummonSuccesses < 4 ||
                result.Diagnostics.Player.NonTokenDeaths < 2 ||
                result.Diagnostics.Player.TokenDeaths < 3)
            {
                details =
                    "Nested deathrattle diagnostics failed: " +
                    $"effectLimit={result.Diagnostics.HitEffectLimit}, " +
                    $"summons={result.Diagnostics.Player.SummonSuccesses}, " +
                    $"nonTokenDeaths=" +
                    $"{result.Diagnostics.Player.NonTokenDeaths}, " +
                    $"tokenDeaths={result.Diagnostics.Player.TokenDeaths}.";
                return false;
            }

            const string foxId = "g4-stress:fox-matriarch";
            const string deerId = "g4-stress:young-deer";
            var expectedGroupTargets =
                new HashSet<string>(
                    new[] { foxId, deerId },
                    StringComparer.Ordinal);
            var events = result.PlaybackEvents;
            var changedGroupTargets = new HashSet<string>(
                events.Where(value =>
                        value.Kind == BattlePlaybackEventKind.StatsChanged &&
                        value.AttackDelta == 1 &&
                        value.HealthDelta == 1 &&
                        expectedGroupTargets.Contains(value.TargetInstanceId))
                    .Select(value => value.TargetInstanceId),
                StringComparer.Ordinal);
            var permanentGroupTargets = new HashSet<string>(
                result.PermanentDeltas.Where(value =>
                        value.Attack == 1 &&
                        value.Health == 1 &&
                        value.ApplicationCount == 1 &&
                        expectedGroupTargets.Contains(value.SourceInstanceId))
                    .Select(value => value.SourceInstanceId),
                StringComparer.Ordinal);
            if (!changedGroupTargets.SetEquals(expectedGroupTargets) ||
                !permanentGroupTargets.SetEquals(expectedGroupTargets))
            {
                details =
                    "Thousand-ring tomb guardian did not visibly and " +
                    "permanently apply +1/+1 to both surviving non-token " +
                    "allies.";
                return false;
            }

            var foxDeath = FindBattleEventIndex(
                events,
                0,
                value =>
                    value.Kind == BattlePlaybackEventKind.UnitDied &&
                    value.TargetInstanceId == foxId);
            var shadowSummon = FindBattleEventIndex(
                events,
                foxDeath + 1,
                value =>
                    value.Kind == BattlePlaybackEventKind.UnitSummoned &&
                    EndsWithRuntimeId(
                        value.TargetInstanceId,
                        "token_two_tailed_fox_shadow"));
            var shadowRuntimeId = shadowSummon < 0
                ? null
                : events[shadowSummon].TargetInstanceId;
            var shadowDeath = FindBattleEventIndex(
                events,
                shadowSummon + 1,
                value =>
                    value.Kind == BattlePlaybackEventKind.UnitDied &&
                    value.TargetInstanceId == shadowRuntimeId);
            var nestedYoungSpiritSummons = shadowDeath < 0
                ? 0
                : events.Skip(shadowDeath + 1).Count(value =>
                    value.Kind == BattlePlaybackEventKind.UnitSummoned &&
                    value.SourceInstanceId == shadowRuntimeId &&
                    EndsWithRuntimeId(
                        value.TargetInstanceId,
                        "token_young_spirit"));
            if (foxDeath < 0 ||
                shadowSummon <= foxDeath ||
                shadowDeath <= shadowSummon ||
                nestedYoungSpiritSummons < 2)
            {
                details =
                    "Nested deathrattle playback order did not include fox " +
                    "death -> shadow summon -> shadow death -> nested young " +
                    "spirit summons.";
                return false;
            }

            details =
                "Nested deathrattle passed: " +
                $"processedEffects={result.Diagnostics.ProcessedEffectCount}, " +
                $"summons={result.Diagnostics.Player.SummonSuccesses}, " +
                $"nestedYoungSpiritSummons={nestedYoungSpiritSummons}, " +
                "groupPermanentTargets=2; " +
                "all presentation transients returned to zero.";
            return true;
        }

        private static int FindBattleEventIndex(
            IReadOnlyList<BattlePlaybackEvent> events,
            int startIndex,
            Func<BattlePlaybackEvent, bool> predicate)
        {
            for (var index = Math.Max(0, startIndex);
                 index < events.Count;
                 index++)
            {
                if (predicate(events[index]))
                {
                    return index;
                }
            }

            return -1;
        }

        private static bool EndsWithRuntimeId(
            string runtimeId,
            string configId)
        {
            return !string.IsNullOrWhiteSpace(runtimeId) &&
                   runtimeId.EndsWith(
                       ":" + configId,
                       StringComparison.Ordinal);
        }

        private static bool HasActiveNonLoopingAudio()
        {
            return Object.FindObjectsOfType<AudioSource>().Any(source =>
                source != null &&
                source.gameObject.activeInHierarchy &&
                source.isPlaying &&
                !source.loop);
        }

        private static bool HasActiveNonLoopingAnimation()
        {
            foreach (var animation in
                     Object.FindObjectsOfType<Animation>())
            {
                if (animation == null ||
                    !animation.gameObject.activeInHierarchy ||
                    !animation.isPlaying ||
                    animation.clip == null)
                {
                    continue;
                }

                if (animation.clip.wrapMode != WrapMode.Loop &&
                    animation.clip.wrapMode != WrapMode.PingPong)
                {
                    return true;
                }
            }

            foreach (var animator in
                     Object.FindObjectsOfType<Animator>())
            {
                if (animator == null ||
                    !animator.enabled ||
                    !animator.gameObject.activeInHierarchy ||
                    animator.runtimeAnimatorController == null)
                {
                    continue;
                }

                for (var layer = 0;
                     layer < animator.layerCount;
                     layer++)
                {
                    var state =
                        animator.GetCurrentAnimatorStateInfo(layer);
                    if (animator.IsInTransition(layer) ||
                        (!state.loop && state.normalizedTime < 1f))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private IEnumerator CompleteAcceptance(string details)
        {
            if (!G4PerformanceCollector.ValidateFullSampleCatalog(
                    out var sampleCatalogDetails))
            {
                Fail(
                    "The complete G2 sample Sprite Catalog gate failed. " +
                    sampleCatalogDetails);
                yield break;
            }
            G4PerformanceCollector.RecordCheckpoint(
                "sample-catalog-exact",
                true,
                sampleCatalogDetails,
                string.Empty);

            yield return WaitForCondition(
                () => G4PerformanceCollector.ValidateCurrentCleanup(
                    out _),
                "presentation cleanup after final checkpoint");
            if (failed)
            {
                yield break;
            }

            yield return new WaitForSecondsRealtime(
                CheckpointSettleSeconds);
            if (!G4PerformanceCollector.ValidateNoRuntimeFailures(
                    out var runtimeLogDetails))
            {
                Fail(
                    "Runtime Error/Exception/Assert log gate failed. " +
                    runtimeLogDetails);
                yield break;
            }
            G4PerformanceCollector.RecordCheckpoint(
                "acceptance-complete",
                true,
                details + " Runtime log gate: " + runtimeLogDetails + ".",
                string.Empty);
            var reportPath = G4PerformanceCollector.Complete(
                "AcceptancePassed",
                details);
            if (string.IsNullOrWhiteSpace(reportPath))
            {
                Debug.LogError(
                    "[G4] Acceptance flow passed, but the performance " +
                    "report could not be written.");
                yield return null;
                Application.Quit(1);
                yield break;
            }
            yield return null;
            Application.Quit(0);
        }

        private IEnumerator WaitForScene<T>(string sceneName)
            where T : MonoBehaviour
        {
            yield return WaitForCondition(
                () => string.Equals(
                          SceneManager.GetActiveScene().name,
                          sceneName,
                          StringComparison.Ordinal) &&
                      Object.FindObjectOfType<T>() != null,
                $"formal scene {sceneName} with {typeof(T).Name}");
        }

        private IEnumerator WaitForCondition(
            Func<bool> predicate,
            string description,
            float timeoutSeconds = SceneTimeoutSeconds)
        {
            var deadline =
                Time.realtimeSinceStartupAsDouble + timeoutSeconds;
            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                bool ready;
                try
                {
                    ready = predicate();
                }
                catch (Exception exception)
                {
                    Fail(
                        $"Waiting for {description} threw " +
                        $"{exception.GetType().Name}: {exception.Message}");
                    yield break;
                }

                if (ready)
                {
                    yield break;
                }
                yield return null;
            }

            Fail($"Timed out waiting for {description}.");
        }

        private IEnumerator WaitForBattleFeedback(
            BattleTestController controller,
            BattleScreenView screen,
            string feedbackId,
            string description,
            Func<bool> presentationPredicate = null)
        {
            var deadline =
                Time.realtimeSinceStartupAsDouble + SceneTimeoutSeconds;
            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                if (controller == null || screen == null)
                {
                    Fail(
                        $"Battle presentation disappeared while waiting for " +
                        $"{description}.");
                    yield break;
                }

                if (screen.IsAnimationPlaying &&
                    string.Equals(
                        screen.LastFeedbackId,
                        feedbackId,
                        StringComparison.Ordinal) &&
                    (presentationPredicate == null ||
                     presentationPredicate()))
                {
                    yield break;
                }

                if (controller.LastResult != null)
                {
                    Fail(
                        $"Battle completed without the required {description} " +
                        $"feedback '{feedbackId}'.");
                    yield break;
                }

                yield return null;
            }

            Fail(
                $"Timed out waiting for {description} feedback " +
                $"'{feedbackId}'.");
        }

        private IEnumerator CaptureCheckpoint(
            string checkpoint,
            string details,
            float settleSeconds = CheckpointSettleSeconds,
            bool captureCurrentFrameSynchronously = false,
            int fileOrdinal = 0)
        {
            if (settleSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(settleSeconds);
            }

            if (!G4PerformanceCollector.ValidateCurrentSampleArtwork(
                    out var artworkDetails))
            {
                Fail(
                    $"Checkpoint '{checkpoint}' contains a G2 sample-scope " +
                    $"artwork that did not resolve Exact. {artworkDetails}");
                yield break;
            }
            details = string.IsNullOrWhiteSpace(details)
                ? artworkDetails
                : details + " " + artworkDetails;

            var screenshotPath = string.Empty;
            if (!G4RuntimeArguments.HasFlag(
                    G4RuntimeArguments.NoScreenshotsFlag))
            {
                var ordinal = fileOrdinal > 0
                    ? fileOrdinal
                    : screenshotSequence + 1;
                if (!screenshotOrdinals.Add(ordinal))
                {
                    Fail(
                        $"Screenshot ordinal {ordinal} was requested more " +
                        $"than once at checkpoint '{checkpoint}'.");
                    yield break;
                }
                screenshotSequence = Math.Max(screenshotSequence, ordinal);
                var fileName = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:00}-{1}-{2}x{3}.png",
                    ordinal,
                    G4RuntimeArguments.SanitizeFileName(
                        checkpoint,
                        "checkpoint"),
                    Screen.width,
                    Screen.height);
                screenshotPath = Path.Combine(
                    evidenceDirectory,
                    fileName);
                if (captureCurrentFrameSynchronously)
                {
                    yield return new WaitForEndOfFrame();
                    var texture =
                        ScreenCapture.CaptureScreenshotAsTexture();
                    try
                    {
                        if (texture == null)
                        {
                            Fail(
                                $"Screenshot '{checkpoint}' could not " +
                                "capture the current rendered frame.");
                            yield break;
                        }

                        File.WriteAllBytes(
                            screenshotPath,
                            texture.EncodeToPNG());
                    }
                    finally
                    {
                        if (texture != null)
                        {
                            Object.Destroy(texture);
                        }
                    }
                }
                else
                {
                    ScreenCapture.CaptureScreenshot(screenshotPath);

                    var deadline =
                        Time.realtimeSinceStartupAsDouble +
                        ScreenshotTimeoutSeconds;
                    while ((!File.Exists(screenshotPath) ||
                            new FileInfo(screenshotPath).Length <= 0L) &&
                           Time.realtimeSinceStartupAsDouble < deadline)
                    {
                        yield return null;
                    }
                }

                if (!File.Exists(screenshotPath) ||
                    new FileInfo(screenshotPath).Length <= 0L)
                {
                    Fail(
                        $"Screenshot '{checkpoint}' was not written: " +
                        screenshotPath);
                    yield break;
                }
            }

            G4PerformanceCollector.RecordCheckpoint(
                checkpoint,
                true,
                details,
                screenshotPath);
        }

        private bool ExecuteStep(string description, Func<bool> action)
        {
            try
            {
                if (action())
                {
                    return true;
                }

                Fail($"G4 acceptance step failed: {description}.");
                return false;
            }
            catch (Exception exception)
            {
                Fail(
                    $"G4 acceptance step '{description}' threw " +
                    $"{exception.GetType().Name}: {exception.Message}");
                return false;
            }
        }

        private static int FindFirstMinionOfferIndex(
            ShopTestController controller)
        {
            var offers = controller?.Session?.MinionOffers;
            if (offers == null)
            {
                return -1;
            }

            for (var index = 0; index < offers.Count; index++)
            {
                if (offers[index] != null)
                {
                    return index;
                }
            }

            return -1;
        }

        private static int FindFirstOccupiedBenchIndex(
            ShopTestController controller)
        {
            var bench = controller?.Session?.Collection?.Bench;
            if (bench == null)
            {
                return -1;
            }

            for (var index = 0; index < bench.Count; index++)
            {
                if (bench[index] != null)
                {
                    return index;
                }
            }

            return -1;
        }

        private static bool InvokeMapNode(
            RunTestController controller,
            string nodeId)
        {
            var node = controller?.FormalScreenView?.FindNode(nodeId);
            var button = node == null ? null : node.GetComponent<Button>();
            if (button == null || !button.IsInteractable())
            {
                return false;
            }

            return InvokeButton(button);
        }

        private static bool InvokeNamedButton(Component root, string name)
        {
            if (root == null)
            {
                return false;
            }

            var matches = root
                .GetComponentsInChildren<Button>(true)
                .Where(button => string.Equals(
                    button.name,
                    name,
                    StringComparison.Ordinal))
                .ToArray();
            if (matches.Length != 1 || !matches[0].IsInteractable())
            {
                return false;
            }

            return InvokeButton(matches[0]);
        }

        private static bool IsNamedButtonReady(
            Component root,
            string name)
        {
            if (root == null)
            {
                return false;
            }

            var matches = root
                .GetComponentsInChildren<Button>(true)
                .Where(button => string.Equals(
                    button.name,
                    name,
                    StringComparison.Ordinal))
                .ToArray();
            return matches.Length == 1 &&
                   TryCreateButtonPointer(matches[0], out _);
        }

        private static bool InvokeButton(Button button)
        {
            if (!TryCreateButtonPointer(button, out var pointer))
            {
                return false;
            }

            return ExecuteEvents.Execute(
                button.gameObject,
                pointer,
                ExecuteEvents.pointerClickHandler);
        }

        private static bool TryCreateButtonPointer(
            Button button,
            out PointerEventData pointer)
        {
            pointer = null;
            if (button == null ||
                !button.gameObject.activeInHierarchy ||
                !button.IsActive() ||
                !button.IsInteractable())
            {
                return false;
            }

            var rect = button.transform as RectTransform;
            var eventSystem = EventSystem.current;
            if (rect == null || eventSystem == null)
            {
                return false;
            }

            Canvas.ForceUpdateCanvases();
            var canvas = button.GetComponentInParent<Canvas>();
            var eventCamera = canvas != null &&
                              canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            var screenPoint = RectTransformUtility.WorldToScreenPoint(
                eventCamera,
                rect.TransformPoint(rect.rect.center));
            if (!IsFinite(screenPoint.x) ||
                !IsFinite(screenPoint.y) ||
                screenPoint.x < 0f ||
                screenPoint.y < 0f ||
                screenPoint.x >= Screen.width ||
                screenPoint.y >= Screen.height)
            {
                return false;
            }

            pointer = CreatePointerEvent(screenPoint);
            var raycastResults = new List<RaycastResult>();
            eventSystem.RaycastAll(pointer, raycastResults);
            var topmost = raycastResults.FirstOrDefault().gameObject;
            if (topmost == null ||
                (topmost.transform != button.transform &&
                 !topmost.transform.IsChildOf(button.transform)))
            {
                return false;
            }

            return true;
        }

        private static bool InvokePointerClick(Component root)
        {
            if (root == null || !root.gameObject.activeInHierarchy)
            {
                return false;
            }

            var rect = root.transform as RectTransform;
            var eventSystem = EventSystem.current;
            if (rect == null || eventSystem == null)
            {
                return false;
            }

            Canvas.ForceUpdateCanvases();
            var canvas = root.GetComponentInParent<Canvas>();
            var eventCamera = canvas != null &&
                              canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            var screenPoint = RectTransformUtility.WorldToScreenPoint(
                eventCamera,
                rect.TransformPoint(rect.rect.center));
            if (!IsFinite(screenPoint.x) ||
                !IsFinite(screenPoint.y) ||
                screenPoint.x < 0f ||
                screenPoint.y < 0f ||
                screenPoint.x >= Screen.width ||
                screenPoint.y >= Screen.height)
            {
                return false;
            }

            var pointer = CreatePointerEvent(screenPoint);
            var raycastResults = new List<RaycastResult>();
            eventSystem.RaycastAll(pointer, raycastResults);
            var topmost = raycastResults.FirstOrDefault().gameObject;
            if (topmost == null ||
                (topmost.transform != root.transform &&
                 !topmost.transform.IsChildOf(root.transform)))
            {
                return false;
            }

            var handler = ExecuteEvents.ExecuteHierarchy(
                topmost,
                pointer,
                ExecuteEvents.pointerClickHandler);
            return handler != null &&
                   (handler.transform == root.transform ||
                    handler.transform.IsChildOf(root.transform));
        }

        private static ShopCardView FindShopCard(
            ShopCardZone zone,
            int index)
        {
            return Object.FindObjectsOfType<ShopCardView>()
                .SingleOrDefault(value =>
                    value.Zone == zone && value.Index == index);
        }

        private static ShopSlotView FindShopSlot(
            ShopCardZone zone,
            int index)
        {
            return Object.FindObjectsOfType<ShopSlotView>()
                .SingleOrDefault(value =>
                    value.Zone == zone && value.Index == index);
        }

        private static PointerEventData CreatePointerEvent(
            Vector2? position = null)
        {
            return new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = position ?? Vector2.zero
            };
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void Fail(string message)
        {
            if (failed)
            {
                return;
            }

            failed = true;
            Debug.LogError("[G4] " + message);
            G4PerformanceCollector.RecordCheckpoint(
                "acceptance-failed",
                false,
                message,
                string.Empty);
            G4PerformanceCollector.Complete(
                "AcceptanceFailed",
                message);
            StartCoroutine(QuitAfterReport());
        }

        private static IEnumerator QuitAfterReport()
        {
            yield return null;
            Application.Quit(1);
        }
    }
}

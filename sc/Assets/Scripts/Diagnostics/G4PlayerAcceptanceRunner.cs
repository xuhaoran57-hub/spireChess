using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SpireChess.App;
using SpireChess.Run;
using SpireChess.Save;
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
        private const float SceneTimeoutSeconds = 20f;
        private const float ScreenshotTimeoutSeconds = 10f;
        private const float CheckpointSettleSeconds = 0.65f;

        private static G4PlayerAcceptanceRunner instance;
        private string evidenceDirectory;
        private int screenshotSequence;
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

            yield return WaitForScene<MainMenuController>(
                GameSceneNames.MainMenu);
            if (failed)
            {
                yield break;
            }

            GameApp.Instance.AbandonRun();
            yield return CaptureCheckpoint(
                "main-menu",
                "Fresh isolated save root; no continue slot.");
            if (failed)
            {
                yield break;
            }

            var seed = G4RuntimeArguments.ReadInt(
                G4RuntimeArguments.AcceptanceSeedArgument,
                940101,
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
            G4PerformanceCollector.RecordCheckpoint(
                "acceptance-complete",
                true,
                "Formal MainMenu -> Run -> Shop -> Run -> Battle -> Run -> " +
                "MainMenu -> Continue chain completed.",
                string.Empty);
            var reportPath = G4PerformanceCollector.Complete(
                "AcceptancePassed",
                "All formal Player checkpoints passed with isolated persistence.");
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
            string description)
        {
            var deadline =
                Time.realtimeSinceStartupAsDouble + SceneTimeoutSeconds;
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
            string description)
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
                        StringComparison.Ordinal))
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
            bool captureCurrentFrameSynchronously = false)
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
                screenshotSequence++;
                var fileName = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:00}-{1}-{2}x{3}.png",
                    screenshotSequence,
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

        private static bool InvokeButton(Button button)
        {
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

            var pointer = CreatePointerEvent(screenPoint);
            var raycastResults = new List<RaycastResult>();
            eventSystem.RaycastAll(pointer, raycastResults);
            var topmost = raycastResults.FirstOrDefault().gameObject;
            if (topmost == null ||
                (topmost.transform != button.transform &&
                 !topmost.transform.IsChildOf(button.transform)))
            {
                return false;
            }

            return ExecuteEvents.Execute(
                button.gameObject,
                pointer,
                ExecuteEvents.pointerClickHandler);
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

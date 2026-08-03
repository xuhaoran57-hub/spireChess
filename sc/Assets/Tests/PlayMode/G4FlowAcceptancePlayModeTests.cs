using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SpireChess.App;
using SpireChess.Audio;
using SpireChess.Diagnostics;
using SpireChess.Run;
using SpireChess.Save;
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
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SpireChess.Tests
{
    public sealed class G4FlowAcceptancePlayModeTests
    {
        private string saveRoot;
        private RunSaveRepository originalRepository;
        private RunPersistenceCoordinator originalPersistence;
        private PlayerPrefSnapshot masterPreference;
        private PlayerPrefSnapshot musicPreference;
        private PlayerPrefSnapshot sfxPreference;
        private PlayerPrefSnapshot uiPreference;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            masterPreference = CapturePreference(
                PresentationAudioSettings.MasterPrefKey);
            musicPreference = CapturePreference(
                PresentationAudioSettings.MusicPrefKey);
            sfxPreference = CapturePreference(
                PresentationAudioSettings.SfxPrefKey);
            uiPreference = CapturePreference(
                PresentationAudioSettings.UiPrefKey);

            yield return EnsureGameApp();

            var app = GameApp.Instance;
            app.ClearInMemoryRunForAutomatedTests();
            originalRepository = app.RunSaves;
            originalPersistence = app.Persistence;

            saveRoot = Path.Combine(
                Path.GetTempPath(),
                "spire-chess-g4-flow-tests",
                Guid.NewGuid().ToString("N"));
            var repository = new RunSaveRepository(
                app.Configs,
                new AtomicFileSaveStorage(saveRoot));
            SetGameAppProperty(nameof(GameApp.RunSaves), repository);
            SetGameAppProperty(
                nameof(GameApp.Persistence),
                new RunPersistenceCoordinator(repository, true));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            RestorePreference(
                PresentationAudioSettings.MasterPrefKey,
                masterPreference);
            RestorePreference(
                PresentationAudioSettings.MusicPrefKey,
                musicPreference);
            RestorePreference(
                PresentationAudioSettings.SfxPrefKey,
                sfxPreference);
            RestorePreference(
                PresentationAudioSettings.UiPrefKey,
                uiPreference);
            PlayerPrefs.Save();
            AudioService.Instance?.ReloadSettings();

            if (GameApp.Instance != null)
            {
                GameApp.Instance.ClearInMemoryRunForAutomatedTests();
                SetGameAppProperty(nameof(GameApp.RunSaves), originalRepository);
                SetGameAppProperty(nameof(GameApp.Persistence), originalPersistence);
            }

            if (!string.IsNullOrWhiteSpace(saveRoot) &&
                Directory.Exists(saveRoot))
            {
                Directory.Delete(saveRoot, true);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator FormalFlow_SaveReturnAndContinue_RestoresShopAndKeepsRuntimeSingletons()
        {
            SceneManager.LoadScene(GameSceneNames.MainMenu);
            yield return null;
            yield return null;

            AssertFormalScene<MainMenuController, MainMenuScreenView>(
                GameSceneNames.MainMenu);
            var initialMenu = Object.FindObjectOfType<MainMenuScreenView>();
            var initialController = Object.FindObjectOfType<MainMenuController>();
            if (initialController.CurrentPage == JournalMenuPage.Cover)
            {
                FindButton(initialMenu, "CoverSkipButton").onClick.Invoke();
                CompleteJournalTurn(initialMenu);
            }
            Assert.That(
                initialController.CurrentPage,
                Is.EqualTo(JournalMenuPage.Contents));
            Assert.That(initialMenu.ContinueInteractable, Is.False);
            var audio = AudioService.Instance;
            SaveAudioSettings(
                audio,
                master: 0.83f,
                music: 0.61f,
                sfx: 0.47f,
                ui: 0.29f);
            AssertAudioSettings(
                audio.Settings,
                master: 0.83f,
                music: 0.61f,
                sfx: 0.47f,
                ui: 0.29f);
            AssertSavedAudioPreferences(
                master: 0.83f,
                music: 0.61f,
                sfx: 0.47f,
                ui: 0.29f);

            FindButton(initialMenu, "NewGameButton").onClick.Invoke();
            Assert.That(initialController.IsPageInputLocked, Is.True);
            FindButton(initialMenu, "NewGameButton").onClick.Invoke();
            CompleteJournalTurn(initialMenu);
            yield return null;
            Assert.That(initialMenu.HeroSelectionVisible, Is.True);
            Assert.That(GameApp.Instance.Run, Is.Null);
            FindButton(initialMenu, "ConfirmHeroButton").onClick.Invoke();
            var originalRun = GameApp.Instance.Run;
            Assert.That(originalRun, Is.Not.Null);
            Assert.That(originalRun.State.HeroId, Is.EqualTo(HeroIds.Warrior));
            var createdRevision = GameApp.Instance.Persistence.CurrentRevision;
            FindButton(initialMenu, "ConfirmHeroButton").onClick.Invoke();
            Assert.That(GameApp.Instance.Run, Is.SameAs(originalRun));
            Assert.That(
                GameApp.Instance.Persistence.CurrentRevision,
                Is.EqualTo(createdRevision),
                "Repeated confirmation must not create a second run or save.");
            CompleteJournalTurn(initialMenu);
            yield return null;

            AssertFormalScene<RunTestController, RunScreenView>(
                GameSceneNames.Run);
            var runController = Object.FindObjectOfType<RunTestController>();
            var shopNode = runController.FormalScreenView.FindNode(
                "f1_shop_start");
            Assert.That(shopNode, Is.Not.Null);
            shopNode.GetComponent<Button>().onClick.Invoke();
            yield return null;

            AssertFormalScene<ShopTestController, ShopScreenView>(
                GameSceneNames.Shop);
            var mapper = new RunSnapshotMapper(GameApp.Instance.Configs);
            var expectedFingerprint = RunStateFingerprint.Compute(
                mapper.Capture(originalRun));
            var expectedAttemptId =
                originalRun.State.CurrentAttempt.NodeAttemptId;
            var expectedRevision =
                GameApp.Instance.Persistence.CurrentRevision;
            AssertRunSaveExcludesAudioPreferences(
                GameApp.Instance.RunSaves.Storage.ReadMain());

            SetRuntimeAudioSettings(
                audio,
                master: 0.12f,
                music: 0.23f,
                sfx: 0.34f,
                ui: 0.45f);
            Assert.That(
                RunStateFingerprint.Compute(mapper.Capture(originalRun)),
                Is.EqualTo(expectedFingerprint),
                "Runtime audio settings must not mutate single-run state.");
            audio.ReloadSettings();
            AssertAudioSettings(
                audio.Settings,
                master: 0.83f,
                music: 0.61f,
                sfx: 0.47f,
                ui: 0.29f);

            var systemMenu = Object.FindObjectOfType<RunSystemMenuView>();
            Assert.That(systemMenu, Is.Not.Null);
            FindButton(systemMenu, "MenuButton").onClick.Invoke();
            Assert.That(systemMenu.IsOpen, Is.True);
            FindButton(systemMenu, "SaveReturnButton").onClick.Invoke();
            yield return null;
            yield return null;

            Assert.That(GameApp.Instance.Run, Is.Null);
            AssertFormalScene<MainMenuController, MainMenuScreenView>(
                GameSceneNames.MainMenu);
            var resumedMenu = Object.FindObjectOfType<MainMenuScreenView>();
            Assert.That(resumedMenu.ContinueInteractable, Is.True);
            Assert.That(
                Object.FindObjectOfType<MainMenuController>()
                    .Inspection.Document.Revision,
                Is.EqualTo(expectedRevision));

            FindButton(resumedMenu, "ContinueButton").onClick.Invoke();
            yield return null;
            yield return null;

            AssertFormalScene<ShopTestController, ShopScreenView>(
                GameSceneNames.Shop);
            Assert.That(GameApp.Instance.Run, Is.Not.Null);
            Assert.That(GameApp.Instance.Run, Is.Not.SameAs(originalRun));
            Assert.That(GameApp.Instance.Run.State.Phase, Is.EqualTo(RunPhase.Shop));
            Assert.That(
                GameApp.Instance.Run.State.CurrentAttempt.NodeAttemptId,
                Is.EqualTo(expectedAttemptId));
            Assert.That(
                RunStateFingerprint.Compute(
                    mapper.Capture(GameApp.Instance.Run)),
                Is.EqualTo(expectedFingerprint));
            AssertRunSaveExcludesAudioPreferences(
                GameApp.Instance.RunSaves.Storage.ReadMain());

            SetRuntimeAudioSettings(
                audio,
                master: 0.18f,
                music: 0.27f,
                sfx: 0.36f,
                ui: 0.45f);
            audio.ReloadSettings();
            AssertAudioSettings(
                audio.Settings,
                master: 0.83f,
                music: 0.61f,
                sfx: 0.47f,
                ui: 0.29f);
            AssertSavedAudioPreferences(
                master: 0.83f,
                music: 0.61f,
                sfx: 0.47f,
                ui: 0.29f);

            var restoredShop = Object.FindObjectOfType<ShopTestController>();
            Assert.That(restoredShop.EndShopAndEnterBattle().Success, Is.True);
            yield return null;

            AssertFormalScene<RunTestController, RunScreenView>(
                GameSceneNames.Run);
            runController = Object.FindObjectOfType<RunTestController>();
            Assert.That(
                runController.EnterNode("f1_opening_normal").Success,
                Is.True);
            yield return null;

            AssertFormalScene<BattleTestController, BattleScreenView>(
                GameSceneNames.Battle);
            Assert.That(GameApp.Instance.Run.State.Phase, Is.EqualTo(RunPhase.Battle));
            Assert.That(GameApp.Instance.Run.PendingBattle, Is.Not.Null);

            var battleController =
                Object.FindObjectOfType<BattleTestController>();
            var result = battleController.ResolveImmediately();
            Assert.That(result, Is.Not.Null);
            Assert.That(
                GameApp.Instance.Run.State.Phase,
                Is.EqualTo(RunPhase.BattleResult));
            Assert.That(
                GameApp.Instance.Run.LastBattleResult,
                Is.SameAs(result));
            battleController.ReturnToFlow();
            yield return null;

            AssertFormalScene<RunTestController, RunScreenView>(
                GameSceneNames.Run);
            Assert.That(
                GameApp.Instance.Run.State.Phase,
                Is.EqualTo(RunPhase.BattleResult));
            Assert.That(
                GameApp.Instance.Run.LastBattleResult,
                Is.SameAs(result));
        }

        [UnityTest]
        public IEnumerator OneTimesTwoTimesAndSkip_MatchDomainResultAndSettleOnce()
        {
            const int seed = 940101;
            var mapper = new RunSnapshotMapper(GameApp.Instance.Configs);

            var normalRun = PrepareOpeningBattle(seed);
            var normalSettlements = SettlementCount(normalRun);
            SceneManager.LoadScene(GameSceneNames.Battle);
            yield return null;

            var normalController =
                Object.FindObjectOfType<BattleTestController>();
            var normalScreen = Object.FindObjectOfType<BattleScreenView>();
            Assert.That(normalController.PlaybackSpeed, Is.EqualTo(1f));
            normalController.StartBattle();
            yield return WaitForBattleResult(normalController, normalScreen);
            Assert.That(normalController.LastResult, Is.Not.Null);
            Assert.That(normalScreen.IsAnimationPlaying, Is.False);
            Assert.That(normalScreen.ActiveFeedbackFxCount, Is.Zero);
            Assert.That(
                SettlementCount(normalRun),
                Is.EqualTo(normalSettlements + 1));
            var normalBattleHash =
                BattleDeterminismHasher.Compute(normalController.LastResult);
            var normalRunFingerprint =
                ComparableRunFingerprint(mapper, normalRun);

            var acceleratedRun = PrepareOpeningBattle(seed);
            var acceleratedSettlements = SettlementCount(acceleratedRun);
            SceneManager.LoadScene(GameSceneNames.Battle);
            yield return null;

            var acceleratedController =
                Object.FindObjectOfType<BattleTestController>();
            var acceleratedScreen =
                Object.FindObjectOfType<BattleScreenView>();
            acceleratedController.TogglePlaybackSpeed();
            Assert.That(acceleratedController.PlaybackSpeed, Is.EqualTo(2f));
            acceleratedController.StartBattle();
            yield return WaitForBattleResult(
                acceleratedController,
                acceleratedScreen);

            Assert.That(acceleratedController.LastResult, Is.Not.Null);
            Assert.That(acceleratedScreen.IsAnimationPlaying, Is.False);
            Assert.That(acceleratedScreen.ActiveFeedbackFxCount, Is.Zero);
            Assert.That(
                SettlementCount(acceleratedRun),
                Is.EqualTo(acceleratedSettlements + 1));
            Assert.That(
                BattleDeterminismHasher.Compute(
                    acceleratedController.LastResult),
                Is.EqualTo(normalBattleHash));
            Assert.That(
                ComparableRunFingerprint(mapper, acceleratedRun),
                Is.EqualTo(normalRunFingerprint));

            var skippedRun = PrepareOpeningBattle(seed);
            var skippedSettlements = SettlementCount(skippedRun);
            SceneManager.LoadScene(GameSceneNames.Battle);
            yield return null;

            var skippedController =
                Object.FindObjectOfType<BattleTestController>();
            var skippedScreen =
                Object.FindObjectOfType<BattleScreenView>();
            skippedController.StartBattle();
            yield return null;
            skippedController.SkipPlayback();
            yield return WaitForBattleResult(skippedController, skippedScreen);

            Assert.That(skippedController.LastResult, Is.Not.Null);
            Assert.That(skippedScreen.IsAnimationPlaying, Is.False);
            Assert.That(skippedScreen.ActiveFeedbackFxCount, Is.Zero);
            Assert.That(
                SettlementCount(skippedRun),
                Is.EqualTo(skippedSettlements + 1));
            Assert.That(
                BattleDeterminismHasher.Compute(skippedController.LastResult),
                Is.EqualTo(normalBattleHash));
            Assert.That(
                ComparableRunFingerprint(mapper, skippedRun),
                Is.EqualTo(normalRunFingerprint));

            var settledOnce = SettlementCount(skippedRun);
            skippedController.SkipPlayback();
            skippedController.StartBattle();
            skippedController.ResolveImmediately();
            yield return null;
            Assert.That(
                SettlementCount(skippedRun),
                Is.EqualTo(settledOnce));
            Assert.That(
                skippedRun.LastBattleResult,
                Is.SameAs(skippedController.LastResult));
        }

        [UnityTest]
        public IEnumerator VisibleBoundViews_EmptyLastArtId_IsMissingAndFailsArtworkGate()
        {
            GameApp.Instance.StartNewRun(940203);
            var run = GameApp.Instance.Run;
            Assert.That(run, Is.Not.Null);
            Assert.That(run.EnterNode("f1_shop_start").Success, Is.True);
            SceneManager.LoadScene(GameSceneNames.Shop);
            yield return null;
            yield return null;

            AssertFormalScene<ShopTestController, ShopScreenView>(
                GameSceneNames.Shop);
            var card = Object.FindObjectsOfType<CardView>()
                .FirstOrDefault(value =>
                    value.gameObject.activeInHierarchy &&
                    value.HasCompleteBindings &&
                    !string.IsNullOrWhiteSpace(value.LastArtId));
            Assert.That(card, Is.Not.Null);
            Assert.That(
                G4PerformanceCollector.ValidateCurrentSampleArtwork(
                    out var shopBaseline),
                Is.True,
                shopBaseline);

            var cardArtId = card.LastArtId;
            SetLastArtId(card, string.Empty);
            Assert.That(
                G4PerformanceCollector.ValidateCurrentSampleArtwork(
                    out var cardDetails),
                Is.False,
                cardDetails);
            Assert.That(
                cardDetails,
                Does.Contain("<missing-art-id>:Missing"));
            Assert.That(cardDetails, Does.Contain("missing=1"));
            SetLastArtId(card, cardArtId);

            Assert.That(
                run.EndShopAndPrepareBattle(GameSceneNames.Run).Success,
                Is.True);
            Assert.That(run.EnterNode("f1_opening_normal").Success, Is.True);
            SceneManager.LoadScene(GameSceneNames.Battle);
            yield return null;
            yield return null;

            AssertFormalScene<BattleTestController, BattleScreenView>(
                GameSceneNames.Battle);
            var standee = Object.FindObjectsOfType<BattleStandeeView>()
                .FirstOrDefault(value =>
                    value.gameObject.activeInHierarchy &&
                    value.HasCompleteBindings &&
                    value.Model != null &&
                    !string.IsNullOrWhiteSpace(value.Model.ArtId));
            Assert.That(standee, Is.Not.Null);
            Assert.That(
                G4PerformanceCollector.ValidateCurrentSampleArtwork(
                    out var battleBaseline),
                Is.True,
                battleBaseline);

            var standeeArtId = standee.LastArtId;
            var expectedStandeeArtId = standee.Model.ArtId;
            SetLastArtId(standee, string.Empty);
            Assert.That(
                G4PerformanceCollector.ValidateCurrentSampleArtwork(
                    out var standeeDetails),
                Is.False,
                standeeDetails);
            Assert.That(
                standeeDetails,
                Does.Contain(expectedStandeeArtId + ":Missing"));
            Assert.That(standeeDetails, Does.Contain("missing=1"));
            SetLastArtId(standee, standeeArtId);
        }

        private static RunSession PrepareOpeningBattle(int seed)
        {
            GameApp.Instance.StartNewRun(seed);
            var run = GameApp.Instance.Run;
            Assert.That(run, Is.Not.Null);
            Assert.That(run.EnterNode("f1_shop_start").Success, Is.True);
            var offerIndex = Enumerable.Range(0, run.Shop.MinionOffers.Count)
                .First(index => run.Shop.MinionOffers[index] != null);
            var purchase = run.Shop.BuyMinion(offerIndex);
            Assert.That(purchase.Success, Is.True);
            var purchased = run.Shop.Collection.Bench[purchase.BenchIndex];
            Assert.That(purchased, Is.Not.Null);
            Assert.That(
                purchased.ConfigId,
                Is.EqualTo("young_deer_spirit"),
                "The equivalence fixture must exercise the legal deathrattle " +
                "summon board used by the G4 acceptance chain.");
            Assert.That(
                run.Shop.PlayMinion(purchase.BenchIndex, 0).Success,
                Is.True);
            Assert.That(
                run.Shop.Collection.Battle[0]?.ConfigId,
                Is.EqualTo(purchased.ConfigId));
            Assert.That(run.EndShopAndPrepareBattle(GameSceneNames.Run).Success,
                Is.True);
            Assert.That(run.EnterNode("f1_opening_normal").Success, Is.True);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.Battle));
            return run;
        }

        private static void SetLastArtId(object target, string value)
        {
            Assert.That(target, Is.Not.Null);
            var field = target.GetType().GetField(
                "<LastArtId>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, target.GetType().Name);
            field.SetValue(target, value);
        }

        private static int SettlementCount(RunSession run)
        {
            return run.State.Statistics.BattlesWon +
                   run.State.Statistics.BattlesNotWon;
        }

        private static IEnumerator WaitForBattleResult(
            BattleTestController controller,
            BattleScreenView screen)
        {
            var deadline = Time.realtimeSinceStartupAsDouble + 30d;
            while (controller != null &&
                   screen != null &&
                   (controller.LastResult == null ||
                    screen.IsAnimationPlaying ||
                    screen.ActiveFeedbackFxCount > 0) &&
                   Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
            }
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

        private static void AssertFormalScene<TController, TView>(
            string expectedScene)
            where TController : MonoBehaviour
            where TView : MonoBehaviour
        {
            Assert.That(
                SceneManager.GetActiveScene().name,
                Is.EqualTo(expectedScene));
            Assert.That(
                Object.FindObjectsOfType<TController>(),
                Has.Length.EqualTo(1));
            Assert.That(
                Object.FindObjectsOfType<TView>(),
                Has.Length.EqualTo(1));
            Assert.That(
                Object.FindObjectsOfType<Canvas>(),
                Has.Length.EqualTo(1));
            Assert.That(
                Object.FindObjectsOfType<EventSystem>(),
                Has.Length.EqualTo(1));
            Assert.That(
                Object.FindObjectsOfType<GameApp>(),
                Has.Length.EqualTo(1));
            Assert.That(
                Object.FindObjectsOfType<AudioService>(),
                Has.Length.EqualTo(1));
            Assert.That(
                Object.FindObjectsOfType<MusicDirector>(),
                Has.Length.EqualTo(1));
        }

        private static Button FindButton(Component root, string name)
        {
            var matches = root.GetComponentsInChildren<Button>(true)
                .Where(value => value.name == name)
                .ToArray();
            Assert.That(matches, Has.Length.EqualTo(1), name);
            return matches[0];
        }

        private static void CompleteJournalTurn(MainMenuScreenView view)
        {
            if (view != null && view.IsPageTurnRunning)
            {
                FindButton(view, "SkipPageTurnButton").onClick.Invoke();
            }
        }

        private static void SaveAudioSettings(
            AudioService service,
            float master,
            float music,
            float sfx,
            float ui)
        {
            SetRuntimeAudioSettings(service, master, music, sfx, ui);
            service.SaveSettings();
        }

        private static void SetRuntimeAudioSettings(
            AudioService service,
            float master,
            float music,
            float sfx,
            float ui)
        {
            Assert.That(service, Is.Not.Null);
            service.SetMasterVolume(master, false);
            service.SetBusVolume(PresentationAudioBus.Music, music, false);
            service.SetBusVolume(PresentationAudioBus.Sfx, sfx, false);
            service.SetBusVolume(PresentationAudioBus.Ui, ui, false);
        }

        private static void AssertAudioSettings(
            PresentationAudioSettings settings,
            float master,
            float music,
            float sfx,
            float ui)
        {
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.Master, Is.EqualTo(master).Within(0.0001f));
            Assert.That(settings.Music, Is.EqualTo(music).Within(0.0001f));
            Assert.That(settings.Sfx, Is.EqualTo(sfx).Within(0.0001f));
            Assert.That(settings.Ui, Is.EqualTo(ui).Within(0.0001f));
        }

        private static void AssertSavedAudioPreferences(
            float master,
            float music,
            float sfx,
            float ui)
        {
            AssertSavedPreference(
                PresentationAudioSettings.MasterPrefKey,
                master);
            AssertSavedPreference(
                PresentationAudioSettings.MusicPrefKey,
                music);
            AssertSavedPreference(
                PresentationAudioSettings.SfxPrefKey,
                sfx);
            AssertSavedPreference(
                PresentationAudioSettings.UiPrefKey,
                ui);
        }

        private static void AssertSavedPreference(
            string key,
            float expected)
        {
            Assert.That(PlayerPrefs.HasKey(key), Is.True, key);
            Assert.That(
                PlayerPrefs.GetFloat(key),
                Is.EqualTo(expected).Within(0.0001f),
                key);
        }

        private static void AssertRunSaveExcludesAudioPreferences(
            string saveJson)
        {
            Assert.That(saveJson, Is.Not.Null.And.Not.Empty);
            Assert.That(
                saveJson,
                Does.Not.Contain(PresentationAudioSettings.MasterPrefKey));
            Assert.That(
                saveJson,
                Does.Not.Contain(PresentationAudioSettings.MusicPrefKey));
            Assert.That(
                saveJson,
                Does.Not.Contain(PresentationAudioSettings.SfxPrefKey));
            Assert.That(
                saveJson,
                Does.Not.Contain(PresentationAudioSettings.UiPrefKey));
        }

        private static PlayerPrefSnapshot CapturePreference(string key)
        {
            return new PlayerPrefSnapshot(
                PlayerPrefs.HasKey(key),
                PlayerPrefs.GetFloat(key));
        }

        private static void RestorePreference(
            string key,
            PlayerPrefSnapshot snapshot)
        {
            if (snapshot.Exists)
            {
                PlayerPrefs.SetFloat(key, snapshot.Value);
            }
            else
            {
                PlayerPrefs.DeleteKey(key);
            }
        }

        private static void SetGameAppProperty<T>(string name, T value)
        {
            var property = typeof(GameApp).GetProperty(
                name,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, name);
            var setter = property.GetSetMethod(true);
            Assert.That(setter, Is.Not.Null, name);
            setter.Invoke(GameApp.Instance, new object[] { value });
        }

        private static IEnumerator EnsureGameApp()
        {
            if (GameApp.Instance == null)
            {
                yield return null;
            }

            Assert.That(GameApp.Instance, Is.Not.Null);
            Assert.That(GameApp.Instance.Configs, Is.Not.Null);
            Assert.That(AudioService.EnsurePresent(), Is.Not.Null);
            yield return null;
        }

        private readonly struct PlayerPrefSnapshot
        {
            public PlayerPrefSnapshot(bool exists, float value)
            {
                Exists = exists;
                Value = value;
            }

            public bool Exists { get; }
            public float Value { get; }
        }
    }
}

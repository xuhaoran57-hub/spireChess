using System;
using System.IO;
using System.Linq;
using SpireChess.Config;
using SpireChess.Diagnostics;
using SpireChess.Run;
using SpireChess.Save;
using SpireChess.UI.MainMenu;
using SpireChess.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpireChess.App
{
    public sealed class GameApp : MonoBehaviour
    {
        private const string BalanceRunSeedArgument = "-balanceRunSeed";
        private const string BalanceRunOutputArgument = "-balanceRunOutput";
        private const string StableSaveDirectoryName = "sc";
        private static GameApp instance;

        public static GameApp Instance => instance;
        public ConfigService Configs { get; private set; }
        public RunSession Run { get; private set; }
        public RunSaveRepository RunSaves { get; private set; }
        public ProfileProgressRepository ProfileSaves { get; private set; }
        public ProfileProgressService Profiles { get; private set; }
        public LegacyRunArchiveResult LegacyArchive { get; private set; }
        public RunPersistenceCoordinator Persistence { get; private set; }
        public SceneFlowRouter Router { get; private set; }
        public string SaveRootPath { get; private set; }
        public static string InitializationFailure { get; private set; } =
            string.Empty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
            {
                return;
            }

            var gameObject = new GameObject("GameApp");
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<GameApp>();
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
            InitializationFailure = string.Empty;
            try
            {
                Initialize();
            }
            catch (Exception exception)
            {
                InitializationFailure =
                    $"{exception.GetType().Name}: {exception.Message}";
                instance = null;
                enabled = false;
                Debug.LogException(exception, this);
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Initialize()
        {
            var serializer = new NewtonsoftJsonSerializer();
            Configs = new ConfigService(serializer);

            var validation = Configs.LoadFromResources();
            LogValidation(validation);
            validation.ThrowIfInvalid();
            SaveRootPath = ResolveSaveRootPath();
            var isolatedProfileMode =
                HasArgument("-runTests") ||
                ReadIntArgument(BalanceRunSeedArgument).HasValue;
            var profileRootPath = isolatedProfileMode
                ? Path.Combine(
                    Directory.GetParent(Application.dataPath)?.FullName ??
                    Application.dataPath,
                    "Temp",
                    "ProfileTests",
                    Guid.NewGuid().ToString("N"))
                : SaveRootPath;
            ProfileSaves = new ProfileProgressRepository(profileRootPath);
            Profiles = new ProfileProgressService(ProfileSaves);
            var profileLoad = Profiles.Initialize();
            if (!profileLoad.IsUsable)
            {
                throw new InvalidOperationException(
                    "Profile progress could not be loaded: " +
                    profileLoad.Status + " " + profileLoad.Diagnostic);
            }

            LegacyArchive = isolatedProfileMode
                ? new LegacyRunArchiveResult(false, false)
                : new LegacyRunArchiveService(SaveRootPath)
                    .ArchiveIfNeeded(
                        Profiles.Progress.LegacyV033ArchiveCompleted);
            if (LegacyArchive.LegacyDetected)
            {
                if (LegacyArchive.Succeeded)
                {
                    Profiles.MarkLegacyArchiveCompleted(
                        LegacyArchive.ArchiveRelativePath);
                    Debug.Log(
                        "[Save] Legacy v0.3.3 run archived at " +
                        LegacyArchive.ArchiveRelativePath + ".");
                }
                else
                {
                    Debug.LogWarning(
                        "[Save] Legacy v0.3.3 archive failed without " +
                        "blocking startup: " + LegacyArchive.Diagnostic);
                }
            }

            RunSaves = new RunSaveRepository(
                Configs,
                new AtomicFileSaveStorage(SaveRootPath));
            var persistenceEnabled = ReadIntArgument(BalanceRunSeedArgument) == null &&
                                     !HasArgument("-runTests");
            Persistence = new RunPersistenceCoordinator(RunSaves, persistenceEnabled);
            Persistence.RunSaved += OnRunSaved;
            Router = new SceneFlowRouter();

            if (ReadIntArgument(BalanceRunSeedArgument).HasValue)
            {
                StartNewRun(ReadIntArgument(BalanceRunSeedArgument));
            }

            Debug.Log(
                $"[GameApp] Ready. Loaded {Configs.Minions.Count} minions " +
                $"({Configs.Minions.Count(minion => minion.IsToken)} tokens) and " +
                $"{Configs.Spells.Count} spells. config={Configs.Identity?.ConfigHash}.");
        }

        private static string ResolveSaveRootPath()
        {
            var injected = ReadArgument(G4RuntimeArguments.SaveRootArgument);
            if (string.IsNullOrWhiteSpace(injected))
            {
                if (G4RuntimeArguments.IsPerformanceRequested)
                {
                    throw new InvalidOperationException(
                        "G4 Player validation requires an isolated absolute " +
                        $"{G4RuntimeArguments.SaveRootArgument}.");
                }

                return ResolveStableSaveRootPath(Application.persistentDataPath);
            }

            if (!G4RuntimeArguments.IsPerformanceRequested)
            {
                throw new InvalidOperationException(
                    $"{G4RuntimeArguments.SaveRootArgument} is reserved for " +
                    "isolated G4 validation runs.");
            }

            var isolated =
                G4RuntimeArguments.RequirePristineIsolatedSaveRoot();
            Debug.Log($"[G4] Isolated run-save root: {isolated}");
            return isolated;
        }

        public static string ResolveStableSaveRootPath(string productPersistentDataPath)
        {
            if (string.IsNullOrWhiteSpace(productPersistentDataPath))
            {
                throw new ArgumentException(
                    "Product persistent data path is required.",
                    nameof(productPersistentDataPath));
            }

            var productPath = Path.GetFullPath(productPersistentDataPath);
            var parent = Directory.GetParent(productPath);
            return parent == null
                ? productPath
                : Path.Combine(parent.FullName, StableSaveDirectoryName);
        }

        public void StartNewRun(int? randomSeed = null)
        {
            StartNewRun(HeroIds.Warrior, randomSeed);
        }

        public bool StartNewRun(string heroId, int? randomSeed = null)
        {
            if (!HeroIds.IsKnown(heroId) ||
                Profiles?.IsHeroUnlocked(heroId) != true)
            {
                return false;
            }

            var seed = randomSeed ?? ReadIntArgument(BalanceRunSeedArgument) ??
                Environment.TickCount;
            var candidate = new RunSession(Configs, seed, heroId);
            if (!Persistence.BeginNewRun(candidate))
            {
                candidate.ReleaseOutstandingRewards();
                return false;
            }

            Run?.ReleaseOutstandingRewards();
            Run = candidate;
            EnableBalanceRunTelemetryIfRequested(seed);
            try
            {
                Profiles.AcknowledgeLegacyArchiveNotice();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[Profile] Could not acknowledge legacy archive notice: " +
                    exception.Message);
            }

            return true;
        }

        public RunSaveLoadResult InspectRunSave()
        {
            return RunSaves.Inspect();
        }

        public RunSaveLoadResult ContinueRun()
        {
            var loaded = RunSaves.Load();
            if (!loaded.CanContinue || loaded.Session == null)
            {
                return loaded;
            }

            Run?.ReleaseOutstandingRewards();
            Run = loaded.Session;
            Persistence.AdoptLoadedRun(loaded.Document);
            TryRecordCommittedChapterBossProgress(
                loaded.Session,
                "ContinueRunRecovery");
            if (loaded.UsedBackup)
            {
                try
                {
                    RunSaves.RepairMainFromBackup();
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[Save] Backup loaded but main repair failed: " + exception.Message);
                }
            }

            Debug.Log(
                $"[Save] Run resumed. revision={loaded.Document.Revision}, " +
                $"phase={Run.State.Phase}.");
            return loaded;
        }

        public bool SaveAndReturnToMainMenu()
        {
            if (Run == null || !Persistence.RetrySave(Run, "ReturnToMainMenu"))
            {
                return false;
            }

            Run = null;
            Persistence.Reset();
            Router.GoToMainMenu();
            return true;
        }

        public void AbandonRun()
        {
            Run?.ReleaseOutstandingRewards();
            Run = null;
            RunSaves.Delete();
            Persistence.Reset();
        }

        public void ClearInMemoryRunForAutomatedTests()
        {
            if (!HasArgument("-runTests"))
            {
                throw new InvalidOperationException(
                    "In-memory run reset is only available to the Unity test runner.");
            }

            Run?.ReleaseOutstandingRewards();
            Run = null;
            Persistence.Reset();
        }

        private void EnableBalanceRunTelemetryIfRequested(int seed)
        {
            var outputDirectory = ReadArgument(BalanceRunOutputArgument);
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                return;
            }

            if (!Path.IsPathRooted(outputDirectory))
            {
                var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ??
                                  Application.dataPath;
                var repositoryRoot = Directory.GetParent(projectRoot)?.FullName ??
                                     projectRoot;
                outputDirectory = Path.Combine(repositoryRoot, outputDirectory);
            }
            outputDirectory = Path.GetFullPath(outputDirectory);
            var path = Path.Combine(outputDirectory, $"run-{seed}.ndjson");
            if (File.Exists(path))
            {
                path = Path.Combine(
                    outputDirectory,
                    $"run-{seed}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.ndjson");
            }

            Run.EnableTelemetry(new RunTelemetry(
                path,
                Configs.ContentRelease.ContentVersion,
                seed));
            Debug.Log($"[Balance] Run telemetry enabled: seed={seed}, path={path}");
        }

        private static int? ReadIntArgument(string name)
        {
            var value = ReadArgument(name);
            return int.TryParse(value, out var parsed) ? parsed : (int?)null;
        }

        private static string ReadArgument(string name)
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], name, StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[index + 1];
                }
            }
            return null;
        }

        private static bool HasArgument(string name)
        {
            return Environment.GetCommandLineArgs().Any(value =>
                string.Equals(value, name, StringComparison.OrdinalIgnoreCase));
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (instance == null || instance.Router == null)
            {
                return;
            }

            if (scene.name == "Boot" || scene.name == "SampleScene")
            {
                if (instance.Run != null && ReadIntArgument(BalanceRunSeedArgument).HasValue)
                {
                    instance.Router.GoToCurrentRunPhase(instance.Run);
                }
                else
                {
                    instance.Router.GoToMainMenu();
                }
            }
            else if (scene.name == GameSceneNames.MainMenu)
            {
                MainMenuController.EnsurePresent();
            }
        }

        private static void LogValidation(ConfigValidationResult validation)
        {
            foreach (var warning in validation.Warnings)
            {
                Debug.LogWarning("[Config] " + warning);
            }

            foreach (var error in validation.Errors)
            {
                Debug.LogError("[Config] " + error);
            }

            if (validation.IsValid)
            {
                Debug.Log("[Config] Validation passed.");
            }
        }

        private void OnRunSaved(RunSession run, string reason)
        {
            TryRecordCommittedChapterBossProgress(run, reason);
        }

        private void TryRecordCommittedChapterBossProgress(
            RunSession run,
            string reason)
        {
            var state = run?.State;
            if (Profiles?.IsReady != true ||
                state?.CurrentAttempt == null ||
                state.CurrentAttempt.NodeType != RunNodeType.Boss ||
                !state.CurrentAttempt.NodeResolved ||
                (state.Phase != RunPhase.FloorComplete &&
                 state.Phase != RunPhase.RunWon))
            {
                return;
            }

            try
            {
                if (Profiles.RecordChapterBossVictory(state.CurrentMap.Id))
                {
                    Debug.Log(
                        $"[Profile] Chapter Boss recorded. map={state.CurrentMap.Id}, " +
                        $"reason={reason}.");
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[Profile] Chapter Boss progress could not be saved; " +
                    "the committed run remains valid and a later save can retry. " +
                    exception.Message);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && Run != null && Persistence?.HasUnsavedChanges == true)
            {
                Persistence.RetrySave(Run, "ApplicationPause");
            }
        }

        private void OnApplicationQuit()
        {
            if (Run != null && Persistence?.HasUnsavedChanges == true)
            {
                Persistence.RetrySave(Run, "ApplicationQuit");
            }
        }
    }
}

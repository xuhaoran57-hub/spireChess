using System;
using System.IO;
using System.Linq;
using SpireChess.Config;
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
        private static GameApp instance;

        public static GameApp Instance => instance;
        public ConfigService Configs { get; private set; }
        public RunSession Run { get; private set; }
        public RunSaveRepository RunSaves { get; private set; }
        public RunPersistenceCoordinator Persistence { get; private set; }
        public SceneFlowRouter Router { get; private set; }

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
            Initialize();
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
            RunSaves = new RunSaveRepository(Configs);
            var persistenceEnabled = ReadIntArgument(BalanceRunSeedArgument) == null &&
                                     !HasArgument("-runTests");
            Persistence = new RunPersistenceCoordinator(RunSaves, persistenceEnabled);
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

        public void StartNewRun(int? randomSeed = null)
        {
            var seed = randomSeed ?? ReadIntArgument(BalanceRunSeedArgument) ??
                Environment.TickCount;
            var candidate = new RunSession(Configs, seed);
            if (!Persistence.BeginNewRun(candidate))
            {
                candidate.ReleaseOutstandingRewards();
                return;
            }

            Run?.ReleaseOutstandingRewards();
            Run = candidate;
            EnableBalanceRunTelemetryIfRequested(seed);
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

using System;
using System.IO;
using System.Linq;
using SpireChess.Config;
using SpireChess.Run;
using SpireChess.Save;
using SpireChess.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpireChess.App
{
    public sealed class GameApp : MonoBehaviour
    {
        private const string TestSaveKey = "save_smoke_test.json";
        private const string RunTestSceneName = "RunTest";
        private const string BalanceRunSeedArgument = "-balanceRunSeed";
        private const string BalanceRunOutputArgument = "-balanceRunOutput";
        private static GameApp instance;

        public static GameApp Instance => instance;
        public ConfigService Configs { get; private set; }
        public SaveService Saves { get; private set; }
        public RunSession Run { get; private set; }

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
            Saves = new SaveService(new FileSaveStorage(), serializer);

            var validation = Configs.LoadFromResources();
            LogValidation(validation);
            validation.ThrowIfInvalid();

            StartNewRun();

            RunSaveSmokeTest();

            Debug.Log(
                $"[GameApp] Ready. Loaded {Configs.Minions.Count} minions " +
                $"({Configs.Minions.Count(minion => minion.IsToken)} tokens) and {Configs.Spells.Count} spells.");
        }

        public void StartNewRun(int? randomSeed = null)
        {
            Run?.ReleaseOutstandingRewards();
            var seed = randomSeed ?? ReadIntArgument(BalanceRunSeedArgument) ??
                Environment.TickCount;
            Run = new RunSession(
                Configs,
                seed);
            EnableBalanceRunTelemetryIfRequested(seed);
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

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!ShouldAutoOpenRunTest(scene.name))
            {
                return;
            }

            Debug.Log($"[GameApp] Auto loading {RunTestSceneName} from {scene.name}.");
            SceneManager.LoadScene(RunTestSceneName);
        }

        private static bool ShouldAutoOpenRunTest(string sceneName)
        {
            return sceneName == "Boot" || sceneName == "SampleScene" || sceneName == "MainMenu";
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

        private void RunSaveSmokeTest()
        {
            var data = new TestSaveData
            {
                Version = "0.1",
                Gold = 3,
                TestMinionIds = Configs.Minions
                    .Where(minion => !minion.IsToken)
                    .Take(3)
                    .Select(minion => minion.Id)
                    .ToList()
            };

            Saves.Save(TestSaveKey, data);
            var loaded = Saves.Load<TestSaveData>(TestSaveKey);

            Debug.Log(
                $"[Save] Smoke test passed. gold={loaded.Gold}, " +
                $"minions={loaded.TestMinionIds.Count}, path={Application.persistentDataPath}/{TestSaveKey}");
        }
    }
}

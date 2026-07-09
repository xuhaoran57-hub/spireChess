using System.Linq;
using SpireChess.Config;
using SpireChess.Save;
using SpireChess.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpireChess.App
{
    public sealed class GameApp : MonoBehaviour
    {
        private const string TestSaveKey = "save_slot_0.json";
        private const string BattleTestSceneName = "BattleTest";
        private static GameApp instance;

        public static GameApp Instance => instance;
        public ConfigService Configs { get; private set; }
        public SaveService Saves { get; private set; }

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

            RunSaveSmokeTest();

            Debug.Log(
                $"[GameApp] Ready. Loaded {Configs.Minions.Count} minions " +
                $"({Configs.Minions.Count(minion => minion.IsToken)} tokens) and {Configs.Spells.Count} spells.");
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!ShouldAutoOpenBattleTest(scene.name))
            {
                return;
            }

            Debug.Log($"[GameApp] Auto loading {BattleTestSceneName} from {scene.name}.");
            SceneManager.LoadScene(BattleTestSceneName);
        }

        private static bool ShouldAutoOpenBattleTest(string sceneName)
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

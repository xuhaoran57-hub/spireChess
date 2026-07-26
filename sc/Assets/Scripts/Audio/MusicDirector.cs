using System;
using SpireChess.App;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpireChess.Audio
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-900)]
    public sealed class MusicDirector : MonoBehaviour
    {
        private static MusicDirector instance;

        [SerializeField, Min(0f)] private float sceneMusicFadeSeconds = 0.4f;

        public static MusicDirector Instance => instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            if (instance != null && instance != this)
            {
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            RefreshForActiveScene();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public void RefreshForActiveScene()
        {
            ApplyScene(SceneManager.GetActiveScene().name);
        }

        public static bool TryGetCueForScene(
            string sceneName,
            out string cueId)
        {
            if (string.Equals(
                    sceneName,
                    GameSceneNames.MainMenu,
                    StringComparison.Ordinal) ||
                string.Equals(
                    sceneName,
                    "MainMenuUiPreview",
                    StringComparison.Ordinal))
            {
                cueId = PresentationAudioCueIds.BgmMainMenu;
                return true;
            }

            if (string.Equals(
                    sceneName,
                    GameSceneNames.Run,
                    StringComparison.Ordinal) ||
                string.Equals(
                    sceneName,
                    GameSceneNames.Shop,
                    StringComparison.Ordinal) ||
                string.Equals(
                    sceneName,
                    "RunUiPreview",
                    StringComparison.Ordinal) ||
                string.Equals(
                    sceneName,
                    "ShopUiPreview",
                    StringComparison.Ordinal))
            {
                cueId = PresentationAudioCueIds.BgmRunShop;
                return true;
            }

            if (string.Equals(
                    sceneName,
                    GameSceneNames.Battle,
                    StringComparison.Ordinal) ||
                string.Equals(
                    sceneName,
                    "BattleUiPreview",
                    StringComparison.Ordinal))
            {
                cueId = PresentationAudioCueIds.BgmBattleNormal;
                return true;
            }

            cueId = null;
            return false;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyScene(scene.name);
        }

        private void ApplyScene(string sceneName)
        {
            if (!TryGetCueForScene(sceneName, out var cueId))
            {
                return;
            }

            AudioService.Instance?.PlayMusic(
                cueId,
                sceneMusicFadeSeconds);
        }
    }
}

using System;
using SpireChess.Run;
using UnityEngine.SceneManagement;

namespace SpireChess.App
{
    public sealed class SceneFlowRouter
    {
        private readonly Func<string> currentSceneName;
        private readonly Action<string> loadScene;

        public SceneFlowRouter(
            Func<string> currentSceneName = null,
            Action<string> loadScene = null)
        {
            this.currentSceneName = currentSceneName ??
                                    (() => SceneManager.GetActiveScene().name);
            this.loadScene = loadScene ?? SceneManager.LoadScene;
        }

        public GameSceneId Resolve(RunPhase phase)
        {
            switch (phase)
            {
                case RunPhase.Shop:
                    return GameSceneId.Shop;
                case RunPhase.Battle:
                    return GameSceneId.Battle;
                case RunPhase.EnteringNode:
                    throw new InvalidOperationException(
                        "EnteringNode cannot be routed as a durable phase.");
                default:
                    return GameSceneId.Run;
            }
        }

        public void GoToCurrentRunPhase(RunSession run)
        {
            if (run == null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            GoTo(Resolve(run.State.Phase));
        }

        public void GoToMainMenu()
        {
            GoTo(GameSceneId.MainMenu);
        }

        public void GoTo(GameSceneId scene)
        {
            var target = GameSceneNames.Get(scene);
            if (string.Equals(currentSceneName(), target, StringComparison.Ordinal))
            {
                return;
            }

            loadScene(target);
        }
    }
}

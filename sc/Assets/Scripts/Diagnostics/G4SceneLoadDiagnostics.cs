using System;

namespace SpireChess.Diagnostics
{
    public static class G4SceneLoadDiagnostics
    {
        public static event Action<string, double> SceneLoadRequested;

        public static void NotifySceneLoadRequested(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return;
            }

            SceneLoadRequested?.Invoke(
                sceneName,
                UnityEngine.Time.realtimeSinceStartupAsDouble);
        }
    }
}

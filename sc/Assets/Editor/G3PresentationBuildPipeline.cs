using UnityEditor;
using UnityEngine;

namespace SpireChess.Editor
{
    public static class G3PresentationBuildPipeline
    {
        [MenuItem("Spire Chess/UI/Rebuild and Capture G3 Presentation")]
        public static void BuildAllAndCapture()
        {
            G3AudioAssetBuilder.Build();

            MainMenuUiPrefabBuilder.Build();
            MainMenuUiPrefabBuilder.CaptureValidationScreenshots();

            ShopUiPrefabBuilder.BuildAndCapture();
            BattleUiPrefabBuilder.BuildAndCapture();
            RunUiPrefabBuilder.BuildAndCapture();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[G3] Presentation assets rebuilt and captured.");
        }

        public static void BuildFromCommandLine()
        {
            BuildAllAndCapture();
        }
    }
}

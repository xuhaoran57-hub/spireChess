using System.IO;
using SpireChess.UI.MainMenu;
using SpireChess.Save;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SpireChess.Editor
{
    public static class MainMenuUiPrefabBuilder
    {
        private const string PrefabDirectory = "Assets/Prefabs/UI/MainMenu";
        private const string ScreenPrefabPath = PrefabDirectory + "/PF_MainMenuScreen.prefab";
        private const string DialogPrefabPath = PrefabDirectory + "/PF_ConfirmDialog.prefab";
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";

        [MenuItem("SpireChess/UI/Build Main Menu")]
        public static void Build()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Prefabs/UI/MainMenu"));
            var view = MainMenuScreenView.CreateRuntime();
            var dialog = view.transform.Find("PF_ConfirmDialog");
            if (dialog == null)
            {
                throw new MissingReferenceException("PF_ConfirmDialog was not generated.");
            }

            PrefabUtility.SaveAsPrefabAsset(dialog.gameObject, DialogPrefabPath);
            PrefabUtility.SaveAsPrefabAsset(view.gameObject, ScreenPrefabPath);
            Object.DestroyImmediate(view.gameObject);
            foreach (var eventSystem in Object.FindObjectsOfType<EventSystem>())
            {
                Object.DestroyImmediate(eventSystem.gameObject);
            }

            BuildRuntimeScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UI] Main menu prefabs and scene rebuilt.");
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        private static void BuildRuntimeScene()
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            CreateRuntimeCamera();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPrefabPath);
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                throw new MissingReferenceException("Main menu prefab could not be instantiated.");
            }

            var controllerObject = new GameObject("MainMenuController");
            var controller = controllerObject.AddComponent<MainMenuController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("screenView").objectReferenceValue =
                instance.GetComponent<MainMenuScreenView>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void CreateRuntimeCamera()
        {
            var cameraObject = new GameObject("MainMenuCamera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.05f, 0.08f);
            camera.cullingMask = 0;
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = false;
            camera.transform.position = new Vector3(0f, 0f, -100f);
        }

        public static void CaptureValidationScreenshots()
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            var cameraObject = new GameObject("MainMenuPreviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.05f, 0.08f);
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            camera.transform.position = new Vector3(0f, 0f, -100f);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPrefabPath);
            var screen = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            var canvas = screen.GetComponent<Canvas>();
            var canvasRect = screen.GetComponent<RectTransform>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.sortingOrder = 1;
            canvasRect.anchorMin = new Vector2(0.5f, 0.5f);
            canvasRect.anchorMax = new Vector2(0.5f, 0.5f);
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.sizeDelta = new Vector2(1920f, 1080f);
            canvasRect.position = Vector3.zero;
            canvasRect.localScale = Vector3.one;
            screen.SetActive(true);
            var view = screen.GetComponent<MainMenuScreenView>();
            view.Render(new MainMenuScreenState
            {
                ContinueEnabled = true,
                ContinueSummary = "第 2 层 · 生命 13/20 · 回合 8 · RelicChoice",
                StatusMessage = "发现可继续的单局",
                SaveStatus = RunSaveLoadStatus.Valid
            });

            var repositoryRoot = Directory.GetParent(
                Directory.GetParent(Application.dataPath).FullName).FullName;
            var outputDirectory = Path.Combine(
                repositoryRoot,
                "ui-concepts",
                "unity-validation",
                "pf-main-menu-v0.1");
            Directory.CreateDirectory(outputDirectory);
            Capture(camera, canvasRect, 1920, 1080,
                Path.Combine(outputDirectory, "main-menu-1920x1080.png"));
            Capture(camera, canvasRect, 1920, 1200,
                Path.Combine(outputDirectory, "main-menu-1920x1200.png"));
            view.ShowConfirmation(
                "已有单局存档。开始新游戏会替换当前进度，是否继续？",
                () => { });
            Capture(camera, canvasRect, 1920, 1080,
                Path.Combine(outputDirectory, "confirm-dialog-1920x1080.png"));
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenuUiPreview.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] Main menu validation screenshots captured to " + outputDirectory);
        }

        private static void Capture(
            Camera camera,
            RectTransform canvas,
            int width,
            int height,
            string outputPath)
        {
            canvas.sizeDelta = new Vector2(width, height);
            camera.aspect = (float)width / height;
            camera.orthographicSize = height * 0.5f;
            var renderTexture = new RenderTexture(
                width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(
                width, height, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = renderTexture;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = null;
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(renderTexture);
            }
        }
    }
}

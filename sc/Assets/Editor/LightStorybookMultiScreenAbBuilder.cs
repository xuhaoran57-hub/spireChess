using System;
using SpireChess.UI;
using SpireChess.UI.MainMenu;
using SpireChess.UI.Run;
using SpireChess.UI.Shop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpireChess.Editor
{
    public static class LightStorybookMultiScreenAbBuilder
    {
        private const string PrefabRoot =
            "Assets/Prefabs/UI/Calibration/LightStorybook";
        private const string SceneRoot =
            "Assets/Scenes/Calibration/LightStorybook";

        private const string MainMenuPrefabPath =
            PrefabRoot + "/PF_MainMenuScreen_LightStorybook.prefab";
        private const string ShopPrefabPath =
            PrefabRoot + "/PF_ShopScreen_LightStorybook.prefab";
        private const string RunPrefabPath =
            PrefabRoot + "/PF_RunScreen_LightStorybook.prefab";
        private const string RunNodePrefabPath =
            PrefabRoot + "/PF_RunMapNode_LightStorybook.prefab";
        private const string RunEdgePrefabPath =
            PrefabRoot + "/PF_RunMapEdge_LightStorybook.prefab";
        private const string RunRelicPrefabPath =
            PrefabRoot + "/PF_RunRelicEntry_LightStorybook.prefab";
        private const string RunChoicePrefabPath =
            PrefabRoot + "/PF_RunChoiceOption_LightStorybook.prefab";

        [MenuItem("Spire Chess/UI/Build Light Storybook Multi-Screen A-B")]
        public static void Build()
        {
            LightStorybookAbBuilder.Build();
            EnsureFolder("Assets/Prefabs/UI/Calibration", "LightStorybook");
            EnsureFolder("Assets/Scenes/Calibration", "LightStorybook");

            var theme = AssetDatabase.LoadAssetAtPath<PresentationTheme>(
                LightStorybookAbBuilder.ThemePath);
            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    LightStorybookAbBuilder.CatalogPath);
            if (theme == null || catalog == null)
            {
                throw new InvalidOperationException(
                    "The Light Storybook theme and catalog are required.");
            }

            var mainMenu = CopyAndStylePrefab(
                "Assets/Prefabs/UI/MainMenu/PF_MainMenuScreen.prefab",
                MainMenuPrefabPath,
                theme,
                catalog);
            var shop = CopyAndStylePrefab(
                ShopUiPrefabBuilder.ScreenPrefabPath,
                ShopPrefabPath,
                theme,
                catalog);
            var run = BuildRunPrefab(theme, catalog);

            BuildMainMenuScene(mainMenu);
            BuildShopScene(shop);
            BuildRunScene(run);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[LightStorybook] Built Main Menu, Shop and Run A/B scenes.");
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        private static GameObject BuildRunPrefab(
            PresentationTheme theme,
            PresentationSpriteCatalog catalog)
        {
            var node = CopyAndStylePrefab(
                RunUiPrefabBuilder.NodePrefabPath,
                RunNodePrefabPath,
                theme,
                catalog);
            var edge = CopyAndStylePrefab(
                RunUiPrefabBuilder.EdgePrefabPath,
                RunEdgePrefabPath,
                theme,
                catalog);
            var relic = CopyAndStylePrefab(
                RunUiPrefabBuilder.RelicPrefabPath,
                RunRelicPrefabPath,
                theme,
                catalog);
            var choice = CopyAndStylePrefab(
                RunUiPrefabBuilder.ChoicePrefabPath,
                RunChoicePrefabPath,
                theme,
                catalog);

            CopyAssetReplacing(
                RunUiPrefabBuilder.ScreenPrefabPath,
                RunPrefabPath);
            var root = PrefabUtility.LoadPrefabContents(RunPrefabPath);
            ReplacePresentationReferences(root, theme, catalog);
            ApplyLightPalette(root, theme);
            var view = root.GetComponent<RunScreenView>();
            var serialized = new SerializedObject(view);
            SetReference(serialized, "mapNodePrefab", node);
            SetReference(serialized, "mapEdgePrefab", edge);
            SetReference(serialized, "relicEntryPrefab", relic);
            SetReference(serialized, "choiceOptionPrefab", choice);
            serialized.FindProperty("suppressProductionBackdrop").boolValue =
                true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, RunPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(RunPrefabPath);
        }

        private static GameObject CopyAndStylePrefab(
            string source,
            string destination,
            PresentationTheme theme,
            PresentationSpriteCatalog catalog)
        {
            CopyAssetReplacing(source, destination);
            var root = PrefabUtility.LoadPrefabContents(destination);
            ReplacePresentationReferences(root, theme, catalog);
            ApplyLightPalette(root, theme);
            PrefabUtility.SaveAsPrefabAsset(root, destination);
            PrefabUtility.UnloadPrefabContents(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(destination);
        }

        private static void ApplyLightPalette(
            GameObject root,
            PresentationTheme theme)
        {
            foreach (var backdrop in
                     root.GetComponentsInChildren<PresentationBackdropGraphic>(
                         true))
            {
                var serialized = new SerializedObject(backdrop);
                serialized.FindProperty("suppressProductionArtwork").boolValue =
                    true;
                serialized.FindProperty("topColor").colorValue =
                    new Color(0.72f, 0.82f, 0.82f, 1f);
                serialized.FindProperty("bottomColor").colorValue =
                    theme.ScreenBackground;
                serialized.FindProperty("accentColor").colorValue =
                    theme.Accent;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.color.a < 0.05f || image.sprite != null)
                {
                    continue;
                }
                var name = image.name.ToLowerInvariant();
                if (name.Contains("blocker") ||
                    name.Contains("scrim") ||
                    name.Contains("overlay"))
                {
                    image.color = theme.ModalScrim;
                }
                else if (name.Contains("background") ||
                         name.Contains("safearea"))
                {
                    image.color = theme.ScreenBackground;
                }
                else if (name.Contains("button"))
                {
                    image.color = name.Contains("delete") ||
                                  name.Contains("danger")
                        ? new Color(0.66f, 0.27f, 0.23f, 1f)
                        : theme.ButtonNormal;
                }
                else if (Luminance(image.color) < 0.42f)
                {
                    image.color = name.Contains("panel") ||
                                  name.Contains("card") ||
                                  name.Contains("dialog")
                        ? theme.PanelBackground
                        : theme.PanelRaised;
                }
            }

            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                if (Luminance(text.color) < 0.78f)
                {
                    text.color = theme.TextPrimary;
                }
            }

            foreach (var outline in
                     root.GetComponentsInChildren<Outline>(true))
            {
                outline.effectColor = theme.PanelBorder;
            }
        }

        private static void BuildMainMenuScene(GameObject prefab)
        {
            var scene = CreateSceneWithScreen(
                prefab,
                "PF_MainMenuScreen_LightStorybook");
            var controllerObject = new GameObject(
                "MainMenuController",
                typeof(MainMenuController));
            SceneManager.MoveGameObjectToScene(controllerObject, scene);
            Bind(
                controllerObject.GetComponent<MainMenuController>(),
                "screenView",
                scene.GetRootGameObjects()[0]
                    .GetComponent<MainMenuScreenView>());
            SaveScene(scene, SceneRoot + "/MainMenuLightStorybookAB.unity");
        }

        private static void BuildShopScene(GameObject prefab)
        {
            var scene = CreateSceneWithScreen(
                prefab,
                "PF_ShopScreen_LightStorybook");
            var controllerObject = new GameObject(
                "ShopTestController",
                typeof(ShopTestController));
            SceneManager.MoveGameObjectToScene(controllerObject, scene);
            Bind(
                controllerObject.GetComponent<ShopTestController>(),
                "screenView",
                scene.GetRootGameObjects()[0].GetComponent<ShopScreenView>());
            SaveScene(scene, SceneRoot + "/ShopLightStorybookAB.unity");
        }

        private static void BuildRunScene(GameObject prefab)
        {
            var scene = CreateSceneWithScreen(
                prefab,
                "PF_RunScreen_LightStorybook");
            var controllerObject = new GameObject(
                "RunTestController",
                typeof(RunTestController));
            SceneManager.MoveGameObjectToScene(controllerObject, scene);
            Bind(
                controllerObject.GetComponent<RunTestController>(),
                "screenView",
                scene.GetRootGameObjects()[0].GetComponent<RunScreenView>());
            SaveScene(scene, SceneRoot + "/RunLightStorybookAB.unity");
        }

        private static Scene CreateSceneWithScreen(
            GameObject prefab,
            string instanceName)
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            var screen = PrefabUtility.InstantiatePrefab(
                prefab,
                scene) as GameObject;
            if (screen == null)
            {
                throw new InvalidOperationException(
                    "Failed to instantiate " + instanceName);
            }
            screen.name = instanceName;
            var eventSystem = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
            SceneManager.MoveGameObjectToScene(eventSystem, scene);
            return scene;
        }

        private static void SaveScene(Scene scene, string path)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void Bind(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SetReference(serialized, propertyName, value);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ReplacePresentationReferences(
            GameObject root,
            PresentationTheme theme,
            PresentationSpriteCatalog catalog)
        {
            foreach (var component in
                     root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null)
                {
                    continue;
                }
                var serialized = new SerializedObject(component);
                var changed = false;
                var themeProperty = serialized.FindProperty("theme");
                if (themeProperty != null)
                {
                    themeProperty.objectReferenceValue = theme;
                    changed = true;
                }
                var catalogProperty = serialized.FindProperty("spriteCatalog");
                if (catalogProperty != null)
                {
                    catalogProperty.objectReferenceValue = catalog;
                    changed = true;
                }
                if (changed)
                {
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void SetReference(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    "Serialized property is missing: " + propertyName);
            }
            property.objectReferenceValue = value;
        }

        private static float Luminance(Color color)
        {
            return color.r * 0.2126f +
                   color.g * 0.7152f +
                   color.b * 0.0722f;
        }

        private static void CopyAssetReplacing(string source, string destination)
        {
            if (AssetDatabase.LoadMainAssetAtPath(destination) != null)
            {
                AssetDatabase.DeleteAsset(destination);
            }
            if (!AssetDatabase.CopyAsset(source, destination))
            {
                throw new InvalidOperationException(
                    $"Failed to copy '{source}' to '{destination}'.");
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}

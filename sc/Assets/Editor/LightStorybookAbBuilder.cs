using System;
using System.IO;
using SpireChess.UI;
using SpireChess.UI.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpireChess.Editor
{
    public static class LightStorybookAbBuilder
    {
        public const string ThemePath =
            "Assets/Configs/Presentation/PresentationTheme_LightStorybook.asset";
        public const string CatalogPath =
            "Assets/Configs/Presentation/PresentationSpriteCatalog_LightStorybook.asset";
        public const string StandeePrefabPath =
            "Assets/Prefabs/UI/Calibration/PF_BattleStandee_LightStorybook.prefab";
        public const string ScreenPrefabPath =
            "Assets/Prefabs/UI/Calibration/PF_BattleScreen_LightStorybook.prefab";
        public const string ScenePath =
            "Assets/Scenes/Calibration/BattleLightStorybookAB.unity";

        private const string ArtFolder =
            "Assets/Art/Presentation/Calibration/LightStorybook";
        private const string ForgeArtPath =
            ArtFolder + "/forge-card-new-light.png";
        private const string BattleBackdropPath =
            ArtFolder + "/battle-backdrop-new-light.png";

        [MenuItem("Spire Chess/UI/Build Light Storybook A-B Scene")]
        public static void Build()
        {
            EnsureFolder("Assets/Prefabs/UI", "Calibration");
            EnsureFolder("Assets/Scenes", "Calibration");
            EnsureFolder("Assets/Art/Presentation", "Calibration");
            EnsureFolder("Assets/Art/Presentation/Calibration", "LightStorybook");

            CopyCalibrationImage("forge-card-new-light.png", ForgeArtPath);
            CopyCalibrationImage(
                "battle-backdrop-new-light.png",
                BattleBackdropPath);

            var forgeArt = ConfigureSprite(ForgeArtPath);
            var battleBackdrop = ConfigureSprite(BattleBackdropPath);
            var theme = BuildTheme();
            var catalog = BuildCatalog(forgeArt);
            var standeePrefab = BuildStandeePrefab(theme, catalog);
            var screenPrefab = BuildScreenPrefab(
                theme,
                catalog,
                standeePrefab,
                battleBackdrop);
            BuildScene(screenPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LightStorybook] Built isolated A/B battle scene.");
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        private static PresentationTheme BuildTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<PresentationTheme>(
                ThemePath);
            if (theme == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<PresentationTheme>(
                    BattleUiPrefabBuilder.ThemePath);
                if (source == null)
                {
                    throw new InvalidOperationException(
                        "Build the production PresentationTheme first.");
                }
                theme = UnityEngine.Object.Instantiate(source);
                theme.name = "PresentationTheme_LightStorybook";
                AssetDatabase.CreateAsset(theme, ThemePath);
            }

            var serialized = new SerializedObject(theme);
            SetColor(serialized, "screenBackground",
                new Color(0.80f, 0.85f, 0.83f, 1f));
            SetColor(serialized, "panelBackground",
                new Color(0.91f, 0.84f, 0.69f, 0.96f));
            SetColor(serialized, "panelRaised",
                new Color(0.97f, 0.91f, 0.78f, 1f));
            SetColor(serialized, "panelBorder",
                new Color(0.39f, 0.27f, 0.16f, 0.78f));
            SetColor(serialized, "buttonNormal",
                new Color(0.29f, 0.43f, 0.55f, 1f));
            SetColor(serialized, "buttonHighlighted",
                new Color(0.39f, 0.57f, 0.67f, 1f));
            SetColor(serialized, "buttonPressed",
                new Color(0.22f, 0.34f, 0.44f, 1f));
            SetColor(serialized, "buttonDisabled",
                new Color(0.57f, 0.57f, 0.52f, 0.88f));
            SetColor(serialized, "textPrimary",
                new Color(0.20f, 0.14f, 0.09f, 1f));
            SetColor(serialized, "textSecondary",
                new Color(0.35f, 0.34f, 0.31f, 1f));
            SetColor(serialized, "accent",
                new Color(0.76f, 0.43f, 0.17f, 1f));
            SetColor(serialized, "modalScrim",
                new Color(0.22f, 0.17f, 0.12f, 0.48f));
            SetColor(serialized, "mapCanvasBackground",
                new Color(0.84f, 0.79f, 0.65f, 0.98f));
            SetColor(serialized, "mapDecorationTint",
                new Color(0.35f, 0.29f, 0.18f, 0.22f));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
            return theme;
        }

        private static PresentationSpriteCatalog BuildCatalog(Sprite forgeArt)
        {
            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    CatalogPath);
            if (catalog == null)
            {
                var source =
                    AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                        CardUiPrefabBuilder.SpriteCatalogPath);
                if (source == null)
                {
                    throw new InvalidOperationException(
                        "Build the production sprite catalog first.");
                }
                catalog = UnityEngine.Object.Instantiate(source);
                catalog.name = "PresentationSpriteCatalog_LightStorybook";
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var serialized = new SerializedObject(catalog);
            var artworks = serialized.FindProperty("artworks");
            var replaced = false;
            for (var index = 0; index < artworks.arraySize; index++)
            {
                var entry = artworks.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("id").stringValue !=
                    "placeholder_card_undying_furnace_king")
                {
                    continue;
                }
                entry.FindPropertyRelative("sprite").objectReferenceValue =
                    forgeArt;
                entry.FindPropertyRelative("focalPointY").floatValue = 0.28f;
                replaced = true;
                break;
            }
            if (!replaced)
            {
                throw new InvalidOperationException(
                    "The Furnace King artwork entry is missing.");
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static GameObject BuildStandeePrefab(
            PresentationTheme theme,
            PresentationSpriteCatalog catalog)
        {
            CopyAssetReplacing(
                BattleUiPrefabBuilder.StandeePrefabPath,
                StandeePrefabPath);
            var root = PrefabUtility.LoadPrefabContents(StandeePrefabPath);
            ReplacePresentationReferences(root, theme, catalog);
            PrefabUtility.SaveAsPrefabAsset(root, StandeePrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(StandeePrefabPath);
        }

        private static GameObject BuildScreenPrefab(
            PresentationTheme theme,
            PresentationSpriteCatalog catalog,
            GameObject standeePrefab,
            Sprite battleBackdrop)
        {
            CopyAssetReplacing(
                BattleUiPrefabBuilder.ScreenPrefabPath,
                ScreenPrefabPath);
            var root = PrefabUtility.LoadPrefabContents(ScreenPrefabPath);
            ReplacePresentationReferences(root, theme, catalog);
            var view = root.GetComponent<BattleScreenView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("standeePrefab").objectReferenceValue =
                standeePrefab;
            serialized.FindProperty("backdropOverride").objectReferenceValue =
                battleBackdrop;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, ScreenPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPrefabPath);
        }

        private static void BuildScene(GameObject screenPrefab)
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            var screen = PrefabUtility.InstantiatePrefab(
                screenPrefab,
                scene) as GameObject;
            if (screen == null)
            {
                throw new InvalidOperationException(
                    "Failed to instantiate Light Storybook battle screen.");
            }
            screen.name = "PF_BattleScreen_LightStorybook";

            var controllerObject = new GameObject(
                "BattleTestController",
                typeof(BattleTestController));
            SceneManager.MoveGameObjectToScene(controllerObject, scene);
            var controller = controllerObject.GetComponent<BattleTestController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("screenView").objectReferenceValue =
                screen.GetComponent<BattleScreenView>();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var eventSystem = new GameObject(
                "EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
            SceneManager.MoveGameObjectToScene(eventSystem, scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
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

        private static void CopyCalibrationImage(
            string sourceName,
            string destinationAssetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var repositoryRoot = Directory.GetParent(projectRoot).FullName;
            var source = Path.Combine(
                repositoryRoot,
                "ui-concepts",
                "phase-9c",
                "light-storybook-production-v0.1",
                "ab-production-v0.1",
                sourceName);
            if (!File.Exists(source))
            {
                throw new FileNotFoundException(
                    "Calibration source image is missing.",
                    source);
            }
            File.Copy(
                source,
                Path.Combine(projectRoot, destinationAssetPath),
                true);
            AssetDatabase.ImportAsset(
                destinationAssetPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }

        private static Sprite ConfigureSprite(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    "Unable to configure sprite at " + assetPath);
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
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

        private static void SetColor(
            SerializedObject serialized,
            string propertyName,
            Color color)
        {
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    "Theme property is missing: " + propertyName);
            }
            property.colorValue = color;
        }
    }
}

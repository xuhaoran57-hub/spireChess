using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.UI;
using SpireChess.UI.Battle;
using SpireChess.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        private const string BattleBackdropPath =
            ArtFolder + "/battle-backdrop-new-light.png";

        private static readonly string[] BattlePreviewPlayerIds =
        {
            "forge_soul_shield_squire",
            "rootbound_soul_guide",
            "glimmer_mage",
            "traveling_physician",
            "mirrorsteel_duelist"
        };

        private static readonly string[] BattlePreviewEnemyIds =
        {
            "resonance_bell_guard",
            "undying_furnace_king",
            "young_deer_spirit",
            "ancient_mountain_spirit",
            "fate_track_recorder"
        };

        private static readonly int[] BattlePreviewPlayerGoldenSlots = { 4 };
        private static readonly int[] BattlePreviewEnemyGoldenSlots = { 1 };

        [MenuItem("Spire Chess/UI/Build Light Storybook A-B Scene")]
        public static void Build()
        {
            EnsureFolder("Assets/Prefabs/UI", "Calibration");
            EnsureFolder("Assets/Scenes", "Calibration");
            EnsureFolder("Assets/Art/Presentation", "Calibration");
            EnsureFolder("Assets/Art/Presentation/Calibration", "LightStorybook");

            CopyCalibrationImage(
                "battle-backdrop-new-light.png",
                BattleBackdropPath);

            var battleBackdrop = ConfigureSprite(BattleBackdropPath);
            var theme = BuildTheme();
            var catalog = BuildCatalog();
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

        private static PresentationSpriteCatalog BuildCatalog()
        {
            var source =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    LightStorybookFormalCatalogBuilder.CatalogPath);
            if (source == null)
            {
                throw new InvalidOperationException(
                    "Build the Light Storybook formal catalog before the " +
                    "battle validation scene.");
            }

            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    CatalogPath);
            if (catalog == null)
            {
                catalog = UnityEngine.Object.Instantiate(source);
                catalog.name = "PresentationSpriteCatalog_LightStorybook";
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            else
            {
                EditorUtility.CopySerialized(source, catalog);
                catalog.name = "PresentationSpriteCatalog_LightStorybook";
            }

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
            try
            {
                ReplacePresentationReferences(root, theme, catalog);
                ConfigureShieldContrast(root, catalog);
                PrefabUtility.SaveAsPrefabAsset(root, StandeePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(StandeePrefabPath);
        }

        private static void ConfigureShieldContrast(
            GameObject root,
            PresentationSpriteCatalog catalog)
        {
            var view = root.GetComponent<BattleStandeeView>();
            if (view == null)
            {
                throw new InvalidOperationException(
                    "The Light Storybook standee has no BattleStandeeView.");
            }

            var serialized = new SerializedObject(view);
            var shield = serialized.FindProperty("shieldOverlay")
                .objectReferenceValue as Image;
            if (shield == null)
            {
                throw new InvalidOperationException(
                    "The Light Storybook standee shield overlay is missing.");
            }

            var underlayObject = new GameObject(
                "ShieldContrastUnderlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            underlayObject.transform.SetParent(root.transform, false);
            var underlayRect = underlayObject.GetComponent<RectTransform>();
            underlayRect.anchorMin = Vector2.zero;
            underlayRect.anchorMax = Vector2.zero;
            underlayRect.pivot = Vector2.zero;
            underlayRect.anchoredPosition = new Vector2(10f, 6f);
            underlayRect.sizeDelta = new Vector2(140f, 230f);

            var underlay = underlayObject.GetComponent<Image>();
            underlay.sprite = catalog.BattleStandeeShieldOverlay;
            underlay.type = Image.Type.Simple;
            underlay.preserveAspect = true;
            underlay.color = new Color(0.04f, 0.20f, 0.32f, 0.90f);
            underlay.raycastTarget = false;
            underlay.material = null;
            underlayObject.SetActive(false);
            underlay.transform.SetSiblingIndex(
                shield.transform.GetSiblingIndex());

            shield.color = new Color(0.22f, 0.68f, 1f, 0.66f);
            shield.raycastTarget = false;
            serialized.FindProperty("shieldContrastUnderlay")
                .objectReferenceValue = underlay;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
            CreateValidationCamera(scene);
            var screen = PrefabUtility.InstantiatePrefab(
                screenPrefab,
                scene) as GameObject;
            if (screen == null)
            {
                throw new InvalidOperationException(
                    "Failed to instantiate Light Storybook battle screen.");
            }
            screen.name = "PF_BattleScreen_LightStorybook";

            var view = screen.GetComponent<BattleScreenView>();
            var state = CreateBattlePreviewState();
            ValidateExactArtwork(state);
            view.Render(state);
            if (view.RenderedCardCount != 10)
            {
                throw new InvalidOperationException(
                    "The Light Storybook battle preview must render exactly " +
                    $"10 standees, rendered={view.RenderedCardCount}.");
            }

            var controllerObject = new GameObject(
                "BattleTestController",
                typeof(BattleTestController));
            SceneManager.MoveGameObjectToScene(controllerObject, scene);
            var controllerSerialized = new SerializedObject(
                controllerObject.GetComponent<BattleTestController>());
            controllerSerialized.FindProperty("screenView")
                .objectReferenceValue = view;
            controllerSerialized.FindProperty("validationPresetName")
                .stringValue = "明亮绘本正式卡池";
            SetStringArray(
                controllerSerialized,
                "validationPlayerIds",
                BattlePreviewPlayerIds);
            SetStringArray(
                controllerSerialized,
                "validationEnemyIds",
                BattlePreviewEnemyIds);
            SetIntArray(
                controllerSerialized,
                "validationPlayerGoldenSlots",
                BattlePreviewPlayerGoldenSlots);
            SetIntArray(
                controllerSerialized,
                "validationEnemyGoldenSlots",
                BattlePreviewEnemyGoldenSlots);
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

            var eventSystem = new GameObject(
                "EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
            SceneManager.MoveGameObjectToScene(eventSystem, scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void CreateValidationCamera(Scene scene)
        {
            var cameraObject = new GameObject(
                "ValidationCamera",
                typeof(Camera),
                typeof(AudioListener));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.tag = "MainCamera";

            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.80f, 0.85f, 0.83f, 1f);
            camera.cullingMask = 0;
        }

        private static BattleScreenState CreateBattlePreviewState()
        {
            var configs = new ConfigService(
                new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            validation.ThrowIfInvalid();

            var state = new BattleScreenState
            {
                Title = "战斗 · 明亮绘本正式卡池",
                Status = "5 vs 5 · 静态材质与裁切验收",
                RoundText = "v0.3.3",
                LogText = string.Join("\n", new[]
                {
                    "检查人物主体居中覆盖裁切。",
                    "检查护盾中心透明且边缘不过曝。",
                    "检查金色框、攻击、生命与关键词标记。"
                }),
                Start = Button("开始战斗"),
                Speed = Button("速度 1×"),
                Skip = Button("跳过表现"),
                Preset = Button("正式卡池"),
                Reset = Button("重置"),
                Return = Button("返回")
            };

            for (var index = 0; index < BattleBoardState.SlotCount; index++)
            {
                state.PlayerCards[index] = CreateBattleCard(
                    configs,
                    BattlePreviewPlayerIds[index],
                    BattleSide.Player,
                    index,
                    BattlePreviewPlayerGoldenSlots.Contains(index),
                    index == 0);
                state.EnemyCards[index] = CreateBattleCard(
                    configs,
                    BattlePreviewEnemyIds[index],
                    BattleSide.Enemy,
                    index,
                    BattlePreviewEnemyGoldenSlots.Contains(index),
                    index == 1);
            }

            return state;
        }

        private static CardViewModel CreateBattleCard(
            ConfigService configs,
            string id,
            BattleSide side,
            int slotIndex,
            bool golden,
            bool shield)
        {
            if (!configs.TryGetMinion(id, out var config))
            {
                throw new InvalidOperationException(
                    "The Light Storybook battle preview minion is missing: " +
                    id);
            }

            var runtime = new BattleMinionRuntime(
                config,
                golden,
                runtimeInstanceId:
                    $"light-storybook-{side}-{slotIndex}-{id}");
            var model = BattleCardViewModelFactory.FromRuntime(
                runtime,
                side,
                slotIndex);
            model.ShowCost = true;
            model.HasShield = shield;
            return model;
        }

        private static void ValidateExactArtwork(BattleScreenState state)
        {
            var catalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    CatalogPath);
            var models = state.PlayerCards
                .Concat(state.EnemyCards)
                .Where(value => value != null);
            foreach (var model in models)
            {
                if (catalog == null ||
                    string.IsNullOrWhiteSpace(model.ArtId) ||
                    !catalog.TryGetArtwork(model.ArtId, out var sprite) ||
                    sprite == null)
                {
                    throw new InvalidOperationException(
                        "The Light Storybook battle preview requires exact " +
                        "artwork for " + (model?.ArtId ?? "<null>"));
                }
            }
        }

        private static BattleButtonState Button(string label)
        {
            return new BattleButtonState
            {
                Label = label,
                IsVisible = true,
                IsInteractable = false
            };
        }

        private static void SetStringArray(
            SerializedObject serialized,
            string propertyName,
            IReadOnlyList<string> values)
        {
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    "Serialized property is missing: " + propertyName);
            }
            property.arraySize = values.Count;
            for (var index = 0; index < values.Count; index++)
            {
                property.GetArrayElementAtIndex(index).stringValue =
                    values[index];
            }
        }

        private static void SetIntArray(
            SerializedObject serialized,
            string propertyName,
            IReadOnlyList<int> values)
        {
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    "Serialized property is missing: " + propertyName);
            }
            property.arraySize = values.Count;
            for (var index = 0; index < values.Count; index++)
            {
                property.GetArrayElementAtIndex(index).intValue =
                    values[index];
            }
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

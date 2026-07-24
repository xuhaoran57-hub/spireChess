using System;
using System.IO;
using System.Linq;
using SpireChess.Config;
using SpireChess.UI;
using SpireChess.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.Editor
{
    public static class CardUiPrefabBuilder
    {
        public const string SpriteCatalogPath =
            "Assets/Configs/Presentation/PresentationSpriteCatalog.asset";
        public const string FontPath =
            "Assets/Art/Fonts/NotoSansCJKsc-Regular.otf";
        public const string PrefabPath =
            "Assets/Prefabs/UI/Common/PF_Card.prefab";
        public const string PreviewScenePath =
            "Assets/Scenes/CardUiPreview.unity";
        public const string StorybookNormalFramePath =
            "Assets/Art/Presentation/UI/Common/" +
            "card_frame_storybook_normal_v2.png";
        public const string StorybookGoldenFramePath =
            "Assets/Art/Presentation/UI/Common/" +
            "card_frame_storybook_golden_v2.png";
        public const string CardCostCoinPath =
            "Assets/Art/Presentation/UI/Card/card_cost_coin_v1.png";
        public const string CardTierBookmarkPath =
            "Assets/Art/Presentation/UI/Card/card_tier_bookmark_v1.png";
        public const string CardAttackTagPath =
            "Assets/Art/Presentation/UI/Card/card_attack_tag_v1.png";
        public const string CardHealthTagPath =
            "Assets/Art/Presentation/UI/Card/card_health_tag_v1.png";

        private const string MinionArtRoot =
            "Assets/Art/Presentation/Cards/Minions/";

        private static readonly ArtworkSpec[] ArtworkSpecs =
        {
            new ArtworkSpec(
                "placeholder_card_forge_soul_shield_squire",
                MinionArtRoot +
                "ForgeSoul/card_minion_forge_soul_shield_squire.png",
                0.31f),
            new ArtworkSpec(
                "placeholder_card_undying_furnace_king",
                MinionArtRoot +
                "ForgeSoul/card_minion_undying_furnace_king.png",
                0.18f),
            new ArtworkSpec(
                "placeholder_card_young_deer_spirit",
                MinionArtRoot +
                "WildSpirit/card_minion_young_deer_spirit.png",
                0.27f),
            new ArtworkSpec(
                "placeholder_card_ten_thousand_hoof_surge",
                MinionArtRoot +
                "WildSpirit/card_minion_ten_thousand_hoof_surge.png",
                0.27f),
            new ArtworkSpec(
                "placeholder_card_astrolabe_calibrator",
                MinionArtRoot +
                "Starbound/card_minion_astrolabe_calibrator.png",
                0.27f),
            new ArtworkSpec(
                "placeholder_card_sky_covenant_bearer",
                MinionArtRoot +
                "Starbound/card_minion_sky_covenant_bearer.png",
                0.25f),
            new ArtworkSpec(
                "placeholder_card_traveling_physician",
                MinionArtRoot +
                "Wayfarer/card_minion_traveling_physician.png",
                0.27f),
            new ArtworkSpec(
                "placeholder_card_many_arts_apprentice",
                MinionArtRoot +
                "Wayfarer/card_minion_many_arts_apprentice.png",
                0.27f)
        };

        [MenuItem("Spire Chess/UI/Rebuild PF_Card")]
        public static void Build()
        {
            EnsureFolder("Assets/Prefabs", "UI");
            EnsureFolder("Assets/Prefabs/UI", "Common");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(
                FontPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null)
            {
                throw new InvalidOperationException(
                    "Unable to load the pinned card font at " + FontPath);
            }

            AssetDatabase.ImportAsset(
                SpriteCatalogPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            var spriteCatalog =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    SpriteCatalogPath);
            if (spriteCatalog == null)
            {
                throw new InvalidOperationException(
                    "Unable to load the presentation sprite catalog at " +
                    SpriteCatalogPath);
            }
            ConfigureCardPresentation(spriteCatalog);

            var root = new GameObject(
                "PF_Card",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(CardView));
            try
            {
                var rootRect = root.GetComponent<RectTransform>();
                rootRect.pivot = new Vector2(0f, 1f);
                rootRect.sizeDelta = new Vector2(240f, 360f);
                var rootImage = root.GetComponent<Image>();
                rootImage.color = new Color(1f, 1f, 1f, 0.01f);
                rootImage.raycastTarget = true;
                var canvasGroup = root.GetComponent<CanvasGroup>();

                var background = CreateImage(
                    "Background",
                    root.transform,
                    new Color(0.82f, 0.80f, 0.75f, 1f));
                var raceSkin = CreateImage(
                    "RaceSkin",
                    root.transform,
                    new Color(0.20f, 0.34f, 0.62f, 0.44f));
                var artworkMaskImage = CreateImage(
                    "ArtworkMask",
                    root.transform,
                    Color.white);
                var artworkMask = artworkMaskImage.gameObject.AddComponent<Mask>();
                artworkMask.showMaskGraphic = false;
                var artwork = CreateImage(
                    "Artwork",
                    artworkMaskImage.transform,
                    new Color(0.34f, 0.48f, 0.80f, 0.92f));
                artwork.preserveAspect = true;

                var normalFrame = CreateImage(
                    "NormalFrame",
                    root.transform,
                    new Color(0.72f, 0.78f, 0.84f, 0.28f));
                ConfigureFrame(normalFrame);
                var goldenFrame = CreateImage(
                    "GoldenFrame",
                    root.transform,
                    new Color(1f, 0.72f, 0.08f, 0.92f));
                ConfigureFrame(goldenFrame);

                var costBadge = CreateImage(
                    "CostBadge",
                    root.transform,
                    new Color(0.16f, 0.42f, 0.66f, 0.96f));
                var costText = CreateText(
                    "Cost",
                    costBadge.transform,
                    font,
                    26,
                    TextAnchor.MiddleCenter,
                    HorizontalWrapMode.Overflow);
                var tierBadge = CreateImage(
                    "TierBadge",
                    root.transform,
                    new Color(0.12f, 0.14f, 0.18f, 0.96f));
                var tierText = CreateText(
                    "Tier",
                    tierBadge.transform,
                    font,
                    26,
                    TextAnchor.MiddleCenter,
                    HorizontalWrapMode.Overflow);
                var namePlate = CreateImage(
                    "NamePlate",
                    root.transform,
                    new Color(0.08f, 0.10f, 0.14f, 0.95f));
                var nameText = CreateText(
                    "Name",
                    namePlate.transform,
                    font,
                    22,
                    TextAnchor.MiddleCenter,
                    HorizontalWrapMode.Overflow);

                var infoPanel = CreateImage(
                    "InfoPanel",
                    root.transform,
                    new Color(0.07f, 0.08f, 0.11f, 0.94f));
                var raceText = CreateText(
                    "RaceOrSpellType",
                    infoPanel.transform,
                    font,
                    14,
                    TextAnchor.MiddleCenter,
                    HorizontalWrapMode.Overflow);
                var abilityLabelRow = CreateRect(
                    "AbilityLabelRow",
                    infoPanel.transform);
                var abilityLabels = new Text[3];
                for (var index = 0; index < abilityLabels.Length; index++)
                {
                    abilityLabels[index] = CreateText(
                        "Label" + index,
                        abilityLabelRow,
                        font,
                        12,
                        TextAnchor.MiddleCenter,
                        HorizontalWrapMode.Overflow);
                    abilityLabels[index].color =
                        new Color(0.68f, 0.88f, 1f, 1f);
                }

                var descriptionText = CreateText(
                    "Description",
                    infoPanel.transform,
                    font,
                    14,
                    TextAnchor.UpperCenter,
                    HorizontalWrapMode.Wrap,
                    VerticalWrapMode.Overflow);
                var progressRoot = CreateRect("Progress", infoPanel.transform);
                var progressFill = CreateImage(
                    "ProgressFill",
                    progressRoot,
                    new Color(0.25f, 0.68f, 0.88f, 0.45f));
                var progressText = CreateText(
                    "ProgressText",
                    progressRoot,
                    font,
                    12,
                    TextAnchor.MiddleCenter,
                    HorizontalWrapMode.Overflow);

                // NamePlate overlaps the artwork and information panel. Keep it
                // after InfoPanel in draw order while leaving state/overlay
                // layers above it.
                namePlate.transform.SetSiblingIndex(
                    infoPanel.transform.GetSiblingIndex() + 1);

                var stateBadgeRow = CreateRect("StateBadgeRow", root.transform);
                var goldenBadge = CreateText(
                    "GoldenBadge",
                    stateBadgeRow,
                    font,
                    11,
                    TextAnchor.MiddleCenter,
                    HorizontalWrapMode.Overflow);
                goldenBadge.color = new Color32(0xFF, 0xD2, 0x58, 0xFF);
                goldenBadge.fontStyle = FontStyle.Bold;
                var shieldBadge = CreateText(
                    "ShieldBadge",
                    stateBadgeRow,
                    font,
                    11,
                    TextAnchor.MiddleCenter,
                    HorizontalWrapMode.Overflow);
                shieldBadge.color = new Color32(0x68, 0xC7, 0xFF, 0xFF);
                var nextCombatShieldBadge = CreateText(
                    "NextCombatShieldBadge",
                    stateBadgeRow,
                    font,
                    11,
                    TextAnchor.MiddleCenter,
                    HorizontalWrapMode.Overflow);
                nextCombatShieldBadge.color =
                    new Color32(0x9E, 0xEB, 0xFF, 0xFF);
                var temporaryBadge = CreateText(
                    "TemporaryBadge",
                    stateBadgeRow,
                    font,
                    11,
                    TextAnchor.MiddleCenter,
                    HorizontalWrapMode.Overflow);
                temporaryBadge.color = new Color32(0xC9, 0x8B, 0xFF, 0xFF);

                var attackBadge = CreateImage(
                    "AttackBadge",
                    root.transform,
                    new Color(0.54f, 0.18f, 0.16f, 0.98f));
                var attackText = CreateText(
                    "Attack",
                    attackBadge.transform,
                    font,
                    26,
                    TextAnchor.MiddleCenter,
                    HorizontalWrapMode.Overflow);
                var healthBadge = CreateImage(
                    "HealthBadge",
                    root.transform,
                    new Color(0.18f, 0.50f, 0.26f, 0.98f));
                var healthText = CreateText(
                    "Health",
                    healthBadge.transform,
                    font,
                    26,
                    TextAnchor.MiddleCenter,
                    HorizontalWrapMode.Overflow);
                var spellFooter = CreateText(
                    "SpellFooter",
                    root.transform,
                    font,
                    12,
                    TextAnchor.MiddleCenter,
                    HorizontalWrapMode.Overflow);
                spellFooter.color = new Color(0.74f, 0.82f, 1f, 1f);

                var growthFeedbackRoot = CreateRect(
                    "GrowthFeedbackRoot",
                    root.transform);
                var growthFeedbackCanvasGroup =
                    growthFeedbackRoot.gameObject.AddComponent<CanvasGroup>();
                growthFeedbackCanvasGroup.interactable = false;
                growthFeedbackCanvasGroup.blocksRaycasts = false;
                var growthFeedbackText = CreateText(
                    "FeedbackText",
                    growthFeedbackRoot,
                    font,
                    20,
                    TextAnchor.MiddleCenter,
                    HorizontalWrapMode.Overflow);
                growthFeedbackText.fontStyle = FontStyle.Bold;
                Stretch(growthFeedbackText.rectTransform, 10f);
                var selectionFrame = CreateImage(
                    "SelectionFrame",
                    root.transform,
                    new Color(0.35f, 0.85f, 1f, 0.34f));
                ConfigureFrame(selectionFrame);
                var legalTargetFrame = CreateImage(
                    "LegalTargetFrame",
                    root.transform,
                    new Color(0.42f, 1f, 0.56f, 0.30f));
                ConfigureFrame(legalTargetFrame);
                var disabledMask = CreateImage(
                    "DisabledMask",
                    root.transform,
                    new Color(0f, 0f, 0f, 0.55f));
                var disabledIcon = CreateText(
                    "DisabledIcon",
                    disabledMask.transform,
                    font,
                    24,
                    TextAnchor.MiddleCenter);
                disabledIcon.color = new Color(1f, 0.55f, 0.55f, 1f);
                var disabledReasonText = CreateText(
                    "DisabledReason",
                    disabledMask.transform,
                    font,
                    13,
                    TextAnchor.MiddleCenter);

                infoPanel.transform.SetSiblingIndex(3);
                namePlate.transform.SetSiblingIndex(4);
                normalFrame.transform.SetSiblingIndex(5);
                goldenFrame.transform.SetSiblingIndex(6);
                costBadge.transform.SetSiblingIndex(7);
                tierBadge.transform.SetSiblingIndex(8);

                goldenFrame.gameObject.SetActive(false);
                stateBadgeRow.gameObject.SetActive(false);
                goldenBadge.gameObject.SetActive(false);
                shieldBadge.gameObject.SetActive(false);
                nextCombatShieldBadge.gameObject.SetActive(false);
                temporaryBadge.gameObject.SetActive(false);
                progressRoot.gameObject.SetActive(false);
                growthFeedbackRoot.gameObject.SetActive(false);
                selectionFrame.gameObject.SetActive(false);
                legalTargetFrame.gameObject.SetActive(false);
                disabledMask.gameObject.SetActive(false);

                var view = root.GetComponent<CardView>();
                var serialized = new SerializedObject(view);
                SetReference(serialized, "rootRect", rootRect);
                SetReference(serialized, "rootImage", rootImage);
                SetReference(serialized, "canvasGroup", canvasGroup);
                SetReference(serialized, "background", background);
                SetReference(serialized, "raceSkin", raceSkin);
                SetReference(
                    serialized,
                    "artworkMask",
                    artworkMaskImage.rectTransform);
                SetReference(serialized, "artworkMaskComponent", artworkMask);
                SetReference(serialized, "artwork", artwork);
                SetReference(serialized, "normalFrame", normalFrame);
                SetReference(serialized, "goldenFrame", goldenFrame);
                SetReference(serialized, "spriteCatalog", spriteCatalog);
                SetReference(serialized, "costBadge", costBadge);
                SetReference(serialized, "costText", costText);
                SetReference(serialized, "tierBadge", tierBadge);
                SetReference(serialized, "tierText", tierText);
                SetReference(serialized, "namePlate", namePlate);
                SetReference(serialized, "nameText", nameText);
                SetReference(serialized, "infoPanel", infoPanel);
                SetReference(serialized, "raceOrSpellTypeText", raceText);
                SetReference(serialized, "abilityLabelRow", abilityLabelRow);
                SetReferenceArray(serialized, "abilityLabelTexts", abilityLabels);
                SetReference(serialized, "descriptionText", descriptionText);
                SetReference(serialized, "progressRoot", progressRoot);
                SetReference(serialized, "progressFill", progressFill);
                SetReference(serialized, "progressText", progressText);
                SetReference(serialized, "stateBadgeRow", stateBadgeRow);
                SetReference(serialized, "goldenBadge", goldenBadge);
                SetReference(serialized, "shieldBadge", shieldBadge);
                SetReference(
                    serialized,
                    "nextCombatShieldBadge",
                    nextCombatShieldBadge);
                SetReference(serialized, "temporaryBadge", temporaryBadge);
                SetReference(serialized, "attackBadge", attackBadge);
                SetReference(serialized, "attackText", attackText);
                SetReference(serialized, "healthBadge", healthBadge);
                SetReference(serialized, "healthText", healthText);
                SetReference(serialized, "spellFooter", spellFooter);
                SetReference(
                    serialized,
                    "growthFeedbackRoot",
                    growthFeedbackRoot);
                SetReference(
                    serialized,
                    "growthFeedbackCanvasGroup",
                    growthFeedbackCanvasGroup);
                SetReference(
                    serialized,
                    "growthFeedbackText",
                    growthFeedbackText);
                SetReference(serialized, "selectionFrame", selectionFrame);
                SetReference(serialized, "legalTargetFrame", legalTargetFrame);
                SetReference(serialized, "disabledMask", disabledMask);
                SetReference(serialized, "disabledIcon", disabledIcon);
                SetReference(
                    serialized,
                    "disabledReasonText",
                    disabledReasonText);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                view.Render(new CardViewModel
                {
                    Name = "天穹契约者",
                    Description = "每完成指定次数刷新，使所有友方星契永久获得成长。",
                    RaceText = "星契",
                    AbilityLabels = new[] { "刷新成长", "永久成长" },
                    Tier = 3,
                    Attack = 4,
                    Health = 8,
                    BaseAttack = 4,
                    BaseHealth = 8,
                    Cost = 3,
                    DisplayMode = CardDisplayMode.Full,
                    IsMinion = true,
                    ShowCost = true,
                    IsInteractable = true,
                    IsAffordable = true
                });

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        "Failed to save card prefab at " + PrefabPath);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[CardUI] Rebuilt " + PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [MenuItem("Spire Chess/UI/Rebuild and Capture PF_Card")]
        public static void BuildAndCapture()
        {
            Build();
            CaptureValidationScreenshots();
        }

        public static void CaptureValidationScreenshots()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    "Build PF_Card before capturing validation screenshots.");
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null)
            {
                throw new InvalidOperationException(
                    "Pinned card font is missing at " + FontPath);
            }

            var configs = new ConfigService(new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            validation.ThrowIfInvalid();
            var sky = configs.MinionsById["sky_covenant_bearer"];
            var furnaceKing =
                configs.MinionsById["undying_furnace_king"];
            var longestSpell = configs.Spells
                .OrderByDescending(value =>
                    UiTextFormatter.CountTextElements(value.Description))
                .First();

            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            var cameraObject = new GameObject("CardUiPreviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.07f, 1f);
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            camera.transform.position = new Vector3(0f, 0f, -100f);

            var canvasObject = new GameObject(
                "CardUiPreviewCanvas",
                typeof(RectTransform),
                typeof(Canvas));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.sortingOrder = 1;
            var canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.sizeDelta = new Vector2(1920f, 1080f);
            canvasRect.position = Vector3.zero;
            canvasRect.localScale = Vector3.one;

            var title = CreateText(
                "ValidationTitle",
                canvasRect,
                font,
                30,
                TextAnchor.MiddleLeft,
                HorizontalWrapMode.Overflow);
            title.text = "PF_Card v0.1 · Full / Compact Validation";
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(0f, 1f);
            title.rectTransform.pivot = new Vector2(0f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(60f, -24f);
            title.rectTransform.sizeDelta = new Vector2(900f, 42f);

            var fullNormal = CreateMinionModel(
                furnaceKing,
                false,
                CardDisplayMode.Full);
            CreatePreviewCard(prefab, canvasRect, fullNormal, 80f, 90f);

            var fullGolden = CreateMinionModel(
                furnaceKing,
                true,
                CardDisplayMode.Full);
            CreatePreviewCard(prefab, canvasRect, fullGolden, 360f, 90f);

            var fullSpell = CreateSpellModel(longestSpell, CardDisplayMode.Full);
            CreatePreviewCard(prefab, canvasRect, fullSpell, 640f, 90f);

            var compactNormal = CreateMinionModel(
                sky,
                false,
                CardDisplayMode.Compact);
            compactNormal.ShowCost = false;
            compactNormal.ProgressText = "3/4";
            CreatePreviewCard(prefab, canvasRect, compactNormal, 1000f, 100f);

            var compactGolden = CreateMinionModel(
                sky,
                true,
                CardDisplayMode.Compact);
            compactGolden.ShowCost = false;
            compactGolden.Attack += 6;
            compactGolden.Health += 8;
            compactGolden.HasShield = true;
            compactGolden.HasNextCombatShield = true;
            CreatePreviewCard(prefab, canvasRect, compactGolden, 1180f, 100f);

            var compactSpell = CreateSpellModel(
                longestSpell,
                CardDisplayMode.Compact);
            compactSpell.ShowCost = false;
            compactSpell.IsTemporary = true;
            CreatePreviewCard(prefab, canvasRect, compactSpell, 1360f, 100f);

            var compactTarget = CreateMinionModel(
                sky,
                false,
                CardDisplayMode.Compact);
            compactTarget.ShowCost = false;
            compactTarget.IsSelected = true;
            compactTarget.IsLegalTarget = true;
            CreatePreviewCard(prefab, canvasRect, compactTarget, 1540f, 100f);

            var compactDisabled = CreateMinionModel(
                sky,
                false,
                CardDisplayMode.Compact);
            compactDisabled.ShowCost = false;
            compactDisabled.IsInteractable = false;
            compactDisabled.IsLegalTarget = true;
            compactDisabled.DisabledReason = "没有合法目标";
            CreatePreviewCard(prefab, canvasRect, compactDisabled, 1720f, 100f);

            EditorSceneManager.SaveScene(scene, PreviewScenePath);

            var repositoryRoot = Directory.GetParent(
                Directory.GetParent(Application.dataPath).FullName).FullName;
            var outputDirectory = Path.Combine(
                repositoryRoot,
                "ui-concepts",
                "unity-validation",
                "pf-card-v0.1");
            Directory.CreateDirectory(outputDirectory);
            Capture(
                camera,
                canvasRect,
                1920,
                1080,
                Path.Combine(outputDirectory, "pf-card-1920x1080.png"));
            Capture(
                camera,
                canvasRect,
                1920,
                1200,
                Path.Combine(outputDirectory, "pf-card-1920x1200.png"));
            AssetDatabase.SaveAssets();
            Debug.Log("[CardUI] Captured validation screenshots to " +
                      outputDirectory);
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        private static void ConfigureCardPresentation(
            PresentationSpriteCatalog spriteCatalog)
        {
            var serialized = new SerializedObject(spriteCatalog);
            SetReference(
                serialized,
                "normalCardFrame",
                LoadSprite(StorybookNormalFramePath));
            SetReference(
                serialized,
                "goldenCardFrame",
                LoadSprite(StorybookGoldenFramePath));
            SetReference(
                serialized,
                "cardCostCoin",
                LoadSprite(
                    CardCostCoinPath,
                    pixelsPerUnit: 400,
                    maxTextureSize: 512,
                    alphaTransparency: true));
            SetReference(
                serialized,
                "cardTierBookmark",
                LoadSprite(
                    CardTierBookmarkPath,
                    pixelsPerUnit: 400,
                    maxTextureSize: 512,
                    alphaTransparency: true));
            SetReference(
                serialized,
                "cardAttackTag",
                LoadSprite(
                    CardAttackTagPath,
                    pixelsPerUnit: 400,
                    maxTextureSize: 512,
                    alphaTransparency: true,
                    spriteBorder: new Vector4(58f, 16f, 25f, 16f)));
            SetReference(
                serialized,
                "cardHealthTag",
                LoadSprite(
                    CardHealthTagPath,
                    pixelsPerUnit: 400,
                    maxTextureSize: 512,
                    alphaTransparency: true,
                    spriteBorder: new Vector4(25f, 16f, 69f, 16f)));
            foreach (var spec in ArtworkSpecs)
            {
                AddOrReplaceArtwork(
                    serialized,
                    spec.Id,
                    LoadSprite(
                        spec.Path,
                        pixelsPerUnit: 100,
                        maxTextureSize: 2048,
                        alphaTransparency: false),
                    spec.FocalPointY);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(spriteCatalog);
        }

        private static Sprite LoadSprite(
            string path,
            int pixelsPerUnit = 100,
            int maxTextureSize = 2048,
            bool alphaTransparency = true,
            Vector4 spriteBorder = default)
        {
            AssetDatabase.ImportAsset(
                path,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    "Unable to configure presentation sprite at " + path);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = alphaTransparency;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = maxTextureSize;
            importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.spriteMeshType = SpriteMeshType.FullRect;
            importer.spriteBorder = spriteBorder;
            importer.SaveAndReimport();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    "Unable to load presentation sprite at " + path);
            }
            return sprite;
        }

        private static void AddOrReplaceArtwork(
            SerializedObject catalog,
            string id,
            Sprite sprite,
            float focalPointY)
        {
            var artworks = catalog.FindProperty("artworks");
            if (artworks == null)
            {
                throw new InvalidOperationException(
                    "PresentationSpriteCatalog.artworks is unavailable.");
            }

            SerializedProperty entry = null;
            for (var index = 0; index < artworks.arraySize; index++)
            {
                var candidate = artworks.GetArrayElementAtIndex(index);
                if (candidate.FindPropertyRelative("id").stringValue == id)
                {
                    entry = candidate;
                    break;
                }
            }
            if (entry == null)
            {
                artworks.arraySize++;
                entry = artworks.GetArrayElementAtIndex(artworks.arraySize - 1);
            }

            entry.FindPropertyRelative("id").stringValue = id;
            entry.FindPropertyRelative("sprite").objectReferenceValue = sprite;
            entry.FindPropertyRelative("focalPointY").floatValue = focalPointY;
        }

        private static void CreatePreviewCard(
            GameObject prefab,
            RectTransform canvas,
            CardViewModel model,
            float x,
            float y)
        {
            var card = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (card == null)
            {
                throw new InvalidOperationException("Failed to instantiate PF_Card.");
            }

            card.transform.SetParent(canvas, false);
            card.GetComponent<CardView>().Render(model);
            var rect = card.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.localScale = Vector3.one;
        }

        private static CardViewModel CreateMinionModel(
            MinionConfig config,
            bool isGolden,
            CardDisplayMode mode)
        {
            var attack = isGolden ? config.GoldenAttack : config.Attack;
            var health = isGolden ? config.GoldenHealth : config.Health;
            return new CardViewModel
            {
                InstanceId = "preview_" + config.Id,
                ArtId = config.ArtId,
                Name = config.Name,
                Description = config.GetPrototypeDescription(isGolden),
                RaceText = ToRaceText(config.Race),
                AbilityLabels = config.Tags
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Take(5)
                    .Select(ToPreviewAbilityLabel)
                    .ToArray(),
                Tier = config.Tier,
                Attack = attack,
                Health = health,
                BaseAttack = attack,
                BaseHealth = health,
                Cost = 3,
                DisplayMode = mode,
                IsMinion = true,
                ShowCost = mode == CardDisplayMode.Full,
                IsGolden = isGolden,
                IsInteractable = true,
                IsAffordable = true,
                HasShield = config.Keywords.Contains("Shield")
            };
        }

        private static CardViewModel CreateSpellModel(
            SpellConfig config,
            CardDisplayMode mode)
        {
            return new CardViewModel
            {
                InstanceId = "preview_" + config.Id,
                ArtId = config.ArtId,
                Name = config.Name,
                Description = config.Description,
                RaceText = "商店法术",
                AbilityLabels = config.Tags
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Take(5)
                    .Select(ToPreviewAbilityLabel)
                    .ToArray(),
                Tier = config.Tier,
                Cost = 1,
                DisplayMode = mode,
                ShowCost = mode == CardDisplayMode.Full,
                IsInteractable = true,
                IsAffordable = true
            };
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
                width,
                height,
                24,
                RenderTextureFormat.ARGB32);
            var texture = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false);
            try
            {
                camera.targetTexture = renderTexture;
                PrepareTextForCapture(canvas);
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
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static void PrepareTextForCapture(RectTransform canvas)
        {
            var texts = canvas.GetComponentsInChildren<Text>(true)
                .Where(value => value.gameObject.activeInHierarchy &&
                                value.font != null)
                .ToArray();
            foreach (var group in texts.GroupBy(value => new
                     {
                         value.font,
                         value.fontSize,
                         value.fontStyle
                     }))
            {
                group.Key.font.RequestCharactersInTexture(
                    string.Concat(group.Select(value => value.text)),
                    group.Key.fontSize,
                    group.Key.fontStyle);
            }

            foreach (var text in texts)
            {
                text.SetAllDirty();
            }
        }

        private static string ToRaceText(string race)
        {
            switch (race)
            {
                case "ForgeSoul": return "铸魂";
                case "WildSpirit": return "荒灵";
                case "Starbound": return "星契";
                case "Wayfarer": return "旅团";
                default: return "无种族";
            }
        }

        private static string ToPreviewAbilityLabel(string value)
        {
            switch (value)
            {
                case "refresh": return "刷新";
                case "refresh_growth": return "刷新成长";
                case "permanent_growth": return "永久成长";
                case "discover_minion": return "随从发现";
                case "spell": return "法术";
                case "economy": return "经济";
                case "shield": return "护盾";
                case "summon": return "召唤";
                case "immediate_attack": return "立即攻击";
                case "race": return "种族";
                case "global_growth": return "群体成长";
                case "golden_bonus": return "金色加成";
                case "next_combat": return "下场战斗";
                default: return value;
            }
        }

        private static Image CreateImage(
            string name,
            Transform parent,
            Color color)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void ConfigureFrame(Image image)
        {
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.fillCenter = false;
            image.pixelsPerUnitMultiplier = 2f;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            int fontSize,
            TextAnchor alignment,
            HorizontalWrapMode horizontalOverflow = HorizontalWrapMode.Wrap,
            VerticalWrapMode verticalOverflow = VerticalWrapMode.Overflow)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.95f, 0.96f, 0.98f, 1f);
            text.supportRichText = false;
            text.resizeTextForBestFit = false;
            text.horizontalOverflow = horizontalOverflow;
            text.verticalOverflow = verticalOverflow;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform target, float inset = 0f)
        {
            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.one;
            target.pivot = new Vector2(0.5f, 0.5f);
            target.offsetMin = new Vector2(inset, inset);
            target.offsetMax = new Vector2(-inset, -inset);
            target.localScale = Vector3.one;
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
                    "Missing serialized CardView property: " + propertyName);
            }

            property.objectReferenceValue = value;
        }

        private static void SetReferenceArray<T>(
            SerializedObject serialized,
            string propertyName,
            T[] values)
            where T : UnityEngine.Object
        {
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    "Missing serialized CardView array: " + propertyName);
            }

            property.arraySize = values.Length;
            for (var index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    values[index];
            }
        }

        private static void EnsureFolder(string parent, string name)
        {
            var path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private readonly struct ArtworkSpec
        {
            public ArtworkSpec(
                string id,
                string path,
                float focalPointY)
            {
                Id = id;
                Path = path;
                FocalPointY = focalPointY;
            }

            public string Id { get; }
            public string Path { get; }
            public float FocalPointY { get; }
        }
    }
}

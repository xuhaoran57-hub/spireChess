using System;
using System.IO;
using System.Linq;
using SpireChess.UI;
using SpireChess.UI.Shop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpireChess.Editor
{
    public static class ShopUiPrefabBuilder
    {
        public const string SlotPrefabPath =
            "Assets/Prefabs/UI/Shop/PF_ShopSlot.prefab";
        public const string ScreenPrefabPath =
            "Assets/Prefabs/UI/Shop/PF_ShopScreen.prefab";
        public const string ChoicePrefabPath =
            "Assets/Prefabs/UI/Shop/PF_ChoiceOverlay.prefab";
        public const string PreviewScenePath =
            "Assets/Scenes/ShopUiPreview.unity";
        public const string ShopScenePath =
            "Assets/Scenes/ShopTest.unity";

        private static readonly Color ScreenBackground =
            new Color(0.035f, 0.045f, 0.07f, 1f);
        private static readonly Color PanelColor =
            new Color(0.075f, 0.09f, 0.13f, 0.97f);
        private static readonly Color PanelBorder =
            new Color(0.22f, 0.28f, 0.38f, 0.85f);
        private static readonly Color ButtonColor =
            new Color(0.16f, 0.19f, 0.25f, 1f);

        [MenuItem("Spire Chess/UI/Rebuild Shop UI")]
        public static void Build()
        {
            CardUiPrefabBuilder.Build();
            EnsureFolder("Assets/Prefabs/UI", "Shop");
            AssetDatabase.ImportAsset(
                CardUiPrefabBuilder.FontPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            var font = AssetDatabase.LoadAssetAtPath<Font>(
                CardUiPrefabBuilder.FontPath);
            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CardUiPrefabBuilder.PrefabPath);
            if (font == null || cardPrefab == null)
            {
                throw new InvalidOperationException(
                    "Shop UI requires the pinned font and PF_Card.");
            }

            BuildSlot(font);
            var slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                SlotPrefabPath);
            BuildChoice(font, cardPrefab);
            var choicePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                ChoicePrefabPath);
            if (slotPrefab == null || choicePrefab == null)
            {
                throw new InvalidOperationException(
                    "Failed to reload generated shop child prefabs.");
            }

            BuildScreen(font, cardPrefab, slotPrefab, choicePrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WireShopTestScene();
            Debug.Log("[ShopUI] Rebuilt static shop prefabs.");
        }

        [MenuItem("Spire Chess/UI/Rebuild and Capture Shop UI")]
        public static void BuildAndCapture()
        {
            Build();
            CaptureValidationScreenshots();
        }

        public static void CaptureValidationScreenshots()
        {
            var screenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                ScreenPrefabPath);
            if (screenPrefab == null)
            {
                throw new InvalidOperationException(
                    "Build PF_ShopScreen before capturing screenshots.");
            }

            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            var cameraObject = new GameObject("ShopUiPreviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ScreenBackground;
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            camera.transform.position = new Vector3(0f, 0f, -100f);

            var screen = PrefabUtility.InstantiatePrefab(screenPrefab) as GameObject;
            if (screen == null)
            {
                throw new InvalidOperationException(
                    "Failed to instantiate PF_ShopScreen for validation.");
            }

            screen.name = "ShopUiPreview";
            var canvas = screen.GetComponent<Canvas>();
            var canvasRect = screen.GetComponent<RectTransform>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.sortingOrder = 1;
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.sizeDelta = new Vector2(1920f, 1080f);
            canvasRect.position = Vector3.zero;
            canvasRect.localScale = Vector3.one;

            var view = screen.GetComponent<ShopScreenView>();
            var state = CreatePreviewState();
            view.Render(state);
            EditorSceneManager.SaveScene(scene, PreviewScenePath);

            var repositoryRoot = Directory.GetParent(
                Directory.GetParent(Application.dataPath).FullName).FullName;
            var outputDirectory = Path.Combine(
                repositoryRoot,
                "ui-concepts",
                "unity-validation",
                "pf-shop-screen-v0.1");
            Directory.CreateDirectory(outputDirectory);
            Capture(
                camera,
                canvasRect,
                1920,
                1080,
                Path.Combine(outputDirectory, "shop-screen-1920x1080.png"));
            canvasRect.sizeDelta = new Vector2(1920f, 1200f);
            view.Render(state);
            Capture(
                camera,
                canvasRect,
                1920,
                1200,
                Path.Combine(outputDirectory, "shop-screen-1920x1200.png"));

            canvasRect.sizeDelta = new Vector2(1920f, 1080f);
            view.Render(state);
            view.RenderChoice(CreatePreviewChoice());
            Capture(
                camera,
                canvasRect,
                1920,
                1080,
                Path.Combine(outputDirectory, "choice-overlay-1920x1080.png"));

            view.RenderChoice(null);
            state.BattleCards[0].Attack += 2;
            state.BattleCards[0].Health += 1;
            state.BattleCards[0].HasShield = true;
            state.IsFrozen = true;
            state.Buttons.Freeze.IsActive = true;
            view.Render(state);
            view.ShowStatus("金币不足：这是错误 Toast 验收状态", true);
            Capture(
                camera,
                canvasRect,
                1920,
                1080,
                Path.Combine(outputDirectory, "shop-feedback-1920x1080.png"));
            AssetDatabase.SaveAssets();
            Debug.Log("[ShopUI] Captured validation screenshots to " +
                      outputDirectory);
        }

        public static void WireShopTestScene()
        {
            var screenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                ScreenPrefabPath);
            if (screenPrefab == null)
            {
                throw new InvalidOperationException(
                    "Build PF_ShopScreen before wiring ShopTest.");
            }

            var scene = EditorSceneManager.OpenScene(
                ShopScenePath,
                OpenSceneMode.Single);
            foreach (var rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.GetComponent<ShopScreenView>() != null ||
                    rootObject.GetComponent<ShopTestController>() != null)
                {
                    UnityEngine.Object.DestroyImmediate(rootObject);
                }
            }

            var screen = PrefabUtility.InstantiatePrefab(
                screenPrefab,
                scene) as GameObject;
            if (screen == null)
            {
                throw new InvalidOperationException(
                    "Failed to place PF_ShopScreen in ShopTest.");
            }

            screen.name = "PF_ShopScreen";
            var screenView = screen.GetComponent<ShopScreenView>();
            var controllerObject = new GameObject(
                "ShopTestController",
                typeof(ShopTestController));
            SceneManager.MoveGameObjectToScene(controllerObject, scene);
            var controller = controllerObject.GetComponent<ShopTestController>();
            var serialized = new SerializedObject(controller);
            SetReference(serialized, "screenView", screenView);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var eventSystems = scene.GetRootGameObjects()
                .SelectMany(value =>
                    value.GetComponentsInChildren<EventSystem>(true))
                .ToArray();
            if (eventSystems.Length == 0)
            {
                var eventSystem = new GameObject(
                    "EventSystem",
                    typeof(EventSystem),
                    typeof(StandaloneInputModule));
                SceneManager.MoveGameObjectToScene(eventSystem, scene);
            }
            else
            {
                for (var index = 1; index < eventSystems.Length; index++)
                {
                    UnityEngine.Object.DestroyImmediate(
                        eventSystems[index].gameObject);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ShopScenePath);
            Debug.Log("[ShopUI] Wired formal UI into " + ShopScenePath);
        }

        private static void BuildSlot(Font font)
        {
            var root = new GameObject(
                "PF_ShopSlot",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement),
                typeof(ShopSlotView));
            try
            {
                var rootRect = root.GetComponent<RectTransform>();
                SetTopLeft(rootRect, 240f, 360f);
                var rootImage = root.GetComponent<Image>();
                rootImage.color = new Color(1f, 1f, 1f, 0.01f);
                rootImage.raycastTarget = true;
                var layout = root.GetComponent<LayoutElement>();
                layout.minWidth = layout.preferredWidth = 240f;
                layout.minHeight = layout.preferredHeight = 360f;

                var background = CreateImage(
                    "Background",
                    root.transform,
                    new Color(0.08f, 0.105f, 0.15f, 0.96f));
                Stretch(background.rectTransform);
                ConfigureFrame(background, true);

                var emptyHint = CreateText(
                    "EmptyHint",
                    root.transform,
                    font,
                    16,
                    TextAnchor.MiddleCenter);
                emptyHint.text = "空槽位";
                emptyHint.color = new Color(0.48f, 0.54f, 0.64f, 1f);
                Stretch(emptyHint.rectTransform, 12f);

                var selectionFrame = CreateImage(
                    "SelectionFrame",
                    root.transform,
                    new Color(0.28f, 0.88f, 1f, 0.92f));
                Stretch(selectionFrame.rectTransform);
                ConfigureFrame(selectionFrame, false);
                selectionFrame.gameObject.SetActive(false);

                var content = CreateRect("Content", root.transform);
                Stretch(content);

                var serialized = new SerializedObject(
                    root.GetComponent<ShopSlotView>());
                SetReference(serialized, "background", background);
                SetReference(serialized, "emptyHint", emptyHint);
                SetReference(serialized, "selectionFrame", selectionFrame);
                SetReference(serialized, "content", content);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                SavePrefab(root, SlotPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildChoice(Font font, GameObject cardPrefab)
        {
            var root = new GameObject(
                "PF_ChoiceOverlay",
                typeof(RectTransform),
                typeof(ChoiceOverlayView));
            try
            {
                var rootRect = root.GetComponent<RectTransform>();
                SetTopLeft(rootRect, 1920f, 1080f);

                var panel = CreateImage(
                    "Dialog",
                    root.transform,
                    new Color(0.055f, 0.07f, 0.105f, 0.99f));
                SetCentered(panel.rectTransform, 1100f, 720f);
                ConfigureFrame(panel, true);

                var title = CreateText(
                    "Title",
                    panel.transform,
                    font,
                    30,
                    TextAnchor.MiddleCenter,
                    HorizontalWrapMode.Overflow);
                SetTop(title.rectTransform, 24f, 24f, 1052f, 48f);

                var description = CreateText(
                    "Description",
                    panel.transform,
                    font,
                    17,
                    TextAnchor.MiddleCenter);
                description.color = new Color(0.70f, 0.76f, 0.86f, 1f);
                SetTop(description.rectTransform, 60f, 78f, 980f, 46f);

                var candidateRow = CreateRect("Candidates", panel.transform);
                SetTop(candidateRow, 30f, 132f, 1040f, 430f);
                var rowLayout = candidateRow.gameObject.AddComponent<
                    HorizontalLayoutGroup>();
                rowLayout.spacing = 10f;
                rowLayout.childAlignment = TextAnchor.MiddleCenter;
                rowLayout.childControlWidth = false;
                rowLayout.childControlHeight = false;
                rowLayout.childForceExpandWidth = false;
                rowLayout.childForceExpandHeight = false;

                var roots = new GameObject[4];
                var contents = new RectTransform[4];
                var buttons = new Button[4];
                var labels = new Text[4];
                var descriptions = new Text[4];
                for (var index = 0; index < roots.Length; index++)
                {
                    var candidate = CreateButtonRoot(
                        "Candidate" + index,
                        candidateRow,
                        new Color(0.10f, 0.13f, 0.19f, 1f));
                    roots[index] = candidate.gameObject;
                    buttons[index] = candidate;
                    var candidateRect = candidate.GetComponent<RectTransform>();
                    candidateRect.sizeDelta = new Vector2(250f, 420f);
                    var element = candidate.gameObject.AddComponent<LayoutElement>();
                    element.minWidth = element.preferredWidth = 250f;
                    element.minHeight = element.preferredHeight = 420f;

                    contents[index] = CreateRect(
                        "CardContent",
                        candidate.transform);
                    SetTop(contents[index], 5f, 4f, 240f, 360f);
                    labels[index] = CreateText(
                        "Label",
                        candidate.transform,
                        font,
                        17,
                        TextAnchor.MiddleCenter,
                        HorizontalWrapMode.Overflow);
                    SetTop(labels[index].rectTransform, 8f, 366f, 234f, 26f);
                    descriptions[index] = CreateText(
                        "Description",
                        candidate.transform,
                        font,
                        13,
                        TextAnchor.MiddleCenter);
                    descriptions[index].color =
                        new Color(0.68f, 0.74f, 0.84f, 1f);
                    SetTop(
                        descriptions[index].rectTransform,
                        8f,
                        392f,
                        234f,
                        24f);
                }

                var cancel = CreateButton(
                    "CancelButton",
                    panel.transform,
                    font,
                    "取消",
                    18,
                    ButtonColor);
                SetTop(
                    cancel.Button.GetComponent<RectTransform>(),
                    390f,
                    632f,
                    320f,
                    58f);

                var serialized = new SerializedObject(
                    root.GetComponent<ChoiceOverlayView>());
                SetReference(serialized, "cardPrefab", cardPrefab);
                SetReference(serialized, "titleText", title);
                SetReference(serialized, "descriptionText", description);
                SetReferenceArray(serialized, "candidateRoots", roots);
                SetReferenceArray(serialized, "cardContents", contents);
                SetReferenceArray(serialized, "candidateButtons", buttons);
                SetReferenceArray(serialized, "candidateLabels", labels);
                SetReferenceArray(
                    serialized,
                    "candidateDescriptions",
                    descriptions);
                SetReference(serialized, "cancelButton", cancel.Button);
                SetReference(serialized, "cancelButtonText", cancel.Label);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                SavePrefab(root, ChoicePrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildScreen(
            Font font,
            GameObject cardPrefab,
            GameObject slotPrefab,
            GameObject choicePrefab)
        {
            var root = new GameObject(
                "PF_ShopScreen",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(ShopScreenView));
            try
            {
                var rootRect = root.GetComponent<RectTransform>();
                rootRect.sizeDelta = new Vector2(1920f, 1080f);
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode =
                    CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                var safeArea = CreateRect("SafeArea", root.transform);
                Stretch(safeArea);
                var background = CreateImage(
                    "Background",
                    safeArea,
                    ScreenBackground);
                Stretch(background.rectTransform);

                var topBar = BuildTopBar(safeArea, font, out var topTexts);
                var content = BuildContent(
                    safeArea,
                    font,
                    slotPrefab,
                    out var minionSlots,
                    out var spellSlot,
                    out var battleSlots,
                    out var handSlots,
                    out var pageControls,
                    out var pageLeft,
                    out var pageText,
                    out var pageRight);
                var actionRail = BuildActionRail(
                    safeArea,
                    font,
                    out var detailContent,
                    out var detailTexts,
                    out var actionButtons);
                var feedbackLayer = BuildFeedbackLayer(
                    safeArea,
                    font,
                    out var statusToast,
                    out var statusToastImage,
                    out var statusToastCanvasGroup,
                    out var statusToastText);
                var modalLayer = BuildModalLayer(
                    safeArea,
                    font,
                    choicePrefab,
                    out var blocker,
                    out var choiceOverlay,
                    out var rewardOverlay,
                    out var rewardText,
                    out var rewardClaimButton,
                    out var rewardSkipButton);

                // Keep contract nodes used even when their references are only
                // expressed by the hierarchy.
                _ = topBar;
                _ = content;
                _ = actionRail;
                _ = feedbackLayer;
                _ = modalLayer;

                var serialized = new SerializedObject(
                    root.GetComponent<ShopScreenView>());
                SetReference(serialized, "rootCanvas", canvas);
                SetReference(serialized, "safeArea", safeArea);
                SetReference(serialized, "cardPrefab", cardPrefab);
                SetReference(serialized, "roundText", topTexts[0]);
                SetReference(serialized, "goldText", topTexts[1]);
                SetReference(serialized, "tavernTierText", topTexts[2]);
                SetReference(serialized, "upgradeCostText", topTexts[3]);
                SetReference(serialized, "statusText", topTexts[4]);
                SetReferenceArray(serialized, "minionOfferSlots", minionSlots);
                SetReference(serialized, "spellOfferSlot", spellSlot);
                SetReferenceArray(serialized, "battleSlots", battleSlots);
                SetReferenceArray(serialized, "handSlots", handSlots);
                SetReference(serialized, "pageControls", pageControls);
                SetReference(serialized, "pageLeftButton", pageLeft);
                SetReference(serialized, "pageText", pageText);
                SetReference(serialized, "pageRightButton", pageRight);
                SetReference(serialized, "detailContent", detailContent);
                SetReference(serialized, "detailTitleText", detailTexts[0]);
                SetReference(serialized, "detailMetaText", detailTexts[1]);
                SetReference(
                    serialized,
                    "detailDescriptionText",
                    detailTexts[2]);
                SetReference(serialized, "detailStatusesText", detailTexts[3]);
                SetReference(serialized, "refreshButton", actionButtons[0].Button);
                SetReference(
                    serialized,
                    "refreshButtonText",
                    actionButtons[0].Label);
                SetReference(serialized, "freezeButton", actionButtons[1].Button);
                SetReference(
                    serialized,
                    "freezeButtonText",
                    actionButtons[1].Label);
                SetReference(serialized, "upgradeButton", actionButtons[2].Button);
                SetReference(
                    serialized,
                    "upgradeButtonText",
                    actionButtons[2].Label);
                SetReference(serialized, "sellButton", actionButtons[3].Button);
                SetReference(
                    serialized,
                    "sellButtonText",
                    actionButtons[3].Label);
                SetReference(serialized, "endButton", actionButtons[4].Button);
                SetReference(
                    serialized,
                    "endButtonText",
                    actionButtons[4].Label);
                SetReference(serialized, "statusToast", statusToast);
                SetReference(
                    serialized,
                    "statusToastImage",
                    statusToastImage);
                SetReference(
                    serialized,
                    "statusToastCanvasGroup",
                    statusToastCanvasGroup);
                SetReference(serialized, "statusToastText", statusToastText);
                SetReference(serialized, "modalBlocker", blocker);
                SetReference(serialized, "choiceOverlay", choiceOverlay);
                SetReference(serialized, "rewardOverlay", rewardOverlay);
                SetReference(serialized, "rewardText", rewardText);
                SetReference(
                    serialized,
                    "rewardClaimButton",
                    rewardClaimButton);
                SetReference(
                    serialized,
                    "rewardSkipButton",
                    rewardSkipButton);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                SavePrefab(root, ScreenPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static RectTransform BuildTopBar(
            RectTransform safeArea,
            Font font,
            out Text[] texts)
        {
            var topBar = CreateImage("TopBar", safeArea, PanelColor);
            SetTopStretch(topBar.rectTransform, 32f, 32f, 0f, 96f);
            ConfigureFrame(topBar, true);
            var layout = topBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 10, 10);
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            texts = new Text[5];
            texts[0] = CreateStatusText("RoundText", topBar.transform, font, 20, 190f);
            texts[1] = CreateStatusText("GoldText", topBar.transform, font, 20, 170f);
            texts[2] = CreateStatusText(
                "TavernTierText",
                topBar.transform,
                font,
                20,
                230f);
            texts[3] = CreateStatusText(
                "UpgradeCostText",
                topBar.transform,
                font,
                20,
                230f);
            texts[4] = CreateStatusText("StatusText", topBar.transform, font, 18, 0f);
            texts[4].GetComponent<LayoutElement>().flexibleWidth = 1f;

            texts[0].text = "第 3 回合";
            texts[1].text = "金币：8";
            texts[2].text = "酒馆等级：5";
            texts[3].text = "升级费用：5";
            texts[4].text = "商店阶段 · 购买、使用手牌或调整阵容";
            return topBar.rectTransform;
        }

        private static RectTransform BuildContent(
            RectTransform safeArea,
            Font font,
            GameObject slotPrefab,
            out ShopSlotView[] minionSlots,
            out ShopSlotView spellSlot,
            out ShopSlotView[] battleSlots,
            out ShopSlotView[] handSlots,
            out GameObject pageControls,
            out Button pageLeft,
            out Text pageText,
            out Button pageRight)
        {
            var content = CreateRect("Content", safeArea);
            SetStretch(content, 32f, 272f, 104f, 32f);
            var vertical = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.spacing = 10f;
            vertical.childAlignment = TextAnchor.UpperCenter;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;

            var offer = CreatePanel("OfferPanel", content, 388f);
            var offerTitle = CreatePanelTitle("Title", offer, font, "商品区", 28f);
            var offerRow = CreateSlotRow("OfferSlots", offer, 360f);
            _ = offerTitle;
            minionSlots = new ShopSlotView[4];
            for (var index = 0; index < minionSlots.Length; index++)
            {
                minionSlots[index] = CreateSlotInstance(
                    slotPrefab,
                    offerRow,
                    "MinionSlot" + index,
                    240f,
                    360f);
            }

            spellSlot = CreateSlotInstance(
                slotPrefab,
                offerRow,
                "SpellSlot",
                240f,
                360f);

            var battle = CreatePanel("BattlePanel", content, 268f);
            CreatePanelTitle(
                "Title",
                battle,
                font,
                "战斗区 · 最多 5 个随从",
                28f);
            var battleRow = CreateSlotRow("BattleSlots", battle, 240f);
            battleSlots = new ShopSlotView[5];
            for (var index = 0; index < battleSlots.Length; index++)
            {
                battleSlots[index] = CreateSlotInstance(
                    slotPrefab,
                    battleRow,
                    "BattleSlot" + index,
                    160f,
                    240f);
            }

            var hand = CreatePanel("HandPanel", content, 268f);
            CreatePanelTitle("Title", hand, font, "手牌", 28f);
            var handRow = CreateSlotRow("HandSlots", hand, 240f);
            handSlots = new ShopSlotView[5];
            for (var index = 0; index < handSlots.Length; index++)
            {
                handSlots[index] = CreateSlotInstance(
                    slotPrefab,
                    handRow,
                    "HandSlot" + index,
                    160f,
                    240f);
            }

            var paging = CreateRect("PageControls", hand);
            pageControls = paging.gameObject;
            SetTopRight(paging, 16f, 2f, 230f, 24f);
            var pagingLayout = paging.gameObject.AddComponent<
                HorizontalLayoutGroup>();
            pagingLayout.spacing = 8f;
            pagingLayout.childAlignment = TextAnchor.MiddleCenter;
            pagingLayout.childControlWidth = false;
            pagingLayout.childControlHeight = false;
            pagingLayout.childForceExpandWidth = false;
            pagingLayout.childForceExpandHeight = false;
            paging.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var left = CreateButton(
                "PageLeft",
                paging,
                font,
                "‹",
                17,
                ButtonColor);
            left.Button.GetComponent<RectTransform>().sizeDelta =
                new Vector2(52f, 24f);
            var current = CreateText(
                "PageText",
                paging,
                font,
                14,
                TextAnchor.MiddleCenter,
                HorizontalWrapMode.Overflow);
            current.rectTransform.sizeDelta = new Vector2(90f, 24f);
            current.text = "1 / 1";
            var right = CreateButton(
                "PageRight",
                paging,
                font,
                "›",
                17,
                ButtonColor);
            right.Button.GetComponent<RectTransform>().sizeDelta =
                new Vector2(52f, 24f);
            pageLeft = left.Button;
            pageText = current;
            pageRight = right.Button;
            pageControls.SetActive(false);
            return content;
        }

        private static RectTransform BuildActionRail(
            RectTransform safeArea,
            Font font,
            out GameObject detailContent,
            out Text[] detailTexts,
            out ButtonParts[] buttons)
        {
            var rail = CreateImage("ActionRail", safeArea, PanelColor);
            rail.rectTransform.anchorMin = new Vector2(1f, 0f);
            rail.rectTransform.anchorMax = new Vector2(1f, 1f);
            rail.rectTransform.pivot = new Vector2(1f, 1f);
            rail.rectTransform.offsetMin = new Vector2(-252f, 32f);
            rail.rectTransform.offsetMax = new Vector2(-32f, -104f);
            ConfigureFrame(rail, true);
            var layout = rail.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var detailPanel = CreateImage(
                "CardDetailPanel",
                rail.transform,
                new Color(0.055f, 0.07f, 0.10f, 1f));
            var detailElement = detailPanel.gameObject.AddComponent<LayoutElement>();
            detailElement.preferredHeight = detailElement.minHeight = 330f;
            ConfigureFrame(detailPanel, true);
            var detailRoot = CreateRect("Content", detailPanel.transform);
            detailContent = detailRoot.gameObject;
            Stretch(detailRoot, 10f);
            detailTexts = new Text[4];
            detailTexts[0] = CreateText(
                "Title",
                detailRoot,
                font,
                20,
                TextAnchor.UpperLeft,
                HorizontalWrapMode.Overflow);
            SetTop(detailTexts[0].rectTransform, 0f, 0f, 176f, 34f);
            detailTexts[1] = CreateText(
                "Meta",
                detailRoot,
                font,
                13,
                TextAnchor.UpperLeft);
            detailTexts[1].color = new Color(0.66f, 0.74f, 0.86f, 1f);
            SetTop(detailTexts[1].rectTransform, 0f, 38f, 176f, 42f);
            detailTexts[2] = CreateText(
                "Description",
                detailRoot,
                font,
                14,
                TextAnchor.UpperLeft);
            SetTop(detailTexts[2].rectTransform, 0f, 88f, 176f, 105f);
            detailTexts[3] = CreateText(
                "Statuses",
                detailRoot,
                font,
                12,
                TextAnchor.UpperLeft);
            detailTexts[3].color = new Color(0.47f, 0.88f, 0.72f, 1f);
            SetTop(detailTexts[3].rectTransform, 0f, 202f, 176f, 104f);

            buttons = new[]
            {
                CreateActionButton("RefreshButton", rail.transform, font, "刷新"),
                CreateActionButton("FreezeButton", rail.transform, font, "冻结"),
                CreateActionButton("UpgradeButton", rail.transform, font, "升级酒馆"),
                CreateActionButton("SellButton", rail.transform, font, "出售"),
                CreateActionButton("EndButton", rail.transform, font, "锁定阵容并进入战斗")
            };

            var hint = CreateText(
                "DisabledReasonHint",
                rail.transform,
                font,
                13,
                TextAnchor.UpperCenter);
            hint.text = "操作反馈和禁用原因会显示在这里";
            hint.color = new Color(0.50f, 0.56f, 0.66f, 1f);
            var hintElement = hint.gameObject.AddComponent<LayoutElement>();
            hintElement.preferredHeight = 70f;
            hintElement.flexibleHeight = 1f;
            return rail.rectTransform;
        }

        private static RectTransform BuildFeedbackLayer(
            RectTransform safeArea,
            Font font,
            out GameObject toastObject,
            out Image toastImage,
            out CanvasGroup toastCanvasGroup,
            out Text toastText)
        {
            var layer = CreateRect("FeedbackLayer", safeArea);
            Stretch(layer);
            var floating = CreateRect("FloatingTextRoot", layer);
            Stretch(floating);
            var toast = CreateImage(
                "StatusToast",
                layer,
                new Color(0.06f, 0.08f, 0.12f, 0.96f));
            toastObject = toast.gameObject;
            toastImage = toast;
            toastCanvasGroup = toast.gameObject.AddComponent<CanvasGroup>();
            toastCanvasGroup.interactable = false;
            toastCanvasGroup.blocksRaycasts = false;
            toast.raycastTarget = false;
            SetBottomCenter(toast.rectTransform, 560f, 54f, 22f);
            ConfigureFrame(toast, true);
            toastText = CreateText(
                "Text",
                toast.transform,
                font,
                16,
                TextAnchor.MiddleCenter);
            Stretch(toastText.rectTransform, 10f);
            toastObject.SetActive(false);
            return layer;
        }

        private static RectTransform BuildModalLayer(
            RectTransform safeArea,
            Font font,
            GameObject choicePrefab,
            out GameObject blocker,
            out ChoiceOverlayView choiceOverlay,
            out GameObject rewardOverlay,
            out Text rewardText,
            out Button rewardClaimButton,
            out Button rewardSkipButton)
        {
            var layer = CreateRect("ModalLayer", safeArea);
            Stretch(layer);
            var blockerImage = CreateImage(
                "Blocker",
                layer,
                new Color(0f, 0f, 0f, 0.68f));
            blocker = blockerImage.gameObject;
            blockerImage.raycastTarget = true;
            Stretch(blockerImage.rectTransform);
            blocker.SetActive(false);

            var choice = PrefabUtility.InstantiatePrefab(
                choicePrefab,
                layer) as GameObject;
            if (choice == null)
            {
                throw new InvalidOperationException(
                    "Failed to nest PF_ChoiceOverlay.");
            }

            choice.name = "ChoiceOverlay";
            Stretch(choice.GetComponent<RectTransform>());
            choiceOverlay = choice.GetComponent<ChoiceOverlayView>();
            choice.SetActive(false);

            var reward = CreateImage(
                "RewardOverlay",
                layer,
                new Color(0.07f, 0.09f, 0.13f, 0.98f));
            rewardOverlay = reward.gameObject;
            SetCentered(reward.rectTransform, 720f, 420f);
            ConfigureFrame(reward, true);
            var rewardTitle = CreateText(
                "Title",
                reward.transform,
                font,
                28,
                TextAnchor.MiddleCenter,
                HorizontalWrapMode.Overflow);
            rewardTitle.text = "待领取卡牌奖励";
            SetTop(rewardTitle.rectTransform, 30f, 24f, 660f, 52f);
            rewardText = CreateText(
                "RewardText",
                reward.transform,
                font,
                20,
                TextAnchor.MiddleCenter);
            SetTop(rewardText.rectTransform, 40f, 96f, 640f, 170f);
            var claim = CreateButton(
                "ClaimButton",
                reward.transform,
                font,
                "领取",
                18,
                ButtonColor);
            SetTop(
                claim.Button.GetComponent<RectTransform>(),
                70f,
                310f,
                270f,
                64f);
            rewardClaimButton = claim.Button;
            var skip = CreateButton(
                "SkipButton",
                reward.transform,
                font,
                "跳过并返还牌池",
                18,
                ButtonColor);
            SetTop(
                skip.Button.GetComponent<RectTransform>(),
                380f,
                310f,
                270f,
                64f);
            rewardSkipButton = skip.Button;
            reward.gameObject.SetActive(false);
            return layer;
        }

        private static ShopScreenState CreatePreviewState()
        {
            var offers = new[]
            {
                CreateMinion("铁甲卫士", "获得 8 护盾。相邻友军获得 2 护盾。", "铸魂", 1, 2, 4),
                CreateMinion("森林射手", "攻击时，永久获得 +1 攻击。", "荒灵", 1, 3, 2),
                CreateMinion("山岭巨人", "战斗开始时，获得 20 护盾。每有 100 护盾，体型 +1。", "铸魂", 2, 4, 6),
                CreateMinion("暗影刺客", "首次攻击会突进至最远的敌人身后。", "旅团", 1, 3, 3)
            };
            offers[1].AbilityLabels = new[] { "成长", "穿透" };
            offers[2].IsGolden = true;
            offers[2].HasShield = true;

            var battle = new CardViewModel[5];
            battle[0] = CreateOwned("森林射手", "攻击时永久成长。", "荒灵", 1, 3, 2);
            battle[1] = CreateOwned("铁甲卫士", "获得护盾并保护相邻友军。", "铸魂", 1, 2, 4);
            battle[1].IsSelected = true;
            battle[1].HasShield = true;
            battle[2] = CreateOwned("山岭巨人", "护盾越高，力量越强。", "铸魂", 2, 4, 6);
            battle[2].Attack = 7;
            battle[2].Health = 10;

            var handModels = new CardViewModel[5];
            handModels[0] = CreateOwned("暗影刺客", "首次攻击会突进。", "旅团", 1, 3, 3);
            handModels[1] = CreateOwned("炼金学徒", "本回合下一次刷新免费。", "星契", 1, 2, 3);
            handModels[1].HasNextCombatShield = true;
            handModels[2] = CreateSpell(
                "能量涌动",
                "使一个友军获得 +3 攻击，持续 1 回合。",
                CardDisplayMode.Compact);
            handModels[3] = CreateSpell(
                "治疗之泉",
                "恢复一个友军 6 点生命。",
                CardDisplayMode.Compact);
            handModels[3].IsTemporary = true;

            return new ShopScreenState
            {
                Round = 3,
                Gold = 8,
                TavernTier = 5,
                UpgradeCost = 5,
                RefreshCount = 2,
                FreeRefreshes = 1,
                IsShopOpen = true,
                MinionOffers = offers,
                SpellOffer = CreateSpell(
                    "能量涌动",
                    "使一个友军获得 +3 攻击，持续 1 回合。",
                    CardDisplayMode.Full),
                BattleCards = battle,
                HandCards = new HandCardsState
                {
                    Count = 4,
                    Limit = 5,
                    PageSize = 5,
                    PageIndex = 0,
                    PageCount = 1,
                    VisibleSlots = handModels.Select((card, index) =>
                        new HandCardSlotState
                        {
                            SlotIndex = index,
                            Card = card
                        }).ToArray()
                },
                Buttons = new ShopButtonStates
                {
                    Refresh = Action("刷新（免费 1 次）", true),
                    Freeze = Action("冻结", true),
                    Upgrade = Action("升级酒馆（5 金币）", true),
                    Sell = Action("出售（1 金币）", true),
                    EndShop = Action("锁定阵容并进入战斗", true)
                },
                DetailPanel = new CardDetailPanelState
                {
                    Card = battle[1],
                    Location = ShopCardLocation.Battle,
                    SlotIndex = 1,
                    Statuses = new[]
                    {
                        new CardDetailStatusState
                        {
                            Type = CardDetailStatusType.PermanentShield,
                            Label = "永久护盾",
                            Description = "持续存在，不会在回合结束时消失"
                        }
                    }
                },
                StatusMessage = string.Empty
            };
        }

        private static ChoiceViewModel CreatePreviewChoice()
        {
            var first = CreateMinion(
                "天穹契约者",
                "每完成指定次数刷新，使所有友方星契永久获得成长。",
                "星契",
                3,
                4,
                8);
            var second = CreateMinion(
                "旧塔向导",
                "每当你刷新商店时，使相邻随从永久获得属性提升，并在达到阈值后获得护盾。",
                "旅团",
                3,
                5,
                7);
            second.IsGolden = true;
            var third = CreateMinion(
                "万蹄奔涌",
                "战斗开始时召唤援军；每次友方随从完成成长后，提高本次召唤的属性。",
                "荒灵",
                3,
                6,
                6);
            return new ChoiceViewModel
            {
                Title = "选择一张发现卡牌",
                Description = "三连奖励必须选择一张，其他商店操作暂时不可用。",
                CanCancel = false,
                Candidates = new[]
                {
                    new ChoiceCandidateViewModel { Label = first.Name, Card = first },
                    new ChoiceCandidateViewModel { Label = second.Name, Card = second },
                    new ChoiceCandidateViewModel { Label = third.Name, Card = third }
                }
            };
        }

        private static CardViewModel CreateMinion(
            string name,
            string description,
            string race,
            int tier,
            int attack,
            int health)
        {
            return new CardViewModel
            {
                Name = name,
                Description = description,
                RaceText = race,
                AbilityLabels = new[] { "护盾", "成长" },
                Tier = tier,
                Attack = attack,
                Health = health,
                BaseAttack = attack,
                BaseHealth = health,
                Cost = 3,
                DisplayMode = CardDisplayMode.Full,
                IsMinion = true,
                ShowCost = true,
                IsInteractable = true,
                IsAffordable = true
            };
        }

        private static CardViewModel CreateOwned(
            string name,
            string description,
            string race,
            int tier,
            int attack,
            int health)
        {
            var model = CreateMinion(
                name,
                description,
                race,
                tier,
                attack,
                health);
            model.DisplayMode = CardDisplayMode.Compact;
            model.ShowCost = false;
            model.InstanceId = "owned_preview_" + name;
            return model;
        }

        private static CardViewModel CreateSpell(
            string name,
            string description,
            CardDisplayMode mode)
        {
            return new CardViewModel
            {
                InstanceId = "preview_" + name,
                Name = name,
                Description = description,
                RaceText = "商店法术",
                AbilityLabels = new[] { "法术" },
                Tier = 1,
                Cost = 1,
                DisplayMode = mode,
                ShowCost = mode == CardDisplayMode.Full,
                IsInteractable = true,
                IsAffordable = true
            };
        }

        private static ShopActionButtonState Action(string text, bool interactable)
        {
            return new ShopActionButtonState
            {
                Text = text,
                IsVisible = true,
                IsInteractable = interactable
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

        private static RectTransform CreatePanel(
            string name,
            Transform parent,
            float height)
        {
            var panel = CreateImage(name, parent, PanelColor);
            panel.raycastTarget = false;
            var element = panel.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = height;
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return panel.rectTransform;
        }

        private static Text CreatePanelTitle(
            string name,
            Transform parent,
            Font font,
            string value,
            float height)
        {
            var title = CreateText(
                name,
                parent,
                font,
                18,
                TextAnchor.MiddleLeft,
                HorizontalWrapMode.Overflow);
            title.text = value;
            var element = title.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = height;
            return title;
        }

        private static RectTransform CreateSlotRow(
            string name,
            Transform parent,
            float height)
        {
            var row = CreateRect(name, parent);
            var element = row.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = height;
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return row;
        }

        private static ShopSlotView CreateSlotInstance(
            GameObject prefab,
            Transform parent,
            string name,
            float width,
            float height)
        {
            var instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException(
                    "Failed to instantiate PF_ShopSlot.");
            }

            instance.name = name;
            var rect = instance.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
            var element = instance.GetComponent<LayoutElement>();
            element.minWidth = element.preferredWidth = width;
            element.minHeight = element.preferredHeight = height;
            return instance.GetComponent<ShopSlotView>();
        }

        private static Text CreateStatusText(
            string name,
            Transform parent,
            Font font,
            int size,
            float width)
        {
            var text = CreateText(
                name,
                parent,
                font,
                size,
                TextAnchor.MiddleLeft,
                HorizontalWrapMode.Overflow);
            var element = text.gameObject.AddComponent<LayoutElement>();
            element.minWidth = width;
            element.preferredWidth = width;
            return text;
        }

        private static ButtonParts CreateActionButton(
            string name,
            Transform parent,
            Font font,
            string label)
        {
            var result = CreateButton(
                name,
                parent,
                font,
                label,
                16,
                ButtonColor);
            var element = result.Button.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = 68f;
            return result;
        }

        private static ButtonParts CreateButton(
            string name,
            Transform parent,
            Font font,
            string label,
            int fontSize,
            Color color)
        {
            var button = CreateButtonRoot(name, parent, color);
            var text = CreateText(
                "Text",
                button.transform,
                font,
                fontSize,
                TextAnchor.MiddleCenter);
            text.text = label;
            Stretch(text.rectTransform, 8f);
            return new ButtonParts(button, text);
        }

        private static Button CreateButtonRoot(
            string name,
            Transform parent,
            Color color)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.color = color;
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.raycastTarget = true;
            var button = gameObject.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.10f, 1.10f, 1.10f, 1f);
            colors.pressedColor = new Color(0.72f, 0.78f, 0.90f, 1f);
            colors.disabledColor = new Color(0.40f, 0.42f, 0.48f, 0.72f);
            button.colors = colors;
            return button;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
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

        private static void ConfigureFrame(Image image, bool fillCenter)
        {
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.fillCenter = fillCenter;
            image.pixelsPerUnitMultiplier = 2f;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    "Failed to save prefab at " + path);
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
                    "Missing serialized property: " + propertyName);
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
                    "Missing serialized array: " + propertyName);
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

        private static void SetTopLeft(
            RectTransform rect,
            float width,
            float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
        }

        private static void SetTop(
            RectTransform rect,
            float x,
            float y,
            float width,
            float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
        }

        private static void SetTopStretch(
            RectTransform rect,
            float left,
            float right,
            float top,
            float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
            rect.localScale = Vector3.one;
        }

        private static void SetTopRight(
            RectTransform rect,
            float right,
            float top,
            float width,
            float height)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-right, -top);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
        }

        private static void SetCentered(
            RectTransform rect,
            float width,
            float height)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
        }

        private static void SetBottomCenter(
            RectTransform rect,
            float width,
            float height,
            float bottom)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, bottom);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
        }

        private static void SetStretch(
            RectTransform rect,
            float left,
            float right,
            float top,
            float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            rect.localScale = Vector3.one;
        }

        private readonly struct ButtonParts
        {
            public ButtonParts(Button button, Text label)
            {
                Button = button;
                Label = label;
            }

            public Button Button { get; }
            public Text Label { get; }
        }
    }
}

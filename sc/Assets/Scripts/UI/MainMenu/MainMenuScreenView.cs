using System;
using SpireChess.Audio;
using SpireChess.Save;
using SpireChess.UI.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpireChess.UI.MainMenu
{
    public sealed class MainMenuScreenView : MonoBehaviour
    {
        [SerializeField] private Text continueSummary;
        [SerializeField] private Text statusText;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private GameObject confirmDialog;
        [SerializeField] private Text confirmMessage;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private AudioSettingsPanelView audioSettingsPanel;

        private MainMenuController controller;
        private Action pendingConfirmation;

        public bool ContinueInteractable => continueButton != null && continueButton.interactable;
        public bool ConfirmationVisible => confirmDialog != null && confirmDialog.activeSelf;
        public bool SettingsVisible =>
            audioSettingsPanel != null && audioSettingsPanel.IsOpen;
        public string StatusText => statusText == null ? string.Empty : statusText.text;

        public void Bind(MainMenuController value)
        {
            controller = value ?? throw new ArgumentNullException(nameof(value));
            newGameButton.onClick.RemoveAllListeners();
            continueButton.onClick.RemoveAllListeners();
            settingsButton.onClick.RemoveAllListeners();
            deleteButton.onClick.RemoveAllListeners();
            quitButton.onClick.RemoveAllListeners();
            confirmButton.onClick.RemoveAllListeners();
            cancelButton.onClick.RemoveAllListeners();
            newGameButton.onClick.AddListener(controller.NewGame);
            newGameButton.onClick.AddListener(
                () => PlayUiCue(PresentationAudioCueIds.UiConfirm));
            continueButton.onClick.AddListener(controller.ContinueGame);
            continueButton.onClick.AddListener(
                () => PlayUiCue(PresentationAudioCueIds.UiConfirm));
            settingsButton.onClick.AddListener(ShowSettings);
            settingsButton.onClick.AddListener(
                () => PlayUiCue(PresentationAudioCueIds.UiClick));
            deleteButton.onClick.AddListener(controller.DeleteSave);
            deleteButton.onClick.AddListener(
                () => PlayUiCue(PresentationAudioCueIds.UiClick));
            quitButton.onClick.AddListener(controller.QuitGame);
            quitButton.onClick.AddListener(
                () => PlayUiCue(PresentationAudioCueIds.UiCancel));
            confirmButton.onClick.AddListener(Confirm);
            confirmButton.onClick.AddListener(
                () => PlayUiCue(PresentationAudioCueIds.UiConfirm));
            cancelButton.onClick.AddListener(HideConfirmation);
            cancelButton.onClick.AddListener(
                () => PlayUiCue(PresentationAudioCueIds.UiCancel));
        }

        public void Render(MainMenuScreenState state)
        {
            if (state == null)
            {
                return;
            }

            continueButton.interactable = state.ContinueEnabled;
            deleteButton.interactable = state.SaveStatus != RunSaveLoadStatus.Missing;
            continueSummary.text = state.ContinueSummary ?? string.Empty;
            statusText.text = state.StatusMessage ?? string.Empty;
            statusText.color = state.StatusIsError
                ? new Color(0.95f, 0.38f, 0.32f)
                : new Color(0.72f, 0.78f, 0.86f);
        }

        public void ShowConfirmation(string message, Action onConfirm)
        {
            pendingConfirmation = onConfirm;
            confirmMessage.text = message ?? string.Empty;
            confirmDialog.SetActive(true);
            confirmButton.Select();
        }

        public void HideConfirmation()
        {
            pendingConfirmation = null;
            confirmDialog.SetActive(false);
        }

        public void ShowSettings()
        {
            audioSettingsPanel?.Open();
        }

        private void Confirm()
        {
            var action = pendingConfirmation;
            HideConfirmation();
            action?.Invoke();
        }

        private static void PlayUiCue(string cueId)
        {
            AudioService.Instance?.PlayCue(cueId);
        }

        public static MainMenuScreenView CreateRuntime(Font preferredFont = null)
        {
            EnsureEventSystem();
            var font = preferredFont ??
                       Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root = new GameObject(
                "PF_MainMenuScreen",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(MainMenuScreenView));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            Stretch(root.GetComponent<RectTransform>());

            var background = CreatePanel(
                root.transform,
                "Background",
                new Color(0.010f, 0.020f, 0.025f, 1f));
            Stretch(background.rectTransform);
            var backdropObject = new GameObject(
                "BackdropArt",
                typeof(RectTransform),
                typeof(PresentationBackdropGraphic));
            backdropObject.transform.SetParent(background.transform, false);
            Stretch(backdropObject.GetComponent<RectTransform>());
            backdropObject.GetComponent<PresentationBackdropGraphic>().Configure(
                PresentationBackdropVariant.MainMenu,
                new Color(0.055f, 0.070f, 0.080f, 1f),
                new Color(0.010f, 0.020f, 0.025f, 1f),
                new Color(0.78f, 0.58f, 0.25f, 1f));

            var card = CreatePanel(
                background.transform,
                "MenuCard",
                new Color(0.055f, 0.054f, 0.066f, 0.97f));
            AddFrame(
                card,
                new Color(0.66f, 0.49f, 0.25f, 0.82f),
                new Vector2(2f, -2f));
            var cardShadow = card.gameObject.AddComponent<Shadow>();
            cardShadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            cardShadow.effectDistance = new Vector2(10f, -12f);
            SetRect(
                card.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(760f, 900f),
                Vector2.zero);
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(64, 64, 40, 40);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            var title = CreateText(
                card.transform,
                "Title",
                "尖塔棋局",
                58,
                104f,
                FontStyle.Bold,
                font);
            title.color = new Color(0.98f, 0.88f, 0.62f, 1f);
            title.verticalOverflow = VerticalWrapMode.Overflow;
            var subtitle = CreateText(
                card.transform,
                "Subtitle",
                "正式单局 · 三层远征",
                26,
                42f,
                FontStyle.Normal,
                font);
            subtitle.color = new Color(0.72f, 0.78f, 0.76f, 1f);
            var summary = CreateText(
                card.transform,
                "ContinueSummary",
                string.Empty,
                24,
                64f,
                FontStyle.Normal,
                font);
            var newGame = CreateButton(
                card.transform,
                "NewGameButton",
                "新游戏",
                font,
                true);
            var continueGame = CreateButton(
                card.transform,
                "ContinueButton",
                "继续游戏",
                font);
            var settings = CreateButton(
                card.transform,
                "SettingsButton",
                "设置",
                font);
            var delete = CreateButton(
                card.transform,
                "DeleteButton",
                "删除单局存档",
                font,
                false,
                true);
            var quit = CreateButton(
                card.transform,
                "QuitButton",
                "退出游戏",
                font);
            var status = CreateText(
                card.transform,
                "Status",
                string.Empty,
                22,
                54f,
                FontStyle.Normal,
                font);

            var overlay = CreatePanel(root.transform, "PF_ConfirmDialog", new Color(0f, 0f, 0f, 0.72f));
            Stretch(overlay.rectTransform);
            var dialog = CreatePanel(
                overlay.transform,
                "DialogCard",
                new Color(0.070f, 0.068f, 0.082f, 1f));
            AddFrame(
                dialog,
                new Color(0.70f, 0.50f, 0.24f, 0.88f),
                new Vector2(2f, -2f));
            SetRect(dialog.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(660f, 440f), Vector2.zero);
            var dialogLayout = dialog.gameObject.AddComponent<VerticalLayoutGroup>();
            dialogLayout.padding = new RectOffset(48, 48, 36, 36);
            dialogLayout.spacing = 18f;
            dialogLayout.childAlignment = TextAnchor.MiddleCenter;
            dialogLayout.childControlWidth = true;
            dialogLayout.childControlHeight = true;
            dialogLayout.childForceExpandHeight = false;
            var message = CreateText(
                dialog.transform,
                "Message",
                string.Empty,
                28,
                130f,
                FontStyle.Bold,
                font);
            var confirm = CreateButton(
                dialog.transform,
                "ConfirmButton",
                "确认",
                font,
                true);
            var cancel = CreateButton(
                dialog.transform,
                "CancelButton",
                "取消",
                font);
            overlay.gameObject.SetActive(false);
            var settingsPanel = AudioSettingsPanelView.Create(
                root.transform,
                font,
                true);

            var view = root.GetComponent<MainMenuScreenView>();
            view.continueSummary = summary;
            view.statusText = status;
            view.newGameButton = newGame;
            view.continueButton = continueGame;
            view.settingsButton = settings;
            view.deleteButton = delete;
            view.quitButton = quit;
            view.confirmDialog = overlay.gameObject;
            view.confirmMessage = message;
            view.confirmButton = confirm;
            view.cancelButton = cancel;
            view.audioSettingsPanel = settingsPanel;
            return view;
        }

        private static Image CreatePanel(Transform parent, string name, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            int size,
            float height,
            FontStyle style,
            Font font = null)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.GetComponent<Text>();
            text.font = font ??
                        Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.96f, 0.93f, 0.84f, 1f);
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            var element = gameObject.GetComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = height;
            return text;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Font font = null,
            bool primary = false,
            bool danger = false)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.color = danger
                ? new Color(0.30f, 0.12f, 0.12f, 1f)
                : primary
                    ? new Color(0.17f, 0.38f, 0.34f, 1f)
                    : new Color(0.14f, 0.20f, 0.21f, 1f);
            AddFrame(
                image,
                primary
                    ? new Color(0.74f, 0.61f, 0.32f, 0.82f)
                    : danger
                        ? new Color(0.72f, 0.28f, 0.24f, 0.76f)
                        : new Color(0.42f, 0.36f, 0.24f, 0.64f),
                new Vector2(1f, -1f));
            var button = gameObject.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1.12f, 1.08f, 0.96f, 1f);
            colors.pressedColor = new Color(0.70f, 0.76f, 0.72f, 1f);
            colors.disabledColor = new Color(0.12f, 0.14f, 0.17f, 0.7f);
            button.colors = colors;
            gameObject.GetComponent<LayoutElement>().preferredHeight = 66f;
            var text = CreateText(
                gameObject.transform,
                "Label",
                label,
                28,
                66f,
                FontStyle.Bold,
                font);
            Stretch(text.rectTransform);
            return button;
        }

        private static void AddFrame(
            Image image,
            Color color,
            Vector2 distance)
        {
            var outline = image.GetComponent<Outline>();
            if (outline == null)
            {
                outline = image.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchor,
            Vector2 size,
            Vector2 position)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
    }
}

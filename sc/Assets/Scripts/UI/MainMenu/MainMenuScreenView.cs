using System;
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

        private MainMenuController controller;
        private Action pendingConfirmation;

        public bool ContinueInteractable => continueButton != null && continueButton.interactable;
        public bool ConfirmationVisible => confirmDialog != null && confirmDialog.activeSelf;
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
            continueButton.onClick.AddListener(controller.ContinueGame);
            settingsButton.onClick.AddListener(controller.OpenSettingsPlaceholder);
            deleteButton.onClick.AddListener(controller.DeleteSave);
            quitButton.onClick.AddListener(controller.QuitGame);
            confirmButton.onClick.AddListener(Confirm);
            cancelButton.onClick.AddListener(HideConfirmation);
        }

        public void Render(MainMenuScreenState state)
        {
            if (state == null)
            {
                return;
            }

            continueButton.interactable = state.ContinueEnabled;
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

        private void Confirm()
        {
            var action = pendingConfirmation;
            HideConfirmation();
            action?.Invoke();
        }

        public static MainMenuScreenView CreateRuntime()
        {
            EnsureEventSystem();
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

            var background = CreatePanel(root.transform, "Background", new Color(0.035f, 0.05f, 0.08f));
            Stretch(background.rectTransform);
            var card = CreatePanel(background.transform, "MenuCard", new Color(0.08f, 0.105f, 0.15f, 0.98f));
            SetRect(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(720f, 900f), Vector2.zero);
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(64, 64, 40, 40);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            CreateText(card.transform, "Title", "尖塔棋局", 58, 80f, FontStyle.Bold);
            CreateText(card.transform, "Subtitle", "正式单局 · 三层远征", 26, 42f, FontStyle.Normal);
            var summary = CreateText(card.transform, "ContinueSummary", string.Empty, 24, 64f, FontStyle.Normal);
            var newGame = CreateButton(card.transform, "NewGameButton", "新游戏");
            var continueGame = CreateButton(card.transform, "ContinueButton", "继续游戏");
            var settings = CreateButton(card.transform, "SettingsButton", "设置（即将开放）");
            var delete = CreateButton(card.transform, "DeleteButton", "删除单局存档");
            var quit = CreateButton(card.transform, "QuitButton", "退出游戏");
            var status = CreateText(card.transform, "Status", string.Empty, 22, 54f, FontStyle.Normal);

            var overlay = CreatePanel(root.transform, "PF_ConfirmDialog", new Color(0f, 0f, 0f, 0.72f));
            Stretch(overlay.rectTransform);
            var dialog = CreatePanel(overlay.transform, "DialogCard", new Color(0.105f, 0.13f, 0.18f, 1f));
            SetRect(dialog.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(660f, 440f), Vector2.zero);
            var dialogLayout = dialog.gameObject.AddComponent<VerticalLayoutGroup>();
            dialogLayout.padding = new RectOffset(48, 48, 36, 36);
            dialogLayout.spacing = 18f;
            dialogLayout.childAlignment = TextAnchor.MiddleCenter;
            dialogLayout.childControlWidth = true;
            dialogLayout.childControlHeight = true;
            dialogLayout.childForceExpandHeight = false;
            var message = CreateText(dialog.transform, "Message", string.Empty, 28, 130f, FontStyle.Bold);
            var confirm = CreateButton(dialog.transform, "ConfirmButton", "确认");
            var cancel = CreateButton(dialog.transform, "CancelButton", "取消");
            overlay.gameObject.SetActive(false);

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
            FontStyle style)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            gameObject.GetComponent<LayoutElement>().preferredHeight = height;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            gameObject.transform.SetParent(parent, false);
            gameObject.GetComponent<Image>().color = new Color(0.16f, 0.24f, 0.34f);
            var button = gameObject.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.22f, 0.34f, 0.48f);
            colors.pressedColor = new Color(0.11f, 0.18f, 0.27f);
            colors.disabledColor = new Color(0.12f, 0.14f, 0.17f, 0.7f);
            button.colors = colors;
            gameObject.GetComponent<LayoutElement>().preferredHeight = 66f;
            var text = CreateText(gameObject.transform, "Label", label, 28, 66f, FontStyle.Bold);
            Stretch(text.rectTransform);
            return button;
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

using System;
using SpireChess.App;
using SpireChess.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.UI.Common
{
    public sealed class RunSystemMenuView : MonoBehaviour
    {
        private Func<bool> canLeave;
        private GameObject overlay;
        private Text status;
        private Button saveAndReturnButton;
        private Button abandonButton;
        private Text abandonLabel;
        private AudioSettingsPanelView audioSettingsPanel;
        private bool confirmingAbandon;

        public bool IsOpen => overlay != null && overlay.activeSelf;
        public bool SettingsOpen =>
            audioSettingsPanel != null && audioSettingsPanel.IsOpen;
        public bool HasAudioSettings =>
            audioSettingsPanel != null &&
            audioSettingsPanel.HasCompleteBindings;

        public static RunSystemMenuView Attach(
            Component screen,
            Func<bool> canLeave = null)
        {
            var canvas = screen == null ? null : screen.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return null;
            }

            var existing = canvas.GetComponentInChildren<RunSystemMenuView>(true);
            if (existing != null)
            {
                existing.canLeave = canLeave ?? (() => true);
                return existing;
            }

            var host = new GameObject("RunSystemMenu", typeof(RectTransform), typeof(RunSystemMenuView));
            host.transform.SetParent(canvas.transform, false);
            var hostRect = host.GetComponent<RectTransform>();
            hostRect.anchorMin = Vector2.zero;
            hostRect.anchorMax = Vector2.one;
            hostRect.offsetMin = Vector2.zero;
            hostRect.offsetMax = Vector2.zero;
            var view = host.GetComponent<RunSystemMenuView>();
            view.canLeave = canLeave ?? (() => true);
            view.Build();
            return view;
        }

        private void Build()
        {
            var font = GetComponentInParent<Canvas>()
                ?.GetComponentInChildren<Text>(true)?.font ??
                Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var openButton = CreateButton(
                transform,
                "MenuButton",
                "菜单",
                new Vector2(150f, 58f),
                font);
            var openRect = openButton.GetComponent<RectTransform>();
            openRect.anchorMin = new Vector2(1f, 1f);
            openRect.anchorMax = new Vector2(1f, 1f);
            openRect.pivot = new Vector2(1f, 1f);
            openRect.anchoredPosition = new Vector2(-24f, -20f);
            openButton.onClick.AddListener(Open);
            openButton.onClick.AddListener(
                () => PlayUiCue(PresentationAudioCueIds.UiClick));

            overlay = new GameObject("SystemMenuOverlay", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(transform, false);
            var overlayRect = overlay.GetComponent<RectTransform>();
            Stretch(overlayRect);
            overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.74f);
            var card = new GameObject(
                "SystemMenuCard",
                typeof(RectTransform),
                typeof(Image),
                typeof(VerticalLayoutGroup));
            card.transform.SetParent(overlay.transform, false);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(600f, 640f);
            var cardImage = card.GetComponent<Image>();
            cardImage.color = new Color(0.060f, 0.058f, 0.072f, 1f);
            AddOutline(
                cardImage,
                new Color(0.68f, 0.50f, 0.25f, 0.84f));
            var layout = card.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 46, 46);
            layout.spacing = 22f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            var title = CreateText(
                card.transform,
                "Title",
                "单局菜单",
                38,
                70f,
                FontStyle.Bold,
                font);
            title.color = new Color(0.98f, 0.86f, 0.58f, 1f);
            status = CreateText(
                card.transform,
                "Status",
                string.Empty,
                22,
                62f,
                FontStyle.Normal,
                font);
            var resume = CreateButton(
                card.transform,
                "ResumeButton",
                "继续游戏",
                new Vector2(0f, 70f),
                font);
            var settings = CreateButton(
                card.transform,
                "AudioSettingsButton",
                "音频设置",
                new Vector2(0f, 70f),
                font);
            saveAndReturnButton = CreateButton(
                card.transform,
                "SaveReturnButton",
                "保存并返回主菜单",
                new Vector2(0f, 70f),
                font);
            abandonButton = CreateButton(
                card.transform,
                "AbandonButton",
                "放弃当前单局",
                new Vector2(0f, 70f),
                font,
                true);
            abandonLabel = abandonButton.GetComponentInChildren<Text>();
            resume.onClick.AddListener(Close);
            resume.onClick.AddListener(
                () => PlayUiCue(PresentationAudioCueIds.UiCancel));
            settings.onClick.AddListener(OpenAudioSettings);
            settings.onClick.AddListener(
                () => PlayUiCue(PresentationAudioCueIds.UiClick));
            saveAndReturnButton.onClick.AddListener(SaveAndReturn);
            abandonButton.onClick.AddListener(Abandon);
            overlay.SetActive(false);
            audioSettingsPanel = AudioSettingsPanelView.Create(
                transform,
                font,
                true);
        }

        private void Open()
        {
            confirmingAbandon = false;
            abandonLabel.text = "放弃当前单局";
            var allowed = canLeave == null || canLeave();
            saveAndReturnButton.interactable = allowed;
            abandonButton.interactable = allowed;
            status.text = allowed
                ? "当前操作均已自动保存"
                : "战斗播放中，暂时不能退出单局";
            overlay.SetActive(true);
        }

        private void Close()
        {
            audioSettingsPanel?.Close();
            overlay.SetActive(false);
        }

        private void OpenAudioSettings()
        {
            audioSettingsPanel?.Open();
        }

        private void SaveAndReturn()
        {
            var succeeded = GameApp.Instance.SaveAndReturnToMainMenu();
            PlayUiCue(ResolveSaveAndReturnCue(succeeded));
            if (succeeded)
            {
                return;
            }

            status.text = "保存失败，请检查存储空间后重试";
        }

        public static string ResolveSaveAndReturnCue(bool succeeded)
        {
            return succeeded
                ? PresentationAudioCueIds.UiConfirm
                : PresentationAudioCueIds.UiError;
        }

        private void Abandon()
        {
            if (!confirmingAbandon)
            {
                PlayUiCue(PresentationAudioCueIds.UiClick);
                confirmingAbandon = true;
                abandonLabel.text = "再次点击确认放弃";
                status.text = "放弃后将删除本地单局存档";
                return;
            }

            PlayUiCue(PresentationAudioCueIds.UiConfirm);
            GameApp.Instance.AbandonRun();
            GameApp.Instance.Router.GoToMainMenu();
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 size,
            Font font = null,
            bool danger = false)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            gameObject.transform.SetParent(parent, false);
            gameObject.GetComponent<RectTransform>().sizeDelta = size;
            var layout = gameObject.GetComponent<LayoutElement>();
            layout.preferredHeight = size.y;
            if (size.x > 0f) layout.preferredWidth = size.x;
            var image = gameObject.GetComponent<Image>();
            image.color = danger
                ? new Color(0.30f, 0.12f, 0.12f, 0.98f)
                : new Color(0.14f, 0.20f, 0.21f, 0.98f);
            AddOutline(
                image,
                danger
                    ? new Color(0.72f, 0.28f, 0.24f, 0.76f)
                    : new Color(0.46f, 0.38f, 0.22f, 0.66f));
            var button = gameObject.GetComponent<Button>();
            var text = CreateText(
                gameObject.transform,
                "Label",
                label,
                26,
                size.y,
                FontStyle.Bold,
                font);
            Stretch(text.rectTransform);
            return button;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            float height,
            FontStyle fontStyle,
            Font font = null)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.GetComponent<Text>();
            text.font = font ??
                        Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.96f, 0.92f, 0.82f, 1f);
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            gameObject.GetComponent<LayoutElement>().preferredHeight = height;
            return text;
        }

        private static void AddOutline(Image image, Color color)
        {
            var outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
        }

        private static void PlayUiCue(string cueId)
        {
            AudioService.Instance?.PlayCue(cueId);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}

using System;
using SpireChess.App;
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
        private bool confirmingAbandon;

        public bool IsOpen => overlay != null && overlay.activeSelf;

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
            var openButton = CreateButton(transform, "MenuButton", "菜单", new Vector2(150f, 58f));
            var openRect = openButton.GetComponent<RectTransform>();
            openRect.anchorMin = new Vector2(1f, 1f);
            openRect.anchorMax = new Vector2(1f, 1f);
            openRect.pivot = new Vector2(1f, 1f);
            openRect.anchoredPosition = new Vector2(-24f, -20f);
            openButton.onClick.AddListener(Open);

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
            cardRect.sizeDelta = new Vector2(560f, 500f);
            card.GetComponent<Image>().color = new Color(0.08f, 0.105f, 0.15f, 1f);
            var layout = card.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 46, 46);
            layout.spacing = 22f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            CreateText(card.transform, "Title", "单局菜单", 38, 70f, FontStyle.Bold);
            status = CreateText(card.transform, "Status", string.Empty, 22, 62f, FontStyle.Normal);
            var resume = CreateButton(card.transform, "ResumeButton", "继续游戏", new Vector2(0f, 70f));
            saveAndReturnButton = CreateButton(
                card.transform,
                "SaveReturnButton",
                "保存并返回主菜单",
                new Vector2(0f, 70f));
            abandonButton = CreateButton(
                card.transform,
                "AbandonButton",
                "放弃当前单局",
                new Vector2(0f, 70f));
            abandonLabel = abandonButton.GetComponentInChildren<Text>();
            resume.onClick.AddListener(Close);
            saveAndReturnButton.onClick.AddListener(SaveAndReturn);
            abandonButton.onClick.AddListener(Abandon);
            overlay.SetActive(false);
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
            overlay.SetActive(false);
        }

        private void SaveAndReturn()
        {
            if (GameApp.Instance.SaveAndReturnToMainMenu())
            {
                return;
            }

            status.text = "保存失败，请检查存储空间后重试";
        }

        private void Abandon()
        {
            if (!confirmingAbandon)
            {
                confirmingAbandon = true;
                abandonLabel.text = "再次点击确认放弃";
                status.text = "放弃后将删除本地单局存档";
                return;
            }

            GameApp.Instance.AbandonRun();
            GameApp.Instance.Router.GoToMainMenu();
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 size)
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
            gameObject.GetComponent<Image>().color = new Color(0.16f, 0.24f, 0.34f, 0.98f);
            var button = gameObject.GetComponent<Button>();
            var text = CreateText(gameObject.transform, "Label", label, 26, size.y, FontStyle.Bold);
            Stretch(text.rectTransform);
            return button;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            float height,
            FontStyle fontStyle)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            gameObject.GetComponent<LayoutElement>().preferredHeight = height;
            return text;
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

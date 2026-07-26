using SpireChess.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.UI.Common
{
    [DisallowMultipleComponent]
    public sealed class AudioSettingsPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider uiSlider;
        [SerializeField] private Text masterValueText;
        [SerializeField] private Text musicValueText;
        [SerializeField] private Text sfxValueText;
        [SerializeField] private Text uiValueText;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button closeButton;

        private bool listenersBound;
        private bool updatingValues;
        private bool hasUnsavedChanges;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public bool HasCompleteBindings =>
            panelRoot != null &&
            masterSlider != null && musicSlider != null &&
            sfxSlider != null && uiSlider != null &&
            masterValueText != null && musicValueText != null &&
            sfxValueText != null && uiValueText != null &&
            resetButton != null && closeButton != null;

        private void Awake()
        {
            BindListeners();
        }

        private void OnEnable()
        {
            BindListeners();
        }

        public void Open()
        {
            if (!HasCompleteBindings)
            {
                return;
            }

            BindListeners();
            RefreshFromSettings();
            panelRoot.SetActive(true);
            closeButton.Select();
        }

        public void Close()
        {
            CommitPendingChanges();
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public void RefreshFromSettings()
        {
            var settings = Application.isPlaying
                ? AudioService.EnsurePresent().Settings ??
                  PresentationAudioSettings.Load()
                : PresentationAudioSettings.Load();
            updatingValues = true;
            masterSlider.SetValueWithoutNotify(settings.Master);
            musicSlider.SetValueWithoutNotify(settings.Music);
            sfxSlider.SetValueWithoutNotify(settings.Sfx);
            uiSlider.SetValueWithoutNotify(settings.Ui);
            updatingValues = false;
            UpdateValueLabels();
        }

        public static AudioSettingsPanelView Create(
            Transform parent,
            Font font,
            bool startHidden = true)
        {
            font = font ??
                   Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root = new GameObject(
                "AudioSettingsPanel",
                typeof(RectTransform),
                typeof(Image),
                typeof(AudioSettingsPanelView));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>());
            var blocker = root.GetComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.80f);
            blocker.raycastTarget = true;

            var card = new GameObject(
                "SettingsCard",
                typeof(RectTransform),
                typeof(Image),
                typeof(VerticalLayoutGroup));
            card.transform.SetParent(root.transform, false);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(760f, 700f);
            var cardImage = card.GetComponent<Image>();
            cardImage.color = new Color(0.060f, 0.058f, 0.072f, 1f);
            AddOutline(
                cardImage,
                new Color(0.70f, 0.52f, 0.25f, 0.88f),
                new Vector2(2f, -2f));
            var layout = card.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(58, 58, 40, 40);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var title = CreateText(
                card.transform,
                "Title",
                "音频设置",
                font,
                38,
                76f,
                FontStyle.Bold);
            title.color = new Color(0.98f, 0.86f, 0.58f, 1f);
            var hint = CreateText(
                card.transform,
                "Hint",
                "设置保存在本机，与单局存档相互独立",
                font,
                20,
                48f,
                FontStyle.Normal);
            hint.color = new Color(0.68f, 0.74f, 0.74f, 1f);

            var master = CreateVolumeRow(
                card.transform,
                "Master",
                "总音量",
                font);
            var music = CreateVolumeRow(
                card.transform,
                "Music",
                "音乐",
                font);
            var sfx = CreateVolumeRow(
                card.transform,
                "SFX",
                "音效",
                font);
            var ui = CreateVolumeRow(
                card.transform,
                "UI",
                "界面",
                font);

            var actions = new GameObject(
                "Actions",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            actions.transform.SetParent(card.transform, false);
            var actionsElement = actions.GetComponent<LayoutElement>();
            actionsElement.minHeight = actionsElement.preferredHeight = 72f;
            var actionLayout = actions.GetComponent<HorizontalLayoutGroup>();
            actionLayout.spacing = 20f;
            actionLayout.childAlignment = TextAnchor.MiddleCenter;
            actionLayout.childControlWidth = false;
            actionLayout.childControlHeight = false;
            actionLayout.childForceExpandWidth = false;
            actionLayout.childForceExpandHeight = false;
            var reset = CreateButton(
                actions.transform,
                "ResetButton",
                "恢复默认",
                font,
                new Color(0.16f, 0.20f, 0.22f, 1f));
            var close = CreateButton(
                actions.transform,
                "CloseButton",
                "保存并返回",
                font,
                new Color(0.17f, 0.38f, 0.34f, 1f));

            var view = root.GetComponent<AudioSettingsPanelView>();
            view.panelRoot = root;
            view.masterSlider = master.Slider;
            view.musicSlider = music.Slider;
            view.sfxSlider = sfx.Slider;
            view.uiSlider = ui.Slider;
            view.masterValueText = master.Value;
            view.musicValueText = music.Value;
            view.sfxValueText = sfx.Value;
            view.uiValueText = ui.Value;
            view.resetButton = reset;
            view.closeButton = close;
            view.BindListeners();
            view.RefreshFromSettings();
            root.SetActive(!startHidden);
            return view;
        }

        private void BindListeners()
        {
            if (listenersBound || !HasCompleteBindings)
            {
                return;
            }

            masterSlider.onValueChanged.AddListener(OnMasterChanged);
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            uiSlider.onValueChanged.AddListener(OnUiChanged);
            resetButton.onClick.AddListener(ResetDefaults);
            closeButton.onClick.AddListener(OnCloseClicked);
            listenersBound = true;
        }

        private void OnMasterChanged(float value)
        {
            if (updatingValues)
            {
                return;
            }
            AudioService.EnsurePresent().SetMasterVolume(value, false);
            hasUnsavedChanges = true;
            UpdateValueLabels();
        }

        private void OnMusicChanged(float value)
        {
            if (updatingValues)
            {
                return;
            }
            AudioService.EnsurePresent().SetBusVolume(
                PresentationAudioBus.Music,
                value,
                false);
            hasUnsavedChanges = true;
            UpdateValueLabels();
        }

        private void OnSfxChanged(float value)
        {
            if (updatingValues)
            {
                return;
            }
            AudioService.EnsurePresent().SetBusVolume(
                PresentationAudioBus.Sfx,
                value,
                false);
            hasUnsavedChanges = true;
            UpdateValueLabels();
        }

        private void OnUiChanged(float value)
        {
            if (updatingValues)
            {
                return;
            }
            AudioService.EnsurePresent().SetBusVolume(
                PresentationAudioBus.Ui,
                value,
                false);
            hasUnsavedChanges = true;
            UpdateValueLabels();
        }

        private void ResetDefaults()
        {
            var service = AudioService.EnsurePresent();
            service.SetMasterVolume(
                PresentationAudioSettings.DefaultLinearVolume,
                false);
            service.SetBusVolume(
                PresentationAudioBus.Music,
                PresentationAudioSettings.DefaultLinearVolume,
                false);
            service.SetBusVolume(
                PresentationAudioBus.Sfx,
                PresentationAudioSettings.DefaultLinearVolume,
                false);
            service.SetBusVolume(
                PresentationAudioBus.Ui,
                PresentationAudioSettings.DefaultLinearVolume,
                false);
            service.SaveSettings();
            hasUnsavedChanges = false;
            service.PlayCue(PresentationAudioCueIds.UiConfirm);
            RefreshFromSettings();
        }

        private void OnCloseClicked()
        {
            AudioService.Instance?.PlayCue(PresentationAudioCueIds.UiCancel);
            Close();
        }

        private void OnDisable()
        {
            CommitPendingChanges();
        }

        private void CommitPendingChanges()
        {
            if (!hasUnsavedChanges)
            {
                return;
            }

            AudioService.Instance?.SaveSettings();
            hasUnsavedChanges = false;
        }

        private void UpdateValueLabels()
        {
            if (!HasCompleteBindings)
            {
                return;
            }

            masterValueText.text = ToPercent(masterSlider.value);
            musicValueText.text = ToPercent(musicSlider.value);
            sfxValueText.text = ToPercent(sfxSlider.value);
            uiValueText.text = ToPercent(uiSlider.value);
        }

        private static string ToPercent(float value)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
        }

        private static VolumeRow CreateVolumeRow(
            Transform parent,
            string name,
            string label,
            Font font)
        {
            var root = new GameObject(
                name + "Row",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            root.transform.SetParent(parent, false);
            var rootElement = root.GetComponent<LayoutElement>();
            rootElement.minHeight = rootElement.preferredHeight = 72f;
            var layout = root.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var labelText = CreateText(
                root.transform,
                "Label",
                label,
                font,
                24,
                56f,
                FontStyle.Bold);
            labelText.alignment = TextAnchor.MiddleLeft;
            var labelElement = labelText.GetComponent<LayoutElement>();
            labelElement.preferredWidth = labelElement.minWidth = 130f;

            var slider = CreateSlider(root.transform, name + "Slider");
            var valueText = CreateText(
                root.transform,
                "Value",
                "100%",
                font,
                22,
                56f,
                FontStyle.Normal);
            var valueElement = valueText.GetComponent<LayoutElement>();
            valueElement.preferredWidth = valueElement.minWidth = 76f;
            return new VolumeRow(slider, valueText);
        }

        private static Slider CreateSlider(Transform parent, string name)
        {
            var root = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Slider),
                typeof(LayoutElement));
            root.transform.SetParent(parent, false);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(370f, 44f);
            var element = root.GetComponent<LayoutElement>();
            element.preferredWidth = element.minWidth = 370f;
            element.preferredHeight = element.minHeight = 44f;
            var background = root.GetComponent<Image>();
            background.color = new Color(0.10f, 0.13f, 0.14f, 1f);
            AddOutline(
                background,
                new Color(0.40f, 0.34f, 0.22f, 0.66f),
                new Vector2(1f, -1f));

            var fillArea = new GameObject(
                "FillArea",
                typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRect.offsetMin = new Vector2(12f, -8f);
            fillAreaRect.offsetMax = new Vector2(-12f, 8f);
            var fill = new GameObject(
                "Fill",
                typeof(RectTransform),
                typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fill.GetComponent<Image>();
            fillImage.color = new Color(0.30f, 0.72f, 0.62f, 1f);
            fillImage.raycastTarget = false;

            var handleArea = new GameObject(
                "HandleSlideArea",
                typeof(RectTransform));
            handleArea.transform.SetParent(root.transform, false);
            var handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(12f, 0f);
            handleAreaRect.offsetMax = new Vector2(-12f, 0f);
            var handle = new GameObject(
                "Handle",
                typeof(RectTransform),
                typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(28f, 36f);
            var handleImage = handle.GetComponent<Image>();
            handleImage.color = new Color(0.96f, 0.84f, 0.54f, 1f);

            var slider = root.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.value = 1f;
            return slider;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Font font,
            Color color)
        {
            var root = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(260f, 62f);
            var element = root.GetComponent<LayoutElement>();
            element.preferredWidth = element.minWidth = 260f;
            element.preferredHeight = element.minHeight = 62f;
            var image = root.GetComponent<Image>();
            image.color = color;
            AddOutline(
                image,
                new Color(0.60f, 0.48f, 0.27f, 0.72f),
                new Vector2(1f, -1f));
            var button = root.GetComponent<Button>();
            button.targetGraphic = image;
            var labelText = CreateText(
                root.transform,
                "Label",
                label,
                font,
                24,
                62f,
                FontStyle.Bold);
            Stretch(labelText.rectTransform);
            return button;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            Font font,
            int fontSize,
            float height,
            FontStyle fontStyle)
        {
            var root = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Text),
                typeof(LayoutElement));
            root.transform.SetParent(parent, false);
            var text = root.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.96f, 0.92f, 0.82f, 1f);
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            var element = root.GetComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = height;
            return text;
        }

        private static void AddOutline(
            Image image,
            Color color,
            Vector2 distance)
        {
            var outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private readonly struct VolumeRow
        {
            public VolumeRow(Slider slider, Text value)
            {
                Slider = slider;
                Value = value;
            }

            public Slider Slider { get; }
            public Text Value { get; }
        }
    }
}

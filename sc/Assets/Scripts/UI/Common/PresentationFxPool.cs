using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.UI
{
    public enum PresentationFxEmphasis
    {
        Subtle,
        Normal,
        Strong,
        Critical
    }

    [DisallowMultipleComponent]
    public sealed class PresentationFxPool : MonoBehaviour
    {
        private const int MinimumCapacity = 1;
        private const int MaximumCapacity = 32;

        [SerializeField] private Font font;
        [SerializeField, Range(MinimumCapacity, MaximumCapacity)]
        private int capacity = 12;

        private readonly List<Entry> entries = new List<Entry>();
        private int nextEntry;

        public int Capacity => Mathf.Clamp(
            capacity,
            MinimumCapacity,
            MaximumCapacity);
        public int ActiveCount
        {
            get
            {
                var count = 0;
                foreach (var entry in entries)
                {
                    if (entry.Active)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
        public int TotalPlayCount { get; private set; }
        public bool LastPlayUsedBackdrop { get; private set; } = true;

        public void Configure(Font value, int desiredCapacity = 12)
        {
            font = value;
            capacity = Mathf.Clamp(
                desiredCapacity,
                MinimumCapacity,
                MaximumCapacity);
            if (entries.Count > capacity)
            {
                ClearAndDestroyEntries();
            }
        }

        public void Play(
            string label,
            Color color,
            Vector2 anchoredPosition,
            PresentationFxEmphasis emphasis = PresentationFxEmphasis.Normal,
            float duration = 0.62f,
            float verticalTravel = 74f,
            bool showBackdrop = true)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                return;
            }

            EnsureEntries();
            var entry = FindAvailableEntry();
            entry.Active = true;
            entry.Elapsed = 0f;
            entry.Duration = Mathf.Max(0.08f, duration);
            entry.Origin = anchoredPosition;
            entry.VerticalTravel = verticalTravel;
            entry.StartScale = GetStartScale(emphasis);
            entry.PeakScale = GetPeakScale(emphasis);
            entry.Group.alpha = 0f;
            entry.Group.interactable = false;
            entry.Group.blocksRaycasts = false;
            entry.Text.text = label;
            entry.Text.color = Color.white;
            entry.Text.fontSize = GetFontSize(emphasis);
            LastPlayUsedBackdrop = showBackdrop;
            entry.Backdrop.color = new Color(
                color.r,
                color.g,
                color.b,
                showBackdrop ? GetBackdropAlpha(emphasis) : 0f);
            entry.Rect.anchoredPosition = anchoredPosition;
            entry.Rect.localScale = Vector3.one * entry.StartScale;
            entry.GameObject.SetActive(true);
            TotalPlayCount++;
        }

        public void Advance(float unscaledDeltaTime)
        {
            if (unscaledDeltaTime <= 0f)
            {
                return;
            }

            foreach (var entry in entries)
            {
                if (!entry.Active)
                {
                    continue;
                }

                entry.Elapsed += unscaledDeltaTime;
                var progress = Mathf.Clamp01(entry.Elapsed / entry.Duration);
                var eased = Smooth(progress);
                entry.Group.alpha = EvaluateOpacity(progress);
                entry.Rect.anchoredPosition =
                    entry.Origin + Vector2.up * entry.VerticalTravel * eased;
                var scale = progress < 0.34f
                    ? Mathf.Lerp(
                        entry.StartScale,
                        entry.PeakScale,
                        Smooth(progress / 0.34f))
                    : Mathf.Lerp(
                        entry.PeakScale,
                        1f,
                        Smooth((progress - 0.34f) / 0.66f));
                entry.Rect.localScale = Vector3.one * scale;
                if (progress >= 1f)
                {
                    Deactivate(entry);
                }
            }
        }

        public void ClearImmediate()
        {
            foreach (var entry in entries)
            {
                Deactivate(entry);
            }
        }

        public static float EvaluateOpacity(float progress)
        {
            progress = Mathf.Clamp01(progress);
            if (progress < 0.12f)
            {
                return Smooth(progress / 0.12f);
            }

            if (progress < 0.68f)
            {
                return 1f;
            }

            return 1f - Smooth((progress - 0.68f) / 0.32f);
        }

        private void Update()
        {
            Advance(Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            ClearImmediate();
        }

        private void OnDestroy()
        {
            entries.Clear();
        }

        private void EnsureEntries()
        {
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            while (entries.Count < Capacity)
            {
                entries.Add(CreateEntry(entries.Count));
            }
        }

        private Entry FindAvailableEntry()
        {
            for (var offset = 0; offset < entries.Count; offset++)
            {
                var index = (nextEntry + offset) % entries.Count;
                if (entries[index].Active)
                {
                    continue;
                }

                nextEntry = (index + 1) % entries.Count;
                return entries[index];
            }

            var recycled = entries[nextEntry];
            nextEntry = (nextEntry + 1) % entries.Count;
            Deactivate(recycled);
            return recycled;
        }

        private Entry CreateEntry(int index)
        {
            var root = new GameObject(
                "Fx_" + index,
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image));
            root.transform.SetParent(transform, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(320f, 64f);
            var group = root.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            var backdrop = root.GetComponent<Image>();
            backdrop.raycastTarget = false;

            var labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(Text));
            labelObject.transform.SetParent(root.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(14f, 5f);
            labelRect.offsetMax = new Vector2(-14f, -5f);
            var text = labelObject.GetComponent<Text>();
            text.font = font;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;

            root.SetActive(false);
            return new Entry(root, rect, group, backdrop, text);
        }

        private void ClearAndDestroyEntries()
        {
            foreach (var entry in entries)
            {
                if (entry.GameObject == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(entry.GameObject);
                }
                else
                {
                    DestroyImmediate(entry.GameObject);
                }
            }

            entries.Clear();
            nextEntry = 0;
        }

        private static void Deactivate(Entry entry)
        {
            if (entry == null || entry.GameObject == null)
            {
                return;
            }

            entry.Active = false;
            entry.Elapsed = 0f;
            entry.Group.alpha = 0f;
            entry.Rect.localScale = Vector3.one;
            entry.GameObject.SetActive(false);
        }

        private static int GetFontSize(PresentationFxEmphasis emphasis)
        {
            switch (emphasis)
            {
                case PresentationFxEmphasis.Subtle: return 19;
                case PresentationFxEmphasis.Strong: return 27;
                case PresentationFxEmphasis.Critical: return 32;
                default: return 23;
            }
        }

        private static float GetStartScale(PresentationFxEmphasis emphasis)
        {
            return emphasis == PresentationFxEmphasis.Subtle ? 0.92f : 0.78f;
        }

        private static float GetPeakScale(PresentationFxEmphasis emphasis)
        {
            switch (emphasis)
            {
                case PresentationFxEmphasis.Subtle: return 1.02f;
                case PresentationFxEmphasis.Strong: return 1.14f;
                case PresentationFxEmphasis.Critical: return 1.24f;
                default: return 1.08f;
            }
        }

        private static float GetBackdropAlpha(
            PresentationFxEmphasis emphasis)
        {
            switch (emphasis)
            {
                case PresentationFxEmphasis.Subtle: return 0.68f;
                case PresentationFxEmphasis.Strong: return 0.90f;
                case PresentationFxEmphasis.Critical: return 0.96f;
                default: return 0.82f;
            }
        }

        private static float Smooth(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private sealed class Entry
        {
            public Entry(
                GameObject gameObject,
                RectTransform rect,
                CanvasGroup group,
                Image backdrop,
                Text text)
            {
                GameObject = gameObject;
                Rect = rect;
                Group = group;
                Backdrop = backdrop;
                Text = text;
            }

            public GameObject GameObject { get; }
            public RectTransform Rect { get; }
            public CanvasGroup Group { get; }
            public Image Backdrop { get; }
            public Text Text { get; }
            public bool Active { get; set; }
            public float Elapsed { get; set; }
            public float Duration { get; set; }
            public Vector2 Origin { get; set; }
            public float VerticalTravel { get; set; }
            public float StartScale { get; set; }
            public float PeakScale { get; set; }
        }
    }
}

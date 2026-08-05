using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class BattleImpactFxLayer : MonoBehaviour
    {
        private const int MinimumCapacity = 8;
        public const int FixedPoolCapacity = 32;
        public const int MaximumElementsPerEvent = 12;
        private const int MaximumCapacity = FixedPoolCapacity;

        [SerializeField] private Sprite sprite;
        [SerializeField, Range(MinimumCapacity, MaximumCapacity)]
        private int capacity = 32;

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
        public string LastEffectId { get; private set; } = string.Empty;

        public void Configure(Sprite value, int desiredCapacity = 32)
        {
            sprite = value;
            capacity = Mathf.Clamp(
                desiredCapacity,
                MinimumCapacity,
                MaximumCapacity);
            BindExistingEntries();
            EnsureEntries();
        }

        public void PlayAttackTrail(
            Vector2 source,
            Vector2 target,
            Color color,
            float durationScale = 1f)
        {
            LastEffectId = "attack_trail";
            var scaledDuration = ResolveDurationScale(durationScale);
            var delta = target - source;
            var distance = Mathf.Max(72f, delta.magnitude);
            var direction = delta.sqrMagnitude <= 0.001f
                ? Vector2.right
                : delta.normalized;
            var perpendicular = new Vector2(-direction.y, direction.x);
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            var center = Vector2.Lerp(source, target, 0.58f);
            for (var index = 0; index < 3; index++)
            {
                var offset = (index - 1) * 11f;
                var alpha = 0.92f - index * 0.18f;
                PlayElement(
                    center + perpendicular * offset - direction * (index * 7f),
                    center + perpendicular * offset + direction * (26f + index * 8f),
                    new Vector2(distance * (0.62f - index * 0.06f), 8f - index * 2f),
                    color,
                    alpha,
                    (0.14f + index * 0.02f) * scaledDuration,
                    angle,
                    angle,
                    new Vector2(0.38f, 0.72f),
                    new Vector2(1.06f, 0.20f));
            }
        }

        public void PlayImpact(
            Vector2 position,
            Color color,
            PresentationFxEmphasis emphasis = PresentationFxEmphasis.Normal,
            float durationScale = 1f)
        {
            LastEffectId = "impact";
            var scaledDuration = ResolveDurationScale(durationScale);
            var rayCount = emphasis == PresentationFxEmphasis.Critical
                ? MaximumElementsPerEvent - 1
                : emphasis == PresentationFxEmphasis.Strong ? 8 : 6;
            var radius = emphasis == PresentationFxEmphasis.Critical
                ? 82f
                : emphasis == PresentationFxEmphasis.Strong ? 64f : 46f;
            var duration = emphasis == PresentationFxEmphasis.Critical
                ? 0.24f
                : emphasis == PresentationFxEmphasis.Strong ? 0.20f : 0.16f;

            PlayElement(
                position,
                position,
                new Vector2(radius * 0.72f, radius * 0.72f),
                Color.Lerp(color, Color.white, 0.62f),
                0.86f,
                duration * 0.72f * scaledDuration,
                45f,
                62f,
                Vector2.one * 0.24f,
                Vector2.one * 1.18f);

            for (var index = 0; index < rayCount; index++)
            {
                var angle = 360f * index / rayCount + (index % 2) * 7f;
                var radians = angle * Mathf.Deg2Rad;
                var direction = new Vector2(
                    Mathf.Cos(radians),
                    Mathf.Sin(radians));
                PlayElement(
                    position + direction * 8f,
                    position + direction * radius,
                    new Vector2(radius * 0.46f, emphasis ==
                        PresentationFxEmphasis.Normal ? 4f : 6f),
                    color,
                    0.96f,
                    duration * scaledDuration,
                    angle,
                    angle,
                    new Vector2(0.30f, 1f),
                    new Vector2(1f, 0.18f));
            }
        }

        public void PlayDeath(
            Vector2 position,
            Color color,
            bool token,
            float durationScale = 1f)
        {
            LastEffectId = token ? "token_death" : "death";
            var scaledDuration = ResolveDurationScale(durationScale);
            var count = token ? 5 : 8;
            var duration = token ? 0.20f : 0.30f;
            for (var index = 0; index < count; index++)
            {
                var angle = 360f * index / count + 18f;
                var radians = angle * Mathf.Deg2Rad;
                var direction = new Vector2(
                    Mathf.Cos(radians),
                    Mathf.Sin(radians));
                var travel = token ? 42f : 66f + (index % 3) * 8f;
                var size = token ? 9f : 12f + (index % 2) * 5f;
                PlayElement(
                    position + direction * 6f,
                    position + direction * travel + Vector2.up * 18f,
                    new Vector2(size, size * 0.72f),
                    color,
                    token ? 0.72f : 0.90f,
                    (duration + index * 0.012f) * scaledDuration,
                    angle,
                    angle + (index % 2 == 0 ? 105f : -105f),
                    Vector2.one,
                    Vector2.one * (token ? 0.18f : 0.42f));
            }
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
                entry.Rect.anchoredPosition = Vector2.Lerp(
                    entry.StartPosition,
                    entry.EndPosition,
                    eased);
                var scale = Vector2.Lerp(
                    entry.StartScale,
                    entry.EndScale,
                    eased);
                entry.Rect.localScale = new Vector3(scale.x, scale.y, 1f);
                entry.Rect.localEulerAngles = new Vector3(
                    0f,
                    0f,
                    Mathf.Lerp(
                        entry.StartRotation,
                        entry.EndRotation,
                        eased));
                var color = entry.Color;
                color.a = entry.PeakAlpha * EvaluateOpacity(progress);
                entry.Image.color = color;
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

        private void Awake()
        {
            BindExistingEntries();
            EnsureEntries();
        }

        private void Update()
        {
            Advance(Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            ClearImmediate();
        }

        private void BindExistingEntries()
        {
            if (entries.Count > 0)
            {
                return;
            }

            for (var index = 0; index < Capacity; index++)
            {
                var child = transform.Find($"ImpactFx_{index:00}");
                var image = child == null ? null : child.GetComponent<Image>();
                if (image == null)
                {
                    break;
                }

                entries.Add(new Entry(
                    child.gameObject,
                    (RectTransform)child,
                    image));
            }
        }

        private void EnsureEntries()
        {
            BindExistingEntries();
            if (sprite == null)
            {
                sprite = Resources.GetBuiltinResource<Sprite>(
                    "UI/Skin/UISprite.psd");
            }

            while (entries.Count < Capacity)
            {
                entries.Add(CreateEntry(entries.Count));
            }
        }

        private Entry CreateEntry(int index)
        {
            var root = new GameObject(
                $"ImpactFx_{index:00}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            root.transform.SetParent(transform, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            var image = root.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            root.SetActive(false);
            return new Entry(root, rect, image);
        }

        private void PlayElement(
            Vector2 startPosition,
            Vector2 endPosition,
            Vector2 size,
            Color color,
            float peakAlpha,
            float duration,
            float startRotation,
            float endRotation,
            Vector2 startScale,
            Vector2 endScale)
        {
            EnsureEntries();
            var entry = FindAvailableEntry();
            entry.Active = true;
            entry.Elapsed = 0f;
            entry.Duration = Mathf.Max(0.06f, duration);
            entry.StartPosition = startPosition;
            entry.EndPosition = endPosition;
            entry.StartScale = startScale;
            entry.EndScale = endScale;
            entry.StartRotation = startRotation;
            entry.EndRotation = endRotation;
            entry.Color = new Color(color.r, color.g, color.b, 1f);
            entry.PeakAlpha = Mathf.Clamp01(peakAlpha);
            entry.Rect.sizeDelta = size;
            entry.Rect.anchoredPosition = startPosition;
            entry.Rect.localScale = new Vector3(startScale.x, startScale.y, 1f);
            entry.Rect.localEulerAngles = new Vector3(0f, 0f, startRotation);
            var initialColor = entry.Color;
            initialColor.a = 0f;
            entry.Image.color = initialColor;
            entry.GameObject.SetActive(true);
            entry.Rect.SetAsLastSibling();
            TotalPlayCount++;
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

        private static void Deactivate(Entry entry)
        {
            if (entry == null || entry.GameObject == null)
            {
                return;
            }

            entry.Active = false;
            entry.Elapsed = 0f;
            entry.Rect.localScale = Vector3.one;
            entry.Rect.localEulerAngles = Vector3.zero;
            entry.Image.color = Color.clear;
            entry.GameObject.SetActive(false);
        }

        private static float EvaluateOpacity(float progress)
        {
            progress = Mathf.Clamp01(progress);
            if (progress < 0.14f)
            {
                return Smooth(progress / 0.14f);
            }

            return 1f - Smooth((progress - 0.14f) / 0.86f);
        }

        private static float Smooth(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static float ResolveDurationScale(float value)
        {
            return Mathf.Clamp(value, 0.1f, 1f);
        }

        private sealed class Entry
        {
            public Entry(
                GameObject gameObject,
                RectTransform rect,
                Image image)
            {
                GameObject = gameObject;
                Rect = rect;
                Image = image;
            }

            public GameObject GameObject { get; }
            public RectTransform Rect { get; }
            public Image Image { get; }
            public bool Active { get; set; }
            public float Elapsed { get; set; }
            public float Duration { get; set; }
            public Vector2 StartPosition { get; set; }
            public Vector2 EndPosition { get; set; }
            public Vector2 StartScale { get; set; }
            public Vector2 EndScale { get; set; }
            public float StartRotation { get; set; }
            public float EndRotation { get; set; }
            public Color Color { get; set; }
            public float PeakAlpha { get; set; }
        }
    }
}

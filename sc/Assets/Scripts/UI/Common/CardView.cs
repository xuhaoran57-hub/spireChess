using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.UI
{
    [DisallowMultipleComponent]
    public sealed class CardView : MonoBehaviour
    {
        private static readonly Color NormalFrameColor =
            new Color(0.72f, 0.78f, 0.84f, 0.28f);
        private static readonly Color GoldenFrameColor =
            new Color(1f, 0.78f, 0.20f, 0.42f);
        private static readonly Color SelectionColor =
            new Color(0.35f, 0.85f, 1f, 0.34f);
        private static readonly Color LegalTargetColor =
            new Color(0.42f, 1f, 0.56f, 0.30f);
        private static readonly Color GrowthColor =
            new Color32(0x62, 0xE6, 0xA6, 0xFF);
        private static readonly Color UnaffordableColor =
            new Color32(0xFF, 0x7B, 0x7B, 0xFF);
        private static readonly Color DeclineColor =
            new Color32(0xFF, 0x72, 0x72, 0xFF);
        private static readonly Color NormalTextColor =
            new Color(0.95f, 0.96f, 0.98f, 1f);

        [Header("Root")]
        [SerializeField] private RectTransform rootRect;
        [SerializeField] private Image rootImage;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Card layers")]
        [SerializeField] private Image background;
        [SerializeField] private Image raceSkin;
        [SerializeField] private RectTransform artworkMask;
        [SerializeField] private Mask artworkMaskComponent;
        [SerializeField] private Image artwork;
        [SerializeField] private Image normalFrame;
        [SerializeField] private Image goldenFrame;

        [Header("Header")]
        [SerializeField] private Image costBadge;
        [SerializeField] private Text costText;
        [SerializeField] private Image tierBadge;
        [SerializeField] private Text tierText;
        [SerializeField] private Image namePlate;
        [SerializeField] private Text nameText;

        [Header("Information")]
        [SerializeField] private Image infoPanel;
        [SerializeField] private Text raceOrSpellTypeText;
        [SerializeField] private RectTransform abilityLabelRow;
        [SerializeField] private Text[] abilityLabelTexts = Array.Empty<Text>();
        [SerializeField] private Text descriptionText;
        [SerializeField] private RectTransform progressRoot;
        [SerializeField] private Image progressFill;
        [SerializeField] private Text progressText;

        [Header("States")]
        [SerializeField] private RectTransform stateBadgeRow;
        [SerializeField] private Text shieldBadge;
        [SerializeField] private Text nextCombatShieldBadge;
        [SerializeField] private Text temporaryBadge;
        [SerializeField] private Image attackBadge;
        [SerializeField] private Text attackText;
        [SerializeField] private Image healthBadge;
        [SerializeField] private Text healthText;
        [SerializeField] private Text spellFooter;

        [Header("Overlays")]
        [SerializeField] private RectTransform growthFeedbackRoot;
        [SerializeField] private CanvasGroup growthFeedbackCanvasGroup;
        [SerializeField] private Text growthFeedbackText;
        [SerializeField] private Image selectionFrame;
        [SerializeField] private Image legalTargetFrame;
        [SerializeField] private Image disabledMask;
        [SerializeField] private Text disabledIcon;
        [SerializeField] private Text disabledReasonText;

        public CardDisplayMode CurrentDisplayMode { get; private set; }

        public bool HasCompleteBindings =>
            rootRect != null && rootImage != null && canvasGroup != null &&
            background != null && raceSkin != null && artworkMask != null &&
            artworkMaskComponent != null && artwork != null &&
            normalFrame != null && goldenFrame != null &&
            costBadge != null && costText != null &&
            tierBadge != null && tierText != null &&
            namePlate != null && nameText != null &&
            infoPanel != null && raceOrSpellTypeText != null &&
            abilityLabelRow != null && abilityLabelTexts != null &&
            abilityLabelTexts.Length == 3 &&
            Array.TrueForAll(abilityLabelTexts, value => value != null) &&
            descriptionText != null && progressRoot != null &&
            progressFill != null && progressText != null &&
            stateBadgeRow != null && shieldBadge != null &&
            nextCombatShieldBadge != null && temporaryBadge != null &&
            attackBadge != null && attackText != null &&
            healthBadge != null && healthText != null &&
            spellFooter != null && growthFeedbackRoot != null &&
            growthFeedbackCanvasGroup != null && growthFeedbackText != null &&
            selectionFrame != null && legalTargetFrame != null &&
            disabledMask != null && disabledIcon != null &&
            disabledReasonText != null;

        public void Render(CardViewModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (!HasCompleteBindings)
            {
                throw new InvalidOperationException(
                    "CardView has missing serialized bindings.");
            }

            var isMinion = model.IsMinion;
            var isGolden = isMinion && model.IsGolden;
            var hasProgress = !string.IsNullOrWhiteSpace(model.ProgressText);
            var isInteractable = model.IsInteractable;

            ApplyLayout(model.DisplayMode, hasProgress);
            ResetVisualState();

            CurrentDisplayMode = model.DisplayMode;
            background.color = CardTierPalette.GetBackground(model.Tier);
            raceSkin.color = ResolveRaceColor(model.RaceText);
            artwork.color = ResolveArtworkColor(model.RaceText);
            normalFrame.gameObject.SetActive(!isGolden);
            goldenFrame.gameObject.SetActive(isGolden);
            normalFrame.color = NormalFrameColor;
            goldenFrame.color = GoldenFrameColor;

            costBadge.gameObject.SetActive(model.ShowCost);
            costText.text = model.Cost.ToString();
            costText.color = model.IsAffordable
                ? NormalTextColor
                : UnaffordableColor;
            tierText.text = "T" + model.Tier;

            ApplySingleLineText(
                nameText,
                model.Name,
                model.DisplayMode == CardDisplayMode.Full ? 22 : 16,
                model.DisplayMode == CardDisplayMode.Full ? 18 : 14);
            ApplySingleLineText(
                raceOrSpellTypeText,
                model.RaceText,
                model.DisplayMode == CardDisplayMode.Full ? 14 : 11,
                model.DisplayMode == CardDisplayMode.Full ? 12 : 11);
            ApplyAbilityLabels(model.AbilityLabels, model.DisplayMode);
            ApplyDescription(
                model.Description,
                model.DisplayMode,
                isMinion,
                hasProgress);

            progressRoot.gameObject.SetActive(hasProgress);
            progressText.text = hasProgress
                ? UiTextFormatter.ToSingleLine(model.ProgressText)
                : string.Empty;
            progressText.fontSize = model.DisplayMode == CardDisplayMode.Full
                ? 12
                : 10;

            attackBadge.gameObject.SetActive(isMinion);
            healthBadge.gameObject.SetActive(isMinion);
            spellFooter.gameObject.SetActive(!isMinion);
            attackText.text = model.Attack.ToString();
            healthText.text = model.Health.ToString();
            attackText.fontSize = model.DisplayMode == CardDisplayMode.Full
                ? 26
                : 20;
            healthText.fontSize = attackText.fontSize;
            attackText.color = isInteractable && model.Attack > model.BaseAttack
                ? GrowthColor
                : NormalTextColor;
            healthText.color = isInteractable && model.Health > model.BaseHealth
                ? GrowthColor
                : NormalTextColor;
            spellFooter.text = "商店法术";

            var showShield = isMinion && model.HasShield;
            var showNextShield = isMinion && model.HasNextCombatShield;
            var showTemporary = model.IsTemporary;
            shieldBadge.gameObject.SetActive(showShield);
            nextCombatShieldBadge.gameObject.SetActive(showNextShield);
            temporaryBadge.gameObject.SetActive(showTemporary);
            shieldBadge.text = "护盾";
            nextCombatShieldBadge.text = "下战";
            temporaryBadge.text = "临时";
            stateBadgeRow.gameObject.SetActive(
                showShield || showNextShield || showTemporary);

            selectionFrame.gameObject.SetActive(model.IsSelected);
            selectionFrame.color = new Color(
                SelectionColor.r,
                SelectionColor.g,
                SelectionColor.b,
                isInteractable ? SelectionColor.a : SelectionColor.a * 0.35f);
            legalTargetFrame.gameObject.SetActive(
                model.IsLegalTarget && isInteractable);
            legalTargetFrame.color = LegalTargetColor;
            disabledMask.gameObject.SetActive(!isInteractable);
            disabledIcon.text = "!";
            disabledReasonText.text = model.DisabledReason ?? string.Empty;
            growthFeedbackRoot.gameObject.SetActive(false);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = isInteractable;
            canvasGroup.blocksRaycasts = true;
        }

        public void PlayStatChange(int attackDelta, int healthDelta)
        {
            if (!HasCompleteBindings)
            {
                throw new InvalidOperationException(
                    "CardView has missing serialized bindings.");
            }

            if (attackDelta == 0 && healthDelta == 0)
            {
                return;
            }

            if (attackDelta != 0 && healthDelta != 0)
            {
                growthFeedbackText.text =
                    $"{FormatDelta(attackDelta)}/{FormatDelta(healthDelta)}";
            }
            else if (attackDelta != 0)
            {
                growthFeedbackText.text = $"{FormatDelta(attackDelta)} 攻击";
            }
            else
            {
                growthFeedbackText.text = $"{FormatDelta(healthDelta)} 生命";
            }

            growthFeedbackText.color = attackDelta < 0 || healthDelta < 0
                ? DeclineColor
                : GrowthColor;
            growthFeedbackCanvasGroup.alpha = 1f;
            growthFeedbackRoot.gameObject.SetActive(true);
            if (Application.isPlaying)
            {
                StartCoroutine(HideGrowthFeedback());
            }
        }

        public void PlayShieldGain(bool nextCombat)
        {
            if (!HasCompleteBindings)
            {
                throw new InvalidOperationException(
                    "CardView has missing serialized bindings.");
            }

            var badge = nextCombat
                ? nextCombatShieldBadge.rectTransform
                : shieldBadge.rectTransform;
            badge.localScale = Vector3.one * 1.28f;
            if (Application.isPlaying)
            {
                StartCoroutine(RestoreBadgeScale(badge));
            }
        }

        private IEnumerator HideGrowthFeedback()
        {
            yield return new WaitForSecondsRealtime(0.65f);
            var elapsed = 0f;
            const float duration = 0.35f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                growthFeedbackCanvasGroup.alpha =
                    1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            growthFeedbackRoot.gameObject.SetActive(false);
            growthFeedbackCanvasGroup.alpha = 1f;
        }

        private static IEnumerator RestoreBadgeScale(RectTransform badge)
        {
            var elapsed = 0f;
            const float duration = 0.28f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                badge.localScale = Vector3.Lerp(
                    Vector3.one * 1.28f,
                    Vector3.one,
                    Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            badge.localScale = Vector3.one;
        }

        private static string FormatDelta(int value)
        {
            return value > 0 ? "+" + value : value.ToString();
        }

        public void ApplyLayout(CardDisplayMode mode, bool hasProgress)
        {
            if (!HasCompleteBindings)
            {
                throw new InvalidOperationException(
                    "CardView has missing serialized bindings.");
            }

            var full = mode == CardDisplayMode.Full;
            var root = full
                ? new ContractRect(0f, 0f, 240f, 360f)
                : new ContractRect(0f, 0f, 160f, 240f);
            var frame = full
                ? new ContractRect(6f, 6f, 228f, 348f)
                : new ContractRect(4f, 4f, 152f, 232f);
            var art = full
                ? new ContractRect(12f, 12f, 216f, 184f)
                : new ContractRect(8f, 8f, 144f, 112f);
            var cost = full
                ? new ContractRect(8f, 8f, 48f, 48f)
                : new ContractRect(6f, 6f, 34f, 34f);
            var tier = full
                ? new ContractRect(184f, 8f, 48f, 48f)
                : new ContractRect(120f, 6f, 34f, 34f);
            var state = full
                ? new ContractRect(60f, 157f, 120f, 22f)
                : new ContractRect(42f, 91f, 76f, 18f);
            var name = full
                ? new ContractRect(24f, 181f, 192f, 32f)
                : new ContractRect(16f, 108f, 128f, 26f);
            var info = full
                ? new ContractRect(12f, 199f, 216f, 149f)
                : new ContractRect(8f, 122f, 144f, 110f);
            var race = full
                ? new ContractRect(44f, 215f, 152f, 18f)
                : new ContractRect(28f, 136f, 104f, 14f);
            var labels = full
                ? new ContractRect(20f, 235f, 200f, 20f)
                : new ContractRect(12f, 154f, 136f, 16f);
            var description = full
                ? new ContractRect(12f, 256f, 216f, hasProgress ? 31f : 52f)
                : new ContractRect(12f, 172f, 136f, hasProgress ? 21f : 33f);
            var progress = full
                ? new ContractRect(62f, 293f, 116f, 18f)
                : new ContractRect(44f, 197f, 72f, 14f);
            var attack = full
                ? new ContractRect(8f, 308f, 44f, 44f)
                : new ContractRect(6f, 204f, 32f, 32f);
            var health = full
                ? new ContractRect(188f, 308f, 44f, 44f)
                : new ContractRect(122f, 204f, 32f, 32f);
            var footer = full
                ? new ContractRect(58f, 318f, 124f, 22f)
                : new ContractRect(42f, 211f, 76f, 16f);
            var feedback = full
                ? new ContractRect(40f, 105f, 160f, 40f)
                : new ContractRect(20f, 55f, 120f, 34f);

            SetRootRect(rootRect, root);
            SetRect(background.rectTransform, root);
            SetRect(raceSkin.rectTransform, root);
            SetRect(artworkMask, art);
            Stretch(artwork.rectTransform);
            SetRect(normalFrame.rectTransform, frame);
            SetRect(goldenFrame.rectTransform, frame);
            SetRect(costBadge.rectTransform, cost);
            Stretch(costText.rectTransform, 3f);
            SetRect(tierBadge.rectTransform, tier);
            Stretch(tierText.rectTransform, 3f);
            SetRect(namePlate.rectTransform, name);
            Stretch(nameText.rectTransform, 4f);
            SetRect(infoPanel.rectTransform, info);
            SetRect(raceOrSpellTypeText.rectTransform, race, info);
            SetRect(abilityLabelRow, labels, info);
            SetRect(descriptionText.rectTransform, description, info);
            SetRect(progressRoot, progress, info);
            Stretch(progressFill.rectTransform);
            Stretch(progressText.rectTransform, 2f);
            SetRect(stateBadgeRow, state);
            SetRect(attackBadge.rectTransform, attack);
            Stretch(attackText.rectTransform, 2f);
            SetRect(healthBadge.rectTransform, health);
            Stretch(healthText.rectTransform, 2f);
            SetRect(spellFooter.rectTransform, footer);
            SetRect(growthFeedbackRoot, feedback);
            SetRect(selectionFrame.rectTransform, root);
            SetRect(legalTargetFrame.rectTransform, root);
            SetRect(disabledMask.rectTransform, root);
            LayoutLabels(labels, full ? 3 : 2);
            LayoutStateBadges(state);
            LayoutDisabledContent(root);
            CurrentDisplayMode = mode;
        }

        private void ResetVisualState()
        {
            if (Application.isPlaying)
            {
                StopAllCoroutines();
            }

            costText.text = string.Empty;
            tierText.text = string.Empty;
            nameText.text = string.Empty;
            raceOrSpellTypeText.text = string.Empty;
            descriptionText.text = string.Empty;
            progressText.text = string.Empty;
            attackText.text = string.Empty;
            healthText.text = string.Empty;
            spellFooter.text = string.Empty;
            disabledIcon.text = string.Empty;
            disabledReasonText.text = string.Empty;
            foreach (var label in abilityLabelTexts)
            {
                label.text = string.Empty;
                label.gameObject.SetActive(false);
            }

            progressRoot.gameObject.SetActive(false);
            goldenFrame.gameObject.SetActive(false);
            shieldBadge.gameObject.SetActive(false);
            nextCombatShieldBadge.gameObject.SetActive(false);
            temporaryBadge.gameObject.SetActive(false);
            stateBadgeRow.gameObject.SetActive(false);
            selectionFrame.gameObject.SetActive(false);
            legalTargetFrame.gameObject.SetActive(false);
            disabledMask.gameObject.SetActive(false);
            growthFeedbackRoot.gameObject.SetActive(false);
            growthFeedbackCanvasGroup.alpha = 1f;
            growthFeedbackText.text = string.Empty;
            shieldBadge.rectTransform.localScale = Vector3.one;
            nextCombatShieldBadge.rectTransform.localScale = Vector3.one;
            attackText.color = NormalTextColor;
            healthText.color = NormalTextColor;
        }

        private void ApplyAbilityLabels(
            string[] labels,
            CardDisplayMode displayMode)
        {
            var formatted = UiTextFormatter.FormatAbilityLabels(
                labels,
                displayMode);
            var fontSize = displayMode == CardDisplayMode.Full ? 12 : 10;
            for (var index = 0; index < abilityLabelTexts.Length; index++)
            {
                var visible = index < formatted.Length;
                abilityLabelTexts[index].gameObject.SetActive(visible);
                abilityLabelTexts[index].text = visible
                    ? formatted[index]
                    : string.Empty;
                abilityLabelTexts[index].fontSize = fontSize;
            }
        }

        private void ApplySingleLineText(
            Text target,
            string value,
            int baseSize,
            int minimumSize)
        {
            var normalized = UiTextFormatter.ToSingleLine(value);
            for (var size = baseSize; size >= minimumSize; size--)
            {
                if (!Fits(target, normalized, size, 1, true))
                {
                    continue;
                }

                target.fontSize = size;
                target.text = normalized;
                return;
            }

            target.fontSize = minimumSize;
            target.text = UiTextFormatter.EllipsizeName(
                normalized,
                candidate => Fits(target, candidate, minimumSize, 1, true));
        }

        private void ApplyDescription(
            string value,
            CardDisplayMode mode,
            bool isMinion,
            bool hasProgress)
        {
            var normalized = UiTextFormatter.NormalizeWhitespace(value);
            var baseSize = mode == CardDisplayMode.Full ? 14 : 11;
            var minimumSize = mode == CardDisplayMode.Full ? 11 : 10;
            var maximumLines = UiTextFormatter.GetDescriptionMaxLines(
                mode,
                isMinion,
                hasProgress);
            for (var size = baseSize; size >= minimumSize; size--)
            {
                if (!Fits(
                        descriptionText,
                        normalized,
                        size,
                        maximumLines,
                        false))
                {
                    continue;
                }

                descriptionText.fontSize = size;
                descriptionText.text = normalized;
                return;
            }

            descriptionText.fontSize = minimumSize;
            if (mode == CardDisplayMode.Full)
            {
                throw new InvalidOperationException(
                    "Full card description does not fit its layout contract. " +
                    DescribeFit(
                        descriptionText,
                        normalized,
                        minimumSize,
                        maximumLines,
                        false));
            }

            descriptionText.text = UiTextFormatter.EllipsizeDescription(
                normalized,
                mode,
                candidate => Fits(
                    descriptionText,
                    candidate,
                    minimumSize,
                    maximumLines,
                    false));
        }

        private static bool Fits(
            Text target,
            string value,
            int fontSize,
            int maximumLines,
            bool singleLine)
        {
            if (target.font == null)
            {
                return false;
            }

            var size = target.rectTransform.rect.size;
            if (size.x <= 0f || size.y <= 0f)
            {
                return false;
            }

            var settings = target.GetGenerationSettings(size);
            settings.fontSize = fontSize;
            settings.resizeTextForBestFit = false;
            settings.horizontalOverflow = singleLine
                ? HorizontalWrapMode.Overflow
                : HorizontalWrapMode.Wrap;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            settings.generateOutOfBounds = true;
            var generator = new TextGenerator(Math.Max(8, (value ?? string.Empty).Length));
            if (!generator.Populate(value ?? string.Empty, settings))
            {
                return false;
            }

            var rect = generator.rectExtents;
            return generator.lineCount <= maximumLines &&
                   rect.width <= size.x + 0.5f &&
                   rect.height <= size.y + 0.5f;
        }

        private static string DescribeFit(
            Text target,
            string value,
            int fontSize,
            int maximumLines,
            bool singleLine)
        {
            var size = target.rectTransform.rect.size;
            var settings = target.GetGenerationSettings(size);
            settings.fontSize = fontSize;
            settings.resizeTextForBestFit = false;
            settings.horizontalOverflow = singleLine
                ? HorizontalWrapMode.Overflow
                : HorizontalWrapMode.Wrap;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            settings.generateOutOfBounds = true;
            var generator = new TextGenerator(Math.Max(8, value.Length));
            var populated = generator.Populate(value, settings);
            var rect = generator.rectExtents;
            return $"font={target.font?.name ?? "<null>"}, size={fontSize}, " +
                   $"area={size.x:0.##}x{size.y:0.##}, " +
                   $"generated={rect.width:0.##}x{rect.height:0.##}, " +
                   $"lines={generator.lineCount}/{maximumLines}, " +
                   $"populated={populated}.";
        }

        private void LayoutLabels(
            ContractRect labels,
            int capacity)
        {
            var width = labels.Width / capacity;
            for (var index = 0; index < abilityLabelTexts.Length; index++)
            {
                var column = Math.Min(index, capacity - 1);
                var rect = new ContractRect(
                    labels.X + column * width,
                    labels.Y,
                    width,
                    labels.Height);
                SetRect(abilityLabelTexts[index].rectTransform, rect, labels);
            }
        }

        private void LayoutStateBadges(ContractRect state)
        {
            var badges = new[]
            {
                shieldBadge,
                nextCombatShieldBadge,
                temporaryBadge
            };
            var width = state.Width / badges.Length;
            for (var index = 0; index < badges.Length; index++)
            {
                var rect = new ContractRect(
                    index * width,
                    0f,
                    width,
                    state.Height);
                SetRect(badges[index].rectTransform, rect);
            }
        }

        private void LayoutDisabledContent(ContractRect root)
        {
            SetRect(
                disabledIcon.rectTransform,
                new ContractRect(
                    root.Width * 0.5f - 12f,
                    root.Height * 0.34f,
                    24f,
                    28f));
            SetRect(
                disabledReasonText.rectTransform,
                new ContractRect(
                    root.Width * 0.10f,
                    root.Height * 0.45f,
                    root.Width * 0.80f,
                    root.Height * 0.20f));
        }

        private static void SetRootRect(RectTransform target, ContractRect rect)
        {
            target.pivot = new Vector2(0f, 1f);
            target.sizeDelta = new Vector2(rect.Width, rect.Height);
        }

        private static void SetRect(RectTransform target, ContractRect rect)
        {
            target.anchorMin = new Vector2(0f, 1f);
            target.anchorMax = new Vector2(0f, 1f);
            target.pivot = new Vector2(0f, 1f);
            target.anchoredPosition = new Vector2(rect.X, -rect.Y);
            target.sizeDelta = new Vector2(rect.Width, rect.Height);
            target.localScale = Vector3.one;
        }

        private static void SetRect(
            RectTransform target,
            ContractRect rect,
            ContractRect parent)
        {
            SetRect(
                target,
                new ContractRect(
                    rect.X - parent.X,
                    rect.Y - parent.Y,
                    rect.Width,
                    rect.Height));
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

        private static Color ResolveRaceColor(string race)
        {
            switch (race)
            {
                case "铸魂": return new Color(0.50f, 0.24f, 0.18f, 0.44f);
                case "荒灵": return new Color(0.20f, 0.48f, 0.27f, 0.44f);
                case "星契": return new Color(0.20f, 0.34f, 0.62f, 0.44f);
                case "旅团": return new Color(0.46f, 0.38f, 0.24f, 0.44f);
                default: return new Color(0.30f, 0.28f, 0.36f, 0.40f);
            }
        }

        private static Color ResolveArtworkColor(string race)
        {
            var raceColor = ResolveRaceColor(race);
            return new Color(
                Mathf.Clamp01(raceColor.r + 0.14f),
                Mathf.Clamp01(raceColor.g + 0.14f),
                Mathf.Clamp01(raceColor.b + 0.18f),
                0.92f);
        }

        private readonly struct ContractRect
        {
            public ContractRect(float x, float y, float width, float height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            public float X { get; }
            public float Y { get; }
            public float Width { get; }
            public float Height { get; }
        }
    }
}

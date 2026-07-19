using System;
using SpireChess.Battle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpireChess.UI.Battle
{
    public sealed class BattleCardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const int StatsBaseFontSize = 20;
        private const int StatsMinimumFontSize = 10;
        private const float StatsTextWidth = 76f;
        private const float StatsTextHeight = 32f;

        private BattleTestController controller;
        private Canvas rootCanvas;
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Transform originalParent;
        private Vector2 originalAnchoredPosition;
        private bool draggable;

        public BattleSide Side { get; private set; }
        public int Index { get; private set; }

        public void Initialize(
            BattleTestController controller,
            Canvas rootCanvas,
            BattleSide side,
            int index,
            bool draggable)
        {
            this.controller = controller;
            this.rootCanvas = rootCanvas;
            Side = side;
            Index = index;
            this.draggable = draggable;
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Render(BattleMinionRuntime minion)
        {
            SetText("Name", minion.IsGolden ? $"金色{minion.Name}" : minion.Name);
            SetStatsText($"{minion.CurrentAttack}/{minion.CurrentHealth}");
            SetText("Tier", $"T{minion.Config.Tier}");
            SetText("Race", ToRaceName(minion.Config.Race));
            SetText("Keywords", minion.BuildKeywordText());
            SetText("Description", minion.Config.GetPrototypeDescription(minion.IsGolden));

            var shield = transform.Find("Shield");
            if (shield != null)
            {
                shield.gameObject.SetActive(minion.HasShield);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!draggable || controller.IsBattleLocked)
            {
                return;
            }

            originalParent = transform.parent;
            originalAnchoredPosition = rectTransform.anchoredPosition;
            transform.SetParent(rootCanvas.transform, true);
            transform.SetAsLastSibling();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.82f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!draggable || controller.IsBattleLocked || originalParent == null)
            {
                return;
            }

            rectTransform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (originalParent == null)
            {
                return;
            }

            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;

            if (transform.parent == rootCanvas.transform)
            {
                transform.SetParent(originalParent, false);
                rectTransform.anchoredPosition = originalAnchoredPosition;
            }

            originalParent = null;
        }

        private void SetText(string childName, string value)
        {
            var child = transform.Find(childName);
            if (child == null)
            {
                return;
            }

            var text = child.GetComponent<Text>();
            if (text != null)
            {
                text.text = value;
            }
        }

        private void SetStatsText(string value)
        {
            var child = transform.Find("Stats");
            var text = child == null ? null : child.GetComponent<Text>();
            if (text == null)
            {
                return;
            }

            text.text = value;
            text.fontSize = ResolveStatsFontSize(text, value);
        }

        private static int ResolveStatsFontSize(Text target, string value)
        {
            if (target.font == null)
            {
                return StatsMinimumFontSize;
            }

            for (var size = StatsBaseFontSize;
                 size >= StatsMinimumFontSize;
                 size--)
            {
                if (FitsStatsText(target, value, size))
                {
                    return size;
                }
            }

            return StatsMinimumFontSize;
        }

        private static bool FitsStatsText(Text target, string value, int fontSize)
        {
            var area = new Vector2(StatsTextWidth, StatsTextHeight);
            var settings = target.GetGenerationSettings(area);
            settings.fontSize = fontSize;
            settings.resizeTextForBestFit = false;
            settings.horizontalOverflow = HorizontalWrapMode.Overflow;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            settings.generateOutOfBounds = true;
            var textValue = value ?? string.Empty;
            var generator = new TextGenerator(Math.Max(8, textValue.Length));
            if (!generator.Populate(textValue, settings))
            {
                return false;
            }

            var pixelsPerUnit = target.pixelsPerUnit;
            if (pixelsPerUnit <= 0f || float.IsNaN(pixelsPerUnit) ||
                float.IsInfinity(pixelsPerUnit))
            {
                pixelsPerUnit = 1f;
            }
            var width = generator.GetPreferredWidth(textValue, settings) /
                        pixelsPerUnit;
            var height = generator.rectExtents.height / pixelsPerUnit;
            return width <= StatsTextWidth + 0.5f &&
                   height <= StatsTextHeight + 0.5f;
        }

        private static string ToRaceName(string race)
        {
            switch (race)
            {
                case "ForgeSoul":
                    return "铸魂";
                case "WildSpirit":
                    return "荒灵";
                case "Starbound":
                    return "星契";
                case "Wayfarer":
                    return "旅团";
                default:
                    return race;
            }
        }
    }
}

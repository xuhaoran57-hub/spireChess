using System;
using SpireChess.Battle;
using SpireChess.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpireChess.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class BattleCardView : MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
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
        private bool dropHandled;
        private CardView cardView;

        public BattleSide Side { get; private set; }
        public int Index { get; private set; }
        public string InstanceId { get; private set; }
        public RectTransform RectTransform => rectTransform;
        public CardView CardView => cardView;

        public void Initialize(
            BattleTestController value,
            Canvas canvas,
            BattleSide side,
            int index,
            bool canDrag)
        {
            controller = value;
            rootCanvas = canvas;
            Side = side;
            Index = index;
            draggable = canDrag;
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            cardView = GetComponent<CardView>();
        }

        public void Render(CardViewModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            InstanceId = model.InstanceId;
            if (cardView != null)
            {
                cardView.Render(model);
                return;
            }

            SetText("Name", model.IsGolden ? $"金色{model.Name}" : model.Name);
            SetText("Tier", $"T{model.Tier}");
            SetText("Race", model.RaceText);
            SetText("Keywords", string.Join(" / ", model.Keywords));
            SetText("Description", model.Description);
            SetStatsText($"{model.Attack}/{model.Health}");
            var shield = transform.Find("Shield");
            if (shield != null)
            {
                shield.gameObject.SetActive(model.HasShield);
            }
        }

        public void Render(BattleMinionRuntime minion)
        {
            Render(BattleCardViewModelFactory.FromRuntime(minion, Side, Index));
        }

        public void MarkDropHandled()
        {
            dropHandled = true;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!draggable || controller == null || controller.IsBattleLocked)
            {
                return;
            }

            dropHandled = false;
            originalParent = transform.parent;
            originalAnchoredPosition = rectTransform.anchoredPosition;
            transform.SetParent(rootCanvas.transform, true);
            transform.SetAsLastSibling();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.82f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!draggable || controller == null ||
                controller.IsBattleLocked || originalParent == null)
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
            if (!dropHandled && transform.parent == rootCanvas.transform)
            {
                transform.SetParent(originalParent, false);
                rectTransform.anchoredPosition = originalAnchoredPosition;
            }

            originalParent = null;
            dropHandled = false;
        }

        private void SetText(string childName, string value)
        {
            var child = transform.Find(childName);
            var text = child == null ? null : child.GetComponent<Text>();
            if (text != null)
            {
                text.text = value ?? string.Empty;
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
    }
}

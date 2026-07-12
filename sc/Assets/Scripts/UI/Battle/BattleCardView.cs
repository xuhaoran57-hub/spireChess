using SpireChess.Battle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpireChess.UI.Battle
{
    public sealed class BattleCardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
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
            SetText("Stats", $"{minion.CurrentAttack}/{minion.CurrentHealth}");
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

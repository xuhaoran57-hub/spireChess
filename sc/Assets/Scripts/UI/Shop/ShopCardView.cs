using UnityEngine;
using UnityEngine.EventSystems;

namespace SpireChess.UI.Shop
{
    public enum ShopCardZone
    {
        MinionOffer,
        SpellOffer,
        Bench,
        Battle
    }

    public sealed class ShopCardView : MonoBehaviour,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        private ShopTestController controller;
        private Canvas rootCanvas;
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Transform originalParent;
        private Vector2 originalAnchoredPosition;
        private bool draggable;

        public ShopCardZone Zone { get; private set; }
        public int Index { get; private set; }

        public void Initialize(
            ShopTestController controller,
            Canvas rootCanvas,
            ShopCardZone zone,
            int index,
            bool draggable)
        {
            this.controller = controller;
            this.rootCanvas = rootCanvas;
            Zone = zone;
            Index = index;
            this.draggable = draggable;
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            controller.HandleCardClick(Zone, Index);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!draggable || !controller.CanDrag(Zone, Index))
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
            if (originalParent != null)
            {
                rectTransform.position = eventData.position;
            }
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
    }
}

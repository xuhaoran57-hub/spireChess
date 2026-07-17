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
        private bool dropHandled;

        public ShopCardZone Zone { get; private set; }
        public int Index { get; private set; }
        public bool IsDragging => originalParent != null;

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
            dropHandled = false;
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
            if (dropHandled)
            {
                originalParent = null;
                Destroy(gameObject);
                return;
            }

            if (transform.parent == rootCanvas.transform)
            {
                transform.SetParent(originalParent, false);
                rectTransform.anchoredPosition = originalAnchoredPosition;
            }

            originalParent = null;
        }

        public void CompleteDrop()
        {
            if (originalParent != null)
            {
                dropHandled = true;
            }
        }
    }
}

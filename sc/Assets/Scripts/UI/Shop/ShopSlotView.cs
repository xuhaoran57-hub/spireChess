using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpireChess.UI.Shop
{
    [DisallowMultipleComponent]
    public sealed class ShopSlotView : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        [SerializeField] private Image background;
        [SerializeField] private Text emptyHint;
        [SerializeField] private Image selectionFrame;
        [SerializeField] private RectTransform content;

        private ShopTestController controller;

        public ShopCardZone Zone { get; private set; }
        public int Index { get; private set; }
        public RectTransform Content => content;

        public bool HasCompleteBindings =>
            background != null && emptyHint != null &&
            selectionFrame != null && content != null;

        public void Initialize(ShopTestController controller, ShopCardZone zone, int index)
        {
            this.controller = controller;
            Zone = zone;
            Index = index;
        }

        public void PrepareForRender(string emptyMessage, bool isSelected = false)
        {
            if (!HasCompleteBindings)
            {
                throw new InvalidOperationException(
                    "ShopSlotView has missing serialized bindings.");
            }

            ClearContent();
            emptyHint.text = emptyMessage ?? string.Empty;
            emptyHint.gameObject.SetActive(true);
            selectionFrame.gameObject.SetActive(isSelected);
        }

        public void ShowCard()
        {
            if (!HasCompleteBindings)
            {
                throw new InvalidOperationException(
                    "ShopSlotView has missing serialized bindings.");
            }

            emptyHint.gameObject.SetActive(false);
        }

        public void ClearContent()
        {
            if (content == null)
            {
                return;
            }

            for (var index = content.childCount - 1; index >= 0; index--)
            {
                var child = content.GetChild(index).gameObject;
                if (Application.isPlaying)
                {
                    child.transform.SetParent(null, false);
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (controller == null)
            {
                return;
            }

            var card = eventData.pointerDrag == null
                ? null
                : eventData.pointerDrag.GetComponent<ShopCardView>();
            if (card != null)
            {
                controller.HandleDrop(card.Zone, card.Index, Zone, Index);
                card.CompleteDrop();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (controller != null)
            {
                controller.HandleSlotClick(Zone, Index);
            }
        }
    }
}

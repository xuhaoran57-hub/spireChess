using UnityEngine;
using UnityEngine.EventSystems;

namespace SpireChess.UI.Shop
{
    public sealed class ShopSlotView : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        private ShopTestController controller;

        public ShopCardZone Zone { get; private set; }
        public int Index { get; private set; }

        public void Initialize(ShopTestController controller, ShopCardZone zone, int index)
        {
            this.controller = controller;
            Zone = zone;
            Index = index;
        }

        public void OnDrop(PointerEventData eventData)
        {
            var card = eventData.pointerDrag == null
                ? null
                : eventData.pointerDrag.GetComponent<ShopCardView>();
            if (card != null)
            {
                controller.HandleDrop(card.Zone, card.Index, Zone, Index);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            controller.HandleSlotClick(Zone, Index);
        }
    }
}

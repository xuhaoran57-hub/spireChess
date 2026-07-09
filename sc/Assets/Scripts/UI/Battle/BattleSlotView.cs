using SpireChess.Battle;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SpireChess.UI.Battle
{
    public sealed class BattleSlotView : MonoBehaviour, IDropHandler
    {
        private BattleTestController controller;

        public BattleSide Side { get; private set; }
        public int Index { get; private set; }

        public void Initialize(BattleTestController controller, BattleSide side, int index)
        {
            this.controller = controller;
            Side = side;
            Index = index;
        }

        public void OnDrop(PointerEventData eventData)
        {
            var card = eventData.pointerDrag == null
                ? null
                : eventData.pointerDrag.GetComponent<BattleCardView>();

            if (card == null)
            {
                return;
            }

            controller.MoveCard(card.Side, card.Index, Side, Index);
        }
    }
}

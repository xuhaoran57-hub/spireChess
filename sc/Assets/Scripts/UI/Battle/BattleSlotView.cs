using SpireChess.Battle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpireChess.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class BattleSlotView : MonoBehaviour, IDropHandler
    {
        [SerializeField] private Image background;
        [SerializeField] private Text emptyHint;
        [SerializeField] private RectTransform content;
        [SerializeField] private Outline highlight;

        private BattleTestController controller;

        public BattleSide Side { get; private set; }
        public int Index { get; private set; }
        public RectTransform Content => content;
        public bool HasCompleteBindings =>
            background != null && emptyHint != null &&
            content != null && highlight != null;

        public void Initialize(
            BattleTestController value,
            BattleSide side,
            int index)
        {
            controller = value;
            Side = side;
            Index = index;
        }

        public void PrepareForRender(bool hasCard)
        {
            if (emptyHint != null)
            {
                emptyHint.gameObject.SetActive(!hasCard);
                emptyHint.text = hasCard ? string.Empty : (Index + 1).ToString();
            }
            SetHighlight(Color.clear, Vector2.zero);
        }

        public void SetHighlight(Color color, Vector2 distance)
        {
            if (highlight == null)
            {
                return;
            }

            highlight.effectColor = color;
            highlight.effectDistance = distance;
        }

        public void OnDrop(PointerEventData eventData)
        {
            var card = eventData.pointerDrag == null
                ? null
                : eventData.pointerDrag.GetComponent<BattleCardView>();
            if (card == null || controller == null)
            {
                return;
            }

            card.MarkDropHandled();
            controller.MoveCard(card.Side, card.Index, Side, Index);
        }
    }
}

using System;
using SpireChess.UI;

namespace SpireChess.UI.Shop
{
    public sealed class ShopScreenState
    {
        public int Round { get; set; }
        public int Gold { get; set; }
        public int TavernTier { get; set; }
        public int UpgradeCost { get; set; }
        public int RefreshCount { get; set; }
        public int FreeRefreshes { get; set; }
        public int FlourishStacks { get; set; }

        public bool IsShopOpen { get; set; }
        public bool IsFrozen { get; set; }
        public bool IsInteractionBlocked { get; set; }
        public string BlockReason { get; set; }
        public string StatusMessage { get; set; }

        public CardViewModel[] MinionOffers { get; set; } = Array.Empty<CardViewModel>();
        public CardViewModel SpellOffer { get; set; }
        public CardViewModel[] BattleCards { get; set; } = Array.Empty<CardViewModel>();
        public HandCardsState HandCards { get; set; } = new HandCardsState();
        public ShopButtonStates Buttons { get; set; } = new ShopButtonStates();
        public CardDetailPanelState DetailPanel { get; set; } = new CardDetailPanelState();
    }

    public sealed class HandCardsState
    {
        public const int DefaultLimit = 5;
        public const int DefaultPageSize = 5;

        public HandCardSlotState[] VisibleSlots { get; set; } =
            Array.Empty<HandCardSlotState>();

        public int Count { get; set; }
        public int Limit { get; set; } = DefaultLimit;
        public int PageSize { get; set; } = DefaultPageSize;
        public int PageIndex { get; set; }
        public int PageCount { get; set; } = 1;
        public bool CanPageLeft { get; set; }
        public bool CanPageRight { get; set; }
    }

    public sealed class HandCardSlotState
    {
        public int SlotIndex { get; set; }
        public CardViewModel Card { get; set; }
        public bool IsEmpty => Card == null;
    }

    public sealed class ShopActionButtonState
    {
        public string Text { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsInteractable { get; set; }
        public bool IsActive { get; set; }
        public string DisabledReason { get; set; }
    }

    public sealed class ShopButtonStates
    {
        public ShopActionButtonState Refresh { get; set; } =
            new ShopActionButtonState();

        public ShopActionButtonState Freeze { get; set; } =
            new ShopActionButtonState();

        public ShopActionButtonState Upgrade { get; set; } =
            new ShopActionButtonState();

        public ShopActionButtonState Sell { get; set; } =
            new ShopActionButtonState();

        public ShopActionButtonState EndShop { get; set; } =
            new ShopActionButtonState();
    }

    public enum ShopCardLocation
    {
        None,
        MinionOffer,
        SpellOffer,
        Battle,
        Hand
    }

    public enum CardDetailStatusType
    {
        Growth,
        PermanentShield,
        NextCombatShield,
        Temporary
    }

    public sealed class CardDetailStatusState
    {
        public CardDetailStatusType Type { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
    }

    public sealed class CardDetailPanelState
    {
        public CardViewModel Card { get; set; }
        public ShopCardLocation Location { get; set; }
        public int SlotIndex { get; set; } = -1;
        public CardDetailStatusState[] Statuses { get; set; } =
            Array.Empty<CardDetailStatusState>();

        public bool IsVisible => Card != null;
    }
}

using NUnit.Framework;
using SpireChess.UI;
using SpireChess.UI.Shop;

namespace SpireChess.Tests.EditMode
{
    public sealed class ShopScreenStateTests
    {
        [Test]
        public void Defaults_AreSafeForAnEmptyFirstRender()
        {
            var state = new ShopScreenState();

            SequentialAssert.Run(() =>
            {
                Assert.That(state.MinionOffers, Is.Empty);
                Assert.That(state.BattleCards, Is.Empty);
                Assert.That(state.HandCards, Is.Not.Null);
                Assert.That(state.HandCards.VisibleSlots, Is.Empty);
                Assert.That(state.HandCards.Limit,
                    Is.EqualTo(HandCardsState.DefaultLimit));
                Assert.That(state.HandCards.PageSize,
                    Is.EqualTo(HandCardsState.DefaultPageSize));
                Assert.That(state.HandCards.PageCount, Is.EqualTo(1));
                Assert.That(state.Buttons, Is.Not.Null);
                Assert.That(state.Buttons.Refresh, Is.Not.Null);
                Assert.That(state.Buttons.Freeze, Is.Not.Null);
                Assert.That(state.Buttons.Upgrade, Is.Not.Null);
                Assert.That(state.Buttons.Sell, Is.Not.Null);
                Assert.That(state.Buttons.EndShop, Is.Not.Null);
                Assert.That(state.DetailPanel, Is.Not.Null);
                Assert.That(state.DetailPanel.Statuses, Is.Empty);
                Assert.That(state.DetailPanel.IsVisible, Is.False);
            });
        }

        [Test]
        public void HandCards_RepresentCurrentAndFuturePagedCapacity()
        {
            var current = new HandCardsState
            {
                Count = 5,
                Limit = 5,
                PageSize = 5,
                PageIndex = 0,
                PageCount = 1,
                CanPageLeft = false,
                CanPageRight = false
            };
            var future = new HandCardsState
            {
                Count = 7,
                Limit = 10,
                PageSize = 5,
                PageIndex = 1,
                PageCount = 2,
                CanPageLeft = true,
                CanPageRight = false
            };

            SequentialAssert.Run(() =>
            {
                Assert.That(current.Limit, Is.EqualTo(5));
                Assert.That(current.PageCount, Is.EqualTo(1));
                Assert.That(current.CanPageLeft, Is.False);
                Assert.That(current.CanPageRight, Is.False);
                Assert.That(future.Limit, Is.EqualTo(10));
                Assert.That(future.PageSize, Is.EqualTo(5));
                Assert.That(future.PageIndex, Is.EqualTo(1));
                Assert.That(future.PageCount, Is.EqualTo(2));
                Assert.That(future.CanPageLeft, Is.True);
                Assert.That(future.CanPageRight, Is.False);
            });
        }

        [Test]
        public void HandSlot_PreservesDomainIndexAndReportsEmptyState()
        {
            var occupied = new HandCardSlotState
            {
                SlotIndex = 7,
                Card = new CardViewModel { InstanceId = "card_007" }
            };
            var empty = new HandCardSlotState { SlotIndex = 8 };

            SequentialAssert.Run(() =>
            {
                Assert.That(occupied.SlotIndex, Is.EqualTo(7));
                Assert.That(occupied.IsEmpty, Is.False);
                Assert.That(empty.SlotIndex, Is.EqualTo(8));
                Assert.That(empty.IsEmpty, Is.True);
            });
        }

        [Test]
        public void Buttons_RepresentDynamicLabelsActiveStateAndDisabledReasons()
        {
            var paidRefresh = new ShopActionButtonState
            {
                Text = "Refresh (1 Gold)",
                IsInteractable = true
            };
            var buttons = new ShopButtonStates
            {
                Refresh = new ShopActionButtonState
                {
                    Text = "Refresh (Free)",
                    IsInteractable = true
                },
                Freeze = new ShopActionButtonState
                {
                    Text = "Frozen",
                    IsInteractable = true,
                    IsActive = true
                },
                Upgrade = new ShopActionButtonState
                {
                    Text = "Max Tier",
                    DisabledReason = "Already at maximum tavern tier"
                },
                Sell = new ShopActionButtonState
                {
                    Text = "Sell",
                    DisabledReason = "Select a battle minion"
                }
            };

            SequentialAssert.Run(() =>
            {
                Assert.That(buttons.Refresh.Text, Is.EqualTo("Refresh (Free)"));
                Assert.That(buttons.Refresh.IsInteractable, Is.True);
                Assert.That(paidRefresh.Text, Is.EqualTo("Refresh (1 Gold)"));
                Assert.That(paidRefresh.IsInteractable, Is.True);
                Assert.That(buttons.Freeze.IsActive, Is.True);
                Assert.That(buttons.Upgrade.IsInteractable, Is.False);
                Assert.That(buttons.Upgrade.DisabledReason, Is.Not.Empty);
                Assert.That(buttons.Sell.IsInteractable, Is.False);
                Assert.That(buttons.Sell.DisabledReason, Is.Not.Empty);
                Assert.That(buttons.EndShop.IsVisible, Is.True);
            });
        }

        [Test]
        public void DetailPanel_TracksSelectionLocationAndOnlySuppliedStatuses()
        {
            var hidden = new CardDetailPanelState();
            var visible = new CardDetailPanelState
            {
                Card = new CardViewModel { InstanceId = "battle_003" },
                Location = ShopCardLocation.Battle,
                SlotIndex = 3,
                Statuses = new[]
                {
                    new CardDetailStatusState
                    {
                        Type = CardDetailStatusType.Growth,
                        Label = "Growth",
                        Description = "+2/+3 permanent"
                    },
                    new CardDetailStatusState
                    {
                        Type = CardDetailStatusType.NextCombatShield,
                        Label = "Next combat shield"
                    }
                }
            };

            SequentialAssert.Run(() =>
            {
                Assert.That(hidden.IsVisible, Is.False);
                Assert.That(hidden.Location, Is.EqualTo(ShopCardLocation.None));
                Assert.That(hidden.SlotIndex, Is.EqualTo(-1));
                Assert.That(visible.IsVisible, Is.True);
                Assert.That(visible.Location, Is.EqualTo(ShopCardLocation.Battle));
                Assert.That(visible.SlotIndex, Is.EqualTo(3));
                Assert.That(visible.Statuses, Has.Length.EqualTo(2));
                Assert.That(visible.Statuses[0].Type,
                    Is.EqualTo(CardDetailStatusType.Growth));
                Assert.That(visible.Statuses[1].Type,
                    Is.EqualTo(CardDetailStatusType.NextCombatShield));
            });

            visible.Location = ShopCardLocation.Hand;
            visible.SlotIndex = 4;

            SequentialAssert.Run(() =>
            {
                Assert.That(visible.Location, Is.EqualTo(ShopCardLocation.Hand));
                Assert.That(visible.SlotIndex, Is.EqualTo(4));
            });
        }
    }
}

using System;
using System.Collections.Generic;
using SpireChess.Run;
using SpireChess.Shop;
using SpireChess.UI;

namespace SpireChess.UI.Shop
{
    public static class ShopScreenStateBuilder
    {
        private const string ShopClosedReason = "商店尚未开放";
        private const string InsufficientGoldReason = "金币不足";
        private const string HandFullReason = "手牌已满";

        public static ShopScreenState Build(
            ShopSession session,
            RunSession runSession = null,
            int selectedHandIndex = -1,
            int selectedBattleIndex = -1,
            int selectedEffectTargetIndex = -1,
            int handPageIndex = 0,
            string statusMessage = null)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var ownsSession = runSession != null &&
                              ReferenceEquals(runSession.Shop, session);
            var blockReason = ResolveBlockReason(session, runSession, ownsSession);
            var actionDisabledReason = ResolveActionDisabledReason(
                session,
                blockReason);
            var handCount = CountCards(session.Collection.Bench);
            var handIsFull = handCount >= session.Collection.Bench.Count;

            var state = new ShopScreenState
            {
                Round = session.Round,
                Gold = session.Gold,
                TavernTier = session.TavernTier,
                UpgradeCost = session.CurrentUpgradeCost,
                RefreshCount = session.RefreshCount,
                FreeRefreshes = session.FreeRefreshes,
                FlourishStacks = session.FlourishStacks,
                IsShopOpen = session.IsShopOpen,
                IsFrozen = session.IsFrozen,
                IsInteractionBlocked = blockReason != null,
                BlockReason = blockReason,
                StatusMessage = statusMessage ?? string.Empty
            };

            state.MinionOffers = BuildMinionOffers(
                session,
                actionDisabledReason,
                handIsFull);
            state.SpellOffer = BuildSpellOffer(
                session,
                actionDisabledReason,
                handIsFull);
            state.BattleCards = BuildBattleCards(
                session,
                selectedBattleIndex,
                selectedEffectTargetIndex,
                actionDisabledReason);
            state.HandCards = BuildHandCards(
                session,
                selectedHandIndex,
                handPageIndex,
                actionDisabledReason,
                handCount);
            ApplyTargetingState(
                session,
                state,
                selectedHandIndex,
                actionDisabledReason);
            state.Buttons = BuildButtons(
                session,
                ownsSession,
                selectedBattleIndex,
                actionDisabledReason);
            state.DetailPanel = BuildDetailPanel(
                session,
                state,
                selectedHandIndex,
                selectedBattleIndex,
                selectedEffectTargetIndex,
                actionDisabledReason);
            return state;
        }

        private static void ApplyTargetingState(
            ShopSession session,
            ShopScreenState state,
            int selectedHandIndex,
            string actionDisabledReason)
        {
            if (actionDisabledReason != null)
            {
                return;
            }

            var targeting = ShopTargetingQuery.ForHandCard(
                session,
                selectedHandIndex);
            if (!targeting.RequiresBattleTarget)
            {
                return;
            }

            for (var battleIndex = 0;
                 battleIndex < state.BattleCards.Length;
                 battleIndex++)
            {
                var card = state.BattleCards[battleIndex];
                if (card == null)
                {
                    continue;
                }

                if (targeting.IsLegalBattleTarget(battleIndex))
                {
                    card.IsLegalTarget = true;
                    continue;
                }

                card.IsInteractable = false;
                card.DisabledReason = targeting.GetBattleTargetDisabledReason(
                    battleIndex);
            }

            if (targeting.LegalBattleTargetIndexes.Count > 0)
            {
                return;
            }

            var selectedCard = FindVisibleHandCard(
                state.HandCards,
                selectedHandIndex);
            if (selectedCard != null && targeting.DisabledReason != null)
            {
                selectedCard.IsInteractable = false;
                selectedCard.DisabledReason = targeting.DisabledReason;
            }
        }

        private static CardViewModel[] BuildMinionOffers(
            ShopSession session,
            string actionDisabledReason,
            bool handIsFull)
        {
            var models = new CardViewModel[session.MinionOffers.Count];
            for (var i = 0; i < session.MinionOffers.Count; i++)
            {
                var offer = session.MinionOffers[i];
                if (offer == null)
                {
                    continue;
                }

                var model = ShopCardViewModelFactory.FromOffer(offer, session.Gold);
                ApplyOfferInteraction(
                    model,
                    actionDisabledReason,
                    handIsFull);
                models[i] = model;
            }

            return models;
        }

        private static CardViewModel BuildSpellOffer(
            ShopSession session,
            string actionDisabledReason,
            bool handIsFull)
        {
            if (session.SpellOffer == null)
            {
                return null;
            }

            var model = ShopCardViewModelFactory.FromOffer(
                session.SpellOffer,
                session.Gold);
            ApplyOfferInteraction(model, actionDisabledReason, handIsFull);
            return model;
        }

        private static CardViewModel[] BuildBattleCards(
            ShopSession session,
            int selectedBattleIndex,
            int selectedEffectTargetIndex,
            string actionDisabledReason)
        {
            var cards = session.Collection.Battle;
            var models = new CardViewModel[cards.Count];
            for (var i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null)
                {
                    continue;
                }

                var selected = selectedBattleIndex == i ||
                               selectedEffectTargetIndex == i;
                models[i] = BuildOwnedCard(
                    cards[i],
                    selected,
                    actionDisabledReason);
            }

            return models;
        }

        private static HandCardsState BuildHandCards(
            ShopSession session,
            int selectedHandIndex,
            int requestedPageIndex,
            string actionDisabledReason,
            int handCount)
        {
            var cards = session.Collection.Bench;
            var limit = cards.Count;
            var pageSize = HandCardsState.DefaultPageSize;
            var pageCount = Math.Max(1, (limit + pageSize - 1) / pageSize);
            var pageIndex = Math.Max(0, Math.Min(requestedPageIndex, pageCount - 1));
            var firstSlotIndex = pageIndex * pageSize;
            var visibleCount = Math.Min(pageSize, Math.Max(0, limit - firstSlotIndex));
            var slots = new HandCardSlotState[visibleCount];

            for (var pageSlotIndex = 0; pageSlotIndex < visibleCount; pageSlotIndex++)
            {
                var slotIndex = firstSlotIndex + pageSlotIndex;
                var card = cards[slotIndex];
                slots[pageSlotIndex] = new HandCardSlotState
                {
                    SlotIndex = slotIndex,
                    Card = card == null
                        ? null
                        : BuildOwnedCard(
                            card,
                            selectedHandIndex == slotIndex,
                            actionDisabledReason)
                };
            }

            return new HandCardsState
            {
                VisibleSlots = slots,
                Count = handCount,
                Limit = limit,
                PageSize = pageSize,
                PageIndex = pageIndex,
                PageCount = pageCount,
                CanPageLeft = pageIndex > 0,
                CanPageRight = pageIndex + 1 < pageCount
            };
        }

        private static CardViewModel BuildOwnedCard(
            ShopCardInstance card,
            bool selected,
            string actionDisabledReason)
        {
            var model = ShopCardViewModelFactory.FromOwned(card, selected);
            model.IsInteractable = actionDisabledReason == null;
            model.DisabledReason = actionDisabledReason;
            return model;
        }

        private static void ApplyOfferInteraction(
            CardViewModel model,
            string actionDisabledReason,
            bool handIsFull)
        {
            var disabledReason = actionDisabledReason;
            if (disabledReason == null && handIsFull)
            {
                disabledReason = HandFullReason;
            }

            if (disabledReason == null && !model.IsAffordable)
            {
                disabledReason = InsufficientGoldReason;
            }

            model.IsInteractable = disabledReason == null;
            model.DisabledReason = disabledReason;
        }

        private static ShopButtonStates BuildButtons(
            ShopSession session,
            bool ownsSession,
            int selectedBattleIndex,
            string actionDisabledReason)
        {
            var refreshReason = actionDisabledReason;
            if (refreshReason == null && session.FreeRefreshes <= 0 &&
                session.Gold < ShopEconomyRules.RefreshCost)
            {
                refreshReason = InsufficientGoldReason;
            }

            var upgradeReason = actionDisabledReason;
            if (upgradeReason == null &&
                session.TavernTier >= ShopEconomyRules.MaximumTavernTier)
            {
                upgradeReason = "酒馆已经满级";
            }
            else if (upgradeReason == null && session.UpgradedThisRound)
            {
                upgradeReason = "本回合已经升级过酒馆";
            }
            else if (upgradeReason == null &&
                     session.Gold < session.CurrentUpgradeCost)
            {
                upgradeReason = InsufficientGoldReason;
            }

            var selectedBattleCard = GetCard(
                session.Collection.Battle,
                selectedBattleIndex);
            var sellReason = actionDisabledReason;
            if (sellReason == null && selectedBattleCard == null)
            {
                sellReason = "请选择战斗区随从";
            }
            else if (sellReason == null &&
                     (selectedBattleCard.CardType != ShopCardType.Minion ||
                      selectedBattleCard.Minion.IsToken))
            {
                sellReason = "该随从不能出售";
            }

            var endReason = actionDisabledReason;
            if (endReason == null && !ownsSession)
            {
                endReason = "当前流程不能结束商店";
            }

            return new ShopButtonStates
            {
                Refresh = CreateButton(
                    session.FreeRefreshes > 0
                        ? $"免费刷新（{session.FreeRefreshes}）"
                        : $"刷新（{ShopEconomyRules.RefreshCost} 金币）",
                    refreshReason),
                Freeze = CreateButton(
                    session.IsFrozen ? "解冻" : "冻结",
                    actionDisabledReason,
                    session.IsFrozen),
                Upgrade = CreateButton(
                    session.TavernTier >= ShopEconomyRules.MaximumTavernTier
                        ? "酒馆已满级"
                        : $"升级（{session.CurrentUpgradeCost} 金币）",
                    upgradeReason),
                Sell = CreateButton(
                    $"出售（+{ShopEconomyRules.MinionSellValue} 金币）",
                    sellReason),
                EndShop = CreateButton("进入战斗", endReason)
            };
        }

        private static ShopActionButtonState CreateButton(
            string text,
            string disabledReason,
            bool isActive = false)
        {
            return new ShopActionButtonState
            {
                Text = text,
                IsVisible = true,
                IsInteractable = disabledReason == null,
                IsActive = isActive,
                DisabledReason = disabledReason
            };
        }

        private static CardDetailPanelState BuildDetailPanel(
            ShopSession session,
            ShopScreenState state,
            int selectedHandIndex,
            int selectedBattleIndex,
            int selectedEffectTargetIndex,
            string actionDisabledReason)
        {
            var selectedHandCard = GetCard(
                session.Collection.Bench,
                selectedHandIndex);
            if (selectedHandCard != null)
            {
                var model = FindVisibleHandCard(state.HandCards, selectedHandIndex) ??
                            BuildOwnedCard(
                                selectedHandCard,
                                true,
                                actionDisabledReason);
                return CreateDetailPanel(
                    model,
                    ShopCardLocation.Hand,
                    selectedHandIndex);
            }

            var detailBattleIndex = selectedBattleIndex >= 0
                ? selectedBattleIndex
                : selectedEffectTargetIndex;
            var selectedBattleCard = GetCard(
                session.Collection.Battle,
                detailBattleIndex);
            if (selectedBattleCard == null)
            {
                return new CardDetailPanelState();
            }

            var battleModel = state.BattleCards[detailBattleIndex] ??
                              BuildOwnedCard(
                                  selectedBattleCard,
                                  true,
                                  actionDisabledReason);
            return CreateDetailPanel(
                battleModel,
                ShopCardLocation.Battle,
                detailBattleIndex);
        }

        private static CardDetailPanelState CreateDetailPanel(
            CardViewModel card,
            ShopCardLocation location,
            int slotIndex)
        {
            return new CardDetailPanelState
            {
                Card = card,
                Location = location,
                SlotIndex = slotIndex,
                Statuses = BuildDetailStatuses(card)
            };
        }

        private static CardDetailStatusState[] BuildDetailStatuses(
            CardViewModel card)
        {
            var statuses = new List<CardDetailStatusState>();
            if (card.IsMinion)
            {
                var attackDelta = card.Attack - card.BaseAttack;
                var healthDelta = card.Health - card.BaseHealth;
                if (attackDelta != 0 || healthDelta != 0)
                {
                    statuses.Add(new CardDetailStatusState
                    {
                        Type = CardDetailStatusType.Growth,
                        Label = "成长",
                        Description = $"攻击 {FormatDelta(attackDelta)}，生命 {FormatDelta(healthDelta)}"
                    });
                }
            }

            if (card.HasShield)
            {
                statuses.Add(new CardDetailStatusState
                {
                    Type = CardDetailStatusType.PermanentShield,
                    Label = "永久护盾",
                    Description = "每场战斗开始时拥有护盾"
                });
            }

            if (card.HasNextCombatShield)
            {
                statuses.Add(new CardDetailStatusState
                {
                    Type = CardDetailStatusType.NextCombatShield,
                    Label = "下场护盾",
                    Description = "仅在下一场战斗开始时获得护盾"
                });
            }

            if (card.IsTemporary)
            {
                statuses.Add(new CardDetailStatusState
                {
                    Type = CardDetailStatusType.Temporary,
                    Label = "临时",
                    Description = "商店阶段结束时移除"
                });
            }

            return statuses.ToArray();
        }

        private static string ResolveBlockReason(
            ShopSession session,
            RunSession runSession,
            bool ownsSession)
        {
            if (session.PendingDiscover != null)
            {
                return "请先完成发现选择";
            }

            if (session.PendingChoice != null)
            {
                return "请先完成效果选择";
            }

            if (ownsSession && runSession.State.PendingCardRewards.Count > 0)
            {
                return "请先领取或跳过待领取奖励";
            }

            return null;
        }

        private static string ResolveActionDisabledReason(
            ShopSession session,
            string blockReason)
        {
            if (blockReason != null)
            {
                return blockReason;
            }

            return session.IsShopOpen ? null : ShopClosedReason;
        }

        private static CardViewModel FindVisibleHandCard(
            HandCardsState handCards,
            int slotIndex)
        {
            foreach (var slot in handCards.VisibleSlots)
            {
                if (slot.SlotIndex == slotIndex)
                {
                    return slot.Card;
                }
            }

            return null;
        }

        private static ShopCardInstance GetCard(
            IReadOnlyList<ShopCardInstance> cards,
            int index)
        {
            return index >= 0 && index < cards.Count ? cards[index] : null;
        }

        private static int CountCards(IReadOnlyList<ShopCardInstance> cards)
        {
            var count = 0;
            for (var i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static string FormatDelta(int value)
        {
            return value >= 0 ? "+" + value : value.ToString();
        }
    }
}

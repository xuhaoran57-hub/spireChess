using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SpireChess.Config;
using SpireChess.Shop;
using SpireChess.UI;
using SpireChess.UI.Shop;

namespace SpireChess.Tests.EditMode
{
    public sealed class ShopScreenStateBuilderTests
    {
        [Test]
        public void Build_RejectsNullSession()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ShopScreenStateBuilder.Build(null));
        }

        [Test]
        public void Build_MapsOpenShopEconomyOffersAndEmptySlots()
        {
            var session = CreateSession();
            Assert.That(session.StartRound(1).Success, Is.True);

            var state = ShopScreenStateBuilder.Build(
                session,
                statusMessage: "商店已开启");

            Assert.Multiple(() =>
            {
                Assert.That(state.Round, Is.EqualTo(1));
                Assert.That(state.Gold, Is.EqualTo(3));
                Assert.That(state.TavernTier, Is.EqualTo(1));
                Assert.That(state.UpgradeCost, Is.EqualTo(5));
                Assert.That(state.RefreshCount, Is.Zero);
                Assert.That(state.FreeRefreshes, Is.Zero);
                Assert.That(state.IsShopOpen, Is.True);
                Assert.That(state.StatusMessage, Is.EqualTo("商店已开启"));
                Assert.That(state.MinionOffers, Has.Length.EqualTo(2));
                Assert.That(state.MinionOffers.All(card => card != null), Is.True);
                Assert.That(state.MinionOffers.All(card =>
                    card.DisplayMode == CardDisplayMode.Full &&
                    card.ShowCost && card.IsInteractable), Is.True);
                Assert.That(state.SpellOffer, Is.Not.Null);
                Assert.That(state.SpellOffer.IsInteractable, Is.True);
                Assert.That(state.BattleCards, Has.Length.EqualTo(5));
                Assert.That(state.BattleCards.All(card => card == null), Is.True);
                Assert.That(state.HandCards.Count, Is.Zero);
                Assert.That(state.HandCards.Limit, Is.EqualTo(5));
                Assert.That(state.HandCards.VisibleSlots, Has.Length.EqualTo(5));
                Assert.That(state.HandCards.VisibleSlots.Select(slot => slot.SlotIndex),
                    Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
                Assert.That(state.HandCards.VisibleSlots.All(slot => slot.IsEmpty), Is.True);
                Assert.That(state.Buttons.Refresh.Text, Is.EqualTo("刷新（1 金币）"));
                Assert.That(state.Buttons.Refresh.IsInteractable, Is.True);
                Assert.That(state.Buttons.Upgrade.IsInteractable, Is.False);
                Assert.That(state.Buttons.Upgrade.DisabledReason, Is.EqualTo("金币不足"));
                Assert.That(state.Buttons.EndShop.IsInteractable, Is.False);
                Assert.That(state.DetailPanel.IsVisible, Is.False);
            });
        }

        [Test]
        public void Build_MapsFreeRefreshFreezeAndOneUpgradePerRound()
        {
            var session = CreateSession();
            Assert.That(session.StartRound(1).Success, Is.True);
            session.GrantFreeRefreshes(2);
            session.GrantGold(2);
            Assert.That(session.ToggleFreeze().Success, Is.True);
            Assert.That(session.UpgradeTavern().Success, Is.True);

            var state = ShopScreenStateBuilder.Build(session);

            Assert.Multiple(() =>
            {
                Assert.That(state.Gold, Is.Zero);
                Assert.That(state.TavernTier, Is.EqualTo(2));
                Assert.That(state.IsFrozen, Is.True);
                Assert.That(state.Buttons.Refresh.Text, Is.EqualTo("免费刷新（2）"));
                Assert.That(state.Buttons.Refresh.IsInteractable, Is.True);
                Assert.That(state.Buttons.Freeze.Text, Is.EqualTo("解冻"));
                Assert.That(state.Buttons.Freeze.IsActive, Is.True);
                Assert.That(state.Buttons.Upgrade.Text, Is.EqualTo("升级（7 金币）"));
                Assert.That(state.Buttons.Upgrade.IsInteractable, Is.False);
                Assert.That(state.Buttons.Upgrade.DisabledReason,
                    Is.EqualTo("本回合已经升级过酒馆"));
            });
        }

        [Test]
        public void Build_MaximumTavernTierDisablesUpgradeWithExplicitReason()
        {
            var session = CreateSession();
            var upgradeGold = new[] { 2, 3, 4, 4 };
            for (var round = 1; round <= upgradeGold.Length; round++)
            {
                Assert.That(session.StartRound(round).Success, Is.True);
                session.GrantGold(upgradeGold[round - 1]);
                Assert.That(session.UpgradeTavern().Success, Is.True);
                if (round < upgradeGold.Length)
                {
                    Assert.That(session.EndRound().Success, Is.True);
                }
            }

            var state = ShopScreenStateBuilder.Build(session);

            Assert.Multiple(() =>
            {
                Assert.That(state.TavernTier,
                    Is.EqualTo(ShopEconomyRules.MaximumTavernTier));
                Assert.That(state.UpgradeCost, Is.Zero);
                Assert.That(state.Buttons.Upgrade.Text, Is.EqualTo("酒馆已满级"));
                Assert.That(state.Buttons.Upgrade.IsInteractable, Is.False);
                Assert.That(state.Buttons.Upgrade.DisabledReason,
                    Is.EqualTo("酒馆已经满级"));
            });
        }

        [Test]
        public void Build_FullHandDisablesPurchasesAndMapsTemporaryDetail()
        {
            var session = CreateSession();
            for (var i = 0; i < ShopEconomyRules.BenchSlotCount; i++)
            {
                Assert.That(session.Collection.TryAddToBench(
                    ShopCardInstance.CreateSpell(
                        "temporary_" + i,
                        CreateSpell("temporary_" + i),
                        expiresAtShopEnd: true),
                    out _), Is.True);
            }

            Assert.That(session.StartRound(1).Success, Is.True);

            var state = ShopScreenStateBuilder.Build(
                session,
                selectedHandIndex: 2);

            Assert.Multiple(() =>
            {
                Assert.That(state.HandCards.Count, Is.EqualTo(5));
                Assert.That(state.HandCards.VisibleSlots[2].Card.IsSelected, Is.True);
                Assert.That(state.MinionOffers.Where(card => card != null)
                    .All(card => !card.IsInteractable &&
                                 card.DisabledReason == "手牌已满"), Is.True);
                Assert.That(state.SpellOffer.IsInteractable, Is.False);
                Assert.That(state.SpellOffer.DisabledReason, Is.EqualTo("手牌已满"));
                Assert.That(state.Buttons.Refresh.IsInteractable, Is.True);
                Assert.That(state.DetailPanel.IsVisible, Is.True);
                Assert.That(state.DetailPanel.Location, Is.EqualTo(ShopCardLocation.Hand));
                Assert.That(state.DetailPanel.SlotIndex, Is.EqualTo(2));
                Assert.That(state.DetailPanel.Statuses.Select(status => status.Type),
                    Is.EqualTo(new[] { CardDetailStatusType.Temporary }));
            });
        }

        [Test]
        public void Build_SelectedBattleCardMapsGrowthAndShieldDetails()
        {
            var minion = CreateMinion("owned");
            var session = CreateSession(new[] { minion });
            var owned = ShopCardInstance.CreateMinion(
                "owned_001",
                minion,
                permanentAttackBonus: 2,
                permanentHealthBonus: 3,
                permanentKeywords: new[] { "Shield" });
            Assert.That(session.Collection.TryAddToBench(owned, out var minionIndex),
                Is.True);
            Assert.That(session.StartRound(1).Success, Is.True);
            Assert.That(session.PlayMinion(minionIndex, 0).Success, Is.True);
            Assert.That(session.Collection.TryAddToBench(
                ShopCardInstance.CreateSpell("ward_001", CreateNextCombatShieldSpell()),
                out var spellIndex), Is.True);
            Assert.That(session.UseSpell(spellIndex, 0).Success, Is.True);

            var state = ShopScreenStateBuilder.Build(
                session,
                selectedBattleIndex: 0,
                selectedEffectTargetIndex: 0);

            Assert.Multiple(() =>
            {
                Assert.That(state.BattleCards[0].IsSelected, Is.True);
                Assert.That(state.BattleCards[0].Attack, Is.EqualTo(4));
                Assert.That(state.BattleCards[0].Health, Is.EqualTo(6));
                Assert.That(state.DetailPanel.Location,
                    Is.EqualTo(ShopCardLocation.Battle));
                Assert.That(state.DetailPanel.SlotIndex, Is.Zero);
                Assert.That(state.DetailPanel.Card,
                    Is.SameAs(state.BattleCards[0]));
                Assert.That(state.DetailPanel.Statuses.Select(status => status.Type),
                    Is.EqualTo(new[]
                    {
                        CardDetailStatusType.Growth,
                        CardDetailStatusType.PermanentShield,
                        CardDetailStatusType.NextCombatShield
                    }));
                Assert.That(state.DetailPanel.Statuses[0].Description,
                    Is.EqualTo("攻击 +2，生命 +3"));
                Assert.That(state.Buttons.Sell.IsInteractable, Is.True);
            });
        }

        [Test]
        public void Build_PendingDiscoverBlocksCardsAndEveryAction()
        {
            var minions = new[]
            {
                CreateMinion("candidate_a"),
                CreateMinion("candidate_b"),
                CreateMinion("candidate_c"),
                CreateMinion("candidate_d")
            };
            var rewardSpell = CreateTripleRewardSpell();
            var session = CreateSession(minions, new[] { rewardSpell });
            Assert.That(session.Collection.TryAddToBench(
                ShopCardInstance.CreateSpell("reward_001", rewardSpell),
                out var rewardIndex), Is.True);
            Assert.That(session.StartRound(1).Success, Is.True);
            Assert.That(session.UseSpell(rewardIndex).Success, Is.True);
            Assert.That(session.PendingDiscover, Is.Not.Null);

            var state = ShopScreenStateBuilder.Build(session);

            Assert.Multiple(() =>
            {
                Assert.That(state.IsInteractionBlocked, Is.True);
                Assert.That(state.BlockReason, Is.EqualTo("请先完成发现选择"));
                Assert.That(state.MinionOffers.Where(card => card != null)
                    .All(card => !card.IsInteractable &&
                                 card.DisabledReason == state.BlockReason), Is.True);
                Assert.That(state.HandCards.VisibleSlots[rewardIndex].Card.IsInteractable,
                    Is.False);
                Assert.That(state.Buttons.Refresh.IsInteractable, Is.False);
                Assert.That(state.Buttons.Freeze.IsInteractable, Is.False);
                Assert.That(state.Buttons.Upgrade.IsInteractable, Is.False);
                Assert.That(state.Buttons.Sell.IsInteractable, Is.False);
                Assert.That(state.Buttons.EndShop.IsInteractable, Is.False);
                Assert.That(state.Buttons.Refresh.DisabledReason,
                    Is.EqualTo(state.BlockReason));
            });
        }

        private static ShopSession CreateSession(
            IEnumerable<MinionConfig> minions = null,
            IEnumerable<SpellConfig> spells = null)
        {
            return new ShopSession(
                minions ?? new[]
                {
                    CreateMinion("minion_a"),
                    CreateMinion("minion_b")
                },
                spells ?? new[] { CreateSpell("spell_a") },
                new Random(17));
        }

        private static MinionConfig CreateMinion(string id)
        {
            return new MinionConfig
            {
                Id = id,
                Name = id,
                Description = id + " description",
                GoldenDescription = id + " golden description",
                Tier = 1,
                Race = "Starbound",
                Attack = 2,
                Health = 3,
                GoldenAttack = 4,
                GoldenHealth = 6,
                Enabled = true
            };
        }

        private static SpellConfig CreateSpell(string id)
        {
            return new SpellConfig
            {
                Id = id,
                Name = id,
                Description = id + " description",
                Tier = 1,
                SpellType = "Growth",
                UseTiming = new List<string> { "Shop" },
                Cost = 1,
                ShopEligible = true,
                Enabled = true
            };
        }

        private static SpellConfig CreateNextCombatShieldSpell()
        {
            var spell = CreateSpell("next_combat_shield");
            spell.SpellType = "Defense";
            spell.Effects.Add(new EffectConfig
            {
                Id = "next_combat_shield_manual",
                Trigger = "Manual",
                Action = "AddShield",
                Target = new TargetConfig
                {
                    Side = "Ally",
                    Scope = "Single",
                    Selector = "PlayerChoice",
                    Zones = new List<string> { "Battle" }
                },
                Value = new ValueConfig
                {
                    Duration = "NextCombat",
                    Keyword = "Shield"
                }
            });
            return spell;
        }

        private static SpellConfig CreateTripleRewardSpell()
        {
            var spell = CreateSpell(ShopSession.TripleDiscoveryRewardSpellId);
            spell.SpellType = "Discover";
            spell.ShopEligible = false;
            spell.Effects.Add(new EffectConfig
            {
                Id = "triple_discovery",
                Trigger = "Manual",
                Action = "DiscoverMinion",
                Discover = new DiscoverConfig
                {
                    CardType = "Minion",
                    TierMode = "ExactCurrentTavernTier",
                    Count = 3,
                    Pick = 1,
                    IncludeToken = false,
                    IncludeDisabled = false,
                    RequireGolden = false
                }
            });
            return spell;
        }
    }
}

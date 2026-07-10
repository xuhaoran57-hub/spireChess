using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SpireChess.Config;
using SpireChess.Shop;

namespace SpireChess.Tests
{
    public sealed class ShopSessionTests
    {
        [Test]
        public void EconomyRules_MatchConfirmedValues()
        {
            Assert.That(
                Enumerable.Range(1, 10).Select(ShopEconomyRules.GetRoundBudget),
                Is.EqualTo(new[] { 3, 4, 5, 6, 7, 8, 9, 10, 10, 10 }));
            Assert.That(
                Enumerable.Range(1, 5).Select(ShopEconomyRules.GetMinionSlotCount),
                Is.EqualTo(new[] { 2, 2, 3, 3, 4 }));
            Assert.That(
                Enumerable.Range(1, 5).Select(ShopEconomyRules.GetPoolCopiesPerMinion),
                Is.EqualTo(new[] { 8, 7, 6, 5, 4 }));
            Assert.That(
                Enumerable.Range(1, 4).Select(ShopEconomyRules.GetUpgradeBaseCost),
                Is.EqualTo(new[] { 5, 7, 9, 10 }));
        }

        [Test]
        public void MinionPool_DrawsByRemainingPhysicalCopiesAndReturnsThem()
        {
            var first = CreateMinion("first");
            var second = CreateMinion("second");
            var pool = new MinionPool(new[] { first, second });

            var drawn = pool.Draw(1, new SequenceRandom(8));

            Assert.That(drawn, Is.SameAs(second));
            Assert.That(pool.GetRemainingCopies("first"), Is.EqualTo(8));
            Assert.That(pool.GetRemainingCopies("second"), Is.EqualTo(7));
            Assert.That(pool.Return("second"), Is.True);
            Assert.That(pool.GetRemainingCopies("second"), Is.EqualTo(8));
        }

        [Test]
        public void MinionPool_ExcludesDisabledMinionsAndTokens()
        {
            var disabled = CreateMinion("disabled");
            disabled.Enabled = false;
            var token = CreateMinion("token");
            token.IsToken = true;
            var pool = new MinionPool(new[] { disabled, token });

            Assert.That(pool.Draw(1, new SequenceRandom()), Is.Null);
            Assert.That(pool.GetRemainingCopies("disabled"), Is.EqualTo(0));
            Assert.That(pool.GetRemainingCopies("token"), Is.EqualTo(0));
        }

        [Test]
        public void StartRound_UsesEmptyCollectionAndAutomaticStockDoesNotCountAsRefresh()
        {
            var session = CreateSession();
            var events = new List<ShopEventData>();
            session.EventRaised += events.Add;

            Assert.That(session.Collection.Bench.All(card => card == null), Is.True);
            Assert.That(session.Collection.Battle.All(card => card == null), Is.True);
            Assert.That(session.MinionOffers.All(card => card == null), Is.True);

            session.StartNextRound();

            Assert.That(session.Round, Is.EqualTo(1));
            Assert.That(session.Gold, Is.EqualTo(3));
            Assert.That(session.MinionOffers.Count, Is.EqualTo(2));
            Assert.That(session.MinionOffers.All(card => card != null), Is.True);
            Assert.That(session.SpellOffer, Is.Not.Null);
            Assert.That(session.RefreshCount, Is.EqualTo(0));
            Assert.That(events.Select(data => data.Type),
                Is.EqualTo(new[] { ShopEventType.OnShopPhaseStart }));
        }

        [Test]
        public void Refresh_ConsumesPaidThenFreeRefreshAndCountsOnlyActiveRefreshes()
        {
            var session = CreateSession();
            var refreshEvents = new List<ShopEventData>();
            session.EventRaised += data =>
            {
                if (data.Type == ShopEventType.OnRefresh)
                {
                    refreshEvents.Add(data);
                }
            };
            session.StartNextRound();

            Assert.That(session.Refresh().Success, Is.True);
            session.GrantFreeRefreshes(1);
            Assert.That(session.Refresh().Success, Is.True);

            Assert.That(session.Gold, Is.EqualTo(2));
            Assert.That(session.RefreshCount, Is.EqualTo(2));
            Assert.That(refreshEvents.Select(data => data.Cost), Is.EqualTo(new[] { 1, 0 }));
            Assert.That(refreshEvents.Select(data => data.RefreshCount), Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void BuyMinion_PaysThreeGoldAndAddsCardToBench()
        {
            var session = CreateSession();
            session.StartNextRound();

            var result = session.BuyMinion(0);

            Assert.That(result.Success, Is.True);
            Assert.That(session.Gold, Is.EqualTo(0));
            Assert.That(session.Collection.Bench[result.BenchIndex].CardType,
                Is.EqualTo(ShopCardType.Minion));
            Assert.That(session.MinionOffers[0], Is.Null);
        }

        [Test]
        public void BuySpell_PaysOneGoldAndSpellCannotBeSoldOrMovedToBattle()
        {
            var session = CreateSession();
            session.StartNextRound();
            var result = session.BuySpell();

            var sellResult = session.SellBenchMinion(result.BenchIndex);
            var playResult = session.PlayMinion(result.BenchIndex, 0);

            Assert.That(result.Success, Is.True);
            Assert.That(session.Gold, Is.EqualTo(2));
            Assert.That(sellResult.Error, Is.EqualTo(ShopOperationError.InvalidCardLocation));
            Assert.That(session.Collection.Bench[result.BenchIndex], Is.Not.Null);
            Assert.That(playResult.Error, Is.EqualTo(ShopOperationError.InvalidCardType));
        }

        [Test]
        public void FullBench_PreventsPurchaseWithoutChangingGoldOrOffer()
        {
            var session = CreateSession();
            FillBenchWithSpells(session.Collection);
            session.StartNextRound();
            var offeredId = session.MinionOffers[0].Id;
            var remainingCopies = session.MinionPool.GetRemainingCopies(offeredId);

            var result = session.BuyMinion(0);

            Assert.That(result.Error, Is.EqualTo(ShopOperationError.BenchFull));
            Assert.That(session.Gold, Is.EqualTo(3));
            Assert.That(session.MinionOffers[0].Id, Is.EqualTo(offeredId));
            Assert.That(session.MinionPool.GetRemainingCopies(offeredId),
                Is.EqualTo(remainingCopies));
        }

        [Test]
        public void SellMinion_ReturnsCopyAndGoldCanExceedTen()
        {
            var session = CreateSession();
            session.StartNextRound();
            var boughtId = session.MinionOffers[0].Id;
            var purchase = session.BuyMinion(0);
            var beforeSaleCopies = session.MinionPool.GetRemainingCopies(boughtId);
            session.PlayMinion(purchase.BenchIndex, 0);
            session.GrantGold(10);

            var sale = session.SellBattleMinion(0);

            Assert.That(sale.Success, Is.True);
            Assert.That(session.Gold, Is.EqualTo(11));
            Assert.That(session.MinionPool.GetRemainingCopies(boughtId),
                Is.EqualTo(beforeSaleCopies + 1));

            session.EndRound();
            session.StartNextRound();
            Assert.That(session.Gold, Is.EqualTo(4));
        }

        [Test]
        public void TokenMinion_CannotBeSold()
        {
            var session = CreateSession();
            var token = CreateMinion("token");
            token.IsToken = true;
            session.Collection.TryAddToBench(
                ShopCardInstance.CreateMinion("token_instance", token),
                out var benchIndex);
            session.StartNextRound();
            session.PlayMinion(benchIndex, 0);

            var result = session.SellBattleMinion(0);

            Assert.That(result.Error, Is.EqualTo(ShopOperationError.InvalidCardType));
            Assert.That(session.Gold, Is.EqualTo(3));
            Assert.That(session.Collection.Battle[0], Is.Not.Null);
        }

        [Test]
        public void UpgradeDiscount_DropsAfterSkippedRoundAndAllowsOnlyOneUpgradePerRound()
        {
            var session = CreateSession();
            session.StartNextRound();
            Assert.That(session.CurrentUpgradeCost, Is.EqualTo(5));
            session.EndRound();
            session.StartNextRound();

            var firstUpgrade = session.UpgradeTavern();
            var secondUpgrade = session.UpgradeTavern();

            Assert.That(firstUpgrade.Success, Is.True);
            Assert.That(session.TavernTier, Is.EqualTo(2));
            Assert.That(session.Gold, Is.EqualTo(0));
            Assert.That(secondUpgrade.Error,
                Is.EqualTo(ShopOperationError.AlreadyUpgradedThisRound));
            Assert.That(session.CurrentUpgradeCost, Is.EqualTo(7));
        }

        [Test]
        public void UpgradeToTierThree_AddsAnEmptyOfferSlotUntilRestock()
        {
            var session = CreateSession();
            session.StartNextRound();
            session.GrantGold(2);
            session.UpgradeTavern();
            session.EndRound();
            session.StartNextRound();
            session.GrantGold(3);

            var result = session.UpgradeTavern();

            Assert.That(result.Success, Is.True);
            Assert.That(session.TavernTier, Is.EqualTo(3));
            Assert.That(session.MinionOffers.Count, Is.EqualTo(3));
            Assert.That(session.MinionOffers[2], Is.Null);
        }

        [Test]
        public void FrozenShop_KeepsOffersRestocksEmptySlotsAndThenUnfreezes()
        {
            var session = CreateSession();
            session.StartNextRound();
            var retainedOffer = session.MinionOffers[1];
            session.BuyMinion(0);
            session.ToggleFreeze();
            session.EndRound();

            session.StartNextRound();

            Assert.That(session.MinionOffers[0], Is.Not.Null);
            Assert.That(session.MinionOffers[1], Is.SameAs(retainedOffer));
            Assert.That(session.SpellOffer, Is.Not.Null);
            Assert.That(session.IsFrozen, Is.False);
            Assert.That(session.RefreshCount, Is.EqualTo(0));
        }

        [Test]
        public void UnfrozenShop_ReturnsAllUnsoldMinionsToPool()
        {
            var first = CreateMinion("first");
            var second = CreateMinion("second");
            var session = new ShopSession(
                new[] { first, second },
                new[] { CreateSpell("spell") },
                new SequenceRandom());
            session.StartNextRound();

            Assert.That(
                session.MinionPool.GetRemainingCopies("first") +
                session.MinionPool.GetRemainingCopies("second"),
                Is.EqualTo(14));

            session.EndRound();

            Assert.That(
                session.MinionPool.GetRemainingCopies("first") +
                session.MinionPool.GetRemainingCopies("second"),
                Is.EqualTo(16));
        }

        [Test]
        public void ShopOperations_RaiseAllDeclaredEventsWithOperationContext()
        {
            var session = CreateSession();
            var events = new List<ShopEventData>();
            session.EventRaised += events.Add;

            session.StartNextRound();
            var purchase = session.BuyMinion(0);
            session.PlayMinion(purchase.BenchIndex, 0);
            session.SellBattleMinion(0);
            session.Refresh();
            session.GrantGold(5);
            session.UpgradeTavern();
            session.EndRound();

            Assert.That(events.Select(data => data.Type), Is.EqualTo(new[]
            {
                ShopEventType.OnShopPhaseStart,
                ShopEventType.OnBuy,
                ShopEventType.OnPlay,
                ShopEventType.OnSell,
                ShopEventType.OnRefresh,
                ShopEventType.OnTavernUpgraded,
                ShopEventType.OnShopPhaseEnd
            }));
            Assert.That(events[1].Cost, Is.EqualTo(3));
            Assert.That(events[1].Card, Is.Not.Null);
            Assert.That(events[3].Cost, Is.EqualTo(-1));
            Assert.That(events[4].RefreshCount, Is.EqualTo(1));
            Assert.That(events[5].PreviousTavernTier, Is.EqualTo(1));
            Assert.That(events[5].TavernTier, Is.EqualTo(2));
        }

        [Test]
        public void Formation_AllowsPlayingToEmptySlotAndBattleRepositionOnly()
        {
            var session = CreateSession();
            session.Collection.TryAddToBench(
                ShopCardInstance.CreateMinion("minion", CreateMinion("minion")),
                out var minionIndex);
            session.Collection.TryAddToBench(
                ShopCardInstance.CreateSpell("spell", CreateSpell("spell")),
                out var spellIndex);
            session.Collection.TryAddToBench(
                ShopCardInstance.CreateMinion("second", CreateMinion("second")),
                out var secondMinionIndex);
            session.StartNextRound();

            Assert.That(session.PlayMinion(minionIndex, 0).Success, Is.True);
            Assert.That(session.Collection.Battle[0].ConfigId, Is.EqualTo("minion"));
            Assert.That(session.PlayMinion(spellIndex, 1).Error,
                Is.EqualTo(ShopOperationError.InvalidCardType));
            Assert.That(session.PlayMinion(secondMinionIndex, 0).Error,
                Is.EqualTo(ShopOperationError.OccupiedBattleSlot));
            Assert.That(session.RepositionBattleMinion(0, 1).Success, Is.True);
            Assert.That(session.Collection.Battle[1].ConfigId, Is.EqualTo("minion"));
            Assert.That(session.SellBenchMinion(secondMinionIndex).Error,
                Is.EqualTo(ShopOperationError.InvalidCardLocation));
        }

        private static ShopSession CreateSession()
        {
            return new ShopSession(
                new[] { CreateMinion("minion_a"), CreateMinion("minion_b") },
                new[] { CreateSpell("spell_a"), CreateSpell("spell_tier_2", 2) },
                new SequenceRandom());
        }

        private static MinionConfig CreateMinion(string id, int tier = 1)
        {
            return new MinionConfig
            {
                Id = id,
                Name = id,
                Tier = tier,
                Attack = 1,
                Health = 1,
                GoldenAttack = 2,
                GoldenHealth = 2,
                Enabled = true
            };
        }

        private static SpellConfig CreateSpell(string id, int tier = 1)
        {
            return new SpellConfig
            {
                Id = id,
                Name = id,
                Tier = tier,
                SpellType = "Growth",
                UseTiming = new List<string> { "Shop" },
                Cost = 1,
                ShopEligible = true,
                Enabled = true
            };
        }

        private static void FillBenchWithSpells(PlayerCollection collection)
        {
            for (var i = 0; i < ShopEconomyRules.BenchSlotCount; i++)
            {
                collection.TryAddToBench(
                    ShopCardInstance.CreateSpell($"spell_{i}", CreateSpell($"spell_{i}")),
                    out _);
            }
        }

        private sealed class SequenceRandom : Random
        {
            private readonly Queue<int> values;

            public SequenceRandom(params int[] values)
            {
                this.values = new Queue<int>(values);
            }

            public override int Next(int maxValue)
            {
                if (maxValue <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(maxValue));
                }

                var value = values.Count > 0 ? values.Dequeue() : 0;
                return Math.Abs(value % maxValue);
            }
        }
    }
}

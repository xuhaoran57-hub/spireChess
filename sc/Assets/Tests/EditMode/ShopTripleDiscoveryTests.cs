using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SpireChess.Config;
using SpireChess.Shop;

namespace SpireChess.Tests
{
    public sealed class ShopTripleDiscoveryTests
    {
        [Test]
        public void DiscoverPool_UsesRemainingPhysicalCopyWeights()
        {
            var first = CreateMinion("first");
            var second = CreateMinion("second");
            var pool = new MinionPool(new[] { first, second });
            var drawRandom = new SequenceRandom();
            for (var i = 0; i < 7; i++)
            {
                Assert.That(pool.Draw(1, drawRandom), Is.SameAs(first));
            }

            var reserved = pool.ReserveDistinctAtTier(
                1,
                1,
                new SequenceRandom(2));

            Assert.That(reserved.Single(), Is.SameAs(second));
            Assert.That(pool.GetRemainingCopies(first.Id), Is.EqualTo(1));
            Assert.That(pool.GetRemainingCopies(second.Id), Is.EqualTo(7));
        }

        [Test]
        public void Triple_UsesBenchBeforeBattleAndInheritsPermanentState()
        {
            var minion = CreateMinion("triple", 1, 6, 8);
            minion.Keywords.Add("Taunt");
            var session = CreateSession(new[] { minion });
            var benchFirst = ShopCardInstance.CreateMinion(
                "bench_first", minion, permanentAttackBonus: 1,
                permanentHealthBonus: 2, permanentKeywords: new[] { "Shield" });
            var benchSecond = ShopCardInstance.CreateMinion(
                "bench_second", minion, permanentAttackBonus: 2,
                permanentHealthBonus: 3, permanentKeywords: new[] { "Cleave" });
            var battleFirst = ShopCardInstance.CreateMinion(
                "battle_first", minion, permanentAttackBonus: 4,
                permanentHealthBonus: 5, permanentKeywords: new[] { "Shield" });
            var battleRemainder = ShopCardInstance.CreateMinion(
                "battle_remainder", minion, permanentAttackBonus: 20,
                permanentHealthBonus: 20);
            session.Collection.TryAddToBench(benchFirst, out _);
            session.Collection.TryAddToBench(benchSecond, out _);
            SeedBattle(session.Collection, 0, battleFirst);
            SeedBattle(session.Collection, 1, battleRemainder);
            var events = new List<ShopEventData>();
            session.EventRaised += events.Add;

            session.StartNextRound();

            var golden = session.Collection.Bench.Single(card => card != null && card.IsGolden);
            Assert.That(golden.ConfigId, Is.EqualTo("triple"));
            Assert.That(golden.PermanentAttackBonus, Is.EqualTo(7));
            Assert.That(golden.PermanentHealthBonus, Is.EqualTo(10));
            Assert.That(golden.CurrentAttack, Is.EqualTo(13));
            Assert.That(golden.CurrentHealth, Is.EqualTo(18));
            Assert.That(golden.PermanentKeywords, Is.EquivalentTo(new[] { "Shield", "Cleave" }));
            Assert.That(golden.TripleDiscoveryPending, Is.True);
            Assert.That(golden.PoolCopiesHeld, Is.EqualTo(3));
            Assert.That(session.Collection.Battle[0], Is.Null);
            Assert.That(session.Collection.Battle[1], Is.SameAs(battleRemainder));
            Assert.That(events.Select(data => data.Type), Does.Contain(ShopEventType.OnTripleFormed));
        }

        [Test]
        public void Triple_DefersWithFullBenchAndResolvesWhenSpellCreatesSpace()
        {
            var minion = CreateMinion("deferred");
            var session = CreateSession(new[] { minion });
            for (var i = 0; i < ShopEconomyRules.BenchSlotCount; i++)
            {
                session.Collection.TryAddToBench(
                    ShopCardInstance.CreateSpell(
                        $"filler_{i}", CreateResourceSpell($"filler_{i}")),
                    out _);
            }

            SeedBattle(session.Collection, 0, ShopCardInstance.CreateMinion("a", minion));
            SeedBattle(session.Collection, 1, ShopCardInstance.CreateMinion("b", minion));
            SeedBattle(session.Collection, 2, ShopCardInstance.CreateMinion("c", minion));
            session.StartNextRound();

            Assert.That(session.Collection.Battle.Count(card => card != null), Is.EqualTo(3));
            Assert.That(session.Collection.Bench.Any(card => card != null && card.IsGolden), Is.False);

            var result = session.UseSpell(0);

            Assert.That(result.Success, Is.True);
            Assert.That(session.Collection.Battle.Take(3).All(card => card == null), Is.True);
            Assert.That(session.Collection.Bench.Count(card => card != null && card.IsGolden), Is.EqualTo(1));
        }

        [Test]
        public void GoldenPlay_GrantsRewardOnceAndClearsPendingFlag()
        {
            var minion = CreateMinion("golden");
            var session = CreateSession(new[] { minion });
            var golden = ShopCardInstance.CreateMinion(
                "golden_instance", minion, isGolden: true,
                tripleDiscoveryPending: true);
            session.Collection.TryAddToBench(golden, out var benchIndex);
            session.StartNextRound();
            var events = new List<ShopEventData>();
            session.EventRaised += events.Add;

            var firstPlay = session.PlayMinion(benchIndex, 0);

            Assert.That(firstPlay.Success, Is.True);
            Assert.That(golden.TripleDiscoveryPending, Is.False);
            Assert.That(session.Collection.Bench.Count(IsTripleReward), Is.EqualTo(1));
            Assert.That(events.Select(data => data.Type), Is.EqualTo(new[]
            {
                ShopEventType.OnPlay,
                ShopEventType.OnTripleRewardGranted
            }));

            var replayBenchIndex = MoveBattleToBenchForTest(session.Collection, 0);
            events.Clear();
            var replay = session.PlayMinion(replayBenchIndex, 1);

            Assert.That(replay.Success, Is.True);
            Assert.That(session.Collection.Bench.Count(IsTripleReward), Is.EqualTo(1));
            Assert.That(events.Select(data => data.Type), Is.EqualTo(new[] { ShopEventType.OnPlay }));
        }

        [Test]
        public void TripleDiscover_BlocksAllOtherOperationsAndCannotBeCancelled()
        {
            var minions = new[]
            {
                CreateMinion("candidate_a"),
                CreateMinion("candidate_b"),
                CreateMinion("candidate_c")
            };
            var session = CreateSession(minions);
            var reward = ShopCardInstance.CreateSpell(
                "reward", CreateTripleRewardSpell());
            session.Collection.TryAddToBench(reward, out var rewardIndex);
            session.StartNextRound();
            var before = minions.ToDictionary(
                minion => minion.Id,
                minion => session.MinionPool.GetRemainingCopies(minion.Id));

            var begin = session.UseSpell(rewardIndex);

            Assert.That(begin.Success, Is.True);
            Assert.That(session.PendingDiscover, Is.Not.Null);
            Assert.That(session.PendingDiscover.Candidates.Count, Is.EqualTo(3));
            Assert.That(session.PendingDiscover.Candidates.Select(card => card.Id).Distinct().Count(),
                Is.EqualTo(3));
            foreach (var candidate in session.PendingDiscover.Candidates)
            {
                Assert.That(session.MinionPool.GetRemainingCopies(candidate.Id),
                    Is.EqualTo(before[candidate.Id] - 1));
            }

            AssertBlocked(session.Refresh());
            AssertBlocked(session.BuyMinion(0));
            AssertBlocked(session.BuySpell());
            AssertBlocked(session.SellBenchMinion(rewardIndex));
            AssertBlocked(session.PlayMinion(rewardIndex, 0));
            AssertBlocked(session.RepositionBattleMinion(0, 1));
            AssertBlocked(session.UpgradeTavern());
            AssertBlocked(session.ToggleFreeze());
            AssertBlocked(session.EndRound());

            var cancel = session.CancelDiscover();

            Assert.That(cancel.Success, Is.False);
            Assert.That(cancel.Error, Is.EqualTo(
                ShopOperationError.DiscoveryCannotBeCancelled));
            Assert.That(session.PendingDiscover, Is.Not.Null);
            Assert.That(session.Collection.Bench[rewardIndex], Is.SameAs(reward));
            foreach (var candidate in session.PendingDiscover.Candidates)
            {
                Assert.That(session.MinionPool.GetRemainingCopies(candidate.Id),
                    Is.EqualTo(before[candidate.Id] - 1));
            }

            AssertBlocked(session.Refresh());
            Assert.That(session.SelectDiscover(0).Success, Is.True);
            Assert.That(session.PendingDiscover, Is.Null);
            Assert.That(session.Refresh().Success, Is.True);
        }

        [Test]
        public void Discover_SelectConsumesSpellKeepsSelectedCopyAndReturnsOthers()
        {
            var minions = new[]
            {
                CreateMinion("candidate_a"),
                CreateMinion("candidate_b"),
                CreateMinion("candidate_c")
            };
            var session = CreateSession(minions);
            var reward = ShopCardInstance.CreateSpell(
                "reward", CreateTripleRewardSpell());
            session.Collection.TryAddToBench(reward, out var rewardIndex);
            session.StartNextRound();
            session.UseSpell(rewardIndex);
            var candidates = session.PendingDiscover.Candidates.ToList();
            var countsWhileReserved = minions.ToDictionary(
                minion => minion.Id,
                minion => session.MinionPool.GetRemainingCopies(minion.Id));
            var events = new List<ShopEventData>();
            session.EventRaised += events.Add;

            var result = session.SelectDiscover(1);

            Assert.That(result.Success, Is.True);
            Assert.That(session.PendingDiscover, Is.Null);
            Assert.That(session.Collection.Bench[rewardIndex].CardType,
                Is.EqualTo(ShopCardType.Minion));
            Assert.That(session.Collection.Bench[rewardIndex].ConfigId,
                Is.EqualTo(candidates[1].Id));
            for (var i = 0; i < candidates.Count; i++)
            {
                var expected = countsWhileReserved[candidates[i].Id] + (i == 1 ? 0 : 1);
                Assert.That(session.MinionPool.GetRemainingCopies(candidates[i].Id),
                    Is.EqualTo(expected));
            }
            Assert.That(events.Select(data => data.Type), Is.EqualTo(new[]
            {
                ShopEventType.OnSpellUsed,
                ShopEventType.OnDiscoverResolved
            }));
        }

        [Test]
        public void Discover_UsesCurrentExactTavernTierAtCastTime()
        {
            var minions = new[]
            {
                CreateMinion("tier_1", 1),
                CreateMinion("tier_2_a", 2),
                CreateMinion("tier_2_b", 2),
                CreateMinion("tier_2_c", 2)
            };
            var session = CreateSession(minions);
            session.Collection.TryAddToBench(
                ShopCardInstance.CreateSpell("reward", CreateTripleRewardSpell()),
                out var rewardIndex);
            session.StartNextRound();
            session.GrantGold(2);
            Assert.That(session.UpgradeTavern().Success, Is.True);

            var result = session.UseSpell(rewardIndex);

            Assert.That(result.Success, Is.True);
            Assert.That(session.PendingDiscover.Candidates.Count, Is.EqualTo(3));
            Assert.That(session.PendingDiscover.Candidates.All(card => card.Tier == 2), Is.True);
        }

        [Test]
        public void Discover_ShowsFewerDistinctCandidatesAndRejectsEmptyPool()
        {
            var twoCandidates = CreateSession(new[]
            {
                CreateMinion("only_a"),
                CreateMinion("only_b")
            });
            twoCandidates.Collection.TryAddToBench(
                ShopCardInstance.CreateSpell("reward", CreateTripleRewardSpell()),
                out var rewardIndex);
            twoCandidates.StartNextRound();

            Assert.That(twoCandidates.UseSpell(rewardIndex).Success, Is.True);
            Assert.That(twoCandidates.PendingDiscover.Candidates.Count, Is.EqualTo(2));

            var noTierTwo = CreateSession(new[] { CreateMinion("tier_1") });
            var reward = ShopCardInstance.CreateSpell("reward", CreateTripleRewardSpell());
            noTierTwo.Collection.TryAddToBench(reward, out rewardIndex);
            noTierTwo.StartNextRound();
            noTierTwo.GrantGold(2);
            noTierTwo.UpgradeTavern();

            var empty = noTierTwo.UseSpell(rewardIndex);

            Assert.That(empty.Error, Is.EqualTo(ShopOperationError.NoBenefit));
            Assert.That(noTierTwo.PendingDiscover, Is.Null);
            Assert.That(noTierTwo.Collection.Bench[rewardIndex], Is.SameAs(reward));
        }

        [Test]
        public void MinionDiscover_ExcludesSourceCardBeforePoolReservation()
        {
            var fateShuffler = CreateMinion("fate_shuffler");
            fateShuffler.Race = "Starbound";
            fateShuffler.Keywords.Add("Battlecry");
            fateShuffler.Effects.Add(new EffectConfig
            {
                Id = "fate_shuffler_play",
                Trigger = "OnPlay",
                Action = "DiscoverMinion",
                Discover = new DiscoverConfig
                {
                    CardType = "Minion",
                    TierMode = "ExactCurrentTavernTier",
                    Count = 3,
                    Pick = 1
                }
            });
            var firstStarbound = CreateMinion("starbound_a");
            firstStarbound.Race = "Starbound";
            var secondStarbound = CreateMinion("starbound_b");
            secondStarbound.Race = "Starbound";
            var wayfarer = CreateMinion("wayfarer");
            var session = CreateSession(new[]
            {
                fateShuffler,
                firstStarbound,
                secondStarbound,
                wayfarer
            });
            session.StartNextRound();
            var reward = session.ClaimRewardMinion(fateShuffler);
            Assert.That(reward.Success, Is.True);
            for (var refresh = 0; refresh < 3; refresh++)
            {
                Assert.That(session.Refresh().Success, Is.True);
            }
            var sourceCopiesBefore =
                session.MinionPool.GetRemainingCopies(fateShuffler.Id);

            var play = session.PlayMinion(reward.BenchIndex, 0);

            Assert.That(play.Success, Is.True);
            Assert.That(session.PendingChoice, Is.Not.Null);
            Assert.That(session.PendingChoice.Candidates.Select(value => value.Id),
                Is.EquivalentTo(new[] { "starbound_a", "starbound_b" }));
            Assert.That(session.PendingChoice.Candidates,
                Has.None.Matches<EffectChoiceCandidate>(value =>
                    value.Id == fateShuffler.Id));
            Assert.That(session.MinionPool.GetRemainingCopies(fateShuffler.Id),
                Is.EqualTo(sourceCopiesBefore));
        }

        [Test]
        public void Discover_InvalidSelectionKeepsPendingStateAndReservations()
        {
            var minions = new[]
            {
                CreateMinion("candidate_a"),
                CreateMinion("candidate_b")
            };
            var session = CreateSession(minions);
            session.Collection.TryAddToBench(
                ShopCardInstance.CreateSpell("reward", CreateTripleRewardSpell()),
                out var rewardIndex);
            session.StartNextRound();
            session.UseSpell(rewardIndex);
            var counts = minions.ToDictionary(
                minion => minion.Id,
                minion => session.MinionPool.GetRemainingCopies(minion.Id));

            var result = session.SelectDiscover(99);

            Assert.That(result.Error, Is.EqualTo(ShopOperationError.InvalidIndex));
            Assert.That(session.PendingDiscover, Is.Not.Null);
            Assert.That(session.Collection.Bench[rewardIndex].CardType,
                Is.EqualTo(ShopCardType.Spell));
            foreach (var minion in minions)
            {
                Assert.That(session.MinionPool.GetRemainingCopies(minion.Id),
                    Is.EqualTo(counts[minion.Id]));
            }
        }

        [Test]
        public void DiscoverSelection_CanCompleteAnotherTriple()
        {
            var minion = CreateMinion("candidate");
            var session = CreateSession(new[] { minion });
            session.Collection.TryAddToBench(
                ShopCardInstance.CreateMinion("first", minion), out _);
            session.Collection.TryAddToBench(
                ShopCardInstance.CreateMinion("second", minion), out _);
            session.Collection.TryAddToBench(
                ShopCardInstance.CreateSpell("reward", CreateTripleRewardSpell()),
                out var rewardIndex);
            session.StartNextRound();
            session.UseSpell(rewardIndex);

            var result = session.SelectDiscover(0);

            Assert.That(result.Success, Is.True);
            Assert.That(session.Collection.Bench.Count(card => card != null && card.IsGolden),
                Is.EqualTo(1));
            Assert.That(session.Collection.Bench.Single(card => card != null && card.IsGolden)
                .TripleDiscoveryPending, Is.True);
        }

        [Test]
        public void GoldenSale_ReturnsThreePhysicalCopies()
        {
            var minion = CreateMinion("pool_triple");
            var session = CreateSession(new[] { minion });
            session.StartNextRound();
            session.BuyMinion(0);
            session.GrantGold(3);
            session.BuyMinion(1);
            session.GrantGold(1);
            session.Refresh();
            session.GrantGold(3);
            var third = session.BuyMinion(0);
            var goldenIndex = session.Collection.Bench
                .Select((card, index) => new { card, index })
                .Single(item => item.card != null && item.card.IsGolden)
                .index;
            Assert.That(third.Success, Is.True);
            session.PlayMinion(goldenIndex, 0);
            var beforeSale = session.MinionPool.GetRemainingCopies(minion.Id);

            var result = session.SellBattleMinion(0);

            Assert.That(result.Success, Is.True);
            Assert.That(session.MinionPool.GetRemainingCopies(minion.Id),
                Is.EqualTo(beforeSale + 3));
        }

        private static ShopSession CreateSession(IEnumerable<MinionConfig> minions)
        {
            return new ShopSession(
                minions,
                new[] { CreateTripleRewardSpell() },
                new SequenceRandom());
        }

        private static MinionConfig CreateMinion(
            string id,
            int tier = 1,
            int goldenAttack = 2,
            int goldenHealth = 2)
        {
            return new MinionConfig
            {
                Id = id,
                Name = id,
                Tier = tier,
                Race = "Wayfarer",
                Attack = 1,
                Health = 1,
                GoldenAttack = goldenAttack,
                GoldenHealth = goldenHealth,
                Enabled = true
            };
        }

        private static SpellConfig CreateTripleRewardSpell()
        {
            var spell = new SpellConfig
            {
                Id = ShopSession.TripleDiscoveryRewardSpellId,
                Name = "Triple Discovery",
                Tier = 1,
                SpellType = "Discover",
                UseTiming = new List<string> { "Shop" },
                Cost = 1,
                ShopEligible = false,
                Enabled = true
            };
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

        private static SpellConfig CreateResourceSpell(string id)
        {
            var spell = new SpellConfig
            {
                Id = id,
                Name = id,
                Tier = 1,
                SpellType = "Economy",
                UseTiming = new List<string> { "Shop" },
                Cost = 1,
                ShopEligible = true,
                Enabled = true
            };
            spell.Effects.Add(new EffectConfig
            {
                Id = id,
                Trigger = "Manual",
                Action = "GainGold",
                Value = new ValueConfig { Amount = 1 }
            });
            return spell;
        }

        private static bool IsTripleReward(ShopCardInstance card)
        {
            return card != null &&
                card.CardType == ShopCardType.Spell &&
                card.ConfigId == ShopSession.TripleDiscoveryRewardSpellId;
        }

        private static void AssertBlocked(ShopOperationResult result)
        {
            Assert.That(result.Error, Is.EqualTo(ShopOperationError.DiscoveryPending));
        }

        private static void SeedBattle(
            PlayerCollection collection,
            int index,
            ShopCardInstance card)
        {
            GetSlots(collection, "battle")[index] = card;
        }

        private static int MoveBattleToBenchForTest(
            PlayerCollection collection,
            int battleIndex)
        {
            var battle = GetSlots(collection, "battle");
            var bench = GetSlots(collection, "bench");
            var targetIndex = Array.FindIndex(bench, card => card == null);
            bench[targetIndex] = battle[battleIndex];
            battle[battleIndex] = null;
            return targetIndex;
        }

        private static ShopCardInstance[] GetSlots(
            PlayerCollection collection,
            string fieldName)
        {
            return (ShopCardInstance[])typeof(PlayerCollection)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(collection);
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

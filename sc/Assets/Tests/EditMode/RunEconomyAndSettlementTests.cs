using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Run;
using SpireChess.Shop;

namespace SpireChess.Tests
{
    public sealed class RunEconomyAndSettlementTests
    {
        [Test]
        public void ExplicitEconomyClock_IsIdempotentAndCountsSkippedTurns()
        {
            var minion = CreateMinion("test", 1, 2, 2);
            var shop = new ShopSession(new[] { minion }, Array.Empty<SpellConfig>(), new Random(5));

            Assert.That(shop.StartRound(1).Success, Is.True);
            Assert.That(shop.Gold, Is.EqualTo(3));
            Assert.That(shop.StartRound(1).Success, Is.True);
            Assert.That(shop.Gold, Is.EqualTo(3));
            Assert.That(shop.EndRound().Success, Is.True);
            Assert.That(shop.AdvanceSkippedRound(2).Success, Is.True);
            Assert.That(shop.AdvanceSkippedRound(2).Success, Is.True);
            Assert.That(shop.StartRound(3).Success, Is.True);
            Assert.That(shop.Gold, Is.EqualTo(5));
            Assert.That(shop.CurrentUpgradeCost, Is.EqualTo(3));

            shop.GrantUpgradeDiscount(10);
            Assert.That(shop.CurrentUpgradeCost, Is.EqualTo(1));
            Assert.That(shop.UpgradeTavern().Success, Is.True);
            Assert.That(shop.TavernTier, Is.EqualTo(2));
            Assert.That(shop.CurrentUpgradeCost, Is.EqualTo(7));
        }

        [Test]
        public void BattleSimulator_DistinguishesMutualEliminationAndRoundLimit()
        {
            var simulator = new BattleSimulator(new Random(3));
            var mutual = simulator.Simulate(new BattleBoardState());
            Assert.That(mutual.Winner, Is.Null);
            Assert.That(mutual.OutcomeReason, Is.EqualTo(BattleOutcomeReason.MutualElimination));

            var stalled = new BattleBoardState();
            stalled.Player[0] = new BattleMinionRuntime(CreateMinion("player_wall", 1, 0, 100));
            stalled.Enemy[0] = new BattleMinionRuntime(CreateMinion("enemy_wall", 1, 0, 100));
            var timeout = simulator.Simulate(stalled);
            Assert.That(timeout.Winner, Is.Null);
            Assert.That(timeout.OutcomeReason, Is.EqualTo(BattleOutcomeReason.RoundLimit));
        }

        [Test]
        public void Settlement_UsesDrawDamageAndEnemySurvivorBreakdown()
        {
            var encounter = new EncounterConfig
            {
                Id = "boss",
                Category = "Boss",
                DamageBonus = 4
            };
            var draw = new BattleSimulationResult(
                new BattleBoardState(),
                null,
                BattleOutcomeReason.RoundLimit,
                new List<string>(),
                new List<BattleStep>());
            var drawSettlement = BattleSettlementCalculator.Calculate(draw, encounter);
            Assert.That(drawSettlement.Damage, Is.EqualTo(1));

            var finalState = new BattleBoardState();
            finalState.Enemy[0] = new BattleMinionRuntime(CreateMinion("tier_one", 1, 2, 2));
            finalState.Enemy[1] = new BattleMinionRuntime(CreateMinion("tier_two", 2, 2, 2));
            var loss = new BattleSimulationResult(
                finalState,
                BattleSide.Enemy,
                BattleOutcomeReason.Victory,
                new List<string>(),
                new List<BattleStep>());
            var settlement = BattleSettlementCalculator.Calculate(loss, encounter);
            Assert.That(settlement.SurvivingEnemies, Is.EqualTo(2));
            Assert.That(settlement.HighestEnemyTier, Is.EqualTo(2));
            Assert.That(settlement.Damage, Is.EqualTo(8));
        }

        private static MinionConfig CreateMinion(
            string id,
            int tier,
            int attack,
            int health)
        {
            return new MinionConfig
            {
                Id = id,
                Name = id,
                Tier = tier,
                Attack = attack,
                Health = health,
                GoldenAttack = attack * 2,
                GoldenHealth = health * 2,
                Enabled = true
            };
        }
    }
}

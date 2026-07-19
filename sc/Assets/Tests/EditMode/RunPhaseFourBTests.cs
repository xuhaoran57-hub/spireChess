using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Run;
using SpireChess.Shop;
using SpireChess.Utils;

namespace SpireChess.Tests
{
    public sealed class RunPhaseFourBTests
    {
        [Test]
        public void EliteVictory_BlocksOnThreeCandidates_AndSkipReleasesReservation()
        {
            var run = FindEliteChoice(candidate => candidate.Type == "Minion", out var candidate);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.RewardChoice));
            Assert.That(run.State.PendingRewardChoice.Candidates.Count, Is.EqualTo(3));
            Assert.That(run.State.CurrentAttempt.NodeResolved, Is.False);
            var remaining = run.Shop.MinionPool.GetRemainingCopies(candidate.CardId);

            Assert.That(run.SkipRewardChoice().Success, Is.True);
            Assert.That(run.Shop.MinionPool.GetRemainingCopies(candidate.CardId), Is.EqualTo(remaining + 1));
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.BattleResult));
            Assert.That(run.State.CurrentAttempt.NodeResolved, Is.True);
        }

        [Test]
        public void EliteLoss_UsesDamageBonus_WhileDrawAlwaysDealsOne()
        {
            var lossRun = ReachElite(41);
            var finalState = new BattleBoardState();
            finalState.Enemy[0] = new BattleMinionRuntime(
                CreateConfigs().MinionsById["copper_ring_apprentice"]);
            ResolveBattle(lossRun, new BattleSimulationResult(
                finalState, BattleSide.Enemy, BattleOutcomeReason.Victory,
                new List<string>(), new List<BattleStep>()));
            Assert.That(lossRun.State.Health, Is.EqualTo(17), "1 survivor + tier 1 + elite bonus 1");

            var drawRun = ReachElite(42);
            ResolveBattle(drawRun, new BattleSimulationResult(
                new BattleBoardState(), null, BattleOutcomeReason.RoundLimit,
                new List<string>(), new List<BattleStep>()));
            Assert.That(drawRun.State.Health, Is.EqualTo(19));
        }

        [Test]
        public void EliteTargetReward_ModifiesOnlySelectedOwnedBattleMinion()
        {
            RunSession selectedRun = null;
            RewardCandidate selectedCandidate = null;
            ShopCardInstance target = null;
            for (var seed = 1; seed <= 200 && selectedRun == null; seed++)
            {
                var run = CreateRun(seed);
                target = SeedBattleMinion(run, "target");
                ResolvePlayerWin(run);
                Assert.That(run.ContinueAfterBattle().Success, Is.True);
                CompleteShop(run, "f1_shop_2");
                Assert.That(run.EnterNode("f1_elite_wall").Success, Is.True);
                ResolvePlayerWin(run);
                selectedCandidate = run.State.PendingRewardChoice.Candidates
                    .FirstOrDefault(value => value.RequiresOwnedMinionTarget);
                if (selectedCandidate != null) selectedRun = run;
            }

            Assert.That(selectedRun, Is.Not.Null, "expected a deterministic seed with target reward");
            var attack = target.CurrentAttack;
            var health = target.CurrentHealth;
            Assert.That(selectedRun.SelectRewardCandidate(
                selectedCandidate.CandidateId, target.InstanceId).Success, Is.True);
            Assert.That(target.CurrentAttack, Is.EqualTo(attack + 1));
            Assert.That(target.CurrentHealth, Is.EqualTo(health + 2));
        }

        [Test]
        public void Enhance_AppliesPermanentStats_AndRejectsDuplicateKeyword()
        {
            var run = CreateRun(51);
            var target = SeedBattleMinion(run, "forge_target");
            ResolvePlayerWin(run);
            Assert.That(run.ContinueAfterBattle().Success, Is.True);
            CompleteShop(run, "f1_shop_2");
            Assert.That(run.EnterNode("f1_elite_wall").Success, Is.True);
            ResolvePlayerLoss(run);
            Assert.That(run.ContinueAfterBattle().Success, Is.True);
            Assert.That(run.EnterNode("f1_enhance").Success, Is.True);
            var attack = target.CurrentAttack;
            var health = target.CurrentHealth;

            Assert.That(run.ApplyEnhancement("balanced_forging", target.InstanceId).Success, Is.True);
            Assert.That(target.CurrentAttack, Is.EqualTo(attack + 2));
            Assert.That(target.CurrentHealth, Is.EqualTo(health + 2));
            Assert.That(run.Shop.ModifyOwnedBattleMinion(target.InstanceId, 2, 2).Success, Is.True);
            Assert.That(target.CurrentAttack, Is.EqualTo(attack + 4));
            Assert.That(run.Shop.ModifyOwnedBattleMinion(target.InstanceId, 0, 0, "Shield").Success, Is.True);
            Assert.That(run.Shop.ModifyOwnedBattleMinion(target.InstanceId, 0, 0, "Shield").Error,
                Is.EqualTo(ShopOperationError.NoBenefit));
        }

        [Test]
        public void NonCombatTurn_DoesNotConsumeDelayedShopResources()
        {
            var run = FindEliteChoice(candidate => candidate.Type == "NextShopGold", out var gold);
            Assert.That(run.SelectRewardCandidate(gold.CandidateId).Success, Is.True);
            Assert.That(run.ContinueAfterBattle().Success, Is.True);
            Assert.That(run.State.DelayedShopResources.GoldBonus, Is.EqualTo(gold.Amount));

            Assert.That(run.EnterNode("f1_enhance").Success, Is.True);
            Assert.That(run.State.ShopTurn, Is.EqualTo(2));
            Assert.That(run.State.DelayedShopResources.GoldBonus, Is.EqualTo(gold.Amount));
            Assert.That(run.SkipEnhancement().Success, Is.True);
            Assert.That(run.EnterNode("f1_shop_3").Success, Is.True);
            Assert.That(run.Shop.Gold, Is.EqualTo(5 + gold.Amount));
            Assert.That(run.State.DelayedShopResources.GoldBonus, Is.Zero);
        }

        [Test]
        public void EventChoice_CommitsHealthAndDelayedGoldAtomically()
        {
            var run = FindEvent("blood_contract");
            var health = run.State.Health;
            Assert.That(run.SelectEventOption("blood_contract", "accept").Success, Is.True);
            Assert.That(run.State.Health, Is.EqualTo(health - 3));
            Assert.That(run.State.DelayedShopResources.GoldBonus, Is.EqualTo(4));
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.MapSelection));

            var invalid = FindEvent("blood_contract");
            typeof(RunState).GetProperty(nameof(RunState.Health),
                    BindingFlags.Instance | BindingFlags.Public)
                .SetValue(invalid.State, 3);
            var beforeGold = invalid.State.DelayedShopResources.GoldBonus;
            Assert.That(invalid.SelectEventOption("blood_contract", "accept").Error,
                Is.EqualTo(RunOperationError.NoBenefit));
            Assert.That(invalid.State.Health, Is.EqualTo(3));
            Assert.That(invalid.State.DelayedShopResources.GoldBonus, Is.EqualTo(beforeGold));
            Assert.That(invalid.State.PendingEventChoice, Is.Not.Null);
        }

        [Test]
        public void DangerousRecruit_InsufficientPoolDoesNotChargeHealth()
        {
            var run = FindEvent("dangerous_recruit");
            var configs = CreateConfigs();
            foreach (var minion in configs.Minions.Where(value => value.Tier == run.Shop.TavernTier))
            {
                var remaining = run.Shop.MinionPool.GetRemainingCopies(minion.Id);
                if (remaining > 0) Assert.That(run.Shop.MinionPool.TryReserveCopies(minion.Id, remaining), Is.True);
            }

            var health = run.State.Health;
            Assert.That(run.SelectEventOption("dangerous_recruit", "recruit").Error,
                Is.EqualTo(RunOperationError.InsufficientPool));
            Assert.That(run.State.Health, Is.EqualTo(health));
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.EventChoice));
        }

        [Test]
        public void RestRejectsWastedHeal_ButMaxHealthOptionAlwaysBenefits()
        {
            var run = ReachRest(71);
            Assert.That(run.SelectRestOption("heal_6").Error, Is.EqualTo(RunOperationError.NoBenefit));
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.RestChoice));
            Assert.That(run.SelectRestOption("max_health_2_heal_2").Success, Is.True);
            Assert.That(run.State.MaxHealth, Is.EqualTo(22));
            Assert.That(run.State.Health, Is.EqualTo(22));
        }

        [Test]
        public void EventRandomStream_IsIndependentFromShopRefreshes()
        {
            var first = ReachEvent(88, 0);
            var second = ReachEvent(88, 2);
            Assert.That(second.State.PendingEventChoice.Config.Id,
                Is.EqualTo(first.State.PendingEventChoice.Config.Id));
        }

        private static RunSession FindEliteChoice(
            System.Func<RewardCandidate, bool> predicate,
            out RewardCandidate candidate)
        {
            for (var seed = 1; seed <= 300; seed++)
            {
                var run = ReachElite(seed);
                ResolvePlayerWin(run);
                candidate = run.State.PendingRewardChoice.Candidates.FirstOrDefault(predicate);
                if (candidate != null) return run;
                Assert.That(run.SkipRewardChoice().Success, Is.True);
            }

            candidate = null;
            Assert.Fail("No matching elite candidate was generated.");
            return null;
        }

        private static RunSession FindEvent(string eventId)
        {
            for (var seed = 1; seed <= 300; seed++)
            {
                var run = ReachEvent(seed, 0);
                if (run.State.PendingEventChoice.Config.Id == eventId) return run;
            }

            Assert.Fail($"No deterministic seed generated event {eventId}.");
            return null;
        }

        private static RunSession ReachElite(int seed)
        {
            var run = CreateRun(seed);
            Assert.That(run.EnterNode("f1_shop_start").Success, Is.True);
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);
            Assert.That(run.EnterNode("f1_opening_normal").Success, Is.True);
            ResolvePlayerWin(run);
            Assert.That(run.ContinueAfterBattle().Success, Is.True);
            Assert.That(run.EnterNode("f1_shop_2").Success, Is.True);
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);
            Assert.That(run.EnterNode("f1_elite_wall").Success, Is.True);
            return run;
        }

        private static RunSession ReachEvent(int seed, int refreshes)
        {
            var run = CreateRun(seed);
            Assert.That(run.EnterNode("f1_shop_start").Success, Is.True);
            for (var i = 0; i < refreshes; i++) Assert.That(run.Shop.Refresh().Success, Is.True);
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);
            Assert.That(run.EnterNode("f1_opening_normal").Success, Is.True);
            ResolvePlayerWin(run);
            Assert.That(run.ContinueAfterBattle().Success, Is.True);
            Assert.That(run.EnterNode("f1_shop_2").Success, Is.True);
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);
            Assert.That(run.EnterNode("f1_safe_normal").Success, Is.True);
            ResolvePlayerWin(run);
            Assert.That(run.ContinueAfterBattle().Success, Is.True);
            Assert.That(run.EnterNode("f1_event").Success, Is.True);
            return run;
        }

        private static RunSession ReachRest(int seed)
        {
            var run = CreateRun(seed);
            Assert.That(run.EnterNode("f1_shop_start").Success, Is.True);
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);
            Assert.That(run.EnterNode("f1_opening_normal").Success, Is.True);
            ResolvePlayerWin(run);
            Assert.That(run.ContinueAfterBattle().Success, Is.True);
            Assert.That(run.EnterNode("f1_shop_2").Success, Is.True);
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);
            Assert.That(run.EnterNode("f1_safe_normal").Success, Is.True);
            ResolvePlayerWin(run);
            Assert.That(run.ContinueAfterBattle().Success, Is.True);
            Assert.That(run.EnterNode("f1_rest").Success, Is.True);
            return run;
        }

        private static ShopCardInstance SeedBattleMinion(RunSession run, string instanceId)
        {
            Assert.That(run.EnterNode("f1_shop_start").Success, Is.True);
            var config = CreateConfigs().MinionsById["copper_ring_apprentice"];
            var card = ShopCardInstance.CreateMinion(instanceId, config);
            Assert.That(run.Shop.Collection.TryAddToBench(card, out var bench), Is.True);
            Assert.That(run.Shop.PlayMinion(bench, 0).Success, Is.True);
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);
            Assert.That(run.EnterNode("f1_opening_normal").Success, Is.True);
            return card;
        }

        private static void CompleteShop(RunSession run, string nodeId)
        {
            Assert.That(run.EnterNode(nodeId).Success, Is.True);
            while (run.State.PendingCardRewards.Count > 0)
            {
                var result = run.ClaimNextCardReward();
                if (result.Success)
                    continue;
                Assert.That(result.Error, Is.EqualTo(RunOperationError.BenchFull));
                Assert.That(run.SkipNextCardReward().Success, Is.True);
            }
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);
        }

        private static void ResolvePlayerWin(RunSession run)
        {
            ResolveBattle(run, new BattleSimulationResult(
                new BattleBoardState(), BattleSide.Player, BattleOutcomeReason.Victory,
                new List<string>(), new List<BattleStep>()));
        }

        private static void ResolvePlayerLoss(RunSession run)
        {
            ResolveBattle(run, new BattleSimulationResult(
                new BattleBoardState(), BattleSide.Enemy, BattleOutcomeReason.Victory,
                new List<string>(), new List<BattleStep>()));
        }

        private static void ResolveBattle(RunSession run, BattleSimulationResult result)
        {
            Assert.That(run.TryCompleteBattle(result, out var returnScene), Is.True);
            Assert.That(returnScene, Is.EqualTo("RunTest"));
        }

        private static RunSession CreateRun(int seed)
        {
            return new RunSession(CreateConfigs(), seed);
        }

        private static ConfigService CreateConfigs()
        {
            var configs = new ConfigService(new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));
            return configs;
        }
    }
}

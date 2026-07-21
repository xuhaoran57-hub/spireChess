using System.Linq;
using NUnit.Framework;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Run;
using SpireChess.Save;
using SpireChess.Utils;

namespace SpireChess.Tests.EditMode
{
    public sealed class RunSnapshotTests
    {
        private ConfigService configs;
        private RunSnapshotMapper mapper;
        private RunSnapshotValidator validator;

        [SetUp]
        public void SetUp()
        {
            configs = new ConfigService(new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));
            mapper = new RunSnapshotMapper(configs);
            validator = new RunSnapshotValidator(configs);
        }

        [Test]
        public void InitialRun_CaptureJsonRestoreCapture_IsEquivalent()
        {
            var run = new RunSession(configs, 20260721);
            var original = mapper.Capture(run);
            var serializer = new NewtonsoftJsonSerializer();
            var json = serializer.ToJson(original);
            var deserialized = serializer.FromJson<RunSavePayloadV1>(json);

            var validation = validator.ValidateDto(deserialized);
            Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));
            var restored = mapper.Restore(deserialized);
            var roundTrip = mapper.Capture(restored);

            Assert.That(
                serializer.ToJson(roundTrip, true),
                Is.EqualTo(serializer.ToJson(original, true)));
            Assert.That(
                RunStateFingerprint.Compute(roundTrip),
                Is.EqualTo(RunStateFingerprint.Compute(original)));
        }

        [Test]
        public void OpenShop_RestoreAndContinue_ProducesSameOffersAndFingerprint()
        {
            var originalRun = new RunSession(configs, 712345);
            var startNode = originalRun.State.CurrentMap.StartNodeIds.First();
            Assert.That(originalRun.EnterNode(startNode).Success, Is.True);
            Assert.That(originalRun.State.Phase, Is.EqualTo(RunPhase.Shop));
            var restoredRun = mapper.Restore(mapper.Capture(originalRun));

            Assert.That(originalRun.Shop.Refresh().Success, Is.True);
            Assert.That(restoredRun.Shop.Refresh().Success, Is.True);

            Assert.That(
                restoredRun.Shop.MinionOffers.Select(value => value.Id),
                Is.EqualTo(originalRun.Shop.MinionOffers.Select(value => value.Id)));
            Assert.That(restoredRun.Shop.SpellOffer?.Id, Is.EqualTo(originalRun.Shop.SpellOffer?.Id));
            Assert.That(
                RunStateFingerprint.Compute(mapper.Capture(restoredRun)),
                Is.EqualTo(RunStateFingerprint.Compute(mapper.Capture(originalRun))));
        }

        [Test]
        public void EnteringNode_IsRejectedAsNonDurable()
        {
            var run = new RunSession(configs, 19);
            var payload = mapper.Capture(run);
            payload.RunState.Phase = RunPhase.EnteringNode;

            var result = validator.ValidateDto(payload);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(value => value.Contains("EnteringNode")), Is.True);
        }

        [Test]
        public void BattleAndCommittedResult_RoundTripWithoutDuplicateSettlement()
        {
            var run = new RunSession(configs, 81173);
            Assert.That(run.EnterNode("f1_shop_start").Success, Is.True);
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);
            Assert.That(run.EnterNode("f1_opening_normal").Success, Is.True);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.Battle));

            run = AssertRoundTrip(run);
            var finalBoard = run.PendingBattle.BoardState.Clone();
            for (var index = 0; index < BattleBoardState.SlotCount; index++)
            {
                finalBoard.Enemy[index] = null;
            }
            var result = new BattleSimulationResult(
                finalBoard,
                BattleSide.Player,
                new System.Collections.Generic.List<string> { "save resume win" });
            Assert.That(run.TryCompleteBattle(result, out _), Is.True);
            var health = run.State.Health;
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.BattleResult));

            var restored = AssertRoundTrip(run);

            Assert.That(restored.State.Health, Is.EqualTo(health));
            Assert.That(restored.TryCompleteBattle(result, out _), Is.False);
            Assert.That(restored.State.Health, Is.EqualTo(health));
        }

        [Test]
        public void RewardAndEnhanceChoices_RoundTripWithoutRerollingCandidates()
        {
            var run = ReachRouteChoice(4412);
            CompleteCurrentCombat(run, "f1_elite_wall", false);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.RewardChoice));
            var rewardIds = run.State.PendingRewardChoice.Candidates
                .Select(value => value.CandidateId).ToArray();

            run = AssertRoundTrip(run);

            Assert.That(
                run.State.PendingRewardChoice.Candidates.Select(value => value.CandidateId),
                Is.EqualTo(rewardIds));
            Assert.That(run.SkipRewardChoice().Success, Is.True);
            Assert.That(run.ContinueAfterBattle().Success, Is.True);
            Assert.That(run.EnterNode("f1_enhance").Success, Is.True);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.EnhanceChoice));
            var recipeIds = run.State.PendingEnhanceChoice.Recipes.Select(value => value.Id).ToArray();

            run = AssertRoundTrip(run);

            Assert.That(run.State.PendingEnhanceChoice.Recipes.Select(value => value.Id),
                Is.EqualTo(recipeIds));
        }

        [Test]
        public void EventAndRestChoices_RoundTripAtMaterializedBoundary()
        {
            var eventRun = ReachRouteChoice(5911);
            CompleteCurrentCombat(eventRun, "f1_route_normal");
            Assert.That(eventRun.EnterNode("f1_event").Success, Is.True);
            var eventId = eventRun.State.PendingEventChoice.Config.Id;

            eventRun = AssertRoundTrip(eventRun);

            Assert.That(eventRun.State.Phase, Is.EqualTo(RunPhase.EventChoice));
            Assert.That(eventRun.State.PendingEventChoice.Config.Id, Is.EqualTo(eventId));

            var restRun = ReachRouteChoice(5912);
            CompleteCurrentCombat(restRun, "f1_route_safe");
            Assert.That(restRun.EnterNode("f1_rest").Success, Is.True);
            var optionIds = restRun.State.PendingRestChoice.Config.Options
                .Select(value => value.Id).ToArray();

            restRun = AssertRoundTrip(restRun);

            Assert.That(restRun.State.Phase, Is.EqualTo(RunPhase.RestChoice));
            Assert.That(restRun.State.PendingRestChoice.Config.Options.Select(value => value.Id),
                Is.EqualTo(optionIds));
        }

        [Test]
        public void BossRelicAndFloorComplete_RoundTripWithoutChargingTwice()
        {
            var run = ReachRouteChoice(801);
            CompleteCurrentCombat(run, "f1_route_safe");
            Assert.That(run.EnterNode("f1_rest").Success, Is.True);
            Assert.That(run.SelectRestOption("leave").Success, Is.True);
            CompleteShop(run, "f1_shop_5");
            CompleteCurrentCombat(run, "f1_late_shield");
            CompleteShop(run, "f1_shop_boss");
            CompleteCurrentCombat(run, "f1_boss", false);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.RelicChoice));
            var candidates = run.State.PendingRelicChoice.Candidates
                .Select(value => value.CandidateId).ToArray();
            var health = run.State.Health;

            run = AssertRoundTrip(run);

            Assert.That(run.State.PendingRelicChoice.Candidates.Select(value => value.CandidateId),
                Is.EqualTo(candidates));
            Assert.That(run.State.Health, Is.EqualTo(health));
            Assert.That(run.SelectRelicCandidate(candidates[0]).Success, Is.True);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.FloorComplete));
            Assert.That(run.State.Health, Is.EqualTo(health));
            AssertRoundTrip(run);
        }

        private RunSession AssertRoundTrip(RunSession run)
        {
            var before = mapper.Capture(run);
            var validation = validator.ValidateDto(before);
            Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));
            var restored = mapper.Restore(before);
            Assert.That(
                RunStateFingerprint.Compute(mapper.Capture(restored)),
                Is.EqualTo(RunStateFingerprint.Compute(before)));
            return restored;
        }

        private RunSession ReachRouteChoice(int seed)
        {
            var run = new RunSession(configs, seed);
            CompleteShop(run, "f1_shop_start");
            CompleteCurrentCombat(run, "f1_opening_normal");
            CompleteShop(run, "f1_shop_2");
            CompleteCurrentCombat(run, "f1_safe_normal");
            CompleteShop(run, "f1_shop_3");
            CompleteCurrentCombat(run, "f1_mid_mechanic");
            CompleteShop(run, "f1_shop_4");
            return run;
        }

        private static void CompleteShop(RunSession run, string nodeId)
        {
            Assert.That(run.EnterNode(nodeId).Success, Is.True, nodeId);
            while (run.State.PendingCardRewards.Count > 0)
            {
                var claim = run.ClaimNextCardReward();
                if (!claim.Success)
                {
                    Assert.That(run.SkipNextCardReward().Success, Is.True);
                }
            }
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True, nodeId);
        }

        private static void CompleteCurrentCombat(
            RunSession run,
            string nodeId,
            bool continueAfter = true)
        {
            Assert.That(run.EnterNode(nodeId).Success, Is.True, nodeId);
            var finalBoard = run.PendingBattle.BoardState.Clone();
            for (var index = 0; index < BattleBoardState.SlotCount; index++)
            {
                finalBoard.Enemy[index] = null;
            }
            var result = new BattleSimulationResult(
                finalBoard,
                BattleSide.Player,
                new System.Collections.Generic.List<string> { "snapshot path win" });
            Assert.That(run.TryCompleteBattle(result, out _), Is.True, nodeId);
            if (continueAfter)
            {
                Assert.That(run.ContinueAfterBattle().Success, Is.True, nodeId);
            }
        }
    }
}

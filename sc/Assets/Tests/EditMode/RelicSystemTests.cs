using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Run;
using SpireChess.Shop;
using SpireChess.Simulation;
using SpireChess.Utils;

namespace SpireChess.Tests
{
    public sealed class RelicSystemTests
    {
        [Test]
        public void ReleasedConfig_ContainsFifteenRelicsAndCurioEvent()
        {
            var configs = CreateConfigs();

            Assert.That(configs.Relics.Count, Is.EqualTo(15));
            Assert.That(configs.Relics.Count(value => value.Grade == "Crown"), Is.EqualTo(8));
            Assert.That(configs.Relics.Count(value => value.Grade == "Curio"), Is.EqualTo(7));
            Assert.That(configs.ContentRelease.RelicIds.Distinct().Count(), Is.EqualTo(15));
            Assert.That(configs.EventsById["sealed_reliquary"].Options
                .Any(value => value.FollowupRelicGrade == "Curio"), Is.True);
        }

        [Test]
        public void OwnedRelics_HaveNoCountCapButRejectDuplicateIds()
        {
            var configs = CreateConfigs();
            var run = new RunSession(configs, 1001);

            foreach (var relic in configs.Relics)
            {
                AddOwnedRelic(run, relic);
            }

            Assert.That(run.State.OwnedRelics.Count, Is.EqualTo(15));
            var exception = Assert.Throws<TargetInvocationException>(() =>
                AddOwnedRelic(run, configs.Relics[0]));
            Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void BossLossDoesNotDropRelic_AndWinningRetryCreatesMandatoryChoiceOnce()
        {
            var configs = CreateConfigs();
            var run = ReachFirstBoss(configs, 1002);
            var losingState = new BattleBoardState();
            losingState.Enemy[0] = new BattleMinionRuntime(
                configs.MinionsById["copper_ring_apprentice"]);
            var loss = new BattleSimulationResult(
                losingState,
                BattleSide.Enemy,
                BattleOutcomeReason.Victory,
                new List<string>(),
                new List<BattleStep>());

            Assert.That(run.TryCompleteBattle(loss, out _), Is.True);
            Assert.That(run.State.PendingRelicChoice, Is.Null);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.BattleResult));
            Assert.That(run.RetryBoss().Success, Is.True);

            var win = CreateWin();
            Assert.That(run.TryCompleteBattle(win, out _), Is.True);
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.RelicChoice));
            Assert.That(run.State.PendingRelicChoice.Candidates.Count, Is.EqualTo(3));
            Assert.That(run.State.PendingRelicChoice.AllowSkip, Is.False);
            var choiceId = run.State.PendingRelicChoice.ChoiceId;
            Assert.That(run.TryCompleteBattle(win, out _), Is.False);
            Assert.That(run.State.PendingRelicChoice.ChoiceId, Is.EqualTo(choiceId));
            Assert.That(run.SkipRelicChoice().Error, Is.EqualTo(RunOperationError.InvalidChoice));

            Assert.That(run.SelectRelicCandidate(
                run.State.PendingRelicChoice.Candidates[0].CandidateId).Success, Is.True);
            Assert.That(run.State.OwnedRelics.Count, Is.EqualTo(1));
            Assert.That(run.State.Phase, Is.EqualTo(RunPhase.FloorComplete));
        }

        [Test]
        public void EventRelic_ChargesOnlyOnAtomicSelectionAndCannotKillPlayer()
        {
            var configs = CreateConfigs();
            var run = FindSealedReliquaryRun(configs);
            var initialHealth = run.State.Health;

            Assert.That(run.SelectEventOption("sealed_reliquary", "inspect").Success, Is.True);
            Assert.That(run.State.Health, Is.EqualTo(initialHealth));
            Assert.That(run.State.PendingRelicChoice.HealthCost, Is.EqualTo(5));
            Assert.That(run.SkipRelicChoice().Success, Is.True);
            Assert.That(run.State.Health, Is.EqualTo(initialHealth));
            Assert.That(run.State.OwnedRelics, Is.Empty);

            run = FindSealedReliquaryRun(configs);
            Assert.That(run.SelectEventOption("sealed_reliquary", "inspect").Success, Is.True);
            SetRunHealth(run, 5);
            var candidateId = run.State.PendingRelicChoice.Candidates[0].CandidateId;
            Assert.That(run.SelectRelicCandidate(candidateId).Error,
                Is.EqualTo(RunOperationError.NoBenefit));
            Assert.That(run.State.Health, Is.EqualTo(5));
            Assert.That(run.State.OwnedRelics, Is.Empty);
            Assert.That(run.State.PendingRelicChoice, Is.Not.Null);

            SetRunHealth(run, 6);
            Assert.That(run.SelectRelicCandidate(candidateId).Success, Is.True);
            Assert.That(run.State.Health, Is.EqualTo(1));
            Assert.That(run.State.OwnedRelics.Count, Is.EqualTo(1));
        }

        [Test]
        public void ShopRules_ApplyFreePurchasePaidRefreshPrecedenceAndSaleBonus()
        {
            var configs = CreateConfigs();
            var shop = new ShopSession(configs.Minions, configs.Spells, new Random(1003));
            shop.ConfigureRuleModifiers(new ShopRuleModifiers
            {
                FirstPurchaseFree = true,
                FirstPaidRefreshFree = true,
                FirstMinionSaleBonusGold = 2
            });
            var events = new List<ShopEventData>();
            shop.EventRaised += events.Add;

            Assert.That(shop.StartRound(1).Success, Is.True);
            Assert.That(shop.BuyMinion(-1).Error, Is.EqualTo(ShopOperationError.InvalidIndex));
            var gold = shop.Gold;
            var buy = shop.BuyMinion(0);
            Assert.That(buy.Success, Is.True);
            Assert.That(shop.Gold, Is.EqualTo(gold));
            Assert.That(events.Last(value => value.Type == ShopEventType.OnBuy).Cost, Is.Zero);
            Assert.That(shop.PlayMinion(buy.BenchIndex, 0).Success, Is.True);
            gold = shop.Gold;
            Assert.That(shop.SellBattleMinion(0).Success, Is.True);
            Assert.That(shop.Gold, Is.EqualTo(gold + 3));
            Assert.That(events.Last(value => value.Type == ShopEventType.OnSell).Cost, Is.EqualTo(-3));

            shop.GrantFreeRefreshes(1);
            gold = shop.Gold;
            Assert.That(shop.Refresh().Success, Is.True);
            Assert.That(shop.Refresh().Success, Is.True);
            Assert.That(shop.Gold, Is.EqualTo(gold));
            Assert.That(shop.Refresh().Success, Is.True);
            Assert.That(shop.Gold, Is.EqualTo(gold - 1));
            Assert.That(events.Where(value => value.Type == ShopEventType.OnRefresh)
                .Select(value => value.Cost), Is.EqualTo(new[] { 0, 0, 1 }));
        }

        [Test]
        public void BattlecryRelic_RepeatsEffectsButRaisesOnePlayEvent()
        {
            var configs = CreateConfigs();
            var shop = new ShopSession(configs.Minions, configs.Spells, new Random(1004));
            shop.ConfigureRuleModifiers(new ShopRuleModifiers { ExtraBattlecryTriggers = 1 });
            var playEvents = 0;
            shop.EventRaised += value =>
            {
                if (value.Type == ShopEventType.OnPlay) playEvents++;
            };

            Assert.That(shop.StartRound(1).Success, Is.True);
            var claim = shop.ClaimRewardMinion(configs.MinionsById["black_market_vendor"]);
            var gold = shop.Gold;
            Assert.That(shop.PlayMinion(claim.BenchIndex, 0).Success, Is.True);

            Assert.That(shop.Gold, Is.EqualTo(gold + 2));
            Assert.That(playEvents, Is.EqualTo(1));
        }

        [Test]
        public void ShopStartRelics_AdvanceIndependentlyAndReserveMinionPoolCopies()
        {
            var configs = CreateConfigs();
            var run = new RunSession(configs, 1005);
            AddOwnedRelic(run, configs.RelicsById["crown_star_satchel"]);
            AddOwnedRelic(run, configs.RelicsById["crown_tier_five_decree"]);
            AddOwnedRelic(run, configs.RelicsById["curio_ink_bottle"]);
            AddOwnedRelic(run, configs.RelicsById["curio_recruit_badge"]);

            var byTurn = new List<IReadOnlyList<RelicCardGrant>>();
            for (var turn = 1; turn <= 3; turn++)
            {
                SetShopTurn(run, turn);
                IReadOnlyList<RelicCardGrant> grants = null;
                Assert.That(run.Shop.StartRound(
                    turn,
                    () => grants = run.Relics.ApplyShopStartEffects()).Success, Is.True);
                byTurn.Add(grants);
                Assert.That(run.Relics.ApplyShopStartEffects(), Is.Empty,
                    "the same ShopTurn must be idempotent");
                Assert.That(run.Shop.EndRound().Success, Is.True);
            }

            Assert.That(byTurn[0].Count(value => value.CardType == ShopCardType.Spell),
                Is.EqualTo(1));
            Assert.That(byTurn[1].Count(value => value.CardType == ShopCardType.Spell),
                Is.EqualTo(2));
            var tierFiveGrant = byTurn[1].Single(value =>
                value.CardType == ShopCardType.Minion);
            Assert.That(configs.MinionsById[tierFiveGrant.ConfigId].Tier, Is.EqualTo(5));
            Assert.That(run.Shop.MinionPool.GetRemainingCopies(tierFiveGrant.ConfigId),
                Is.EqualTo(ShopEconomyRules.GetPoolCopiesPerMinion(5) - 1));
            Assert.That(configs.MinionsById[
                byTurn[2].Single(value => value.CardType == ShopCardType.Minion).ConfigId].Tier,
                Is.EqualTo(1));
            foreach (var grant in byTurn.SelectMany(value => value)
                         .Where(value => value.CardType == ShopCardType.Minion))
            {
                Assert.That(grant.ReservedPoolCopies, Is.EqualTo(1));
            }
        }

        [Test]
        public void LowHealthAndVictoryRelics_ApplyAtTheirExactTimings()
        {
            var configs = CreateConfigs();
            var run = new RunSession(configs, 1006);
            AddOwnedRelic(run, configs.RelicsById["curio_merchant_pouch"]);
            AddOwnedRelic(run, configs.RelicsById["curio_field_medicine"]);
            SetRunHealth(run, 10);
            SetShopTurn(run, 1);

            Assert.That(run.Shop.StartRound(
                1,
                () => run.Relics.ApplyShopStartEffects()).Success, Is.True);
            Assert.That(run.Shop.Gold, Is.EqualTo(5));
            Assert.That(run.Shop.EndRound().Success, Is.True);
            Assert.That(run.Relics.ApplyVictoryHealing(RunNodeType.Normal), Is.Zero);
            Assert.That(run.Relics.ApplyVictoryHealing(RunNodeType.Elite), Is.EqualTo(2));
            Assert.That(run.State.Health, Is.EqualTo(12));
        }

        [Test]
        public void BattleSnapshotRelics_MergeShieldsAndCountFormalRaces()
        {
            var configs = CreateConfigs();
            var run = new RunSession(configs, 1007);
            AddOwnedRelic(run, configs.RelicsById["crown_thousand_shields"]);
            AddOwnedRelic(run, configs.RelicsById["curio_cracked_ward"]);
            AddOwnedRelic(run, configs.RelicsById["crown_manyfold_banner"]);
            var board = new BattleBoardState();
            board.Player[0] = new BattleMinionRuntime(
                configs.Minions.First(value => value.Race == "ForgeSoul" && !value.IsToken));
            board.Player[1] = new BattleMinionRuntime(
                configs.Minions.First(value => value.Race == "WildSpirit" && !value.IsToken));
            board.Player[2] = new BattleMinionRuntime(
                configs.Minions.First(value => value.Race == "Starbound" && !value.IsToken));
            board.BattleStartEffects.Add(new BattleStartEffectState(
                BattleSide.Player,
                new EffectConfig
                {
                    Id = "existing_spell_effect",
                    Trigger = "OnBattleStart",
                    Action = "ModifyStats",
                    Value = new ValueConfig()
                }));

            run.Relics.ApplyBattleRules(board);

            var shield = board.BattleStartEffects.Single(value =>
                value.Effect.Id == "relic_battle_start_shield").Effect;
            var banner = board.BattleStartEffects.Single(value =>
                value.Effect.Id == "relic_manyfold_banner").Effect;
            Assert.That(shield.Target.MaxTargets, Is.EqualTo(3));
            Assert.That(shield.Target.Selector, Is.EqualTo("NoShieldLowestHealth"));
            Assert.That(banner.Value.Attack, Is.EqualTo(3));
            Assert.That(banner.Value.Health, Is.EqualTo(3));
            Assert.That(board.BattleStartEffects.Select(value => value.Effect.Id),
                Is.EqualTo(new[]
                {
                    "relic_battle_start_shield",
                    "relic_manyfold_banner",
                    "existing_spell_effect"
                }));
        }

        [Test]
        public void DeathrattleAndWildBroodRules_UseSourceOnlyAndTriggerBroodOnce()
        {
            var configs = CreateConfigs();
            MinionConfig Resolve(string id) =>
                configs.MinionsById.TryGetValue(id, out var value) ? value : null;

            var deathrattleBoard = new BattleBoardState();
            deathrattleBoard.RuleModifiers.PlayerExtraDeathrattleTriggers = 1;
            deathrattleBoard.Player[0] = new BattleMinionRuntime(
                configs.MinionsById["young_deer_spirit"],
                initialAttack: 0,
                initialHealth: 1);
            deathrattleBoard.Enemy[0] = new BattleMinionRuntime(
                configs.MinionsById["copper_ring_apprentice"],
                initialAttack: 1,
                initialHealth: 100);
            var deathrattle = new BattleSimulator(new Random(1008), Resolve)
                .Simulate(deathrattleBoard);
            Assert.That(deathrattle.Diagnostics.Player.SummonSuccesses, Is.EqualTo(2));
            Assert.That(deathrattle.Diagnostics.Player.NonTokenDeaths, Is.EqualTo(1));

            var broodBoard = new BattleBoardState();
            broodBoard.RuleModifiers.PlayerFirstNonTokenDeathSummonCount = 2;
            broodBoard.RuleModifiers.PlayerFirstNonTokenDeathTokenId = "token_young_spirit";
            broodBoard.RuleModifiers.PlayerFirstNonTokenDeathTokenAttack = 2;
            broodBoard.RuleModifiers.PlayerFirstNonTokenDeathTokenHealth = 2;
            for (var slot = 0; slot < 2; slot++)
            {
                broodBoard.Player[slot] = new BattleMinionRuntime(
                    configs.MinionsById["copper_ring_apprentice"],
                    initialAttack: 0,
                    initialHealth: 1);
                broodBoard.Enemy[slot] = new BattleMinionRuntime(
                    configs.MinionsById["copper_ring_apprentice"],
                    initialAttack: 5,
                    initialHealth: 100);
            }

            var brood = new BattleSimulator(new Random(1009), Resolve).Simulate(broodBoard);
            Assert.That(brood.Diagnostics.Player.SummonAttempts, Is.EqualTo(2));
            Assert.That(brood.Diagnostics.Player.SummonSuccesses, Is.EqualTo(2));
        }

        [Test]
        public void RelicBattleRules_KeepNormalAndPlaybackSimulationDeterministic()
        {
            var configs = CreateConfigs();
            MinionConfig Resolve(string id) =>
                configs.MinionsById.TryGetValue(id, out var value) ? value : null;
            var board = new BattleBoardState();
            board.RuleModifiers.PlayerExtraDeathrattleTriggers = 1;
            board.Player[0] = new BattleMinionRuntime(
                configs.MinionsById["young_deer_spirit"],
                initialHealth: 1);
            board.Enemy[0] = new BattleMinionRuntime(
                configs.MinionsById["copper_ring_apprentice"],
                initialAttack: 2,
                initialHealth: 20);

            var normal = new BattleSimulator(new Random(1010), Resolve).Simulate(board);
            var playback = new BattleSimulator(new Random(1010), Resolve).SimulatePlayback(board);

            Assert.That(BattleDeterminismHasher.Compute(playback),
                Is.EqualTo(BattleDeterminismHasher.Compute(normal)));
        }

        private static RunSession FindSealedReliquaryRun(ConfigService configs)
        {
            var run = new RunSession(configs, 21);
            CompleteShop(run, "f1_shop_start");
            CompleteCombat(run, "f1_opening_normal");
            CompleteShop(run, "f1_shop_2");
            CompleteCombat(run, "f1_safe_normal");
            CompleteShop(run, "f1_shop_3");
            CompleteCombat(run, "f1_mid_mechanic");
            CompleteShop(run, "f1_shop_4");
            CompleteCombat(run, "f1_route_normal");
            Assert.That(run.EnterNode("f1_event").Success, Is.True);
            Assert.That(run.State.PendingEventChoice.Config.Id, Is.EqualTo("sealed_reliquary"));
            return run;
        }

        private static RunSession ReachFirstBoss(ConfigService configs, int seed)
        {
            var run = new RunSession(configs, seed);
            CompleteShop(run, "f1_shop_start");
            CompleteCombat(run, "f1_opening_normal");
            CompleteShop(run, "f1_shop_2");
            CompleteCombat(run, "f1_safe_normal");
            CompleteShop(run, "f1_shop_3");
            CompleteCombat(run, "f1_mid_mechanic");
            CompleteShop(run, "f1_shop_4");
            CompleteCombat(run, "f1_route_safe");
            Assert.That(run.EnterNode("f1_rest").Success, Is.True);
            Assert.That(run.SelectRestOption("leave").Success, Is.True);
            CompleteShop(run, "f1_shop_5");
            CompleteCombat(run, "f1_late_shield");
            CompleteShop(run, "f1_shop_boss");
            Assert.That(run.EnterNode("f1_boss").Success, Is.True);
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
                    Assert.That(claim.Error, Is.EqualTo(RunOperationError.BenchFull));
                    Assert.That(run.SkipNextCardReward().Success, Is.True);
                }
            }
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);
        }

        private static void CompleteCombat(RunSession run, string nodeId)
        {
            Assert.That(run.EnterNode(nodeId).Success, Is.True, nodeId);
            Assert.That(run.TryCompleteBattle(CreateWin(), out _), Is.True);
            Assert.That(run.ContinueAfterBattle().Success, Is.True);
        }

        private static BattleSimulationResult CreateWin()
        {
            return new BattleSimulationResult(
                new BattleBoardState(),
                BattleSide.Player,
                BattleOutcomeReason.Victory,
                new List<string>(),
                new List<BattleStep>());
        }

        private static void AddOwnedRelic(RunSession run, RelicConfig relic)
        {
            var constructor = typeof(OwnedRelicState).GetConstructors(
                BindingFlags.Instance | BindingFlags.NonPublic).Single();
            var owned = (OwnedRelicState)constructor.Invoke(new object[]
            {
                relic,
                "Test",
                "test",
                run.State.Floor,
                run.State.ShopTurn
            });
            var add = typeof(RunState).GetMethod(
                "AddOwnedRelic",
                BindingFlags.Instance | BindingFlags.NonPublic);
            add.Invoke(run.State, new object[] { owned });
        }

        private static void SetRunHealth(RunSession run, int value)
        {
            typeof(RunState).GetProperty(nameof(RunState.Health))
                .GetSetMethod(true)
                .Invoke(run.State, new object[] { value });
        }

        private static void SetShopTurn(RunSession run, int value)
        {
            typeof(RunState).GetProperty(nameof(RunState.ShopTurn))
                .GetSetMethod(true)
                .Invoke(run.State, new object[] { value });
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

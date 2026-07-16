using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Shop;
using SpireChess.Utils;
using Application = UnityEngine.Application;

namespace SpireChess.Tests.EditMode
{
    public sealed class MinionPoolV02Tests
    {
        private ConfigService configs;

        [SetUp]
        public void SetUp()
        {
            configs = new ConfigService(new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));
        }

        [Test]
        public void ReleasedPool_HasV02CountsDistributionAndNoNewArchetypeMetadata()
        {
            Assert.That(configs.Minions, Has.Count.EqualTo(67));
            Assert.That(configs.Minions.Count(value => value.IsToken), Is.EqualTo(3));
            Assert.That(configs.Minions.Count(value => !value.IsToken), Is.EqualTo(64));

            var expectedMainRaceCounts = new[] { 3, 4, 4, 4, 3 };
            foreach (var race in new[] { "ForgeSoul", "WildSpirit", "Starbound" })
            {
                for (var tier = 1; tier <= 5; tier++)
                {
                    Assert.That(configs.Minions.Count(value => !value.IsToken &&
                        value.Race == race && value.Tier == tier),
                        Is.EqualTo(expectedMainRaceCounts[tier - 1]),
                        $"{race} tier {tier}");
                }
            }

            for (var tier = 1; tier <= 5; tier++)
            {
                Assert.That(configs.Minions.Count(value => !value.IsToken &&
                    value.Race == "Wayfarer" && value.Tier == tier), Is.EqualTo(2));
            }

            var newIds = new[]
            {
                "ember_engraver", "counterflow_smith", "cinder_armor_arbiter",
                "rotleaf_heir", "rootbound_soul_guide", "fox_den_matriarch",
                "star_etched_timekeeper", "secret_page_refractor", "star_ring_treasurer",
                "traveling_physician", "mercenary_shieldbearer", "many_arts_apprentice",
                "pack_hunt_inspector", "mirrorsteel_duelist"
            };
            Assert.That(newIds.Select(id => configs.MinionsById[id]),
                Has.All.Matches<MinionConfig>(value => value.Archetypes.Count == 0));
            Assert.That(configs.ContentRelease.MinionIds, Is.EquivalentTo(
                configs.Minions.Select(value => value.Id)));
        }

        [Test]
        public void RootAndResourcesMinionConfigs_AreExactMirrors()
        {
            var resourcesPath = Path.Combine(
                Application.dataPath,
                "Resources",
                "Configs",
                "Json",
                "minions.v0.1.json");
            var rootPath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "..",
                "minions.v0.1.json"));

            Assert.That(File.Exists(rootPath), Is.True);
            Assert.That(File.ReadAllText(rootPath), Is.EqualTo(File.ReadAllText(resourcesPath)));
        }

        [Test]
        public void RootAndResourcesSpellConfigs_AreExactMirrors()
        {
            var resourcesPath = Path.Combine(
                Application.dataPath,
                "Resources",
                "Configs",
                "Json",
                "spells.v0.1.json");
            var rootPath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "..",
                "spells.v0.1.json"));

            Assert.That(File.Exists(rootPath), Is.True);
            Assert.That(File.ReadAllText(rootPath), Is.EqualTo(File.ReadAllText(resourcesPath)));
        }

        [Test]
        public void EncountersAndRewards_ReferenceV02Content()
        {
            var encounterIds = configs.Encounters
                .SelectMany(value => value.EnemySlots)
                .Select(value => value.MinionId)
                .ToList();
            Assert.That(encounterIds, Does.Contain("counterflow_smith"));
            Assert.That(encounterIds, Does.Contain("cinder_armor_arbiter"));
            Assert.That(encounterIds, Does.Contain("fox_den_matriarch"));
            Assert.That(encounterIds, Does.Contain("pack_hunt_inspector"));

            var rewardCardIds = configs.RewardTables
                .SelectMany(value => value.Entries)
                .Select(value => value.CardId)
                .ToList();
            Assert.That(rewardCardIds, Does.Contain("traveling_physician"));
            Assert.That(rewardCardIds, Does.Contain("ember_engraver"));
            Assert.That(rewardCardIds, Does.Contain("fox_den_matriarch"));
        }

        [Test]
        public void FurnaceKingAndGrowthFinishers_HaveR1Caps()
        {
            var furnace = configs.MinionsById["undying_furnace_king"];
            var normalTransfer = furnace.Effects.Single(value =>
                value.Id == "undying_furnace_king_transfer");
            var goldenTransfer = furnace.GoldenEffects.Single(value =>
                value.Id == "golden_undying_furnace_king_transfer");
            Assert.That(normalTransfer.Limit.PerCombat, Is.EqualTo(2));
            Assert.That(goldenTransfer.Limit.PerCombat, Is.EqualTo(4));
            Assert.That(normalTransfer.Condition.Type, Is.EqualTo("HasUnshieldedRaceTarget"));
            Assert.That(goldenTransfer.Condition.Type, Is.EqualTo("HasUnshieldedRaceTarget"));

            var oathbroken = configs.MinionsById["oathbroken_blade_soul"];
            Assert.That(oathbroken.Effects.Concat(oathbroken.GoldenEffects),
                Has.None.Matches<EffectConfig>(value =>
                    value.Trigger == "OnShieldLost" &&
                    value.Value?.Duration == "Permanent"));

            var treasurer = configs.MinionsById["star_ring_treasurer"];
            Assert.That(treasurer.Effects.Single(value =>
                value.Id == "star_ring_treasurer_shield").Target.MaxTargets,
                Is.EqualTo(1));
            Assert.That(treasurer.GoldenEffects.Single(value =>
                value.Id == "golden_star_ring_treasurer_shield").Target.MaxTargets,
                Is.EqualTo(2));

            var finalBloom = configs.MinionsById["world_eating_final_bloom"];
            Assert.That(finalBloom.GoldenEffects.Single(value =>
                value.Id == "golden_world_eating_final_bloom_death").Value.Duration,
                Is.EqualTo("Combat"));
            Assert.That(finalBloom.GoldenEffects.Single(value =>
                value.Id == "golden_world_eating_final_bloom_death_permanent")
                .Limit.PerCombat, Is.EqualTo(2));
        }

        [Test]
        public void BreakAndDeathFinishers_HaveCurrentTriggerAndGrowthRules()
        {
            var oathbroken = configs.MinionsById["oathbroken_blade_soul"];
            Assert.That(oathbroken.Effects.Where(effect =>
                    effect.Trigger == "OnShieldLost"),
                Has.All.Matches<EffectConfig>(effect =>
                    effect.Condition?.Type == "SubjectIsSelf"));
            Assert.That(oathbroken.GoldenEffects.Where(effect =>
                    effect.Trigger == "OnShieldLost"),
                Has.All.Matches<EffectConfig>(effect =>
                    effect.Condition?.Type == "SubjectIsSelf"));

            var avenger = configs.MinionsById["cracked_armor_avenger"];
            Assert.That(avenger.Effects, Has.Count.EqualTo(1));
            Assert.That(avenger.GoldenEffects, Has.Count.EqualTo(1));
            var normalReward = avenger.Effects[0];
            var goldenReward = avenger.GoldenEffects[0];
            Assert.That(normalReward.Trigger, Is.EqualTo("OnDeath"));
            Assert.That(goldenReward.Trigger, Is.EqualTo("OnDeath"));
            Assert.That(normalReward.Action,
                Is.EqualTo("GrantRandomMinionAfterCombat"));
            Assert.That(goldenReward.Action,
                Is.EqualTo("GrantRandomMinionAfterCombat"));
            Assert.That(normalReward.Value.Amount, Is.EqualTo(1));
            Assert.That(goldenReward.Value.Amount, Is.EqualTo(2));
            Assert.That(normalReward.Value.Duration, Is.EqualTo("Run"));
            Assert.That(goldenReward.Value.Duration, Is.EqualTo("Run"));
            Assert.That(normalReward.Discover.CardType, Is.EqualTo("Minion"));
            Assert.That(goldenReward.Discover.CardType, Is.EqualTo("Minion"));
            Assert.That(normalReward.Discover.Race, Is.EqualTo("ForgeSoul"));
            Assert.That(goldenReward.Discover.Race, Is.EqualTo("ForgeSoul"));
            Assert.That(normalReward.Discover.TierMode,
                Is.EqualTo("BelowCurrentTavernTier"));
            Assert.That(goldenReward.Discover.TierMode,
                Is.EqualTo("BelowCurrentTavernTier"));
            Assert.That(normalReward.Limit, Is.Null);
            Assert.That(goldenReward.Limit, Is.Null);

            var mountain = configs.MinionsById["ancient_mountain_spirit"];
            Assert.That(mountain.Effects, Has.Count.EqualTo(1));
            Assert.That(mountain.Effects[0].Value.Duration, Is.EqualTo("Permanent"));
            Assert.That(mountain.Effects[0].Value.Attack, Is.EqualTo(1));
            Assert.That(mountain.Effects[0].Limit.PerCombat, Is.EqualTo(3));
            Assert.That(mountain.GoldenEffects[0].Value.Attack, Is.EqualTo(2));
            Assert.That(mountain.GoldenEffects[0].Value.Health, Is.EqualTo(2));

            var finalBloom = configs.MinionsById["world_eating_final_bloom"];
            var normalPermanentGrowth = finalBloom.Effects.Single(effect =>
                effect.Id == "world_eating_final_bloom_death_permanent");
            Assert.That(normalPermanentGrowth.Value.Attack, Is.EqualTo(1));
            Assert.That(normalPermanentGrowth.Value.Health, Is.EqualTo(1));
            Assert.That(normalPermanentGrowth.Limit.PerCombat, Is.EqualTo(2));
            var copiedHealth = finalBloom.GoldenEffects.Single(effect =>
                effect.Id == "golden_world_eating_final_bloom_death_health");
            Assert.That(copiedHealth.Value.Resource, Is.EqualTo("SubjectHealth"));
            Assert.That(copiedHealth.Value.Duration, Is.EqualTo("Combat"));
            Assert.That(copiedHealth.Limit.PerCombat, Is.EqualTo(2));
            var permanentGrowth = finalBloom.GoldenEffects.Single(effect =>
                effect.Id == "golden_world_eating_final_bloom_death_permanent");
            Assert.That(permanentGrowth.Value.Attack, Is.EqualTo(2));
            Assert.That(permanentGrowth.Value.Health, Is.EqualTo(2));
            Assert.That(permanentGrowth.Limit.PerCombat, Is.EqualTo(2));
        }

        [Test]
        public void ForgeSoulReplenishment_IsRemovedFromLowerTierSupports()
        {
            var keeper = configs.MinionsById["shieldwall_furnace_keeper"];
            Assert.That(keeper.Effects, Has.Count.EqualTo(1));
            Assert.That(keeper.Effects[0].Action, Is.EqualTo("ModifyStats"));
            Assert.That(keeper.Effects[0].Value.Attack, Is.EqualTo(1));
            Assert.That(keeper.Effects[0].Value.Health, Is.EqualTo(2));
            Assert.That(keeper.Effects[0].Limit.PerCombat, Is.EqualTo(1));
            Assert.That(keeper.GoldenEffects[0].Value.Attack, Is.EqualTo(2));
            Assert.That(keeper.GoldenEffects[0].Value.Health, Is.EqualTo(4));
            Assert.That(keeper.GoldenEffects[0].Limit.PerCombat, Is.EqualTo(2));

            var bellGuard = configs.MinionsById["resonance_bell_guard"];
            Assert.That(bellGuard.Effects, Has.Count.EqualTo(1));
            Assert.That(bellGuard.Effects[0].Action, Is.EqualTo("ModifyStats"));
            Assert.That(bellGuard.Effects[0].Value.Health, Is.EqualTo(1));
            Assert.That(bellGuard.GoldenEffects, Has.Count.EqualTo(1));
            Assert.That(bellGuard.GoldenEffects[0].Value.Health, Is.EqualTo(4));

            var officer = configs.MinionsById["hearth_core_aegis_officer"];
            var deathEffect = officer.Effects.Single(effect =>
                effect.Id == "hearth_core_aegis_officer_death");
            Assert.That(deathEffect.Action, Is.EqualTo("ModifyStats"));
            Assert.That(deathEffect.Target.Selector, Is.EqualTo("Random"));
            Assert.That(deathEffect.Value.Attack, Is.EqualTo(2));
            Assert.That(deathEffect.Value.Health, Is.EqualTo(2));
            var goldenDeathEffect = officer.GoldenEffects.Single(effect =>
                effect.Id == "golden_hearth_core_aegis_officer_death");
            Assert.That(goldenDeathEffect.Action, Is.EqualTo("ModifyStats"));
            Assert.That(goldenDeathEffect.Target.MaxTargets, Is.EqualTo(2));

            var allowedMidCombatShieldEffects = configs.Minions
                .Where(minion => minion.Race == "ForgeSoul")
                .SelectMany(minion => minion.Effects.Concat(minion.GoldenEffects))
                .Where(effect => effect.Action == "AddShield" &&
                                 effect.Trigger != "OnBattleStart" &&
                                 effect.Trigger != "OnPlay")
                .Select(effect => effect.Id)
                .ToList();
            Assert.That(allowedMidCombatShieldEffects, Is.EquivalentTo(new[]
            {
                "undying_furnace_king_transfer",
                "golden_undying_furnace_king_transfer",
                "oathbroken_blade_soul_kill",
                "golden_oathbroken_blade_soul_kill",
                "thousand_ring_tomb_guardian_death_shield",
                "golden_thousand_ring_tomb_guardian_death_shield"
            }));
        }

        [Test]
        public void NewStarboundMinions_ExecuteRefreshAndPermanentSpellGrowth()
        {
            var shop = new ShopSession(configs.Minions, configs.Spells, new Random(11));
            Assert.That(shop.StartRound(1).Success, Is.True);

            Assert.That(shop.ClaimRewardMinion(
                configs.MinionsById["star_etched_timekeeper"]).Success, Is.True);
            Assert.That(shop.PlayMinion(
                FindBench(shop, "star_etched_timekeeper"), 0).Success, Is.True);
            Assert.That(shop.ClaimRewardMinion(
                configs.MinionsById["secret_page_refractor"]).Success, Is.True);
            Assert.That(shop.PlayMinion(
                FindBench(shop, "secret_page_refractor"), 1).Success, Is.True);

            Assert.That(shop.Refresh().Success, Is.True);
            Assert.That(shop.FreeRefreshes, Is.EqualTo(0));
            Assert.That(shop.Refresh().Success, Is.True);
            Assert.That(shop.FreeRefreshes, Is.EqualTo(1));

            Assert.That(shop.ClaimRewardSpell(
                configs.SpellsById["delayed_supply"]).Success, Is.True);
            Assert.That(shop.UseSpell(FindBench(shop, "delayed_supply")).Success, Is.True);
            Assert.That(shop.ClaimRewardSpell(
                configs.SpellsById["delayed_supply"]).Success, Is.True);
            Assert.That(shop.UseSpell(FindBench(shop, "delayed_supply")).Success, Is.True);
            Assert.That(shop.ClaimRewardSpell(
                configs.SpellsById["delayed_supply"]).Success, Is.True);
            Assert.That(shop.UseSpell(FindBench(shop, "delayed_supply")).Success, Is.True);
            var refractor = shop.Collection.Battle[1];
            Assert.That(refractor.HasPermanentShield, Is.True);
            Assert.That(refractor.PermanentAttackBonus, Is.EqualTo(2));
            Assert.That(refractor.PermanentHealthBonus, Is.EqualTo(2));
            Assert.That(refractor.PendingCombatModifiers, Is.Empty);
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void AstrolabeCalibrator_BuffsLowestAttackOncePerShopAndResetsNextShop(
            bool golden,
            int attackPerShop)
        {
            var shop = new ShopSession(configs.Minions, configs.Spells, new Random(17));
            Assert.That(shop.StartRound(1).Success, Is.True);

            Assert.That(shop.Collection.TryAddToBench(
                ShopCardInstance.CreateMinion(
                    "astrolabe-lowest",
                    configs.MinionsById["rune_ward_reader"]),
                out var lowestBench), Is.True);
            Assert.That(shop.PlayMinion(lowestBench, 0).Success, Is.True);
            Assert.That(shop.Collection.TryAddToBench(
                ShopCardInstance.CreateMinion(
                    "astrolabe-higher",
                    configs.MinionsById["echo_starchanter"]),
                out var higherBench), Is.True);
            Assert.That(shop.PlayMinion(higherBench, 1).Success, Is.True);
            Assert.That(shop.Collection.TryAddToBench(
                ShopCardInstance.CreateMinion(
                    "astrolabe-source",
                    configs.MinionsById["astrolabe_calibrator"],
                    golden),
                out var sourceBench), Is.True);
            Assert.That(shop.PlayMinion(sourceBench, 2).Success, Is.True);
            SetGold(shop, 20);

            Assert.That(shop.Refresh().Success, Is.True);
            Assert.That(shop.Collection.Battle[0].PermanentAttackBonus,
                Is.EqualTo(attackPerShop));
            Assert.That(shop.Collection.Battle[0].PermanentHealthBonus, Is.Zero);
            Assert.That(shop.Collection.Battle[1].PermanentAttackBonus, Is.Zero);
            Assert.That(shop.Collection.Battle[2].PermanentAttackBonus, Is.Zero);

            Assert.That(shop.Refresh().Success, Is.True);
            Assert.That(shop.Collection.Battle.Sum(card =>
                card?.PermanentAttackBonus ?? 0), Is.EqualTo(attackPerShop),
                "The calibrator must trigger only once in one shop phase.");

            Assert.That(shop.EndRound().Success, Is.True);
            Assert.That(shop.StartRound(2).Success, Is.True);
            SetGold(shop, 20);
            Assert.That(shop.Refresh().Success, Is.True);
            Assert.That(shop.Collection.Battle[0].PermanentAttackBonus,
                Is.EqualTo(attackPerShop * 2),
                "Per-shop usage must reset when the next shop starts.");
            Assert.That(shop.Collection.Battle.Sum(card =>
                card?.PermanentHealthBonus ?? 0), Is.Zero);
        }

        [TestCase(false, 4)]
        [TestCase(true, 3)]
        public void SkyCovenantBearer_BuffsAllStarboundAtConfiguredRefreshInterval(
            bool golden,
            int refreshInterval)
        {
            var sky = configs.MinionsById["sky_covenant_bearer"];
            var effect = (golden ? sky.GoldenEffects : sky.Effects).Single();
            Assert.That(effect.Condition.Type, Is.EqualTo("PhaseStatMultipleOf"));
            Assert.That(effect.Condition.PhaseStat, Is.EqualTo("RefreshCount"));
            Assert.That(effect.Condition.Threshold, Is.EqualTo(refreshInterval));
            Assert.That(effect.Limit, Is.Null);
            Assert.That(effect.Target.Scope, Is.EqualTo("All"));
            Assert.That(effect.Value.Attack, Is.EqualTo(1));
            Assert.That(effect.Value.Health, Is.EqualTo(1));

            var shop = new ShopSession(configs.Minions, configs.Spells, new Random(19));
            Assert.That(shop.StartRound(1).Success, Is.True);
            var allyIds = new[]
            {
                "glimmer_mage",
                "glimmer_mage",
                "glimmer_mage"
            };
            for (var index = 0; index < allyIds.Length; index++)
            {
                Assert.That(shop.Collection.TryAddToBench(
                    ShopCardInstance.CreateMinion(
                        $"sky-test-ally-{index}",
                        configs.MinionsById[allyIds[index]]),
                    out var allyBench), Is.True);
                Assert.That(shop.PlayMinion(allyBench, index).Success, Is.True);
            }
            Assert.That(shop.Collection.TryAddToBench(
                ShopCardInstance.CreateMinion("sky-test-source", sky, golden),
                out var skyBench), Is.True);
            Assert.That(shop.PlayMinion(skyBench, 3).Success, Is.True);
            SetGold(shop, 20);

            for (var refresh = 1; refresh <= 12; refresh++)
            {
                Assert.That(shop.Refresh().Success, Is.True);
                var expected = refresh / refreshInterval;
                var cards = shop.Collection.Battle.Where(card => card != null).ToList();
                Assert.That(cards.All(card =>
                    card.PermanentAttackBonus == expected), Is.True,
                    $"attack after refresh {refresh}");
                Assert.That(cards.All(card =>
                    card.PermanentHealthBonus == expected), Is.True,
                    $"health after refresh {refresh}");
            }
        }

        [Test]
        public void SameSummonEvent_DeduplicatesImmediateAttackAcrossMultipleSources()
        {
            var state = new BattleBoardState();
            state.Player[0] = new BattleMinionRuntime(configs.MinionsById["hundred_song_herd"]);
            state.Player[1] = new BattleMinionRuntime(configs.MinionsById["ten_thousand_hoof_surge"]);
            state.Enemy[0] = new BattleMinionRuntime(
                configs.MinionsById["mirrorsteel_duelist"],
                initialAttack: 100,
                initialHealth: 200);

            var result = CreateSimulator().SimulatePlayback(state);
            var immediateAttackCount = result.Log.Count(message =>
                message.Contains("迅捷幼灵") && message.Contains("立即攻击"));

            Assert.That(immediateAttackCount, Is.EqualTo(2),
                "Each of the two summon events should enqueue one immediate attack.");
        }

        [Test]
        public void GoldenHundredSongHerd_SummonsFourTwoOneSpiritsWithImmediateAttacks()
        {
            var hundredSong = configs.MinionsById["hundred_song_herd"];
            var summon = hundredSong.GoldenEffects.Single(effect =>
                effect.Action == "SummonToken");
            Assert.That(summon.Value.Attack, Is.EqualTo(2));
            Assert.That(summon.Value.Health, Is.EqualTo(1));
            Assert.That(summon.Value.Amount, Is.EqualTo(4));

            var state = new BattleBoardState();
            state.Player[0] = new BattleMinionRuntime(
                hundredSong,
                true,
                initialHealth: 1);
            state.Enemy[0] = new BattleMinionRuntime(
                configs.MinionsById["mirrorsteel_duelist"],
                initialAttack: 100,
                initialHealth: 500);

            var result = CreateSimulator().SimulatePlayback(state);
            var immediateAttackCount = result.Log.Count(message =>
                message.Contains("迅捷幼灵") && message.Contains("立即攻击"));

            Assert.That(result.FinalState.PlayerFlourishStacks, Is.EqualTo(1));
            Assert.That(result.Diagnostics.Player.SummonSuccesses, Is.EqualTo(4));
            Assert.That(immediateAttackCount, Is.EqualTo(4));
        }

        [Test]
        public void GoldenFoxMatriarch_UsesNestedTokenChainAndKeepsTokensOutOfWriteback()
        {
            var state = new BattleBoardState();
            state.Player[0] = new BattleMinionRuntime(
                configs.MinionsById["fox_den_matriarch"],
                true,
                sourceInstanceId: "matriarch");
            state.Player[1] = new BattleMinionRuntime(
                configs.MinionsById["moss_mark_seedling"],
                sourceInstanceId: "seedling");
            state.Enemy[0] = new BattleMinionRuntime(
                configs.MinionsById["mirrorsteel_duelist"],
                initialAttack: 100,
                initialHealth: 300);

            var result = CreateSimulator().SimulatePlayback(state);
            var foxStep = result.Steps.First(step => step.BoardState.Player.Any(value =>
                value?.Id == "token_two_tailed_fox_shadow"));
            var fox = foxStep.BoardState.Player.First(value =>
                value?.Id == "token_two_tailed_fox_shadow");
            Assert.That(fox.CurrentAttack, Is.EqualTo(4));
            Assert.That(fox.CurrentHealth, Is.EqualTo(4));
            Assert.That(fox.SourceInstanceId, Is.Null);

            var youngSpiritStep = result.Steps.First(step => step.BoardState.Player.Any(value =>
                value?.Id == "token_young_spirit"));
            var youngSpirit = youngSpiritStep.BoardState.Player.First(value =>
                value?.Id == "token_young_spirit");
            Assert.That(youngSpirit.CurrentAttack, Is.EqualTo(2));
            Assert.That(youngSpirit.CurrentHealth, Is.EqualTo(2));
            Assert.That(youngSpirit.SourceInstanceId, Is.Null);

            var seedlingDelta = result.PermanentDeltas.Single(value =>
                value.SourceInstanceId == "seedling");
            Assert.That(seedlingDelta.Health, Is.EqualTo(1),
                "Only the non-token matriarch death may grant permanent growth.");
            Assert.That(result.PermanentDeltas.Any(value =>
                value.SourceInstanceId == null), Is.False);
        }

        [Test]
        public void FoxShadow_SingleOpenSlotReusesSlotThenFailsSecondYoungSpirit()
        {
            var state = new BattleBoardState();
            state.Player[0] = new BattleMinionRuntime(configs.MinionsById["fox_den_matriarch"]);
            state.Player[1] = new BattleMinionRuntime(
                configs.MinionsById["thousand_ring_tomb_guardian"]);
            state.Player[2] = new BattleMinionRuntime(configs.MinionsById["moss_mark_seedling"]);
            state.Player[3] = new BattleMinionRuntime(configs.MinionsById["root_devourer"]);
            state.Player[4] = new BattleMinionRuntime(configs.MinionsById["many_branch_invoker"]);
            state.Enemy[0] = new BattleMinionRuntime(
                configs.MinionsById["mirrorsteel_duelist"],
                initialAttack: 6,
                initialHealth: 300);

            var result = CreateSimulator().SimulatePlayback(state);
            var youngSpiritSummons = result.Log.Count(message =>
                message.Contains("召唤了 幼灵"));
            Assert.That(youngSpiritSummons, Is.EqualTo(1));
            Assert.That(result.Log.Any(message => message.Contains("没有空位")), Is.True);
        }

        [Test]
        public void ManyArtsApprentice_CopiesOnlyAdjacentCombatKeywords()
        {
            var state = new BattleBoardState();
            state.Player[0] = new BattleMinionRuntime(
                configs.MinionsById["formation_breaker_mercenary"],
                permanentKeywords: new[] { "Taunt", "Cleave", "Shield" });
            state.Player[1] = new BattleMinionRuntime(
                configs.MinionsById["many_arts_apprentice"],
                sourceInstanceId: "apprentice");
            state.Enemy[0] = new BattleMinionRuntime(
                configs.MinionsById["mirrorsteel_duelist"],
                initialAttack: 0,
                initialHealth: 200);

            var result = CreateSimulator().SimulatePlayback(state);
            var copied = result.Steps.Select(step => step.BoardState.Player[1])
                .First(value => value != null && value.HasTaunt && value.HasShield && value.HasCleave);
            Assert.That(copied.Keywords, Does.Contain("Taunt"));
            Assert.That(copied.Keywords, Does.Contain("Shield"));
            Assert.That(copied.Keywords, Does.Contain("Cleave"));
            Assert.That(result.PermanentDeltas.Any(value =>
                value.SourceInstanceId == "apprentice"), Is.False);
        }

        [Test]
        public void EnemyTokenObserverAndMirrorsteelDifference_ApplyConfiguredCaps()
        {
            var state = new BattleBoardState();
            state.Player[0] = new BattleMinionRuntime(configs.MinionsById["pack_hunt_inspector"]);
            state.Enemy[0] = new BattleMinionRuntime(configs.MinionsById["hundred_song_herd"]);
            var result = CreateSimulator().SimulatePlayback(state);
            Assert.That(result.Steps.Any(step => step.BoardState.Player[0] != null &&
                step.BoardState.Player[0].CurrentAttack >= 7 &&
                step.BoardState.Player[0].HasCleave), Is.True);

            var differenceState = new BattleBoardState();
            differenceState.Player[0] = new BattleMinionRuntime(
                configs.MinionsById["mirrorsteel_duelist"], true);
            differenceState.Enemy[0] = new BattleMinionRuntime(
                configs.MinionsById["formation_breaker_mercenary"],
                initialAttack: 40,
                initialHealth: 200);
            var differenceResult = CreateSimulator().SimulatePlayback(differenceState);
            Assert.That(differenceResult.Steps.Any(step =>
                step.BoardState.Player[0]?.CurrentAttack == 30 &&
                step.BoardState.Player[0].HasCleave), Is.True);
        }

        private BattleSimulator CreateSimulator()
        {
            return new BattleSimulator(
                new Random(7),
                id => configs.MinionsById.TryGetValue(id, out var value) ? value : null);
        }

        private static int FindBench(ShopSession shop, string configId)
        {
            for (var i = 0; i < shop.Collection.Bench.Count; i++)
            {
                if (shop.Collection.Bench[i]?.ConfigId == configId)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void SetGold(ShopSession shop, int value)
        {
            var setter = typeof(ShopSession).GetProperty(
                    nameof(ShopSession.Gold),
                    BindingFlags.Instance | BindingFlags.Public)
                ?.GetSetMethod(true);
            Assert.That(setter, Is.Not.Null);
            setter.Invoke(shop, new object[] { value });
        }
    }
}

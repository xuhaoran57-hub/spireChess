using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Run;
using SpireChess.Utils;

namespace SpireChess.Tests.EditMode
{
    public sealed class ChapterEncounterIdentityTests
    {
        private ConfigService configs;

        [SetUp]
        public void SetUp()
        {
            configs = new ConfigService(new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            Assert.That(
                validation.IsValid,
                Is.True,
                string.Join("\n", validation.Errors));
        }

        [Test]
        public void MapAndEventEncounters_UseOnlyTheirChapterFaction()
        {
            foreach (var map in configs.RunMaps)
            {
                var encounterIds = map.Nodes
                    .Where(node =>
                        node.Type == "Normal" ||
                        node.Type == "Elite" ||
                        node.Type == "Boss")
                    .Select(node => node.PayloadId)
                    .Concat(new[]
                    {
                        $"f{map.Floor}_c4_event_ambush_encounter"
                    })
                    .Distinct()
                    .ToArray();

                foreach (var encounterId in encounterIds)
                {
                    var encounter = configs.EncountersById[encounterId];
                    Assert.That(
                        encounter.EnemySlots.Select(slot =>
                            configs.MinionsById[slot.MinionId].Race),
                        Has.All.EqualTo(map.ThemeFaction),
                        $"{map.Id}/{encounterId}");
                }
            }
        }

        [Test]
        public void EveryMapEncounter_ExpressesItsChapterSignature()
        {
            foreach (var map in configs.RunMaps)
            {
                foreach (var node in map.Nodes.Where(value =>
                             value.Type == "Normal" ||
                             value.Type == "Elite" ||
                             value.Type == "Boss"))
                {
                    var encounter = configs.EncountersById[node.PayloadId];
                    var minions = encounter.EnemySlots
                        .Select(slot => configs.MinionsById[slot.MinionId])
                        .ToArray();

                    switch (map.ThemeFaction)
                    {
                        case "WildSpirit":
                            Assert.That(
                                minions.Any(IsWildSummonOrDeathUnit),
                                Is.True,
                                encounter.Id);
                            break;
                        case "Starbound":
                            Assert.That(
                                CountPreparedShieldOrCleave(encounter) >= 2,
                                Is.True,
                                encounter.Id);
                            break;
                        case "ForgeSoul":
                            Assert.That(
                                minions.Count(IsForgeShieldUnit) >= 4,
                                Is.True,
                                encounter.Id);
                            break;
                    }
                }
            }
        }

        [Test]
        public void LateBranches_CreateDifferentThreatProfiles()
        {
            var wildTempo = configs.EncountersById["f1_c5_shield_encounter"];
            var wildNest = configs.EncountersById["f1_c5_summon_encounter"];
            Assert.That(
                HasMinionTag(wildTempo, "immediate_attack"),
                Is.True);
            Assert.That(
                wildNest.EnemySlots.Any(slot =>
                    slot.MinionId == "fox_den_matriarch"),
                Is.True);

            var starDefense = Totals("f2_c5_break_encounter");
            var starBurst = Totals("f2_c5_spell_encounter");
            Assert.That(starBurst.Attack - starDefense.Attack, Is.EqualTo(6));
            Assert.That(starDefense.Health - starBurst.Health, Is.EqualTo(8));
            Assert.That(starDefense.DamageBonus, Is.Zero);
            Assert.That(starBurst.DamageBonus, Is.EqualTo(1));

            var forgeWall = Totals("f3_c5_forge_encounter");
            var forgeBlade = Totals("f3_c5_wild_encounter");
            Assert.That(forgeBlade.Attack - forgeWall.Attack, Is.EqualTo(-13));
            Assert.That(forgeWall.Health - forgeBlade.Health, Is.EqualTo(66));
            Assert.That(forgeWall.DamageBonus, Is.EqualTo(1));
            Assert.That(forgeBlade.DamageBonus, Is.EqualTo(2));
        }

        [Test]
        public void F3FrontAndMidEncounters_StageGoldenShieldDensity()
        {
            var opening = configs.EncountersById["f3_opening_encounter"];
            var early = configs.EncountersById["f3_early_summon_encounter"];
            var normal = configs.EncountersById["f3_normal_encounter"];
            var mid = configs.EncountersById["f3_mid_mechanic_encounter"];
            var elite = configs.EncountersById["f3_c4_elite_encounter"];
            var eventAmbush = configs.EncountersById[
                "f3_c4_event_ambush_encounter"];

            Assert.That(opening.EnemySlots.Count(slot => slot.Golden), Is.Zero);
            Assert.That(early.EnemySlots.Count(slot => slot.Golden), Is.Zero);
            Assert.That(normal.EnemySlots.Count(slot => slot.Golden), Is.Zero);
            Assert.That(mid.EnemySlots.Count(slot => slot.Golden), Is.EqualTo(1));
            Assert.That(
                mid.EnemySlots.Single(slot => slot.Golden).MinionId,
                Is.EqualTo("oathbroken_blade_soul"));
            Assert.That(elite.EnemySlots.Count(slot => slot.Golden), Is.EqualTo(2));
            Assert.That(
                eventAmbush.EnemySlots.Count(slot => slot.Golden),
                Is.EqualTo(2));
        }

        [Test]
        public void ChapterBosses_EscalateStatsAndLossPressure()
        {
            var bosses = new[]
            {
                Totals("f1_c6_boss_encounter"),
                Totals("f2_c6_boss_encounter"),
                Totals("f3_c6_boss_encounter")
            };

            Assert.That(
                bosses.Select(value => value.Attack),
                Is.EqualTo(new[] { 13, 48, 85 }));
            Assert.That(
                bosses.Select(value => value.Health),
                Is.EqualTo(new[] { 23, 60, 100 }));
            Assert.That(
                bosses.Select(value => value.DamageBonus),
                Is.EqualTo(new[] { 2, 3, 4 }));
        }

        [Test]
        public void ThreatRatings_EscalateAcrossChaptersAndSeparateRoutes()
        {
            var provider = new FixedMapProvider(
                configs.RunMaps,
                configs.MapRuleProfilesById);
            var maps = configs.RunMaps
                .OrderBy(map => map.Floor)
                .Select(map => provider.CreateMapById(map.Id))
                .ToArray();

            Assert.That(
                maps.Select(map => Rating(
                    map,
                    map.Nodes.Single(node => node.CombatIndex == 1))),
                Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(
                maps.Select(map => Rating(
                    map,
                    map.Nodes.Single(node => node.CombatIndex == 3))),
                Is.EqualTo(new[] { 2, 3, 4 }));
            Assert.That(
                maps.Select(map => Rating(
                    map,
                    map.Nodes.Single(node => node.Type == RunNodeType.Boss))),
                Is.EqualTo(new[] { 4, 5, 5 }));

            Assert.That(
                maps.Select(map => RouteRatings(map)),
                Is.EqualTo(new[]
                {
                    new[] { 2, 3, 4 },
                    new[] { 2, 3, 4 },
                    new[] { 3, 4, 5 }
                }));
        }

        [Test]
        public void ValidatorRejectsCrossFactionAndUnsupportedEncounterKeyword()
        {
            var opening = configs.EncountersById["f1_opening_encounter"];
            opening.EnemySlots[0].MinionId = "copper_ring_apprentice";
            opening.EnemySlots[0].PermanentKeywords.Add("Poison");

            var validation = ValidateCurrentConfigs();
            var errors = string.Join("\n", validation.Errors);
            Assert.That(validation.IsValid, Is.False);
            Assert.That(errors, Does.Contain("expected chapter faction WildSpirit"));
            Assert.That(errors, Does.Contain("invalid permanent keyword Poison"));
        }

        [Test]
        public void ValidatorRejectsCrossFactionEventFollowup()
        {
            var ambush = configs.EncountersById[
                "f2_c4_event_ambush_encounter"];
            ambush.EnemySlots[0].MinionId = "root_devourer";

            var validation = ValidateCurrentConfigs();
            var errors = string.Join("\n", validation.Errors);
            Assert.That(validation.IsValid, Is.False);
            Assert.That(errors, Does.Contain("event follow-up ravine_ambush_f2"));
            Assert.That(errors, Does.Contain("expected chapter faction Starbound"));
        }

        [Test]
        public void ValidatorRejectsStaleFormationAndLossPressureText()
        {
            var encounter = configs.EncountersById[
                "f1_c6_boss_encounter"];
            encounter.EnemySlots[0].AttackBonus += 1;
            encounter.DamageBonus += 1;

            var validation = ValidateCurrentConfigs();
            var errors = string.Join("\n", validation.Errors);
            Assert.That(validation.IsValid, Is.False);
            Assert.That(errors, Does.Contain(
                "risk text must contain the current formation target '目标 14/23'"));
            Assert.That(errors, Does.Contain(
                "risk text must contain the current loss pressure '失败修正 +3'"));
        }

        [Test]
        public void ChapterBosses_SimulateTheirSignatureMechanics()
        {
            var bossIds = new[]
            {
                "f1_c6_boss_encounter",
                "f2_c6_boss_encounter",
                "f3_c6_boss_encounter"
            };

            for (var index = 0; index < bossIds.Length; index++)
            {
                var board = CreateMirroredBoard(bossIds[index]);
                var result = new BattleSimulator(
                    new Random(5600 + index),
                    id => configs.MinionsById[id]).Simulate(board);
                var player = result.Diagnostics.Player;
                var enemy = result.Diagnostics.Enemy;

                Assert.That(result.Diagnostics.HitEffectLimit, Is.False, bossIds[index]);
                Assert.That(
                    result.Diagnostics.RoundCount,
                    Is.LessThanOrEqualTo(BattleSimulator.MaxRounds),
                    bossIds[index]);

                switch (index)
                {
                    case 0:
                        Assert.That(
                            player.SummonAttempts + enemy.SummonAttempts,
                            Is.GreaterThan(0));
                        Assert.That(
                            player.SummonSuccesses + enemy.SummonSuccesses,
                            Is.GreaterThan(0));
                        break;
                    case 1:
                        Assert.That(
                            player.CleaveHits + enemy.CleaveHits,
                            Is.GreaterThan(0));
                        Assert.That(
                            player.ShieldDamageBlocks +
                            enemy.ShieldDamageBlocks,
                            Is.GreaterThan(0));
                        break;
                    case 2:
                        Assert.That(
                            player.ShieldsGranted + enemy.ShieldsGranted,
                            Is.GreaterThan(0));
                        Assert.That(
                            player.ShieldsLost + enemy.ShieldsLost,
                            Is.GreaterThan(0));
                        Assert.That(
                            player.FurnaceTransfers + enemy.FurnaceTransfers,
                            Is.GreaterThan(0));
                        break;
                }
            }
        }

        private static bool IsWildSummonOrDeathUnit(MinionConfig minion)
        {
            return minion.Keywords.Contains("Deathrattle") ||
                   minion.Effects.Any(effect =>
                       effect.Action == "SummonToken" ||
                       effect.Trigger == "OnFriendlyDeath" ||
                       effect.Trigger == "OnSummon" ||
                       effect.Trigger == "OnSummonedUnitDeath");
        }

        private int CountPreparedShieldOrCleave(EncounterConfig encounter)
        {
            return encounter.EnemySlots.Count(slot =>
            {
                var minion = configs.MinionsById[slot.MinionId];
                return minion.Keywords.Any(keyword =>
                           keyword == "Shield" || keyword == "Cleave") ||
                       slot.PermanentKeywords.Any(keyword =>
                           keyword == "Shield" || keyword == "Cleave");
            });
        }

        private static bool IsForgeShieldUnit(MinionConfig minion)
        {
            return minion.Keywords.Any(keyword =>
                       keyword == "Shield" || keyword == "Taunt") ||
                   minion.Effects.Any(effect =>
                       effect.Trigger == "OnShieldLost" ||
                       effect.Action == "AddShield");
        }

        private bool HasMinionTag(EncounterConfig encounter, string tag)
        {
            return encounter.EnemySlots.Any(slot =>
                configs.MinionsById[slot.MinionId].Tags.Contains(tag));
        }

        private int Rating(MapDefinition map, MapNodeDefinition node)
        {
            var encounter = configs.EncountersById[node.PayloadId];
            return ChapterThreatRating.Calculate(
                map.Floor,
                node.CombatIndex,
                node.Type,
                node.RouteTag,
                encounter.DamageBonus);
        }

        private int[] RouteRatings(MapDefinition map)
        {
            return new[]
            {
                "Conservative",
                "Adventure",
                "Aggressive"
            }.Select(routeTag => Rating(
                map,
                map.Nodes.Single(node =>
                    string.Equals(
                        node.RouteTag,
                        routeTag,
                        StringComparison.Ordinal))))
                .ToArray();
        }

        private ConfigValidationResult ValidateCurrentConfigs()
        {
            return RunContentValidator.Validate(
                configs.RunMaps,
                configs.MapRuleProfiles,
                configs.Encounters,
                configs.RewardTables,
                configs.MinionsById,
                configs.SpellsById,
                configs.EventPoolsById,
                configs.EventsById,
                configs.EnhancementRecipesById,
                configs.EnhanceNodesById,
                configs.RestNodesById);
        }

        private BattleBoardState CreateMirroredBoard(string encounterId)
        {
            var encounter = configs.EncountersById[encounterId];
            var board = new BattleBoardState();
            foreach (var slot in encounter.EnemySlots)
            {
                var minion = configs.MinionsById[slot.MinionId];
                board.Player[slot.Slot] = new BattleMinionRuntime(
                    minion,
                    slot.Golden,
                    permanentAttackBonus: slot.AttackBonus,
                    permanentHealthBonus: slot.HealthBonus,
                    permanentKeywords: slot.PermanentKeywords);
                board.Enemy[slot.Slot] = new BattleMinionRuntime(
                    minion,
                    slot.Golden,
                    permanentAttackBonus: slot.AttackBonus,
                    permanentHealthBonus: slot.HealthBonus,
                    permanentKeywords: slot.PermanentKeywords);
            }

            return board;
        }

        private EncounterTotals Totals(string encounterId)
        {
            var encounter = configs.EncountersById[encounterId];
            var attack = 0;
            var health = 0;
            foreach (var slot in encounter.EnemySlots)
            {
                var minion = configs.MinionsById[slot.MinionId];
                attack += (slot.Golden
                    ? minion.GoldenAttack
                    : minion.Attack) + slot.AttackBonus;
                health += (slot.Golden
                    ? minion.GoldenHealth
                    : minion.Health) + slot.HealthBonus;
            }

            return new EncounterTotals(
                attack,
                health,
                encounter.DamageBonus);
        }

        private sealed class EncounterTotals
        {
            public EncounterTotals(int attack, int health, int damageBonus)
            {
                Attack = attack;
                Health = health;
                DamageBonus = damageBonus;
            }

            public int Attack { get; }
            public int Health { get; }
            public int DamageBonus { get; }
        }
    }
}

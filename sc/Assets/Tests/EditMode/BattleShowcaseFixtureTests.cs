using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Shop;
using SpireChess.Utils;

namespace SpireChess.Tests.EditMode
{
    public sealed class BattleShowcaseFixtureTests
    {
        [Test]
        public void Showcase_UsesShopBattlecryAndBattleEventSequence()
        {
            var configs = new ConfigService(new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            Assert.That(validation.IsValid, Is.True,
                string.Join("\n", validation.Errors));

            var shop = new ShopSession(
                configs.Minions,
                configs.Spells,
                new Random(40606));
            Assert.That(shop.StartRound(1).Success, Is.True);
            Assert.That(
                shop.Collection.TryAddToBench(
                    ShopCardInstance.CreateMinion(
                        "showcase:cub",
                        configs.MinionsById["rending_cub"]),
                    out var cubBenchIndex),
                Is.True);
            Assert.That(shop.PlayMinion(cubBenchIndex, 3).Success, Is.True);

            var beforeBattlecry = shop.CreateBattleSnapshot();
            var events = new List<ShopEventData>();
            shop.EventRaised += events.Add;
            Assert.That(
                shop.Collection.TryAddToBench(
                    ShopCardInstance.CreateMinion(
                        "showcase:physician",
                        configs.MinionsById["traveling_physician"]),
                    out var physicianBenchIndex),
                Is.True);
            Assert.That(
                shop.PlayMinion(physicianBenchIndex, 4, 3).Success,
                Is.True);

            var physicianPlay = events.Single(value =>
                value.Type == ShopEventType.OnPlay &&
                value.Card?.InstanceId == "showcase:physician");
            var battleState = shop.CreateBattleSnapshot();
            Assert.That(
                physicianPlay.TargetCard?.InstanceId,
                Is.EqualTo("showcase:cub"));
            Assert.That(
                battleState.Player[3].CurrentHealth,
                Is.EqualTo(beforeBattlecry.Player[3].CurrentHealth + 1));

            ConfigureBattleState(battleState, configs);
            var simulator = new BattleSimulator(
                new Random(40606),
                id => configs.MinionsById.TryGetValue(id, out var config)
                    ? config
                    : null);
            var result = simulator.SimulatePlayback(battleState);
            var playback = result.PlaybackEvents.ToList();

            Assert.That(
                playback.Any(value =>
                    value.Kind == BattlePlaybackEventKind.EffectTriggered &&
                    value.EffectId == "mercenary_shieldbearer_start" &&
                    value.EffectTrigger == "OnBattleStart" &&
                    value.EffectAction == "AddShield"),
                Is.True);

            var cleaveAttack = playback.First(value =>
                value.Kind == BattlePlaybackEventKind.AttackStarted &&
                value.SourceSide == BattleSide.Player &&
                value.SourceIndex == 0);
            var splashTargets = playback.Where(value =>
                    value.Kind == BattlePlaybackEventKind.DamageApplied &&
                    value.IsSplashDamage &&
                    value.SourceSide == BattleSide.Player &&
                    value.SourceIndex == 0)
                .Select(value => value.TargetIndex)
                .ToArray();
            Assert.That(cleaveAttack.TargetIndex, Is.EqualTo(2));
            Assert.That(splashTargets, Is.EqualTo(new[] { 1, 3 }));

            var deathrattleIndex = playback.FindIndex(value =>
                value.Kind == BattlePlaybackEventKind.EffectTriggered &&
                value.EffectId == "fox_den_matriarch_death" &&
                value.EffectTrigger == "OnDeath");
            var summonIndex = playback.FindIndex(value =>
                value.Kind == BattlePlaybackEventKind.UnitSummoned &&
                value.EffectId == "fox_den_matriarch_death");
            Assert.That(deathrattleIndex, Is.GreaterThan(0));
            Assert.That(
                playback.Take(deathrattleIndex).Any(value =>
                    value.Kind == BattlePlaybackEventKind.UnitDied &&
                    value.TargetSide == BattleSide.Enemy &&
                    value.TargetIndex == 3),
                Is.True);
            Assert.That(summonIndex, Is.GreaterThan(deathrattleIndex));
        }

        private static void ConfigureBattleState(
            BattleBoardState state,
            ConfigService configs)
        {
            state.Player[0] = CreateMinion(
                configs,
                "mirrorsteel_duelist",
                true,
                initialHealth: 40);
            state.Player[1] = CreateMinion(
                configs,
                "forge_soul_shield_squire",
                false,
                initialHealth: 10);
            state.Player[2] = CreateMinion(
                configs,
                "mercenary_shieldbearer",
                false,
                initialHealth: 70);
            state.Enemy[1] = CreateMinion(
                configs,
                "wandering_swordsman",
                false,
                initialHealth: 8);
            state.Enemy[2] = CreateMinion(
                configs,
                "mercenary_shieldbearer",
                false,
                initialAttack: 22,
                initialHealth: 27);
            state.Enemy[3] = CreateMinion(
                configs,
                "fox_den_matriarch",
                false,
                initialAttack: 1,
                initialHealth: 5);
        }

        private static BattleMinionRuntime CreateMinion(
            ConfigService configs,
            string id,
            bool golden,
            int? initialAttack = null,
            int? initialHealth = null)
        {
            return new BattleMinionRuntime(
                configs.MinionsById[id],
                golden,
                initialAttack,
                initialHealth);
        }
    }
}

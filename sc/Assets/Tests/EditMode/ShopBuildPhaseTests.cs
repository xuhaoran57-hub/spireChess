using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SpireChess.Config;
using SpireChess.Shop;

namespace SpireChess.Tests
{
    public sealed class ShopBuildPhaseTests
    {
        [Test]
        public void OnPlay_ResourceBattlecryAppliesAfterPlacement()
        {
            var minion = CreateMinion("refresh_minion");
            minion.Effects.Add(CreateResourceEffect("FreeRefresh", 1));
            var session = CreateSession();
            session.Collection.TryAddToBench(
                ShopCardInstance.CreateMinion("refresh_instance", minion),
                out var benchIndex);
            var events = new List<ShopEventData>();
            session.EventRaised += events.Add;
            session.StartNextRound();

            var result = session.PlayMinion(benchIndex, 0);

            Assert.That(result.Success, Is.True);
            Assert.That(session.Collection.Bench[benchIndex], Is.Null);
            Assert.That(session.Collection.Battle[0].InstanceId,
                Is.EqualTo("refresh_instance"));
            Assert.That(session.FreeRefreshes, Is.EqualTo(1));
            Assert.That(events.Last().Type, Is.EqualTo(ShopEventType.OnPlay));
        }

        [Test]
        public void OnPlay_GoldenMinionUsesGoldenEffectsOnly()
        {
            var minion = CreateMinion("golden_vendor");
            minion.Effects.Add(CreateResourceEffect("GainGold", 1));
            minion.GoldenEffects.Add(CreateResourceEffect("GainGold", 2));
            var session = CreateSession();
            session.Collection.TryAddToBench(
                ShopCardInstance.CreateMinion("golden_vendor", minion, true),
                out var benchIndex);
            session.StartNextRound();

            var result = session.PlayMinion(benchIndex, 0);

            Assert.That(result.Success, Is.True);
            Assert.That(session.Gold, Is.EqualTo(5));
        }

        [Test]
        public void OnPlay_NoLegalTargetStillPlacesMinionAndSkipsEffect()
        {
            var minion = CreateMinion("targeted_minion");
            minion.Effects.Add(CreateStatEffect("OnPlay", 1, 1));
            var session = CreateSession();
            session.Collection.TryAddToBench(
                ShopCardInstance.CreateMinion("targeted_instance", minion),
                out var benchIndex);
            session.StartNextRound();

            var result = session.PlayMinion(benchIndex, 0);

            Assert.That(result.Success, Is.True);
            Assert.That(session.Collection.Battle[0].PermanentAttackBonus,
                Is.EqualTo(0));
            Assert.That(session.Collection.Battle[0].PermanentHealthBonus,
                Is.EqualTo(0));
        }

        [Test]
        public void OnPlay_FriendlyTargetExcludesSourceAndUsesFinalBattleBoard()
        {
            var sourceConfig = CreateMinion("source");
            sourceConfig.Effects.Add(CreateStatEffect("OnPlay", 1, 1));
            var session = CreateSession();
            session.Collection.TryAddToBench(
                ShopCardInstance.CreateMinion("ally", CreateMinion("ally")),
                out var allyBenchIndex);
            session.Collection.TryAddToBench(
                ShopCardInstance.CreateMinion("source", sourceConfig),
                out var sourceBenchIndex);
            session.StartNextRound();
            session.PlayMinion(allyBenchIndex, 0);

            var invalid = session.PlayMinion(sourceBenchIndex, 1, 1);
            var valid = session.PlayMinion(sourceBenchIndex, 1, 0);

            Assert.That(invalid.Error, Is.EqualTo(ShopOperationError.InvalidTarget));
            Assert.That(valid.Success, Is.True);
            Assert.That(session.Collection.Battle[0].PermanentAttackBonus,
                Is.EqualTo(1));
            Assert.That(session.Collection.Battle[1].PermanentAttackBonus,
                Is.EqualTo(0));
        }

        [Test]
        public void Spell_TargetsBattleOnlyAndConsumesOnlyAfterSuccess()
        {
            var session = CreateSession();
            session.Collection.TryAddToBench(
                ShopCardInstance.CreateMinion("battle", CreateMinion("battle")),
                out var battleBenchIndex);
            session.Collection.TryAddToBench(
                ShopCardInstance.CreateMinion("bench", CreateMinion("bench")),
                out _);
            session.Collection.TryAddToBench(
                ShopCardInstance.CreateSpell(
                    "spell",
                    CreateSpell("spell", CreateStatEffect("Manual", 1, 1))),
                out var spellBenchIndex);
            session.StartNextRound();
            session.PlayMinion(battleBenchIndex, 0);

            var invalid = session.UseSpell(spellBenchIndex, 1);

            Assert.That(invalid.Error, Is.EqualTo(ShopOperationError.InvalidTarget));
            Assert.That(session.Collection.Bench[spellBenchIndex], Is.Not.Null);

            var success = session.UseSpell(spellBenchIndex, 0);

            Assert.That(success.Success, Is.True);
            Assert.That(session.Collection.Bench[spellBenchIndex], Is.Null);
            Assert.That(session.Collection.Battle[0].PermanentAttackBonus,
                Is.EqualTo(1));
            Assert.That(session.Collection.Battle[0].PermanentHealthBonus,
                Is.EqualTo(1));
        }

        [Test]
        public void Spell_ZeroBenefitDoesNotConsumeCard()
        {
            var session = CreateSession();
            session.Collection.TryAddToBench(
                ShopCardInstance.CreateSpell("empty_spell", CreateSpell("empty_spell")),
                out var benchIndex);
            session.StartNextRound();

            var result = session.UseSpell(benchIndex);

            Assert.That(result.Error, Is.EqualTo(ShopOperationError.NoBenefit));
            Assert.That(session.Collection.Bench[benchIndex], Is.Not.Null);
        }

        [Test]
        public void FreeRefreshSpell_AddsChargesAndConsumesCard()
        {
            var session = CreateSession();
            session.Collection.TryAddToBench(
                ShopCardInstance.CreateSpell(
                    "refresh_spell",
                    CreateSpell("refresh_spell", CreateResourceEffect("FreeRefresh", 2, "Manual"))),
                out var benchIndex);
            session.StartNextRound();

            var result = session.UseSpell(benchIndex);

            Assert.That(result.Success, Is.True);
            Assert.That(session.FreeRefreshes, Is.EqualTo(2));
            Assert.That(session.Collection.Bench[benchIndex], Is.Null);
            session.Refresh();
            session.Refresh();
            Assert.That(session.Gold, Is.EqualTo(3));
        }

        [Test]
        public void ShopIneligibleSpell_NeverAppearsInOffer()
        {
            var spell = CreateSpell("reward_only");
            spell.ShopEligible = false;
            var session = new ShopSession(
                Array.Empty<MinionConfig>(),
                new[] { spell },
                new SequenceRandom());

            session.StartNextRound();

            Assert.That(session.SpellOffer, Is.Null);
        }

        [Test]
        public void BattleSnapshot_PreservesSourceInstanceAndPermanentState()
        {
            var minion = CreateMinion("golden");
            var card = ShopCardInstance.CreateMinion(
                "owned_001",
                minion,
                true,
                3,
                4,
                new[] { "Cleave" });
            var session = CreateSession();
            session.Collection.TryAddToBench(card, out var benchIndex);
            session.StartNextRound();
            session.PlayMinion(benchIndex, 2);

            var snapshot = session.CreateBattleSnapshot();
            var runtime = snapshot.Player[2];
            var clone = runtime.Clone();

            Assert.That(runtime.SourceInstanceId, Is.EqualTo("owned_001"));
            Assert.That(runtime.IsGolden, Is.True);
            Assert.That(runtime.CurrentAttack, Is.EqualTo(5));
            Assert.That(runtime.CurrentHealth, Is.EqualTo(6));
            Assert.That(runtime.PermanentAttackBonus, Is.EqualTo(3));
            Assert.That(runtime.Keywords.Contains("Cleave"), Is.True);
            Assert.That(clone.SourceInstanceId, Is.EqualTo("owned_001"));
        }

        [Test]
        public void EndShop_LocksFormationAndSpellUse()
        {
            var session = CreateSession();
            session.Collection.TryAddToBench(
                ShopCardInstance.CreateMinion("minion", CreateMinion("minion")),
                out var minionIndex);
            session.Collection.TryAddToBench(
                ShopCardInstance.CreateSpell("spell", CreateSpell("spell")),
                out var spellIndex);
            session.StartNextRound();
            session.EndRound();

            Assert.That(session.PlayMinion(minionIndex, 0).Error,
                Is.EqualTo(ShopOperationError.ShopClosed));
            Assert.That(session.UseSpell(spellIndex).Error,
                Is.EqualTo(ShopOperationError.ShopClosed));
            Assert.That(session.RepositionBattleMinion(0, 1).Error,
                Is.EqualTo(ShopOperationError.ShopClosed));
        }

        [Test]
        public void EndShopEvent_IsRaisedAfterShopIsLocked()
        {
            var session = CreateSession();
            var wasLocked = false;
            var operationError = ShopOperationError.None;
            session.EventRaised += data =>
            {
                if (data.Type != ShopEventType.OnShopPhaseEnd)
                {
                    return;
                }

                wasLocked = !session.IsShopOpen;
                operationError = session.Refresh().Error;
            };
            session.StartNextRound();

            session.EndRound();

            Assert.That(wasLocked, Is.True);
            Assert.That(operationError, Is.EqualTo(ShopOperationError.ShopClosed));
        }

        private static ShopSession CreateSession()
        {
            return new ShopSession(
                Array.Empty<MinionConfig>(),
                Array.Empty<SpellConfig>(),
                new SequenceRandom());
        }

        private static MinionConfig CreateMinion(string id)
        {
            return new MinionConfig
            {
                Id = id,
                Name = id,
                Tier = 1,
                Race = "Wayfarer",
                Attack = 1,
                Health = 1,
                GoldenAttack = 2,
                GoldenHealth = 2,
                Enabled = true
            };
        }

        private static SpellConfig CreateSpell(string id, EffectConfig effect = null)
        {
            var spell = new SpellConfig
            {
                Id = id,
                Name = id,
                Tier = 1,
                SpellType = "Growth",
                UseTiming = new List<string> { "Shop" },
                Cost = 1,
                ShopEligible = true,
                Enabled = true
            };
            if (effect != null)
            {
                spell.Effects.Add(effect);
            }

            return spell;
        }

        private static EffectConfig CreateStatEffect(
            string trigger,
            int attack,
            int health)
        {
            return new EffectConfig
            {
                Id = "stats",
                Trigger = trigger,
                Action = "ModifyStats",
                Target = new TargetConfig
                {
                    Side = "Ally",
                    Scope = "Single",
                    Zones = new List<string> { "Battle" },
                    IncludeSelf = false,
                    IncludeToken = false,
                    MaxTargets = 1,
                    Selector = "PlayerChoice"
                },
                Value = new ValueConfig
                {
                    Attack = attack,
                    Health = health,
                    Duration = "Permanent"
                }
            };
        }

        private static EffectConfig CreateResourceEffect(
            string action,
            int amount,
            string trigger = "OnPlay")
        {
            return new EffectConfig
            {
                Id = action,
                Trigger = trigger,
                Action = action,
                Value = new ValueConfig { Amount = amount }
            };
        }

        private sealed class SequenceRandom : Random
        {
            public override int Next(int maxValue)
            {
                return 0;
            }
        }
    }
}

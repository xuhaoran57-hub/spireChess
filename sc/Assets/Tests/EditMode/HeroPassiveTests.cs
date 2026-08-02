using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Run;
using SpireChess.Save;
using SpireChess.Shop;
using SpireChess.Utils;

namespace SpireChess.Tests.EditMode
{
    public sealed class HeroPassiveTests
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
        public void NewRun_AppliesRunStartExactlyOnceForEachHero()
        {
            var warrior = new RunSession(configs, 40101, HeroIds.Warrior);
            var mage = new RunSession(configs, 40102, HeroIds.Mage);
            var rogue = new RunSession(configs, 40103, HeroIds.Rogue);

            Assert.That(
                warrior.State.Armor,
                Is.EqualTo(HeroPassiveRules.WarriorStartingArmor));
            Assert.That(mage.State.Armor, Is.Zero);
            Assert.That(rogue.State.Armor, Is.Zero);
            Assert.That(warrior.State.HeroRuntime.RunStartApplied, Is.True);
            Assert.That(mage.State.HeroRuntime.RunStartApplied, Is.True);
            Assert.That(rogue.State.HeroRuntime.RunStartApplied, Is.True);
        }

        [Test]
        public void WarriorArmor_AbsorbsBattleDamageBeforeHealthAndPersists()
        {
            var run = new RunSession(configs, 40201, HeroIds.Warrior);
            Assert.That(run.EnterNode("f1_shop_start").Success, Is.True);
            Assert.That(
                run.EndShopAndPrepareBattle("RunTest").Success,
                Is.True);
            Assert.That(run.EnterNode("f1_opening_normal").Success, Is.True);

            configs.EncountersById[
                "f1_opening_encounter"].DamageBonus = 2;
            var highTierEnemy = configs.MinionsById[
                "undying_furnace_king"];
            var finalState = new BattleBoardState();
            for (var index = 0; index < BattleBoardState.SlotCount; index++)
            {
                finalState.Enemy[index] = new BattleMinionRuntime(
                    highTierEnemy);
            }

            var result = new BattleSimulationResult(
                finalState,
                BattleSide.Enemy,
                BattleOutcomeReason.Victory,
                new List<string>(),
                new List<BattleStep>());
            Assert.That(run.TryCompleteBattle(result, out _), Is.True);

            var settlement = run.State.LastSettlement;
            Assert.That(settlement.Damage, Is.GreaterThan(10));
            Assert.That(settlement.ArmorAbsorbed, Is.EqualTo(10));
            Assert.That(
                settlement.HealthDamage,
                Is.EqualTo(settlement.Damage - 10));
            Assert.That(run.State.Armor, Is.Zero);
            Assert.That(
                run.State.Health,
                Is.EqualTo(20 - settlement.HealthDamage));
            Assert.That(
                settlement.BuildDamageText(),
                Does.Contain("护甲吸收 10"));
            Assert.That(
                settlement.BuildDamageText(),
                Does.Contain($"生命损失 {settlement.HealthDamage}"));

            var mapper = new RunSnapshotMapper(configs);
            var payload = mapper.Capture(run);
            var validation = new RunSnapshotValidator(configs)
                .ValidateDto(payload);
            Assert.That(
                validation.IsValid,
                Is.True,
                string.Join("\n", validation.Errors));
            var restored = mapper.Restore(payload);
            Assert.That(restored.State.Armor, Is.Zero);
            Assert.That(
                restored.State.Health,
                Is.EqualTo(run.State.Health));
            Assert.That(
                restored.State.LastSettlement.ArmorAbsorbed,
                Is.EqualTo(10));
            Assert.That(
                restored.State.LastSettlement.HealthDamage,
                Is.EqualTo(settlement.HealthDamage));
            Assert.That(
                restored.State.HeroRuntime.RunStartApplied,
                Is.True);
            Assert.That(restored.TryCompleteBattle(result, out _), Is.False);
            Assert.That(restored.State.Armor, Is.Zero);
        }

        [Test]
        public void BattleDamageRule_HandlesPartialAndOverflowAbsorption()
        {
            var fullyAbsorbed = HeroPassiveRules.ResolveBattleDamage(
                20,
                10,
                3);
            Assert.That(fullyAbsorbed.ArmorAbsorbed, Is.EqualTo(3));
            Assert.That(fullyAbsorbed.HealthDamage, Is.Zero);
            Assert.That(fullyAbsorbed.RemainingArmor, Is.EqualTo(7));
            Assert.That(fullyAbsorbed.RemainingHealth, Is.EqualTo(20));

            var overflow = HeroPassiveRules.ResolveBattleDamage(
                20,
                4,
                9);
            Assert.That(overflow.ArmorAbsorbed, Is.EqualTo(4));
            Assert.That(overflow.HealthDamage, Is.EqualTo(5));
            Assert.That(overflow.RemainingArmor, Is.Zero);
            Assert.That(overflow.RemainingHealth, Is.EqualTo(15));
        }

        [Test]
        public void MagePassive_NewShopGrantsOneTemporarySpellAndRestoresExactlyOnce()
        {
            var run = new RunSession(configs, 40301, HeroIds.Mage);
            var mapper = new RunSnapshotMapper(configs);

            Assert.That(run.EnterNode("f1_shop_start").Success, Is.True);

            var temporarySpells = run.Shop.Collection.Bench
                .Where(card => card != null && card.ExpiresAtShopEnd)
                .ToArray();
            Assert.That(temporarySpells, Has.Length.EqualTo(1));
            Assert.That(temporarySpells[0].CardType, Is.EqualTo(ShopCardType.Spell));
            Assert.That(temporarySpells[0].Spell.Enabled, Is.True);
            Assert.That(
                temporarySpells[0].Spell.ImplementationStatus,
                Is.EqualTo("Playable"));
            Assert.That(temporarySpells[0].Spell.ShopEligible, Is.True);
            Assert.That(
                temporarySpells[0].Spell.Tier,
                Is.LessThanOrEqualTo(run.Shop.TavernTier));
            Assert.That(run.Shop.Gold, Is.EqualTo(ShopEconomyRules.GetRoundBudget(1)));
            Assert.That(run.Shop.SpellOffer, Is.Not.Null);
            Assert.That(run.Shop.PhaseStats.SpellBoughtCount, Is.Zero);
            Assert.That(
                run.State.HeroRuntime.ProcessedShopStartTurns,
                Does.Contain(1));
            Assert.That(
                run.State.HeroRuntime.LastShopStartOutcome,
                Is.EqualTo(HeroPassiveShopStartOutcome.GrantedTemporarySpell));
            Assert.That(
                run.CurrentShopHeroPassiveMessage,
                Does.Contain(temporarySpells[0].Spell.Name));

            var openPayload = mapper.Capture(run);
            Assert.That(openPayload.RandomStreams.Hero.Entries, Has.Count.EqualTo(1));
            Assert.That(openPayload.RandomStreams.Shop.Entries, Is.Not.Empty);
            Assert.That(openPayload.ShopSession.PhaseStats.SpellBoughtCount, Is.Zero);
            Assert.That(
                new RunSnapshotValidator(configs).ValidateDto(openPayload).IsValid,
                Is.True);

            var restored = mapper.Restore(openPayload);
            Assert.That(restored.EnsureShopOpen().Success, Is.True);
            Assert.That(
                restored.Shop.Collection.Bench.Count(card =>
                    card != null && card.ExpiresAtShopEnd),
                Is.EqualTo(1));
            Assert.That(
                mapper.Capture(restored).RandomStreams.Hero.Entries,
                Has.Count.EqualTo(1));
            Assert.That(
                restored.State.HeroRuntime.ProcessedShopStartTurns,
                Is.EquivalentTo(new[] { 1 }));

            Assert.That(restored.EndShopAndPrepareBattle("RunTest").Success, Is.True);
            Assert.That(
                restored.Shop.Collection.Bench.Any(card =>
                    card != null && card.ExpiresAtShopEnd),
                Is.False);
            Assert.That(
                restored.State.HeroRuntime.ProcessedShopEndTurns,
                Is.EquivalentTo(new[] { 1 }));
            Assert.That(restored.CurrentShopHeroPassiveMessage, Is.Empty);
        }

        [Test]
        public void MagePassive_FullBenchSkipsGrantWithoutConsumingHeroRandom()
        {
            var run = new RunSession(configs, 40302, HeroIds.Mage);
            var mapper = new RunSnapshotMapper(configs);
            var fillerSpell = configs.Spells.First(spell =>
                spell.Enabled &&
                spell.ImplementationStatus == "Playable" &&
                spell.Effects.Count > 0);
            for (var index = 0; index < ShopEconomyRules.BenchSlotCount; index++)
            {
                Assert.That(
                    run.Shop.Collection.TryAddToBench(
                        ShopCardInstance.CreateSpell(
                            $"mage_full_bench_{index}",
                            fillerSpell),
                        out _),
                    Is.True);
            }

            var before = mapper.Capture(run);
            Assert.That(before.RandomStreams.Hero.Entries, Is.Empty);
            Assert.That(run.EnterNode("f1_shop_start").Success, Is.True);

            var after = mapper.Capture(run);
            Assert.That(after.RandomStreams.Hero.Entries, Is.Empty);
            Assert.That(
                run.Shop.Collection.Bench.Count(card => card != null),
                Is.EqualTo(ShopEconomyRules.BenchSlotCount));
            Assert.That(
                run.Shop.Collection.Bench.Any(card =>
                    card != null && card.ExpiresAtShopEnd),
                Is.False);
            Assert.That(
                run.State.HeroRuntime.LastShopStartOutcome,
                Is.EqualTo(HeroPassiveShopStartOutcome.BenchFull));
            Assert.That(
                run.CurrentShopHeroPassiveMessage,
                Does.Contain("备战区已满"));

            Assert.That(run.EnsureShopOpen().Success, Is.True);
            Assert.That(
                mapper.Capture(run).RandomStreams.Hero.Entries,
                Is.Empty);
            Assert.That(
                run.State.HeroRuntime.ProcessedShopStartTurns,
                Is.EquivalentTo(new[] { 1 }));
        }

        [Test]
        public void MageCandidateFilter_UsesOnlyPlayableEligibleSpellsAtCurrentTier()
        {
            var candidates = HeroPassiveRules.GetMageShopStartCandidates(
                new[]
                {
                    CreateCandidateSpell("eligible", 2, true, "Playable", true),
                    CreateCandidateSpell("disabled", 1, false, "Playable", true),
                    CreateCandidateSpell("prototype", 1, true, "Prototype", true),
                    CreateCandidateSpell("system", 1, true, "Playable", false),
                    CreateCandidateSpell("too_high", 3, true, "Playable", true)
                },
                2);

            Assert.That(
                candidates.Select(spell => spell.Id),
                Is.EquivalentTo(new[] { "eligible" }));
            Assert.That(
                HeroPassiveRules.GetMageShopStartCandidates(
                    new[]
                    {
                        CreateCandidateSpell(
                            "triple_discovery_reward",
                            1,
                            true,
                            "Playable",
                            false)
                    },
                    6),
                Is.Empty);
        }

        [Test]
        public void MagePassive_UsesIndependentRandomStreamWithoutChangingShopOffers()
        {
            const int seed = 40303;
            var warrior = new RunSession(configs, seed, HeroIds.Warrior);
            var mage = new RunSession(configs, seed, HeroIds.Mage);

            Assert.That(warrior.EnterNode("f1_shop_start").Success, Is.True);
            Assert.That(mage.EnterNode("f1_shop_start").Success, Is.True);

            Assert.That(
                mage.Shop.MinionOffers.Select(card => card?.Id),
                Is.EqualTo(warrior.Shop.MinionOffers.Select(card => card?.Id)));
            Assert.That(mage.Shop.SpellOffer?.Id, Is.EqualTo(warrior.Shop.SpellOffer?.Id));

            var mapper = new RunSnapshotMapper(configs);
            var warriorRandom = mapper.Capture(warrior).RandomStreams;
            var mageRandom = mapper.Capture(mage).RandomStreams;
            Assert.That(
                mageRandom.Shop.Entries.Select(entry => entry.IntResult),
                Is.EqualTo(warriorRandom.Shop.Entries.Select(entry => entry.IntResult)));
            Assert.That(warriorRandom.Hero.Entries, Is.Empty);
            Assert.That(mageRandom.Hero.Entries, Has.Count.EqualTo(1));
        }

        [Test]
        public void RoguePassive_StealsOneVisibleMinionWithoutBuyingAndRestoresExactlyOnce()
        {
            var run = new RunSession(configs, 40401, HeroIds.Rogue);
            var mapper = new RunSnapshotMapper(configs);
            var initialPoolCopies = TotalPoolCopies(mapper, run);
            var events = new List<ShopEventData>();
            run.Shop.EventRaised += events.Add;

            Assert.That(run.EnterNode("f1_shop_start").Success, Is.True);
            Assert.That(run.Shop.MinionOffers.Count(card => card != null), Is.GreaterThan(0));
            Assert.That(mapper.Capture(run).RandomStreams.Hero.Entries, Is.Empty);

            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);

            Assert.That(
                run.State.HeroRuntime.LastShopEndOutcome,
                Is.EqualTo(HeroPassiveShopEndOutcome.StoleMinion));
            Assert.That(
                run.State.HeroRuntime.ProcessedShopEndTurns,
                Is.EquivalentTo(new[] { 1 }));
            Assert.That(
                run.Shop.Collection.Bench.Count(card => card != null),
                Is.EqualTo(1));
            var stolen = run.Shop.Collection.Bench.Single(card => card != null);
            Assert.That(stolen.CardType, Is.EqualTo(ShopCardType.Minion));
            Assert.That(stolen.IsGolden, Is.False);
            Assert.That(stolen.Minion.IsToken, Is.False);
            Assert.That(
                stolen.ConfigId,
                Is.EqualTo(run.State.HeroRuntime.LastStolenMinionId));
            Assert.That(run.Shop.PhaseStats.MinionBoughtCount, Is.Zero);
            Assert.That(run.State.Statistics.MinionsBought, Is.Zero);
            Assert.That(
                events.Any(data => data.Type == ShopEventType.OnBuy),
                Is.False);
            Assert.That(run.Shop.MinionOffers.All(card => card == null), Is.True);
            Assert.That(TotalPoolCopies(mapper, run), Is.EqualTo(initialPoolCopies - 1));
            Assert.That(
                run.CurrentShopEndHeroPassiveMessage,
                Does.Contain(stolen.Minion.Name));

            var closedPayload = mapper.Capture(run);
            Assert.That(closedPayload.RandomStreams.Hero.Entries, Has.Count.EqualTo(1));
            var validation = new RunSnapshotValidator(configs).ValidateDto(closedPayload);
            Assert.That(
                validation.IsValid,
                Is.True,
                string.Join("\n", validation.Errors));

            var restored = mapper.Restore(closedPayload);
            Assert.That(restored.EndShopAndPrepareBattle("RunTest").Success, Is.False);
            Assert.That(
                mapper.Capture(restored).RandomStreams.Hero.Entries,
                Has.Count.EqualTo(1));
            Assert.That(
                restored.Shop.Collection.Bench.Count(card =>
                    card != null && card.ConfigId == stolen.ConfigId),
                Is.EqualTo(1));
        }

        [Test]
        public void RoguePassive_FullBenchSkipsStealWithoutConsumingHeroRandom()
        {
            var run = new RunSession(configs, 40402, HeroIds.Rogue);
            var mapper = new RunSnapshotMapper(configs);
            var initialPoolCopies = TotalPoolCopies(mapper, run);
            var filler = configs.Spells.First(spell =>
                spell.Enabled &&
                spell.ImplementationStatus == "Playable" &&
                spell.Effects.Count > 0);
            for (var index = 0; index < ShopEconomyRules.BenchSlotCount; index++)
            {
                Assert.That(
                    run.Shop.Collection.TryAddToBench(
                        ShopCardInstance.CreateSpell($"rogue_full_{index}", filler),
                        out _),
                    Is.True);
            }

            Assert.That(run.EnterNode("f1_shop_start").Success, Is.True);
            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);

            Assert.That(
                mapper.Capture(run).RandomStreams.Hero.Entries,
                Is.Empty);
            Assert.That(
                run.State.HeroRuntime.LastShopEndOutcome,
                Is.EqualTo(HeroPassiveShopEndOutcome.BenchFull));
            Assert.That(
                run.Shop.Collection.Bench.Count(card => card != null),
                Is.EqualTo(ShopEconomyRules.BenchSlotCount));
            Assert.That(TotalPoolCopies(mapper, run), Is.EqualTo(initialPoolCopies));
            Assert.That(
                run.CurrentShopEndHeroPassiveMessage,
                Does.Contain("备战区已满"));
        }

        [Test]
        public void RoguePassive_NoVisibleMinionSkipsRandomAndIgnoresSpellOffer()
        {
            var run = new RunSession(configs, 40403, HeroIds.Rogue);
            var mapper = new RunSnapshotMapper(configs);
            Assert.That(run.EnterNode("f1_shop_start").Success, Is.True);
            Assert.That(run.Shop.SpellOffer, Is.Not.Null);
            run.Shop.GrantGold(30);
            for (var index = 0; index < run.Shop.MinionOffers.Count; index++)
            {
                Assert.That(run.Shop.BuyMinion(index).Success, Is.True);
            }
            Assert.That(run.Shop.MinionOffers.All(card => card == null), Is.True);
            Assert.That(
                run.Shop.Collection.Bench.Count(card => card == null),
                Is.GreaterThan(0));

            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);

            Assert.That(
                mapper.Capture(run).RandomStreams.Hero.Entries,
                Is.Empty);
            Assert.That(
                run.State.HeroRuntime.LastShopEndOutcome,
                Is.EqualTo(HeroPassiveShopEndOutcome.NoVisibleMinion));
            Assert.That(
                run.CurrentShopEndHeroPassiveMessage,
                Does.Contain("没有可偷取的随从"));
        }

        [Test]
        public void RoguePassive_FrozenShopKeepsEveryOtherOfferReserved()
        {
            var run = new RunSession(configs, 40404, HeroIds.Rogue);
            var mapper = new RunSnapshotMapper(configs);
            var initialPoolCopies = TotalPoolCopies(mapper, run);
            Assert.That(run.EnterNode("f1_shop_start").Success, Is.True);
            var visibleBefore = run.Shop.MinionOffers
                .Where(card => card != null)
                .Select(card => card.Id)
                .ToList();
            Assert.That(run.Shop.ToggleFreeze().Success, Is.True);

            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);

            Assert.That(run.Shop.IsFrozen, Is.True);
            Assert.That(
                run.Shop.MinionOffers.Count(card => card != null),
                Is.EqualTo(visibleBefore.Count - 1));
            var expectedRemaining = new List<string>(visibleBefore);
            Assert.That(
                expectedRemaining.Remove(
                    run.State.HeroRuntime.LastStolenMinionId),
                Is.True);
            Assert.That(
                run.Shop.MinionOffers
                    .Where(card => card != null)
                    .Select(card => card.Id),
                Is.EquivalentTo(expectedRemaining));
            Assert.That(run.Shop.SpellOffer, Is.Not.Null);
            Assert.That(
                TotalPoolCopies(mapper, run),
                Is.EqualTo(initialPoolCopies - visibleBefore.Count));
        }

        [Test]
        public void RoguePassive_StolenThirdCopyFormsGoldenAndKeepsThreePoolCopiesHeld()
        {
            const int seed = 40405;
            var run = new RunSession(configs, seed, HeroIds.Rogue);
            var mapper = new RunSnapshotMapper(configs);
            var initialPoolCopies = TotalPoolCopies(mapper, run);
            Assert.That(run.EnterNode("f1_shop_start").Success, Is.True);
            var candidates = run.Shop.MinionOffers
                .Where(card => card != null && !card.IsToken)
                .ToArray();
            var selectedIndex = new System.Random(
                    SeedDeriver.Combine(seed, 505))
                .Next(candidates.Length);
            var selected = candidates[selectedIndex];
            Assert.That(
                run.Shop.MinionPool.TryReserveCopies(selected.Id, 2),
                Is.True);
            Assert.That(
                run.Shop.Collection.TryAddToBench(
                    ShopCardInstance.CreateMinion("rogue_triple_a", selected),
                    out _),
                Is.True);
            Assert.That(
                run.Shop.Collection.TryAddToBench(
                    ShopCardInstance.CreateMinion("rogue_triple_b", selected),
                    out _),
                Is.True);

            Assert.That(run.EndShopAndPrepareBattle("RunTest").Success, Is.True);

            var golden = run.Shop.Collection.Bench.Single(card => card != null);
            Assert.That(golden.ConfigId, Is.EqualTo(selected.Id));
            Assert.That(golden.IsGolden, Is.True);
            Assert.That(golden.TripleDiscoveryPending, Is.True);
            Assert.That(golden.PoolCopiesHeld, Is.EqualTo(3));
            Assert.That(run.State.Statistics.TriplesFormed, Is.EqualTo(1));
            Assert.That(run.Shop.PhaseStats.MinionBoughtCount, Is.Zero);
            Assert.That(
                TotalPoolCopies(mapper, run),
                Is.EqualTo(initialPoolCopies - 3));
        }

        [Test]
        public void RoguePassive_PendingDiscoverBlocksBeforeMarkerOrRandomConsumption()
        {
            var run = new RunSession(configs, 40406, HeroIds.Rogue);
            var mapper = new RunSnapshotMapper(configs);
            Assert.That(run.EnterNode("f1_shop_start").Success, Is.True);
            Assert.That(
                configs.TryGetSpell(
                    ShopSession.TripleDiscoveryRewardSpellId,
                    out var discoverySpell),
                Is.True);
            Assert.That(
                run.Shop.Collection.TryAddToBench(
                    ShopCardInstance.CreateSpell(
                        "rogue_pending_discover",
                        discoverySpell),
                    out var benchIndex),
                Is.True);
            Assert.That(run.Shop.UseSpell(benchIndex).Success, Is.True);
            Assert.That(run.Shop.PendingDiscover, Is.Not.Null);

            var result = run.EndShopAndPrepareBattle("RunTest");

            Assert.That(result.Error, Is.EqualTo(ShopOperationError.DiscoveryPending));
            Assert.That(run.Shop.IsShopOpen, Is.True);
            Assert.That(
                run.State.HeroRuntime.ProcessedShopEndTurns,
                Is.Empty);
            Assert.That(
                mapper.Capture(run).RandomStreams.Hero.Entries,
                Is.Empty);
        }

        [Test]
        public void RoguePassive_UsesHeroStreamWithoutAdvancingShopRandom()
        {
            const int seed = 40407;
            var warrior = new RunSession(configs, seed, HeroIds.Warrior);
            var rogue = new RunSession(configs, seed, HeroIds.Rogue);
            var mapper = new RunSnapshotMapper(configs);
            Assert.That(warrior.EnterNode("f1_shop_start").Success, Is.True);
            Assert.That(rogue.EnterNode("f1_shop_start").Success, Is.True);

            Assert.That(warrior.EndShopAndPrepareBattle("RunTest").Success, Is.True);
            Assert.That(rogue.EndShopAndPrepareBattle("RunTest").Success, Is.True);

            var warriorStreams = mapper.Capture(warrior).RandomStreams;
            var rogueStreams = mapper.Capture(rogue).RandomStreams;
            Assert.That(
                rogueStreams.Shop.Entries.Select(entry => entry.IntResult),
                Is.EqualTo(warriorStreams.Shop.Entries.Select(entry => entry.IntResult)));
            Assert.That(warriorStreams.Hero.Entries, Is.Empty);
            Assert.That(rogueStreams.Hero.Entries, Has.Count.EqualTo(1));
        }

        private static int TotalPoolCopies(
            RunSnapshotMapper mapper,
            RunSession run)
        {
            return mapper.Capture(run)
                .ShopSession
                .MinionPoolRemainingCopies
                .Values
                .Sum();
        }

        private static SpellConfig CreateCandidateSpell(
            string id,
            int tier,
            bool enabled,
            string implementationStatus,
            bool shopEligible)
        {
            return new SpellConfig
            {
                Id = id,
                Name = id,
                Tier = tier,
                Enabled = enabled,
                ImplementationStatus = implementationStatus,
                ShopEligible = shopEligible
            };
        }
    }
}

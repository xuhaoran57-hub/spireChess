using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SpireChess.Config;
using SpireChess.Shop;
using SpireChess.UI.Shop;

namespace SpireChess.Tests.EditMode
{
    public sealed class ShopTargetingQueryTests
    {
        [Test]
        public void TargetedSpell_ReturnsNonTokenTargetsWithoutMutatingState()
        {
            var session = CreateSession();
            Assert.That(session.StartRound(1).Success, Is.True);
            var normal = AddMinionToBattle(
                session,
                CreateMinion("normal"),
                0);
            AddMinionToBattle(
                session,
                CreateMinion("token", isToken: true),
                1);
            var spell = ShopCardInstance.CreateSpell(
                "targeted_spell",
                CreateTargetedSpell("targeted_spell"));
            Assert.That(session.Collection.TryAddToBench(spell, out var handIndex),
                Is.True);
            var attackBefore = normal.CurrentAttack;
            var healthBefore = normal.CurrentHealth;

            var result = ShopTargetingQuery.ForHandCard(session, handIndex);
            var state = ShopScreenStateBuilder.Build(
                session,
                selectedHandIndex: handIndex);

            Assert.Multiple(() =>
            {
                Assert.That(result.RequiresBattleTarget, Is.True);
                Assert.That(result.LegalBattleTargetIndexes, Is.EqualTo(new[] { 0 }));
                Assert.That(result.IsLegalBattleTarget(0), Is.True);
                Assert.That(result.IsLegalBattleTarget(1), Is.False);
                Assert.That(result.DisabledReason, Is.Null);
                Assert.That(normal.CurrentAttack, Is.EqualTo(attackBefore));
                Assert.That(normal.CurrentHealth, Is.EqualTo(healthBefore));
                Assert.That(session.Collection.Bench[handIndex], Is.SameAs(spell));
                Assert.That(session.PendingDiscover, Is.Null);
                Assert.That(session.PendingChoice, Is.Null);
                Assert.That(state.BattleCards[0].IsLegalTarget, Is.True);
                Assert.That(state.BattleCards[0].IsInteractable, Is.True);
                Assert.That(state.BattleCards[1].IsLegalTarget, Is.False);
                Assert.That(state.BattleCards[1].IsInteractable, Is.False);
                Assert.That(state.BattleCards[1].DisabledReason,
                    Is.EqualTo("该随从不是合法目标"));
            });
        }

        [Test]
        public void TargetedSpell_RespectsRaceAndGoldenTargetConditions()
        {
            var session = CreateSession();
            Assert.That(session.StartRound(1).Success, Is.True);
            AddMinionToBattle(
                session,
                CreateMinion("star_normal", race: "Starbound"),
                0);
            AddMinionToBattle(
                session,
                CreateMinion("star_golden", race: "Starbound"),
                1,
                isGolden: true);
            AddMinionToBattle(
                session,
                CreateMinion("wild_golden", race: "WildSpirit"),
                2,
                isGolden: true);
            var spell = CreateTargetedSpell(
                "golden_star_only",
                race: "Starbound",
                condition: new ConditionConfig { Type = "IsGolden" });
            Assert.That(session.Collection.TryAddToBench(
                ShopCardInstance.CreateSpell("golden_star_only", spell),
                out var handIndex), Is.True);

            var result = ShopTargetingQuery.ForHandCard(session, handIndex);

            Assert.That(result.LegalBattleTargetIndexes, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void Battlecry_ReturnsExistingFriendlyTargetsBeforePlacement()
        {
            var session = CreateSession();
            Assert.That(session.StartRound(1).Success, Is.True);
            AddMinionToBattle(session, CreateMinion("ally"), 0);
            AddMinionToBattle(
                session,
                CreateMinion("token", isToken: true),
                1);
            var battlecry = CreateMinion("battlecry");
            battlecry.Effects.Add(CreateTargetedEffect("battlecry_target", "OnPlay"));
            Assert.That(session.Collection.TryAddToBench(
                ShopCardInstance.CreateMinion("battlecry", battlecry),
                out var handIndex), Is.True);

            var result = ShopTargetingQuery.ForHandCard(session, handIndex);

            Assert.Multiple(() =>
            {
                Assert.That(result.RequiresBattleTarget, Is.True);
                Assert.That(result.LegalBattleTargetIndexes, Is.EqualTo(new[] { 0 }));
            });
        }

        [Test]
        public void BattlecryWithoutLegalTarget_DoesNotBlockMinionPlacement()
        {
            var session = CreateSession();
            Assert.That(session.StartRound(1).Success, Is.True);
            var battlecry = CreateMinion("battlecry_without_target");
            battlecry.Effects.Add(CreateTargetedEffect(
                "battlecry_without_target_effect",
                "OnPlay"));
            Assert.That(session.Collection.TryAddToBench(
                ShopCardInstance.CreateMinion(
                    "battlecry_without_target",
                    battlecry),
                out var handIndex), Is.True);

            var result = ShopTargetingQuery.ForHandCard(session, handIndex);
            var state = ShopScreenStateBuilder.Build(
                session,
                selectedHandIndex: handIndex);

            Assert.Multiple(() =>
            {
                Assert.That(result.RequiresBattleTarget, Is.False);
                Assert.That(result.LegalBattleTargetIndexes, Is.Empty);
                Assert.That(result.DisabledReason, Is.Null);
                Assert.That(state.HandCards.VisibleSlots[handIndex].Card.IsInteractable,
                    Is.True);
                Assert.That(session.PlayMinion(handIndex, 0).Success, Is.True);
            });
        }

        [Test]
        public void NoLegalTarget_DisablesSelectedHandCardInBuilder()
        {
            var session = CreateSession();
            Assert.That(session.StartRound(1).Success, Is.True);
            Assert.That(session.Collection.TryAddToBench(
                ShopCardInstance.CreateSpell(
                    "targeted_spell",
                    CreateTargetedSpell("targeted_spell")),
                out var handIndex), Is.True);

            var query = ShopTargetingQuery.ForHandCard(session, handIndex);
            var state = ShopScreenStateBuilder.Build(
                session,
                selectedHandIndex: handIndex);

            Assert.Multiple(() =>
            {
                Assert.That(query.RequiresBattleTarget, Is.True);
                Assert.That(query.LegalBattleTargetIndexes, Is.Empty);
                Assert.That(query.DisabledReason, Is.EqualTo("没有合法目标"));
                Assert.That(state.HandCards.VisibleSlots[handIndex].Card.IsInteractable,
                    Is.False);
                Assert.That(state.HandCards.VisibleSlots[handIndex].Card.DisabledReason,
                    Is.EqualTo("没有合法目标"));
                Assert.That(state.DetailPanel.Card.DisabledReason,
                    Is.EqualTo("没有合法目标"));
                Assert.That(state.BattleCards.Where(card => card != null)
                    .All(card => !card.IsLegalTarget), Is.True);
            });
        }

        [Test]
        public void NonTargetedAndModalChoiceSpells_DoNotExposeDirectTargets()
        {
            var session = CreateSession();
            Assert.That(session.StartRound(1).Success, Is.True);
            var resource = CreateResourceSpell("resource_spell");
            var copy = CreateCopySpell("copy_spell");
            Assert.That(session.Collection.TryAddToBench(
                ShopCardInstance.CreateSpell("resource_spell", resource),
                out var resourceIndex), Is.True);
            Assert.That(session.Collection.TryAddToBench(
                ShopCardInstance.CreateSpell("copy_spell", copy),
                out var copyIndex), Is.True);

            var resourceResult = ShopTargetingQuery.ForHandCard(
                session,
                resourceIndex);
            var copyResult = ShopTargetingQuery.ForHandCard(
                session,
                copyIndex);

            Assert.Multiple(() =>
            {
                Assert.That(resourceResult.RequiresBattleTarget, Is.False);
                Assert.That(resourceResult.LegalBattleTargetIndexes, Is.Empty);
                Assert.That(copyResult.RequiresBattleTarget, Is.False);
                Assert.That(copyResult.LegalBattleTargetIndexes, Is.Empty);
            });
        }

        [Test]
        public void Builder_MapsLegalTargetsAndClearsThemDuringModalBlock()
        {
            var minions = new[]
            {
                CreateMinion("candidate_a"),
                CreateMinion("candidate_b"),
                CreateMinion("candidate_c"),
                CreateMinion("candidate_d")
            };
            var rewardSpell = CreateTripleRewardSpell();
            var session = CreateSession(minions, new[] { rewardSpell });
            Assert.That(session.StartRound(1).Success, Is.True);
            AddMinionToBattle(session, CreateMinion("target"), 0);
            Assert.That(session.Collection.TryAddToBench(
                ShopCardInstance.CreateSpell(
                    "targeted_spell",
                    CreateTargetedSpell("targeted_spell")),
                out var targetedIndex), Is.True);
            Assert.That(session.Collection.TryAddToBench(
                ShopCardInstance.CreateSpell("reward_spell", rewardSpell),
                out var rewardIndex), Is.True);

            var available = ShopScreenStateBuilder.Build(
                session,
                selectedHandIndex: targetedIndex);
            Assert.That(session.UseSpell(rewardIndex).Success, Is.True);
            var blocked = ShopScreenStateBuilder.Build(
                session,
                selectedHandIndex: targetedIndex);

            Assert.Multiple(() =>
            {
                Assert.That(available.BattleCards[0].IsLegalTarget, Is.True);
                Assert.That(blocked.IsInteractionBlocked, Is.True);
                Assert.That(blocked.BattleCards[0].IsLegalTarget, Is.False);
                Assert.That(blocked.HandCards.VisibleSlots[targetedIndex]
                    .Card.DisabledReason, Is.EqualTo(blocked.BlockReason));
            });
        }

        [Test]
        public void Query_DoesNotAdvanceShopRandomSequence()
        {
            var minions = new[]
            {
                CreateMinion("pool_a"),
                CreateMinion("pool_b"),
                CreateMinion("pool_c"),
                CreateMinion("pool_d")
            };
            var queried = CreateSession(minions, seed: 91);
            var control = CreateSession(minions, seed: 91);
            Assert.That(queried.StartRound(1).Success, Is.True);
            Assert.That(control.StartRound(1).Success, Is.True);
            AddMinionToBattle(queried, CreateMinion("target"), 0);
            AddMinionToBattle(control, CreateMinion("target"), 0);
            Assert.That(queried.Collection.TryAddToBench(
                ShopCardInstance.CreateSpell(
                    "targeted_spell",
                    CreateTargetedSpell("targeted_spell")),
                out var queriedIndex), Is.True);
            Assert.That(control.Collection.TryAddToBench(
                ShopCardInstance.CreateSpell(
                    "targeted_spell",
                    CreateTargetedSpell("targeted_spell")),
                out _), Is.True);

            ShopTargetingQuery.ForHandCard(queried, queriedIndex);
            Assert.That(queried.Refresh().Success, Is.True);
            Assert.That(control.Refresh().Success, Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(queried.MinionOffers.Select(card => card?.Id),
                    Is.EqualTo(control.MinionOffers.Select(card => card?.Id)));
                Assert.That(queried.SpellOffer?.Id, Is.EqualTo(control.SpellOffer?.Id));
            });
        }

        private static ShopSession CreateSession(
            IEnumerable<MinionConfig> minions = null,
            IEnumerable<SpellConfig> spells = null,
            int seed = 23)
        {
            return new ShopSession(
                minions ?? new[]
                {
                    CreateMinion("shop_a"),
                    CreateMinion("shop_b")
                },
                spells ?? new[] { CreateResourceSpell("shop_spell") },
                new Random(seed));
        }

        private static ShopCardInstance AddMinionToBattle(
            ShopSession session,
            MinionConfig minion,
            int battleIndex,
            bool isGolden = false)
        {
            var card = ShopCardInstance.CreateMinion(
                minion.Id + "_instance",
                minion,
                isGolden);
            Assert.That(session.Collection.TryAddToBench(card, out var handIndex),
                Is.True);
            Assert.That(session.PlayMinion(handIndex, battleIndex).Success, Is.True);
            return card;
        }

        private static MinionConfig CreateMinion(
            string id,
            string race = "Starbound",
            bool isToken = false)
        {
            return new MinionConfig
            {
                Id = id,
                Name = id,
                Description = id + " description",
                GoldenDescription = id + " golden description",
                Tier = isToken ? 0 : 1,
                Race = race,
                IsToken = isToken,
                Attack = 2,
                Health = 3,
                GoldenAttack = 4,
                GoldenHealth = 6,
                Enabled = true
            };
        }

        private static SpellConfig CreateTargetedSpell(
            string id,
            string race = null,
            ConditionConfig condition = null)
        {
            var spell = CreateSpell(id);
            var effect = CreateTargetedEffect(id + "_manual", "Manual");
            effect.Target.Race = race;
            effect.Condition = condition;
            spell.Effects.Add(effect);
            return spell;
        }

        private static EffectConfig CreateTargetedEffect(
            string id,
            string trigger)
        {
            return new EffectConfig
            {
                Id = id,
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
                    Attack = 1,
                    Health = 1,
                    Duration = "Permanent"
                }
            };
        }

        private static SpellConfig CreateResourceSpell(string id)
        {
            var spell = CreateSpell(id);
            spell.Effects.Add(new EffectConfig
            {
                Id = id + "_manual",
                Trigger = "Manual",
                Action = "FreeRefresh",
                Value = new ValueConfig { Amount = 1 }
            });
            return spell;
        }

        private static SpellConfig CreateCopySpell(string id)
        {
            var spell = CreateSpell(id);
            spell.Effects.Add(new EffectConfig
            {
                Id = id + "_manual",
                Trigger = "Manual",
                Action = "CopyMinion",
                Target = new TargetConfig
                {
                    Side = "Ally",
                    Scope = "Single",
                    Zones = new List<string> { "Battle" },
                    Selector = "PlayerChoice"
                }
            });
            return spell;
        }

        private static SpellConfig CreateTripleRewardSpell()
        {
            var spell = CreateSpell(ShopSession.TripleDiscoveryRewardSpellId);
            spell.ShopEligible = false;
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

        private static SpellConfig CreateSpell(string id)
        {
            return new SpellConfig
            {
                Id = id,
                Name = id,
                Description = id + " description",
                Tier = 1,
                SpellType = "Growth",
                UseTiming = new List<string> { "Shop" },
                Cost = 1,
                ShopEligible = true,
                Enabled = true
            };
        }
    }
}

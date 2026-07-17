using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SpireChess.Config;
using SpireChess.Shop;
using SpireChess.UI;
using SpireChess.UI.Shop;
using SpireChess.Utils;

namespace SpireChess.Tests.EditMode
{
    public sealed class CardViewModelFactoryTests
    {
        [Test]
        public void MinionOffer_UsesFullNormalLayoutAndEconomyCost()
        {
            var minion = CreateMinion();
            minion.Keywords.AddRange(new[] { "Taunt", "Shield" });
            minion.Tags.AddRange(new[]
            {
                "refresh_growth", "permanent_growth", "unknown_tag"
            });

            var model = ShopCardViewModelFactory.FromOffer(minion, 2);

            Assert.Multiple(() =>
            {
                Assert.That(model.InstanceId, Is.Null);
                Assert.That(model.Name, Is.EqualTo("天穹契约者"));
                Assert.That(model.Description, Is.EqualTo("普通描述"));
                Assert.That(model.RaceText, Is.EqualTo("星契"));
                Assert.That(model.AbilityLabels,
                    Is.EqualTo(new[] { "刷新成长", "永久成长" }));
                Assert.That(model.Keywords,
                    Is.EqualTo(new[] { "嘲讽", "护盾" }));
                Assert.That(model.Tier, Is.EqualTo(3));
                Assert.That(model.Attack, Is.EqualTo(4));
                Assert.That(model.Health, Is.EqualTo(8));
                Assert.That(model.BaseAttack, Is.EqualTo(4));
                Assert.That(model.BaseHealth, Is.EqualTo(8));
                Assert.That(model.Cost, Is.EqualTo(ShopEconomyRules.MinionPurchaseCost));
                Assert.That(model.DisplayMode, Is.EqualTo(CardDisplayMode.Full));
                Assert.That(model.IsMinion, Is.True);
                Assert.That(model.ShowCost, Is.True);
                Assert.That(model.IsGolden, Is.False);
                Assert.That(model.IsAffordable, Is.False);
                Assert.That(model.IsInteractable, Is.False);
                Assert.That(model.HasShield, Is.True);
                Assert.That(model.HasNextCombatShield, Is.False);
                Assert.That(model.IsTemporary, Is.False);
            });
        }

        [Test]
        public void SpellOffer_UsesFullSpellVariantAndFixedEconomyCost()
        {
            var spell = CreateSpell();
            spell.Cost = 7;
            spell.Tags.AddRange(new[]
            {
                "discover_minion", "economy", "unknown_tag"
            });

            var model = ShopCardViewModelFactory.FromOffer(spell, 0);

            Assert.Multiple(() =>
            {
                Assert.That(model.InstanceId, Is.Null);
                Assert.That(model.Name, Is.EqualTo("高阶发现"));
                Assert.That(model.Description, Is.EqualTo("法术描述"));
                Assert.That(model.RaceText, Is.EqualTo("发现"));
                Assert.That(model.AbilityLabels,
                    Is.EqualTo(new[] { "随从发现", "经济" }));
                Assert.That(model.Keywords, Is.Empty);
                Assert.That(model.Cost, Is.EqualTo(ShopEconomyRules.SpellPurchaseCost));
                Assert.That(model.DisplayMode, Is.EqualTo(CardDisplayMode.Full));
                Assert.That(model.IsMinion, Is.False);
                Assert.That(model.ShowCost, Is.True);
                Assert.That(model.IsGolden, Is.False);
                Assert.That(model.IsAffordable, Is.False);
                Assert.That(model.IsInteractable, Is.False);
                Assert.That(model.Attack, Is.Zero);
                Assert.That(model.Health, Is.Zero);
                Assert.That(model.HasShield, Is.False);
                Assert.That(model.IsTemporary, Is.False);
            });
        }

        [Test]
        public void OwnedGoldenMinion_MapsGrowthAndBothShieldStates()
        {
            var minion = CreateMinion();
            minion.Keywords.Add("Taunt");
            minion.Tags.AddRange(new[] { "refresh_growth", "permanent_growth" });
            var owned = ShopCardInstance.CreateMinion(
                "owned_001",
                minion,
                true,
                permanentAttackBonus: 3,
                permanentHealthBonus: 4,
                permanentKeywords: new[] { "Shield", "Cleave" });
            var ward = CreateNextCombatShieldSpell();
            var session = new ShopSession(
                Array.Empty<MinionConfig>(),
                Array.Empty<SpellConfig>(),
                new Random(7));
            Assert.That(session.StartRound(1).Success, Is.True);
            Assert.That(session.Collection.TryAddToBench(owned, out var minionIndex), Is.True);
            Assert.That(session.PlayMinion(minionIndex, 0).Success, Is.True);
            Assert.That(session.Collection.TryAddToBench(
                ShopCardInstance.CreateSpell("ward_001", ward), out var spellIndex), Is.True);
            Assert.That(session.UseSpell(spellIndex, 0).Success, Is.True);

            var model = ShopCardViewModelFactory.FromOwned(owned, true);

            Assert.Multiple(() =>
            {
                Assert.That(model.InstanceId, Is.EqualTo("owned_001"));
                Assert.That(model.Description, Is.EqualTo("金色描述"));
                Assert.That(model.DisplayMode, Is.EqualTo(CardDisplayMode.Compact));
                Assert.That(model.IsMinion, Is.True);
                Assert.That(model.ShowCost, Is.False);
                Assert.That(model.IsGolden, Is.True);
                Assert.That(model.IsSelected, Is.True);
                Assert.That(model.IsAffordable, Is.True);
                Assert.That(model.IsInteractable, Is.True);
                Assert.That(model.BaseAttack, Is.EqualTo(8));
                Assert.That(model.BaseHealth, Is.EqualTo(16));
                Assert.That(model.Attack, Is.EqualTo(11));
                Assert.That(model.Health, Is.EqualTo(20));
                Assert.That(model.HasShield, Is.True);
                Assert.That(model.HasNextCombatShield, Is.True);
                Assert.That(model.IsTemporary, Is.False);
                Assert.That(model.Keywords,
                    Is.EquivalentTo(new[] { "嘲讽", "护盾", "溅射" }));
            });
        }

        [Test]
        public void OwnedTemporarySpell_UsesCompactSpellVariant()
        {
            var spell = CreateSpell();
            spell.SpellType = "Defense";
            spell.Tags.AddRange(new[] { "next_combat", "shield" });
            var owned = ShopCardInstance.CreateSpell(
                "spell_001", spell, expiresAtShopEnd: true);

            var model = ShopCardViewModelFactory.FromOwned(owned, false);

            Assert.Multiple(() =>
            {
                Assert.That(model.InstanceId, Is.EqualTo("spell_001"));
                Assert.That(model.RaceText, Is.EqualTo("防御"));
                Assert.That(model.AbilityLabels,
                    Is.EqualTo(new[] { "下场战斗", "护盾" }));
                Assert.That(model.DisplayMode, Is.EqualTo(CardDisplayMode.Compact));
                Assert.That(model.IsMinion, Is.False);
                Assert.That(model.ShowCost, Is.False);
                Assert.That(model.IsGolden, Is.False);
                Assert.That(model.IsSelected, Is.False);
                Assert.That(model.IsInteractable, Is.True);
                Assert.That(model.IsTemporary, Is.True);
                Assert.That(model.Attack, Is.Zero);
                Assert.That(model.Health, Is.Zero);
                Assert.That(model.Keywords, Is.Empty);
            });
        }

        [Test]
        public void ReleasedContent_AllTagsAndKeywordsHavePlayerFacingLabels()
        {
            var configs = new ConfigService(new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));

            foreach (var minion in configs.Minions)
            {
                var model = ShopCardViewModelFactory.FromOffer(minion, 10);
                Assert.That(model.AbilityLabels.Length,
                    Is.EqualTo(minion.Tags.Distinct().Count()), minion.Id);
                Assert.That(model.Keywords.Length,
                    Is.EqualTo(minion.Keywords.Distinct().Count()), minion.Id);
            }

            foreach (var spell in configs.Spells)
            {
                var model = ShopCardViewModelFactory.FromOffer(spell, 10);
                Assert.That(model.AbilityLabels.Length,
                    Is.EqualTo(spell.Tags.Distinct().Count()), spell.Id);
            }
        }

        [Test]
        public void Factory_RejectsNullInputs()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ShopCardViewModelFactory.FromOffer((MinionConfig)null, 0));
            Assert.Throws<ArgumentNullException>(() =>
                ShopCardViewModelFactory.FromOffer((SpellConfig)null, 0));
            Assert.Throws<ArgumentNullException>(() =>
                ShopCardViewModelFactory.FromOwned(null, false));
        }

        private static MinionConfig CreateMinion()
        {
            return new MinionConfig
            {
                Id = "sky_covenant_bearer",
                Name = "天穹契约者",
                Description = "普通描述",
                GoldenDescription = "金色描述",
                Tier = 3,
                Race = "Starbound",
                Attack = 4,
                Health = 8,
                GoldenAttack = 8,
                GoldenHealth = 16,
                Enabled = true,
                Effects = new List<EffectConfig> { new EffectConfig() },
                GoldenEffects = new List<EffectConfig> { new EffectConfig() }
            };
        }

        private static SpellConfig CreateSpell()
        {
            return new SpellConfig
            {
                Id = "advanced_discovery",
                Name = "高阶发现",
                Description = "法术描述",
                Tier = 3,
                SpellType = "Discover",
                Cost = 1,
                Enabled = true
            };
        }

        private static SpellConfig CreateNextCombatShieldSpell()
        {
            var spell = new SpellConfig
            {
                Id = "temporary_ward",
                Name = "临时护符",
                Description = "使一个随从下一场战斗获得护盾。",
                Tier = 1,
                SpellType = "Defense",
                Cost = 1,
                Enabled = true
            };
            spell.Effects.Add(new EffectConfig
            {
                Id = "temporary_ward_manual",
                Trigger = "Manual",
                Action = "AddShield",
                Target = new TargetConfig
                {
                    Side = "Ally",
                    Scope = "Single",
                    Selector = "PlayerChoice",
                    Zones = new List<string> { "Battle" }
                },
                Value = new ValueConfig
                {
                    Duration = "NextCombat",
                    Keyword = "Shield"
                }
            });
            return spell;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using SpireChess.Config;

namespace SpireChess.Run
{
    public static class HeroIds
    {
        public const string Warrior = "warrior";
        public const string Mage = "mage";
        public const string Rogue = "rogue";

        public static bool IsKnown(string heroId)
        {
            return HeroCatalog.TryGet(heroId, out _);
        }
    }

    public sealed class HeroDefinition
    {
        public HeroDefinition(
            string id,
            string displayName,
            string passiveName,
            string passiveDescription,
            string unlockCondition)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            PassiveName = passiveName ?? throw new ArgumentNullException(nameof(passiveName));
            PassiveDescription = passiveDescription ??
                                 throw new ArgumentNullException(nameof(passiveDescription));
            UnlockCondition = unlockCondition ??
                              throw new ArgumentNullException(nameof(unlockCondition));
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string PassiveName { get; }
        public string PassiveDescription { get; }
        public string UnlockCondition { get; }
    }

    public static class HeroCatalog
    {
        private static readonly IReadOnlyList<HeroDefinition> Definitions =
            new[]
            {
                new HeroDefinition(
                    HeroIds.Warrior,
                    "战士",
                    "坚甲启程",
                    "创建战士单局时获得 10 点旅团护甲；旅团护甲优先抵挡战斗结算伤害。",
                    "默认解锁"),
                new HeroDefinition(
                    HeroIds.Mage,
                    "法师",
                    "旅途灵感",
                    "每个商店阶段开始时，随机获得一张不高于当前酒馆等级的临时法术。",
                    "首次击败“荒野”Boss"),
                new HeroDefinition(
                    HeroIds.Rogue,
                    "盗贼",
                    "顺手牵羊",
                    "商店阶段关闭前，从仍在商店中的随从里随机偷取一张。",
                    "首次击败“星轨高原”Boss")
            };

        private static readonly IReadOnlyDictionary<string, HeroDefinition> ById =
            Definitions.ToDictionary(value => value.Id, StringComparer.Ordinal);

        public static IReadOnlyList<HeroDefinition> All => Definitions;

        public static bool TryGet(string heroId, out HeroDefinition definition)
        {
            return ById.TryGetValue(heroId ?? string.Empty, out definition);
        }

        public static HeroDefinition GetRequired(string heroId)
        {
            if (!TryGet(heroId, out var definition))
            {
                throw new ArgumentException($"Unknown hero id {heroId}.", nameof(heroId));
            }

            return definition;
        }
    }

    public readonly struct BattleDamageResolution
    {
        public BattleDamageResolution(
            int armorAbsorbed,
            int healthDamage,
            int remainingArmor,
            int remainingHealth)
        {
            ArmorAbsorbed = armorAbsorbed;
            HealthDamage = healthDamage;
            RemainingArmor = remainingArmor;
            RemainingHealth = remainingHealth;
        }

        public int ArmorAbsorbed { get; }
        public int HealthDamage { get; }
        public int RemainingArmor { get; }
        public int RemainingHealth { get; }
    }

    public enum HeroPassiveShopStartOutcome
    {
        None,
        GrantedTemporarySpell,
        BenchFull,
        NoEligibleSpell
    }

    public enum HeroPassiveShopEndOutcome
    {
        None,
        StoleMinion,
        BenchFull,
        NoVisibleMinion
    }

    public static class HeroPassiveRules
    {
        public const int WarriorStartingArmor = 10;

        public static int GetStartingArmor(string heroId)
        {
            HeroCatalog.GetRequired(heroId);
            return string.Equals(
                heroId,
                HeroIds.Warrior,
                StringComparison.Ordinal)
                ? WarriorStartingArmor
                : 0;
        }

        public static IReadOnlyList<SpellConfig> GetMageShopStartCandidates(
            IEnumerable<SpellConfig> spells,
            int tavernTier)
        {
            return (spells ?? Enumerable.Empty<SpellConfig>())
                .Where(spell =>
                    spell != null &&
                    spell.Enabled &&
                    spell.ImplementationStatus == "Playable" &&
                    spell.ShopEligible &&
                    spell.Tier <= tavernTier)
                .ToList()
                .AsReadOnly();
        }

        public static BattleDamageResolution ResolveBattleDamage(
            int currentHealth,
            int currentArmor,
            int rawDamage)
        {
            if (currentHealth < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentHealth));
            }

            if (currentArmor < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentArmor));
            }

            if (rawDamage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rawDamage));
            }

            var armorAbsorbed = Math.Min(currentArmor, rawDamage);
            var healthDamage = rawDamage - armorAbsorbed;
            return new BattleDamageResolution(
                armorAbsorbed,
                healthDamage,
                currentArmor - armorAbsorbed,
                Math.Max(0, currentHealth - healthDamage));
        }
    }
}

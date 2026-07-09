using System;
using System.Collections.Generic;
using System.Linq;
using SpireChess.Config;

namespace SpireChess.Battle
{
    public sealed class BattleMinionRuntime
    {
        private readonly HashSet<string> keywords;

        public BattleMinionRuntime(MinionConfig config, bool isGolden = false)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            IsGolden = isGolden;
            keywords = new HashSet<string>(config.Keywords ?? Enumerable.Empty<string>());
            CurrentAttack = BaseAttack;
            CurrentHealth = BaseHealth;
            HasShield = keywords.Contains("Shield");
        }

        private BattleMinionRuntime(
            MinionConfig config,
            bool isGolden,
            int currentAttack,
            int currentHealth,
            int permanentAttackBonus,
            int permanentHealthBonus,
            bool hasShield,
            IEnumerable<string> keywords)
        {
            Config = config;
            IsGolden = isGolden;
            CurrentAttack = currentAttack;
            CurrentHealth = currentHealth;
            PermanentAttackBonus = permanentAttackBonus;
            PermanentHealthBonus = permanentHealthBonus;
            HasShield = hasShield;
            this.keywords = new HashSet<string>(keywords ?? Enumerable.Empty<string>());
        }

        public MinionConfig Config { get; }
        public string Id => Config.Id;
        public string Name => Config.Name;
        public int BaseAttack => IsGolden ? Config.GoldenAttack : Config.Attack;
        public int BaseHealth => IsGolden ? Config.GoldenHealth : Config.Health;
        public int CurrentAttack { get; private set; }
        public int CurrentHealth { get; private set; }
        public int PermanentAttackBonus { get; private set; }
        public int PermanentHealthBonus { get; private set; }
        public bool HasShield { get; private set; }
        public bool IsGolden { get; }
        public bool IsAlive => CurrentHealth > 0;
        public bool HasTaunt => keywords.Contains("Taunt");
        public bool HasCleave => keywords.Contains("Cleave");
        public IReadOnlyCollection<string> Keywords => keywords;

        public BattleMinionRuntime Clone()
        {
            return new BattleMinionRuntime(
                Config,
                IsGolden,
                CurrentAttack,
                CurrentHealth,
                PermanentAttackBonus,
                PermanentHealthBonus,
                HasShield,
                keywords);
        }

        public bool TakeDamage(int damage, IList<string> log)
        {
            if (damage <= 0 || !IsAlive)
            {
                return false;
            }

            if (HasShield)
            {
                HasShield = false;
                log.Add($"{Name} 的护盾抵挡了 {damage} 点伤害。");
                return false;
            }

            CurrentHealth -= damage;
            log.Add($"{Name} 受到 {damage} 点伤害，剩余生命 {Math.Max(CurrentHealth, 0)}。");

            if (CurrentHealth > 0)
            {
                return false;
            }

            log.Add($"{Name} 死亡。");
            return true;
        }

        public string BuildKeywordText()
        {
            if (keywords.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(" / ", keywords.Select(ToDisplayKeyword));
        }

        private static string ToDisplayKeyword(string keyword)
        {
            switch (keyword)
            {
                case "Taunt":
                    return "嘲讽";
                case "Shield":
                    return "护盾";
                case "Deathrattle":
                    return "亡语";
                case "Battlecry":
                    return "战吼";
                case "Cleave":
                    return "溅射";
                default:
                    return keyword;
            }
        }
    }
}

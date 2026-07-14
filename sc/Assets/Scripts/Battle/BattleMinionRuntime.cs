using System;
using System.Collections.Generic;
using System.Linq;
using SpireChess.Config;

namespace SpireChess.Battle
{
    public sealed class BattleMinionRuntime
    {
        private readonly HashSet<string> keywords;

        public BattleMinionRuntime(
            MinionConfig config,
            bool isGolden = false,
            int? initialAttack = null,
            int? initialHealth = null,
            string sourceInstanceId = null,
            int permanentAttackBonus = 0,
            int permanentHealthBonus = 0,
            IEnumerable<string> permanentKeywords = null,
            int summonEffectMultiplier = 1,
            int flourishStacks = 0)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            IsGolden = isGolden;
            keywords = new HashSet<string>(config.Keywords ?? Enumerable.Empty<string>());
            keywords.UnionWith(permanentKeywords ?? Enumerable.Empty<string>());
            SourceInstanceId = sourceInstanceId;
            PermanentAttackBonus = permanentAttackBonus;
            PermanentHealthBonus = permanentHealthBonus;
            CurrentAttack = initialAttack ?? BaseAttack + permanentAttackBonus;
            CurrentHealth = initialHealth ?? BaseHealth + permanentHealthBonus;
            CombatMaxHealth = CurrentHealth;
            HasShield = keywords.Contains("Shield");
            SummonEffectMultiplier = Math.Max(1, summonEffectMultiplier);
            FlourishStacks = Math.Max(0, Math.Min(isGolden ? 8 : 4, flourishStacks));
        }

        private BattleMinionRuntime(
            MinionConfig config,
            bool isGolden,
            int currentAttack,
            int currentHealth,
            int combatMaxHealth,
            int permanentAttackBonus,
            int permanentHealthBonus,
            bool hasShield,
            IEnumerable<string> keywords,
            string sourceInstanceId,
            int summonEffectMultiplier,
            int flourishStacks)
        {
            Config = config;
            IsGolden = isGolden;
            CurrentAttack = currentAttack;
            CurrentHealth = currentHealth;
            CombatMaxHealth = combatMaxHealth;
            PermanentAttackBonus = permanentAttackBonus;
            PermanentHealthBonus = permanentHealthBonus;
            HasShield = hasShield;
            SourceInstanceId = sourceInstanceId;
            this.keywords = new HashSet<string>(keywords ?? Enumerable.Empty<string>());
            SummonEffectMultiplier = summonEffectMultiplier;
            FlourishStacks = flourishStacks;
        }

        public MinionConfig Config { get; }
        public string SourceInstanceId { get; }
        public string Id => Config.Id;
        public string Name => Config.Name;
        public int BaseAttack => IsGolden ? Config.GoldenAttack : Config.Attack;
        public int BaseHealth => IsGolden ? Config.GoldenHealth : Config.Health;
        public int CurrentAttack { get; private set; }
        public int CurrentHealth { get; private set; }
        public int CombatMaxHealth { get; private set; }
        public int PermanentAttackBonus { get; private set; }
        public int PermanentHealthBonus { get; private set; }
        public bool HasShield { get; private set; }
        public bool IsGolden { get; }
        public int SummonEffectMultiplier { get; }
        public int FlourishStacks { get; private set; }
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
                CombatMaxHealth,
                PermanentAttackBonus,
                PermanentHealthBonus,
                HasShield,
                keywords,
                SourceInstanceId,
                SummonEffectMultiplier,
                FlourishStacks);
        }

        public int GainFlourish(int amount, int maximum)
        {
            if (amount <= 0 || maximum <= FlourishStacks)
            {
                return 0;
            }

            var previous = FlourishStacks;
            FlourishStacks = Math.Min(maximum, FlourishStacks + amount);
            return FlourishStacks - previous;
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

        public void AddTemporaryStats(int attack, int health, IList<string> log)
        {
            if (!IsAlive || (attack == 0 && health == 0))
            {
                return;
            }

            CurrentAttack += attack;
            CurrentHealth += health;
            CombatMaxHealth += health;
            log.Add($"{Name} 获得 {FormatStatChange(attack, health)}，当前为 {CurrentAttack}/{CurrentHealth}。");
        }

        public bool TryAddShield(IList<string> log)
        {
            if (!IsAlive || HasShield)
            {
                return false;
            }

            HasShield = true;
            keywords.Add("Shield");
            log?.Add($"{Name} 获得护盾。");
            return true;
        }

        public bool TryRemoveShield(IList<string> log)
        {
            if (!IsAlive || !HasShield)
            {
                return false;
            }

            HasShield = false;
            log?.Add($"{Name} 失去护盾。");
            return true;
        }

        public bool TryAddKeyword(string keyword, IList<string> log)
        {
            if (!IsAlive || string.IsNullOrWhiteSpace(keyword) || keywords.Contains(keyword))
            {
                return false;
            }

            if (keyword == "Shield")
            {
                return TryAddShield(log);
            }

            keywords.Add(keyword);
            log?.Add($"{Name} 获得 {ToDisplayKeyword(keyword)}。");
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

        private static string FormatStatChange(int attack, int health)
        {
            if (attack != 0 && health != 0)
            {
                return $"{FormatSigned(attack)}/{FormatSigned(health)}";
            }

            return attack != 0
                ? $"{FormatSigned(attack)} 攻击"
                : $"{FormatSigned(health)} 生命";
        }

        private static string FormatSigned(int value)
        {
            return value >= 0 ? $"+{value}" : value.ToString();
        }
    }
}

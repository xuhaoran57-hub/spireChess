using System;
using System.Collections.Generic;
using System.Linq;
using SpireChess.Config;
using SpireChess.Shop;

namespace SpireChess.UI.Shop
{
    public static class ShopCardViewModelFactory
    {
        private static readonly IReadOnlyDictionary<string, string> AbilityLabelByTag =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["adjacent"] = "相邻",
                ["aoe"] = "群体伤害",
                ["attack"] = "攻击",
                ["battlecry"] = "战吼",
                ["cleave"] = "溅射",
                ["copy"] = "复制",
                ["counter"] = "反制",
                ["damage"] = "伤害",
                ["death_buff"] = "死亡增益",
                ["death_growth"] = "死亡成长",
                ["deathrattle"] = "亡语",
                ["delayed"] = "延迟",
                ["discover_minion"] = "随从发现",
                ["discover_spell"] = "法术发现",
                ["economy"] = "经济",
                ["frontline"] = "前排",
                ["global_growth"] = "群体成长",
                ["gold"] = "金币",
                ["golden_bonus"] = "金色加成",
                ["health"] = "生命",
                ["immediate_attack"] = "立即攻击",
                ["keyword_copy"] = "关键词复制",
                ["late_game"] = "后期",
                ["next_combat"] = "下场战斗",
                ["permanent_growth"] = "永久成长",
                ["position"] = "站位",
                ["race"] = "种族",
                ["refresh"] = "刷新",
                ["refresh_growth"] = "刷新成长",
                ["sell"] = "出售",
                ["shield"] = "护盾",
                ["shield_break"] = "破盾",
                ["shield_gain"] = "授盾",
                ["shield_loop"] = "盾链",
                ["spell"] = "法术",
                ["spell_growth"] = "施法成长",
                ["summon"] = "召唤",
                ["summon_buff"] = "召唤增益",
                ["summon_death"] = "召唤物死亡",
                ["survival"] = "生存",
                ["system_reward"] = "系统奖励",
                ["taunt"] = "嘲讽",
                ["token"] = "召唤物",
                ["triple_helper"] = "三连辅助",
                ["triple_reward"] = "三连奖励"
            };

        private static readonly IReadOnlyDictionary<string, string> KeywordLabelById =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Battlecry"] = "战吼",
                ["Cleave"] = "溅射",
                ["Deathrattle"] = "亡语",
                ["Shield"] = "护盾",
                ["Taunt"] = "嘲讽"
            };

        public static CardViewModel FromOffer(MinionConfig minion, int currentGold)
        {
            if (minion == null)
            {
                throw new ArgumentNullException(nameof(minion));
            }

            var cost = ShopEconomyRules.MinionPurchaseCost;
            var affordable = currentGold >= cost;
            return new CardViewModel
            {
                ArtId = minion.ArtId,
                Name = minion.Name,
                Description = minion.GetPrototypeDescription(false),
                RaceText = ToRaceText(minion.Race),
                AbilityLabels = ToAbilityLabels(minion.Tags),
                Tier = minion.Tier,
                Attack = minion.Attack,
                Health = minion.Health,
                BaseAttack = minion.Attack,
                BaseHealth = minion.Health,
                Cost = cost,
                DisplayMode = CardDisplayMode.Full,
                IsMinion = true,
                ShowCost = true,
                IsInteractable = affordable,
                IsAffordable = affordable,
                HasShield = ContainsKeyword(minion.Keywords, "Shield"),
                Keywords = ToKeywordLabels(minion.Keywords)
            };
        }

        public static CardViewModel FromOffer(SpellConfig spell, int currentGold)
        {
            if (spell == null)
            {
                throw new ArgumentNullException(nameof(spell));
            }

            var cost = ShopEconomyRules.SpellPurchaseCost;
            var affordable = currentGold >= cost;
            return new CardViewModel
            {
                ArtId = spell.ArtId,
                Name = spell.Name,
                Description = spell.Description,
                RaceText = ToSpellTypeText(spell.SpellType),
                AbilityLabels = ToAbilityLabels(spell.Tags),
                Tier = spell.Tier,
                Cost = cost,
                DisplayMode = CardDisplayMode.Full,
                ShowCost = true,
                IsInteractable = affordable,
                IsAffordable = affordable
            };
        }

        public static CardViewModel FromOwned(ShopCardInstance card, bool selected)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            return card.CardType == ShopCardType.Minion
                ? FromOwnedMinion(card, selected)
                : FromOwnedSpell(card, selected);
        }

        private static CardViewModel FromOwnedMinion(ShopCardInstance card, bool selected)
        {
            var minion = card.Minion;
            var baseAttack = card.IsGolden ? minion.GoldenAttack : minion.Attack;
            var baseHealth = card.IsGolden ? minion.GoldenHealth : minion.Health;
            var keywords = (minion.Keywords ?? new List<string>())
                .Concat(card.PermanentKeywords ?? Array.Empty<string>());

            return new CardViewModel
            {
                InstanceId = card.InstanceId,
                ArtId = minion.ArtId,
                Name = minion.Name,
                Description = minion.GetPrototypeDescription(card.IsGolden),
                RaceText = ToRaceText(minion.Race),
                AbilityLabels = ToAbilityLabels(minion.Tags),
                Tier = minion.Tier,
                Attack = card.CurrentAttack,
                Health = card.CurrentHealth,
                BaseAttack = baseAttack,
                BaseHealth = baseHealth,
                Cost = ShopEconomyRules.MinionPurchaseCost,
                DisplayMode = CardDisplayMode.Compact,
                IsMinion = true,
                IsGolden = card.IsGolden,
                IsSelected = selected,
                IsInteractable = true,
                IsAffordable = true,
                HasShield = card.HasPermanentShield,
                HasNextCombatShield = card.HasPendingCombatShield,
                IsTemporary = card.ExpiresAtShopEnd,
                Keywords = ToKeywordLabels(keywords)
            };
        }

        private static CardViewModel FromOwnedSpell(ShopCardInstance card, bool selected)
        {
            var spell = card.Spell;
            return new CardViewModel
            {
                InstanceId = card.InstanceId,
                ArtId = spell.ArtId,
                Name = spell.Name,
                Description = spell.Description,
                RaceText = ToSpellTypeText(spell.SpellType),
                AbilityLabels = ToAbilityLabels(spell.Tags),
                Tier = spell.Tier,
                Cost = ShopEconomyRules.SpellPurchaseCost,
                DisplayMode = CardDisplayMode.Compact,
                IsSelected = selected,
                IsInteractable = true,
                IsAffordable = true,
                IsTemporary = card.ExpiresAtShopEnd
            };
        }

        private static string[] ToAbilityLabels(IEnumerable<string> tags)
        {
            return (tags ?? Array.Empty<string>())
                .Select(ToAbilityLabel)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        private static string ToAbilityLabel(string tag)
        {
            return !string.IsNullOrWhiteSpace(tag) &&
                   AbilityLabelByTag.TryGetValue(tag, out var label)
                ? label
                : null;
        }

        private static string[] ToKeywordLabels(IEnumerable<string> keywords)
        {
            return (keywords ?? Array.Empty<string>())
                .Select(ToKeywordLabel)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        private static string ToKeywordLabel(string keyword)
        {
            return !string.IsNullOrWhiteSpace(keyword) &&
                   KeywordLabelById.TryGetValue(keyword, out var label)
                ? label
                : null;
        }

        private static bool ContainsKeyword(IEnumerable<string> keywords, string keyword)
        {
            return keywords != null && keywords.Contains(keyword);
        }

        private static string ToRaceText(string race)
        {
            switch (race)
            {
                case "ForgeSoul": return "铸魂";
                case "WildSpirit": return "荒灵";
                case "Starbound": return "星契";
                case "Wayfarer": return "旅团";
                default: return string.IsNullOrWhiteSpace(race) ? "无种族" : race;
            }
        }

        private static string ToSpellTypeText(string spellType)
        {
            switch (spellType)
            {
                case "CombatBuff": return "战斗强化";
                case "Copy": return "复制";
                case "Defense": return "防御";
                case "Discover": return "发现";
                case "Economy": return "经济";
                case "Growth": return "成长";
                case "Refresh": return "刷新";
                default: return string.IsNullOrWhiteSpace(spellType) ? "法术" : spellType;
            }
        }
    }
}

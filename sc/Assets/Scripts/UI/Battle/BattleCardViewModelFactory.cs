using System;
using System.Collections.Generic;
using System.Linq;
using SpireChess.Battle;
using SpireChess.UI;

namespace SpireChess.UI.Battle
{
    public static class BattleCardViewModelFactory
    {
        private static readonly IReadOnlyDictionary<string, string> KeywordLabels =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Battlecry"] = "战吼",
                ["Cleave"] = "溅射",
                ["Deathrattle"] = "亡语",
                ["Shield"] = "护盾",
                ["Taunt"] = "嘲讽"
            };

        public static CardViewModel FromRuntime(
            BattleMinionRuntime minion,
            BattleSide side,
            int slotIndex)
        {
            if (minion == null)
            {
                throw new ArgumentNullException(nameof(minion));
            }

            var keywords = (minion.Keywords ?? Array.Empty<string>())
                .Select(ToKeywordLabel)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            return new CardViewModel
            {
                InstanceId = ResolveInstanceId(minion, side, slotIndex),
                Name = minion.Name,
                Description = minion.Config.GetPrototypeDescription(minion.IsGolden),
                RaceText = ToRaceText(minion.Config.Race),
                AbilityLabels = keywords,
                Tier = minion.Config.Tier,
                Attack = minion.CurrentAttack,
                Health = Math.Max(0, minion.CurrentHealth),
                BaseAttack = minion.BaseAttack,
                BaseHealth = minion.BaseHealth,
                DisplayMode = CardDisplayMode.Compact,
                IsMinion = true,
                IsGolden = minion.IsGolden,
                IsInteractable = true,
                IsAffordable = true,
                HasShield = minion.HasShield,
                Keywords = keywords
            };
        }

        private static string ResolveInstanceId(
            BattleMinionRuntime minion,
            BattleSide side,
            int slotIndex)
        {
            return !string.IsNullOrWhiteSpace(minion.RuntimeInstanceId)
                ? minion.RuntimeInstanceId
                : $"{side}:{slotIndex}:{minion.Id}";
        }

        private static string ToKeywordLabel(string keyword)
        {
            return !string.IsNullOrWhiteSpace(keyword) &&
                   KeywordLabels.TryGetValue(keyword, out var label)
                ? label
                : null;
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
    }
}

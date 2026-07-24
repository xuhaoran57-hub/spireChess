using System;

namespace SpireChess.UI
{
    public static class PresentationArtworkFallbackIds
    {
        public const string Missing = "fallback_missing_art";

        public static string ForMinion(string race)
        {
            switch (race)
            {
                case "ForgeSoul": return "fallback_minion_forge_soul";
                case "WildSpirit": return "fallback_minion_wild_spirit";
                case "Starbound": return "fallback_minion_starbound";
                case "Wayfarer": return "fallback_minion_wayfarer";
                default: return null;
            }
        }

        public static string ForSpell(string spellType)
        {
            if (string.IsNullOrWhiteSpace(spellType))
            {
                return "fallback_spell_generic";
            }

            return "fallback_spell_" + ToSnakeCase(spellType);
        }

        private static string ToSnakeCase(string value)
        {
            var result = string.Empty;
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (char.IsUpper(character) && index > 0)
                {
                    result += "_";
                }

                result += char.ToLowerInvariant(character);
            }

            return result;
        }
    }
}

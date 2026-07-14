using System;
using System.Collections.Generic;
using System.Linq;
using SpireChess.Battle;
using SpireChess.Shop;

namespace SpireChess.Run
{
    public sealed class CoreActivationEvidence
    {
        public int ShieldEvents { get; set; }
        public int ShieldBenefitEvents { get; set; }
        public int SummonSuccesses { get; set; }
        public int NonTokenDeathBenefitEvents { get; set; }
        public int SpellsUsed { get; set; }
        public int Refreshes { get; set; }
    }

    public static class CoreBuildClassifier
    {
        public const string Version = "0.2.1";

        private static readonly IReadOnlyDictionary<string, CoreDefinition> Definitions =
            new Dictionary<string, CoreDefinition>
            {
                ["B01_SHIELD"] = new CoreDefinition(
                    new[] { "shieldwall_furnace_keeper", "resonance_bell_guard", "hearth_core_aegis_officer", "undying_furnace_king" },
                    new[] { "molten_core_standard", "thousand_ring_tomb_guardian" }),
                ["B02_BREAK"] = new CoreDefinition(
                    new[] { "shieldbreaker_blade_blank", "oathblade_armor", "cracked_armor_avenger", "cinder_armor_arbiter", "oathbroken_blade_soul" },
                    new[] { "ember_engraver", "counterflow_smith", "cracked_armor_avenger", "cinder_armor_arbiter", "oathbroken_blade_soul" }),
                ["B03_SUMMON"] = new CoreDefinition(
                    new[] { "young_deer_spirit", "two_tailed_fox_spirit", "fox_den_matriarch", "hundred_song_herd" },
                    new[] { "rending_cub", "swiftwing_forest_hawk", "many_branch_invoker", "vinecrown_priest", "tuskherd_pathrunner", "ten_thousand_hoof_surge" }),
                ["B04_DEATH"] = new CoreDefinition(
                    new[] { "moss_mark_seedling", "root_devourer", "ancient_moss_hatchling", "rotleaf_heir", "mountain_belly_soul_eater" },
                    new[] { "rootbound_soul_guide", "ancient_mountain_spirit", "world_eating_final_bloom" }),
                ["B05_SPELL"] = new CoreDefinition(
                    new[] { "glimmer_mage", "rune_ward_reader", "echo_starchanter", "secret_page_refractor" },
                    new[] { "stargate_lecturer", "falling_light_arbiter", "falling_star_prophet" }),
                ["B06_REFRESH"] = new CoreDefinition(
                    new[] { "stargazing_apprentice", "star_etched_timekeeper", "fate_track_recorder" },
                    new[] { "stardust_attendant", "astrolabe_calibrator", "moonwheel_dispatcher", "star_ring_treasurer", "sky_covenant_bearer" })
            };

        public static bool ContainsAnyCore(IEnumerable<ShopCardInstance> cards)
        {
            var ids = new HashSet<string>((cards ?? Enumerable.Empty<ShopCardInstance>())
                .Where(value => value != null && value.CardType == ShopCardType.Minion)
                .Select(value => value.ConfigId));
            return Definitions.Values.Any(definition =>
                definition.CoreA.Overlaps(ids) || definition.CoreB.Overlaps(ids));
        }

        public static bool IsCoreCardId(string cardId)
        {
            return !string.IsNullOrWhiteSpace(cardId) && Definitions.Values.Any(definition =>
                definition.CoreA.Contains(cardId) || definition.CoreB.Contains(cardId));
        }

        public static IReadOnlyList<string> MatchActivatedBuilds(
            IEnumerable<ShopCardInstance> battleCards,
            CoreActivationEvidence evidence)
        {
            if (evidence == null) throw new ArgumentNullException(nameof(evidence));
            var cards = (battleCards ?? Enumerable.Empty<ShopCardInstance>())
                .Where(value => value != null && value.CardType == ShopCardType.Minion)
                .ToList();
            var matches = new List<string>();
            foreach (var pair in Definitions)
            {
                var a = cards.Where(card => pair.Value.CoreA.Contains(card.ConfigId)).ToList();
                var b = cards.Where(card => pair.Value.CoreB.Contains(card.ConfigId)).ToList();
                if (!a.Any(first => b.Any(second => !ReferenceEquals(first, second))) ||
                    !HasActivationEvidence(pair.Key, evidence))
                {
                    continue;
                }

                matches.Add(pair.Key);
            }

            return matches.AsReadOnly();
        }

        public static string ClassifyFinalBuild(
            IEnumerable<ShopCardInstance> battleCards,
            CoreActivationEvidence evidence)
        {
            var matches = MatchActivatedBuilds(battleCards, evidence);
            return matches.Count == 0 ? "Unclassified" : matches.Count == 1 ? matches[0] : "Mixed";
        }

        private static bool HasActivationEvidence(
            string buildId,
            CoreActivationEvidence evidence)
        {
            switch (buildId)
            {
                case "B01_SHIELD":
                    return evidence.ShieldEvents >= 2;
                case "B02_BREAK":
                    return evidence.ShieldEvents >= 2 && evidence.ShieldBenefitEvents >= 1;
                case "B03_SUMMON":
                    return evidence.SummonSuccesses >= 2;
                case "B04_DEATH":
                    return evidence.NonTokenDeathBenefitEvents >= 1;
                case "B05_SPELL":
                    return evidence.SpellsUsed >= 2;
                case "B06_REFRESH":
                    return evidence.Refreshes >= 2;
                default:
                    return false;
            }
        }

        private sealed class CoreDefinition
        {
            public CoreDefinition(IEnumerable<string> coreA, IEnumerable<string> coreB)
            {
                CoreA = new HashSet<string>(coreA);
                CoreB = new HashSet<string>(coreB);
            }

            public HashSet<string> CoreA { get; }
            public HashSet<string> CoreB { get; }
        }
    }
}

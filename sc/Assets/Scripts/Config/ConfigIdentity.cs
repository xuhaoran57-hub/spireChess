using System;
using System.Collections.Generic;
using System.Linq;
using SpireChess.Utils;

namespace SpireChess.Config
{
    public sealed class ConfigIdentity
    {
        public static readonly IReadOnlyList<string> ResourceFileNames = new[]
        {
            "minions.v0.1.json",
            "spells.v0.1.json",
            "encounters.v0.1.json",
            "rewards.v0.1.json",
            "events.v0.1.json",
            "relics.v0.1.json",
            "content-release.v0.1.json",
            "run-maps.v0.1.json",
            "enhancements.v0.1.json",
            "rests.v0.1.json"
        };

        public ConfigIdentity(
            string contentVersion,
            string rulesVersion,
            string configHash)
        {
            ContentVersion = contentVersion ?? string.Empty;
            RulesVersion = rulesVersion ?? string.Empty;
            ConfigHash = configHash ?? string.Empty;
        }

        public string ContentVersion { get; }
        public string RulesVersion { get; }
        public string ConfigHash { get; }

        public bool Matches(ConfigIdentity other)
        {
            return other != null &&
                   string.Equals(ContentVersion, other.ContentVersion, StringComparison.Ordinal) &&
                   string.Equals(RulesVersion, other.RulesVersion, StringComparison.Ordinal) &&
                   string.Equals(ConfigHash, other.ConfigHash, StringComparison.Ordinal);
        }

        public static ConfigIdentity Create(
            ContentReleaseConfig release,
            IEnumerable<string> jsonDocuments)
        {
            if (release == null)
            {
                throw new ArgumentNullException(nameof(release));
            }

            var documents = (jsonDocuments ?? throw new ArgumentNullException(
                    nameof(jsonDocuments)))
                .ToArray();
            if (documents.Length != ResourceFileNames.Count)
            {
                throw new ArgumentException(
                    $"Expected {ResourceFileNames.Count} config documents, got {documents.Length}.",
                    nameof(jsonDocuments));
            }

            return new ConfigIdentity(
                release.ContentVersion,
                release.MinimumRulesVersion,
                CanonicalJson.ComputeSha256(documents));
        }
    }
}

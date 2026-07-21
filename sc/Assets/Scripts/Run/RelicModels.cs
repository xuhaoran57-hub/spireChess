using System;
using System.Collections.Generic;
using SpireChess.Config;
using SpireChess.Shop;

namespace SpireChess.Run
{
    public enum RelicCompletionMode
    {
        ResolveNodeToMap,
        FloorComplete
    }

    public sealed class RelicCandidate
    {
        public RelicCandidate(string candidateId, RelicConfig config)
        {
            CandidateId = candidateId ?? throw new ArgumentNullException(nameof(candidateId));
            RelicId = config?.Id ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name;
            Description = config.Description;
            Grade = config.Grade;
            Category = config.Category;
        }

        public string CandidateId { get; }
        public string RelicId { get; }
        public string Name { get; }
        public string Description { get; }
        public string Grade { get; }
        public string Category { get; }
        public string DisplayText => $"{Name}\n{Description}";
    }

    public sealed class PendingRelicChoice
    {
        public PendingRelicChoice(
            string choiceId,
            string sourceAttemptId,
            string grade,
            RelicCompletionMode completionMode,
            IEnumerable<RelicCandidate> candidates,
            int healthCost,
            bool allowSkip)
        {
            ChoiceId = choiceId ?? throw new ArgumentNullException(nameof(choiceId));
            SourceAttemptId = sourceAttemptId ??
                throw new ArgumentNullException(nameof(sourceAttemptId));
            Grade = grade ?? throw new ArgumentNullException(nameof(grade));
            CompletionMode = completionMode;
            Candidates = new List<RelicCandidate>(
                candidates ?? throw new ArgumentNullException(nameof(candidates))).AsReadOnly();
            HealthCost = Math.Max(0, healthCost);
            AllowSkip = allowSkip;
        }

        public string ChoiceId { get; }
        public string SourceAttemptId { get; }
        public string Grade { get; }
        public RelicCompletionMode CompletionMode { get; }
        public IReadOnlyList<RelicCandidate> Candidates { get; }
        public int HealthCost { get; }
        public bool AllowSkip { get; }
    }

    public sealed class OwnedRelicState
    {
        internal OwnedRelicState(
            RelicConfig config,
            string sourceType,
            string sourceId,
            int acquiredFloor,
            int acquiredShopTurn)
        {
            RelicId = config?.Id ?? throw new ArgumentNullException(nameof(config));
            Grade = config.Grade;
            SourceType = sourceType ?? string.Empty;
            SourceId = sourceId ?? string.Empty;
            AcquiredFloor = acquiredFloor;
            AcquiredShopTurn = acquiredShopTurn;
            LastResolvedShopTurn = acquiredShopTurn;
        }

        public string RelicId { get; }
        public string Grade { get; }
        public string SourceType { get; }
        public string SourceId { get; }
        public int AcquiredFloor { get; }
        public int AcquiredShopTurn { get; }
        public int ShopProgress { get; internal set; }
        public int LastResolvedShopTurn { get; internal set; }
        public int ActivationCount { get; internal set; }
    }

    public sealed class RelicCardGrant
    {
        public RelicCardGrant(
            string relicId,
            ShopCardType cardType,
            string configId,
            int reservedPoolCopies)
        {
            RelicId = relicId;
            CardType = cardType;
            ConfigId = configId;
            ReservedPoolCopies = Math.Max(0, reservedPoolCopies);
        }

        public string RelicId { get; }
        public ShopCardType CardType { get; }
        public string ConfigId { get; }
        public int ReservedPoolCopies { get; }
    }

    public sealed class RelicActivationData
    {
        public RelicActivationData(string relicId, string trigger, int amount = 0, string cardId = null)
        {
            RelicId = relicId;
            Trigger = trigger;
            Amount = amount;
            CardId = cardId;
        }

        public string RelicId { get; }
        public string Trigger { get; }
        public int Amount { get; }
        public string CardId { get; }
    }
}

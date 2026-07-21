using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Run;
using SpireChess.Shop;
using SpireChess.Utils;

namespace SpireChess.Save
{
    public sealed class RunSnapshotValidationResult
    {
        private readonly List<string> errors = new List<string>();

        public IReadOnlyList<string> Errors => errors;
        public bool IsValid => errors.Count == 0;

        internal void Add(string error)
        {
            errors.Add(error);
        }
    }

    public sealed class RunSnapshotValidator
    {
        private readonly ConfigService configs;

        public RunSnapshotValidator(ConfigService configs)
        {
            this.configs = configs ?? throw new ArgumentNullException(nameof(configs));
        }

        public RunSnapshotValidationResult ValidateDto(RunSavePayloadV1 payload)
        {
            var result = new RunSnapshotValidationResult();
            if (payload == null)
            {
                result.Add("Payload is missing.");
                return result;
            }

            if (payload.RunState == null) result.Add("Run state is missing.");
            if (payload.ShopSession == null) result.Add("Shop state is missing.");
            if (payload.RandomStreams == null) result.Add("Random streams are missing.");
            if (payload.Sequences == null) result.Add("Sequences are missing.");
            if (!result.IsValid)
            {
                return result;
            }

            ValidateRunState(payload, result);
            ValidateShop(payload.ShopSession, result);
            ValidateRandomStreams(payload.RandomStreams, result);
            ValidateSequences(payload, result);
            return result;
        }

        public RunSnapshotValidationResult ValidateHydratedRun(RunSession session)
        {
            if (session == null)
            {
                var missing = new RunSnapshotValidationResult();
                missing.Add("Restored run is missing.");
                return missing;
            }

            return ValidateDto(new RunSnapshotMapper(configs).Capture(session));
        }

        private void ValidateRunState(
            RunSavePayloadV1 payload,
            RunSnapshotValidationResult result)
        {
            var state = payload.RunState;
            if (!Enum.IsDefined(typeof(RunPhase), state.Phase))
            {
                result.Add($"Unknown run phase {state.Phase}.");
            }
            if (state.Phase == RunPhase.EnteringNode)
            {
                result.Add("EnteringNode is not a durable phase.");
            }

            if (state.Floor < 1 || state.Floor > 3)
            {
                result.Add($"Invalid floor {state.Floor}.");
            }

            if (state.ShopTurn < 0 || state.MapStep < 0)
            {
                result.Add("Run turn counters cannot be negative.");
            }

            if (state.MaxHealth <= 0 || state.Health < 0 || state.Health > state.MaxHealth)
            {
                result.Add($"Invalid health {state.Health}/{state.MaxHealth}.");
            }

            if (state.Phase == RunPhase.RunLost && state.Health > 0)
            {
                result.Add("RunLost requires zero health.");
            }

            var mapConfig = configs.RunMaps.FirstOrDefault(value =>
                value != null && value.Floor == state.Floor);
            if (mapConfig == null || !string.Equals(
                    mapConfig.Id,
                    state.MapId,
                    StringComparison.Ordinal))
            {
                result.Add($"Map {state.MapId} does not match floor {state.Floor}.");
            }
            else
            {
                var nodeIds = new HashSet<string>(mapConfig.Nodes.Select(value => value.Id));
                if (state.NodeStatuses == null || state.NodeStatuses.Count != nodeIds.Count ||
                    state.NodeStatuses.Keys.Any(value => !nodeIds.Contains(value)))
                {
                    result.Add("Map node statuses do not match current map.");
                }
                else if (state.NodeStatuses.Values.Any(value =>
                             !Enum.IsDefined(typeof(RunNodeStatus), value)))
                {
                    result.Add("Map node statuses contain an unknown value.");
                }

                if (!string.IsNullOrWhiteSpace(state.CurrentNodeId) &&
                    !nodeIds.Contains(state.CurrentNodeId))
                {
                    result.Add($"Current node {state.CurrentNodeId} is not in current map.");
                }
            }

            ValidateAttempt(state, result);
            ValidatePendingPhase(payload, result);
            ValidateRunReferences(state, result);
            if (state.Statistics == null || state.Statistics.StartedAtUtc == default(DateTime))
            {
                result.Add("Run statistics start time is missing.");
            }
        }

        private static void ValidateAttempt(
            RunStateSnapshotV1 state,
            RunSnapshotValidationResult result)
        {
            var attempt = state.CurrentAttempt;
            if (attempt == null)
            {
                if (!string.IsNullOrWhiteSpace(state.CurrentNodeId))
                {
                    result.Add("Current node has no attempt.");
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(attempt.NodeAttemptId) ||
                !string.Equals(attempt.NodeId, state.CurrentNodeId, StringComparison.Ordinal))
            {
                result.Add("Current attempt does not match current node.");
            }
        }

        private static void ValidatePendingPhase(
            RunSavePayloadV1 payload,
            RunSnapshotValidationResult result)
        {
            var state = payload.RunState;
            var pendingCount =
                (state.PendingRewardChoice == null ? 0 : 1) +
                (state.PendingRelicChoice == null ? 0 : 1) +
                (state.PendingEventChoice == null ? 0 : 1) +
                (state.PendingEnhanceChoice == null ? 0 : 1) +
                (state.PendingRestChoice == null ? 0 : 1);
            var expectedChoice = state.Phase == RunPhase.RewardChoice ||
                                 state.Phase == RunPhase.RelicChoice ||
                                 state.Phase == RunPhase.EventChoice ||
                                 state.Phase == RunPhase.EnhanceChoice ||
                                 state.Phase == RunPhase.RestChoice;
            if (expectedChoice && pendingCount != 1)
            {
                result.Add($"Phase {state.Phase} requires exactly one pending choice.");
            }
            else if (!expectedChoice && pendingCount != 0)
            {
                result.Add($"Phase {state.Phase} cannot retain a run choice.");
            }

            if ((state.Phase == RunPhase.RewardChoice) !=
                (state.PendingRewardChoice != null))
                result.Add("Reward choice phase mismatch.");
            if ((state.Phase == RunPhase.RelicChoice) !=
                (state.PendingRelicChoice != null))
                result.Add("Relic choice phase mismatch.");
            if ((state.Phase == RunPhase.EventChoice) !=
                (state.PendingEventChoice != null))
                result.Add("Event choice phase mismatch.");
            if ((state.Phase == RunPhase.EnhanceChoice) !=
                (state.PendingEnhanceChoice != null))
                result.Add("Enhance choice phase mismatch.");
            if ((state.Phase == RunPhase.RestChoice) !=
                (state.PendingRestChoice != null))
                result.Add("Rest choice phase mismatch.");

            if (state.Phase == RunPhase.Battle && payload.PendingBattle == null)
            {
                result.Add("Battle phase requires a pending battle.");
            }
            else if (state.Phase != RunPhase.Battle && payload.PendingBattle != null)
            {
                result.Add($"Phase {state.Phase} cannot retain a pending battle.");
            }

            if (state.Phase == RunPhase.BattleResult &&
                (payload.LastBattleContext == null || payload.LastBattleResult == null))
            {
                result.Add("BattleResult requires the committed battle result.");
            }
        }

        private void ValidateRunReferences(
            RunStateSnapshotV1 state,
            RunSnapshotValidationResult result)
        {
            foreach (var reward in state.PendingCardRewards ??
                     new List<PendingCardRewardSnapshotV1>())
            {
                if (!CardExists(reward.CardType, reward.ConfigId))
                {
                    result.Add($"Pending reward references missing card {reward.ConfigId}.");
                }
            }

            foreach (var relic in state.OwnedRelics ?? new List<OwnedRelicSnapshotV1>())
            {
                if (!configs.TryGetRelic(relic.RelicId, out _))
                {
                    result.Add($"Owned relic {relic.RelicId} is missing.");
                }
            }
        }

        private void ValidateShop(
            ShopSessionSnapshotV1 shop,
            RunSnapshotValidationResult result)
        {
            if (shop.Round < 0 || shop.Gold < 0 || shop.TavernTier < 1 ||
                shop.TavernTier > ShopEconomyRules.MaximumTavernTier)
            {
                result.Add("Shop economy values are invalid.");
            }

            if (shop.Bench == null || shop.Bench.Count != ShopEconomyRules.BenchSlotCount ||
                shop.Battle == null || shop.Battle.Count != ShopEconomyRules.BattleSlotCount)
            {
                result.Add("Shop collection slot count is invalid.");
                return;
            }

            var cards = shop.Bench.Concat(shop.Battle)
                .Where(value => value != null).ToList();
            var duplicateIds = cards.GroupBy(value => value.InstanceId)
                .Where(value => string.IsNullOrWhiteSpace(value.Key) || value.Count() > 1)
                .Select(value => value.Key)
                .ToArray();
            if (duplicateIds.Length > 0)
            {
                result.Add("Shop card instance ids are empty or duplicated.");
            }

            foreach (var card in cards)
            {
                if (!CardExists(card.CardType, card.ConfigId))
                {
                    result.Add($"Shop card {card.ConfigId} is missing.");
                }
            }

            if (shop.MinionPoolRemainingCopies == null ||
                shop.MinionPoolRemainingCopies.Any(value => value.Value < 0))
            {
                result.Add("Minion pool remaining copies are invalid.");
            }

            if (shop.CardInstanceSequence < MaximumSuffix(
                    cards.Select(value => value.InstanceId),
                    "shop_card_"))
            {
                result.Add("Card instance sequence trails an existing id.");
            }
        }

        private static void ValidateRandomStreams(
            RandomStreamsSnapshotV1 streams,
            RunSnapshotValidationResult result)
        {
            ValidateRandom(streams.Shop, "Shop", result);
            ValidateRandom(streams.Reward, "Reward", result);
            ValidateRandom(streams.Event, "Event", result);
            ValidateRandom(streams.Relic, "Relic", result);
        }

        private static void ValidateRandom(
            RandomStreamSnapshotV1 stream,
            string name,
            RunSnapshotValidationResult result)
        {
            if (stream?.Entries == null)
            {
                result.Add($"{name} random stream is missing.");
            }
            else if (stream.Entries.Count > RecordedRandom.MaximumRecordedCalls)
            {
                result.Add($"{name} random stream exceeds its call limit.");
            }
        }

        private static void ValidateSequences(
            RunSavePayloadV1 payload,
            RunSnapshotValidationResult result)
        {
            var sequences = payload.Sequences;
            if (sequences.AttemptSequence < 0 || sequences.RewardSequence < 0 ||
                sequences.ChoiceSequence < 0 || sequences.RelicChoiceSequence < 0 ||
                sequences.RelicCandidateSequence < 0)
            {
                result.Add("Sequence counters cannot be negative.");
                return;
            }

            var state = payload.RunState;
            var attemptIds = state.CurrentAttempt == null
                ? Array.Empty<string>()
                : new[] { state.CurrentAttempt.NodeAttemptId };
            if (sequences.AttemptSequence < MaximumSuffix(attemptIds, "attempt_"))
                result.Add("Attempt sequence trails an existing id.");
            if (sequences.RewardSequence < MaximumSuffix(
                    (state.PendingCardRewards ?? new List<PendingCardRewardSnapshotV1>())
                    .Select(value => value.RewardInstanceId),
                    "reward_"))
                result.Add("Reward sequence trails an existing id.");
            if (sequences.RelicCandidateSequence < MaximumSuffix(
                    state.PendingRelicChoice?.Candidates.Select(value => value.CandidateId) ??
                    Array.Empty<string>(),
                    "relic_candidate_"))
                result.Add("Relic candidate sequence trails an existing id.");
        }

        private bool CardExists(ShopCardType type, string id)
        {
            return type == ShopCardType.Minion
                ? configs.TryGetMinion(id, out _)
                : configs.TryGetSpell(id, out _);
        }

        private static int MaximumSuffix(IEnumerable<string> ids, string prefix)
        {
            var maximum = 0;
            foreach (var id in ids ?? Array.Empty<string>())
            {
                if (id != null && id.StartsWith(prefix, StringComparison.Ordinal) &&
                    int.TryParse(id.Substring(prefix.Length), out var value))
                {
                    maximum = Math.Max(maximum, value);
                }
            }

            return maximum;
        }
    }

    public static class RunStateFingerprint
    {
        public static string Compute(RunSavePayloadV1 payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            return CanonicalJson.ComputeTokenSha256(JToken.FromObject(payload));
        }
    }
}

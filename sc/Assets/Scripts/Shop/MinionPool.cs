using System;
using System.Collections.Generic;
using SpireChess.Config;

namespace SpireChess.Shop
{
    public sealed class MinionPool
    {
        private readonly List<MinionConfig> minions = new List<MinionConfig>();
        private readonly Dictionary<string, int> initialCopies = new Dictionary<string, int>();
        private readonly Dictionary<string, int> remainingCopies = new Dictionary<string, int>();

        public MinionPool(IEnumerable<MinionConfig> configs)
        {
            if (configs == null)
            {
                throw new ArgumentNullException(nameof(configs));
            }

            foreach (var config in configs)
            {
                if (config == null || !config.Enabled ||
                    config.ImplementationStatus != "Playable" || config.IsToken ||
                    config.Tier < 1 || config.Tier > ShopEconomyRules.MaximumTavernTier)
                {
                    continue;
                }

                if (remainingCopies.ContainsKey(config.Id))
                {
                    throw new InvalidOperationException($"Duplicate minion id in pool: {config.Id}.");
                }

                var copies = ShopEconomyRules.GetPoolCopiesPerMinion(config.Tier);
                minions.Add(config);
                initialCopies.Add(config.Id, copies);
                remainingCopies.Add(config.Id, copies);
            }
        }

        public MinionConfig Draw(int maximumTier, Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var totalCopies = 0;
            foreach (var minion in minions)
            {
                if (minion.Tier <= maximumTier)
                {
                    totalCopies += remainingCopies[minion.Id];
                }
            }

            if (totalCopies == 0)
            {
                return null;
            }

            var roll = random.Next(totalCopies);
            foreach (var minion in minions)
            {
                if (minion.Tier > maximumTier)
                {
                    continue;
                }

                var copies = remainingCopies[minion.Id];
                if (roll >= copies)
                {
                    roll -= copies;
                    continue;
                }

                remainingCopies[minion.Id] = copies - 1;
                return minion;
            }

            throw new InvalidOperationException("Minion pool draw failed after a valid roll.");
        }

        public bool Return(string minionId)
        {
            return ReturnCopies(minionId, 1) == 1;
        }

        public bool TryReserveCopies(string minionId, int copies)
        {
            if (copies <= 0 || string.IsNullOrWhiteSpace(minionId) ||
                !remainingCopies.TryGetValue(minionId, out var remaining) ||
                remaining < copies)
            {
                return false;
            }

            remainingCopies[minionId] = remaining - copies;
            return true;
        }

        public IReadOnlyList<MinionConfig> ReserveDistinctAtTier(
            int tier,
            int count,
            Random random)
        {
            return ReserveDistinct(tier, tier, null, count, random);
        }

        public IReadOnlyList<MinionConfig> ReserveDistinct(
            int minimumTier,
            int maximumTier,
            string race,
            int count,
            Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var reserved = new List<MinionConfig>();
            if (count <= 0)
            {
                return reserved;
            }

            var eligible = new List<MinionConfig>();
            foreach (var minion in minions)
            {
                if (minion.Tier >= minimumTier && minion.Tier <= maximumTier &&
                    (string.IsNullOrWhiteSpace(race) || minion.Race == race) &&
                    remainingCopies[minion.Id] > 0)
                {
                    eligible.Add(minion);
                }
            }

            while (reserved.Count < count && eligible.Count > 0)
            {
                var totalCopies = 0;
                foreach (var minion in eligible)
                {
                    totalCopies += remainingCopies[minion.Id];
                }

                if (totalCopies <= 0)
                {
                    break;
                }

                var roll = random.Next(totalCopies);
                MinionConfig selected = null;
                foreach (var minion in eligible)
                {
                    var copies = remainingCopies[minion.Id];
                    if (roll >= copies)
                    {
                        roll -= copies;
                        continue;
                    }

                    selected = minion;
                    break;
                }

                if (selected == null)
                {
                    throw new InvalidOperationException(
                        "Discover reservation failed after a valid roll.");
                }

                remainingCopies[selected.Id]--;
                reserved.Add(selected);
                eligible.Remove(selected);
            }

            return reserved;
        }

        public int ReturnCopies(string minionId, int copies)
        {
            if (copies <= 0 || string.IsNullOrWhiteSpace(minionId) ||
                !remainingCopies.TryGetValue(minionId, out var remaining))
            {
                return 0;
            }

            var maximum = initialCopies[minionId];
            if (remaining >= maximum)
            {
                return 0;
            }

            var returned = Math.Min(copies, maximum - remaining);
            remainingCopies[minionId] = remaining + returned;
            return returned;
        }

        public int GetRemainingCopies(string minionId)
        {
            return remainingCopies.TryGetValue(minionId, out var count) ? count : 0;
        }
    }
}

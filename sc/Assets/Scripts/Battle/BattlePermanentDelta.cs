using System;
using System.Collections.Generic;

namespace SpireChess.Battle
{
    public sealed class BattlePermanentDelta
    {
        private readonly HashSet<string> keywords = new HashSet<string>();

        public BattlePermanentDelta(string sourceInstanceId)
        {
            SourceInstanceId = sourceInstanceId ??
                throw new ArgumentNullException(nameof(sourceInstanceId));
        }

        public string SourceInstanceId { get; }
        public int Attack { get; private set; }
        public int Health { get; private set; }
        public int Flourish { get; private set; }
        public IReadOnlyCollection<string> Keywords => keywords;
        public int ApplicationCount { get; private set; }

        internal void Add(int attack, int health, string keyword = null)
        {
            Attack += attack;
            Health += health;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keywords.Add(keyword);
            }

            ApplicationCount++;
        }

        internal void AddFlourish(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Flourish += amount;
            ApplicationCount++;
        }
    }
}

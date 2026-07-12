using System;
using System.Collections.Generic;
using SpireChess.Config;

namespace SpireChess.Shop
{
    public sealed class ShopPhaseStats
    {
        public int RefreshCount { get; internal set; }
        public int SpellUsedCount { get; internal set; }
        public int SpellBoughtCount { get; internal set; }
        public int MinionBoughtCount { get; internal set; }

        internal void Reset()
        {
            RefreshCount = 0;
            SpellUsedCount = 0;
            SpellBoughtCount = 0;
            MinionBoughtCount = 0;
        }
    }

    public sealed class PendingCombatModifier
    {
        public PendingCombatModifier(
            string effectId,
            int attack,
            int health,
            string keyword,
            bool addShield)
        {
            EffectId = effectId;
            Attack = attack;
            Health = health;
            Keyword = keyword;
            AddShield = addShield;
        }

        public string EffectId { get; }
        public int Attack { get; }
        public int Health { get; }
        public string Keyword { get; }
        public bool AddShield { get; }
    }

    public sealed class ActiveShopEffect
    {
        public ActiveShopEffect(
            string sourceInstanceId,
            string sourceConfigId,
            EffectConfig effect,
            int activationRefreshCount)
        {
            SourceInstanceId = sourceInstanceId;
            SourceConfigId = sourceConfigId;
            Effect = effect ?? throw new ArgumentNullException(nameof(effect));
            ActivationRefreshCount = activationRefreshCount;
        }

        public string SourceInstanceId { get; }
        public string SourceConfigId { get; }
        public EffectConfig Effect { get; }
        public int ActivationRefreshCount { get; }
        public int TriggerCount { get; internal set; }
    }

    public enum EffectChoiceType
    {
        MinionCard,
        SpellCard,
        BattleTarget,
        Race
    }

    public sealed class EffectChoiceCandidate
    {
        public EffectChoiceCandidate(
            string id,
            string displayName,
            MinionConfig minion = null,
            SpellConfig spell = null,
            ShopCardInstance target = null)
        {
            Id = id;
            DisplayName = displayName;
            Minion = minion;
            Spell = spell;
            Target = target;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public MinionConfig Minion { get; }
        public SpellConfig Spell { get; }
        public ShopCardInstance Target { get; }
    }

    public sealed class PendingEffectChoice
    {
        public PendingEffectChoice(
            EffectChoiceType choiceType,
            ShopCardInstance sourceCard,
            int benchIndex,
            EffectConfig effect,
            IEnumerable<EffectChoiceCandidate> candidates,
            bool replaceSourceCard = true)
        {
            ChoiceType = choiceType;
            SourceCard = sourceCard ?? throw new ArgumentNullException(nameof(sourceCard));
            BenchIndex = benchIndex;
            Effect = effect ?? throw new ArgumentNullException(nameof(effect));
            Candidates = new List<EffectChoiceCandidate>(
                candidates ?? throw new ArgumentNullException(nameof(candidates))).AsReadOnly();
            ReplaceSourceCard = replaceSourceCard;
        }

        public EffectChoiceType ChoiceType { get; }
        public ShopCardInstance SourceCard { get; }
        public int BenchIndex { get; }
        public EffectConfig Effect { get; }
        public IReadOnlyList<EffectChoiceCandidate> Candidates { get; }
        public bool ReplaceSourceCard { get; }
    }
}

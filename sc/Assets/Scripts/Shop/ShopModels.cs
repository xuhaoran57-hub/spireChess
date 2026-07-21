using System;
using System.Collections.Generic;
using System.Linq;
using SpireChess.Config;

namespace SpireChess.Shop
{
    public enum ShopCardType
    {
        Minion,
        Spell
    }

    public enum ShopOperationError
    {
        None,
        ShopClosed,
        ShopAlreadyOpen,
        InvalidIndex,
        EmptySlot,
        InsufficientGold,
        BenchFull,
        InvalidCardType,
        InvalidCardLocation,
        OccupiedBattleSlot,
        InvalidTarget,
        NoBenefit,
        UnsupportedEffect,
        InvalidTiming,
        DiscoveryPending,
        NoDiscoveryPending,
        DiscoveryCannotBeCancelled,
        AlreadyUpgradedThisRound,
        MaximumTavernTier
    }

    public enum ShopEventType
    {
        OnShopPhaseStart,
        OnShopPhaseEnd,
        OnRefresh,
        OnBuy,
        OnSell,
        OnPlay,
        OnSpellUsed,
        OnTripleFormed,
        OnTripleRewardGranted,
        OnDiscoverStarted,
        OnDiscoverResolved,
        OnDiscoverCancelled,
        OnTavernUpgraded
    }

    public sealed class ShopOperationResult
    {
        private ShopOperationResult(bool success, ShopOperationError error, int benchIndex)
        {
            Success = success;
            Error = error;
            BenchIndex = benchIndex;
        }

        public bool Success { get; }
        public ShopOperationError Error { get; }
        public int BenchIndex { get; }

        public static ShopOperationResult Succeed(int benchIndex = -1)
        {
            return new ShopOperationResult(true, ShopOperationError.None, benchIndex);
        }

        public static ShopOperationResult Fail(ShopOperationError error)
        {
            return new ShopOperationResult(false, error, -1);
        }
    }

    public sealed class ShopRuleModifiers
    {
        public bool FirstPurchaseFree { get; set; }
        public bool FirstPaidRefreshFree { get; set; }
        public int FirstMinionSaleBonusGold { get; set; }
        public int ExtraBattlecryTriggers { get; set; }

        public ShopRuleModifiers Clone()
        {
            return new ShopRuleModifiers
            {
                FirstPurchaseFree = FirstPurchaseFree,
                FirstPaidRefreshFree = FirstPaidRefreshFree,
                FirstMinionSaleBonusGold = FirstMinionSaleBonusGold,
                ExtraBattlecryTriggers = ExtraBattlecryTriggers
            };
        }
    }

    public sealed class ShopCardInstance
    {
        private ShopCardInstance(
            string instanceId,
            ShopCardType cardType,
            MinionConfig minion,
            SpellConfig spell,
            bool isGolden,
            int permanentAttackBonus,
            int permanentHealthBonus,
            IEnumerable<string> permanentKeywords,
            bool tripleDiscoveryPending,
            bool expiresAtShopEnd)
        {
            InstanceId = instanceId;
            CardType = cardType;
            Minion = minion;
            Spell = spell;
            IsGolden = isGolden;
            PermanentAttackBonus = permanentAttackBonus;
            PermanentHealthBonus = permanentHealthBonus;
            permanentKeywordsSet = new HashSet<string>(
                permanentKeywords ?? Array.Empty<string>());
            PermanentKeywords = permanentKeywordsSet;
            TripleDiscoveryPending = tripleDiscoveryPending;
            ExpiresAtShopEnd = expiresAtShopEnd;
        }

        public string InstanceId { get; }
        public ShopCardType CardType { get; }
        public MinionConfig Minion { get; }
        public SpellConfig Spell { get; }
        public string ConfigId => CardType == ShopCardType.Minion ? Minion.Id : Spell.Id;
        public bool IsGolden { get; }
        public int PermanentAttackBonus { get; private set; }
        public int PermanentHealthBonus { get; private set; }
        public int FlourishAttackBonus { get; private set; }
        private readonly HashSet<string> permanentKeywordsSet;
        public IReadOnlyCollection<string> PermanentKeywords { get; }
        public bool TripleDiscoveryPending { get; internal set; }
        public bool ExpiresAtShopEnd { get; }
        private readonly List<PendingCombatModifier> pendingCombatModifiers =
            new List<PendingCombatModifier>();
        public IReadOnlyList<PendingCombatModifier> PendingCombatModifiers =>
            pendingCombatModifiers;
        public bool HasPermanentShield => HasEffectiveKeyword("Shield");
        public bool HasPendingCombatShield => pendingCombatModifiers.Any(modifier =>
            modifier.AddShield || modifier.Keyword == "Shield");
        public int PoolCopiesHeld => IsGolden ? 3 : 1;
        public int CurrentAttack => CardType == ShopCardType.Minion
            ? (IsGolden ? Minion.GoldenAttack : Minion.Attack) +
              PermanentAttackBonus + FlourishAttackBonus
            : 0;
        public int CurrentHealth => CardType == ShopCardType.Minion
            ? (IsGolden ? Minion.GoldenHealth : Minion.Health) + PermanentHealthBonus
            : 0;

        public static ShopCardInstance CreateMinion(
            string instanceId,
            MinionConfig minion,
            bool isGolden = false,
            int permanentAttackBonus = 0,
            int permanentHealthBonus = 0,
            IEnumerable<string> permanentKeywords = null,
            bool tripleDiscoveryPending = false)
        {
            return new ShopCardInstance(
                instanceId,
                ShopCardType.Minion,
                minion ?? throw new ArgumentNullException(nameof(minion)),
                null,
                isGolden,
                permanentAttackBonus,
                permanentHealthBonus,
                permanentKeywords,
                tripleDiscoveryPending,
                false);
        }

        public static ShopCardInstance CreateSpell(
            string instanceId,
            SpellConfig spell,
            bool expiresAtShopEnd = false)
        {
            return new ShopCardInstance(
                instanceId,
                ShopCardType.Spell,
                null,
                spell ?? throw new ArgumentNullException(nameof(spell)),
                false,
                0,
                0,
                null,
                false,
                expiresAtShopEnd);
        }

        internal void ApplyPermanentStats(int attack, int health)
        {
            if (CardType != ShopCardType.Minion)
            {
                throw new InvalidOperationException("Only minions can receive permanent stats.");
            }

            PermanentAttackBonus += attack;
            PermanentHealthBonus += health;
        }

        internal void SetFlourishAttackBonus(int amount)
        {
            FlourishAttackBonus = CardType == ShopCardType.Minion
                ? Math.Max(0, amount)
                : 0;
        }

        internal bool HasEffectiveKeyword(string keyword)
        {
            return !string.IsNullOrWhiteSpace(keyword) &&
                   ((Minion?.Keywords?.Contains(keyword) ?? false) ||
                    PermanentKeywords.Contains(keyword));
        }

        internal bool TryGrantPermanentKeyword(string keyword)
        {
            if (CardType != ShopCardType.Minion ||
                string.IsNullOrWhiteSpace(keyword) ||
                HasEffectiveKeyword(keyword))
            {
                return false;
            }

            permanentKeywordsSet.Add(keyword);
            return true;
        }

        internal void AddPendingCombatModifier(PendingCombatModifier modifier)
        {
            if (CardType != ShopCardType.Minion || modifier == null)
            {
                return;
            }

            pendingCombatModifiers.Add(modifier);
        }

        internal IReadOnlyList<PendingCombatModifier> ConsumePendingCombatModifiers()
        {
            var consumed = pendingCombatModifiers.ToArray();
            pendingCombatModifiers.Clear();
            return consumed;
        }

        internal static ShopCardInstance Restore(
            string instanceId,
            ShopCardType cardType,
            MinionConfig minion,
            SpellConfig spell,
            bool isGolden,
            int permanentAttackBonus,
            int permanentHealthBonus,
            int flourishAttackBonus,
            IEnumerable<string> permanentKeywords,
            bool tripleDiscoveryPending,
            bool expiresAtShopEnd,
            IEnumerable<PendingCombatModifier> pendingModifiers)
        {
            var restored = new ShopCardInstance(
                instanceId,
                cardType,
                minion,
                spell,
                isGolden,
                permanentAttackBonus,
                permanentHealthBonus,
                permanentKeywords,
                tripleDiscoveryPending,
                expiresAtShopEnd);
            restored.SetFlourishAttackBonus(flourishAttackBonus);
            restored.pendingCombatModifiers.AddRange(
                pendingModifiers ?? Array.Empty<PendingCombatModifier>());
            return restored;
        }
    }

    public sealed class ShopEventData
    {
        public ShopEventData(
            ShopEventType type,
            ShopCardInstance card = null,
            int cost = 0,
            int refreshCount = 0,
            int previousTavernTier = 0,
            int tavernTier = 0,
            ShopCardInstance targetCard = null,
            int gold = 0,
            int freeRefreshes = 0)
        {
            Type = type;
            Card = card;
            Cost = cost;
            RefreshCount = refreshCount;
            PreviousTavernTier = previousTavernTier;
            TavernTier = tavernTier;
            TargetCard = targetCard;
            Gold = gold;
            FreeRefreshes = freeRefreshes;
        }

        public ShopEventType Type { get; }
        public ShopCardInstance Card { get; }
        public int Cost { get; }
        public int RefreshCount { get; }
        public int PreviousTavernTier { get; }
        public int TavernTier { get; }
        public ShopCardInstance TargetCard { get; }
        public int Gold { get; }
        public int FreeRefreshes { get; }
    }

    public sealed class ShopDiscoverState
    {
        internal ShopDiscoverState(
            ShopCardInstance sourceSpell,
            int benchIndex,
            IEnumerable<MinionConfig> candidates,
            bool canCancel)
        {
            SourceSpell = sourceSpell ??
                throw new ArgumentNullException(nameof(sourceSpell));
            BenchIndex = benchIndex;
            Candidates = new List<MinionConfig>(
                candidates ?? throw new ArgumentNullException(nameof(candidates)))
                .AsReadOnly();
            CanCancel = canCancel;
        }

        public ShopCardInstance SourceSpell { get; }
        public int BenchIndex { get; }
        public IReadOnlyList<MinionConfig> Candidates { get; }
        public bool CanCancel { get; }
    }

    public sealed class PlayerCollection
    {
        private readonly ShopCardInstance[] bench =
            new ShopCardInstance[ShopEconomyRules.BenchSlotCount];
        private readonly ShopCardInstance[] battle =
            new ShopCardInstance[ShopEconomyRules.BattleSlotCount];

        public IReadOnlyList<ShopCardInstance> Bench => bench;
        public IReadOnlyList<ShopCardInstance> Battle => battle;

        public bool TryAddToBench(ShopCardInstance card, out int benchIndex)
        {
            if (card == null)
            {
                benchIndex = -1;
                return false;
            }

            for (var i = 0; i < bench.Length; i++)
            {
                if (bench[i] != null)
                {
                    continue;
                }

                bench[i] = card;
                benchIndex = i;
                return true;
            }

            benchIndex = -1;
            return false;
        }

        internal ShopCardInstance RemoveSellableMinionFromBattle(int index)
        {
            if (!IsValidIndex(index, battle.Length))
            {
                return null;
            }

            var card = battle[index];
            if (!IsSellableMinion(card))
            {
                return null;
            }

            battle[index] = null;
            return card;
        }

        internal bool PlaceBenchMinionInBattle(int benchIndex, int battleIndex)
        {
            if (!IsValidIndex(benchIndex, bench.Length) ||
                !IsValidIndex(battleIndex, battle.Length))
            {
                return false;
            }

            var source = bench[benchIndex];
            if (source == null || source.CardType != ShopCardType.Minion ||
                battle[battleIndex] != null)
            {
                return false;
            }

            bench[benchIndex] = null;
            battle[battleIndex] = source;
            return true;
        }

        internal bool RepositionBattleMinion(int sourceIndex, int targetIndex)
        {
            if (!IsValidIndex(sourceIndex, battle.Length) ||
                !IsValidIndex(targetIndex, battle.Length) ||
                battle[sourceIndex] == null)
            {
                return false;
            }

            if (sourceIndex == targetIndex)
            {
                return true;
            }

            var target = battle[targetIndex];
            battle[targetIndex] = battle[sourceIndex];
            battle[sourceIndex] = target;
            return true;
        }

        internal ShopCardInstance RemoveUsedSpellFromBench(int index)
        {
            if (!IsValidIndex(index, bench.Length) ||
                bench[index] == null ||
                bench[index].CardType != ShopCardType.Spell)
            {
                return null;
            }

            var spell = bench[index];
            bench[index] = null;
            return spell;
        }

        internal IReadOnlyList<ShopCardInstance> RemoveTemporarySpells()
        {
            var removed = new List<ShopCardInstance>();
            for (var i = 0; i < bench.Length; i++)
            {
                var card = bench[i];
                if (card == null || card.CardType != ShopCardType.Spell ||
                    !card.ExpiresAtShopEnd)
                {
                    continue;
                }

                removed.Add(card);
                bench[i] = null;
            }

            return removed;
        }

        internal int EmptyBenchSlotCount()
        {
            var count = 0;
            for (var i = 0; i < bench.Length; i++)
            {
                if (bench[i] == null)
                {
                    count++;
                }
            }

            return count;
        }

        internal bool RemoveTripleMaterials(
            IReadOnlyCollection<ShopCardInstance> materials)
        {
            if (materials == null || materials.Count != 3)
            {
                return false;
            }

            var unique = new HashSet<ShopCardInstance>(materials);
            if (unique.Count != 3)
            {
                return false;
            }

            var owned = new HashSet<ShopCardInstance>();
            for (var i = 0; i < bench.Length; i++)
            {
                if (bench[i] != null)
                {
                    owned.Add(bench[i]);
                }
            }

            for (var i = 0; i < battle.Length; i++)
            {
                if (battle[i] != null)
                {
                    owned.Add(battle[i]);
                }
            }

            if (unique.Any(card => !owned.Contains(card)))
            {
                return false;
            }

            for (var i = 0; i < bench.Length; i++)
            {
                if (bench[i] != null && unique.Contains(bench[i]))
                {
                    bench[i] = null;
                }
            }

            for (var i = 0; i < battle.Length; i++)
            {
                if (battle[i] != null && unique.Contains(battle[i]))
                {
                    battle[i] = null;
                }
            }

            return true;
        }

        internal bool ReplaceBenchCard(
            int index,
            ShopCardInstance expected,
            ShopCardInstance replacement)
        {
            if (!IsValidIndex(index, bench.Length) ||
                expected == null || replacement == null ||
                !ReferenceEquals(bench[index], expected))
            {
                return false;
            }

            bench[index] = replacement;
            return true;
        }

        private static bool IsValidIndex(int index, int count)
        {
            return index >= 0 && index < count;
        }

        private static bool IsSellableMinion(ShopCardInstance card)
        {
            return card != null &&
                card.CardType == ShopCardType.Minion &&
                !card.Minion.IsToken;
        }

        internal void RestoreSlots(
            IReadOnlyList<ShopCardInstance> restoredBench,
            IReadOnlyList<ShopCardInstance> restoredBattle)
        {
            if (restoredBench == null || restoredBench.Count != bench.Length ||
                restoredBattle == null || restoredBattle.Count != battle.Length)
            {
                throw new InvalidOperationException("Player collection snapshot has invalid slots.");
            }

            for (var index = 0; index < bench.Length; index++)
            {
                bench[index] = restoredBench[index];
            }

            for (var index = 0; index < battle.Length; index++)
            {
                battle[index] = restoredBattle[index];
            }
        }
    }
}

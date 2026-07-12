using System;
using System.Collections.Generic;
using System.Linq;
using SpireChess.Battle;
using SpireChess.Config;

namespace SpireChess.Shop
{
    public sealed class ShopSession
    {
        public const string TripleDiscoveryRewardSpellId =
            "triple_discovery_reward";

        private readonly Random random;
        private readonly List<SpellConfig> spells;
        private readonly SpellConfig tripleDiscoveryRewardSpell;
        private readonly List<MinionConfig> minionOffers = new List<MinionConfig>();
        private int cardInstanceSequence;
        private int roundsWithoutUpgradeAtCurrentTier;
        private int pendingUpgradeDiscount;

        public ShopSession(
            IEnumerable<MinionConfig> minions,
            IEnumerable<SpellConfig> spells,
            Random random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            MinionPool = new MinionPool(minions);
            var enabledSpells = (spells ?? throw new ArgumentNullException(nameof(spells)))
                .Where(spell => spell != null && spell.Enabled)
                .ToList();
            this.spells = enabledSpells
                .Where(spell => spell.ShopEligible)
                .ToList();
            tripleDiscoveryRewardSpell = enabledSpells.FirstOrDefault(
                spell => spell.Id == TripleDiscoveryRewardSpellId);
            Collection = new PlayerCollection();
            TavernTier = 1;
            EnsureMinionOfferCapacity();
        }

        public event Action<ShopEventData> EventRaised;

        public int Round { get; private set; }
        public int LastEconomyTurn => Round;
        public int Gold { get; private set; }
        public int TavernTier { get; private set; }
        public int RefreshCount { get; private set; }
        public int FreeRefreshes { get; private set; }
        public bool IsShopOpen { get; private set; }
        public bool IsFrozen { get; private set; }
        public bool UpgradedThisRound { get; private set; }
        public IReadOnlyList<MinionConfig> MinionOffers => minionOffers;
        public SpellConfig SpellOffer { get; private set; }
        public MinionPool MinionPool { get; }
        public PlayerCollection Collection { get; }
        public ShopDiscoverState PendingDiscover { get; private set; }

        public int CurrentUpgradeCost
        {
            get
            {
                if (TavernTier >= ShopEconomyRules.MaximumTavernTier)
                {
                    return 0;
                }

                return Math.Max(
                    1,
                    ShopEconomyRules.GetUpgradeBaseCost(TavernTier) -
                    roundsWithoutUpgradeAtCurrentTier -
                    pendingUpgradeDiscount);
            }
        }

        public ShopOperationResult StartNextRound()
        {
            return StartRound(Round + 1);
        }

        public ShopOperationResult StartRound(int runTurn)
        {
            if (runTurn < 1)
            {
                return ShopOperationResult.Fail(ShopOperationError.InvalidTiming);
            }

            if (runTurn == Round && IsShopOpen)
            {
                return ShopOperationResult.Succeed();
            }

            if (IsShopOpen)
            {
                return ShopOperationResult.Fail(ShopOperationError.ShopAlreadyOpen);
            }

            if (runTurn <= Round || runTurn != Round + 1)
            {
                return ShopOperationResult.Fail(ShopOperationError.InvalidTiming);
            }

            Round = runTurn;
            Gold = ShopEconomyRules.GetRoundBudget(Round);
            RefreshCount = 0;
            FreeRefreshes = 0;
            UpgradedThisRound = false;
            IsShopOpen = true;

            EnsureMinionOfferCapacity();
            FillEmptyOffers();
            IsFrozen = false;
            var triples = ResolveAllTriples();
            RaiseEvent(new ShopEventData(
                ShopEventType.OnShopPhaseStart,
                tavernTier: TavernTier));
            RaiseTripleEvents(triples);
            return ShopOperationResult.Succeed();
        }

        public ShopOperationResult AdvanceSkippedRound(int runTurn)
        {
            if (IsShopOpen || runTurn < 1)
            {
                return ShopOperationResult.Fail(ShopOperationError.InvalidTiming);
            }

            if (runTurn == Round)
            {
                return ShopOperationResult.Succeed();
            }

            if (runTurn != Round + 1)
            {
                return ShopOperationResult.Fail(ShopOperationError.InvalidTiming);
            }

            Round = runTurn;
            if (TavernTier < ShopEconomyRules.MaximumTavernTier)
            {
                roundsWithoutUpgradeAtCurrentTier++;
            }

            return ShopOperationResult.Succeed();
        }

        public ShopOperationResult EndRound()
        {
            if (!IsShopOpen)
            {
                return ShopOperationResult.Fail(ShopOperationError.ShopClosed);
            }

            if (PendingDiscover != null)
            {
                return ShopOperationResult.Fail(ShopOperationError.DiscoveryPending);
            }

            if (!UpgradedThisRound && TavernTier < ShopEconomyRules.MaximumTavernTier)
            {
                roundsWithoutUpgradeAtCurrentTier++;
            }

            if (!IsFrozen)
            {
                ReturnAndClearMinionOffers();
                SpellOffer = null;
            }

            Gold = 0;
            FreeRefreshes = 0;
            IsShopOpen = false;
            RaiseEvent(new ShopEventData(
                ShopEventType.OnShopPhaseEnd,
                refreshCount: RefreshCount,
                tavernTier: TavernTier));
            return ShopOperationResult.Succeed();
        }

        public ShopOperationResult Refresh()
        {
            if (!IsShopOpen)
            {
                return ShopOperationResult.Fail(ShopOperationError.ShopClosed);
            }

            if (PendingDiscover != null)
            {
                return ShopOperationResult.Fail(ShopOperationError.DiscoveryPending);
            }

            var cost = FreeRefreshes > 0 ? 0 : ShopEconomyRules.RefreshCost;
            if (Gold < cost)
            {
                return ShopOperationResult.Fail(ShopOperationError.InsufficientGold);
            }

            if (FreeRefreshes > 0)
            {
                FreeRefreshes--;
            }
            else
            {
                Gold -= cost;
            }

            ReturnAndClearMinionOffers();
            SpellOffer = null;
            FillEmptyOffers();
            IsFrozen = false;
            RefreshCount++;
            RaiseEvent(new ShopEventData(
                ShopEventType.OnRefresh,
                cost: cost,
                refreshCount: RefreshCount,
                tavernTier: TavernTier));
            return ShopOperationResult.Succeed();
        }

        public ShopOperationResult BuyMinion(int offerIndex)
        {
            if (!IsShopOpen)
            {
                return ShopOperationResult.Fail(ShopOperationError.ShopClosed);
            }

            if (PendingDiscover != null)
            {
                return ShopOperationResult.Fail(ShopOperationError.DiscoveryPending);
            }

            if (offerIndex < 0 || offerIndex >= minionOffers.Count)
            {
                return ShopOperationResult.Fail(ShopOperationError.InvalidIndex);
            }

            var config = minionOffers[offerIndex];
            if (config == null)
            {
                return ShopOperationResult.Fail(ShopOperationError.EmptySlot);
            }

            if (!HasBenchSpace())
            {
                return ShopOperationResult.Fail(ShopOperationError.BenchFull);
            }

            if (Gold < ShopEconomyRules.MinionPurchaseCost)
            {
                return ShopOperationResult.Fail(ShopOperationError.InsufficientGold);
            }

            var card = ShopCardInstance.CreateMinion(NextCardInstanceId(), config);
            Collection.TryAddToBench(card, out var benchIndex);
            Gold -= ShopEconomyRules.MinionPurchaseCost;
            minionOffers[offerIndex] = null;
            var triples = ResolveAllTriples();
            RaiseEvent(new ShopEventData(
                ShopEventType.OnBuy,
                card,
                ShopEconomyRules.MinionPurchaseCost,
                RefreshCount,
                tavernTier: TavernTier));
            RaiseTripleEvents(triples);
            return ShopOperationResult.Succeed(benchIndex);
        }

        public ShopOperationResult BuySpell()
        {
            if (!IsShopOpen)
            {
                return ShopOperationResult.Fail(ShopOperationError.ShopClosed);
            }

            if (PendingDiscover != null)
            {
                return ShopOperationResult.Fail(ShopOperationError.DiscoveryPending);
            }

            if (SpellOffer == null)
            {
                return ShopOperationResult.Fail(ShopOperationError.EmptySlot);
            }

            if (!HasBenchSpace())
            {
                return ShopOperationResult.Fail(ShopOperationError.BenchFull);
            }

            if (Gold < ShopEconomyRules.SpellPurchaseCost)
            {
                return ShopOperationResult.Fail(ShopOperationError.InsufficientGold);
            }

            var card = ShopCardInstance.CreateSpell(NextCardInstanceId(), SpellOffer);
            Collection.TryAddToBench(card, out var benchIndex);
            Gold -= ShopEconomyRules.SpellPurchaseCost;
            SpellOffer = null;
            RaiseEvent(new ShopEventData(
                ShopEventType.OnBuy,
                card,
                ShopEconomyRules.SpellPurchaseCost,
                RefreshCount,
                tavernTier: TavernTier));
            return ShopOperationResult.Succeed(benchIndex);
        }

        public ShopOperationResult SellBenchMinion(int benchIndex)
        {
            if (!IsShopOpen)
            {
                return ShopOperationResult.Fail(ShopOperationError.ShopClosed);
            }

            if (PendingDiscover != null)
            {
                return ShopOperationResult.Fail(ShopOperationError.DiscoveryPending);
            }

            if (benchIndex < 0 || benchIndex >= Collection.Bench.Count)
            {
                return ShopOperationResult.Fail(ShopOperationError.InvalidIndex);
            }

            var card = Collection.Bench[benchIndex];
            if (card == null)
            {
                return ShopOperationResult.Fail(ShopOperationError.EmptySlot);
            }

            return ShopOperationResult.Fail(ShopOperationError.InvalidCardLocation);
        }

        public ShopOperationResult SellBattleMinion(int battleIndex)
        {
            if (!IsShopOpen)
            {
                return ShopOperationResult.Fail(ShopOperationError.ShopClosed);
            }

            if (PendingDiscover != null)
            {
                return ShopOperationResult.Fail(ShopOperationError.DiscoveryPending);
            }

            if (battleIndex < 0 || battleIndex >= Collection.Battle.Count)
            {
                return ShopOperationResult.Fail(ShopOperationError.InvalidIndex);
            }

            var card = Collection.Battle[battleIndex];
            if (card == null)
            {
                return ShopOperationResult.Fail(ShopOperationError.EmptySlot);
            }

            if (card.CardType != ShopCardType.Minion || card.Minion.IsToken)
            {
                return ShopOperationResult.Fail(ShopOperationError.InvalidCardType);
            }

            Collection.RemoveSellableMinionFromBattle(battleIndex);
            ResolveMinionSale(card);
            return ShopOperationResult.Succeed();
        }

        public ShopOperationResult PlayMinion(
            int benchIndex,
            int battleIndex,
            int effectTargetBattleIndex = -1)
        {
            if (!IsShopOpen)
            {
                return ShopOperationResult.Fail(ShopOperationError.ShopClosed);
            }

            if (PendingDiscover != null)
            {
                return ShopOperationResult.Fail(ShopOperationError.DiscoveryPending);
            }

            if (benchIndex < 0 || benchIndex >= Collection.Bench.Count ||
                battleIndex < 0 || battleIndex >= Collection.Battle.Count)
            {
                return ShopOperationResult.Fail(ShopOperationError.InvalidIndex);
            }

            var card = Collection.Bench[benchIndex];
            if (card == null)
            {
                return ShopOperationResult.Fail(ShopOperationError.EmptySlot);
            }

            if (card.CardType != ShopCardType.Minion)
            {
                return ShopOperationResult.Fail(ShopOperationError.InvalidCardType);
            }

            if (Collection.Battle[battleIndex] != null)
            {
                return ShopOperationResult.Fail(ShopOperationError.OccupiedBattleSlot);
            }

            var grantsTripleReward = card.TripleDiscoveryPending;
            if (grantsTripleReward && !IsValidTripleDiscoveryRewardSpell())
            {
                return ShopOperationResult.Fail(ShopOperationError.UnsupportedEffect);
            }

            var effects = GetTriggeredEffects(card, "OnPlay");
            if (!TryBuildEffectPlan(
                    effects,
                    card,
                    battleIndex,
                    effectTargetBattleIndex,
                    false,
                    out var plan,
                    out var error))
            {
                return ShopOperationResult.Fail(error);
            }

            Collection.PlaceBenchMinionInBattle(benchIndex, battleIndex);
            ApplyEffectPlan(plan);
            ShopCardInstance rewardCard = null;
            if (grantsTripleReward)
            {
                rewardCard = ShopCardInstance.CreateSpell(
                    NextCardInstanceId(),
                    tripleDiscoveryRewardSpell);
                if (!Collection.TryAddToBench(rewardCard, out _))
                {
                    throw new InvalidOperationException(
                        "A validated triple reward could not enter the bench.");
                }

                card.TripleDiscoveryPending = false;
            }

            var triples = ResolveAllTriples();
            RaiseEvent(new ShopEventData(
                ShopEventType.OnPlay,
                card,
                tavernTier: TavernTier,
                targetCard: GetFirstTarget(plan)));
            if (rewardCard != null)
            {
                RaiseEvent(new ShopEventData(
                    ShopEventType.OnTripleRewardGranted,
                    rewardCard,
                    tavernTier: TavernTier,
                    targetCard: card));
            }

            RaiseTripleEvents(triples);
            return ShopOperationResult.Succeed();
        }

        public ShopOperationResult RepositionBattleMinion(int sourceIndex, int targetIndex)
        {
            if (!IsShopOpen)
            {
                return ShopOperationResult.Fail(ShopOperationError.ShopClosed);
            }

            if (PendingDiscover != null)
            {
                return ShopOperationResult.Fail(ShopOperationError.DiscoveryPending);
            }

            if (sourceIndex < 0 || sourceIndex >= Collection.Battle.Count ||
                targetIndex < 0 || targetIndex >= Collection.Battle.Count)
            {
                return ShopOperationResult.Fail(ShopOperationError.InvalidIndex);
            }

            if (Collection.Battle[sourceIndex] == null)
            {
                return ShopOperationResult.Fail(ShopOperationError.EmptySlot);
            }

            Collection.RepositionBattleMinion(sourceIndex, targetIndex);
            return ShopOperationResult.Succeed();
        }

        public ShopOperationResult UseSpell(int benchIndex, int targetBattleIndex = -1)
        {
            if (!IsShopOpen)
            {
                return ShopOperationResult.Fail(ShopOperationError.ShopClosed);
            }

            if (PendingDiscover != null)
            {
                return ShopOperationResult.Fail(ShopOperationError.DiscoveryPending);
            }

            if (benchIndex < 0 || benchIndex >= Collection.Bench.Count)
            {
                return ShopOperationResult.Fail(ShopOperationError.InvalidIndex);
            }

            var card = Collection.Bench[benchIndex];
            if (card == null)
            {
                return ShopOperationResult.Fail(ShopOperationError.EmptySlot);
            }

            if (card.CardType != ShopCardType.Spell)
            {
                return ShopOperationResult.Fail(ShopOperationError.InvalidCardType);
            }

            if (card.Spell.UseTiming == null || !card.Spell.UseTiming.Contains("Shop"))
            {
                return ShopOperationResult.Fail(ShopOperationError.InvalidTiming);
            }

            var effects = GetTriggeredEffects(card, "Manual");
            if (effects.Count == 0)
            {
                return ShopOperationResult.Fail(ShopOperationError.NoBenefit);
            }

            var discoverEffects = effects
                .Where(effect => effect.Action == "DiscoverMinion")
                .ToList();
            if (discoverEffects.Count > 0)
            {
                if (discoverEffects.Count != 1 || effects.Count != 1)
                {
                    return ShopOperationResult.Fail(ShopOperationError.UnsupportedEffect);
                }

                return BeginDiscover(benchIndex, card, discoverEffects[0]);
            }

            if (!TryBuildEffectPlan(
                    effects,
                    null,
                    -1,
                    targetBattleIndex,
                    true,
                    out var plan,
                    out var error))
            {
                return ShopOperationResult.Fail(error);
            }

            Collection.RemoveUsedSpellFromBench(benchIndex);
            ApplyEffectPlan(plan);
            var triples = ResolveAllTriples();
            RaiseEvent(new ShopEventData(
                ShopEventType.OnSpellUsed,
                card,
                tavernTier: TavernTier,
                targetCard: GetFirstTarget(plan)));
            RaiseTripleEvents(triples);
            return ShopOperationResult.Succeed();
        }

        public ShopOperationResult SelectDiscover(int candidateIndex)
        {
            if (!IsShopOpen)
            {
                return ShopOperationResult.Fail(ShopOperationError.ShopClosed);
            }

            if (PendingDiscover == null)
            {
                return ShopOperationResult.Fail(ShopOperationError.NoDiscoveryPending);
            }

            if (candidateIndex < 0 ||
                candidateIndex >= PendingDiscover.Candidates.Count)
            {
                return ShopOperationResult.Fail(ShopOperationError.InvalidIndex);
            }

            var state = PendingDiscover;
            var selectedConfig = state.Candidates[candidateIndex];
            var selectedCard = ShopCardInstance.CreateMinion(
                NextCardInstanceId(),
                selectedConfig);
            if (!Collection.ReplaceBenchCard(
                    state.BenchIndex,
                    state.SourceSpell,
                    selectedCard))
            {
                return ShopOperationResult.Fail(
                    ShopOperationError.InvalidCardLocation);
            }

            for (var i = 0; i < state.Candidates.Count; i++)
            {
                if (i != candidateIndex)
                {
                    MinionPool.Return(state.Candidates[i].Id);
                }
            }

            PendingDiscover = null;
            var triples = ResolveAllTriples();
            RaiseEvent(new ShopEventData(
                ShopEventType.OnSpellUsed,
                state.SourceSpell,
                tavernTier: TavernTier,
                targetCard: selectedCard));
            RaiseEvent(new ShopEventData(
                ShopEventType.OnDiscoverResolved,
                state.SourceSpell,
                tavernTier: TavernTier,
                targetCard: selectedCard));
            RaiseTripleEvents(triples);
            return ShopOperationResult.Succeed(state.BenchIndex);
        }

        public ShopOperationResult CancelDiscover()
        {
            if (!IsShopOpen)
            {
                return ShopOperationResult.Fail(ShopOperationError.ShopClosed);
            }

            if (PendingDiscover == null)
            {
                return ShopOperationResult.Fail(ShopOperationError.NoDiscoveryPending);
            }

            var state = PendingDiscover;
            foreach (var candidate in state.Candidates)
            {
                MinionPool.Return(candidate.Id);
            }

            PendingDiscover = null;
            RaiseEvent(new ShopEventData(
                ShopEventType.OnDiscoverCancelled,
                state.SourceSpell,
                tavernTier: TavernTier));
            return ShopOperationResult.Succeed(state.BenchIndex);
        }

        public BattleBoardState CreateBattleSnapshot()
        {
            var state = new BattleBoardState();
            for (var i = 0; i < Collection.Battle.Count; i++)
            {
                var card = Collection.Battle[i];
                if (card == null || card.CardType != ShopCardType.Minion)
                {
                    continue;
                }

                state.Player[i] = new BattleMinionRuntime(
                    card.Minion,
                    card.IsGolden,
                    card.CurrentAttack,
                    card.CurrentHealth,
                    card.InstanceId,
                    card.PermanentAttackBonus,
                    card.PermanentHealthBonus,
                    card.PermanentKeywords);
            }

            return state;
        }

        public ShopOperationResult UpgradeTavern()
        {
            if (!IsShopOpen)
            {
                return ShopOperationResult.Fail(ShopOperationError.ShopClosed);
            }

            if (PendingDiscover != null)
            {
                return ShopOperationResult.Fail(ShopOperationError.DiscoveryPending);
            }

            if (TavernTier >= ShopEconomyRules.MaximumTavernTier)
            {
                return ShopOperationResult.Fail(ShopOperationError.MaximumTavernTier);
            }

            if (UpgradedThisRound)
            {
                return ShopOperationResult.Fail(ShopOperationError.AlreadyUpgradedThisRound);
            }

            var cost = CurrentUpgradeCost;
            if (Gold < cost)
            {
                return ShopOperationResult.Fail(ShopOperationError.InsufficientGold);
            }

            var previousTier = TavernTier;
            Gold -= cost;
            TavernTier++;
            roundsWithoutUpgradeAtCurrentTier = 0;
            pendingUpgradeDiscount = 0;
            UpgradedThisRound = true;
            EnsureMinionOfferCapacity();
            RaiseEvent(new ShopEventData(
                ShopEventType.OnTavernUpgraded,
                cost: cost,
                previousTavernTier: previousTier,
                tavernTier: TavernTier));
            return ShopOperationResult.Succeed();
        }

        public ShopOperationResult ToggleFreeze()
        {
            if (!IsShopOpen)
            {
                return ShopOperationResult.Fail(ShopOperationError.ShopClosed);
            }

            if (PendingDiscover != null)
            {
                return ShopOperationResult.Fail(ShopOperationError.DiscoveryPending);
            }

            IsFrozen = !IsFrozen;
            return ShopOperationResult.Succeed();
        }

        public void GrantGold(int amount)
        {
            if (amount > 0)
            {
                Gold += amount;
            }
        }

        public void GrantFreeRefreshes(int amount)
        {
            if (amount > 0)
            {
                FreeRefreshes += amount;
            }
        }

        public void GrantUpgradeDiscount(int amount)
        {
            if (amount > 0)
            {
                pendingUpgradeDiscount += amount;
            }
        }

        public ShopOperationResult ClaimRewardMinion(MinionConfig config)
        {
            if (!IsShopOpen)
            {
                return ShopOperationResult.Fail(ShopOperationError.ShopClosed);
            }

            if (config == null || config.IsToken || !config.Enabled)
            {
                return ShopOperationResult.Fail(ShopOperationError.InvalidCardType);
            }

            if (Collection.EmptyBenchSlotCount() <= 0)
            {
                return ShopOperationResult.Fail(ShopOperationError.BenchFull);
            }

            var card = ShopCardInstance.CreateMinion(NextCardInstanceId(), config);
            Collection.TryAddToBench(card, out var benchIndex);
            var triples = ResolveAllTriples();
            RaiseTripleEvents(triples);
            return ShopOperationResult.Succeed(benchIndex);
        }

        public ShopOperationResult ClaimRewardSpell(SpellConfig config)
        {
            if (!IsShopOpen)
            {
                return ShopOperationResult.Fail(ShopOperationError.ShopClosed);
            }

            if (config == null || !config.Enabled ||
                config.Effects == null || config.Effects.Count == 0)
            {
                return ShopOperationResult.Fail(ShopOperationError.InvalidCardType);
            }

            if (Collection.EmptyBenchSlotCount() <= 0)
            {
                return ShopOperationResult.Fail(ShopOperationError.BenchFull);
            }

            var card = ShopCardInstance.CreateSpell(NextCardInstanceId(), config);
            Collection.TryAddToBench(card, out var benchIndex);
            return ShopOperationResult.Succeed(benchIndex);
        }

        public ShopOperationResult ModifyOwnedBattleMinion(
            string instanceId,
            int attack,
            int health,
            string keyword = null)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return ShopOperationResult.Fail(ShopOperationError.InvalidTarget);
            }

            var target = Collection.Battle.FirstOrDefault(
                card => card != null && card.InstanceId == instanceId);
            if (target == null || target.CardType != ShopCardType.Minion)
            {
                return ShopOperationResult.Fail(ShopOperationError.InvalidTarget);
            }

            if (attack == 0 && health == 0 && string.IsNullOrWhiteSpace(keyword))
            {
                return ShopOperationResult.Fail(ShopOperationError.NoBenefit);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                if (keyword != "Shield" && keyword != "Taunt")
                {
                    return ShopOperationResult.Fail(ShopOperationError.UnsupportedEffect);
                }

                if (!target.TryGrantPermanentKeyword(keyword))
                {
                    return ShopOperationResult.Fail(ShopOperationError.NoBenefit);
                }
            }

            if (attack != 0 || health != 0)
            {
                target.ApplyPermanentStats(attack, health);
            }

            return ShopOperationResult.Succeed();
        }

        private void ResolveMinionSale(ShopCardInstance card)
        {
            Gold += ShopEconomyRules.MinionSellValue;
            MinionPool.ReturnCopies(card.ConfigId, card.PoolCopiesHeld);
            RaiseEvent(new ShopEventData(
                ShopEventType.OnSell,
                card,
                cost: -ShopEconomyRules.MinionSellValue,
                refreshCount: RefreshCount,
                tavernTier: TavernTier));
        }

        private ShopOperationResult BeginDiscover(
            int benchIndex,
            ShopCardInstance spell,
            EffectConfig effect)
        {
            if (!IsSupportedTripleDiscoverEffect(effect))
            {
                return ShopOperationResult.Fail(ShopOperationError.UnsupportedEffect);
            }

            var candidates = MinionPool.ReserveDistinctAtTier(
                TavernTier,
                effect.Discover.Count,
                random);
            if (candidates.Count == 0)
            {
                return ShopOperationResult.Fail(ShopOperationError.NoBenefit);
            }

            PendingDiscover = new ShopDiscoverState(
                spell,
                benchIndex,
                candidates);
            RaiseEvent(new ShopEventData(
                ShopEventType.OnDiscoverStarted,
                spell,
                tavernTier: TavernTier));
            return ShopOperationResult.Succeed(benchIndex);
        }

        private bool IsValidTripleDiscoveryRewardSpell()
        {
            if (tripleDiscoveryRewardSpell == null ||
                tripleDiscoveryRewardSpell.ShopEligible ||
                tripleDiscoveryRewardSpell.UseTiming == null ||
                !tripleDiscoveryRewardSpell.UseTiming.Contains("Shop"))
            {
                return false;
            }

            var effects = (tripleDiscoveryRewardSpell.Effects ??
                    new List<EffectConfig>())
                .Where(effect => effect != null && effect.Trigger == "Manual")
                .ToList();
            return effects.Count == 1 &&
                IsSupportedTripleDiscoverEffect(effects[0]);
        }

        private static bool IsSupportedTripleDiscoverEffect(EffectConfig effect)
        {
            var discover = effect?.Discover;
            return effect?.Action == "DiscoverMinion" &&
                discover != null &&
                discover.CardType == "Minion" &&
                discover.TierMode == "ExactCurrentTavernTier" &&
                discover.Count > 0 &&
                discover.Pick == 1 &&
                !discover.IncludeToken &&
                !discover.IncludeDisabled &&
                !discover.RequireGolden;
        }

        private List<ShopCardInstance> ResolveAllTriples()
        {
            var formed = new List<ShopCardInstance>();
            while (TryResolveNextTriple(out var golden))
            {
                formed.Add(golden);
            }

            return formed;
        }

        private bool TryResolveNextTriple(out ShopCardInstance golden)
        {
            golden = null;
            var orderedMaterials = Collection.Bench
                .Concat(Collection.Battle)
                .Where(IsTripleMaterial)
                .ToList();
            var configIds = orderedMaterials
                .Select(card => card.ConfigId)
                .Distinct()
                .ToList();

            foreach (var configId in configIds)
            {
                var materials = orderedMaterials
                    .Where(card => card.ConfigId == configId)
                    .Take(3)
                    .ToList();
                if (materials.Count < 3)
                {
                    continue;
                }

                var benchMaterialCount = Collection.Bench.Count(
                    card => card != null && materials.Contains(card));
                if (Collection.EmptyBenchSlotCount() + benchMaterialCount < 1)
                {
                    continue;
                }

                var permanentKeywords = materials
                    .SelectMany(card => card.PermanentKeywords)
                    .Distinct()
                    .ToList();
                golden = ShopCardInstance.CreateMinion(
                    NextCardInstanceId(),
                    materials[0].Minion,
                    isGolden: true,
                    permanentAttackBonus: materials.Sum(
                        card => card.PermanentAttackBonus),
                    permanentHealthBonus: materials.Sum(
                        card => card.PermanentHealthBonus),
                    permanentKeywords: permanentKeywords,
                    tripleDiscoveryPending: true);

                if (!Collection.RemoveTripleMaterials(materials) ||
                    !Collection.TryAddToBench(golden, out _))
                {
                    throw new InvalidOperationException(
                        "A validated triple could not be committed.");
                }

                return true;
            }

            return false;
        }

        private static bool IsTripleMaterial(ShopCardInstance card)
        {
            return card != null &&
                card.CardType == ShopCardType.Minion &&
                !card.IsGolden &&
                !card.Minion.IsToken;
        }

        private void RaiseTripleEvents(IEnumerable<ShopCardInstance> triples)
        {
            foreach (var golden in triples)
            {
                RaiseEvent(new ShopEventData(
                    ShopEventType.OnTripleFormed,
                    golden,
                    tavernTier: TavernTier));
            }
        }

        private void EnsureMinionOfferCapacity()
        {
            var capacity = ShopEconomyRules.GetMinionSlotCount(TavernTier);
            while (minionOffers.Count < capacity)
            {
                minionOffers.Add(null);
            }
        }

        private void FillEmptyOffers()
        {
            for (var i = 0; i < minionOffers.Count; i++)
            {
                if (minionOffers[i] == null)
                {
                    minionOffers[i] = MinionPool.Draw(TavernTier, random);
                }
            }

            if (SpellOffer == null)
            {
                SpellOffer = DrawSpell();
            }
        }

        private SpellConfig DrawSpell()
        {
            var eligible = spells
                .Where(spell => spell.Tier >= 1 && spell.Tier <= TavernTier)
                .ToList();
            return eligible.Count == 0 ? null : eligible[random.Next(eligible.Count)];
        }

        private void ReturnAndClearMinionOffers()
        {
            for (var i = 0; i < minionOffers.Count; i++)
            {
                var offer = minionOffers[i];
                if (offer != null)
                {
                    MinionPool.Return(offer.Id);
                    minionOffers[i] = null;
                }
            }
        }

        private bool HasBenchSpace()
        {
            return Collection.Bench.Any(card => card == null);
        }

        private string NextCardInstanceId()
        {
            cardInstanceSequence++;
            return $"shop_card_{cardInstanceSequence:D6}";
        }

        private void RaiseEvent(ShopEventData eventData)
        {
            EventRaised?.Invoke(eventData);
        }

        private IReadOnlyList<EffectConfig> GetTriggeredEffects(
            ShopCardInstance card,
            string trigger)
        {
            IEnumerable<EffectConfig> effects;
            if (card.CardType == ShopCardType.Spell)
            {
                effects = card.Spell.Effects;
            }
            else
            {
                effects = card.IsGolden
                    ? card.Minion.GoldenEffects
                    : card.Minion.Effects;
            }

            return (effects ?? Enumerable.Empty<EffectConfig>())
                .Where(effect => effect != null && effect.Trigger == trigger)
                .ToList();
        }

        private bool TryBuildEffectPlan(
            IReadOnlyList<EffectConfig> effects,
            ShopCardInstance source,
            int sourceBattleIndex,
            int playerTargetIndex,
            bool requireEveryEffect,
            out List<ResolvedShopEffect> plan,
            out ShopOperationError error)
        {
            plan = new List<ResolvedShopEffect>();
            error = ShopOperationError.None;

            foreach (var effect in effects)
            {
                if (effect.Condition != null &&
                    !string.IsNullOrWhiteSpace(effect.Condition.Type) &&
                    effect.Condition.Type != "None")
                {
                    error = ShopOperationError.UnsupportedEffect;
                    return false;
                }

                var value = effect.Value;
                switch (effect.Action)
                {
                    case "ModifyStats":
                    {
                        if (value == null ||
                            (value.Attack == 0 && value.Health == 0) ||
                            (!string.IsNullOrWhiteSpace(value.Duration) &&
                             value.Duration != "Permanent"))
                        {
                            error = requireEveryEffect
                                ? ShopOperationError.NoBenefit
                                : ShopOperationError.UnsupportedEffect;
                            return false;
                        }

                        if (!TryResolveTargets(
                                effect.Target,
                                source,
                                sourceBattleIndex,
                                playerTargetIndex,
                                out var targets,
                                out error))
                        {
                            return false;
                        }

                        if (targets.Count == 0)
                        {
                            if (requireEveryEffect)
                            {
                                error = ShopOperationError.NoBenefit;
                                return false;
                            }

                            continue;
                        }

                        plan.Add(new ResolvedShopEffect(effect, targets));
                        break;
                    }
                    case "GainGold":
                    case "FreeRefresh":
                        if (value == null || value.Amount <= 0)
                        {
                            error = requireEveryEffect
                                ? ShopOperationError.NoBenefit
                                : ShopOperationError.UnsupportedEffect;
                            return false;
                        }

                        plan.Add(new ResolvedShopEffect(
                            effect,
                            Array.Empty<ShopCardInstance>()));
                        break;
                    default:
                        error = ShopOperationError.UnsupportedEffect;
                        return false;
                }
            }

            if (requireEveryEffect && plan.Count == 0)
            {
                error = ShopOperationError.NoBenefit;
                return false;
            }

            return true;
        }

        private bool TryResolveTargets(
            TargetConfig target,
            ShopCardInstance source,
            int sourceBattleIndex,
            int playerTargetIndex,
            out IReadOnlyList<ShopCardInstance> targets,
            out ShopOperationError error)
        {
            targets = Array.Empty<ShopCardInstance>();
            error = ShopOperationError.None;
            if (target == null ||
                (!string.IsNullOrWhiteSpace(target.Side) && target.Side != "Ally") ||
                (target.Zones != null && target.Zones.Count > 0 &&
                 !target.Zones.Contains("Battle")))
            {
                error = ShopOperationError.UnsupportedEffect;
                return false;
            }

            if (target.Scope == "Self")
            {
                if (source != null)
                {
                    targets = new[] { source };
                }

                return true;
            }

            var candidates = new List<KeyValuePair<int, ShopCardInstance>>();
            for (var i = 0; i < Collection.Battle.Count; i++)
            {
                var card = i == sourceBattleIndex
                    ? source
                    : Collection.Battle[i];
                if (!IsEligibleTarget(card, source, target))
                {
                    continue;
                }

                candidates.Add(new KeyValuePair<int, ShopCardInstance>(i, card));
            }

            if (target.Scope == "All")
            {
                targets = candidates.Select(candidate => candidate.Value).ToList();
                return true;
            }

            if (target.Scope != "Single")
            {
                error = ShopOperationError.UnsupportedEffect;
                return false;
            }

            if (candidates.Count == 0)
            {
                return true;
            }

            switch (target.Selector)
            {
                case "Random":
                    targets = new[] { candidates[random.Next(candidates.Count)].Value };
                    return true;
                case "LowestAttack":
                    targets = new[] { candidates
                        .OrderBy(candidate => candidate.Value.CurrentAttack)
                        .ThenBy(candidate => candidate.Key)
                        .First().Value };
                    return true;
                case "LowestHealth":
                    targets = new[] { candidates
                        .OrderBy(candidate => candidate.Value.CurrentHealth)
                        .ThenBy(candidate => candidate.Key)
                        .First().Value };
                    return true;
                case "PlayerChoice":
                case "None":
                    var selected = candidates.FirstOrDefault(
                        candidate => candidate.Key == playerTargetIndex);
                    if (selected.Value == null)
                    {
                        error = ShopOperationError.InvalidTarget;
                        return false;
                    }

                    targets = new[] { selected.Value };
                    return true;
                default:
                    error = ShopOperationError.UnsupportedEffect;
                    return false;
            }
        }

        private static bool IsEligibleTarget(
            ShopCardInstance card,
            ShopCardInstance source,
            TargetConfig target)
        {
            if (card == null || card.CardType != ShopCardType.Minion)
            {
                return false;
            }

            if (!target.IncludeSelf && source != null && ReferenceEquals(card, source))
            {
                return false;
            }

            if (!target.IncludeToken && card.Minion.IsToken)
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(target.Race) ||
                target.Race == card.Minion.Race;
        }

        private void ApplyEffectPlan(IEnumerable<ResolvedShopEffect> plan)
        {
            foreach (var resolved in plan)
            {
                switch (resolved.Effect.Action)
                {
                    case "ModifyStats":
                        foreach (var target in resolved.Targets)
                        {
                            target.ApplyPermanentStats(
                                resolved.Effect.Value.Attack,
                                resolved.Effect.Value.Health);
                        }
                        break;
                    case "GainGold":
                        Gold += resolved.Effect.Value.Amount;
                        break;
                    case "FreeRefresh":
                        FreeRefreshes += resolved.Effect.Value.Amount;
                        break;
                }
            }
        }

        private static ShopCardInstance GetFirstTarget(
            IEnumerable<ResolvedShopEffect> plan)
        {
            return plan.SelectMany(resolved => resolved.Targets).FirstOrDefault();
        }

        private sealed class ResolvedShopEffect
        {
            public ResolvedShopEffect(
                EffectConfig effect,
                IReadOnlyList<ShopCardInstance> targets)
            {
                Effect = effect;
                Targets = targets;
            }

            public EffectConfig Effect { get; }
            public IReadOnlyList<ShopCardInstance> Targets { get; }
        }
    }
}

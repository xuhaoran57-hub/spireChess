using System;
using System.Collections.Generic;
using System.Linq;
using SpireChess.App;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Run;
using SpireChess.Shop;

namespace SpireChess.Save
{
    public sealed class RunSnapshotException : InvalidOperationException
    {
        public RunSnapshotException(string message)
            : base(message)
        {
        }

        public RunSnapshotException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public sealed class RunSnapshotMapper
    {
        private readonly ConfigService configs;

        public RunSnapshotMapper(ConfigService configs)
        {
            this.configs = configs ?? throw new ArgumentNullException(nameof(configs));
        }

        public RunSavePayloadV1 Capture(RunSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (session.State.Phase == RunPhase.EnteringNode)
            {
                throw new RunSnapshotException("EnteringNode cannot be persisted.");
            }

            return new RunSavePayloadV1
            {
                RunState = CaptureRunState(session.State),
                ShopSession = CaptureShop(session.Shop),
                RandomStreams = new RandomStreamsSnapshotV1
                {
                    Shop = CaptureRandom(session.ShopRandom, "Shop"),
                    Reward = CaptureRandom(session.RewardRandom, "Reward"),
                    Event = CaptureRandom(session.EventRandom, "Event"),
                    Relic = CaptureRandom(session.RelicRandom, "Relic")
                },
                PendingBattle = CaptureContext(session.PendingBattle),
                LastBattleContext = CaptureContext(session.LastBattleContext),
                LastBattleResult = CaptureResult(session.LastBattleResult),
                Sequences = new RunSequenceSnapshotV1
                {
                    AttemptSequence = session.AttemptSequence,
                    RewardSequence = session.RewardSequence,
                    ChoiceSequence = session.ChoiceSequence,
                    RelicChoiceSequence = session.Relics.ChoiceSequence,
                    RelicCandidateSequence = session.Relics.CandidateSequence
                },
                CoreEvidence = CaptureEvidence(session.CoreEvidence),
                TurnTenSnapshotRecorded = session.TurnTenSnapshotRecorded,
                RunEndedRecorded = session.RunEndedRecorded
            };
        }

        public RunSession Restore(RunSavePayloadV1 payload)
        {
            if (payload?.RunState == null || payload.ShopSession == null ||
                payload.RandomStreams == null || payload.Sequences == null)
            {
                throw new RunSnapshotException("Run save payload is incomplete.");
            }

            try
            {
                var mapProvider = new FixedMapProvider(
                    configs.RunMaps,
                    configs.MapRuleProfilesById);
                var stateSnapshot = payload.RunState;
                var map = mapProvider.CreateMap(new MapRequest(
                    stateSnapshot.Seed,
                    stateSnapshot.Floor));
                if (!string.Equals(map.Id, stateSnapshot.MapId, StringComparison.Ordinal))
                {
                    throw new RunSnapshotException(
                        $"Map mismatch: save={stateSnapshot.MapId}, content={map.Id}.");
                }

                var randoms = payload.RandomStreams;
                var shopRandom = RestoreRandom(randoms.Shop, "Shop");
                var rewardRandom = RestoreRandom(randoms.Reward, "Reward");
                var eventRandom = RestoreRandom(randoms.Event, "Event");
                var relicRandom = RestoreRandom(randoms.Relic, "Relic");

                var state = RestoreRunState(stateSnapshot, map);
                var shop = RestoreShop(payload.ShopSession, shopRandom);
                var evidence = RestoreEvidence(payload.CoreEvidence);
                var sequences = payload.Sequences;
                return RunSession.Restore(
                    configs,
                    mapProvider,
                    state,
                    shop,
                    rewardRandom,
                    eventRandom,
                    relicRandom,
                    RestoreContext(payload.PendingBattle),
                    RestoreContext(payload.LastBattleContext),
                    RestoreResult(payload.LastBattleResult),
                    sequences.AttemptSequence,
                    sequences.RewardSequence,
                    sequences.ChoiceSequence,
                    sequences.RelicChoiceSequence,
                    sequences.RelicCandidateSequence,
                    evidence,
                    payload.TurnTenSnapshotRecorded,
                    payload.RunEndedRecorded);
            }
            catch (RunSnapshotException)
            {
                throw;
            }
            catch (RandomReplayException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new RunSnapshotException("Run snapshot restore failed.", exception);
            }
        }

        private RunStateSnapshotV1 CaptureRunState(RunState state)
        {
            return new RunStateSnapshotV1
            {
                Seed = state.Seed,
                Floor = state.Floor,
                ShopTurn = state.ShopTurn,
                MapStep = state.MapStep,
                Health = state.Health,
                MaxHealth = state.MaxHealth,
                Phase = state.Phase,
                MapId = state.CurrentMap?.Id,
                NodeStatuses = state.MapProgress?.Statuses.ToDictionary(
                    value => value.Key,
                    value => value.Value) ?? new Dictionary<string, RunNodeStatus>(),
                CurrentNodeId = state.CurrentNodeId,
                CurrentAttempt = CaptureAttempt(state.CurrentAttempt),
                LastSettlement = CaptureSettlement(state.LastSettlement),
                LastRewardSummary = state.LastRewardSummary ?? string.Empty,
                DelayedShopResources = new DelayedShopResourcesSnapshotV1
                {
                    GoldBonus = state.DelayedShopResources.GoldBonus,
                    FreeRefreshes = state.DelayedShopResources.FreeRefreshes,
                    UpgradeDiscount = state.DelayedShopResources.UpgradeDiscount,
                    LastAppliedRunTurn = state.DelayedShopResources.LastAppliedRunTurn
                },
                PendingCardRewards = state.PendingCardRewards.Select(value =>
                    new PendingCardRewardSnapshotV1
                    {
                        RewardInstanceId = value.RewardInstanceId,
                        CardType = value.CardType,
                        ConfigId = value.ConfigId,
                        ReservedPoolCopies = value.ReservedPoolCopies
                    }).ToList(),
                PendingRewardChoice = CaptureRewardChoice(state.PendingRewardChoice),
                PendingRelicChoice = CaptureRelicChoice(state.PendingRelicChoice),
                PendingEventChoice = state.PendingEventChoice == null
                    ? null
                    : new PendingEventChoiceSnapshotV1
                    {
                        SourceAttemptId = state.PendingEventChoice.SourceAttemptId,
                        EventId = state.PendingEventChoice.Config?.Id
                    },
                PendingEnhanceChoice = state.PendingEnhanceChoice == null
                    ? null
                    : new PendingEnhanceChoiceSnapshotV1
                    {
                        SourceAttemptId = state.PendingEnhanceChoice.SourceAttemptId,
                        NodeConfigId = state.PendingEnhanceChoice.NodeConfig?.Id,
                        RecipeIds = state.PendingEnhanceChoice.Recipes
                            .Select(value => value.Id).ToList()
                    },
                PendingRestChoice = state.PendingRestChoice == null
                    ? null
                    : new PendingRestChoiceSnapshotV1
                    {
                        SourceAttemptId = state.PendingRestChoice.SourceAttemptId,
                        RestNodeId = state.PendingRestChoice.Config?.Id
                    },
                OwnedRelics = state.OwnedRelics.Select(CaptureOwnedRelic).ToList(),
                Statistics = CaptureStatistics(state.Statistics)
            };
        }

        private RunState RestoreRunState(RunStateSnapshotV1 snapshot, MapDefinition map)
        {
            if (snapshot.Phase == RunPhase.EnteringNode)
            {
                throw new RunSnapshotException("EnteringNode snapshot is not durable.");
            }

            var state = new RunState(snapshot.Seed, map)
            {
                Floor = snapshot.Floor,
                ShopTurn = snapshot.ShopTurn,
                MapStep = snapshot.MapStep,
                Health = snapshot.Health,
                MaxHealth = snapshot.MaxHealth,
                Phase = snapshot.Phase,
                CurrentNodeId = snapshot.CurrentNodeId,
                CurrentAttempt = RestoreAttempt(snapshot.CurrentAttempt),
                LastSettlement = RestoreSettlement(snapshot.LastSettlement),
                LastRewardSummary = snapshot.LastRewardSummary ?? string.Empty,
                PendingRewardChoice = RestoreRewardChoice(snapshot.PendingRewardChoice),
                PendingRelicChoice = RestoreRelicChoice(snapshot.PendingRelicChoice),
                PendingEventChoice = RestoreEventChoice(snapshot.PendingEventChoice),
                PendingEnhanceChoice = RestoreEnhanceChoice(snapshot.PendingEnhanceChoice),
                PendingRestChoice = RestoreRestChoice(snapshot.PendingRestChoice)
            };
            state.MapProgress.RestoreStatuses(snapshot.NodeStatuses);
            var delayed = snapshot.DelayedShopResources ??
                          new DelayedShopResourcesSnapshotV1();
            state.DelayedShopResources.GoldBonus = delayed.GoldBonus;
            state.DelayedShopResources.FreeRefreshes = delayed.FreeRefreshes;
            state.DelayedShopResources.UpgradeDiscount = delayed.UpgradeDiscount;
            state.DelayedShopResources.LastAppliedRunTurn = delayed.LastAppliedRunTurn;
            RestoreStatistics(state.Statistics, snapshot.Statistics);
            state.RestoreCollections(
                (snapshot.PendingCardRewards ?? new List<PendingCardRewardSnapshotV1>())
                .Select(value => new PendingCardReward(
                    Required(value.RewardInstanceId, "reward instance id"),
                    value.CardType,
                    RequiredCard(value.ConfigId, value.CardType),
                    value.ReservedPoolCopies)),
                (snapshot.OwnedRelics ?? new List<OwnedRelicSnapshotV1>())
                .Select(RestoreOwnedRelic));
            return state;
        }

        private ShopSessionSnapshotV1 CaptureShop(ShopSession shop)
        {
            return new ShopSessionSnapshotV1
            {
                Round = shop.Round,
                Gold = shop.Gold,
                TavernTier = shop.TavernTier,
                RefreshCount = shop.RefreshCount,
                FreeRefreshes = shop.FreeRefreshes,
                IsShopOpen = shop.IsShopOpen,
                IsFrozen = shop.IsFrozen,
                UpgradedThisRound = shop.UpgradedThisRound,
                MinionOfferIds = shop.MinionOffers.Select(value => value?.Id).ToList(),
                SpellOfferId = shop.SpellOffer?.Id,
                MinionPoolRemainingCopies = shop.MinionPool.RemainingCopies
                    .ToDictionary(value => value.Key, value => value.Value),
                Bench = shop.Collection.Bench.Select(CaptureCard).ToList(),
                Battle = shop.Collection.Battle.Select(CaptureCard).ToList(),
                PendingDiscover = CaptureDiscover(shop.PendingDiscover),
                PendingChoice = CaptureEffectChoice(shop.PendingChoice),
                ActiveShopEffects = shop.ActiveShopEffects.Select(value =>
                    new ActiveShopEffectSnapshotV1
                    {
                        SourceInstanceId = value.SourceInstanceId,
                        SourceConfigId = value.SourceConfigId,
                        Effect = CaptureEffect(value.Effect, value.SourceConfigId),
                        ActivationRefreshCount = value.ActivationRefreshCount,
                        TriggerCount = value.TriggerCount
                    }).ToList(),
                PerShopEffectUsage = shop.PerShopEffectUsage.ToDictionary(
                    value => value.Key,
                    value => value.Value),
                PendingPostCombatBuffs = shop.PendingPostCombatBuffs.ToDictionary(
                    value => value.Key,
                    value => CaptureValue(value.Value)),
                PendingBattleStartEffects = shop.PendingBattleStartEffects
                    .Select(value => CaptureEffect(value, null)).ToList(),
                PhaseStats = new ShopPhaseStatsSnapshotV1
                {
                    RefreshCount = shop.PhaseStats.RefreshCount,
                    SpellUsedCount = shop.PhaseStats.SpellUsedCount,
                    SpellBoughtCount = shop.PhaseStats.SpellBoughtCount,
                    MinionBoughtCount = shop.PhaseStats.MinionBoughtCount
                },
                FlourishStacks = shop.FlourishStacks,
                CardInstanceSequence = shop.CardInstanceSequence,
                RoundsWithoutUpgradeAtCurrentTier =
                    shop.RoundsWithoutUpgradeAtCurrentTier,
                PendingUpgradeDiscount = shop.PendingUpgradeDiscount,
                ScheduledGold = shop.ScheduledGold,
                RuleModifiers = CaptureShopRules(shop.RuleModifiers),
                FirstPurchaseFreeAvailable = shop.FirstPurchaseFreeAvailable,
                FirstPaidRefreshFreeAvailable = shop.FirstPaidRefreshFreeAvailable,
                FirstMinionSaleBonusAvailable = shop.FirstMinionSaleBonusAvailable
            };
        }

        private ShopSession RestoreShop(
            ShopSessionSnapshotV1 snapshot,
            RecordedRandom random)
        {
            var shop = new ShopSession(configs.Minions, configs.Spells, random);
            shop.MinionPool.RestoreRemainingCopies(snapshot.MinionPoolRemainingCopies);
            var bench = RestoreCards(snapshot.Bench, ShopEconomyRules.BenchSlotCount);
            var battle = RestoreCards(snapshot.Battle, ShopEconomyRules.BattleSlotCount);
            shop.Collection.RestoreSlots(bench, battle);
            var cardsById = bench.Concat(battle)
                .Where(value => value != null)
                .GroupBy(value => value.InstanceId)
                .ToDictionary(value => value.Key, value => value.First());
            var phaseStats = snapshot.PhaseStats ?? new ShopPhaseStatsSnapshotV1();
            var restoredPhaseStats = new ShopPhaseStats
            {
                RefreshCount = phaseStats.RefreshCount,
                SpellUsedCount = phaseStats.SpellUsedCount,
                SpellBoughtCount = phaseStats.SpellBoughtCount,
                MinionBoughtCount = phaseStats.MinionBoughtCount
            };
            shop.Restore(new ShopSessionRestoreData
            {
                Round = snapshot.Round,
                Gold = snapshot.Gold,
                TavernTier = snapshot.TavernTier,
                RefreshCount = snapshot.RefreshCount,
                FreeRefreshes = snapshot.FreeRefreshes,
                IsShopOpen = snapshot.IsShopOpen,
                IsFrozen = snapshot.IsFrozen,
                UpgradedThisRound = snapshot.UpgradedThisRound,
                MinionOffers = (snapshot.MinionOfferIds ?? new List<string>())
                    .Select(value => string.IsNullOrWhiteSpace(value)
                        ? null
                        : RequiredMinion(value)).ToList(),
                SpellOffer = string.IsNullOrWhiteSpace(snapshot.SpellOfferId)
                    ? null
                    : RequiredSpell(snapshot.SpellOfferId),
                PendingDiscover = RestoreDiscover(snapshot.PendingDiscover, cardsById),
                PendingChoice = RestoreEffectChoice(snapshot.PendingChoice, cardsById),
                ActiveShopEffects = (snapshot.ActiveShopEffects ??
                    new List<ActiveShopEffectSnapshotV1>()).Select(value =>
                {
                    var restored = new ActiveShopEffect(
                        value.SourceInstanceId,
                        value.SourceConfigId,
                        RequiredEffect(value.Effect),
                        value.ActivationRefreshCount)
                    {
                        TriggerCount = value.TriggerCount
                    };
                    return restored;
                }).ToList(),
                PerShopEffectUsage = snapshot.PerShopEffectUsage ??
                                     new Dictionary<string, int>(),
                PendingPostCombatBuffs = (snapshot.PendingPostCombatBuffs ??
                    new Dictionary<string, ValueConfigSnapshotV1>()).ToDictionary(
                    value => value.Key,
                    value => RestoreValue(value.Value)),
                PendingBattleStartEffects = (snapshot.PendingBattleStartEffects ??
                    new List<EffectReferenceSnapshotV1>()).Select(RequiredEffect).ToList(),
                PhaseStats = restoredPhaseStats,
                FlourishStacks = snapshot.FlourishStacks,
                CardInstanceSequence = snapshot.CardInstanceSequence,
                RoundsWithoutUpgradeAtCurrentTier =
                    snapshot.RoundsWithoutUpgradeAtCurrentTier,
                PendingUpgradeDiscount = snapshot.PendingUpgradeDiscount,
                ScheduledGold = snapshot.ScheduledGold,
                RuleModifiers = RestoreShopRules(snapshot.RuleModifiers),
                FirstPurchaseFreeAvailable = snapshot.FirstPurchaseFreeAvailable,
                FirstPaidRefreshFreeAvailable = snapshot.FirstPaidRefreshFreeAvailable,
                FirstMinionSaleBonusAvailable = snapshot.FirstMinionSaleBonusAvailable
            });
            return shop;
        }

        private ShopCardSnapshotV1 CaptureCard(ShopCardInstance card)
        {
            if (card == null)
            {
                return null;
            }

            return new ShopCardSnapshotV1
            {
                InstanceId = card.InstanceId,
                CardType = card.CardType,
                ConfigId = card.ConfigId,
                IsGolden = card.IsGolden,
                PermanentAttackBonus = card.PermanentAttackBonus,
                PermanentHealthBonus = card.PermanentHealthBonus,
                FlourishAttackBonus = card.FlourishAttackBonus,
                PermanentKeywords = card.PermanentKeywords.OrderBy(value => value).ToList(),
                TripleDiscoveryPending = card.TripleDiscoveryPending,
                ExpiresAtShopEnd = card.ExpiresAtShopEnd,
                PendingCombatModifiers = card.PendingCombatModifiers.Select(value =>
                    new PendingCombatModifierSnapshotV1
                    {
                        EffectId = value.EffectId,
                        Attack = value.Attack,
                        Health = value.Health,
                        Keyword = value.Keyword,
                        AddShield = value.AddShield
                    }).ToList()
            };
        }

        private IReadOnlyList<ShopCardInstance> RestoreCards(
            IReadOnlyList<ShopCardSnapshotV1> snapshots,
            int expectedCount)
        {
            if (snapshots == null || snapshots.Count != expectedCount)
            {
                throw new RunSnapshotException(
                    $"Expected {expectedCount} collection slots.");
            }

            return snapshots.Select(RestoreCard).ToList();
        }

        private ShopCardInstance RestoreCard(ShopCardSnapshotV1 snapshot)
        {
            if (snapshot == null)
            {
                return null;
            }

            var minion = snapshot.CardType == ShopCardType.Minion
                ? RequiredMinion(snapshot.ConfigId)
                : null;
            var spell = snapshot.CardType == ShopCardType.Spell
                ? RequiredSpell(snapshot.ConfigId)
                : null;
            return ShopCardInstance.Restore(
                Required(snapshot.InstanceId, "card instance id"),
                snapshot.CardType,
                minion,
                spell,
                snapshot.IsGolden,
                snapshot.PermanentAttackBonus,
                snapshot.PermanentHealthBonus,
                snapshot.FlourishAttackBonus,
                snapshot.PermanentKeywords,
                snapshot.TripleDiscoveryPending,
                snapshot.ExpiresAtShopEnd,
                (snapshot.PendingCombatModifiers ??
                    new List<PendingCombatModifierSnapshotV1>()).Select(value =>
                    new PendingCombatModifier(
                        value.EffectId,
                        value.Attack,
                        value.Health,
                        value.Keyword,
                        value.AddShield)));
        }

        private ShopDiscoverSnapshotV1 CaptureDiscover(ShopDiscoverState discover)
        {
            return discover == null
                ? null
                : new ShopDiscoverSnapshotV1
                {
                    SourceInstanceId = discover.SourceSpell.InstanceId,
                    BenchIndex = discover.BenchIndex,
                    CandidateIds = discover.Candidates.Select(value => value.Id).ToList(),
                    CanCancel = discover.CanCancel
                };
        }

        private ShopDiscoverState RestoreDiscover(
            ShopDiscoverSnapshotV1 snapshot,
            IReadOnlyDictionary<string, ShopCardInstance> cardsById)
        {
            if (snapshot == null)
            {
                return null;
            }

            return new ShopDiscoverState(
                RequiredCardInstance(cardsById, snapshot.SourceInstanceId),
                snapshot.BenchIndex,
                (snapshot.CandidateIds ?? new List<string>()).Select(RequiredMinion),
                snapshot.CanCancel);
        }

        private PendingEffectChoiceSnapshotV1 CaptureEffectChoice(
            PendingEffectChoice choice)
        {
            if (choice == null)
            {
                return null;
            }

            return new PendingEffectChoiceSnapshotV1
            {
                ChoiceType = choice.ChoiceType,
                SourceInstanceId = choice.SourceCard.InstanceId,
                BenchIndex = choice.BenchIndex,
                Effect = CaptureEffect(choice.Effect, choice.SourceCard.ConfigId),
                Candidates = choice.Candidates.Select(value =>
                    new EffectChoiceCandidateSnapshotV1
                    {
                        Id = value.Id,
                        DisplayName = value.DisplayName,
                        MinionId = value.Minion?.Id,
                        SpellId = value.Spell?.Id,
                        TargetInstanceId = value.Target?.InstanceId
                    }).ToList(),
                ReplaceSourceCard = choice.ReplaceSourceCard,
                RemainingChoices = choice.RemainingChoices,
                TotalChoices = choice.TotalChoices
            };
        }

        private PendingEffectChoice RestoreEffectChoice(
            PendingEffectChoiceSnapshotV1 snapshot,
            IReadOnlyDictionary<string, ShopCardInstance> cardsById)
        {
            if (snapshot == null)
            {
                return null;
            }

            return new PendingEffectChoice(
                snapshot.ChoiceType,
                RequiredCardInstance(cardsById, snapshot.SourceInstanceId),
                snapshot.BenchIndex,
                RequiredEffect(snapshot.Effect),
                (snapshot.Candidates ?? new List<EffectChoiceCandidateSnapshotV1>())
                .Select(value => new EffectChoiceCandidate(
                    value.Id,
                    value.DisplayName,
                    string.IsNullOrWhiteSpace(value.MinionId)
                        ? null
                        : RequiredMinion(value.MinionId),
                    string.IsNullOrWhiteSpace(value.SpellId)
                        ? null
                        : RequiredSpell(value.SpellId),
                    string.IsNullOrWhiteSpace(value.TargetInstanceId)
                        ? null
                        : RequiredCardInstance(cardsById, value.TargetInstanceId))),
                snapshot.ReplaceSourceCard,
                snapshot.RemainingChoices,
                snapshot.TotalChoices);
        }

        private BattleContextSnapshotV1 CaptureContext(BattleContext context)
        {
            if (context == null)
            {
                return null;
            }

            if (!GameSceneNames.TryParse(context.ReturnSceneName, out var returnScene))
            {
                throw new RunSnapshotException(
                    $"Unsupported battle return scene {context.ReturnSceneName}.");
            }

            return new BattleContextSnapshotV1
            {
                Board = CaptureBoard(context.BoardState),
                EncounterName = context.EncounterName,
                ReturnScene = returnScene,
                NodeAttemptId = context.NodeAttemptId,
                EncounterId = context.EncounterId,
                BattleSeed = context.BattleSeed
            };
        }

        private BattleContext RestoreContext(BattleContextSnapshotV1 snapshot)
        {
            return snapshot == null
                ? null
                : new BattleContext(
                    RestoreBoard(snapshot.Board),
                    snapshot.EncounterName,
                    GameSceneNames.Get(snapshot.ReturnScene),
                    snapshot.NodeAttemptId,
                    snapshot.EncounterId,
                    snapshot.BattleSeed);
        }

        private BattleResultSnapshotV1 CaptureResult(BattleSimulationResult result)
        {
            return result == null
                ? null
                : new BattleResultSnapshotV1
                {
                    FinalBoard = CaptureBoard(result.FinalState),
                    Winner = result.Winner,
                    OutcomeReason = result.OutcomeReason,
                    Log = (result.Log ?? Array.Empty<string>()).ToList()
                };
        }

        private BattleSimulationResult RestoreResult(BattleResultSnapshotV1 snapshot)
        {
            return snapshot == null
                ? null
                : new BattleSimulationResult(
                    RestoreBoard(snapshot.FinalBoard),
                    snapshot.Winner,
                    snapshot.OutcomeReason,
                    snapshot.Log ?? new List<string>(),
                    new List<BattleStep>());
        }

        private BattleBoardSnapshotV1 CaptureBoard(BattleBoardState board)
        {
            if (board == null)
            {
                return null;
            }

            return new BattleBoardSnapshotV1
            {
                Player = board.Player.Select(CaptureBattleMinion).ToList(),
                Enemy = board.Enemy.Select(CaptureBattleMinion).ToList(),
                BattleStartEffects = board.BattleStartEffects.Select(value =>
                    new BattleStartEffectSnapshotV1
                    {
                        Side = value.Side,
                        Effect = CaptureEffect(value.Effect, null)
                    }).ToList(),
                RuleModifiers = CaptureBattleRules(board.RuleModifiers),
                PlayerFlourishStacks = board.PlayerFlourishStacks,
                EnemyFlourishStacks = board.EnemyFlourishStacks
            };
        }

        private BattleBoardState RestoreBoard(BattleBoardSnapshotV1 snapshot)
        {
            if (snapshot == null || snapshot.Player == null || snapshot.Enemy == null ||
                snapshot.Player.Count != BattleBoardState.SlotCount ||
                snapshot.Enemy.Count != BattleBoardState.SlotCount)
            {
                throw new RunSnapshotException("Battle board snapshot has invalid slots.");
            }

            var board = new BattleBoardState
            {
                PlayerFlourishStacks = snapshot.PlayerFlourishStacks,
                EnemyFlourishStacks = snapshot.EnemyFlourishStacks
            };
            for (var index = 0; index < BattleBoardState.SlotCount; index++)
            {
                board.Player[index] = RestoreBattleMinion(snapshot.Player[index]);
                board.Enemy[index] = RestoreBattleMinion(snapshot.Enemy[index]);
            }

            foreach (var effect in snapshot.BattleStartEffects ??
                     new List<BattleStartEffectSnapshotV1>())
            {
                board.BattleStartEffects.Add(new BattleStartEffectState(
                    effect.Side,
                    RequiredEffect(effect.Effect)));
            }

            RestoreBattleRules(board.RuleModifiers, snapshot.RuleModifiers);
            return board;
        }

        private BattleMinionSnapshotV1 CaptureBattleMinion(BattleMinionRuntime minion)
        {
            return minion == null
                ? null
                : new BattleMinionSnapshotV1
                {
                    ConfigId = minion.Id,
                    SourceInstanceId = minion.SourceInstanceId,
                    RuntimeInstanceId = minion.RuntimeInstanceId,
                    IsGolden = minion.IsGolden,
                    CurrentAttack = minion.CurrentAttack,
                    CurrentHealth = minion.CurrentHealth,
                    CombatMaxHealth = minion.CombatMaxHealth,
                    PermanentAttackBonus = minion.PermanentAttackBonus,
                    PermanentHealthBonus = minion.PermanentHealthBonus,
                    HasShield = minion.HasShield,
                    Keywords = minion.Keywords.OrderBy(value => value).ToList(),
                    SummonEffectMultiplier = minion.SummonEffectMultiplier
                };
        }

        private BattleMinionRuntime RestoreBattleMinion(BattleMinionSnapshotV1 snapshot)
        {
            return snapshot == null
                ? null
                : BattleMinionRuntime.Restore(
                    RequiredMinion(snapshot.ConfigId),
                    snapshot.IsGolden,
                    snapshot.CurrentAttack,
                    snapshot.CurrentHealth,
                    snapshot.CombatMaxHealth,
                    snapshot.PermanentAttackBonus,
                    snapshot.PermanentHealthBonus,
                    snapshot.HasShield,
                    snapshot.Keywords,
                    snapshot.SourceInstanceId,
                    snapshot.SummonEffectMultiplier,
                    snapshot.RuntimeInstanceId);
        }

        private EffectReferenceSnapshotV1 CaptureEffect(
            EffectConfig effect,
            string ownerHint)
        {
            if (effect == null)
            {
                throw new RunSnapshotException("Effect reference is missing.");
            }

            if (!string.IsNullOrWhiteSpace(ownerHint) &&
                configs.TryGetEffect(ownerHint, effect.Id, out var hinted) &&
                ReferenceEquals(hinted, effect))
            {
                return new EffectReferenceSnapshotV1
                {
                    OwnerConfigId = ownerHint,
                    EffectId = effect.Id
                };
            }

            if (!configs.TryGetEffectReference(effect, out var ownerId, out var effectId))
            {
                throw new RunSnapshotException($"Effect {effect.Id} is not in current content.");
            }

            return new EffectReferenceSnapshotV1
            {
                OwnerConfigId = ownerId,
                EffectId = effectId
            };
        }

        private EffectConfig RequiredEffect(EffectReferenceSnapshotV1 reference)
        {
            if (reference == null || !configs.TryGetEffect(
                    reference.OwnerConfigId,
                    reference.EffectId,
                    out var effect))
            {
                throw new RunSnapshotException(
                    $"Missing effect {reference?.OwnerConfigId}/{reference?.EffectId}.");
            }

            return effect;
        }

        private static RandomStreamSnapshotV1 CaptureRandom(
            RecordedRandom random,
            string streamName)
        {
            if (random == null)
            {
                throw new RunSnapshotException(
                    $"{streamName} random stream is not recordable.");
            }

            return new RandomStreamSnapshotV1
            {
                Seed = random.Seed,
                Entries = random.Entries.Select(value => value.Clone()).ToList()
            };
        }

        private static RecordedRandom RestoreRandom(
            RandomStreamSnapshotV1 snapshot,
            string streamName)
        {
            if (snapshot == null)
            {
                throw new RunSnapshotException($"{streamName} random stream is missing.");
            }

            return RecordedRandom.Restore(snapshot.Seed, snapshot.Entries);
        }

        private static NodeAttemptSnapshotV1 CaptureAttempt(NodeAttemptState attempt)
        {
            return attempt == null
                ? null
                : new NodeAttemptSnapshotV1
                {
                    NodeAttemptId = attempt.NodeAttemptId,
                    NodeId = attempt.NodeId,
                    NodeType = attempt.NodeType,
                    ContentId = attempt.ContentId,
                    EncounterId = attempt.EncounterId,
                    RunTurn = attempt.RunTurn,
                    RunTurnCommitted = attempt.RunTurnCommitted,
                    EconomyTurnCommitted = attempt.EconomyTurnCommitted,
                    ContentGenerated = attempt.ContentGenerated,
                    ChoiceCommitted = attempt.ChoiceCommitted,
                    EffectApplied = attempt.EffectApplied,
                    BattleSettled = attempt.BattleSettled,
                    HealthDamageApplied = attempt.HealthDamageApplied,
                    RewardGenerated = attempt.RewardGenerated,
                    RelicGenerated = attempt.RelicGenerated,
                    RelicVictoryEffectsApplied = attempt.RelicVictoryEffectsApplied,
                    NodeResolved = attempt.NodeResolved
                };
        }

        private static NodeAttemptState RestoreAttempt(NodeAttemptSnapshotV1 snapshot)
        {
            if (snapshot == null)
            {
                return null;
            }

            return new NodeAttemptState(
                Required(snapshot.NodeAttemptId, "attempt id"),
                Required(snapshot.NodeId, "node id"),
                snapshot.NodeType,
                snapshot.ContentId,
                snapshot.RunTurn)
            {
                ContentId = snapshot.ContentId,
                EncounterId = snapshot.EncounterId,
                RunTurnCommitted = snapshot.RunTurnCommitted,
                EconomyTurnCommitted = snapshot.EconomyTurnCommitted,
                ContentGenerated = snapshot.ContentGenerated,
                ChoiceCommitted = snapshot.ChoiceCommitted,
                EffectApplied = snapshot.EffectApplied,
                BattleSettled = snapshot.BattleSettled,
                HealthDamageApplied = snapshot.HealthDamageApplied,
                RewardGenerated = snapshot.RewardGenerated,
                RelicGenerated = snapshot.RelicGenerated,
                RelicVictoryEffectsApplied = snapshot.RelicVictoryEffectsApplied,
                NodeResolved = snapshot.NodeResolved
            };
        }

        private static BattleSettlementSnapshotV1 CaptureSettlement(
            BattleSettlementResult settlement)
        {
            return settlement == null
                ? null
                : new BattleSettlementSnapshotV1
                {
                    PlayerWon = settlement.PlayerWon,
                    Damage = settlement.Damage,
                    SurvivingEnemies = settlement.SurvivingEnemies,
                    HighestEnemyTier = settlement.HighestEnemyTier,
                    NodeDamageBonus = settlement.NodeDamageBonus,
                    OutcomeReason = settlement.OutcomeReason
                };
        }

        private static BattleSettlementResult RestoreSettlement(
            BattleSettlementSnapshotV1 snapshot)
        {
            return snapshot == null
                ? null
                : new BattleSettlementResult(
                    snapshot.PlayerWon,
                    snapshot.Damage,
                    snapshot.SurvivingEnemies,
                    snapshot.HighestEnemyTier,
                    snapshot.NodeDamageBonus,
                    snapshot.OutcomeReason);
        }

        private static PendingRewardChoiceSnapshotV1 CaptureRewardChoice(
            PendingRewardChoice choice)
        {
            return choice == null
                ? null
                : new PendingRewardChoiceSnapshotV1
                {
                    ChoiceId = choice.ChoiceId,
                    SourceAttemptId = choice.SourceAttemptId,
                    CompletionMode = choice.CompletionMode,
                    AllowSkip = choice.AllowSkip,
                    Candidates = choice.Candidates.Select(value =>
                        new RewardCandidateSnapshotV1
                        {
                            CandidateId = value.CandidateId,
                            Category = value.Category,
                            Type = value.Type,
                            Amount = value.Amount,
                            CardId = value.CardId,
                            ReservedPoolCopies = value.ReservedPoolCopies,
                            Attack = value.Attack,
                            Health = value.Health,
                            DisplayText = value.DisplayText
                        }).ToList()
                };
        }

        private PendingRewardChoice RestoreRewardChoice(
            PendingRewardChoiceSnapshotV1 snapshot)
        {
            if (snapshot == null)
            {
                return null;
            }

            foreach (var cardId in snapshot.Candidates
                         .Select(value => value.CardId)
                         .Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                RequiredAnyCard(cardId);
            }

            return new PendingRewardChoice(
                snapshot.ChoiceId,
                snapshot.SourceAttemptId,
                snapshot.CompletionMode,
                snapshot.Candidates.Select(value => new RewardCandidate(
                    value.CandidateId,
                    value.Category,
                    value.Type,
                    value.Amount,
                    value.CardId,
                    value.ReservedPoolCopies,
                    value.Attack,
                    value.Health,
                    value.DisplayText)),
                snapshot.AllowSkip);
        }

        private static PendingRelicChoiceSnapshotV1 CaptureRelicChoice(
            PendingRelicChoice choice)
        {
            return choice == null
                ? null
                : new PendingRelicChoiceSnapshotV1
                {
                    ChoiceId = choice.ChoiceId,
                    SourceAttemptId = choice.SourceAttemptId,
                    Grade = choice.Grade,
                    CompletionMode = choice.CompletionMode,
                    Candidates = choice.Candidates.Select(value =>
                        new RelicCandidateSnapshotV1
                        {
                            CandidateId = value.CandidateId,
                            RelicId = value.RelicId
                        }).ToList(),
                    HealthCost = choice.HealthCost,
                    AllowSkip = choice.AllowSkip
                };
        }

        private PendingRelicChoice RestoreRelicChoice(
            PendingRelicChoiceSnapshotV1 snapshot)
        {
            return snapshot == null
                ? null
                : new PendingRelicChoice(
                    snapshot.ChoiceId,
                    snapshot.SourceAttemptId,
                    snapshot.Grade,
                    snapshot.CompletionMode,
                    snapshot.Candidates.Select(value => new RelicCandidate(
                        value.CandidateId,
                        RequiredRelic(value.RelicId))),
                    snapshot.HealthCost,
                    snapshot.AllowSkip);
        }

        private PendingEventChoice RestoreEventChoice(PendingEventChoiceSnapshotV1 snapshot)
        {
            if (snapshot == null)
            {
                return null;
            }

            if (!configs.TryGetEvent(snapshot.EventId, out var config))
            {
                throw new RunSnapshotException($"Missing event {snapshot.EventId}.");
            }

            return new PendingEventChoice(snapshot.SourceAttemptId, config);
        }

        private PendingEnhanceChoice RestoreEnhanceChoice(
            PendingEnhanceChoiceSnapshotV1 snapshot)
        {
            if (snapshot == null)
            {
                return null;
            }

            if (!configs.TryGetEnhanceNode(snapshot.NodeConfigId, out var node))
            {
                throw new RunSnapshotException(
                    $"Missing enhancement node {snapshot.NodeConfigId}.");
            }

            return new PendingEnhanceChoice(
                snapshot.SourceAttemptId,
                node,
                snapshot.RecipeIds.Select(value =>
                {
                    if (!configs.TryGetEnhancementRecipe(value, out var recipe))
                    {
                        throw new RunSnapshotException($"Missing enhancement recipe {value}.");
                    }

                    return recipe;
                }));
        }

        private PendingRestChoice RestoreRestChoice(PendingRestChoiceSnapshotV1 snapshot)
        {
            if (snapshot == null)
            {
                return null;
            }

            if (!configs.TryGetRestNode(snapshot.RestNodeId, out var config))
            {
                throw new RunSnapshotException($"Missing rest node {snapshot.RestNodeId}.");
            }

            return new PendingRestChoice(snapshot.SourceAttemptId, config);
        }

        private static OwnedRelicSnapshotV1 CaptureOwnedRelic(OwnedRelicState relic)
        {
            return new OwnedRelicSnapshotV1
            {
                RelicId = relic.RelicId,
                SourceType = relic.SourceType,
                SourceId = relic.SourceId,
                AcquiredFloor = relic.AcquiredFloor,
                AcquiredShopTurn = relic.AcquiredShopTurn,
                ShopProgress = relic.ShopProgress,
                LastResolvedShopTurn = relic.LastResolvedShopTurn,
                ActivationCount = relic.ActivationCount
            };
        }

        private OwnedRelicState RestoreOwnedRelic(OwnedRelicSnapshotV1 snapshot)
        {
            return new OwnedRelicState(
                RequiredRelic(snapshot.RelicId),
                snapshot.SourceType,
                snapshot.SourceId,
                snapshot.AcquiredFloor,
                snapshot.AcquiredShopTurn)
            {
                ShopProgress = snapshot.ShopProgress,
                LastResolvedShopTurn = snapshot.LastResolvedShopTurn,
                ActivationCount = snapshot.ActivationCount
            };
        }

        private static RunStatisticsSnapshotV1 CaptureStatistics(RunStatistics stats)
        {
            return new RunStatisticsSnapshotV1
            {
                StartedAtUtc = stats.StartedAtUtc,
                CompletedAtUtc = stats.CompletedAtUtc,
                BattlesWon = stats.BattlesWon,
                BattlesNotWon = stats.BattlesNotWon,
                ElitesAttempted = stats.ElitesAttempted,
                ElitesDefeated = stats.ElitesDefeated,
                BossAttempts = stats.BossAttempts,
                BossesDefeated = stats.BossesDefeated,
                TriplesFormed = stats.TriplesFormed,
                RefreshesPaid = stats.RefreshesPaid,
                RefreshesFree = stats.RefreshesFree,
                MinionsBought = stats.MinionsBought,
                MinionsSold = stats.MinionsSold,
                SpellsUsed = stats.SpellsUsed,
                TavernUpgrades = stats.TavernUpgrades,
                GoldWasted = stats.GoldWasted,
                TargetedDiscoversUsed = stats.TargetedDiscoversUsed,
                FirstCoreTurn = stats.FirstCoreTurn,
                SecondCoreTurn = stats.SecondCoreTurn
            };
        }

        private static void RestoreStatistics(
            RunStatistics stats,
            RunStatisticsSnapshotV1 snapshot)
        {
            if (snapshot == null)
            {
                throw new RunSnapshotException("Run statistics are missing.");
            }

            stats.StartedAtUtc = snapshot.StartedAtUtc;
            stats.CompletedAtUtc = snapshot.CompletedAtUtc;
            stats.BattlesWon = snapshot.BattlesWon;
            stats.BattlesNotWon = snapshot.BattlesNotWon;
            stats.ElitesAttempted = snapshot.ElitesAttempted;
            stats.ElitesDefeated = snapshot.ElitesDefeated;
            stats.BossAttempts = snapshot.BossAttempts;
            stats.BossesDefeated = snapshot.BossesDefeated;
            stats.TriplesFormed = snapshot.TriplesFormed;
            stats.RefreshesPaid = snapshot.RefreshesPaid;
            stats.RefreshesFree = snapshot.RefreshesFree;
            stats.MinionsBought = snapshot.MinionsBought;
            stats.MinionsSold = snapshot.MinionsSold;
            stats.SpellsUsed = snapshot.SpellsUsed;
            stats.TavernUpgrades = snapshot.TavernUpgrades;
            stats.GoldWasted = snapshot.GoldWasted;
            stats.TargetedDiscoversUsed = snapshot.TargetedDiscoversUsed;
            stats.FirstCoreTurn = snapshot.FirstCoreTurn;
            stats.SecondCoreTurn = snapshot.SecondCoreTurn;
        }

        private static CoreActivationEvidenceSnapshotV1 CaptureEvidence(
            CoreActivationEvidence evidence)
        {
            return new CoreActivationEvidenceSnapshotV1
            {
                ShieldEvents = evidence.ShieldEvents,
                ShieldBenefitEvents = evidence.ShieldBenefitEvents,
                SummonSuccesses = evidence.SummonSuccesses,
                NonTokenDeathBenefitEvents = evidence.NonTokenDeathBenefitEvents,
                SpellsUsed = evidence.SpellsUsed,
                Refreshes = evidence.Refreshes
            };
        }

        private static CoreActivationEvidence RestoreEvidence(
            CoreActivationEvidenceSnapshotV1 snapshot)
        {
            return new CoreActivationEvidence
            {
                ShieldEvents = snapshot?.ShieldEvents ?? 0,
                ShieldBenefitEvents = snapshot?.ShieldBenefitEvents ?? 0,
                SummonSuccesses = snapshot?.SummonSuccesses ?? 0,
                NonTokenDeathBenefitEvents = snapshot?.NonTokenDeathBenefitEvents ?? 0,
                SpellsUsed = snapshot?.SpellsUsed ?? 0,
                Refreshes = snapshot?.Refreshes ?? 0
            };
        }

        private static ValueConfigSnapshotV1 CaptureValue(ValueConfig value)
        {
            return new ValueConfigSnapshotV1
            {
                Attack = value.Attack,
                Health = value.Health,
                Amount = value.Amount,
                Duration = value.Duration,
                Keyword = value.Keyword,
                Resource = value.Resource,
                CardId = value.CardId,
                Count = value.Count,
                Temporary = value.Temporary,
                SummonEffectMultiplier = value.SummonEffectMultiplier,
                PermanentAttack = value.PermanentAttack,
                PermanentHealth = value.PermanentHealth
            };
        }

        private static ValueConfig RestoreValue(ValueConfigSnapshotV1 value)
        {
            return new ValueConfig
            {
                Attack = value?.Attack ?? 0,
                Health = value?.Health ?? 0,
                Amount = value?.Amount ?? 0,
                Duration = value?.Duration,
                Keyword = value?.Keyword,
                Resource = value?.Resource,
                CardId = value?.CardId,
                Count = value?.Count ?? 0,
                Temporary = value?.Temporary ?? false,
                SummonEffectMultiplier = value?.SummonEffectMultiplier ?? 1,
                PermanentAttack = value?.PermanentAttack ?? 0,
                PermanentHealth = value?.PermanentHealth ?? 0
            };
        }

        private static ShopRuleModifiersSnapshotV1 CaptureShopRules(
            ShopRuleModifiers value)
        {
            return new ShopRuleModifiersSnapshotV1
            {
                FirstPurchaseFree = value.FirstPurchaseFree,
                FirstPaidRefreshFree = value.FirstPaidRefreshFree,
                FirstMinionSaleBonusGold = value.FirstMinionSaleBonusGold,
                ExtraBattlecryTriggers = value.ExtraBattlecryTriggers
            };
        }

        private static ShopRuleModifiers RestoreShopRules(
            ShopRuleModifiersSnapshotV1 value)
        {
            return new ShopRuleModifiers
            {
                FirstPurchaseFree = value?.FirstPurchaseFree ?? false,
                FirstPaidRefreshFree = value?.FirstPaidRefreshFree ?? false,
                FirstMinionSaleBonusGold = value?.FirstMinionSaleBonusGold ?? 0,
                ExtraBattlecryTriggers = value?.ExtraBattlecryTriggers ?? 0
            };
        }

        private static BattleRuleModifiersSnapshotV1 CaptureBattleRules(
            BattleRuleModifiers value)
        {
            return new BattleRuleModifiersSnapshotV1
            {
                PlayerExtraDeathrattleTriggers = value.PlayerExtraDeathrattleTriggers,
                PlayerFirstNonTokenDeathSummonCount =
                    value.PlayerFirstNonTokenDeathSummonCount,
                PlayerFirstNonTokenDeathTokenId = value.PlayerFirstNonTokenDeathTokenId,
                PlayerFirstNonTokenDeathTokenAttack =
                    value.PlayerFirstNonTokenDeathTokenAttack,
                PlayerFirstNonTokenDeathTokenHealth =
                    value.PlayerFirstNonTokenDeathTokenHealth,
                PlayerBattleStartShieldTargets = value.PlayerBattleStartShieldTargets,
                PlayerDistinctRaceStatBonus = value.PlayerDistinctRaceStatBonus
            };
        }

        private static void RestoreBattleRules(
            BattleRuleModifiers target,
            BattleRuleModifiersSnapshotV1 value)
        {
            if (value == null)
            {
                return;
            }

            target.PlayerExtraDeathrattleTriggers = value.PlayerExtraDeathrattleTriggers;
            target.PlayerFirstNonTokenDeathSummonCount =
                value.PlayerFirstNonTokenDeathSummonCount;
            target.PlayerFirstNonTokenDeathTokenId = value.PlayerFirstNonTokenDeathTokenId;
            target.PlayerFirstNonTokenDeathTokenAttack =
                value.PlayerFirstNonTokenDeathTokenAttack;
            target.PlayerFirstNonTokenDeathTokenHealth =
                value.PlayerFirstNonTokenDeathTokenHealth;
            target.PlayerBattleStartShieldTargets = value.PlayerBattleStartShieldTargets;
            target.PlayerDistinctRaceStatBonus = value.PlayerDistinctRaceStatBonus;
        }

        private string RequiredCard(string id, ShopCardType type)
        {
            return type == ShopCardType.Minion
                ? RequiredMinion(id).Id
                : RequiredSpell(id).Id;
        }

        private void RequiredAnyCard(string id)
        {
            if (!configs.TryGetMinion(id, out _) && !configs.TryGetSpell(id, out _))
            {
                throw new RunSnapshotException($"Missing card {id}.");
            }
        }

        private MinionConfig RequiredMinion(string id)
        {
            if (!configs.TryGetMinion(id, out var minion))
            {
                throw new RunSnapshotException($"Missing minion {id}.");
            }

            return minion;
        }

        private SpellConfig RequiredSpell(string id)
        {
            if (!configs.TryGetSpell(id, out var spell))
            {
                throw new RunSnapshotException($"Missing spell {id}.");
            }

            return spell;
        }

        private RelicConfig RequiredRelic(string id)
        {
            if (!configs.TryGetRelic(id, out var relic))
            {
                throw new RunSnapshotException($"Missing relic {id}.");
            }

            return relic;
        }

        private static ShopCardInstance RequiredCardInstance(
            IReadOnlyDictionary<string, ShopCardInstance> cardsById,
            string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !cardsById.TryGetValue(id, out var card))
            {
                throw new RunSnapshotException($"Missing card instance {id}.");
            }

            return card;
        }

        private static string Required(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new RunSnapshotException($"Missing {name}.");
            }

            return value;
        }
    }
}

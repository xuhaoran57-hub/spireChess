using System;
using System.Collections.Generic;
using System.Linq;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Shop;

namespace SpireChess.Run
{
    public sealed class RunSession
    {
        private static readonly string[][] DefaultEnemyLineups =
        {
            new[] { "forge_soul_shield_squire", "hearth_core_spark", "young_deer_spirit" },
            new[] { "moss_mark_seedling", "rending_cub", "stargazing_apprentice", "wandering_swordsman" },
            new[] { "shieldwall_furnace_keeper", "two_tailed_fox_spirit", "swiftwing_forest_hawk", "glimmer_mage" }
        };

        private const int ShopStreamId = 101;
        private const int RewardStreamId = 202;
        private const int EventStreamId = 303;

        private readonly ConfigService configs;
        private readonly Random rewardRandom;
        private readonly Random eventRandom;
        private readonly IMapProvider mapProvider;
        private int attemptSequence;
        private int rewardSequence;
        private int choiceSequence;

        public RunSession(ConfigService configs, Random random)
            : this(
                configs,
                (random ?? throw new ArgumentNullException(nameof(random))).Next())
        {
        }

        public RunSession(ConfigService configs, int seed)
        {
            this.configs = configs ?? throw new ArgumentNullException(nameof(configs));
            Shop = new ShopSession(
                configs.Minions,
                configs.Spells,
                new Random(SeedDeriver.Combine(seed, ShopStreamId)));
            Shop.EventRaised += OnShopEvent;
            rewardRandom = new Random(SeedDeriver.Combine(seed, RewardStreamId));
            eventRandom = new Random(SeedDeriver.Combine(seed, EventStreamId));
            mapProvider = configs.RunMaps.Count == 0
                ? null
                : new FixedMapProvider(configs.RunMaps);
            var map = mapProvider?.CreateMap(new MapRequest(seed, 1));
            State = new RunState(seed, map);
        }

        public ShopSession Shop { get; }
        public RunState State { get; }
        public BattleContext PendingBattle { get; private set; }
        public BattleContext LastBattleContext { get; private set; }
        public BattleSimulationResult LastBattleResult { get; private set; }
        public bool HasStageFourMap => State.CurrentMap != null;

        public RunOperationResult EnterNode(string nodeId)
        {
            if (State.Phase != RunPhase.MapSelection)
            {
                return RunOperationResult.Fail(RunOperationError.InvalidPhase);
            }

            if (State.CurrentMap == null || !State.CurrentMap.TryGetNode(nodeId, out var node))
            {
                return RunOperationResult.Fail(RunOperationError.InvalidNode);
            }

            if (Shop.IsShopOpen || Shop.LastEconomyTurn != State.RunTurn)
            {
                return RunOperationResult.Fail(RunOperationError.InvalidTiming);
            }

            if (State.MapProgress.GetStatus(nodeId) != RunNodeStatus.Reachable)
            {
                return RunOperationResult.Fail(RunOperationError.NodeNotReachable);
            }

            if (!ValidateNodeContent(node))
            {
                return RunOperationResult.Fail(RunOperationError.MissingContent);
            }

            if (!State.MapProgress.TryEnter(nodeId))
            {
                return RunOperationResult.Fail(RunOperationError.NodeNotReachable);
            }

            State.CurrentNodeId = nodeId;
            State.Phase = RunPhase.EnteringNode;
            CreateAttempt(node, node.PayloadId);

            switch (node.Type)
            {
                case RunNodeType.Normal:
                case RunNodeType.Elite:
                case RunNodeType.Boss:
                    return StartAttemptShop();
                case RunNodeType.Event:
                case RunNodeType.Enhance:
                case RunNodeType.Rest:
                    return StartNonCombatNode(node);
                default:
                    return RunOperationResult.Fail(RunOperationError.InvalidNode);
            }
        }

        public RunOperationResult RetryBoss()
        {
            if (State.Phase != RunPhase.BattleResult ||
                State.Health <= 0 ||
                State.CurrentMap == null ||
                !State.CurrentMap.TryGetNode(State.CurrentNodeId, out var node) ||
                node.Type != RunNodeType.Boss ||
                State.CurrentAttempt == null ||
                State.CurrentAttempt.NodeResolved)
            {
                return RunOperationResult.Fail(RunOperationError.InvalidPhase);
            }

            if (!configs.TryGetEncounter(node.PayloadId, out _) ||
                Shop.IsShopOpen || Shop.LastEconomyTurn != State.RunTurn)
            {
                return RunOperationResult.Fail(RunOperationError.InvalidTiming);
            }

            State.Phase = RunPhase.EnteringNode;
            CreateAttempt(node, node.PayloadId);
            return StartAttemptShop();
        }

        public RunOperationResult ContinueAfterBattle()
        {
            if (State.Phase != RunPhase.BattleResult ||
                State.CurrentAttempt == null ||
                !State.CurrentAttempt.NodeResolved)
            {
                return RunOperationResult.Fail(RunOperationError.InvalidPhase);
            }

            State.Phase = RunPhase.MapSelection;
            return RunOperationResult.Succeed();
        }

        public RunOperationResult ContinueToNextFloor()
        {
            if (State.Phase != RunPhase.FloorComplete || State.Floor >= 3 || mapProvider == null)
            {
                return RunOperationResult.Fail(RunOperationError.InvalidPhase);
            }

            MapDefinition nextMap;
            try
            {
                nextMap = mapProvider.CreateMap(new MapRequest(State.Seed, State.Floor + 1));
            }
            catch (InvalidOperationException)
            {
                return RunOperationResult.Fail(RunOperationError.MissingContent);
            }

            State.Floor = nextMap.Floor;
            State.CurrentMap = nextMap;
            State.MapProgress = new MapProgressState(nextMap);
            State.CurrentNodeId = null;
            State.CurrentAttempt = null;
            State.LastSettlement = null;
            State.LastRewardSummary = string.Empty;
            State.Phase = RunPhase.MapSelection;
            return RunOperationResult.Succeed($"进入第 {State.Floor} 层");
        }

        public RunOperationResult SelectRewardCandidate(
            string candidateId,
            string targetInstanceId = null)
        {
            var choice = State.PendingRewardChoice;
            if (State.Phase != RunPhase.RewardChoice || choice == null)
            {
                return RunOperationResult.Fail(
                    State.CurrentAttempt?.ChoiceCommitted == true
                        ? RunOperationError.ChoiceAlreadyResolved
                        : RunOperationError.InvalidPhase);
            }

            var candidate = choice.Candidates.FirstOrDefault(value => value.CandidateId == candidateId);
            if (candidate == null)
            {
                return RunOperationResult.Fail(RunOperationError.InvalidChoice);
            }

            var applyResult = ApplyRewardCandidate(candidate, targetInstanceId);
            if (!applyResult.Success)
            {
                return applyResult;
            }

            foreach (var other in choice.Candidates.Where(value => value != candidate))
            {
                ReleaseCandidateReservation(other);
            }

            State.PendingRewardChoice = null;
            State.CurrentAttempt.ChoiceCommitted = true;
            State.CurrentAttempt.EffectApplied = true;
            State.LastRewardSummary = candidate.DisplayText;
            CompleteRewardChoice(choice.CompletionMode);
            return RunOperationResult.Succeed(candidate.DisplayText);
        }

        public RunOperationResult SkipRewardChoice()
        {
            var choice = State.PendingRewardChoice;
            if (State.Phase != RunPhase.RewardChoice || choice == null)
            {
                return RunOperationResult.Fail(RunOperationError.InvalidPhase);
            }

            if (!choice.AllowSkip)
            {
                return RunOperationResult.Fail(RunOperationError.InvalidChoice);
            }

            foreach (var candidate in choice.Candidates)
            {
                ReleaseCandidateReservation(candidate);
            }

            State.PendingRewardChoice = null;
            State.CurrentAttempt.ChoiceCommitted = true;
            State.CurrentAttempt.EffectApplied = true;
            State.LastRewardSummary = "已跳过奖励";
            CompleteRewardChoice(choice.CompletionMode);
            return RunOperationResult.Succeed("已跳过奖励");
        }

        public RunOperationResult SelectEventOption(string eventId, string optionId)
        {
            var pending = State.PendingEventChoice;
            if (State.Phase != RunPhase.EventChoice || pending == null)
            {
                return RunOperationResult.Fail(RunOperationError.InvalidPhase);
            }

            if (pending.Config.Id != eventId)
            {
                return RunOperationResult.Fail(RunOperationError.InvalidChoice);
            }

            var option = pending.Config.Options.FirstOrDefault(value => value.Id == optionId);
            if (option == null)
            {
                return RunOperationResult.Fail(RunOperationError.InvalidChoice);
            }

            var legality = ValidateEventOption(option);
            if (legality != RunOperationError.None)
            {
                return RunOperationResult.Fail(legality);
            }

            PendingRewardChoice followup = null;
            if (!string.IsNullOrWhiteSpace(option.FollowupRewardTableId))
            {
                if (!configs.TryGetRewardTable(option.FollowupRewardTableId, out var table))
                {
                    return RunOperationResult.Fail(RunOperationError.MissingContent);
                }

                followup = BuildRewardChoice(table, RewardCompletionMode.ResolveNodeToMap);
                if (followup == null)
                {
                    return RunOperationResult.Fail(RunOperationError.InsufficientPool);
                }
            }

            ApplyEventEffects(option.Effects);
            State.PendingEventChoice = null;
            State.CurrentAttempt.ChoiceCommitted = true;
            State.CurrentAttempt.EffectApplied = true;
            if (followup != null)
            {
                State.PendingRewardChoice = followup;
                State.Phase = RunPhase.RewardChoice;
            }
            else
            {
                ResolveCurrentNodeToMap();
            }

            return RunOperationResult.Succeed(option.Label);
        }

        public RunOperationResult ApplyEnhancement(string recipeId, string targetInstanceId)
        {
            var pending = State.PendingEnhanceChoice;
            if (State.Phase != RunPhase.EnhanceChoice || pending == null)
            {
                return RunOperationResult.Fail(RunOperationError.InvalidPhase);
            }

            var recipe = pending.Recipes.FirstOrDefault(value => value.Id == recipeId);
            if (recipe == null)
            {
                return RunOperationResult.Fail(RunOperationError.InvalidChoice);
            }

            var result = Shop.ModifyOwnedBattleMinion(
                targetInstanceId,
                recipe.Attack,
                recipe.Health,
                recipe.Action == "GrantKeyword" ? recipe.Keyword : null);
            if (!result.Success)
            {
                return RunOperationResult.Fail(MapShopError(result.Error));
            }

            State.PendingEnhanceChoice = null;
            State.CurrentAttempt.ChoiceCommitted = true;
            State.CurrentAttempt.EffectApplied = true;
            State.LastRewardSummary = $"{recipe.Name} 已生效";
            ResolveCurrentNodeToMap();
            return RunOperationResult.Succeed(State.LastRewardSummary);
        }

        public RunOperationResult SkipEnhancement()
        {
            if (State.Phase != RunPhase.EnhanceChoice || State.PendingEnhanceChoice == null)
            {
                return RunOperationResult.Fail(RunOperationError.InvalidPhase);
            }

            if (!State.PendingEnhanceChoice.NodeConfig.AllowSkip)
            {
                return RunOperationResult.Fail(RunOperationError.InvalidChoice);
            }

            State.PendingEnhanceChoice = null;
            State.CurrentAttempt.ChoiceCommitted = true;
            State.CurrentAttempt.EffectApplied = true;
            ResolveCurrentNodeToMap();
            return RunOperationResult.Succeed("已离开锻造台");
        }

        public RunOperationResult SelectRestOption(string optionId)
        {
            var pending = State.PendingRestChoice;
            if (State.Phase != RunPhase.RestChoice || pending == null)
            {
                return RunOperationResult.Fail(RunOperationError.InvalidPhase);
            }

            var option = pending.Config.Options.FirstOrDefault(value => value.Id == optionId);
            if (option == null)
            {
                return RunOperationResult.Fail(RunOperationError.InvalidChoice);
            }

            if (option.Heal > 0 && option.MaxHealth == 0 && State.Health >= State.MaxHealth)
            {
                return RunOperationResult.Fail(RunOperationError.NoBenefit);
            }

            State.MaxHealth += Math.Max(0, option.MaxHealth);
            State.Health = Math.Min(State.MaxHealth, State.Health + Math.Max(0, option.Heal));
            State.PendingRestChoice = null;
            State.CurrentAttempt.ChoiceCommitted = true;
            State.CurrentAttempt.EffectApplied = true;
            State.LastRewardSummary = option.Label;
            ResolveCurrentNodeToMap();
            return RunOperationResult.Succeed(option.Label);
        }

        public ShopOperationResult EnsureShopOpen()
        {
            if (State.Phase == RunPhase.Shop && State.CurrentAttempt != null)
            {
                return Shop.StartRound(State.RunTurn);
            }

            return Shop.IsShopOpen
                ? ShopOperationResult.Succeed()
                : Shop.StartNextRound();
        }

        public ShopOperationResult EndShopAndPrepareBattle(string returnSceneName = "ShopTest")
        {
            if (State.Phase == RunPhase.Shop && State.CurrentAttempt != null)
            {
                return EndStageFourShopAndPrepareBattle(returnSceneName);
            }

            return EndLegacyShopAndPrepareBattle(returnSceneName);
        }

        public RunOperationResult ClaimNextCardReward()
        {
            if (State.Phase != RunPhase.Shop)
            {
                return RunOperationResult.Fail(RunOperationError.InvalidPhase);
            }

            var reward = State.PeekCardReward();
            if (reward == null)
            {
                return RunOperationResult.Fail(RunOperationError.NoPendingCardReward);
            }

            ShopOperationResult result;
            if (reward.CardType == ShopCardType.Minion)
            {
                if (!configs.TryGetMinion(reward.ConfigId, out var minion))
                {
                    return RunOperationResult.Fail(RunOperationError.MissingContent);
                }

                result = Shop.ClaimRewardMinion(minion);
            }
            else
            {
                if (!configs.TryGetSpell(reward.ConfigId, out var spell))
                {
                    return RunOperationResult.Fail(RunOperationError.MissingContent);
                }

                result = Shop.ClaimRewardSpell(spell);
            }

            if (!result.Success)
            {
                return RunOperationResult.Fail(MapShopError(result.Error));
            }

            State.DequeueCardReward();
            return RunOperationResult.Succeed($"已领取 {reward.ConfigId}");
        }

        public RunOperationResult SkipNextCardReward()
        {
            if (State.Phase != RunPhase.Shop)
            {
                return RunOperationResult.Fail(RunOperationError.InvalidPhase);
            }

            var reward = State.DequeueCardReward();
            if (reward == null)
            {
                return RunOperationResult.Fail(RunOperationError.NoPendingCardReward);
            }

            ReleaseRewardReservation(reward);
            return RunOperationResult.Succeed($"已跳过 {reward.ConfigId}");
        }

        public void PrepareBattle(BattleContext context)
        {
            PendingBattle = context ?? throw new ArgumentNullException(nameof(context));
            LastBattleContext = null;
            LastBattleResult = null;
        }

        public bool TryCompleteBattle(BattleSimulationResult result, out string returnSceneName)
        {
            returnSceneName = null;
            if (PendingBattle == null || result == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(PendingBattle.NodeAttemptId) &&
                (State.Phase != RunPhase.Battle ||
                 State.CurrentAttempt == null ||
                 State.CurrentAttempt.NodeAttemptId != PendingBattle.NodeAttemptId))
            {
                return false;
            }

            LastBattleContext = PendingBattle;
            LastBattleResult = result;
            PendingBattle = null;
            returnSceneName = LastBattleContext.ReturnSceneName;
            if (!string.IsNullOrWhiteSpace(LastBattleContext.NodeAttemptId))
            {
                SettleStageFourBattle();
            }

            return true;
        }

        public void ReleaseOutstandingRewards()
        {
            if (State.PendingRewardChoice != null)
            {
                foreach (var candidate in State.PendingRewardChoice.Candidates)
                {
                    ReleaseCandidateReservation(candidate);
                }

                State.PendingRewardChoice = null;
            }

            PendingCardReward reward;
            while ((reward = State.DequeueCardReward()) != null)
            {
                ReleaseRewardReservation(reward);
            }
        }

        private bool ValidateNodeContent(MapNodeDefinition node)
        {
            switch (node.Type)
            {
                case RunNodeType.Normal:
                case RunNodeType.Elite:
                case RunNodeType.Boss:
                    return configs.TryGetEncounter(node.PayloadId, out _);
                case RunNodeType.Event:
                    return configs.TryGetEventPool(node.PayloadId, out _);
                case RunNodeType.Enhance:
                    return configs.TryGetEnhanceNode(node.PayloadId, out _);
                case RunNodeType.Rest:
                    return configs.TryGetRestNode(node.PayloadId, out _);
                default:
                    return false;
            }
        }

        private void CreateAttempt(MapNodeDefinition node, string contentId)
        {
            State.RunTurn++;
            attemptSequence++;
            State.CurrentAttempt = new NodeAttemptState(
                $"attempt_{attemptSequence:D6}",
                node.Id,
                node.Type,
                contentId,
                State.RunTurn);
            State.LastSettlement = null;
            State.LastRewardSummary = string.Empty;
            LastBattleContext = null;
            LastBattleResult = null;
        }

        private RunOperationResult StartAttemptShop()
        {
            var result = Shop.StartRound(State.RunTurn);
            if (!result.Success)
            {
                return RunOperationResult.Fail(RunOperationError.InvalidTiming);
            }

            State.CurrentAttempt.EconomyTurnCommitted = true;
            State.CurrentAttempt.ContentGenerated = true;
            ApplyDelayedShopResources();
            State.Phase = RunPhase.Shop;
            return RunOperationResult.Succeed();
        }

        private RunOperationResult StartNonCombatNode(MapNodeDefinition node)
        {
            var result = Shop.AdvanceSkippedRound(State.RunTurn);
            if (!result.Success)
            {
                return RunOperationResult.Fail(RunOperationError.InvalidTiming);
            }

            State.CurrentAttempt.EconomyTurnCommitted = true;
            switch (node.Type)
            {
                case RunNodeType.Event:
                    if (!TryCreateEventChoice(node.PayloadId))
                    {
                        return RunOperationResult.Fail(RunOperationError.MissingContent);
                    }

                    State.Phase = RunPhase.EventChoice;
                    break;
                case RunNodeType.Enhance:
                    if (!TryCreateEnhanceChoice(node.PayloadId))
                    {
                        return RunOperationResult.Fail(RunOperationError.MissingContent);
                    }

                    State.Phase = RunPhase.EnhanceChoice;
                    break;
                case RunNodeType.Rest:
                    if (!configs.TryGetRestNode(node.PayloadId, out var rest))
                    {
                        return RunOperationResult.Fail(RunOperationError.MissingContent);
                    }

                    State.PendingRestChoice = new PendingRestChoice(
                        State.CurrentAttempt.NodeAttemptId,
                        rest);
                    State.Phase = RunPhase.RestChoice;
                    break;
            }

            State.CurrentAttempt.ContentGenerated = true;
            return RunOperationResult.Succeed();
        }

        private bool TryCreateEventChoice(string poolId)
        {
            if (!configs.TryGetEventPool(poolId, out var pool) || pool.Entries.Count == 0)
            {
                return false;
            }

            var total = pool.Entries.Sum(value => Math.Max(0, value.Weight));
            if (total <= 0)
            {
                return false;
            }

            var roll = eventRandom.Next(total);
            EventConfig selected = null;
            foreach (var entry in pool.Entries)
            {
                var weight = Math.Max(0, entry.Weight);
                if (roll >= weight)
                {
                    roll -= weight;
                    continue;
                }

                configs.TryGetEvent(entry.EventId, out selected);
                break;
            }

            if (selected == null)
            {
                return false;
            }

            State.CurrentAttempt.ContentId = selected.Id;
            State.PendingEventChoice = new PendingEventChoice(
                State.CurrentAttempt.NodeAttemptId,
                selected);
            return true;
        }

        private bool TryCreateEnhanceChoice(string nodeId)
        {
            if (!configs.TryGetEnhanceNode(nodeId, out var node))
            {
                return false;
            }

            var recipes = new List<EnhancementRecipeConfig>();
            foreach (var id in node.RecipeIds)
            {
                if (!configs.TryGetEnhancementRecipe(id, out var recipe) || !recipe.Enabled)
                {
                    return false;
                }

                recipes.Add(recipe);
            }

            State.PendingEnhanceChoice = new PendingEnhanceChoice(
                State.CurrentAttempt.NodeAttemptId,
                node,
                recipes);
            return true;
        }

        private RunOperationError ValidateEventOption(EventOptionConfig option)
        {
            var healthCost = option.Effects
                .Where(effect => effect.Type == "LoseHealth")
                .Sum(effect => Math.Max(0, effect.Amount));
            if (healthCost >= State.Health)
            {
                return RunOperationError.NoBenefit;
            }

            var onlyHeal = option.Effects.Count > 0 &&
                           option.Effects.All(effect => effect.Type == "HealHealth");
            if (onlyHeal && State.Health >= State.MaxHealth)
            {
                return RunOperationError.NoBenefit;
            }

            return RunOperationError.None;
        }

        private void ApplyEventEffects(IEnumerable<RunEffectConfig> effects)
        {
            foreach (var effect in effects ?? Array.Empty<RunEffectConfig>())
            {
                var amount = Math.Max(0, effect.Amount);
                switch (effect.Type)
                {
                    case "LoseHealth":
                        State.Health = Math.Max(1, State.Health - amount);
                        break;
                    case "HealHealth":
                        State.Health = Math.Min(State.MaxHealth, State.Health + amount);
                        break;
                    case "NextShopGold":
                        State.DelayedShopResources.GoldBonus += amount;
                        break;
                    case "FreeRefresh":
                        State.DelayedShopResources.FreeRefreshes += amount;
                        break;
                    case "UpgradeDiscount":
                        State.DelayedShopResources.UpgradeDiscount += amount;
                        break;
                    case "QueueRandomSpell":
                        QueueSpellReward(null);
                        break;
                }
            }
        }

        private void ApplyDelayedShopResources()
        {
            var resources = State.DelayedShopResources;
            if (resources.LastAppliedRunTurn == State.RunTurn)
            {
                return;
            }

            Shop.GrantGold(resources.GoldBonus);
            Shop.GrantFreeRefreshes(resources.FreeRefreshes);
            Shop.GrantUpgradeDiscount(resources.UpgradeDiscount);
            resources.LastAppliedRunTurn = State.RunTurn;
            resources.Clear();
        }

        private ShopOperationResult EndStageFourShopAndPrepareBattle(string returnSceneName)
        {
            if (State.PendingCardRewards.Count > 0 || PendingBattle != null)
            {
                return ShopOperationResult.Fail(ShopOperationError.InvalidTiming);
            }

            var result = Shop.EndRound();
            if (!result.Success)
            {
                return result;
            }

            if (!configs.TryGetEncounter(State.CurrentAttempt.EncounterId, out var encounter))
            {
                return ShopOperationResult.Fail(ShopOperationError.InvalidTiming);
            }

            var board = Shop.CreateBattleSnapshot();
            FillEncounterEnemy(board.Enemy, encounter);
            PendingBattle = new BattleContext(
                board,
                encounter.Name,
                string.IsNullOrWhiteSpace(returnSceneName) ? "RunTest" : returnSceneName,
                State.CurrentAttempt.NodeAttemptId,
                encounter.Id);
            State.Phase = RunPhase.Battle;
            return result;
        }

        private ShopOperationResult EndLegacyShopAndPrepareBattle(string returnSceneName)
        {
            if (PendingBattle != null)
            {
                return ShopOperationResult.Fail(ShopOperationError.InvalidTiming);
            }

            var result = Shop.EndRound();
            if (!result.Success)
            {
                return result;
            }

            var board = Shop.CreateBattleSnapshot();
            FillDefaultEnemy(board.Enemy);
            PendingBattle = new BattleContext(board, $"第 {Shop.Round} 回合遭遇", returnSceneName);
            LastBattleContext = null;
            LastBattleResult = null;
            return result;
        }

        private void SettleStageFourBattle()
        {
            var attempt = State.CurrentAttempt;
            if (attempt == null || attempt.BattleSettled ||
                !configs.TryGetEncounter(attempt.EncounterId, out var encounter) ||
                !State.CurrentMap.TryGetNode(attempt.NodeId, out var node))
            {
                return;
            }

            var settlement = BattleSettlementCalculator.Calculate(LastBattleResult, encounter);
            State.LastSettlement = settlement;
            attempt.BattleSettled = true;
            if (settlement.PlayerWon)
            {
                State.Statistics.BattlesWon++;
                if (node.Type == RunNodeType.Elite) State.Statistics.ElitesDefeated++;
                if (node.Type == RunNodeType.Boss) State.Statistics.BossesDefeated++;
            }
            else
            {
                State.Statistics.BattlesNotWon++;
            }
            if (!attempt.HealthDamageApplied)
            {
                State.Health = Math.Max(0, State.Health - settlement.Damage);
                attempt.HealthDamageApplied = true;
            }

            if (State.Health <= 0)
            {
                State.Phase = RunPhase.RunLost;
                State.Statistics.Complete();
                ReleaseOutstandingRewards();
                return;
            }

            if (settlement.PlayerWon)
            {
                if (TryGenerateReward(encounter, attempt, node))
                {
                    return;
                }

                ResolveNode(node, attempt);
                State.Phase = node.Type == RunNodeType.Boss
                    ? (State.Floor >= 3 ? RunPhase.RunWon : RunPhase.FloorComplete)
                    : RunPhase.BattleResult;
                if (State.Phase == RunPhase.RunWon)
                {
                    State.Statistics.Complete();
                    ReleaseOutstandingRewards();
                }

                return;
            }

            attempt.RewardGenerated = true;
            if (node.Type == RunNodeType.Boss)
            {
                State.Phase = RunPhase.BattleResult;
                return;
            }

            ResolveNode(node, attempt);
            State.Phase = RunPhase.BattleResult;
        }

        private bool TryGenerateReward(
            EncounterConfig encounter,
            NodeAttemptState attempt,
            MapNodeDefinition node)
        {
            if (attempt.RewardGenerated)
            {
                return State.PendingRewardChoice != null;
            }

            attempt.RewardGenerated = true;
            if (string.IsNullOrWhiteSpace(encounter.RewardTableId) ||
                !configs.TryGetRewardTable(encounter.RewardTableId, out var table) ||
                table.Entries.Count == 0)
            {
                State.LastRewardSummary = "无功能奖励";
                return false;
            }

            if (table.Mode == "ChooseOne")
            {
                var completionMode = node.Type == RunNodeType.Boss && State.Floor < 3
                    ? RewardCompletionMode.FloorComplete
                    : RewardCompletionMode.ReturnToBattleResult;
                var choice = BuildRewardChoice(table, completionMode);
                if (choice == null)
                {
                    State.LastRewardSummary = "奖励候选不足";
                    return false;
                }

                State.PendingRewardChoice = choice;
                State.Phase = RunPhase.RewardChoice;
                return true;
            }

            var entry = DrawRewardEntry(table.Entries);
            ApplyAutomaticReward(entry);
            return false;
        }

        private PendingRewardChoice BuildRewardChoice(
            RewardTableConfig table,
            RewardCompletionMode completionMode)
        {
            var count = Math.Max(1, table.CandidateCount);
            var candidates = new List<RewardCandidate>();
            if (table.Entries.Count == 1 && table.Entries[0].Type == "Minion" && count > 1)
            {
                var reserved = Shop.MinionPool.ReserveDistinctAtTier(
                    Shop.TavernTier,
                    count,
                    rewardRandom);
                if (reserved.Count != count)
                {
                    foreach (var minion in reserved)
                    {
                        Shop.MinionPool.Return(minion.Id);
                    }

                    return null;
                }

                foreach (var minion in reserved)
                {
                    candidates.Add(CreateCardCandidate("Minion", "Minion", minion.Id, 1));
                }
            }
            else
            {
                var remaining = table.Entries.ToList();
                while (candidates.Count < count && remaining.Count > 0)
                {
                    var entry = DrawRewardEntry(remaining);
                    remaining.Remove(entry);
                    if (table.PreferDistinctCategories &&
                        candidates.Any(value => value.Category == entry.Category))
                    {
                        continue;
                    }

                    var candidate = MaterializeRewardCandidate(entry);
                    if (candidate != null)
                    {
                        candidates.Add(candidate);
                    }
                }
            }

            if (candidates.Count != count)
            {
                foreach (var candidate in candidates)
                {
                    ReleaseCandidateReservation(candidate);
                }

                return null;
            }

            choiceSequence++;
            return new PendingRewardChoice(
                $"choice_{choiceSequence:D6}",
                State.CurrentAttempt.NodeAttemptId,
                completionMode,
                candidates,
                table.AllowSkip);
        }

        private RewardCandidate MaterializeRewardCandidate(RewardEntryConfig entry)
        {
            switch (entry.Type)
            {
                case "Minion":
                    MinionConfig minion;
                    if (string.IsNullOrWhiteSpace(entry.CardId))
                    {
                        if (entry.MaximumTierOffset > 0)
                        {
                            minion = Shop.MinionPool.Draw(
                                Math.Min(ShopEconomyRules.MaximumTavernTier,
                                    Shop.TavernTier + entry.MaximumTierOffset),
                                rewardRandom);
                        }
                        else
                        {
                            var reserved = Shop.MinionPool.ReserveDistinctAtTier(
                                Shop.TavernTier, 1, rewardRandom);
                            minion = reserved.FirstOrDefault();
                        }
                    }
                    else if (configs.TryGetMinion(entry.CardId, out minion) &&
                             !Shop.MinionPool.TryReserveCopies(minion.Id, 1))
                    {
                        minion = null;
                    }

                    return minion == null
                        ? null
                        : CreateCardCandidate(entry.Category, "Minion", minion.Id, 1);
                case "Spell":
                    var spells = configs.Spells
                        .Where(value => value.Enabled && value.ShopEligible && value.Tier <= Shop.TavernTier)
                        .ToList();
                    var spellId = entry.CardId;
                    if (string.IsNullOrWhiteSpace(spellId) && spells.Count > 0)
                    {
                        spellId = spells[rewardRandom.Next(spells.Count)].Id;
                    }

                    return string.IsNullOrWhiteSpace(spellId)
                        ? null
                        : CreateCardCandidate(entry.Category, "Spell", spellId, 0);
                case "PermanentStats":
                    return Shop.Collection.Battle.Any(card => card != null)
                        ? CreateCandidate(entry, $"指定随从永久 +{entry.Attack}/+{entry.Health}")
                        : null;
                case "FreeRefresh":
                    return CreateCandidate(entry, $"下个商店 {entry.Amount} 次免费刷新");
                case "NextShopGold":
                    return CreateCandidate(entry, $"下个商店 +{entry.Amount} 金币");
                case "UpgradeDiscount":
                    return CreateCandidate(entry, $"下次升级费用 -{entry.Amount}");
                default:
                    return null;
            }
        }

        private RewardCandidate CreateCardCandidate(
            string category,
            string type,
            string cardId,
            int reservedCopies)
        {
            rewardSequence++;
            var name = cardId;
            if (type == "Minion" && configs.TryGetMinion(cardId, out var minion))
            {
                name = minion.Name;
            }
            else if (type == "Spell" && configs.TryGetSpell(cardId, out var spell))
            {
                name = spell.Name;
            }

            return new RewardCandidate(
                $"candidate_{rewardSequence:D6}",
                category,
                type,
                cardId: cardId,
                reservedPoolCopies: reservedCopies,
                displayText: name);
        }

        private RewardCandidate CreateCandidate(RewardEntryConfig entry, string text)
        {
            rewardSequence++;
            return new RewardCandidate(
                $"candidate_{rewardSequence:D6}",
                entry.Category,
                entry.Type,
                entry.Amount,
                entry.CardId,
                attack: entry.Attack,
                health: entry.Health,
                displayText: text);
        }

        private RunOperationResult ApplyRewardCandidate(
            RewardCandidate candidate,
            string targetInstanceId)
        {
            switch (candidate.Type)
            {
                case "Minion":
                    rewardSequence++;
                    State.EnqueueCardReward(new PendingCardReward(
                        $"reward_{rewardSequence:D6}",
                        ShopCardType.Minion,
                        candidate.CardId,
                        candidate.ReservedPoolCopies));
                    return RunOperationResult.Succeed();
                case "Spell":
                    rewardSequence++;
                    State.EnqueueCardReward(new PendingCardReward(
                        $"reward_{rewardSequence:D6}",
                        ShopCardType.Spell,
                        candidate.CardId));
                    return RunOperationResult.Succeed();
                case "FreeRefresh":
                    State.DelayedShopResources.FreeRefreshes += candidate.Amount;
                    return RunOperationResult.Succeed();
                case "NextShopGold":
                    State.DelayedShopResources.GoldBonus += candidate.Amount;
                    return RunOperationResult.Succeed();
                case "UpgradeDiscount":
                    State.DelayedShopResources.UpgradeDiscount += candidate.Amount;
                    return RunOperationResult.Succeed();
                case "PermanentStats":
                    var result = Shop.ModifyOwnedBattleMinion(
                        targetInstanceId,
                        candidate.Attack,
                        candidate.Health);
                    return result.Success
                        ? RunOperationResult.Succeed()
                        : RunOperationResult.Fail(MapShopError(result.Error));
                default:
                    return RunOperationResult.Fail(RunOperationError.InvalidChoice);
            }
        }

        private void CompleteRewardChoice(RewardCompletionMode completionMode)
        {
            ResolveNodeForCurrentAttempt();
            switch (completionMode)
            {
                case RewardCompletionMode.ReturnToBattleResult:
                    State.Phase = RunPhase.BattleResult;
                    break;
                case RewardCompletionMode.FloorComplete:
                    State.Phase = RunPhase.FloorComplete;
                    break;
                default:
                    State.Phase = RunPhase.MapSelection;
                    break;
            }
        }

        private void OnShopEvent(ShopEventData eventData)
        {
            if (eventData != null && eventData.Type == ShopEventType.OnTripleFormed)
            {
                State.Statistics.TriplesFormed++;
            }
        }

        private void ResolveCurrentNodeToMap()
        {
            ResolveNodeForCurrentAttempt();
            State.Phase = RunPhase.MapSelection;
        }

        private void ResolveNodeForCurrentAttempt()
        {
            if (State.CurrentMap.TryGetNode(State.CurrentAttempt.NodeId, out var node))
            {
                ResolveNode(node, State.CurrentAttempt);
            }
        }

        private void ResolveNode(MapNodeDefinition node, NodeAttemptState attempt)
        {
            if (attempt.NodeResolved)
            {
                return;
            }

            State.MapProgress.Resolve(node.Id);
            attempt.NodeResolved = true;
        }

        private RewardEntryConfig DrawRewardEntry(IReadOnlyList<RewardEntryConfig> entries)
        {
            var total = entries.Sum(entry => Math.Max(0, entry.Weight));
            if (total <= 0)
            {
                throw new InvalidOperationException("Reward table has no positive weight.");
            }

            var roll = rewardRandom.Next(total);
            foreach (var entry in entries)
            {
                var weight = Math.Max(0, entry.Weight);
                if (roll >= weight)
                {
                    roll -= weight;
                    continue;
                }

                return entry;
            }

            throw new InvalidOperationException("Reward draw failed after a valid roll.");
        }

        private void ApplyAutomaticReward(RewardEntryConfig entry)
        {
            var amount = Math.Max(0, entry.Amount);
            switch (entry.Type)
            {
                case "NextShopGold":
                    State.DelayedShopResources.GoldBonus += amount;
                    State.LastRewardSummary = $"下个商店 +{amount} 金币";
                    break;
                case "FreeRefresh":
                    State.DelayedShopResources.FreeRefreshes += amount;
                    State.LastRewardSummary = $"下个商店 {amount} 次免费刷新";
                    break;
                case "UpgradeDiscount":
                    State.DelayedShopResources.UpgradeDiscount += amount;
                    State.LastRewardSummary = $"下次升级费用 -{amount}";
                    break;
                case "Spell":
                    QueueSpellReward(entry.CardId);
                    break;
                case "Minion":
                    QueueMinionReward(entry.CardId);
                    break;
            }
        }

        private void QueueSpellReward(string spellId)
        {
            if (string.IsNullOrWhiteSpace(spellId))
            {
                var candidates = configs.Spells
                    .Where(spell => spell.Enabled && spell.ShopEligible && spell.Tier <= Shop.TavernTier)
                    .ToList();
                if (candidates.Count == 0)
                {
                    State.LastRewardSummary = "没有可用法术奖励";
                    return;
                }

                spellId = candidates[rewardRandom.Next(candidates.Count)].Id;
            }

            rewardSequence++;
            State.EnqueueCardReward(new PendingCardReward(
                $"reward_{rewardSequence:D6}",
                ShopCardType.Spell,
                spellId));
            State.LastRewardSummary = $"法术奖励已暂存：{spellId}";
        }

        private void QueueMinionReward(string minionId)
        {
            MinionConfig minion;
            if (string.IsNullOrWhiteSpace(minionId))
            {
                minion = Shop.MinionPool.Draw(Shop.TavernTier, rewardRandom);
            }
            else if (!configs.TryGetMinion(minionId, out minion) ||
                     !Shop.MinionPool.TryReserveCopies(minionId, 1))
            {
                minion = null;
            }

            if (minion == null)
            {
                State.LastRewardSummary = "随从奖励牌池不足";
                return;
            }

            rewardSequence++;
            State.EnqueueCardReward(new PendingCardReward(
                $"reward_{rewardSequence:D6}",
                ShopCardType.Minion,
                minion.Id,
                1));
            State.LastRewardSummary = $"随从奖励已暂存：{minion.Name}";
        }

        private void ReleaseCandidateReservation(RewardCandidate candidate)
        {
            if (candidate.Type == "Minion" && candidate.ReservedPoolCopies > 0)
            {
                Shop.MinionPool.ReturnCopies(candidate.CardId, candidate.ReservedPoolCopies);
            }
        }

        private void ReleaseRewardReservation(PendingCardReward reward)
        {
            if (reward.CardType == ShopCardType.Minion && reward.ReservedPoolCopies > 0)
            {
                Shop.MinionPool.ReturnCopies(reward.ConfigId, reward.ReservedPoolCopies);
            }
        }

        private static RunOperationError MapShopError(ShopOperationError error)
        {
            switch (error)
            {
                case ShopOperationError.BenchFull:
                    return RunOperationError.BenchFull;
                case ShopOperationError.InvalidTarget:
                    return RunOperationError.InvalidTarget;
                case ShopOperationError.NoBenefit:
                    return RunOperationError.NoBenefit;
                default:
                    return RunOperationError.InvalidTiming;
            }
        }

        private void FillEncounterEnemy(
            IList<BattleMinionRuntime> enemyRow,
            EncounterConfig encounter)
        {
            foreach (var slot in encounter.EnemySlots ?? new List<EnemySlotConfig>())
            {
                if (slot.Slot < 0 || slot.Slot >= enemyRow.Count ||
                    !configs.TryGetMinion(slot.MinionId, out var config))
                {
                    continue;
                }

                enemyRow[slot.Slot] = new BattleMinionRuntime(
                    config,
                    slot.Golden,
                    permanentAttackBonus: slot.AttackBonus,
                    permanentHealthBonus: slot.HealthBonus,
                    permanentKeywords: slot.PermanentKeywords);
            }
        }

        private void FillDefaultEnemy(IList<BattleMinionRuntime> enemyRow)
        {
            var lineupIndex = Math.Min(Math.Max(Shop.Round - 1, 0), DefaultEnemyLineups.Length - 1);
            var ids = DefaultEnemyLineups[lineupIndex];
            for (var i = 0; i < ids.Length && i < enemyRow.Count; i++)
            {
                if (configs.TryGetMinion(ids[i], out var config))
                {
                    enemyRow[i] = new BattleMinionRuntime(config);
                }
            }
        }
    }
}

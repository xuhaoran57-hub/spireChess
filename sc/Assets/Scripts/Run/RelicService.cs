using System;
using System.Collections.Generic;
using System.Linq;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Shop;

namespace SpireChess.Run
{
    public sealed class RelicService
    {
        private static readonly HashSet<string> FormalRaces = new HashSet<string>(
            new[] { "ForgeSoul", "WildSpirit", "Starbound", "Wayfarer" },
            StringComparer.Ordinal);

        private readonly ConfigService configs;
        private readonly RunState state;
        private readonly ShopSession shop;
        private readonly Random random;
        private int choiceSequence;
        private int candidateSequence;

        public RelicService(
            ConfigService configs,
            RunState state,
            ShopSession shop,
            Random random)
        {
            this.configs = configs ?? throw new ArgumentNullException(nameof(configs));
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.shop = shop ?? throw new ArgumentNullException(nameof(shop));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public event Action<RelicActivationData> Activated;

        public bool Enabled => configs.Relics.Count > 0;
        internal RecordedRandom RandomStream => random as RecordedRandom;
        internal int ChoiceSequence => choiceSequence;
        internal int CandidateSequence => candidateSequence;

        internal void RestoreSequences(int restoredChoiceSequence, int restoredCandidateSequence)
        {
            choiceSequence = restoredChoiceSequence;
            candidateSequence = restoredCandidateSequence;
        }

        public bool HasAvailable(string grade)
        {
            var ownedIds = new HashSet<string>(state.OwnedRelics.Select(value => value.RelicId));
            return configs.Relics.Any(value => IsPlayable(value) &&
                value.Grade == grade && !ownedIds.Contains(value.Id));
        }

        public PendingRelicChoice CreateChoice(
            string grade,
            string sourceAttemptId,
            RelicCompletionMode completionMode,
            int healthCost,
            bool allowSkip)
        {
            var ownedIds = new HashSet<string>(state.OwnedRelics.Select(value => value.RelicId));
            var available = configs.Relics.Where(value => IsPlayable(value) &&
                    value.Grade == grade && !ownedIds.Contains(value.Id))
                .OrderBy(_ => random.Next())
                .ToList();
            if (available.Count == 0)
            {
                return null;
            }

            var selected = new List<RelicConfig>();
            foreach (var relic in available)
            {
                if (selected.Count >= 3)
                {
                    break;
                }

                if (selected.All(value => value.Category != relic.Category))
                {
                    selected.Add(relic);
                }
            }

            foreach (var relic in available)
            {
                if (selected.Count >= Math.Min(3, available.Count))
                {
                    break;
                }

                if (!selected.Contains(relic))
                {
                    selected.Add(relic);
                }
            }

            var candidates = selected.Select(value =>
            {
                candidateSequence++;
                return new RelicCandidate($"relic_candidate_{candidateSequence:D6}", value);
            }).ToList();
            choiceSequence++;
            return new PendingRelicChoice(
                $"relic_choice_{choiceSequence:D6}",
                sourceAttemptId,
                grade,
                completionMode,
                candidates,
                healthCost,
                allowSkip);
        }

        public RunOperationResult Acquire(
            PendingRelicChoice choice,
            string candidateId,
            out RelicConfig acquired)
        {
            acquired = null;
            if (choice == null || choice.SourceAttemptId != state.CurrentAttempt?.NodeAttemptId)
            {
                return RunOperationResult.Fail(RunOperationError.AttemptMismatch);
            }

            var candidate = choice.Candidates.FirstOrDefault(value =>
                value.CandidateId == candidateId);
            if (candidate == null || !configs.TryGetRelic(candidate.RelicId, out var config) ||
                !IsPlayable(config))
            {
                return RunOperationResult.Fail(RunOperationError.InvalidChoice);
            }

            if (state.OwnedRelics.Any(value => value.RelicId == config.Id))
            {
                return RunOperationResult.Fail(RunOperationError.InvalidChoice);
            }

            if (choice.HealthCost >= state.Health)
            {
                return RunOperationResult.Fail(RunOperationError.NoBenefit);
            }

            state.Health -= choice.HealthCost;
            state.AddOwnedRelic(new OwnedRelicState(
                config,
                choice.Grade == "Crown" ? "Boss" : "Event",
                state.CurrentAttempt?.ContentId,
                state.Floor,
                state.ShopTurn));
            acquired = config;
            return RunOperationResult.Succeed(config.Name);
        }

        public ShopRuleModifiers BuildShopRuleModifiers()
        {
            var result = new ShopRuleModifiers();
            foreach (var pair in GetOwnedConfigs())
            {
                switch (pair.config.EffectType)
                {
                    case "FirstPurchaseFree":
                        result.FirstPurchaseFree = true;
                        break;
                    case "FirstPaidRefreshFree":
                        result.FirstPaidRefreshFree = true;
                        break;
                    case "GoldOnFirstMinionSold":
                        result.FirstMinionSaleBonusGold += Math.Max(0, pair.config.Amount);
                        break;
                    case "ExtraBattlecryTriggers":
                        result.ExtraBattlecryTriggers += Math.Max(0, pair.config.Amount);
                        break;
                }
            }

            return result;
        }

        public IReadOnlyList<RelicCardGrant> ApplyShopStartEffects()
        {
            var grants = new List<RelicCardGrant>();
            var ownedConfigs = GetOwnedConfigs().ToList();
            foreach (var pair in ownedConfigs.Where(value =>
                         value.config.EffectType == "GoldOnShopStart"))
            {
                var owned = pair.owned;
                var config = pair.config;
                if (owned.LastResolvedShopTurn >= state.ShopTurn)
                {
                    continue;
                }

                owned.LastResolvedShopTurn = state.ShopTurn;
                if (state.Health * 100 <= state.MaxHealth * config.Threshold)
                {
                    shop.GrantGold(config.Amount);
                    RecordActivation(owned, "ShopStartGold", config.Amount);
                }
            }

            foreach (var pair in ownedConfigs.Where(value =>
                         value.config.EffectType == "GrantRandomSpellByShopInterval" ||
                         value.config.EffectType == "GrantRandomMinionByShopInterval"))
            {
                var owned = pair.owned;
                var config = pair.config;
                if (owned.LastResolvedShopTurn >= state.ShopTurn)
                {
                    continue;
                }

                owned.LastResolvedShopTurn = state.ShopTurn;
                var interval = Math.Max(1, config.Interval);
                owned.ShopProgress = Math.Min(interval, owned.ShopProgress + 1);
                if (owned.ShopProgress < interval)
                {
                    continue;
                }

                RelicCardGrant grant;
                if (config.EffectType == "GrantRandomSpellByShopInterval")
                {
                    grant = DrawSpellGrant(config);
                }
                else
                {
                    grant = DrawMinionGrant(config);
                }

                if (grant == null)
                {
                    continue;
                }

                owned.ShopProgress = 0;
                grants.Add(grant);
                RecordActivation(owned, "ShopCardGrant", 1, grant.ConfigId);
            }

            return grants;
        }

        public int ApplyVictoryHealing(RunNodeType nodeType)
        {
            if (nodeType != RunNodeType.Elite && nodeType != RunNodeType.Boss)
            {
                return 0;
            }

            var totalHealed = 0;
            foreach (var pair in GetOwnedConfigs().Where(value =>
                         value.config.EffectType == "HealAfterEliteOrBossVictory"))
            {
                var before = state.Health;
                state.Health = Math.Min(state.MaxHealth, state.Health + pair.config.Amount);
                var healed = state.Health - before;
                if (healed <= 0)
                {
                    continue;
                }

                totalHealed += healed;
                RecordActivation(pair.owned, "VictoryHeal", healed);
            }

            return totalHealed;
        }

        public void ApplyBattleRules(BattleBoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var shieldTargets = 0;
            var relicStartEffectCount = 0;
            foreach (var pair in GetOwnedConfigs())
            {
                var config = pair.config;
                switch (config.EffectType)
                {
                    case "ExtraDeathrattleTriggers":
                        board.RuleModifiers.PlayerExtraDeathrattleTriggers +=
                            Math.Max(0, config.Amount);
                        break;
                    case "GrantCombatShieldAtBattleStart":
                        shieldTargets += Math.Max(0, config.TargetCount);
                        break;
                    case "SummonOnFirstFriendlyNonTokenDeath":
                        board.RuleModifiers.PlayerFirstNonTokenDeathSummonCount =
                            Math.Max(0, config.Amount);
                        board.RuleModifiers.PlayerFirstNonTokenDeathTokenId = config.TokenId;
                        board.RuleModifiers.PlayerFirstNonTokenDeathTokenAttack =
                            Math.Max(0, config.Attack);
                        board.RuleModifiers.PlayerFirstNonTokenDeathTokenHealth =
                            Math.Max(0, config.Health);
                        break;
                }
            }

            if (shieldTargets > 0)
            {
                board.RuleModifiers.PlayerBattleStartShieldTargets = shieldTargets;
                board.BattleStartEffects.Insert(
                    relicStartEffectCount++,
                    new BattleStartEffectState(
                    BattleSide.Player,
                    new EffectConfig
                    {
                        Id = "relic_battle_start_shield",
                        Trigger = "OnBattleStart",
                        Action = "AddShield",
                        Target = new TargetConfig
                        {
                            Side = "Ally",
                            Scope = "Single",
                            IncludeSelf = true,
                            IncludeToken = true,
                            MaxTargets = shieldTargets,
                            Selector = "NoShieldLowestHealth"
                        },
                        Value = new ValueConfig { Duration = "Combat" }
                    }));
            }

            var banner = GetOwnedConfigs().FirstOrDefault(value =>
                value.config.EffectType == "GrantCombatStatsPerDistinctRace");
            if (banner.config == null)
            {
                return;
            }

            var distinctRaces = board.Player.Where(value => value != null &&
                    value.IsAlive && !value.Config.IsToken && FormalRaces.Contains(value.Config.Race))
                .Select(value => value.Config.Race)
                .Distinct()
                .Count();
            var amount = Math.Min(Math.Max(0, banner.config.Threshold), distinctRaces);
            if (amount <= 0)
            {
                return;
            }

            board.RuleModifiers.PlayerDistinctRaceStatBonus = amount;
            board.BattleStartEffects.Insert(
                relicStartEffectCount,
                new BattleStartEffectState(
                BattleSide.Player,
                new EffectConfig
                {
                    Id = "relic_manyfold_banner",
                    Trigger = "OnBattleStart",
                    Action = "ModifyStats",
                    Target = new TargetConfig
                    {
                        Side = "Ally",
                        Scope = "All",
                        IncludeSelf = true,
                        IncludeToken = true
                    },
                    Value = new ValueConfig
                    {
                        Attack = amount,
                        Health = amount,
                        Duration = "Combat"
                    }
                }));
        }

        private RelicCardGrant DrawSpellGrant(RelicConfig relic)
        {
            var eligible = configs.Spells.Where(value => value != null && value.Enabled &&
                    value.ImplementationStatus == "Playable" && value.ShopEligible &&
                    value.Tier >= 1 && value.Tier <= shop.TavernTier &&
                    value.Id != ShopSession.TripleDiscoveryRewardSpellId)
                .ToList();
            if (eligible.Count == 0)
            {
                return null;
            }

            var selected = eligible[random.Next(eligible.Count)];
            return new RelicCardGrant(relic.Id, ShopCardType.Spell, selected.Id, 0);
        }

        private RelicCardGrant DrawMinionGrant(RelicConfig relic)
        {
            var tier = relic.TierMode == "ExactCurrent"
                ? shop.TavernTier
                : relic.CardTier;
            var reserved = shop.MinionPool.ReserveDistinctAtTier(tier, 1, random);
            var selected = reserved.FirstOrDefault();
            return selected == null
                ? null
                : new RelicCardGrant(relic.Id, ShopCardType.Minion, selected.Id, 1);
        }

        private IEnumerable<(OwnedRelicState owned, RelicConfig config)> GetOwnedConfigs()
        {
            foreach (var owned in state.OwnedRelics)
            {
                if (configs.TryGetRelic(owned.RelicId, out var config) && IsPlayable(config))
                {
                    yield return (owned, config);
                }
            }
        }

        private void RecordActivation(
            OwnedRelicState owned,
            string trigger,
            int amount = 0,
            string cardId = null)
        {
            owned.ActivationCount++;
            Activated?.Invoke(new RelicActivationData(owned.RelicId, trigger, amount, cardId));
        }

        private static bool IsPlayable(RelicConfig config)
        {
            return config != null && config.Enabled && config.ImplementationStatus == "Playable";
        }
    }
}

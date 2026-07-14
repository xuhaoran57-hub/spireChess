using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SpireChess.Battle;
using SpireChess.Config;

namespace SpireChess.Tests
{
    public sealed class BattleSimulatorTests
    {
        [Test]
        public void NoTaunt_SelectsFromAllAliveTargets()
        {
            var state = CreateState();
            state.Player[0] = CreateMinion("attacker", 10, 100);
            state.Enemy[0] = CreateMinion("left", 0, 100);
            state.Enemy[4] = CreateMinion("right", 0, 100);

            var step = FirstAttack(new BattleSimulator(new SequenceRandom(1)).SimulatePlayback(state));

            Assert.That(step.TargetIndex, Is.EqualTo(4));
        }

        [Test]
        public void MultipleTaunts_SelectsOnlyFromAliveTaunts()
        {
            var state = CreateState();
            state.Player[0] = CreateMinion("attacker", 10, 100);
            state.Enemy[0] = CreateMinion("non_taunt", 0, 100);
            state.Enemy[1] = CreateMinion("left_taunt", 0, 100, "Taunt");
            state.Enemy[4] = CreateMinion("right_taunt", 0, 100, "Taunt");

            var step = FirstAttack(new BattleSimulator(new SequenceRandom(1)).SimulatePlayback(state));

            Assert.That(step.TargetIndex, Is.EqualTo(4));
        }

        [Test]
        public void NormalCleave_OddDamageCanGiveExtraPointToLeft()
        {
            var state = CreateCleaveState(false);

            var step = FirstAttack(new BattleSimulator(new SequenceRandom(0)).SimulatePlayback(state));

            Assert.That(step.BoardState.Enemy[1].CurrentHealth, Is.EqualTo(97));
            Assert.That(step.BoardState.Enemy[2].CurrentHealth, Is.EqualTo(95));
            Assert.That(step.BoardState.Enemy[3].CurrentHealth, Is.EqualTo(98));
            Assert.That(step.SplashTargetIndexes, Is.EqualTo(new[] { 1, 3 }));
        }

        [Test]
        public void NormalCleave_OddDamageCanGiveExtraPointToRight()
        {
            var state = CreateCleaveState(false);

            var step = FirstAttack(new BattleSimulator(new SequenceRandom(1)).SimulatePlayback(state));

            Assert.That(step.BoardState.Enemy[1].CurrentHealth, Is.EqualTo(98));
            Assert.That(step.BoardState.Enemy[3].CurrentHealth, Is.EqualTo(97));
        }

        [Test]
        public void NormalCleave_DoesNotTransferDamageFromEmptySide()
        {
            var state = CreateState();
            state.Player[0] = CreateMinion("cleave", 5, 100, "Cleave");
            state.Enemy[0] = CreateMinion("target", 0, 100, "Taunt");
            state.Enemy[1] = CreateMinion("right", 0, 100);

            var step = FirstAttack(new BattleSimulator(new SequenceRandom(0)).SimulatePlayback(state));

            Assert.That(step.BoardState.Enemy[1].CurrentHealth, Is.EqualTo(98));
        }

        [Test]
        public void GoldenCleave_DealsFullAttackDamageToBothSides()
        {
            var state = CreateCleaveState(true);

            var step = FirstAttack(new BattleSimulator(new SequenceRandom()).SimulatePlayback(state));

            Assert.That(step.BoardState.Enemy[1].CurrentHealth, Is.EqualTo(95));
            Assert.That(step.BoardState.Enemy[3].CurrentHealth, Is.EqualTo(95));
        }

        [Test]
        public void CleaveDamage_IsBlockedByShieldAndWrittenToLog()
        {
            var state = CreateCleaveState(false);
            state.Enemy[1] = CreateMinion("shielded", 0, 100, "Shield");

            var step = FirstAttack(new BattleSimulator(new SequenceRandom(0)).SimulatePlayback(state));

            Assert.That(step.BoardState.Enemy[1].CurrentHealth, Is.EqualTo(100));
            Assert.That(step.BoardState.Enemy[1].HasShield, Is.False);
            Assert.That(step.Messages.Any(message => message.Contains("溅射")), Is.True);
        }

        [Test]
        public void Deathrattle_SummonsTokenIntoOriginalSlot()
        {
            var tokenConfig = CreateConfig("token", 1, 1, true);
            var summonerConfig = CreateConfig("summoner", 0, 1);
            summonerConfig.Effects.Add(CreateSummonEffect("token", 1));
            var state = CreateState();
            state.Player[0] = new BattleMinionRuntime(summonerConfig);
            state.Enemy[0] = CreateMinion("enemy", 2, 100);

            var step = FirstAttack(CreateSimulator(tokenConfig).SimulatePlayback(state));

            Assert.That(step.BoardState.Player[0], Is.Not.Null);
            Assert.That(step.BoardState.Player[0].Id, Is.EqualTo("token"));
            Assert.That(step.Messages.Any(message => message.Contains("召唤了")), Is.True);
        }

        [Test]
        public void Deathrattle_UsesNearestRightSlotAfterOriginalSlot()
        {
            var tokenConfig = CreateConfig("token", 1, 1, true);
            var summonerConfig = CreateConfig("summoner", 0, 1);
            summonerConfig.Effects.Add(CreateSummonEffect("token", 2));
            var state = CreateState();
            state.Player[1] = new BattleMinionRuntime(summonerConfig);
            state.Player[4] = CreateMinion("ally", 0, 100);
            state.Enemy[0] = CreateMinion("enemy", 2, 100);

            var result = CreateSimulator(tokenConfig).SimulatePlayback(state);
            var completedSummonStep = result.Steps.First(step =>
                step.BoardState.Player[1]?.Id == "token" &&
                step.BoardState.Player[2]?.Id == "token");

            Assert.That(completedSummonStep.BoardState.Player[1]?.Id, Is.EqualTo("token"));
            Assert.That(completedSummonStep.BoardState.Player[2]?.Id, Is.EqualTo("token"));
        }

        [Test]
        public void Deathrattle_UsesNearestLeftSlotWhenRightSlotIsOccupied()
        {
            var tokenConfig = CreateConfig("token", 1, 1, true);
            var summonerConfig = CreateConfig("summoner", 0, 1);
            summonerConfig.Effects.Add(CreateSummonEffect("token", 2));
            var state = CreateState();
            state.Player[3] = new BattleMinionRuntime(summonerConfig);
            state.Player[4] = CreateMinion("ally", 0, 100);
            state.Enemy[0] = CreateMinion("enemy", 2, 100);

            var result = CreateSimulator(tokenConfig).SimulatePlayback(state);
            var completedSummonStep = result.Steps.First(step =>
                step.BoardState.Player[3]?.Id == "token" &&
                step.BoardState.Player[2]?.Id == "token");

            Assert.That(completedSummonStep.BoardState.Player[3]?.Id, Is.EqualTo("token"));
            Assert.That(completedSummonStep.BoardState.Player[2]?.Id, Is.EqualTo("token"));
        }

        [Test]
        public void SummonFailure_AppliesConfiguredFallbackToNonTokenAlly()
        {
            var tokenConfig = CreateConfig("token", 1, 1, true);
            var summonerConfig = CreateConfig("summoner", 0, 1);
            var summonEffect = CreateSummonEffect("token", 2);
            summonEffect.FallbackEffects.Add(new EffectConfig
            {
                Id = "fallback",
                Action = "ModifyStats",
                Target = new TargetConfig
                {
                    Side = "Ally",
                    Scope = "Single",
                    Race = "WildSpirit",
                    IncludeToken = false,
                    Selector = "Random"
                },
                Value = new ValueConfig { Attack = 1, Health = 1 }
            });
            summonerConfig.Effects.Add(summonEffect);
            var state = CreateState();
            state.Player[0] = new BattleMinionRuntime(summonerConfig);
            for (var i = 1; i < BattleBoardState.SlotCount; i++)
            {
                state.Player[i] = CreateMinion($"ally_{i}", 1, 10);
            }

            state.Enemy[0] = CreateMinion("enemy", 2, 100);

            var result = CreateSimulator(tokenConfig).SimulatePlayback(state);
            var fallbackStep = result.Steps.First(step =>
                step.Messages.Any(message => message.Contains("没有空位")));
            var allyAttack = fallbackStep.BoardState.Player
                .Where(minion => minion != null && !minion.Config.IsToken)
                .Sum(minion => minion.CurrentAttack);

            Assert.That(allyAttack, Is.EqualTo(5));
            Assert.That(fallbackStep.Messages.Any(message => message.Contains("没有空位")), Is.True);
        }

        [Test]
        public void SummonedToken_ImmediateAttackCreatesSeparatePlaybackStep()
        {
            var tokenConfig = CreateConfig("swift_token", 2, 1, true);
            tokenConfig.Effects.Add(new EffectConfig
            {
                Id = "immediate",
                Trigger = "OnSummon",
                Action = "ImmediateAttack"
            });
            var summonerConfig = CreateConfig("summoner", 0, 1);
            summonerConfig.Effects.Add(CreateSummonEffect("swift_token", 1));
            var state = CreateState();
            state.Player[0] = new BattleMinionRuntime(summonerConfig);
            state.Enemy[0] = CreateMinion("enemy", 2, 100);

            var result = CreateSimulator(tokenConfig).SimulatePlayback(state);
            var immediateStep = result.Steps.Single(
                step => step.Messages.Any(message => message.Contains("立即攻击")));

            Assert.That(immediateStep.AttackerSide, Is.EqualTo(BattleSide.Player));
            Assert.That(immediateStep.AttackerIndex, Is.EqualTo(0));
        }

        [Test]
        public void MultiSummon_ImmediateAttackCanFreeSlotForNextToken()
        {
            var tokenConfig = CreateConfig("swift_token", 2, 1, true);
            tokenConfig.Effects.Add(new EffectConfig
            {
                Id = "immediate",
                Trigger = "OnSummon",
                Action = "ImmediateAttack"
            });
            var summonerConfig = CreateConfig("summoner", 0, 1);
            summonerConfig.Effects.Add(CreateSummonEffect("swift_token", 2));
            var state = CreateState();
            state.Player[0] = new BattleMinionRuntime(summonerConfig);
            for (var i = 1; i < BattleBoardState.SlotCount; i++)
            {
                state.Player[i] = CreateMinion($"ally_{i}", 0, 100);
            }

            state.Enemy[0] = CreateMinion("enemy", 2, 100, "Taunt");

            var result = CreateSimulator(tokenConfig).SimulatePlayback(state);
            var summonIndexes = result.Log
                .Select((message, index) => new { message, index })
                .Where(entry => entry.message.Contains("召唤了 swift_token"))
                .Select(entry => entry.index)
                .ToList();
            var immediateIndexes = result.Log
                .Select((message, index) => new { message, index })
                .Where(entry => entry.message.Contains("swift_token 立即攻击"))
                .Select(entry => entry.index)
                .ToList();

            Assert.That(summonIndexes.Count, Is.EqualTo(2));
            Assert.That(immediateIndexes.Count, Is.EqualTo(2));
            Assert.That(summonIndexes[0], Is.LessThan(immediateIndexes[0]));
            Assert.That(immediateIndexes[0], Is.LessThan(summonIndexes[1]));
            Assert.That(summonIndexes[1], Is.LessThan(immediateIndexes[1]));
            Assert.That(result.Log.Any(message => message.Contains("没有空位")), Is.False);
        }

        [Test]
        public void SummonedToken_GetsNormalAttackInCurrentRound()
        {
            var tokenConfig = CreateConfig("token", 2, 100, true);
            var summonerConfig = CreateConfig("summoner", 0, 1, false, "Shield");
            summonerConfig.Effects.Add(CreateSummonEffect("token", 1));
            var state = CreateState();
            state.Player[0] = new BattleMinionRuntime(summonerConfig);
            state.Enemy[0] = CreateMinion("enemy", 2, 100);

            var result = CreateSimulator(tokenConfig).SimulatePlayback(state);
            var tokenAttackStepIndex = FindStepIndex(result,
                step => step.Messages.Any(message => message.Contains("玩家 token 攻击")));
            var secondRoundStepIndex = FindStepIndex(result,
                step => step.Messages.Any(message => message == "第 2 轮。"));

            Assert.That(tokenAttackStepIndex, Is.Not.EqualTo(-1));
            Assert.That(tokenAttackStepIndex < secondRoundStepIndex, Is.True);
        }

        [Test]
        public void ImmediateAttack_DoesNotConsumeCurrentRoundNormalAttack()
        {
            var tokenConfig = CreateConfig("swift_token", 2, 100, true);
            tokenConfig.Effects.Add(new EffectConfig
            {
                Id = "immediate",
                Trigger = "OnSummon",
                Action = "ImmediateAttack"
            });
            var summonerConfig = CreateConfig("summoner", 0, 1, false, "Shield");
            summonerConfig.Effects.Add(CreateSummonEffect("swift_token", 1));
            var state = CreateState();
            state.Player[0] = new BattleMinionRuntime(summonerConfig);
            state.Enemy[0] = CreateMinion("enemy", 2, 100);

            var result = CreateSimulator(tokenConfig).SimulatePlayback(state);
            var immediateStepIndex = FindStepIndex(result,
                step => step.Messages.Any(message => message.Contains("立即攻击")));
            var normalStepIndex = FindStepIndex(result,
                step => step.Messages.Any(message => message.Contains("玩家 swift_token 攻击")));
            var secondRoundStepIndex = FindStepIndex(result,
                step => step.Messages.Any(message => message == "第 2 轮。"));

            Assert.That(immediateStepIndex, Is.Not.EqualTo(-1));
            Assert.That(normalStepIndex, Is.Not.EqualTo(-1));
            Assert.That(immediateStepIndex < normalStepIndex, Is.True);
            Assert.That(normalStepIndex < secondRoundStepIndex, Is.True);
        }

        [Test]
        public void SubjectHealth_UsesCombatMaxHealthAfterLethalDamage()
        {
            var observerConfig = CreateConfig("observer", 0, 100, false, "Shield");
            observerConfig.Effects.Add(new EffectConfig
            {
                Id = "copy_subject_health",
                Trigger = "OnFriendlyDeath",
                Action = "ModifyStats",
                Target = new TargetConfig
                {
                    Side = "Ally",
                    Scope = "Self",
                    IncludeSelf = true
                },
                Value = new ValueConfig
                {
                    Duration = "Combat",
                    Resource = "SubjectHealth"
                },
                Limit = new LimitConfig { PerCombat = 1 }
            });
            var doomed = CreateMinion("doomed", 0, 7, "Taunt");
            doomed.AddTemporaryStats(0, 3, new List<string>());
            var state = CreateState();
            state.Player[0] = new BattleMinionRuntime(observerConfig);
            state.Player[1] = doomed;
            state.Enemy[0] = CreateMinion("enemy", 20, 100);

            var result = new BattleSimulator(new SequenceRandom()).SimulatePlayback(state);
            var deathStep = result.Steps.First(step => step.BoardState.Player[1] == null);

            Assert.That(deathStep.BoardState.Player[0].CurrentHealth, Is.EqualTo(110));
            Assert.That(deathStep.BoardState.Player[0].CombatMaxHealth, Is.EqualTo(110));
        }

        private static BattleBoardState CreateCleaveState(bool golden)
        {
            var state = CreateState();
            state.Player[0] = CreateMinion("cleave", 5, 100, golden, "Cleave");
            state.Enemy[1] = CreateMinion("left", 0, 100);
            state.Enemy[2] = CreateMinion("target", 0, 100, "Taunt");
            state.Enemy[3] = CreateMinion("right", 0, 100);
            return state;
        }

        private static BattleBoardState CreateState()
        {
            return new BattleBoardState();
        }

        private static BattleMinionRuntime CreateMinion(
            string id,
            int attack,
            int health,
            params string[] keywords)
        {
            return CreateMinion(id, attack, health, false, keywords);
        }

        private static BattleMinionRuntime CreateMinion(
            string id,
            int attack,
            int health,
            bool golden,
            params string[] keywords)
        {
            var config = CreateConfig(id, attack, health, false, keywords);
            return new BattleMinionRuntime(config, golden);
        }

        private static MinionConfig CreateConfig(
            string id,
            int attack,
            int health,
            bool isToken = false,
            params string[] keywords)
        {
            return new MinionConfig
            {
                Id = id,
                Name = id,
                Race = "WildSpirit",
                IsToken = isToken,
                Attack = attack,
                Health = health,
                GoldenAttack = attack,
                GoldenHealth = health,
                Keywords = new List<string>(keywords)
            };
        }

        private static EffectConfig CreateSummonEffect(string tokenId, int amount)
        {
            return new EffectConfig
            {
                Id = "summon",
                Trigger = "OnDeath",
                Action = "SummonToken",
                Value = new ValueConfig
                {
                    Amount = amount,
                    Resource = tokenId
                }
            };
        }

        private static BattleSimulator CreateSimulator(params MinionConfig[] configs)
        {
            var configsById = configs.ToDictionary(config => config.Id);
            return new BattleSimulator(
                new SequenceRandom(),
                id => configsById.TryGetValue(id, out var config) ? config : null);
        }

        private static BattleStep FirstAttack(BattleSimulationResult result)
        {
            return result.Steps.First(step => step.HasAttack);
        }

        private static int FindStepIndex(
            BattleSimulationResult result,
            Func<BattleStep, bool> predicate)
        {
            for (var i = 0; i < result.Steps.Count; i++)
            {
                if (predicate(result.Steps[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private sealed class SequenceRandom : Random
        {
            private readonly Queue<int> values;

            public SequenceRandom(params int[] values)
            {
                this.values = new Queue<int>(values);
            }

            public override int Next(int maxValue)
            {
                if (maxValue <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(maxValue));
                }

                var value = values.Count > 0 ? values.Dequeue() : 0;
                return Math.Abs(value % maxValue);
            }
        }
    }
}

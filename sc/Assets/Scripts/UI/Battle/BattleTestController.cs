using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SpireChess.App;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Run;
using SpireChess.Simulation;
using SpireChess.UI.Common;
using UnityEngine;

namespace SpireChess.UI.Battle
{
    public sealed class BattleTestController : MonoBehaviour
    {
        [SerializeField] private BattleScreenView screenView;

        private static readonly BattlePreset[] Presets =
        {
            new BattlePreset(
                "基础阵容",
                new[]
                {
                    "forge_soul_shield_squire",
                    "hearth_core_spark",
                    "young_deer_spirit",
                    "stargazing_apprentice",
                    "wandering_swordsman"
                },
                new[]
                {
                    "moss_mark_seedling",
                    "copper_ring_apprentice",
                    "glimmer_mage",
                    "rending_cub",
                    "forge_soul_shield_squire"
                }),
            new BattlePreset(
                "随机目标",
                new[] { "wandering_swordsman", null, null, null, null },
                new[] { "moss_mark_seedling", null, "glimmer_mage", null, "rending_cub" }),
            new BattlePreset(
                "多个嘲讽",
                new[] { "wandering_swordsman", null, null, null, null },
                new[]
                {
                    "moss_mark_seedling",
                    "forge_soul_shield_squire",
                    "glimmer_mage",
                    "shieldwall_furnace_keeper",
                    "rending_cub"
                }),
            new BattlePreset(
                "普通溅射",
                new[] { "formation_breaker_mercenary", null, null, null, null },
                new[]
                {
                    null,
                    "moss_mark_seedling",
                    "forge_soul_shield_squire",
                    "glimmer_mage",
                    null
                }),
            new BattlePreset(
                "金色溅射",
                new[] { "formation_breaker_mercenary", null, null, null, null },
                new[]
                {
                    null,
                    "hearth_core_spark",
                    "shieldwall_furnace_keeper",
                    "glimmer_mage",
                    null
                },
                new[] { 0 }),
            new BattlePreset(
                "亡语召唤",
                new[] { "young_deer_spirit", null, null, null, null },
                new[] { "wandering_swordsman", null, null, null, null }),
            new BattlePreset(
                "迅捷幼灵",
                new[] { "hundred_song_herd", null, null, null, null },
                new[] { "oathbroken_blade_soul", null, null, null, null }),
            new BattlePreset(
                "召唤失败",
                new[]
                {
                    "young_deer_spirit",
                    "young_deer_spirit",
                    "young_deer_spirit",
                    "young_deer_spirit",
                    "young_deer_spirit"
                },
                new[] { "oathbroken_blade_soul", null, null, null, null },
                new[] { 0, 1, 2, 3, 4 }),
            new BattlePreset(
                "v0.2狐群召唤",
                new[]
                {
                    "fox_den_matriarch",
                    "ten_thousand_hoof_surge",
                    "rending_cub",
                    "many_branch_invoker",
                    "moss_mark_seedling"
                },
                new[] { "mirrorsteel_duelist", null, null, null, null }),
            new BattlePreset(
                "v0.2金色狐群",
                new[]
                {
                    "fox_den_matriarch",
                    "ten_thousand_hoof_surge",
                    "vinecrown_priest",
                    null,
                    null
                },
                new[] { "mirrorsteel_duelist", null, null, null, null },
                new[] { 0 }),
            new BattlePreset(
                "v0.2格位复用",
                new[]
                {
                    "fox_den_matriarch",
                    "thousand_ring_tomb_guardian",
                    "moss_mark_seedling",
                    "root_devourer",
                    "many_branch_invoker"
                },
                new[] { "mirrorsteel_duelist", null, null, null, null }),
            new BattlePreset(
                "v0.2关键词复制",
                new[]
                {
                    "forge_soul_shield_squire",
                    "many_arts_apprentice",
                    "formation_breaker_mercenary",
                    null,
                    null
                },
                new[] { "mirrorsteel_duelist", null, null, null, null },
                new[] { 1 }),
            new BattlePreset(
                "v0.2金色盾链",
                new[]
                {
                    "undying_furnace_king",
                    "forge_soul_shield_squire",
                    "oathbroken_blade_soul",
                    "cinder_armor_arbiter",
                    "ember_engraver"
                },
                new[] { "mirrorsteel_duelist", "mercenary_shieldbearer", null, null, null },
                new[] { 0 }),
            new BattlePreset(
                "v0.2召唤反制",
                new[] { "pack_hunt_inspector", "mirrorsteel_duelist", null, null, null },
                new[]
                {
                    "hundred_song_herd",
                    "fox_den_matriarch",
                    "many_branch_invoker",
                    null,
                    null
                })
        };

        private BattleSimulator simulator;
        private readonly List<string> displayedLog = new List<string>();
        private BattleBoardState setupState;
        private BattleBoardState initialSetupState;
        private BattleBoardState displayedState;
        private Coroutine playbackCoroutine;
        private BattleSimulationResult lastResult;
        private bool battleRunning;
        private bool battleResolved;
        private bool runBattle;
        private string encounterName;
        private string returnSceneName;
        private int presetIndex;
        private string currentStatus;
        private float playbackSpeed = 1f;
        private bool skipPlaybackRequested;
        private bool battleCommitSaved = true;

        public bool IsBattleLocked => battleRunning || battleResolved;
        public bool IsRunBattle => runBattle;
        public BattleBoardState SetupState => setupState;
        public BattleSimulationResult LastResult => lastResult;
        public bool IsAttackAnimationPlaying =>
            screenView != null && screenView.IsAnimationPlaying;
        public bool IsLogScrollable =>
            screenView != null && screenView.IsLogScrollable;
        public string LogContents =>
            screenView == null ? string.Empty : screenView.LogContents;
        public float PlaybackSpeed => playbackSpeed;
        public bool UsesFormalView => screenView != null;

        private void Start()
        {
            if (GameApp.Instance == null || GameApp.Instance.Configs == null)
            {
                Debug.LogError("[BattleTest] GameApp is not ready. Open Boot once or wait for startup.");
                return;
            }
            if (screenView == null)
            {
                Debug.LogError("[BattleTest] Formal BattleScreenView is not configured.");
                return;
            }

            var configs = GameApp.Instance.Configs;
            var activeRun = GameApp.Instance.Run;
            var context = activeRun?.PendingBattle;
            BattleSimulationResult restoredResult = null;
            if (context == null &&
                activeRun?.LastBattleContext != null &&
                !string.IsNullOrWhiteSpace(activeRun.LastBattleContext.NodeAttemptId) &&
                (activeRun.State.Phase == RunPhase.BattleResult ||
                 activeRun.State.Phase == RunPhase.RunWon ||
                 activeRun.State.Phase == RunPhase.RunLost))
            {
                context = activeRun.LastBattleContext;
                restoredResult = activeRun.LastBattleResult;
            }

            simulator = new BattleSimulator(
                context?.BattleSeed.HasValue == true
                    ? new System.Random(context.BattleSeed.Value)
                    : new System.Random(),
                id =>
                {
                    MinionConfig config;
                    return configs.TryGetMinion(id, out config) ? config : null;
                });

            if (context != null)
            {
                runBattle = true;
                encounterName = context.EncounterName;
                setupState = context.BoardState.Clone();
            }
            else
            {
                setupState = BuildInitialState(configs, Presets[presetIndex]);
            }

            initialSetupState = setupState.Clone();
            displayedState = setupState.Clone();
            screenView.Bind(this);
            RunSystemMenuView.Attach(screenView, () => !battleRunning);
            if (restoredResult != null)
            {
                lastResult = restoredResult;
                displayedState = restoredResult.FinalState;
                battleResolved = true;
                returnSceneName = context.ReturnSceneName;
                SetLog(restoredResult.Log);
                currentStatus = BuildResultStatus(restoredResult);
            }
            else
            {
                SetLog(new[] { "拖拽同一阵营的卡牌可以交换站位。点击开始战斗逐步播放结算。" });
                currentStatus = BuildReadyStatus();
            }
            RenderFormalState();
        }

        public void MoveCard(BattleSide fromSide, int fromIndex, BattleSide toSide, int toIndex)
        {
            if (IsBattleLocked || battleResolved || fromSide != toSide)
            {
                RenderFormalState();
                return;
            }

            var row = setupState.GetRow(fromSide);
            var temp = row[fromIndex];
            row[fromIndex] = row[toIndex];
            row[toIndex] = temp;
            displayedState = setupState.Clone();
            if (runBattle && GameApp.Instance?.Run?.UpdatePendingBattleBoard(setupState) == true)
            {
                if (!GameApp.Instance.Persistence.CommitSuccessful(
                        GameApp.Instance.Run,
                        "BattleFormationChanged"))
                {
                    SetStatus("站位已更新，但尚未保存");
                    return;
                }
            }
            RenderFormalState();
            SetStatus("已调整站位");
        }

        public void StartBattle()
        {
            if (battleRunning || battleResolved)
            {
                return;
            }

            if (playbackCoroutine != null)
            {
                StopCoroutine(playbackCoroutine);
            }

            playbackCoroutine = StartCoroutine(PlayBattle());
        }

        private IEnumerator PlayBattle()
        {
            battleRunning = true;
            battleResolved = false;
            skipPlaybackRequested = false;
            displayedLog.Clear();
            displayedState = setupState.Clone();
            currentStatus = "战斗播放中";
            RenderFormalState();

            var result = simulator.SimulatePlayback(setupState);
            foreach (var playbackEvent in result.PlaybackEvents)
            {
                if (skipPlaybackRequested)
                {
                    break;
                }

                currentStatus = string.IsNullOrWhiteSpace(playbackEvent.Message)
                    ? "战斗播放中"
                    : playbackEvent.Message;
                if (!string.IsNullOrWhiteSpace(playbackEvent.Message))
                {
                    displayedLog.Add(playbackEvent.Message);
                }
                if (playbackEvent.Kind == BattlePlaybackEventKind.UnitDied)
                {
                    yield return screenView.PlayEvent(
                        playbackEvent,
                        playbackSpeed);
                    displayedState = playbackEvent.BoardState;
                    RenderFormalState();
                }
                else
                {
                    displayedState = playbackEvent.BoardState;
                    RenderFormalState();
                    yield return screenView.PlayEvent(
                        playbackEvent,
                        playbackSpeed);
                }
            }

            displayedState = result.FinalState;
            battleResolved = true;
            battleRunning = false;
            playbackCoroutine = null;
            SetLog(result.Log);
            currentStatus = BuildResultStatus(result);
            FinalizeBattle(result);
            RenderFormalState();
        }

        public void TogglePlaybackSpeed()
        {
            playbackSpeed = playbackSpeed > 1f ? 1f : 2f;
            RenderFormalState();
        }

        public void SkipPlayback()
        {
            if (battleRunning)
            {
                skipPlaybackRequested = true;
            }
        }

        public BattleSimulationResult ResolveImmediately()
        {
            if (simulator == null || battleRunning)
            {
                return null;
            }

            var result = simulator.Simulate(setupState);
            displayedLog.Clear();
            displayedLog.AddRange(result.Log);
            displayedState = result.FinalState;
            battleRunning = false;
            battleResolved = true;
            currentStatus = BuildResultStatus(result);
            FinalizeBattle(result);
            RenderFormalState();
            return result;
        }

        public void ResetBattle()
        {
            if (playbackCoroutine != null)
            {
                StopCoroutine(playbackCoroutine);
                playbackCoroutine = null;
            }

            setupState = runBattle
                ? initialSetupState.Clone()
                : BuildInitialState(GameApp.Instance.Configs, Presets[presetIndex]);
            displayedState = setupState.Clone();
            displayedLog.Clear();
            battleRunning = false;
            battleResolved = false;
            lastResult = null;
            returnSceneName = null;
            battleCommitSaved = true;
            if (runBattle && GameApp.Instance?.Run?.UpdatePendingBattleBoard(setupState) == true)
            {
                battleCommitSaved = GameApp.Instance.Persistence.CommitSuccessful(
                    GameApp.Instance.Run,
                    "BattleReset");
            }
            SetLog(new[]
            {
                runBattle
                    ? $"已重置遭遇：{encounterName}。"
                    : $"已重置测试阵容：{Presets[presetIndex].Name}。"
            });
            SetStatus(BuildReadyStatus());
        }

        public void NextPreset()
        {
            if (battleRunning || runBattle)
            {
                return;
            }

            presetIndex = (presetIndex + 1) % Presets.Length;
            ResetBattle();
            SetLog(new[] { $"已切换测试阵容：{Presets[presetIndex].Name}。" });
        }

        private void RunBatchSimulation()
        {
            if (battleRunning)
            {
                return;
            }

            const int simulationCount = 100;
            var runner = new BattleBatchRunner(id =>
                GameApp.Instance.Configs.TryGetMinion(id, out var minion)
                    ? minion
                    : null);
            var result = runner.Run(setupState, 1000, simulationCount);

            SetLog(new[]
            {
                $"预设：{Presets[presetIndex].Name}",
                $"固定种子 1000-1099：玩家 {result.PlayerWins} 胜，敌方 {result.EnemyWins} 胜，平局 {result.Draws} 场。"
            });
            SetStatus($"批量模拟完成 · {Presets[presetIndex].Name}");
        }

        private static BattleBoardState BuildInitialState(ConfigService configs, BattlePreset preset)
        {
            var state = new BattleBoardState();
            FillRow(state.Player, configs, preset.PlayerIds, preset.PlayerGoldenSlots);
            FillRow(state.Enemy, configs, preset.EnemyIds, preset.EnemyGoldenSlots);
            return state;
        }

        private static void FillRow(
            IList<BattleMinionRuntime> row,
            ConfigService configs,
            IReadOnlyList<string> ids,
            ISet<int> goldenSlots)
        {
            for (var i = 0; i < BattleBoardState.SlotCount; i++)
            {
                if (!string.IsNullOrEmpty(ids[i]) && configs.TryGetMinion(ids[i], out var config))
                {
                    row[i] = new BattleMinionRuntime(config, goldenSlots.Contains(i));
                }
            }
        }









        private void SetLog(IEnumerable<string> lines)
        {
            var values = (lines ?? Enumerable.Empty<string>()).ToList();
            if (!ReferenceEquals(lines, displayedLog))
            {
                displayedLog.Clear();
                displayedLog.AddRange(values);
            }
            RenderFormalState();
        }

        private void SetStatus(string status)
        {
            currentStatus = status ?? string.Empty;
            RenderFormalState();
        }


        private static string BuildResultStatus(BattleSimulationResult result)
        {
            if (!result.Winner.HasValue)
            {
                return result.OutcomeReason == BattleOutcomeReason.RoundLimit
                    ? "战斗结束：达到回合上限，平局"
                    : "战斗结束：双方同时倒下，平局";
            }

            return result.Winner.Value == BattleSide.Player
                ? "战斗结束：玩家胜利"
                : "战斗结束：敌方胜利";
        }


        private string BuildReadyStatus()
        {
            return runBattle
                ? $"等待战斗 · {encounterName}"
                : $"准备阶段 · {Presets[presetIndex].Name}";
        }

        private void RenderFormalState()
        {
            if (screenView == null || displayedState == null)
            {
                return;
            }

            screenView.Render(BattleScreenStateBuilder.Build(
                displayedState,
                runBattle
                    ? $"战斗 · {encounterName}"
                    : $"战斗测试 · {Presets[presetIndex].Name}",
                currentStatus ?? BuildReadyStatus(),
                displayedLog,
                runBattle,
                battleRunning,
                battleResolved,
                playbackSpeed));
        }

        private void FinalizeBattle(BattleSimulationResult result)
        {
            lastResult = result;
            if (!runBattle)
            {
                returnSceneName = "ShopTest";
                battleCommitSaved = true;
                return;
            }

            if (GameApp.Instance?.Run == null)
            {
                return;
            }

            if (!GameApp.Instance.Run.TryCompleteBattle(result, out returnSceneName))
            {
                battleCommitSaved = false;
                SetStatus("战斗结算未提交，请勿离开当前界面");
                return;
            }

            battleCommitSaved = GameApp.Instance.Persistence.CommitSuccessful(
                GameApp.Instance.Run,
                "BattleCompleted");
            if (!battleCommitSaved)
            {
                SetStatus("战斗已结算，但尚未保存；请稍后重试");
            }
        }

        public void ReturnToFlow()
        {
            if (!battleResolved || !battleCommitSaved || string.IsNullOrEmpty(returnSceneName))
            {
                return;
            }

            if (runBattle)
            {
                var run = GameApp.Instance.Run;
                if (string.IsNullOrWhiteSpace(run.LastBattleContext?.NodeAttemptId) &&
                    GameSceneNames.TryParse(returnSceneName, out var legacyReturnScene))
                {
                    GameApp.Instance.Router.GoTo(legacyReturnScene);
                }
                else
                {
                    GameApp.Instance.Router.GoToCurrentRunPhase(run);
                }
            }
            else
            {
                GameApp.Instance.Router.GoTo(GameSceneId.Shop);
            }
        }

        private sealed class BattlePreset
        {
            public BattlePreset(
                string name,
                string[] playerIds,
                string[] enemyIds,
                IEnumerable<int> playerGoldenSlots = null,
                IEnumerable<int> enemyGoldenSlots = null)
            {
                Name = name;
                PlayerIds = playerIds;
                EnemyIds = enemyIds;
                PlayerGoldenSlots = new HashSet<int>(playerGoldenSlots ?? new int[0]);
                EnemyGoldenSlots = new HashSet<int>(enemyGoldenSlots ?? new int[0]);
            }

            public string Name { get; }
            public IReadOnlyList<string> PlayerIds { get; }
            public IReadOnlyList<string> EnemyIds { get; }
            public ISet<int> PlayerGoldenSlots { get; }
            public ISet<int> EnemyGoldenSlots { get; }
        }
    }

}

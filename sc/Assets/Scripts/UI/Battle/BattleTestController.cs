using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SpireChess.App;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Run;
using SpireChess.Simulation;
using SpireChess.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpireChess.UI.Battle
{
    public sealed class BattleTestController : MonoBehaviour
    {
        [SerializeField] private BattleScreenView screenView;

        private static readonly Color BackgroundColor = new Color(0.08f, 0.09f, 0.11f, 1f);
        private static readonly Color PanelColor = new Color(0.14f, 0.15f, 0.17f, 0.94f);
        private static readonly Color PlayerSlotColor = new Color(0.17f, 0.25f, 0.28f, 1f);
        private static readonly Color EnemySlotColor = new Color(0.28f, 0.18f, 0.20f, 1f);
        private static readonly Color SlotOutlineColor = new Color(1f, 1f, 1f, 0.18f);
        private static readonly Color AttackerOutlineColor = new Color(1f, 0.78f, 0.18f, 1f);
        private static readonly Color TargetOutlineColor = new Color(1f, 0.28f, 0.24f, 1f);
        private static readonly Color SplashOutlineColor = new Color(0.95f, 0.48f, 0.12f, 1f);
        private static readonly Color ImpactColor = new Color(1f, 0.32f, 0.26f, 1f);
        private const float AttackWindupDuration = 0.10f;
        private const float AttackLungeDuration = 0.16f;
        private const float AttackImpactDuration = 0.18f;
        private const float AttackReturnDuration = 0.16f;
        private const float AttackLungeDistance = 82f;
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
        private readonly Dictionary<string, Transform> slotContentRoots = new Dictionary<string, Transform>();
        private readonly Dictionary<string, Outline> slotOutlines = new Dictionary<string, Outline>();
        private readonly List<string> displayedLog = new List<string>();
        private BattleBoardState setupState;
        private BattleBoardState initialSetupState;
        private BattleBoardState displayedState;
        private Canvas canvas;
        private Text logText;
        private ScrollRect logScrollRect;
        private Text statusText;
        private Button startButton;
        private Button returnButton;
        private Coroutine playbackCoroutine;
        private BattleStep activeStep;
        private BattleSimulationResult lastResult;
        private bool battleRunning;
        private bool battleResolved;
        private bool attackAnimationPlaying;
        private bool runBattle;
        private string encounterName;
        private string returnSceneName;
        private int presetIndex;
        private static Font uiFont;
        private string currentStatus;
        private float playbackSpeed = 1f;
        private bool skipPlaybackRequested;

        public bool IsBattleLocked => battleRunning || battleResolved;
        public bool IsRunBattle => runBattle;
        public BattleBoardState SetupState => setupState;
        public BattleSimulationResult LastResult => lastResult;
        public bool IsAttackAnimationPlaying =>
            screenView != null ? screenView.IsAnimationPlaying : attackAnimationPlaying;
        public bool IsLogScrollable =>
            screenView != null ? screenView.IsLogScrollable :
            logScrollRect != null && logScrollRect.vertical;
        public string LogContents =>
            screenView != null ? screenView.LogContents :
            logText == null ? string.Empty : logText.text;
        public float PlaybackSpeed => playbackSpeed;
        public bool UsesFormalView => screenView != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            TryCreateForActiveScene();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryCreateForActiveScene();
        }

        private static void TryCreateForActiveScene()
        {
            if (SceneManager.GetActiveScene().name != "BattleTest")
            {
                return;
            }

            if (FindObjectOfType<BattleTestController>() != null)
            {
                return;
            }

            new GameObject("BattleTestController").AddComponent<BattleTestController>();
        }

        private void Start()
        {
            if (GameApp.Instance == null || GameApp.Instance.Configs == null)
            {
                Debug.LogError("[BattleTest] GameApp is not ready. Open Boot once or wait for startup.");
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
            if (screenView == null)
            {
                BuildUi();
            }
            else
            {
                screenView.Bind(this);
            }
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
            if (screenView == null)
            {
                RebuildCards();
                SetStatus(currentStatus);
                if (restoredResult != null)
                {
                    startButton.interactable = false;
                    returnButton.gameObject.SetActive(true);
                }
            }
            else
            {
                RenderFormalState();
            }
        }

        public void MoveCard(BattleSide fromSide, int fromIndex, BattleSide toSide, int toIndex)
        {
            if (IsBattleLocked || battleResolved || fromSide != toSide)
            {
                RebuildCards();
                return;
            }

            var row = setupState.GetRow(fromSide);
            var temp = row[fromIndex];
            row[fromIndex] = row[toIndex];
            row[toIndex] = temp;
            displayedState = setupState.Clone();
            RebuildCards();
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
            activeStep = null;
            displayedLog.Clear();
            displayedState = setupState.Clone();
            currentStatus = "战斗播放中";
            RenderFormalState();

            var result = simulator.SimulatePlayback(setupState);
            if (screenView == null)
            {
                foreach (var step in result.Steps)
                {
                    activeStep = step;
                    if (step.HasAttack)
                    {
                        UpdateSlotHighlights();
                        SetStatus(BuildStepStatus(step));
                        yield return PlayAttackAnimation(step);
                    }
                    displayedState = step.BoardState;
                    displayedLog.AddRange(step.Messages);
                    RebuildCards();
                    SetLog(displayedLog);
                    SetStatus(BuildStepStatus(step));
                    yield return new WaitForSeconds(
                        (step.HasAttack ? 0.22f : 0.32f) / playbackSpeed);
                }
            }
            else
            {
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
            }

            activeStep = null;
            attackAnimationPlaying = false;
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
            activeStep = null;
            attackAnimationPlaying = false;
            battleRunning = false;
            battleResolved = true;
            RebuildCards();
            SetLog(displayedLog);
            SetStatus(BuildResultStatus(result));
            FinalizeBattle(result);
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
            activeStep = null;
            attackAnimationPlaying = false;
            displayedLog.Clear();
            battleRunning = false;
            battleResolved = false;
            lastResult = null;
            returnSceneName = null;
            if (startButton != null)
            {
                startButton.interactable = true;
            }
            if (returnButton != null)
            {
                returnButton.gameObject.SetActive(false);
            }
            RebuildCards();
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

        private void BuildUi()
        {
            EnsureEventSystem();

            canvas = CreateCanvas();
            var root = CreateRect("BattleTestRoot", canvas.transform);
            Stretch(root);
            AddImage(root.gameObject, BackgroundColor);

            var top = CreatePanel("TopBar", root, PanelColor);
            Anchor(top, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -76f), Vector2.zero);

            var title = CreateText("Title", top, "BattleTest", 32, TextAnchor.MiddleLeft);
            Anchor(title.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(28f, 0f), new Vector2(360f, 0f));

            statusText = CreateText("Status", top, string.Empty, 22, TextAnchor.MiddleLeft);
            Anchor(statusText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(280f, 0f), new Vector2(520f, 0f));

            startButton = CreateButton("StartBattleButton", top, "开始战斗", StartBattle);
            Anchor(startButton.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-520f, -25f), new Vector2(-380f, 25f));

            var batchButton = CreateButton("BatchButton", top, "模拟100场", RunBatchSimulation);
            Anchor(batchButton.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-680f, -25f), new Vector2(-540f, 25f));
            batchButton.gameObject.SetActive(!runBattle);

            var presetButton = CreateButton("PresetButton", top, "切换预设", NextPreset);
            Anchor(presetButton.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-360f, -25f), new Vector2(-220f, 25f));
            presetButton.gameObject.SetActive(!runBattle);

            var resetButton = CreateButton("ResetButton", top, "重置", ResetBattle);
            Anchor(resetButton.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-200f, -25f), new Vector2(-80f, 25f));
            resetButton.gameObject.SetActive(!runBattle);

            returnButton = CreateButton(
                "ReturnButton",
                top,
                runBattle ? "查看结算" : "返回商店",
                ReturnToFlow);
            Anchor(returnButton.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-360f, -25f), new Vector2(-220f, 25f));
            returnButton.gameObject.SetActive(false);

            var board = CreatePanel("Board", root, new Color(0f, 0f, 0f, 0f));
            Anchor(board, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(24f, 24f), new Vector2(-430f, -100f));

            var logPanel = CreatePanel("LogPanel", root, PanelColor);
            Anchor(logPanel, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-400f, 24f), new Vector2(-24f, -100f));

            var logTitle = CreateText("LogTitle", logPanel, "战斗日志", 24, TextAnchor.MiddleLeft);
            Anchor(logTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -52f), new Vector2(-18f, -12f));

            var logScroll = CreateRect("LogScroll", logPanel);
            Anchor(logScroll, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(18f, 18f), new Vector2(-18f, -64f));
            logScrollRect = logScroll.gameObject.AddComponent<ScrollRect>();
            logScrollRect.horizontal = false;
            logScrollRect.vertical = true;
            logScrollRect.movementType = ScrollRect.MovementType.Clamped;
            logScrollRect.scrollSensitivity = 28f;

            var viewport = CreatePanel(
                "Viewport",
                logScroll,
                new Color(0f, 0f, 0f, 0.01f));
            Stretch(viewport);
            var mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            logScrollRect.viewport = viewport;

            logText = CreateText("LogText", viewport, string.Empty, 17, TextAnchor.UpperLeft);
            logText.horizontalOverflow = HorizontalWrapMode.Wrap;
            logText.verticalOverflow = VerticalWrapMode.Overflow;
            logText.rectTransform.anchorMin = new Vector2(0f, 1f);
            logText.rectTransform.anchorMax = new Vector2(1f, 1f);
            logText.rectTransform.pivot = new Vector2(0.5f, 1f);
            logText.rectTransform.anchoredPosition = Vector2.zero;
            logText.rectTransform.sizeDelta = Vector2.zero;
            var fitter = logText.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            logScrollRect.content = logText.rectTransform;

            CreateRow(board, BattleSide.Enemy, "敌方", EnemySlotColor, 0.70f);
            CreateRow(board, BattleSide.Player, "玩家", PlayerSlotColor, 0.16f);
        }

        private void CreateRow(
            RectTransform board,
            BattleSide side,
            string label,
            Color slotColor,
            float yMin)
        {
            var rowPanel = CreatePanel(side + "Row", board, new Color(0f, 0f, 0f, 0f));
            Anchor(rowPanel, new Vector2(0f, yMin), new Vector2(1f, yMin + 0.25f), new Vector2(0f, 0f), Vector2.zero);

            var rowLabel = CreateText(label + "Label", rowPanel, label, 22, TextAnchor.MiddleLeft);
            Anchor(rowLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(4f, -38f), new Vector2(150f, -2f));

            var slotsRoot = CreateRect(side + "Slots", rowPanel);
            Anchor(slotsRoot, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), Vector2.zero);
            var layout = slotsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 18f;
            layout.padding = new RectOffset(8, 8, 30, 0);
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            for (var i = 0; i < BattleBoardState.SlotCount; i++)
            {
                var slot = CreateSlot(slotsRoot, slotColor, side, i);
                slotContentRoots[BuildSlotKey(side, i)] = slot.Find("Content");
            }
        }

        private RectTransform CreateSlot(RectTransform parent, Color slotColor, BattleSide side, int index)
        {
            var slot = CreatePanel($"{side}Slot{index + 1}", parent, slotColor);
            slot.sizeDelta = new Vector2(178f, 236f);
            var slotView = slot.gameObject.AddComponent<BattleSlotView>();
            slotView.Initialize(this, side, index);

            var outline = slot.gameObject.AddComponent<Outline>();
            outline.effectColor = SlotOutlineColor;
            outline.effectDistance = new Vector2(2f, -2f);
            slotOutlines[BuildSlotKey(side, index)] = outline;

            var label = CreateText("SlotLabel", slot, (index + 1).ToString(), 16, TextAnchor.UpperRight);
            label.color = new Color(1f, 1f, 1f, 0.55f);
            Anchor(label.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(8f, 8f), new Vector2(-8f, -8f));

            var content = CreateRect("Content", slot);
            Stretch(content);
            return slot;
        }

        private void RebuildCards()
        {
            if (screenView != null)
            {
                RenderFormalState();
                return;
            }

            foreach (var root in slotContentRoots.Values)
            {
                for (var i = root.childCount - 1; i >= 0; i--)
                {
                    Destroy(root.GetChild(i).gameObject);
                }
            }

            BuildCardsForRow(BattleSide.Enemy);
            BuildCardsForRow(BattleSide.Player);
            UpdateSlotHighlights();
        }

        private void BuildCardsForRow(BattleSide side)
        {
            var row = displayedState.GetRow(side);
            for (var i = 0; i < BattleBoardState.SlotCount; i++)
            {
                var minion = row[i];
                if (minion == null)
                {
                    continue;
                }

                var root = slotContentRoots[BuildSlotKey(side, i)];
                var card = CreateCard(root, side, i, minion);
                card.Render(minion);
            }
        }

        private BattleCardView CreateCard(Transform parent, BattleSide side, int index, BattleMinionRuntime minion)
        {
            var cardRect = CreatePanel(
                "Card",
                parent,
                CardTierPalette.GetBackground(minion.Config.Tier));
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(158f, 212f);
            cardRect.anchoredPosition = Vector2.zero;

            var outline = cardRect.gameObject.AddComponent<Outline>();
            outline.effectColor = minion.HasTaunt
                ? new Color(0.95f, 0.72f, 0.25f, 1f)
                : new Color(0.2f, 0.16f, 0.1f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);

            var header = CreatePanel(
                "Header",
                cardRect,
                CardTierPalette.GetHeader(minion.Config.Tier));
            Anchor(header, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -42f), Vector2.zero);

            var name = CreateText("Name", cardRect, string.Empty, 18, TextAnchor.MiddleCenter);
            name.fontStyle = FontStyle.Bold;
            name.color = Color.white;
            Anchor(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -40f), new Vector2(-8f, -4f));

            var tier = CreateText("Tier", cardRect, string.Empty, 15, TextAnchor.MiddleLeft);
            tier.color = new Color(0.12f, 0.1f, 0.07f, 1f);
            Anchor(tier.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -70f), new Vector2(62f, -46f));

            var race = CreateText("Race", cardRect, string.Empty, 15, TextAnchor.MiddleRight);
            race.color = new Color(0.12f, 0.1f, 0.07f, 1f);
            Anchor(race.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-82f, -70f), new Vector2(-8f, -46f));

            var keywords = CreateText("Keywords", cardRect, string.Empty, 14, TextAnchor.MiddleCenter);
            keywords.color = new Color(0.42f, 0.18f, 0.08f, 1f);
            keywords.horizontalOverflow = HorizontalWrapMode.Overflow;
            Anchor(keywords.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -96f), new Vector2(-8f, -72f));

            var description = CreateText("Description", cardRect, string.Empty, 13, TextAnchor.UpperCenter);
            description.color = new Color(0.12f, 0.1f, 0.07f, 1f);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.verticalOverflow = VerticalWrapMode.Truncate;
            Anchor(description.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(10f, 44f), new Vector2(-10f, -102f));

            var statsBg = CreatePanel("StatsBadge", cardRect, new Color(0.16f, 0.13f, 0.1f, 1f));
            Anchor(statsBg, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-42f, 8f), new Vector2(42f, 40f));

            var stats = CreateText("Stats", cardRect, string.Empty, 20, TextAnchor.MiddleCenter);
            stats.fontStyle = FontStyle.Bold;
            stats.color = Color.white;
            Anchor(stats.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-42f, 8f), new Vector2(42f, 40f));

            var shield = CreatePanel("Shield", cardRect, new Color(0.18f, 0.45f, 0.95f, 0.88f));
            Anchor(shield, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-42f, 8f), new Vector2(-8f, 40f));
            var shieldText = CreateText("ShieldText", shield, "盾", 14, TextAnchor.MiddleCenter);
            shieldText.color = Color.white;
            Stretch(shieldText.rectTransform);

            var view = cardRect.gameObject.AddComponent<BattleCardView>();
            cardRect.gameObject.AddComponent<CanvasGroup>();
            view.Initialize(this, canvas, side, index, true);
            return view;
        }

        private static Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("BattleTestCanvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<GraphicRaycaster>();
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            var rect = CreateRect(name, parent);
            AddImage(rect.gameObject, color);
            return rect;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string text,
            int fontSize,
            TextAnchor alignment)
        {
            var rect = CreateRect(name, parent);
            var textComponent = rect.gameObject.AddComponent<Text>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.alignment = alignment;
            textComponent.color = Color.white;
            textComponent.raycastTarget = false;
            var font = GetUiFont();
            if (font != null)
            {
                textComponent.font = font;
            }

            return textComponent;
        }

        private static Font GetUiFont()
        {
            if (uiFont != null)
            {
                return uiFont;
            }

            uiFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "SimHei", "Arial" },
                18);
            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return uiFont;
        }

        private static Button CreateButton(string name, Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            var rect = CreatePanel(name, parent, new Color(0.26f, 0.36f, 0.44f, 1f));
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(action);

            var text = CreateText("Label", rect, label, 20, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
        }

        private static Image AddImage(GameObject gameObject, Color color)
        {
            var image = gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Anchor(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private void SetLog(IEnumerable<string> lines)
        {
            var values = (lines ?? Enumerable.Empty<string>()).ToList();
            if (!ReferenceEquals(lines, displayedLog))
            {
                displayedLog.Clear();
                displayedLog.AddRange(values);
            }
            if (screenView != null)
            {
                RenderFormalState();
                return;
            }

            logText.text = string.Join("\n", values);
            Canvas.ForceUpdateCanvases();
            if (logScrollRect != null)
            {
                logScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void SetStatus(string status)
        {
            currentStatus = status ?? string.Empty;
            if (screenView != null)
            {
                RenderFormalState();
                return;
            }
            statusText.text = status;
        }

        private void UpdateSlotHighlights()
        {
            foreach (var outline in slotOutlines.Values)
            {
                outline.effectColor = SlotOutlineColor;
                outline.effectDistance = new Vector2(2f, -2f);
            }

            if (activeStep == null || !activeStep.HasAttack)
            {
                return;
            }

            SetSlotHighlight(
                activeStep.AttackerSide.Value,
                activeStep.AttackerIndex,
                AttackerOutlineColor,
                new Vector2(4f, -4f));
            SetSlotHighlight(
                activeStep.TargetSide.Value,
                activeStep.TargetIndex,
                TargetOutlineColor,
                new Vector2(4f, -4f));

            foreach (var splashTargetIndex in activeStep.SplashTargetIndexes)
            {
                SetSlotHighlight(
                    activeStep.TargetSide.Value,
                    splashTargetIndex,
                    SplashOutlineColor,
                    new Vector2(4f, -4f));
            }
        }

        private IEnumerator PlayAttackAnimation(BattleStep step)
        {
            var attacker = FindCardRect(step.AttackerSide.Value, step.AttackerIndex);
            var target = FindCardRect(step.TargetSide.Value, step.TargetIndex);
            if (attacker == null || target == null)
            {
                yield return new WaitForSeconds(0.35f);
                yield break;
            }

            attackAnimationPlaying = true;
            var attackerPosition = attacker.anchoredPosition;
            var targetPosition = target.anchoredPosition;
            var attackerScale = attacker.localScale;
            var targetScale = target.localScale;
            var targetImage = target.GetComponent<Image>();
            var targetOutline = target.GetComponent<Outline>();
            var targetColor = targetImage == null
                ? CardTierPalette.GetBackground(1)
                : targetImage.color;
            var targetOutlineColor = targetOutline == null
                ? TargetOutlineColor
                : targetOutline.effectColor;
            var targetOutlineDistance = targetOutline == null
                ? Vector2.zero
                : targetOutline.effectDistance;

            var worldDirection = (target.position - attacker.position).normalized;
            var localDirection = attacker.parent.InverseTransformVector(worldDirection);
            var direction = new Vector2(localDirection.x, localDirection.y).normalized;
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = step.AttackerSide == BattleSide.Player
                    ? Vector2.up
                    : Vector2.down;
            }

            var lungePosition = attackerPosition + direction * AttackLungeDistance;
            attacker.SetAsLastSibling();

            yield return AnimatePhase(AttackWindupDuration, progress =>
            {
                if (attacker != null)
                {
                    attacker.localScale = Vector3.Lerp(
                        attackerScale,
                        attackerScale * 1.08f,
                        Smooth(progress));
                }
            });

            yield return AnimatePhase(AttackLungeDuration, progress =>
            {
                if (attacker != null)
                {
                    attacker.anchoredPosition = Vector2.Lerp(
                        attackerPosition,
                        lungePosition,
                        Smooth(progress));
                }
            });

            if (targetOutline != null)
            {
                targetOutline.effectColor = ImpactColor;
                targetOutline.effectDistance = new Vector2(7f, -7f);
            }

            yield return AnimatePhase(AttackImpactDuration, progress =>
            {
                if (target == null)
                {
                    return;
                }

                var fade = Smooth(progress);
                var shake = Mathf.Sin(progress * Mathf.PI * 6f) *
                            (1f - progress) * 9f;
                target.anchoredPosition = targetPosition + Vector2.right * shake;
                target.localScale = Vector3.Lerp(
                    targetScale * 0.92f,
                    targetScale,
                    fade);
                if (targetImage != null)
                {
                    targetImage.color = Color.Lerp(ImpactColor, targetColor, fade);
                }
            });

            yield return AnimatePhase(AttackReturnDuration, progress =>
            {
                if (attacker != null)
                {
                    attacker.anchoredPosition = Vector2.Lerp(
                        lungePosition,
                        attackerPosition,
                        Smooth(progress));
                    attacker.localScale = Vector3.Lerp(
                        attackerScale * 1.08f,
                        attackerScale,
                        Smooth(progress));
                }
            });

            if (attacker != null)
            {
                attacker.anchoredPosition = attackerPosition;
                attacker.localScale = attackerScale;
            }

            if (target != null)
            {
                target.anchoredPosition = targetPosition;
                target.localScale = targetScale;
            }

            if (targetImage != null)
            {
                targetImage.color = targetColor;
            }

            if (targetOutline != null)
            {
                targetOutline.effectColor = targetOutlineColor;
                targetOutline.effectDistance = targetOutlineDistance;
            }

            attackAnimationPlaying = false;
        }

        private static IEnumerator AnimatePhase(
            float duration,
            System.Action<float> update)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                update(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            update(1f);
        }

        private RectTransform FindCardRect(BattleSide side, int index)
        {
            if (!slotContentRoots.TryGetValue(
                    BuildSlotKey(side, index),
                    out var root))
            {
                return null;
            }

            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i) as RectTransform;
                if (child != null && child.name == "Card")
                {
                    return child;
                }
            }

            return null;
        }

        private static float Smooth(float value)
        {
            return value * value * (3f - 2f * value);
        }

        private void SetSlotHighlight(BattleSide side, int index, Color color, Vector2 distance)
        {
            Outline outline;
            if (!slotOutlines.TryGetValue(BuildSlotKey(side, index), out outline))
            {
                return;
            }

            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static string BuildStepStatus(BattleStep step)
        {
            if (step.HasAttack)
            {
                return $"{BuildSideName(step.AttackerSide.Value)} {step.AttackerIndex + 1} 号位 → " +
                       $"{BuildSideName(step.TargetSide.Value)} {step.TargetIndex + 1} 号位";
            }

            return step.Messages.Count > 0 ? step.Messages[0] : "战斗播放中";
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

        private static string BuildSideName(BattleSide side)
        {
            return side == BattleSide.Player ? "玩家" : "敌方";
        }

        private static string BuildSlotKey(BattleSide side, int index)
        {
            return side + ":" + index;
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
            if (startButton != null)
            {
                startButton.interactable = false;
            }
            if (!runBattle)
            {
                returnSceneName = "ShopTest";
                return;
            }

            if (GameApp.Instance?.Run == null)
            {
                return;
            }

            if (GameApp.Instance.Run.TryCompleteBattle(result, out returnSceneName) &&
                returnButton != null)
            {
                returnButton.gameObject.SetActive(true);
            }
        }

        public void ReturnToFlow()
        {
            if (!battleResolved || string.IsNullOrEmpty(returnSceneName))
            {
                return;
            }

            SceneManager.LoadScene(returnSceneName);
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

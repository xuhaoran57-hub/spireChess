using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SpireChess.App;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.Run;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpireChess.UI.Battle
{
    public sealed class BattleTestController : MonoBehaviour
    {
        private static readonly Color BackgroundColor = new Color(0.08f, 0.09f, 0.11f, 1f);
        private static readonly Color PanelColor = new Color(0.14f, 0.15f, 0.17f, 0.94f);
        private static readonly Color PlayerSlotColor = new Color(0.17f, 0.25f, 0.28f, 1f);
        private static readonly Color EnemySlotColor = new Color(0.28f, 0.18f, 0.20f, 1f);
        private static readonly Color CardColor = new Color(0.88f, 0.83f, 0.72f, 1f);
        private static readonly Color CardHeaderColor = new Color(0.18f, 0.16f, 0.13f, 1f);
        private static readonly Color SlotOutlineColor = new Color(1f, 1f, 1f, 0.18f);
        private static readonly Color AttackerOutlineColor = new Color(1f, 0.78f, 0.18f, 1f);
        private static readonly Color TargetOutlineColor = new Color(1f, 0.28f, 0.24f, 1f);
        private static readonly Color SplashOutlineColor = new Color(0.95f, 0.48f, 0.12f, 1f);
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
                new[] { 0, 1, 2, 3, 4 })
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
        private Text statusText;
        private Button startButton;
        private Button returnButton;
        private Coroutine playbackCoroutine;
        private BattleStep activeStep;
        private BattleSimulationResult lastResult;
        private bool battleRunning;
        private bool battleResolved;
        private bool runBattle;
        private string encounterName;
        private string returnSceneName;
        private int presetIndex;
        private static Font uiFont;

        public bool IsBattleLocked => battleRunning || battleResolved;
        public bool IsRunBattle => runBattle;
        public BattleBoardState SetupState => setupState;
        public BattleSimulationResult LastResult => lastResult;

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
            simulator = new BattleSimulator(
                new System.Random(),
                id =>
                {
                    MinionConfig config;
                    return configs.TryGetMinion(id, out config) ? config : null;
                });

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
            BuildUi();
            if (restoredResult != null)
            {
                lastResult = restoredResult;
                displayedState = restoredResult.FinalState;
                battleResolved = true;
                returnSceneName = context.ReturnSceneName;
                startButton.interactable = false;
                returnButton.gameObject.SetActive(true);
                RebuildCards();
                SetLog(restoredResult.Log);
                SetStatus(BuildResultStatus(restoredResult));
            }
            else
            {
                RebuildCards();
                SetLog(new[] { "拖拽同一阵营的卡牌可以交换站位。点击开始战斗逐步播放结算。" });
                SetStatus(BuildReadyStatus());
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
            if (battleRunning)
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
            activeStep = null;
            displayedLog.Clear();
            displayedState = setupState.Clone();
            RebuildCards();
            SetLog(displayedLog);
            SetStatus("战斗播放中");

            var result = simulator.SimulatePlayback(setupState);
            foreach (var step in result.Steps)
            {
                activeStep = step;
                displayedState = step.BoardState;
                displayedLog.AddRange(step.Messages);
                RebuildCards();
                SetLog(displayedLog);
                SetStatus(BuildStepStatus(step));
                yield return new WaitForSeconds(step.HasAttack ? 0.65f : 0.32f);
            }

            activeStep = null;
            displayedState = result.FinalState;
            battleResolved = true;
            battleRunning = false;
            playbackCoroutine = null;
            RebuildCards();
            SetLog(displayedLog);
            SetStatus(BuildResultStatus(result));
            FinalizeBattle(result);
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
            battleRunning = false;
            battleResolved = true;
            RebuildCards();
            SetLog(displayedLog);
            SetStatus(BuildResultStatus(result));
            FinalizeBattle(result);
            return result;
        }

        private void ResetBattle()
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

        private void NextPreset()
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

            const int simulationCount = 10;
            var playerWins = 0;
            var enemyWins = 0;
            var draws = 0;
            for (var i = 0; i < simulationCount; i++)
            {
                var result = simulator.Simulate(setupState);
                if (!result.Winner.HasValue)
                {
                    draws++;
                }
                else if (result.Winner.Value == BattleSide.Player)
                {
                    playerWins++;
                }
                else
                {
                    enemyWins++;
                }
            }

            SetLog(new[]
            {
                $"预设：{Presets[presetIndex].Name}",
                $"连续模拟 {simulationCount} 场：玩家 {playerWins} 胜，敌方 {enemyWins} 胜，平局 {draws} 场。"
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

            var batchButton = CreateButton("BatchButton", top, "模拟10场", RunBatchSimulation);
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

            logText = CreateText("LogText", logPanel, string.Empty, 17, TextAnchor.UpperLeft);
            logText.horizontalOverflow = HorizontalWrapMode.Wrap;
            logText.verticalOverflow = VerticalWrapMode.Truncate;
            Anchor(logText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(18f, 18f), new Vector2(-18f, -64f));

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
            var cardRect = CreatePanel("Card", parent, CardColor);
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

            var header = CreatePanel("Header", cardRect, CardHeaderColor);
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
            logText.text = string.Join("\n", lines.TakeLastSafe(32));
        }

        private void SetStatus(string status)
        {
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
                return $"{BuildSideName(step.AttackerSide.Value)} {step.AttackerIndex + 1} 号位攻击";
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

        private void FinalizeBattle(BattleSimulationResult result)
        {
            lastResult = result;
            if (startButton != null)
            {
                startButton.interactable = false;
            }
            if (!runBattle || GameApp.Instance?.Run == null)
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

    internal static class BattleLogExtensions
    {
        public static IEnumerable<T> TakeLastSafe<T>(this IEnumerable<T> source, int count)
        {
            var list = source as IList<T> ?? source.ToList();
            var skip = Mathf.Max(0, list.Count - count);
            return list.Skip(skip);
        }
    }
}

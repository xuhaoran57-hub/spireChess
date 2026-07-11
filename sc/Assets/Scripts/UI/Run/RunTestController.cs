using System;
using System.Collections.Generic;
using SpireChess.App;
using SpireChess.Config;
using SpireChess.Run;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpireChess.UI.Run
{
    public sealed class RunTestController : MonoBehaviour
    {
        private static readonly Color Background = new Color(0.055f, 0.07f, 0.09f, 1f);
        private static readonly Color Panel = new Color(0.12f, 0.15f, 0.18f, 0.98f);
        private static Font uiFont;

        private readonly Dictionary<string, Button> nodeButtons = new Dictionary<string, Button>();
        private RunSession run;
        private Canvas canvas;
        private Text resourcesText;
        private Text statusText;
        private RectTransform mapRoot;
        private RectTransform resultRoot;
        private bool initialized;

        public bool IsInitialized => initialized;
        public RunSession Session => run;
        public int NodeButtonCount => nodeButtons.Count;
        public string StatusMessage { get; private set; }
        public bool ChoiceOverlayVisible => run != null && IsChoicePhase(run.State.Phase);

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
            if (SceneManager.GetActiveScene().name == "RunTest" &&
                FindObjectOfType<RunTestController>() == null)
            {
                new GameObject("RunTestController").AddComponent<RunTestController>();
            }
        }

        private void Start()
        {
            if (GameApp.Instance == null || GameApp.Instance.Run == null)
            {
                Debug.LogError("[RunTest] GameApp is not ready.");
                return;
            }

            Initialize(GameApp.Instance.Run);
        }

        public void InitializeForTests(RunSession session)
        {
            if (initialized)
            {
                throw new InvalidOperationException("RunTestController is already initialized.");
            }

            Initialize(session);
        }

        public RunOperationResult EnterNode(string nodeId)
        {
            var result = run.EnterNode(nodeId);
            if (!result.Success)
            {
                SetStatus(ToErrorText(result.Error));
                RefreshAll();
                return result;
            }

            if (run.State.Phase == RunPhase.Shop)
            {
                SceneManager.LoadScene("ShopTest");
            }
            else
            {
                SetStatus("请选择节点选项");
                RefreshAll();
            }

            return result;
        }

        public RunOperationResult SelectReward(string candidateId, string targetInstanceId = null)
        {
            return CompleteChoice(run.SelectRewardCandidate(candidateId, targetInstanceId));
        }

        public RunOperationResult SkipReward()
        {
            return CompleteChoice(run.SkipRewardChoice());
        }

        public RunOperationResult SelectEvent(string eventId, string optionId)
        {
            return CompleteChoice(run.SelectEventOption(eventId, optionId));
        }

        public RunOperationResult ApplyEnhancement(string recipeId, string targetInstanceId)
        {
            return CompleteChoice(run.ApplyEnhancement(recipeId, targetInstanceId));
        }

        public RunOperationResult SkipEnhancement()
        {
            return CompleteChoice(run.SkipEnhancement());
        }

        public RunOperationResult SelectRest(string optionId)
        {
            return CompleteChoice(run.SelectRestOption(optionId));
        }

        public RunOperationResult ContinueAfterBattle()
        {
            return CompleteChoice(run.ContinueAfterBattle());
        }

        public RunOperationResult ContinueToNextFloor()
        {
            var result = run.ContinueToNextFloor();
            if (result.Success)
            {
                RebuildMap();
            }

            return CompleteChoice(result);
        }

        public RunOperationResult RetryBoss()
        {
            var result = run.RetryBoss();
            if (result.Success)
            {
                SceneManager.LoadScene("ShopTest");
            }
            else
            {
                SetStatus(ToErrorText(result.Error));
                RefreshAll();
            }

            return result;
        }

        public void StartNewRun()
        {
            GameApp.Instance.StartNewRun();
            run = GameApp.Instance.Run;
            SetStatus("已开始新的 4C 三层单局");
            RebuildMap();
            RefreshAll();
        }

        private RunOperationResult CompleteChoice(RunOperationResult result)
        {
            SetStatus(result.Success ? result.Message : ToErrorText(result.Error));
            RefreshAll();
            return result;
        }

        private void Initialize(RunSession session)
        {
            run = session ?? throw new ArgumentNullException(nameof(session));
            initialized = true;
            if (run.State.Phase == RunPhase.Shop)
            {
                SceneManager.LoadScene("ShopTest");
                return;
            }

            if (run.State.Phase == RunPhase.Battle)
            {
                SceneManager.LoadScene("BattleTest");
                return;
            }

            BuildUi();
            SetStatus(IsChoicePhase(run.State.Phase) ? "请完成当前节点选择" : "选择可达节点继续三层单局");
            RefreshAll();
        }

        private void BuildUi()
        {
            EnsureEventSystem();
            canvas = new GameObject("RunCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
                .GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);

            var root = CreateRect("Root", canvas.transform);
            Stretch(root);
            root.gameObject.AddComponent<Image>().color = Background;

            var top = CreatePanel("Top", root, Panel);
            Anchor(top, new Vector2(0f, 0.89f), Vector2.one, new Vector2(20f, 0f), new Vector2(-20f, -14f));
            var title = CreateText("Title", top, "阶段 4C · 三层完整单局", 30, TextAnchor.MiddleLeft);
            Anchor(title.rectTransform, Vector2.zero, new Vector2(0.34f, 1f), new Vector2(18f, 0f));
            resourcesText = CreateText("Resources", top, string.Empty, 20, TextAnchor.MiddleCenter);
            Anchor(resourcesText.rectTransform, new Vector2(0.32f, 0f), new Vector2(0.72f, 1f));
            statusText = CreateText("Status", top, string.Empty, 18, TextAnchor.MiddleRight);
            Anchor(statusText.rectTransform, new Vector2(0.7f, 0f), Vector2.one, Vector2.zero, new Vector2(-18f, 0f));

            mapRoot = CreatePanel("Map", root, new Color(0.08f, 0.105f, 0.13f, 1f));
            Anchor(mapRoot, new Vector2(0.04f, 0.25f), new Vector2(0.96f, 0.84f));
            var route = CreateText("Route", mapRoot,
                "首战 → 精英 / 安全战斗 → 锻造 / 事件 / 恢复 → Boss", 22, TextAnchor.UpperCenter);
            Anchor(route.rectTransform, new Vector2(0f, 0.88f), Vector2.one,
                new Vector2(20f, 0f), new Vector2(-20f, -10f));

            resultRoot = CreatePanel("Result", root, Panel);
            Anchor(resultRoot, new Vector2(0.08f, 0.02f), new Vector2(0.92f, 0.22f));
            RebuildMap();
        }

        private void RebuildMap()
        {
            if (mapRoot == null || run.State.CurrentMap == null)
            {
                return;
            }

            foreach (var button in nodeButtons.Values)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }

            nodeButtons.Clear();
            foreach (var node in run.State.CurrentMap.Nodes)
            {
                var capturedId = node.Id;
                var button = CreateButton(node.Id, mapRoot, BuildNodeLabel(node), () => EnterNode(capturedId));
                var rect = button.GetComponent<RectTransform>();
                var center = GetNodeCenter(node);
                rect.anchorMin = center;
                rect.anchorMax = center;
                rect.sizeDelta = new Vector2(220f, 88f);
                rect.anchoredPosition = Vector2.zero;
                nodeButtons.Add(node.Id, button);
            }
        }

        private void RefreshAll()
        {
            if (!initialized || canvas == null)
            {
                return;
            }

            var state = run.State;
            resourcesText.text = $"生命 {state.Health}/{state.MaxHealth}   RunTurn {state.RunTurn}   " +
                                 $"楼层 {state.Floor}/3   战绩 {state.Statistics.BattlesWon}胜/{state.Statistics.BattlesNotWon}未胜";
            statusText.text = StatusMessage ?? string.Empty;
            foreach (var pair in nodeButtons)
            {
                var nodeStatus = state.MapProgress.GetStatus(pair.Key);
                pair.Value.interactable = state.Phase == RunPhase.MapSelection &&
                                          nodeStatus == RunNodeStatus.Reachable;
                if (state.CurrentMap.TryGetNode(pair.Key, out var node))
                {
                    pair.Value.GetComponentInChildren<Text>().text =
                        $"{BuildNodeLabel(node)}\n[{ToStatusText(nodeStatus)}]";
                }
            }

            RebuildResultPanel();
        }

        private void RebuildResultPanel()
        {
            DestroyChildren(resultRoot);
            var state = run.State;
            if (state.Phase == RunPhase.MapSelection)
            {
                var text = CreateText("Hint", resultRoot, "选择高亮节点；每条路线固定经过 4 个节点。", 20, TextAnchor.MiddleCenter);
                Stretch(text.rectTransform);
                return;
            }

            if (ChoiceOverlayVisible)
            {
                BuildChoicePanel(state);
                return;
            }

            if (state.Phase == RunPhase.FloorComplete)
            {
                var text = CreateText("FloorComplete", resultRoot,
                    $"第 {state.Floor} 层完成 · Boss 已击败，奖励已结算", 20, TextAnchor.MiddleLeft);
                Anchor(text.rectTransform, Vector2.zero, new Vector2(0.7f, 1f), new Vector2(20f, 0f));
                var next = CreateButton("NextFloor", resultRoot, "进入下一层", () => ContinueToNextFloor());
                Anchor(next.GetComponent<RectTransform>(), new Vector2(0.73f, 0.2f), new Vector2(0.98f, 0.8f));
                return;
            }

            var label = CreateText("Summary", resultRoot, BuildResultSummary(state), 19, TextAnchor.MiddleLeft);
            Anchor(label.rectTransform, Vector2.zero, new Vector2(0.7f, 1f), new Vector2(20f, 0f));
            if (state.Phase == RunPhase.BattleResult)
            {
                var currentIsBoss = state.CurrentMap.TryGetNode(state.CurrentNodeId, out var node) &&
                                    node.Type == RunNodeType.Boss && !state.CurrentAttempt.NodeResolved;
                var button = CreateButton(currentIsBoss ? "Retry" : "Continue", resultRoot,
                    currentIsBoss ? "再次挑战" : "继续前进",
                    () => { if (currentIsBoss) RetryBoss(); else ContinueAfterBattle(); });
                Anchor(button.GetComponent<RectTransform>(), new Vector2(0.73f, 0.2f), new Vector2(0.98f, 0.8f));
            }
            else if (state.Phase == RunPhase.RunWon || state.Phase == RunPhase.RunLost)
            {
                var button = CreateButton("NewRun", resultRoot, "重新开始", StartNewRun);
                Anchor(button.GetComponent<RectTransform>(), new Vector2(0.73f, 0.2f), new Vector2(0.98f, 0.8f));
            }
        }

        private void BuildChoicePanel(RunState state)
        {
            var title = "节点选择";
            var actions = new List<Tuple<string, UnityEngine.Events.UnityAction>>();
            if (state.Phase == RunPhase.RewardChoice && state.PendingRewardChoice != null)
            {
                title = "选择一项奖励";
                foreach (var candidate in state.PendingRewardChoice.Candidates)
                {
                    var capturedCandidate = candidate;
                    if (candidate.RequiresOwnedMinionTarget)
                    {
                        foreach (var card in run.Shop.Collection.Battle)
                        {
                            if (card == null) continue;
                            var capturedTarget = card;
                            actions.Add(Tuple.Create(
                                $"{candidate.DisplayText}\n→ {card.Minion.Name}",
                                (UnityEngine.Events.UnityAction)(() => SelectReward(
                                    capturedCandidate.CandidateId, capturedTarget.InstanceId))));
                        }
                    }
                    else
                    {
                        actions.Add(Tuple.Create(candidate.DisplayText,
                            (UnityEngine.Events.UnityAction)(() => SelectReward(capturedCandidate.CandidateId))));
                    }
                }

                if (state.PendingRewardChoice.AllowSkip)
                    actions.Add(Tuple.Create("跳过奖励", (UnityEngine.Events.UnityAction)(() => SkipReward())));
            }
            else if (state.Phase == RunPhase.EventChoice && state.PendingEventChoice != null)
            {
                var pending = state.PendingEventChoice;
                title = $"{pending.Config.Name}：{pending.Config.Description}";
                foreach (var option in pending.Config.Options)
                {
                    var captured = option;
                    actions.Add(Tuple.Create(option.Label,
                        (UnityEngine.Events.UnityAction)(() => SelectEvent(pending.Config.Id, captured.Id))));
                }
            }
            else if (state.Phase == RunPhase.EnhanceChoice && state.PendingEnhanceChoice != null)
            {
                title = "选择锻造配方和目标";
                foreach (var recipe in state.PendingEnhanceChoice.Recipes)
                foreach (var card in run.Shop.Collection.Battle)
                {
                    if (card == null) continue;
                    var capturedRecipe = recipe;
                    var capturedTarget = card;
                    actions.Add(Tuple.Create($"{recipe.Name}\n→ {card.Minion.Name}",
                        (UnityEngine.Events.UnityAction)(() => ApplyEnhancement(
                            capturedRecipe.Id, capturedTarget.InstanceId))));
                }

                if (state.PendingEnhanceChoice.NodeConfig.AllowSkip)
                    actions.Add(Tuple.Create("离开锻造台", (UnityEngine.Events.UnityAction)(() => SkipEnhancement())));
            }
            else if (state.Phase == RunPhase.RestChoice && state.PendingRestChoice != null)
            {
                title = "选择恢复方式";
                foreach (var option in state.PendingRestChoice.Config.Options)
                {
                    var captured = option;
                    actions.Add(Tuple.Create(option.Label,
                        (UnityEngine.Events.UnityAction)(() => SelectRest(captured.Id))));
                }
            }

            var titleText = CreateText("ChoiceTitle", resultRoot, title, 18, TextAnchor.UpperCenter);
            Anchor(titleText.rectTransform, new Vector2(0f, 0.72f), Vector2.one);
            for (var i = 0; i < actions.Count; i++)
            {
                var columns = Math.Min(5, Math.Max(1, actions.Count));
                var row = i / columns;
                var column = i % columns;
                var width = 0.96f / columns;
                var yMax = 0.68f - row * 0.34f;
                var button = CreateButton($"Choice_{i}", resultRoot, actions[i].Item1, actions[i].Item2);
                Anchor(button.GetComponent<RectTransform>(),
                    new Vector2(0.02f + column * width, yMax - 0.3f),
                    new Vector2(0.02f + (column + 1) * width - 0.01f, yMax));
            }
        }

        private string BuildResultSummary(RunState state)
        {
            if (state.Phase == RunPhase.RunWon)
                return $"三层通关 · {state.Statistics.BattlesWon} 胜 / {state.Statistics.BattlesNotWon} 未胜 · " +
                       $"击败 {state.Statistics.BossesDefeated} Boss · 三连 {state.Statistics.TriplesFormed}";
            if (state.Phase == RunPhase.RunLost)
                return $"单局失败：止步第 {state.Floor} 层 · {state.Statistics.BattlesWon} 胜 / " +
                       $"{state.Statistics.BattlesNotWon} 未胜";
            if (state.LastSettlement == null) return "等待节点结算。";
            var outcome = state.LastSettlement.PlayerWon ? "胜利" : "未胜利";
            var reward = string.IsNullOrWhiteSpace(state.LastRewardSummary)
                ? string.Empty : $"；{state.LastRewardSummary}";
            return $"{outcome}；{state.LastSettlement.BuildDamageText()}{reward}";
        }

        private string BuildNodeLabel(MapNodeDefinition node)
        {
            var type = ToNodeTypeText(node.Type);
            if (GameApp.Instance?.Configs != null &&
                GameApp.Instance.Configs.TryGetEncounter(node.PayloadId, out EncounterConfig encounter))
            {
                return $"{type}\n{encounter.Name}";
            }

            return $"{type}\n{node.Id}";
        }

        private static Vector2 GetNodeCenter(MapNodeDefinition node)
        {
            var x = 0.12f + 0.255f * node.Column;
            var y = node.Row < 0 ? 0.68f : node.Row > 0 ? 0.28f : 0.48f;
            return new Vector2(x, y);
        }

        private static bool IsChoicePhase(RunPhase phase)
        {
            return phase == RunPhase.RewardChoice || phase == RunPhase.EventChoice ||
                   phase == RunPhase.EnhanceChoice || phase == RunPhase.RestChoice;
        }

        private static string ToNodeTypeText(RunNodeType type)
        {
            switch (type)
            {
                case RunNodeType.Normal: return "普通战斗";
                case RunNodeType.Elite: return "精英战斗";
                case RunNodeType.Enhance: return "锻造";
                case RunNodeType.Event: return "事件";
                case RunNodeType.Rest: return "恢复";
                case RunNodeType.Boss: return "Boss";
                default: return type.ToString();
            }
        }

        private void SetStatus(string message)
        {
            StatusMessage = message ?? string.Empty;
            if (statusText != null) statusText.text = StatusMessage;
        }

        private static string ToStatusText(RunNodeStatus status)
        {
            switch (status)
            {
                case RunNodeStatus.Reachable: return "可进入";
                case RunNodeStatus.Current: return "当前";
                case RunNodeStatus.Resolved: return "已完成";
                default: return "未解锁";
            }
        }

        private static string ToErrorText(RunOperationError error)
        {
            switch (error)
            {
                case RunOperationError.NodeNotReachable: return "节点当前不可达";
                case RunOperationError.PendingCardRewards: return "请先处理待领取奖励";
                case RunOperationError.BenchFull: return "备战区已满";
                case RunOperationError.InvalidChoice: return "选项无效";
                case RunOperationError.InvalidTarget: return "强化目标无效";
                case RunOperationError.NoBenefit: return "该选项当前不会产生收益";
                case RunOperationError.InsufficientPool: return "随从池不足，无法生成候选";
                default: return $"操作失败：{error}";
            }
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.GetComponent<Image>().color = color;
            return rect;
        }

        private static Text CreateText(string name, Transform parent, string text, int size, TextAnchor alignment)
        {
            var value = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            value.transform.SetParent(parent, false);
            value.font = GetFont();
            value.text = text;
            value.fontSize = size;
            value.alignment = alignment;
            value.color = Color.white;
            return value;
        }

        private static Button CreateButton(string name, Transform parent, string text,
            UnityEngine.Events.UnityAction action)
        {
            var button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            button.transform.SetParent(parent, false);
            button.GetComponent<Image>().color = new Color(0.28f, 0.42f, 0.5f, 1f);
            var label = CreateText("Text", button.transform, text, 18, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, new Vector2(8f, 5f), new Vector2(-8f, -5f));
            button.onClick.AddListener(action);
            return button;
        }

        private static Font GetFont()
        {
            if (uiFont == null)
            {
                uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 18);
                if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return uiFont;
        }

        private static void DestroyChildren(Transform transform)
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
        }

        private static void Stretch(RectTransform rect, Vector2? offsetMin = null, Vector2? offsetMax = null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin ?? Vector2.zero;
            rect.offsetMax = offsetMax ?? Vector2.zero;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max,
            Vector2? offsetMin = null, Vector2? offsetMax = null)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin ?? Vector2.zero;
            rect.offsetMax = offsetMax ?? Vector2.zero;
        }
    }
}

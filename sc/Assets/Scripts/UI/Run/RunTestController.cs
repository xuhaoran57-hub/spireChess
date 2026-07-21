using System;
using SpireChess.App;
using SpireChess.Run;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpireChess.UI.Run
{
    public sealed class RunTestController : MonoBehaviour
    {
        [SerializeField] private RunScreenView screenView;

        private RunSession run;
        private bool initialized;

        public bool IsInitialized => initialized;
        public bool IsUsingFormalView => initialized && screenView != null;
        public RunScreenView FormalScreenView => screenView;
        public RunSession Session => run;
        public int NodeButtonCount => screenView == null
            ? 0
            : screenView.RenderedNodeCount;
        public string StatusMessage { get; private set; }
        public bool ChoiceOverlayVisible => screenView != null
            ? screenView.IsChoiceVisible
            : run != null && IsChoicePhase(run.State.Phase);

        private void Start()
        {
            if (GameApp.Instance == null || GameApp.Instance.Run == null)
            {
                Debug.LogError("[RunTest] GameApp is not ready.");
                return;
            }
            if (screenView == null)
            {
                Debug.LogError("[RunTest] Formal RunScreenView is not configured.");
                return;
            }

            Initialize(GameApp.Instance.Run);
        }

        public void InitializeForTests(RunSession session)
        {
            if (initialized)
            {
                throw new InvalidOperationException(
                    "RunTestController is already initialized.");
            }
            if (screenView == null)
            {
                throw new InvalidOperationException(
                    "RunTestController requires a formal RunScreenView.");
            }
            Initialize(session);
        }

        public void ConfigureFormalViewForTests(RunScreenView value)
        {
            if (initialized)
            {
                throw new InvalidOperationException(
                    "Configure the formal view before initialization.");
            }
            screenView = value ?? throw new ArgumentNullException(nameof(value));
        }

        public RunOperationResult EnterNode(string nodeId)
        {
            var result = run.EnterNode(nodeId);
            if (!result.Success)
            {
                SetStatus(ToErrorText(result.Error));
                return result;
            }

            if (run.State.Phase == RunPhase.Shop)
            {
                SceneManager.LoadScene("ShopTest");
            }
            else if (run.State.Phase == RunPhase.Battle)
            {
                SceneManager.LoadScene("BattleTest");
            }
            else
            {
                SetStatus("请选择节点选项");
            }
            return result;
        }

        public RunOperationResult SelectReward(
            string candidateId,
            string targetInstanceId = null)
        {
            return CompleteChoice(
                run.SelectRewardCandidate(candidateId, targetInstanceId));
        }

        public RunOperationResult SkipReward()
        {
            return CompleteChoice(run.SkipRewardChoice());
        }

        public RunOperationResult SelectRelic(string candidateId)
        {
            return CompleteChoice(run.SelectRelicCandidate(candidateId));
        }

        public RunOperationResult SkipRelic()
        {
            return CompleteChoice(run.SkipRelicChoice());
        }

        public RunOperationResult SelectEvent(string eventId, string optionId)
        {
            var result = run.SelectEventOption(eventId, optionId);
            if (result.Success && run.State.Phase == RunPhase.Battle)
            {
                SceneManager.LoadScene("BattleTest");
                return result;
            }
            return CompleteChoice(result);
        }

        public RunOperationResult ApplyEnhancement(
            string recipeId,
            string targetInstanceId)
        {
            return CompleteChoice(
                run.ApplyEnhancement(recipeId, targetInstanceId));
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
            return CompleteChoice(run.ContinueToNextFloor());
        }

        public RunOperationResult RetryBoss()
        {
            var result = run.RetryBoss();
            if (result.Success)
            {
                SceneManager.LoadScene("BattleTest");
            }
            else
            {
                SetStatus(ToErrorText(result.Error));
            }
            return result;
        }

        public void StartNewRun()
        {
            GameApp.Instance.StartNewRun();
            run = GameApp.Instance.Run;
            SetStatus("已开始新的 8B 完整地图单局");
        }

        public void ExecuteUiAction(
            RunUiActionType action,
            string primaryId = null,
            string secondaryId = null)
        {
            switch (action)
            {
                case RunUiActionType.SelectReward:
                    SelectReward(primaryId, secondaryId);
                    break;
                case RunUiActionType.SkipReward:
                    SkipReward();
                    break;
                case RunUiActionType.SelectRelic:
                    SelectRelic(primaryId);
                    break;
                case RunUiActionType.SkipRelic:
                    SkipRelic();
                    break;
                case RunUiActionType.SelectEvent:
                    SelectEvent(primaryId, secondaryId);
                    break;
                case RunUiActionType.ApplyEnhancement:
                    ApplyEnhancement(primaryId, secondaryId);
                    break;
                case RunUiActionType.SkipEnhancement:
                    SkipEnhancement();
                    break;
                case RunUiActionType.SelectRest:
                    SelectRest(primaryId);
                    break;
                case RunUiActionType.ContinueAfterBattle:
                    ContinueAfterBattle();
                    break;
                case RunUiActionType.RetryBoss:
                    RetryBoss();
                    break;
                case RunUiActionType.ContinueToNextFloor:
                    ContinueToNextFloor();
                    break;
                case RunUiActionType.StartNewRun:
                    StartNewRun();
                    break;
            }
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

            screenView.Bind(this);
            StatusMessage = IsChoicePhase(run.State.Phase)
                ? "请完成当前节点选择"
                : "选择可达节点继续三层单局";
            RefreshAll();
        }

        private RunOperationResult CompleteChoice(RunOperationResult result)
        {
            SetStatus(result.Success ? result.Message : ToErrorText(result.Error));
            return result;
        }

        private void SetStatus(string message)
        {
            StatusMessage = message ?? string.Empty;
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (!initialized || screenView == null ||
                GameApp.Instance?.Configs == null)
            {
                return;
            }
            screenView.Render(RunScreenStateBuilder.Build(
                run,
                GameApp.Instance.Configs,
                StatusMessage));
        }

        private static bool IsChoicePhase(RunPhase phase)
        {
            return phase == RunPhase.RewardChoice ||
                   phase == RunPhase.EventChoice ||
                   phase == RunPhase.RelicChoice ||
                   phase == RunPhase.EnhanceChoice ||
                   phase == RunPhase.RestChoice;
        }

        private static string ToErrorText(RunOperationError error)
        {
            switch (error)
            {
                case RunOperationError.NodeNotReachable:
                    return "节点当前不可达";
                case RunOperationError.PendingCardRewards:
                    return "请先处理待领取奖励";
                case RunOperationError.BenchFull:
                    return "备战区已满";
                case RunOperationError.InvalidChoice:
                    return "选项无效";
                case RunOperationError.InvalidTarget:
                    return "强化目标无效";
                case RunOperationError.NoBenefit:
                    return "该选项当前不会产生收益";
                case RunOperationError.InsufficientPool:
                    return "随从池不足，无法生成候选";
                default:
                    return $"操作失败：{error}";
            }
        }
    }
}

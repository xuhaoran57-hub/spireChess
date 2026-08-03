using System;
using System.Linq;
using SpireChess.App;
using SpireChess.Audio;
using SpireChess.Run;
using SpireChess.UI.Common;
using UnityEngine;

namespace SpireChess.UI.Run
{
    public sealed class RunTestController : MonoBehaviour
    {
        [SerializeField] private RunScreenView screenView;

        private RunSession run;
        private bool initialized;
        private bool returningToMenu;
        private string unlockNotificationText = string.Empty;
        private string unlockNotificationMapId = string.Empty;

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
        public bool JournalPageVisible => screenView != null &&
                                          screenView.IsJournalPageVisible;

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

            if (!Persist("EnterNode"))
            {
                return result;
            }

            AudioService.Instance?.PlayCue(
                PresentationAudioCueIds.RunNodeSelect);
            if (run.State.Phase == RunPhase.Shop)
            {
                GameApp.Instance.Router.GoToCurrentRunPhase(run);
            }
            else if (run.State.Phase == RunPhase.Battle)
            {
                GameApp.Instance.Router.GoToCurrentRunPhase(run);
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
            var result = CompleteChoice(
                run.SelectRewardCandidate(candidateId, targetInstanceId),
                "SelectReward");
            PlayRewardCue(result);
            return result;
        }

        public RunOperationResult SkipReward()
        {
            return CompleteChoice(run.SkipRewardChoice(), "SkipReward");
        }

        public RunOperationResult SelectRelic(string candidateId)
        {
            var result = CompleteChoice(
                run.SelectRelicCandidate(candidateId),
                "SelectRelic");
            PlayRewardCue(result);
            return result;
        }

        public RunOperationResult SkipRelic()
        {
            return CompleteChoice(run.SkipRelicChoice(), "SkipRelic");
        }

        private static void PlayRewardCue(RunOperationResult result)
        {
            if (result != null && result.Success)
            {
                AudioService.Instance?.PlayCue(
                    PresentationAudioCueIds.RunReward);
            }
        }

        public RunOperationResult SelectEvent(string eventId, string optionId)
        {
            var result = run.SelectEventOption(eventId, optionId);
            if (!result.Success)
            {
                SetStatus(ToErrorText(result.Error));
                return result;
            }

            SetStatus(result.Message);
            if (!Persist("SelectEvent"))
            {
                return result;
            }
            if (run.State.Phase == RunPhase.Battle)
            {
                GameApp.Instance.Router.GoToCurrentRunPhase(run);
            }
            return result;
        }

        public RunOperationResult ApplyEnhancement(
            string recipeId,
            string targetInstanceId)
        {
            return CompleteChoice(
                run.ApplyEnhancement(recipeId, targetInstanceId),
                "ApplyEnhancement");
        }

        public RunOperationResult SkipEnhancement()
        {
            return CompleteChoice(run.SkipEnhancement(), "SkipEnhancement");
        }

        public RunOperationResult SelectRest(string optionId)
        {
            return CompleteChoice(run.SelectRestOption(optionId), "SelectRest");
        }

        public RunOperationResult ContinueAfterBattle()
        {
            return CompleteChoice(run.ContinueAfterBattle(), "ContinueAfterBattle");
        }

        public RunOperationResult ContinueToNextFloor()
        {
            return CompleteChoice(run.ContinueToNextFloor(), "ContinueToNextFloor");
        }

        public RunOperationResult RetryBoss()
        {
            var result = run.RetryBoss();
            if (result.Success)
            {
                if (Persist("RetryBoss"))
                {
                    GameApp.Instance.Router.GoToCurrentRunPhase(run);
                }
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
            if (run != null)
            {
                SetStatus("已开始新的 8B 完整地图单局");
            }
            else
            {
                SetStatus("新单局创建失败，原存档未被替换");
            }
        }

        public void ReturnToMainMenu()
        {
            if (returningToMenu || GameApp.Instance == null)
            {
                return;
            }

            returningToMenu = true;
            if (!GameApp.Instance.SaveAndReturnToMainMenu())
            {
                returningToMenu = false;
                SetStatus("旅程状态尚未保存，暂时无法返回目录");
            }
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
                case RunUiActionType.ReturnToMainMenu:
                    ReturnToMainMenu();
                    break;
            }
        }

        private void Initialize(RunSession session)
        {
            run = session ?? throw new ArgumentNullException(nameof(session));
            initialized = true;
            if (run.State.Phase == RunPhase.Shop)
            {
                GameApp.Instance.Router.GoToCurrentRunPhase(run);
                return;
            }
            if (run.State.Phase == RunPhase.Battle)
            {
                GameApp.Instance.Router.GoToCurrentRunPhase(run);
                return;
            }

            screenView.Bind(this);
            RunSystemMenuView.Attach(screenView);
            var heroPassiveMessage = run.CurrentShopEndHeroPassiveMessage;
            StatusMessage = !string.IsNullOrWhiteSpace(heroPassiveMessage)
                ? heroPassiveMessage
                : IsChoicePhase(run.State.Phase)
                    ? "请完成当前节点选择"
                    : "选择可达节点继续旅程";
            RefreshAll();
        }

        private RunOperationResult CompleteChoice(
            RunOperationResult result,
            string saveReason)
        {
            SetStatus(result.Success ? result.Message : ToErrorText(result.Error));
            if (result.Success && Persist(saveReason))
            {
                // Persisting a chapter boundary can create an existing
                // profile unlock notification. Refresh once more after the
                // save event so the presentation consumes that notification
                // on the page where the player earned it.
                RefreshAll();
            }
            return result;
        }

        private bool Persist(string reason)
        {
            var app = GameApp.Instance;
            if (app == null || !ReferenceEquals(app.Run, run) || app.Persistence == null)
            {
                return true;
            }

            if (app.Persistence.CommitSuccessful(run, reason))
            {
                return true;
            }

            SetStatus("操作已生效，但尚未保存；请稍后从菜单重试");
            return false;
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
            var state = RunScreenStateBuilder.Build(
                run,
                GameApp.Instance.Configs,
                StatusMessage);
            CaptureUnlockNotification();
            state.JournalPage = BuildJournalPage(state.Summary);
            screenView.Render(state);
        }

        private RunJournalPageState BuildJournalPage(RunSummaryState summary)
        {
            var mapName = run?.State?.CurrentMap?.DisplayName ?? "当前章节";
            var mapArtworkId = run?.State?.CurrentMap?.Id ?? string.Empty;
            if (run?.State?.Phase == RunPhase.FloorComplete)
            {
                return new RunJournalPageState
                {
                    Kind = RunJournalPageKind.ChapterComplete,
                    Title = mapName + " · 章节完成",
                    Body = summary?.Text ?? string.Empty,
                    UnlockNotification = unlockNotificationText,
                    ArtworkId = mapArtworkId,
                    ActionLabel = summary?.ActionLabel ?? "进入下一章",
                    Action = RunUiActionType.ContinueToNextFloor
                };
            }

            if (run?.State?.Phase == RunPhase.RunWon)
            {
                return new RunJournalPageState
                {
                    Kind = RunJournalPageKind.Ending,
                    Title = "旅团日记 · 完结",
                    Body = summary?.Text ?? string.Empty,
                    UnlockNotification = unlockNotificationText,
                    ArtworkId = mapArtworkId,
                    ActionLabel = "返回目录",
                    Action = RunUiActionType.ReturnToMainMenu
                };
            }

            return null;
        }

        private void CaptureUnlockNotification()
        {
            var phase = run?.State?.Phase;
            var mapId = run?.State?.CurrentMap?.Id ?? string.Empty;
            var canShowOnCurrentPage = phase == RunPhase.FloorComplete ||
                                       phase == RunPhase.RunWon;
            if (!canShowOnCurrentPage)
            {
                unlockNotificationText = string.Empty;
                unlockNotificationMapId = string.Empty;
                return;
            }

            if (!string.Equals(
                    unlockNotificationMapId,
                    mapId,
                    StringComparison.Ordinal))
            {
                unlockNotificationText = string.Empty;
                unlockNotificationMapId = string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(unlockNotificationText))
            {
                return;
            }

            var profiles = GameApp.Instance?.Profiles;
            var notification = profiles?.Progress?.UnreadUnlockNotifications
                ?.FirstOrDefault(value =>
                    string.Equals(
                        value.SourceMapId,
                        run.State.CurrentMap?.Id,
                        StringComparison.Ordinal));
            if (notification == null ||
                !HeroCatalog.TryGet(notification.HeroId, out var hero))
            {
                return;
            }

            unlockNotificationText = $"新角色已解锁：{hero.DisplayName}";
            unlockNotificationMapId = mapId;
            try
            {
                profiles.MarkUnlockNotificationRead(notification.Id);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[Profile] Unlock notification remains unread: " +
                    exception.Message);
            }
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

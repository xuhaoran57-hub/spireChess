using System;
using SpireChess.App;
using SpireChess.Diagnostics;
using SpireChess.Run;
using SpireChess.Save;
using UnityEngine;

namespace SpireChess.UI.MainMenu
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private MainMenuScreenView screenView;

        private RunSaveLoadResult inspection;
        private string statusMessage = string.Empty;
        private bool statusIsError;

        public MainMenuScreenView ScreenView => screenView;
        public RunSaveLoadResult Inspection => inspection;

        private void Start()
        {
            if (screenView == null)
            {
                screenView = FindObjectOfType<MainMenuScreenView>() ??
                             MainMenuScreenView.CreateRuntime();
            }

            screenView.Bind(this);
            Refresh();
        }

        public static void EnsurePresent()
        {
            if (FindObjectOfType<MainMenuController>() != null)
            {
                return;
            }

            new GameObject("MainMenuController", typeof(MainMenuController));
        }

        public void NewGame()
        {
            RefreshInspection();
            if (inspection.Status != RunSaveLoadStatus.Missing)
            {
                screenView.ShowConfirmation(
                    "已有单局存档。开始新游戏会替换当前进度，是否继续？",
                    CreateNewRun);
                return;
            }

            CreateNewRun();
        }

        public void ContinueGame()
        {
            var app = GameApp.Instance;
            var loaded = app?.ContinueRun();
            if (loaded?.CanContinue == true && app.Run != null)
            {
                app.Router.GoToCurrentRunPhase(app.Run);
                return;
            }

            statusMessage = ToPlayerMessage(loaded?.Status ?? RunSaveLoadStatus.IoFailure);
            statusIsError = true;
            Refresh();
        }

        public void DeleteSave()
        {
            screenView.ShowConfirmation("确定删除当前单局存档？此操作无法撤销。", () =>
            {
                GameApp.Instance.AbandonRun();
                statusMessage = "单局存档已删除";
                statusIsError = false;
                Refresh();
            });
        }

        public void OpenSettingsPlaceholder()
        {
            screenView?.ShowSettings();
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        private void CreateNewRun()
        {
            var acceptanceSeed = G4RuntimeArguments.IsAcceptanceRequested
                ? G4RuntimeArguments.ReadInt(
                    G4RuntimeArguments.AcceptanceSeedArgument,
                    G4RuntimeArguments.IsFrozenVisualRequested
                        ? 78
                        : G4RuntimeArguments.IsVisualSliceRequested
                            ? 10
                            : 940101,
                    1,
                    int.MaxValue)
                : (int?)null;
            GameApp.Instance.StartNewRun(acceptanceSeed);
            if (GameApp.Instance.Run == null)
            {
                statusMessage = "无法创建新单局，请检查存储空间后重试";
                statusIsError = true;
                Refresh();
                return;
            }

            GameApp.Instance.Router.GoToCurrentRunPhase(GameApp.Instance.Run);
        }

        private void Refresh()
        {
            RefreshInspection();
            var summary = inspection.Document?.Summary;
            var canContinue = inspection.CanContinue;
            screenView.Render(new MainMenuScreenState
            {
                ContinueEnabled = canContinue,
                ContinueSummary = BuildSummary(inspection, summary),
                StatusMessage = string.IsNullOrWhiteSpace(statusMessage)
                    ? ToPlayerMessage(inspection.Status)
                    : statusMessage,
                StatusIsError = statusIsError ||
                                (!canContinue && inspection.Status != RunSaveLoadStatus.Missing),
                SaveStatus = inspection.Status
            });
        }

        private void RefreshInspection()
        {
            inspection = GameApp.Instance?.InspectRunSave() ??
                         new RunSaveLoadResult(RunSaveLoadStatus.IoFailure);
        }

        private static string BuildSummary(
            RunSaveLoadResult result,
            RunSaveSummaryV1 summary)
        {
            if (!result.CanContinue || summary == null)
            {
                return result.Status == RunSaveLoadStatus.Missing
                    ? "当前没有可继续的单局"
                    : "检测到存档，但当前无法继续";
            }

            if (summary.Floor < 1 || summary.Floor > 3 || summary.MaxHealth <= 0 ||
                summary.Health < 0 || summary.Health > summary.MaxHealth)
            {
                return "已有单局存档";
            }

            return $"第 {summary.Floor} 层 · 生命 {summary.Health}/{summary.MaxHealth} · " +
                   $"回合 {summary.ShopTurn} · {ToPhaseLabel(summary.Phase)}";
        }

        private static string ToPhaseLabel(RunPhase phase)
        {
            switch (phase)
            {
                case RunPhase.MapSelection:
                    return "地图选择";
                case RunPhase.EnteringNode:
                    return "进入节点";
                case RunPhase.Shop:
                    return "商店";
                case RunPhase.Battle:
                    return "战斗";
                case RunPhase.BattleResult:
                    return "战斗结算";
                case RunPhase.RewardChoice:
                    return "奖励选择";
                case RunPhase.RelicChoice:
                    return "遗珍选择";
                case RunPhase.EventChoice:
                    return "事件选择";
                case RunPhase.EnhanceChoice:
                    return "强化选择";
                case RunPhase.RestChoice:
                    return "休整选择";
                case RunPhase.FloorComplete:
                    return "楼层完成";
                case RunPhase.RunWon:
                    return "单局胜利";
                case RunPhase.RunLost:
                    return "单局失败";
                default:
                    return "未知阶段";
            }
        }

        private static string ToPlayerMessage(RunSaveLoadStatus status)
        {
            switch (status)
            {
                case RunSaveLoadStatus.Missing:
                    return "选择新游戏开始三层远征";
                case RunSaveLoadStatus.Valid:
                    return "发现可继续的单局";
                case RunSaveLoadStatus.RecoveredFromBackup:
                    return "主存档异常，将使用安全备份继续";
                case RunSaveLoadStatus.IncompatibleContent:
                case RunSaveLoadStatus.UnsupportedSchema:
                    return "该单局来自不同内容版本，无法继续";
                case RunSaveLoadStatus.CorruptJson:
                case RunSaveLoadStatus.ChecksumMismatch:
                case RunSaveLoadStatus.InvalidReference:
                case RunSaveLoadStatus.InvalidDomainState:
                case RunSaveLoadStatus.RandomReplayMismatch:
                    return "单局存档已损坏，可删除后开始新游戏";
                default:
                    return "读取单局存档失败，请稍后重试";
            }
        }
    }
}

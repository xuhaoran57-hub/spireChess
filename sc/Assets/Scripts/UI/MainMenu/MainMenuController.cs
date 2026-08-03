using System;
using System.Linq;
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

        private static bool coverSeenThisApplication;
        private RunSaveLoadResult inspection;
        private string statusMessage = string.Empty;
        private bool statusIsError;
        private bool creatingRun;
        private bool continuingRun;
        private string selectedHeroId = HeroIds.Warrior;
        private JournalPageFlow journalFlow;

        public MainMenuScreenView ScreenView => screenView;
        public RunSaveLoadResult Inspection => inspection;
        public bool HeroSelectionVisible =>
            CurrentPage == JournalMenuPage.HeroSelection;
        public string SelectedHeroId => selectedHeroId;
        public JournalMenuPage CurrentPage => journalFlow == null
            ? JournalMenuPage.Contents
            : journalFlow.CurrentPage;
        public bool IsPageInputLocked => journalFlow != null &&
                                         journalFlow.IsInputLocked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCoverForApplicationLaunch()
        {
            coverSeenThisApplication = false;
        }

        private void Start()
        {
            journalFlow = new JournalPageFlow(
                coverSeenThisApplication
                    ? JournalMenuPage.Contents
                    : JournalMenuPage.Cover);
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
            if (CurrentPage != JournalMenuPage.Contents ||
                IsPageInputLocked || creatingRun || continuingRun)
            {
                return;
            }

            RefreshInspection();
            if (inspection.Status != RunSaveLoadStatus.Missing)
            {
                screenView.ShowConfirmation(
                    "已有单局存档。开始新游戏会替换当前进度，是否继续？",
                    OpenHeroSelection);
                return;
            }

            OpenHeroSelection();
        }

        public void SelectHero(string heroId)
        {
            if (CurrentPage != JournalMenuPage.HeroSelection ||
                IsPageInputLocked ||
                GameApp.Instance?.Profiles?.IsHeroUnlocked(heroId) != true)
            {
                return;
            }

            selectedHeroId = heroId;
            statusMessage = $"已选择{HeroCatalog.GetRequired(heroId).DisplayName}";
            statusIsError = false;
            Refresh();
        }

        public void ConfirmHeroSelection()
        {
            if (CurrentPage != JournalMenuPage.HeroSelection ||
                IsPageInputLocked || creatingRun)
            {
                return;
            }

            if (GameApp.Instance?.Profiles?.IsHeroUnlocked(selectedHeroId) != true)
            {
                statusMessage = "该角色尚未解锁";
                statusIsError = true;
                Refresh();
                return;
            }

            creatingRun = true;
            try
            {
                CreateNewRun(selectedHeroId);
            }
            finally
            {
                if (!IsPageInputLocked)
                {
                    creatingRun = false;
                    Refresh();
                }
            }
        }

        public void CancelHeroSelection()
        {
            if (CurrentPage != JournalMenuPage.HeroSelection ||
                IsPageInputLocked || creatingRun)
            {
                return;
            }

            BeginPageTurn(JournalMenuPage.Contents);
            statusMessage = "已返回目录";
            statusIsError = false;
            Refresh();
        }

        public void ContinueGame()
        {
            if (CurrentPage != JournalMenuPage.Contents ||
                IsPageInputLocked || creatingRun || continuingRun)
            {
                return;
            }

            continuingRun = true;
            Refresh();
            var app = GameApp.Instance;
            var loaded = app?.ContinueRun();
            if (loaded?.CanContinue == true && app.Run != null)
            {
                app.Router.GoToCurrentRunPhase(app.Run);
                return;
            }

            continuingRun = false;
            statusMessage = ToPlayerMessage(loaded?.Status ?? RunSaveLoadStatus.IoFailure);
            statusIsError = true;
            Refresh();
        }

        public void DeleteSave()
        {
            if (CurrentPage != JournalMenuPage.Contents ||
                IsPageInputLocked || creatingRun || continuingRun)
            {
                return;
            }

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
            if (CurrentPage != JournalMenuPage.Contents ||
                IsPageInputLocked)
            {
                return;
            }

            screenView?.ShowSettings();
        }

        public void QuitGame()
        {
            if (IsPageInputLocked)
            {
                return;
            }

            Application.Quit();
        }

        public void OpenContentsFromCover()
        {
            if (CurrentPage != JournalMenuPage.Cover || IsPageInputLocked)
            {
                return;
            }

            BeginPageTurn(JournalMenuPage.Contents);
        }

        public void SkipPageTurn()
        {
            if (!IsPageInputLocked)
            {
                OpenContentsFromCover();
                return;
            }

            screenView?.SkipPageTurn();
        }

        private void OpenHeroSelection()
        {
            var profiles = GameApp.Instance?.Profiles;
            if (profiles?.IsReady != true)
            {
                statusMessage = "局外档案不可用，暂时无法开始新旅程";
                statusIsError = true;
                Refresh();
                return;
            }

            var firstUnlocked = HeroCatalog.All.FirstOrDefault(value =>
                profiles.IsHeroUnlocked(value.Id));
            if (firstUnlocked == null)
            {
                statusMessage = "没有可用角色，无法开始新旅程";
                statusIsError = true;
                Refresh();
                return;
            }

            if (!profiles.IsHeroUnlocked(selectedHeroId))
            {
                selectedHeroId = firstUnlocked.Id;
            }

            BeginPageTurn(JournalMenuPage.HeroSelection);
            statusMessage = "选择一名已解锁角色并确认启程";
            statusIsError = false;
            Refresh();
        }

        private void CreateNewRun(string heroId)
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
            var created = GameApp.Instance.StartNewRun(heroId, acceptanceSeed);
            if (!created || GameApp.Instance.Run == null)
            {
                statusMessage = "无法创建新单局，请检查存储空间后重试";
                statusIsError = true;
                Refresh();
                return;
            }

            // The run has already been created through the preserved domain
            // entry point. The presentation-only Map page is the final
            // journal turn before the existing router shows its real map.
            BeginPageTurn(JournalMenuPage.Map, () =>
            {
                creatingRun = false;
                GameApp.Instance.Router.GoToCurrentRunPhase(GameApp.Instance.Run);
            });
        }

        private void BeginPageTurn(
            JournalMenuPage destination,
            Action afterTransition = null)
        {
            if (journalFlow == null)
            {
                journalFlow = new JournalPageFlow(JournalMenuPage.Contents);
            }

            var source = journalFlow.CurrentPage;
            if (!journalFlow.TryBeginTransition(destination))
            {
                return;
            }

            Refresh();
            var completed = false;
            Action finish = () =>
            {
                if (completed)
                {
                    return;
                }

                completed = true;
                journalFlow.CompleteTransition();
                if (source == JournalMenuPage.Cover)
                {
                    coverSeenThisApplication = true;
                }

                Refresh();
                afterTransition?.Invoke();
            };
            if (screenView == null)
            {
                finish();
                return;
            }

            screenView.PlayPageTurn(source, destination, finish);
        }

        private void Refresh()
        {
            RefreshInspection();
            var summary = inspection.Document?.Summary;
            var canContinue = inspection.CanContinue;
            var legacyNotice =
                GameApp.Instance?.Profiles?.Progress
                    ?.LegacyV033ArchiveNoticePending == true &&
                (inspection.Status == RunSaveLoadStatus.Missing ||
                 inspection.Status == RunSaveLoadStatus.IncompatibleContent ||
                 inspection.Status == RunSaveLoadStatus.UnsupportedSchema);
            screenView.Render(new MainMenuScreenState
            {
                ContinueEnabled = canContinue,
                ContinueSummary = BuildSummary(inspection, summary),
                StatusMessage = string.IsNullOrWhiteSpace(statusMessage)
                    ? legacyNotice
                        ? "旧旅程已安全归档，v0.4.0 需要开始新旅程"
                        : ToPlayerMessage(inspection.Status)
                    : statusMessage,
                StatusIsError = statusIsError ||
                                (!legacyNotice &&
                                 !canContinue &&
                                 inspection.Status != RunSaveLoadStatus.Missing),
                SaveStatus = inspection.Status,
                Page = CurrentPage,
                IsInputLocked = IsPageInputLocked || creatingRun || continuingRun,
                HeroSelectionVisible =
                    CurrentPage == JournalMenuPage.HeroSelection,
                SelectedHeroId = selectedHeroId,
                HeroOptions = BuildHeroOptions()
            });
        }

        private HeroSelectionOptionState[] BuildHeroOptions()
        {
            var profiles = GameApp.Instance?.Profiles;
            return HeroCatalog.All.Select(hero => new HeroSelectionOptionState
            {
                HeroId = hero.Id,
                DisplayName = hero.DisplayName,
                PassiveName = hero.PassiveName,
                PassiveDescription = hero.PassiveDescription,
                UnlockCondition = hero.UnlockCondition,
                IsUnlocked = profiles?.IsHeroUnlocked(hero.Id) == true,
                IsSelected = string.Equals(
                    hero.Id,
                    selectedHeroId,
                    StringComparison.Ordinal)
            }).ToArray();
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

            if (summary.Floor < 1 || summary.MaxHealth <= 0 ||
                summary.Health < 0 || summary.Health > summary.MaxHealth)
            {
                return "已有单局存档";
            }

            var chapter = string.IsNullOrWhiteSpace(summary.MapName)
                ? $"第 {summary.Floor} 章"
                : summary.MapName;
            var hero = string.IsNullOrWhiteSpace(summary.HeroName)
                ? string.Empty
                : summary.HeroName + " · ";
            return $"{hero}{chapter} · 生命 {summary.Health}/{summary.MaxHealth} · " +
                   $"护甲 {summary.Armor} · " +
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
                    return "章节完成";
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
                    return "选择新游戏，开始书写新的旅团日记";
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

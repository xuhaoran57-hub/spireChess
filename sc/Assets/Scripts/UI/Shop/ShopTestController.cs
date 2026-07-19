using System;
using System.Collections.Generic;
using System.Linq;
using SpireChess.App;
using SpireChess.Config;
using SpireChess.Run;
using SpireChess.Shop;
using SpireChess.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpireChess.UI.Shop
{
    public sealed class ShopTestController : MonoBehaviour
    {
        private readonly List<string> eventLog = new List<string>();
        [SerializeField] private ShopScreenView screenView;
        private ShopSession session;
        private RunSession runSession;
        private int selectedBenchIndex = -1;
        private int selectedBattleIndex = -1;
        private int selectedEffectTargetIndex = -1;
        private bool initialized;
        private bool subscribed;
        private bool statusIsError;
        private int statusRevision;
        private int renderedFormalStatusRevision = -1;

        public ShopSession Session => session;
        public bool IsInitialized => initialized;
        public bool IsUsingFormalView => initialized && screenView != null;
        public ShopScreenView FormalScreenView => screenView;
        public bool DiscoverModalVisible =>
            screenView != null && screenView.IsChoiceVisible;
        public bool DiscoverCancelInteractable =>
            screenView != null && screenView.ChoiceCanCancel;
        public bool RewardModalVisible =>
            screenView != null && screenView.IsRewardVisible;
        public int EventLogCount => eventLog.Count;
        public int SelectedBenchIndex => selectedBenchIndex;
        public string ResourceSummary => initialized
            ? BuildResourceSummary()
            : string.Empty;
        public ShopOperationResult LastOperationResult { get; private set; }
        public string StatusMessage { get; private set; }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Start()
        {
            if (initialized)
            {
                return;
            }

            if (screenView == null)
            {
                Debug.LogError("[ShopTest] Formal ShopScreenView is not configured.");
                return;
            }

            if (GameApp.Instance == null || GameApp.Instance.Run == null)
            {
                Debug.LogError("[ShopTest] GameApp is not ready.");
                return;
            }

            Initialize(GameApp.Instance.Run.Shop, GameApp.Instance.Run);
        }

        public void InitializeForTests(ShopSession customSession, RunSession customRunSession = null)
        {
            if (initialized)
            {
                throw new InvalidOperationException("ShopTestController is already initialized.");
            }

            Initialize(customSession, customRunSession);
        }

        public void ConfigureFormalViewForTests(ShopScreenView value)
        {
            if (initialized)
            {
                throw new InvalidOperationException(
                    "Configure the formal view before initialization.");
            }

            screenView = value ?? throw new ArgumentNullException(nameof(value));
        }

        private void Initialize(ShopSession value, RunSession owningRunSession)
        {
            session = value ?? throw new ArgumentNullException(nameof(value));
            runSession = owningRunSession;
            initialized = true;
            Subscribe();
            if (!session.IsShopOpen)
            {
                LastOperationResult = runSession == null
                    ? session.StartNextRound()
                    : runSession.EnsureShopOpen();
            }

            if (screenView != null)
            {
                screenView.gameObject.SetActive(true);
                screenView.Bind(this);
            }
            SetStatus($"第 {session.Round} 回合商店已开启");
            RefreshAll();
        }

        public ShopOperationResult BuyMinionAt(int offerIndex)
        {
            return ApplyOperation(session.BuyMinion(offerIndex), "已购买随从", true);
        }

        public ShopOperationResult BuySpellOffer()
        {
            return ApplyOperation(session.BuySpell(), "已购买法术", true);
        }

        public ShopOperationResult RefreshShop()
        {
            return ApplyOperation(session.Refresh(), "商店已刷新", true);
        }

        public ShopOperationResult UpgradeTavern()
        {
            return ApplyOperation(session.UpgradeTavern(), "酒馆已升级", true);
        }

        public ShopOperationResult ToggleFreeze()
        {
            return ApplyOperation(
                session.ToggleFreeze(),
                session.IsFrozen ? "商店已冻结" : "商店已解冻",
                false);
        }

        public ShopOperationResult SellSelectedBattleMinion()
        {
            if (selectedBattleIndex < 0)
            {
                return ApplyOperation(
                    ShopOperationResult.Fail(ShopOperationError.InvalidIndex),
                    null,
                    false);
            }

            return ApplyOperation(
                session.SellBattleMinion(selectedBattleIndex),
                "已出售战斗区随从",
                true);
        }

        public ShopOperationResult PlayBenchMinion(
            int benchIndex,
            int battleIndex,
            int effectTargetBattleIndex = -1)
        {
            return ApplyOperation(
                session.PlayMinion(benchIndex, battleIndex, effectTargetBattleIndex),
                "随从已上场",
                true);
        }

        public ShopOperationResult RepositionBattleMinion(int sourceIndex, int targetIndex)
        {
            return ApplyOperation(
                session.RepositionBattleMinion(sourceIndex, targetIndex),
                "战斗区站位已调整",
                true);
        }

        public ShopOperationResult UseBenchSpell(int benchIndex, int targetBattleIndex = -1)
        {
            return ApplyOperation(
                session.UseSpell(benchIndex, targetBattleIndex),
                !HasPendingChoice ? "法术已使用" : "请选择一个效果结果",
                true);
        }

        public ShopOperationResult SelectDiscoverCandidate(int candidateIndex)
        {
            return ApplyOperation(
                session.PendingChoice != null
                    ? session.SelectEffectChoice(candidateIndex)
                    : session.SelectDiscover(candidateIndex),
                "选择已完成",
                true);
        }

        public ShopOperationResult CancelDiscover()
        {
            return ApplyOperation(
                session.PendingChoice != null
                    ? session.CancelEffectChoice()
                    : session.CancelDiscover(),
                "已取消选择",
                true);
        }

        private bool HasPendingChoice =>
            session.PendingDiscover != null || session.PendingChoice != null;

        public ShopOperationResult EndShopAndEnterBattle()
        {
            if (runSession == null || !ReferenceEquals(runSession.Shop, session))
            {
                return ApplyOperation(
                    ShopOperationResult.Fail(ShopOperationError.InvalidTiming),
                    null,
                    false);
            }

            var mapShop = runSession.State.Phase == RunPhase.Shop &&
                          runSession.State.CurrentAttempt?.NodeType == RunNodeType.Shop;
            var result = runSession.EndShopAndPrepareBattle(mapShop ? "RunTest" : "ShopTest");
            ApplyOperation(result, mapShop ? "商店已结束，返回地图" : "阵容已锁定，进入战斗", true);
            if (result.Success)
            {
                SceneManager.LoadScene(mapShop ? "RunTest" : "BattleTest");
            }

            return result;
        }

        public RunOperationResult ClaimPendingReward()
        {
            if (runSession == null)
            {
                return RunOperationResult.Fail(RunOperationError.InvalidTiming);
            }

            var result = runSession.ClaimNextCardReward();
            SetStatus(
                result.Success ? result.Message : BuildRunErrorMessage(result.Error),
                !result.Success);
            RefreshAll();
            return result;
        }

        public RunOperationResult SkipPendingReward()
        {
            if (runSession == null)
            {
                return RunOperationResult.Fail(RunOperationError.InvalidTiming);
            }

            var result = runSession.SkipNextCardReward();
            SetStatus(
                result.Success ? result.Message : BuildRunErrorMessage(result.Error),
                !result.Success);
            RefreshAll();
            return result;
        }

        public bool CanDrag(ShopCardZone zone, int index)
        {
            if (!initialized || !session.IsShopOpen || HasPendingChoice)
            {
                return false;
            }

            if (zone == ShopCardZone.Battle)
            {
                return IsValidCard(session.Collection.Battle, index, ShopCardType.Minion);
            }

            return zone == ShopCardZone.Bench &&
                   IsValidCard(session.Collection.Bench, index, ShopCardType.Minion);
        }

        public void HandleCardClick(ShopCardZone zone, int index)
        {
            if (!initialized || HasPendingChoice)
            {
                return;
            }

            switch (zone)
            {
                case ShopCardZone.MinionOffer:
                    BuyMinionAt(index);
                    break;
                case ShopCardZone.SpellOffer:
                    BuySpellOffer();
                    break;
                case ShopCardZone.Bench:
                    SelectBenchCard(index);
                    break;
                case ShopCardZone.Battle:
                    HandleBattleSlotSelection(index);
                    break;
            }
        }

        public void HandleSlotClick(ShopCardZone zone, int index)
        {
            if (zone == ShopCardZone.Battle)
            {
                HandleBattleSlotSelection(index);
            }
        }

        public void HandleDrop(
            ShopCardZone sourceZone,
            int sourceIndex,
            ShopCardZone targetZone,
            int targetIndex)
        {
            if (targetZone != ShopCardZone.Battle)
            {
                RefreshAll();
                return;
            }

            if (sourceZone == ShopCardZone.Bench)
            {
                PlayBenchMinion(sourceIndex, targetIndex, selectedEffectTargetIndex);
            }
            else if (sourceZone == ShopCardZone.Battle)
            {
                RepositionBattleMinion(sourceIndex, targetIndex);
            }
        }

        private void SelectBenchCard(int index)
        {
            if (!IsValidIndex(session.Collection.Bench, index))
            {
                return;
            }

            if (selectedBenchIndex == index)
            {
                ClearSelection();
                SetStatus("已取消选择");
                RefreshAll();
                return;
            }

            var card = session.Collection.Bench[index];
            selectedBenchIndex = index;
            selectedBattleIndex = -1;
            selectedEffectTargetIndex = -1;
            if (card.CardType == ShopCardType.Spell)
            {
                var result = session.UseSpell(index);
                if (result.Success)
                {
                    ApplyOperation(
                        result,
                        !HasPendingChoice ? "法术已使用" : "请选择一个效果结果",
                        true);
                    return;
                }

                LastOperationResult = result;
                SetStatus(
                    result.Error == ShopOperationError.InvalidTarget
                        ? "请选择一个战斗区随从作为法术目标"
                        : BuildErrorMessage(result.Error),
                    true);
            }
            else
            {
                SetStatus("请选择空战斗位；如需战吼目标，先点击目标随从");
            }

            RefreshAll();
        }

        private void HandleBattleSlotSelection(int index)
        {
            if (!IsValidIndex(session.Collection.Battle, index))
            {
                return;
            }

            var target = session.Collection.Battle[index];
            if (selectedBenchIndex >= 0 &&
                IsValidIndex(session.Collection.Bench, selectedBenchIndex))
            {
                var selected = session.Collection.Bench[selectedBenchIndex];
                if (selected != null && selected.CardType == ShopCardType.Spell)
                {
                    UseBenchSpell(selectedBenchIndex, index);
                    return;
                }

                if (target != null)
                {
                    selectedEffectTargetIndex = index;
                    SetStatus($"已选择 {index + 1} 号位为战吼目标，请点击或拖入空战斗位");
                    RefreshAll();
                    return;
                }

                PlayBenchMinion(selectedBenchIndex, index, selectedEffectTargetIndex);
                return;
            }

            if (selectedBattleIndex >= 0)
            {
                if (selectedBattleIndex == index)
                {
                    ClearSelection();
                    SetStatus("已取消选择");
                    RefreshAll();
                    return;
                }

                RepositionBattleMinion(selectedBattleIndex, index);
                return;
            }

            if (target != null)
            {
                selectedBattleIndex = index;
                selectedBenchIndex = -1;
                SetStatus("已选择战斗区随从：点击其他位置换位，或点击出售");
                RefreshAll();
            }
        }

        private ShopOperationResult ApplyOperation(
            ShopOperationResult result,
            string successMessage,
            bool clearSelection)
        {
            LastOperationResult = result;
            if (result.Success)
            {
                if (clearSelection)
                {
                    ClearSelection();
                }

                SetStatus(successMessage ?? "操作成功");
            }
            else
            {
                SetStatus(BuildErrorMessage(result.Error), true);
            }

            RefreshAll();
            return result;
        }

        private void Subscribe()
        {
            if (subscribed || session == null)
            {
                return;
            }

            session.EventRaised += OnShopEvent;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || session == null)
            {
                return;
            }

            session.EventRaised -= OnShopEvent;
            subscribed = false;
        }

        private void OnShopEvent(ShopEventData eventData)
        {
            var cardName = eventData.Card == null
                ? string.Empty
                : eventData.Card.CardType == ShopCardType.Minion
                    ? eventData.Card.Minion.Name
                    : eventData.Card.Spell.Name;
            eventLog.Add(string.IsNullOrEmpty(cardName)
                ? ToEventText(eventData.Type)
                : $"{ToEventText(eventData.Type)}：{cardName}");
            if (eventLog.Count > 8)
            {
                eventLog.RemoveAt(0);
            }
        }

        private void RefreshAll()
        {
            if (!initialized || screenView == null)
            {
                return;
            }

            var state = ShopScreenStateBuilder.Build(
                session,
                runSession,
                selectedHandIndex: selectedBenchIndex,
                selectedBattleIndex: selectedBattleIndex,
                selectedEffectTargetIndex: selectedEffectTargetIndex,
                statusMessage: StatusMessage);
            screenView.Render(state);
            screenView.RenderChoice(BuildChoiceViewModel());
            screenView.RenderReward(BuildPendingRewardMessage());
            if (renderedFormalStatusRevision != statusRevision)
            {
                screenView.ShowStatus(StatusMessage, statusIsError);
                renderedFormalStatusRevision = statusRevision;
            }
        }

        private ChoiceViewModel BuildChoiceViewModel()
        {
            var discover = session.PendingDiscover;
            if (discover != null)
            {
                return new ChoiceViewModel
                {
                    Title = "选择一个发现随从",
                    Description = discover.CanCancel
                        ? "选择一张卡牌，或取消本次发现。"
                        : "三连发现必须选择一张卡牌。",
                    CanCancel = discover.CanCancel,
                    Candidates = discover.Candidates
                        .Select(candidate => new ChoiceCandidateViewModel
                        {
                            Id = candidate.Id,
                            Label = candidate.Name,
                            Card = ToChoiceCard(candidate)
                        })
                        .ToArray()
                };
            }

            var effectChoice = session.PendingChoice;
            if (effectChoice == null)
            {
                return null;
            }

            return new ChoiceViewModel
            {
                Title = ToChoiceTitle(effectChoice.ChoiceType),
                Description = "请选择一个效果结果后继续商店操作。",
                CanCancel = true,
                Candidates = effectChoice.Candidates
                    .Select(ToChoiceCandidate)
                    .ToArray()
            };
        }

        private static ChoiceCandidateViewModel ToChoiceCandidate(
            EffectChoiceCandidate candidate)
        {
            if (candidate.Minion != null)
            {
                return new ChoiceCandidateViewModel
                {
                    Id = candidate.Id,
                    Label = candidate.DisplayName,
                    Card = ToChoiceCard(candidate.Minion)
                };
            }

            if (candidate.Spell != null)
            {
                return new ChoiceCandidateViewModel
                {
                    Id = candidate.Id,
                    Label = candidate.DisplayName,
                    Card = ToChoiceCard(candidate.Spell)
                };
            }

            if (candidate.Target != null)
            {
                var card = ShopCardViewModelFactory.FromOwned(
                    candidate.Target,
                    false);
                card.DisplayMode = CardDisplayMode.Full;
                card.ShowCost = false;
                return new ChoiceCandidateViewModel
                {
                    Id = candidate.Id,
                    Label = candidate.DisplayName,
                    Description = "复制该随从的基础形态",
                    Card = card
                };
            }

            return new ChoiceCandidateViewModel
            {
                Id = candidate.Id,
                Label = ToRaceName(candidate.DisplayName),
                Description = "选择该种族作为效果目标"
            };
        }

        private static CardViewModel ToChoiceCard(MinionConfig config)
        {
            var card = ShopCardViewModelFactory.FromOffer(config, int.MaxValue);
            card.ShowCost = false;
            card.IsInteractable = true;
            card.IsAffordable = true;
            return card;
        }

        private static CardViewModel ToChoiceCard(SpellConfig config)
        {
            var card = ShopCardViewModelFactory.FromOffer(config, int.MaxValue);
            card.ShowCost = false;
            card.IsInteractable = true;
            card.IsAffordable = true;
            return card;
        }

        private static string ToChoiceTitle(EffectChoiceType choiceType)
        {
            switch (choiceType)
            {
                case EffectChoiceType.MinionCard: return "选择一个随从";
                case EffectChoiceType.SpellCard: return "选择一个法术";
                case EffectChoiceType.BattleTarget: return "选择一个复制目标";
                case EffectChoiceType.Race: return "选择一个种族";
                default: return "选择一个效果结果";
            }
        }

        private static string ToRaceName(string race)
        {
            switch (race)
            {
                case "ForgeSoul": return "铸魂";
                case "WildSpirit": return "荒灵";
                case "Starbound": return "星契";
                case "Wayfarer": return "旅团";
                default: return string.IsNullOrWhiteSpace(race) ? "无种族" : race;
            }
        }

        private string BuildPendingRewardMessage()
        {
            var reward = runSession?.State.PendingCardRewards.Count > 0
                ? runSession.State.PendingCardRewards[0]
                : null;
            if (reward == null)
            {
                return null;
            }

            var displayName = reward.ConfigId;
            var configs = GameApp.Instance == null
                ? null
                : GameApp.Instance.Configs;
            if (configs != null &&
                reward.CardType == ShopCardType.Minion &&
                configs.TryGetMinion(reward.ConfigId, out var minion))
            {
                displayName = minion.Name;
            }
            else if (configs != null &&
                     reward.CardType == ShopCardType.Spell &&
                     configs.TryGetSpell(reward.ConfigId, out var spell))
            {
                displayName = spell.Name;
            }

            return $"{displayName}\n队列剩余 " +
                   $"{runSession.State.PendingCardRewards.Count} 项";
        }

        private string BuildResourceSummary()
        {
            return $"回合 {session.Round}   金币 {session.Gold}   " +
                   $"酒馆 T{session.TavernTier}   " +
                   $"升级 {session.CurrentUpgradeCost}   " +
                   $"刷新 {session.RefreshCount}   " +
                   $"免费刷新 {session.FreeRefreshes}";
        }

        private void SetStatus(string message, bool isError = false)
        {
            StatusMessage = message ?? string.Empty;
            statusIsError = isError;
            statusRevision++;
        }

        private void ClearSelection()
        {
            selectedBenchIndex = -1;
            selectedBattleIndex = -1;
            selectedEffectTargetIndex = -1;
        }

        private static bool IsValidIndex<T>(IReadOnlyList<T> list, int index)
        {
            return index >= 0 && index < list.Count;
        }

        private static bool IsValidCard(
            IReadOnlyList<ShopCardInstance> cards,
            int index,
            ShopCardType type)
        {
            return IsValidIndex(cards, index) && cards[index] != null && cards[index].CardType == type;
        }

        private static string ToEventText(ShopEventType type)
        {
            switch (type)
            {
                case ShopEventType.OnShopPhaseStart: return "商店开始";
                case ShopEventType.OnShopPhaseEnd: return "商店结束";
                case ShopEventType.OnRefresh: return "刷新";
                case ShopEventType.OnBuy: return "购买";
                case ShopEventType.OnSell: return "出售";
                case ShopEventType.OnPlay: return "上场";
                case ShopEventType.OnSpellUsed: return "使用法术";
                case ShopEventType.OnTripleFormed: return "三连合成";
                case ShopEventType.OnTripleRewardGranted: return "获得发现奖励";
                case ShopEventType.OnDiscoverStarted: return "发现开始";
                case ShopEventType.OnDiscoverResolved: return "发现完成";
                case ShopEventType.OnDiscoverCancelled: return "发现取消";
                case ShopEventType.OnTavernUpgraded: return "酒馆升级";
                default: return type.ToString();
            }
        }

        private static string BuildErrorMessage(ShopOperationError error)
        {
            switch (error)
            {
                case ShopOperationError.ShopClosed: return "商店已经关闭";
                case ShopOperationError.InsufficientGold: return "金币不足";
                case ShopOperationError.BenchFull: return "备战区已满";
                case ShopOperationError.OccupiedBattleSlot: return "目标战斗位已有随从";
                case ShopOperationError.InvalidTarget: return "目标不合法，请选择有效战斗区随从";
                case ShopOperationError.NoBenefit: return "该操作当前没有收益，法术未消耗";
                case ShopOperationError.DiscoveryPending: return "请先完成当前选择";
                case ShopOperationError.DiscoveryCannotBeCancelled: return "三连发现必须选择一个候选";
                case ShopOperationError.AlreadyUpgradedThisRound: return "本回合已经升级过酒馆";
                case ShopOperationError.MaximumTavernTier: return "酒馆已经满级";
                case ShopOperationError.InvalidCardLocation: return "该位置的卡牌不能执行此操作";
                case ShopOperationError.InvalidTiming: return "当前流程不能执行此操作";
                case ShopOperationError.EmptySlot: return "该位置没有卡牌";
                default: return "操作失败：" + error;
            }
        }

        private static string BuildRunErrorMessage(RunOperationError error)
        {
            switch (error)
            {
                case RunOperationError.BenchFull: return "备战区已满，可以跳过该奖励";
                case RunOperationError.NoPendingCardReward: return "没有待领取奖励";
                case RunOperationError.InvalidPhase: return "当前不在商店奖励阶段";
                default: return "奖励操作失败：" + error;
            }
        }

    }
}

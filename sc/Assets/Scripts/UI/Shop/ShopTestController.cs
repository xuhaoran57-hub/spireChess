using System;
using System.Collections.Generic;
using System.Linq;
using SpireChess.App;
using SpireChess.Config;
using SpireChess.Run;
using SpireChess.Shop;
using SpireChess.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpireChess.UI.Shop
{
    public sealed class ShopTestController : MonoBehaviour
    {
        private static readonly Color BackgroundColor = new Color(0.07f, 0.09f, 0.11f, 1f);
        private static readonly Color PanelColor = new Color(0.13f, 0.16f, 0.18f, 0.96f);
        private static readonly Color OfferSlotColor = new Color(0.24f, 0.19f, 0.15f, 1f);
        private static readonly Color BattleSlotColor = new Color(0.17f, 0.25f, 0.28f, 1f);
        private static readonly Color BenchSlotColor = new Color(0.19f, 0.20f, 0.24f, 1f);
        private static readonly Color SelectedColor = new Color(0.35f, 0.95f, 0.58f, 1f);

        private readonly Dictionary<string, Transform> slotRoots = new Dictionary<string, Transform>();
        private readonly List<string> eventLog = new List<string>();
        private ShopSession session;
        private RunSession runSession;
        private Canvas canvas;
        private Text resourceText;
        private Text statusText;
        private Text logText;
        private Button refreshButton;
        private Button upgradeButton;
        private Button freezeButton;
        private Button sellButton;
        private Button endButton;
        private GameObject discoverOverlay;
        private RectTransform discoverCandidatesRoot;
        private Button discoverCancelButton;
        private GameObject rewardOverlay;
        private Text rewardText;
        private int selectedBenchIndex = -1;
        private int selectedBattleIndex = -1;
        private int selectedEffectTargetIndex = -1;
        private bool initialized;
        private bool subscribed;
        private static Font uiFont;

        public ShopSession Session => session;
        public bool IsInitialized => initialized;
        public bool DiscoverModalVisible => discoverOverlay != null && discoverOverlay.activeSelf;
        public bool DiscoverCancelInteractable =>
            discoverCancelButton != null && discoverCancelButton.interactable;
        public bool RewardModalVisible => rewardOverlay != null && rewardOverlay.activeSelf;
        public int EventLogCount => eventLog.Count;
        public int SelectedBenchIndex => selectedBenchIndex;
        public string ResourceSummary => resourceText == null ? string.Empty : resourceText.text;
        public ShopOperationResult LastOperationResult { get; private set; }
        public string StatusMessage { get; private set; }

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
            if (SceneManager.GetActiveScene().name != "ShopTest" ||
                FindObjectOfType<ShopTestController>() != null)
            {
                return;
            }

            new GameObject("ShopTestController").AddComponent<ShopTestController>();
        }

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
            if (canvas != null)
            {
                Destroy(canvas.gameObject);
            }
        }

        private void Start()
        {
            if (initialized)
            {
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

            BuildUi();
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

            var returnScene = runSession.State.Phase == RunPhase.Shop &&
                              runSession.State.CurrentAttempt != null
                ? "RunTest"
                : "ShopTest";
            var result = runSession.EndShopAndPrepareBattle(returnScene);
            ApplyOperation(result, "阵容已锁定，进入战斗", true);
            if (result.Success)
            {
                SceneManager.LoadScene("BattleTest");
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
            SetStatus(result.Success ? result.Message : BuildRunErrorMessage(result.Error));
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
            SetStatus(result.Success ? result.Message : BuildRunErrorMessage(result.Error));
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
                SetStatus(result.Error == ShopOperationError.InvalidTarget
                    ? "请选择一个战斗区随从作为法术目标"
                    : BuildErrorMessage(result.Error));
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
                SetStatus(BuildErrorMessage(result.Error));
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

            RefreshAll();
        }

        private void BuildUi()
        {
            EnsureEventSystem();
            canvas = CreateCanvas();
            var root = CreateRect("ShopTestRoot", canvas.transform);
            Stretch(root);
            AddImage(root.gameObject, BackgroundColor);

            var top = CreatePanel("TopBar", root, PanelColor);
            Anchor(top, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -82f), Vector2.zero);
            var title = CreateText("Title", top, "ShopTest", 30, TextAnchor.MiddleLeft);
            Anchor(title.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(22f, 0f), new Vector2(180f, 0f));
            resourceText = CreateText("Resources", top, string.Empty, 19, TextAnchor.MiddleLeft);
            Anchor(resourceText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(180f, 0f), new Vector2(650f, 0f));
            statusText = CreateText("Status", top, string.Empty, 17, TextAnchor.MiddleLeft);
            Anchor(statusText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(650f, 0f), new Vector2(-620f, 0f));

            refreshButton = CreateButton("Refresh", top, "刷新", () => RefreshShop());
            PlaceTopButton(refreshButton, 0);
            upgradeButton = CreateButton("Upgrade", top, "升级", () => UpgradeTavern());
            PlaceTopButton(upgradeButton, 1);
            freezeButton = CreateButton("Freeze", top, "冻结", () => ToggleFreeze());
            PlaceTopButton(freezeButton, 2);
            sellButton = CreateButton("Sell", top, "出售", () => SellSelectedBattleMinion());
            PlaceTopButton(sellButton, 3);
            endButton = CreateButton("End", top, "结束商店", () => EndShopAndEnterBattle());
            PlaceTopButton(endButton, 4, 126f);

            CreateOfferRow(root);
            CreateSlotsRow(root, ShopCardZone.Battle, "战斗区", ShopEconomyRules.BattleSlotCount, BattleSlotColor, 0.38f, 0.62f);
            CreateSlotsRow(root, ShopCardZone.Bench, "备战区", ShopEconomyRules.BenchSlotCount, BenchSlotColor, 0.10f, 0.34f);

            var logPanel = CreatePanel("EventLog", root, PanelColor);
            Anchor(logPanel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(18f, 10f), new Vector2(-18f, 78f));
            logText = CreateText("Log", logPanel, string.Empty, 15, TextAnchor.MiddleLeft);
            Anchor(logText.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 6f), new Vector2(-14f, -6f));

            BuildDiscoverOverlay(root);
            BuildRewardOverlay(root);
        }

        private void CreateOfferRow(RectTransform root)
        {
            var panel = CreatePanel("Offers", root, PanelColor);
            Anchor(panel, new Vector2(0f, 0.66f), new Vector2(1f, 0.94f), new Vector2(18f, 0f), new Vector2(-18f, 0f));
            var label = CreateText("Label", panel, "商店", 20, TextAnchor.MiddleLeft);
            Anchor(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -38f), new Vector2(90f, -6f));
            for (var i = 0; i < 5; i++)
            {
                var zone = i == 4 ? ShopCardZone.SpellOffer : ShopCardZone.MinionOffer;
                CreateSlot(panel, zone, i == 4 ? 0 : i, i, 5, OfferSlotColor);
            }
        }

        private void CreateSlotsRow(
            RectTransform root,
            ShopCardZone zone,
            string labelText,
            int count,
            Color slotColor,
            float minY,
            float maxY)
        {
            var panel = CreatePanel(zone + "Row", root, PanelColor);
            Anchor(panel, new Vector2(0f, minY), new Vector2(1f, maxY), new Vector2(18f, 0f), new Vector2(-18f, 0f));
            var label = CreateText("Label", panel, labelText, 20, TextAnchor.MiddleLeft);
            Anchor(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -38f), new Vector2(100f, -6f));
            for (var i = 0; i < count; i++)
            {
                CreateSlot(panel, zone, i, i, count, slotColor);
            }
        }

        private void CreateSlot(
            RectTransform parent,
            ShopCardZone zone,
            int index,
            int visualIndex,
            int count,
            Color color)
        {
            const float gap = 0.012f;
            var left = 0.06f + visualIndex * (0.92f / count);
            var right = 0.06f + (visualIndex + 1) * (0.92f / count) - gap;
            var slot = CreatePanel(zone + "Slot" + index, parent, color);
            Anchor(slot, new Vector2(left, 0.08f), new Vector2(right, 0.84f), Vector2.zero, Vector2.zero);
            var outline = slot.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.18f);
            outline.effectDistance = new Vector2(2f, -2f);
            slot.gameObject.AddComponent<ShopSlotView>().Initialize(this, zone, index);
            var content = CreateRect("Content", slot);
            Stretch(content);
            slotRoots[BuildSlotKey(zone, index)] = content;
        }

        private void BuildDiscoverOverlay(RectTransform root)
        {
            discoverOverlay = new GameObject("DiscoverOverlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            discoverOverlay.transform.SetParent(root, false);
            var rect = discoverOverlay.GetComponent<RectTransform>();
            Stretch(rect);
            discoverOverlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

            var panel = CreatePanel("DiscoverPanel", rect, new Color(0.12f, 0.15f, 0.20f, 1f));
            Anchor(panel, new Vector2(0.18f, 0.22f), new Vector2(0.82f, 0.78f), Vector2.zero, Vector2.zero);
            var title = CreateText("Title", panel, "选择一个发现随从", 28, TextAnchor.MiddleCenter);
            Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -70f), new Vector2(-20f, -16f));
            discoverCandidatesRoot = CreateRect("Candidates", panel);
            Anchor(discoverCandidatesRoot, new Vector2(0f, 0.18f), new Vector2(1f, 0.82f), new Vector2(24f, 0f), new Vector2(-24f, 0f));
            discoverCancelButton = CreateButton(
                "Cancel",
                panel,
                "取消选择",
                () => CancelDiscover());
            Anchor(discoverCancelButton.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-90f, 18f), new Vector2(90f, 64f));
            discoverOverlay.SetActive(false);
        }

        private void BuildRewardOverlay(RectTransform root)
        {
            rewardOverlay = new GameObject(
                "RewardOverlay",
                typeof(RectTransform),
                typeof(Image),
                typeof(CanvasGroup));
            rewardOverlay.transform.SetParent(root, false);
            var rect = rewardOverlay.GetComponent<RectTransform>();
            Stretch(rect);
            rewardOverlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

            var panel = CreatePanel("RewardPanel", rect, new Color(0.12f, 0.18f, 0.16f, 1f));
            Anchor(panel, new Vector2(0.28f, 0.3f), new Vector2(0.72f, 0.7f), Vector2.zero, Vector2.zero);
            var title = CreateText("Title", panel, "待领取卡牌奖励", 28, TextAnchor.MiddleCenter);
            Anchor(title.rectTransform, new Vector2(0f, 0.72f), Vector2.one, new Vector2(20f, 0f), new Vector2(-20f, -12f));
            rewardText = CreateText("Reward", panel, string.Empty, 22, TextAnchor.MiddleCenter);
            Anchor(rewardText.rectTransform, new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.72f), Vector2.zero, Vector2.zero);
            var claim = CreateButton("Claim", panel, "领取", () => ClaimPendingReward());
            Anchor(claim.GetComponent<RectTransform>(), new Vector2(0.12f, 0.08f), new Vector2(0.46f, 0.3f), Vector2.zero, Vector2.zero);
            var skip = CreateButton("Skip", panel, "跳过并返还牌池", () => SkipPendingReward());
            Anchor(skip.GetComponent<RectTransform>(), new Vector2(0.54f, 0.08f), new Vector2(0.88f, 0.3f), Vector2.zero, Vector2.zero);
            rewardOverlay.SetActive(false);
        }

        private void RefreshAll()
        {
            if (!initialized || canvas == null)
            {
                return;
            }

            resourceText.text = $"回合 {session.Round}   金币 {session.Gold}   酒馆 T{session.TavernTier}   " +
                                $"升级 {session.CurrentUpgradeCost}   刷新 {session.RefreshCount}   " +
                                $"免费刷新 {session.FreeRefreshes}";
            statusText.text = StatusMessage ?? string.Empty;
            logText.text = eventLog.Count == 0 ? "等待商店操作" : string.Join("   |   ", eventLog);

            RebuildOffers();
            RebuildCollection(ShopCardZone.Battle, session.Collection.Battle);
            RebuildCollection(ShopCardZone.Bench, session.Collection.Bench);
            RefreshDiscover();
            RefreshReward();

            var hasPendingReward = runSession != null &&
                                   runSession.State.PendingCardRewards.Count > 0;
            var unlocked = session.IsShopOpen && !HasPendingChoice &&
                           !hasPendingReward;
            refreshButton.interactable = unlocked;
            upgradeButton.interactable = unlocked && session.TavernTier < ShopEconomyRules.MaximumTavernTier;
            freezeButton.interactable = unlocked;
            sellButton.interactable = unlocked && selectedBattleIndex >= 0;
            endButton.interactable = unlocked && runSession != null;
            freezeButton.GetComponentInChildren<Text>().text = session.IsFrozen ? "解冻" : "冻结";
        }

        private void RefreshReward()
        {
            if (rewardOverlay == null)
            {
                return;
            }

            var reward = runSession?.State.PendingCardRewards.Count > 0
                ? runSession.State.PendingCardRewards[0]
                : null;
            rewardOverlay.SetActive(reward != null);
            if (reward == null)
            {
                return;
            }

            var displayName = reward.ConfigId;
            if (reward.CardType == ShopCardType.Minion &&
                GameApp.Instance.Configs.TryGetMinion(reward.ConfigId, out var minion))
            {
                displayName = minion.Name;
            }
            else if (reward.CardType == ShopCardType.Spell &&
                     GameApp.Instance.Configs.TryGetSpell(reward.ConfigId, out var spell))
            {
                displayName = spell.Name;
            }

            rewardText.text = $"{displayName}\n队列剩余 {runSession.State.PendingCardRewards.Count} 项";
        }

        private void RebuildOffers()
        {
            for (var i = 0; i < 4; i++)
            {
                ClearSlot(ShopCardZone.MinionOffer, i);
                if (i < session.MinionOffers.Count && session.MinionOffers[i] != null)
                {
                    CreateConfigCard(
                        GetSlotRoot(ShopCardZone.MinionOffer, i),
                        ShopCardZone.MinionOffer,
                        i,
                        session.MinionOffers[i],
                        null);
                }
            }

            ClearSlot(ShopCardZone.SpellOffer, 0);
            if (session.SpellOffer != null)
            {
                CreateConfigCard(
                    GetSlotRoot(ShopCardZone.SpellOffer, 0),
                    ShopCardZone.SpellOffer,
                    0,
                    null,
                    session.SpellOffer);
            }
        }

        private void RebuildCollection(ShopCardZone zone, IReadOnlyList<ShopCardInstance> cards)
        {
            for (var i = 0; i < cards.Count; i++)
            {
                ClearSlot(zone, i);
                if (cards[i] != null)
                {
                    CreateInstanceCard(GetSlotRoot(zone, i), zone, i, cards[i]);
                }
            }
        }

        private void CreateConfigCard(
            Transform parent,
            ShopCardZone zone,
            int index,
            MinionConfig minion,
            SpellConfig spell)
        {
            if (minion != null)
            {
                CreateCard(parent, zone, index, minion.Name,
                    BuildMinionSubtitle(
                        minion.Attack,
                        minion.Health,
                        minion.Tier,
                        minion.Race,
                        minion.Keywords?.Contains("Shield") == true,
                        false,
                        true),
                    minion.GetPrototypeDescription(false), false,
                    CardTierPalette.GetBackground(minion.Tier));
            }
            else
            {
                CreateCard(parent, zone, index, spell.Name,
                    $"法术 · T{spell.Tier} · 1金",
                    spell.Description, false,
                    CardTierPalette.GetBackground(spell.Tier));
            }
        }

        private void CreateInstanceCard(
            Transform parent,
            ShopCardZone zone,
            int index,
            ShopCardInstance card)
        {
            var selected = zone == ShopCardZone.Bench
                ? selectedBenchIndex == index
                : zone == ShopCardZone.Battle &&
                  (selectedBattleIndex == index || selectedEffectTargetIndex == index);
            if (card.CardType == ShopCardType.Minion)
            {
                CreateCard(parent, zone, index,
                    card.IsGolden ? "金色" + card.Minion.Name : card.Minion.Name,
                    BuildMinionSubtitle(
                        card.CurrentAttack,
                        card.CurrentHealth,
                        card.Minion.Tier,
                        card.Minion.Race,
                        card.HasPermanentShield,
                        card.HasPendingCombatShield,
                        false),
                    card.Minion.GetPrototypeDescription(card.IsGolden),
                    true, selected
                        ? SelectedColor
                        : CardTierPalette.GetBackground(card.Minion.Tier));
            }
            else
            {
                CreateCard(parent, zone, index, card.Spell.Name,
                    $"法术 · T{card.Spell.Tier}", card.Spell.Description,
                    false, selected
                        ? SelectedColor
                        : CardTierPalette.GetBackground(card.Spell.Tier));
            }
        }

        private void CreateCard(
            Transform parent,
            ShopCardZone zone,
            int index,
            string title,
            string subtitle,
            string description,
            bool draggable,
            Color color)
        {
            var rect = CreatePanel("Card", parent, color);
            Stretch(rect, new Vector2(5f, 5f), new Vector2(-5f, -5f));
            rect.gameObject.AddComponent<CanvasGroup>();
            var titleText = CreateText("Name", rect, title, 18, TextAnchor.UpperCenter);
            titleText.color = Color.black;
            Anchor(titleText.rectTransform, new Vector2(0f, 0.66f), new Vector2(1f, 1f), new Vector2(6f, 0f), new Vector2(-6f, -5f));
            var subtitleText = CreateText("Subtitle", rect, subtitle, 15, TextAnchor.MiddleCenter);
            subtitleText.color = new Color(0.12f, 0.10f, 0.08f, 1f);
            Anchor(subtitleText.rectTransform, new Vector2(0f, 0.48f), new Vector2(1f, 0.68f), new Vector2(4f, 0f), new Vector2(-4f, 0f));
            var descriptionText = CreateText("Description", rect, description ?? string.Empty, 13, TextAnchor.UpperCenter);
            descriptionText.color = new Color(0.15f, 0.13f, 0.11f, 1f);
            descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            descriptionText.verticalOverflow = VerticalWrapMode.Truncate;
            Anchor(descriptionText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.48f), new Vector2(8f, 5f), new Vector2(-8f, 0f));
            rect.gameObject.AddComponent<ShopCardView>().Initialize(this, canvas, zone, index, draggable);
        }

        private void RefreshDiscover()
        {
            var state = session.PendingDiscover;
            var effectState = session.PendingChoice;
            discoverOverlay.SetActive(state != null || effectState != null);
            if (state == null && effectState == null)
            {
                return;
            }

            var canCancel = state == null || state.CanCancel;
            discoverCancelButton.interactable = canCancel;
            discoverCancelButton.GetComponentInChildren<Text>().text = canCancel
                ? "取消选择"
                : "三连发现必须选择";

            DestroyChildren(discoverCandidatesRoot);
            var count = state != null
                ? state.Candidates.Count
                : effectState.Candidates.Count;
            for (var i = 0; i < count; i++)
            {
                var candidateIndex = i;
                string label;
                if (state != null)
                {
                    var candidate = state.Candidates[i];
                    label = $"{candidate.Name}\n{candidate.Attack}/{candidate.Health} · T{candidate.Tier} · " +
                            $"{ToRaceName(candidate.Race)}\n{candidate.Description}";
                }
                else
                {
                    var candidate = effectState.Candidates[i];
                    if (candidate.Minion != null)
                    {
                        label = $"{candidate.Minion.Name}\n{candidate.Minion.Attack}/{candidate.Minion.Health} · " +
                                $"T{candidate.Minion.Tier} · {ToRaceName(candidate.Minion.Race)}\n" +
                                candidate.Minion.Description;
                    }
                    else if (candidate.Spell != null)
                    {
                        label = $"{candidate.Spell.Name}\n法术 · T{candidate.Spell.Tier}\n{candidate.Spell.Description}";
                    }
                    else if (candidate.Target != null)
                    {
                        label = $"{candidate.Target.Minion.Name}\n" +
                                $"{ToRaceName(candidate.Target.Minion.Race)} · " +
                                $"复制为基础 {candidate.Target.Minion.Attack}/{candidate.Target.Minion.Health}";
                    }
                    else
                    {
                        label = candidate.DisplayName;
                    }
                }
                var button = CreateButton(
                    "Candidate" + i,
                    discoverCandidatesRoot,
                    label,
                    () => SelectDiscoverCandidate(candidateIndex));
                var left = count == 0 ? 0f : (float)i / count;
                var right = count == 0 ? 1f : (float)(i + 1) / count;
                Anchor(button.GetComponent<RectTransform>(),
                    new Vector2(left, 0f), new Vector2(right, 1f),
                    new Vector2(8f, 4f), new Vector2(-8f, -4f));
            }
        }

        private static string BuildMinionSubtitle(
            int attack,
            int health,
            int tier,
            string race,
            bool hasPermanentShield,
            bool hasPendingCombatShield,
            bool includePrice)
        {
            var summary = $"{attack}/{health} · T{tier} · {ToRaceName(race)}";
            if (includePrice)
            {
                summary += " · 3金";
            }

            var states = new List<string>();
            if (hasPermanentShield)
            {
                states.Add("护盾");
            }

            if (hasPendingCombatShield)
            {
                states.Add("下一战护盾");
            }

            return states.Count == 0
                ? summary
                : summary + "\n[" + string.Join(" / ", states) + "]";
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

        private void SetStatus(string message)
        {
            StatusMessage = message ?? string.Empty;
            if (statusText != null)
            {
                statusText.text = StatusMessage;
            }
        }

        private void ClearSelection()
        {
            selectedBenchIndex = -1;
            selectedBattleIndex = -1;
            selectedEffectTargetIndex = -1;
        }

        private Transform GetSlotRoot(ShopCardZone zone, int index)
        {
            return slotRoots[BuildSlotKey(zone, index)];
        }

        private void ClearSlot(ShopCardZone zone, int index)
        {
            DestroyChildren(GetSlotRoot(zone, index));
        }

        private static void DestroyChildren(Transform root)
        {
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
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

        private static string BuildSlotKey(ShopCardZone zone, int index)
        {
            return zone + ":" + index;
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

        private static Canvas CreateCanvas()
        {
            var gameObject = new GameObject("ShopTestCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var value = gameObject.GetComponent<Canvas>();
            value.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return value;
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
            string value,
            int fontSize,
            TextAnchor alignment)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = GetUiFont();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.supportRichText = false;
            return text;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            string label,
            UnityEngine.Events.UnityAction action)
        {
            var rect = CreatePanel(name, parent, new Color(0.24f, 0.35f, 0.43f, 1f));
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(action);
            var text = CreateText("Label", rect, label, 17, TextAnchor.MiddleCenter);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            Stretch(text.rectTransform, new Vector2(5f, 3f), new Vector2(-5f, -3f));
            return button;
        }

        private static void PlaceTopButton(Button button, int index, float width = 92f)
        {
            var right = -18f - index * 102f;
            Anchor(button.GetComponent<RectTransform>(),
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(right - width, -25f), new Vector2(right, 25f));
        }

        private static void AddImage(GameObject gameObject, Color color)
        {
            var image = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            image.color = color;
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

        private static void Stretch(
            RectTransform rect,
            Vector2? offsetMin = null,
            Vector2? offsetMax = null)
        {
            Anchor(rect, Vector2.zero, Vector2.one,
                offsetMin ?? Vector2.zero,
                offsetMax ?? Vector2.zero);
        }

        private static Font GetUiFont()
        {
            if (uiFont == null)
            {
                uiFont = Font.CreateDynamicFontFromOSFont(
                    new[] { "Microsoft YaHei", "SimHei", "Arial" },
                    18);
            }

            return uiFont;
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var gameObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(gameObject);
        }
    }
}

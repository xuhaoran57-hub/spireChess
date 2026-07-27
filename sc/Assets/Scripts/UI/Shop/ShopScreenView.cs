using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SpireChess.Audio;
using SpireChess.Shop;
using SpireChess.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.UI.Shop
{
    [DisallowMultipleComponent]
    public sealed class ShopScreenView : MonoBehaviour
    {
        private const int MinionOfferCapacity = 4;
        private const int CollectionCapacity = 5;

        [Header("Root")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private GameObject cardPrefab;

        [Header("Top bar")]
        [SerializeField] private Text roundText;
        [SerializeField] private Text goldText;
        [SerializeField] private Text tavernTierText;
        [SerializeField] private Text upgradeCostText;
        [SerializeField] private Text statusText;

        [Header("Slots")]
        [SerializeField] private ShopSlotView[] minionOfferSlots =
            Array.Empty<ShopSlotView>();
        [SerializeField] private ShopSlotView spellOfferSlot;
        [SerializeField] private ShopSlotView[] battleSlots =
            Array.Empty<ShopSlotView>();
        [SerializeField] private ShopSlotView[] handSlots =
            Array.Empty<ShopSlotView>();

        [Header("Hand paging")]
        [SerializeField] private GameObject pageControls;
        [SerializeField] private Button pageLeftButton;
        [SerializeField] private Text pageText;
        [SerializeField] private Button pageRightButton;

        [Header("Action rail")]
        [SerializeField] private GameObject detailContent;
        [SerializeField] private Text detailTitleText;
        [SerializeField] private Text detailMetaText;
        [SerializeField] private Text detailDescriptionText;
        [SerializeField] private Text detailStatusesText;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Text refreshButtonText;
        [SerializeField] private Button freezeButton;
        [SerializeField] private Text freezeButtonText;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Text upgradeButtonText;
        [SerializeField] private Button sellButton;
        [SerializeField] private Text sellButtonText;
        [SerializeField] private Button endButton;
        [SerializeField] private Text endButtonText;

        [Header("Feedback and modal")]
        [SerializeField] private PresentationFxPool eventFxPool;
        [SerializeField] private GameObject statusToast;
        [SerializeField] private Image statusToastImage;
        [SerializeField] private CanvasGroup statusToastCanvasGroup;
        [SerializeField] private Text statusToastText;
        [SerializeField] private GameObject modalBlocker;
        [SerializeField] private ChoiceOverlayView choiceOverlay;
        [SerializeField] private GameObject rewardOverlay;
        [SerializeField] private Text rewardText;
        [SerializeField] private Button rewardClaimButton;
        [SerializeField] private Button rewardSkipButton;

        private ShopTestController controller;
        private bool isBound;
        private bool interactionBlocked;
        private readonly Dictionary<string, CardFeedbackSnapshot>
            previousCardSnapshots =
                new Dictionary<string, CardFeedbackSnapshot>();
        private readonly Dictionary<string, CardFeedbackSnapshot>
            currentCardSnapshots =
                new Dictionary<string, CardFeedbackSnapshot>();
        private bool hasRenderedState;
        private bool previousFrozen;
        private Coroutine toastRoutine;
        private Coroutine freezePulseRoutine;

        public int RenderedCardCount { get; private set; }
        public int LastRenderFeedbackCount { get; private set; }
        public int PresentationEventCount { get; private set; }
        public ShopEventType? LastPresentationEvent { get; private set; }
        public bool IsChoiceVisible => choiceOverlay != null &&
                                       choiceOverlay.IsVisible;
        public bool ChoiceCanCancel => choiceOverlay != null &&
                                       choiceOverlay.CanCancel;
        public bool IsRewardVisible => rewardOverlay != null &&
                                       rewardOverlay.activeSelf;

        public bool HasCompleteBindings =>
            rootCanvas != null && safeArea != null && cardPrefab != null &&
            roundText != null && goldText != null && tavernTierText != null &&
            upgradeCostText != null && statusText != null &&
            HasSlots(minionOfferSlots, MinionOfferCapacity) &&
            spellOfferSlot != null && spellOfferSlot.HasCompleteBindings &&
            HasSlots(battleSlots, CollectionCapacity) &&
            HasSlots(handSlots, CollectionCapacity) &&
            pageControls != null && pageLeftButton != null &&
            pageText != null && pageRightButton != null &&
            detailContent != null && detailTitleText != null &&
            detailMetaText != null && detailDescriptionText != null &&
            detailStatusesText != null &&
            refreshButton != null && refreshButtonText != null &&
            freezeButton != null && freezeButtonText != null &&
            upgradeButton != null && upgradeButtonText != null &&
            sellButton != null && sellButtonText != null &&
            endButton != null && endButtonText != null &&
            eventFxPool != null &&
            statusToast != null && statusToastImage != null &&
            statusToastCanvasGroup != null && statusToastText != null &&
            modalBlocker != null && choiceOverlay != null &&
            choiceOverlay.HasCompleteBindings && rewardOverlay != null &&
            rewardText != null && rewardClaimButton != null &&
            rewardSkipButton != null;

        private void Awake()
        {
            SetPanelAlpha("TopBar", 0.78f);
            SetPanelAlpha("Content/OfferPanel", 0.62f);
            SetPanelAlpha("Content/BattlePanel", 0.66f);
            SetPanelAlpha("Content/HandPanel", 0.66f);
            SetPanelAlpha("ActionRail", 0.80f);
        }

        public void Bind(ShopTestController value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (isBound)
            {
                if (!ReferenceEquals(controller, value))
                {
                    throw new InvalidOperationException(
                        "ShopScreenView is already bound to another controller.");
                }

                return;
            }

            controller = value;
            refreshButton.onClick.AddListener(() => controller.RefreshShop());
            freezeButton.onClick.AddListener(() => controller.ToggleFreeze());
            upgradeButton.onClick.AddListener(() => controller.UpgradeTavern());
            sellButton.onClick.AddListener(
                () => controller.SellSelectedBattleMinion());
            endButton.onClick.AddListener(
                () => controller.EndShopAndEnterBattle());
            choiceOverlay.Bind(controller);
            rewardClaimButton.onClick.AddListener(
                () => controller.ClaimPendingReward());
            rewardSkipButton.onClick.AddListener(
                () => controller.SkipPendingReward());
            isBound = true;
        }

        private void SetPanelAlpha(string path, float alpha)
        {
            var image = safeArea == null
                ? null
                : safeArea.Find(path)?.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            var color = image.color;
            color.a = alpha;
            image.color = color;
        }

        public void Render(ShopScreenState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (!HasCompleteBindings)
            {
                throw new InvalidOperationException(
                    "ShopScreenView has missing serialized bindings.");
            }

            RenderedCardCount = 0;
            LastRenderFeedbackCount = 0;
            currentCardSnapshots.Clear();
            interactionBlocked = state.IsInteractionBlocked;
            var freezeChanged = hasRenderedState &&
                                previousFrozen != state.IsFrozen;
            ResetFreezePulse();
            roundText.text = $"第 {state.Round} 回合";
            goldText.text = $"金币：{state.Gold}";
            tavernTierText.text = $"酒馆等级：{state.TavernTier}";
            upgradeCostText.text = $"升级费用：{state.UpgradeCost}";
            statusText.text = state.IsInteractionBlocked
                ? state.BlockReason ?? "商店操作暂时不可用"
                : state.IsFrozen
                    ? "商店阶段 · 已冻结"
                    : "商店阶段 · 购买、使用手牌或调整阵容";

            RenderOffers(state);
            RenderCollection(
                battleSlots,
                state.BattleCards,
                ShopCardZone.Battle,
                "空战斗位",
                true);
            RenderHand(state.HandCards);
            RenderButtons(state.Buttons ?? new ShopButtonStates());
            RenderDetail(state.DetailPanel ?? new CardDetailPanelState());
            if (freezeChanged)
            {
                PlayFreezePulse();
            }

            previousFrozen = state.IsFrozen;
            hasRenderedState = true;
            CommitCardSnapshots();
            if (!interactionBlocked)
            {
                choiceOverlay.Render(null);
                RenderReward(null);
            }

            RefreshModalBlocker();
        }

        public void RenderChoice(ChoiceViewModel choice)
        {
            if (!HasCompleteBindings)
            {
                throw new InvalidOperationException(
                    "ShopScreenView has missing serialized bindings.");
            }

            choiceOverlay.Render(choice);
            RefreshModalBlocker();
        }

        public void RenderReward(string message)
        {
            if (!HasCompleteBindings)
            {
                throw new InvalidOperationException(
                    "ShopScreenView has missing serialized bindings.");
            }

            var visible = !string.IsNullOrWhiteSpace(message);
            rewardOverlay.SetActive(visible);
            rewardText.text = visible ? message : string.Empty;
            rewardClaimButton.interactable = visible;
            rewardSkipButton.interactable = visible;
            RefreshModalBlocker();
        }

        public void ShowStatus(string message, bool isError = false)
        {
            if (statusToast == null || statusToastImage == null ||
                statusToastCanvasGroup == null || statusToastText == null)
            {
                return;
            }

            if (toastRoutine != null)
            {
                StopCoroutine(toastRoutine);
                toastRoutine = null;
            }

            var visible = !string.IsNullOrWhiteSpace(message);
            statusToast.SetActive(visible);
            statusToastText.text = visible ? message : string.Empty;
            statusToastImage.color = isError
                ? new Color(0.42f, 0.10f, 0.12f, 0.97f)
                : new Color(0.06f, 0.20f, 0.24f, 0.97f);
            statusToastCanvasGroup.alpha = 1f;
            if (visible && Application.isPlaying)
            {
                toastRoutine = StartCoroutine(HideStatusToast());
            }
        }

        public void PlayPresentationEvent(ShopEventData eventData)
        {
            if (eventData == null || eventFxPool == null)
            {
                return;
            }

            var presentation = GetEventPresentation(eventData);
            eventFxPool.Play(
                presentation.Label,
                presentation.Color,
                presentation.Position,
                presentation.Emphasis,
                presentation.Duration,
                presentation.VerticalTravel);
            PresentationEventCount++;
            LastPresentationEvent = eventData.Type;
            if (TryGetAudioCue(eventData.Type, out var cueId))
            {
                AudioService.Instance?.PlayCue(cueId);
            }
        }

        public void ClearPresentationFeedback()
        {
            eventFxPool?.ClearImmediate();
        }

        private void RenderOffers(ShopScreenState state)
        {
            var offers = state.MinionOffers ?? Array.Empty<CardViewModel>();
            for (var index = 0; index < minionOfferSlots.Length; index++)
            {
                var slot = minionOfferSlots[index];
                var unlocked = index < offers.Length;
                slot.gameObject.SetActive(unlocked);
                if (!unlocked)
                {
                    slot.ClearContent();
                    continue;
                }

                RenderSlot(
                    slot,
                    offers[index],
                    ShopCardZone.MinionOffer,
                    index,
                    "暂无随从",
                    false);
            }

            spellOfferSlot.gameObject.SetActive(true);
            RenderSlot(
                spellOfferSlot,
                state.SpellOffer,
                ShopCardZone.SpellOffer,
                0,
                "暂无法术",
                false);
        }

        private void RenderCollection(
            ShopSlotView[] slots,
            CardViewModel[] cards,
            ShopCardZone zone,
            string emptyMessage,
            bool draggable)
        {
            cards = cards ?? Array.Empty<CardViewModel>();
            for (var index = 0; index < slots.Length; index++)
            {
                slots[index].gameObject.SetActive(true);
                var card = index < cards.Length ? cards[index] : null;
                RenderSlot(
                    slots[index],
                    card,
                    zone,
                    index,
                    emptyMessage,
                    draggable);
            }
        }

        private void RenderHand(HandCardsState hand)
        {
            hand = hand ?? new HandCardsState();
            var visibleSlots = hand.VisibleSlots ??
                               Array.Empty<HandCardSlotState>();
            for (var index = 0; index < handSlots.Length; index++)
            {
                var slotState = index < visibleSlots.Length
                    ? visibleSlots[index]
                    : null;
                var domainIndex = slotState == null
                    ? hand.PageIndex * Math.Max(1, hand.PageSize) + index
                    : slotState.SlotIndex;
                RenderSlot(
                    handSlots[index],
                    slotState == null ? null : slotState.Card,
                    ShopCardZone.Bench,
                    domainIndex,
                    "空手牌位",
                    true);
            }

            var showPaging = hand.PageCount > 1;
            pageControls.SetActive(showPaging);
            pageLeftButton.interactable = hand.CanPageLeft;
            pageRightButton.interactable = hand.CanPageRight;
            pageText.text = $"{hand.PageIndex + 1} / {Math.Max(1, hand.PageCount)}";
        }

        private void RenderSlot(
            ShopSlotView slot,
            CardViewModel model,
            ShopCardZone zone,
            int index,
            string emptyMessage,
            bool draggable)
        {
            slot.Initialize(controller, zone, index);
            slot.PrepareForRender(emptyMessage, model != null && model.IsSelected);
            if (model == null)
            {
                return;
            }

            var cardObject = Instantiate(cardPrefab, slot.Content, false);
            var cardRect = cardObject.GetComponent<RectTransform>();
            PlaceAtTopLeft(cardRect);
            var cardView = cardObject.GetComponent<CardView>();
            if (cardView == null)
            {
                throw new InvalidOperationException(
                    "The shop card prefab has no CardView component.");
            }

            if (controller != null)
            {
                var shopCardView = cardObject.GetComponent<ShopCardView>() ??
                                   cardObject.AddComponent<ShopCardView>();
                shopCardView.Initialize(
                    controller,
                    rootCanvas,
                    zone,
                    index,
                    draggable);
            }

            // Bind input and replace the empty state before text fitting. A
            // single-card presentation failure must not leave a visible card
            // without its click/drag route.
            slot.ShowCard();
            cardView.Render(model);
            ApplyCardFeedback(cardView, model);
            RenderedCardCount++;
        }

        private void RenderButtons(ShopButtonStates buttons)
        {
            ApplyButton(refreshButton, refreshButtonText, buttons.Refresh);
            ApplyButton(freezeButton, freezeButtonText, buttons.Freeze);
            ApplyButton(upgradeButton, upgradeButtonText, buttons.Upgrade);
            ApplyButton(sellButton, sellButtonText, buttons.Sell);
            ApplyButton(endButton, endButtonText, buttons.EndShop);
        }

        private static void ApplyButton(
            Button button,
            Text label,
            ShopActionButtonState state)
        {
            state = state ?? new ShopActionButtonState();
            button.gameObject.SetActive(state.IsVisible);
            button.interactable = state.IsInteractable;
            label.text = state.Text ?? string.Empty;
            var image = button.targetGraphic as Image;
            if (image != null)
            {
                image.color = state.IsActive
                    ? new Color(0.16f, 0.48f, 0.66f, 1f)
                    : new Color(0.16f, 0.19f, 0.25f, 1f);
            }
        }

        private void RenderDetail(CardDetailPanelState detail)
        {
            detailContent.SetActive(detail.IsVisible);
            if (!detail.IsVisible)
            {
                detailTitleText.text = "未选择卡牌";
                detailMetaText.text = string.Empty;
                detailDescriptionText.text = string.Empty;
                detailStatusesText.text = string.Empty;
                return;
            }

            var card = detail.Card;
            detailTitleText.text = card.Name ?? string.Empty;
            detailMetaText.text = $"{ToLocationText(detail.Location)} · " +
                                  $"{card.RaceText ?? string.Empty} · " +
                                  $"{card.Tier} 级";
            detailDescriptionText.text = card.Description ?? string.Empty;
            detailStatusesText.text = string.Join(
                "\n",
                (detail.Statuses ?? Array.Empty<CardDetailStatusState>())
                .Where(value => value != null)
                .Select(value => string.IsNullOrWhiteSpace(value.Description)
                    ? value.Label ?? string.Empty
                    : $"{value.Label}：{value.Description}"));
        }

        private static string ToLocationText(ShopCardLocation location)
        {
            switch (location)
            {
                case ShopCardLocation.MinionOffer: return "随从商品";
                case ShopCardLocation.SpellOffer: return "法术商品";
                case ShopCardLocation.Battle: return "战斗区";
                case ShopCardLocation.Hand: return "手牌";
                default: return "卡牌";
            }
        }

        private static bool HasSlots(ShopSlotView[] slots, int capacity)
        {
            return slots != null && slots.Length == capacity &&
                   Array.TrueForAll(
                       slots,
                       value => value != null && value.HasCompleteBindings);
        }

        private static EventPresentation GetEventPresentation(
            ShopEventData eventData)
        {
            switch (eventData.Type)
            {
                case ShopEventType.OnShopPhaseStart:
                    return EventPresentation.Strong(
                        "商店开启",
                        new Color(0.22f, 0.72f, 0.68f, 1f),
                        new Vector2(-120f, 280f));
                case ShopEventType.OnShopPhaseEnd:
                    return EventPresentation.Strong(
                        "阵容锁定",
                        new Color(0.82f, 0.68f, 0.36f, 1f),
                        new Vector2(-120f, 20f));
                case ShopEventType.OnRefresh:
                    return EventPresentation.Normal(
                        eventData.FreeRefreshes > 0 ? "免费刷新" : "刷新",
                        new Color(0.26f, 0.68f, 0.86f, 1f),
                        new Vector2(-310f, 250f));
                case ShopEventType.OnBuy:
                    return EventPresentation.Normal(
                        eventData.Cost > 0
                            ? $"购买  -{eventData.Cost} 金币"
                            : "购买",
                        new Color(0.90f, 0.72f, 0.34f, 1f),
                        new Vector2(-330f, 130f));
                case ShopEventType.OnSell:
                    return EventPresentation.Normal(
                        eventData.Gold > 0
                            ? $"出售  +{eventData.Gold} 金币"
                            : "出售",
                        new Color(0.88f, 0.64f, 0.30f, 1f),
                        new Vector2(590f, -30f));
                case ShopEventType.OnPlay:
                    return EventPresentation.Normal(
                        "随从上场",
                        new Color(0.34f, 0.78f, 0.50f, 1f),
                        new Vector2(-190f, -40f));
                case ShopEventType.OnSpellUsed:
                    return EventPresentation.Strong(
                        "法术生效",
                        new Color(0.48f, 0.60f, 0.98f, 1f),
                        new Vector2(-180f, -110f));
                case ShopEventType.OnTripleFormed:
                    return EventPresentation.Critical(
                        "三连合成",
                        new Color(0.98f, 0.73f, 0.20f, 1f),
                        new Vector2(-100f, 80f));
                case ShopEventType.OnTripleRewardGranted:
                    return EventPresentation.Strong(
                        "获得发现奖励",
                        new Color(0.92f, 0.78f, 0.36f, 1f),
                        new Vector2(0f, 110f));
                case ShopEventType.OnDiscoverStarted:
                    return EventPresentation.Strong(
                        "发现开启",
                        new Color(0.54f, 0.44f, 0.92f, 1f),
                        new Vector2(0f, 130f));
                case ShopEventType.OnDiscoverResolved:
                    return EventPresentation.Strong(
                        "选择完成",
                        new Color(0.38f, 0.78f, 0.66f, 1f),
                        new Vector2(0f, 90f));
                case ShopEventType.OnDiscoverCancelled:
                    return EventPresentation.Normal(
                        "取消发现",
                        new Color(0.54f, 0.58f, 0.66f, 1f),
                        new Vector2(0f, 90f));
                case ShopEventType.OnTavernUpgraded:
                    return EventPresentation.Critical(
                        eventData.TavernTier > 0
                            ? $"酒馆升至 {eventData.TavernTier} 级"
                            : "酒馆升级",
                        new Color(0.94f, 0.62f, 0.24f, 1f),
                        new Vector2(590f, 170f));
                default:
                    return EventPresentation.Normal(
                        eventData.Type.ToString(),
                        new Color(0.40f, 0.66f, 0.72f, 1f),
                        Vector2.zero);
            }
        }

        public static bool TryGetAudioCue(
            ShopEventType eventType,
            out string cueId)
        {
            switch (eventType)
            {
                case ShopEventType.OnRefresh:
                    cueId = PresentationAudioCueIds.ShopRefresh;
                    return true;
                case ShopEventType.OnBuy:
                    cueId = PresentationAudioCueIds.ShopBuy;
                    return true;
                case ShopEventType.OnSell:
                    cueId = PresentationAudioCueIds.ShopSell;
                    return true;
                case ShopEventType.OnPlay:
                    cueId = PresentationAudioCueIds.ShopPlay;
                    return true;
                case ShopEventType.OnSpellUsed:
                    cueId = PresentationAudioCueIds.ShopSpell;
                    return true;
                case ShopEventType.OnTripleFormed:
                    cueId = PresentationAudioCueIds.ShopTriple;
                    return true;
                case ShopEventType.OnDiscoverStarted:
                    cueId = PresentationAudioCueIds.ShopDiscoverOpen;
                    return true;
                case ShopEventType.OnDiscoverResolved:
                    cueId = PresentationAudioCueIds.ShopDiscoverPick;
                    return true;
                case ShopEventType.OnTavernUpgraded:
                    cueId = PresentationAudioCueIds.ShopUpgrade;
                    return true;
                default:
                    cueId = null;
                    return false;
            }
        }

        private void ApplyCardFeedback(CardView view, CardViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.InstanceId))
            {
                return;
            }

            var snapshot = new CardFeedbackSnapshot(
                model.Attack,
                model.Health,
                model.HasShield,
                model.HasNextCombatShield);
            if (previousCardSnapshots.TryGetValue(
                    model.InstanceId,
                    out var previous))
            {
                var attackDelta = snapshot.Attack - previous.Attack;
                var healthDelta = snapshot.Health - previous.Health;
                if (attackDelta != 0 || healthDelta != 0)
                {
                    view.PlayStatChange(attackDelta, healthDelta);
                    LastRenderFeedbackCount++;
                }

                if (!previous.HasShield && snapshot.HasShield)
                {
                    view.PlayShieldGain(false);
                    LastRenderFeedbackCount++;
                }

                if (!previous.HasNextCombatShield &&
                    snapshot.HasNextCombatShield)
                {
                    view.PlayShieldGain(true);
                    LastRenderFeedbackCount++;
                }
            }

            currentCardSnapshots[model.InstanceId] = snapshot;
        }

        private void CommitCardSnapshots()
        {
            previousCardSnapshots.Clear();
            foreach (var pair in currentCardSnapshots)
            {
                previousCardSnapshots[pair.Key] = pair.Value;
            }
        }

        private void ResetFreezePulse()
        {
            if (freezePulseRoutine != null)
            {
                StopCoroutine(freezePulseRoutine);
                freezePulseRoutine = null;
            }

            freezeButton.transform.localScale = Vector3.one;
        }

        private void PlayFreezePulse()
        {
            freezeButton.transform.localScale = Vector3.one * 1.08f;
            if (Application.isPlaying)
            {
                freezePulseRoutine = StartCoroutine(RestoreFreezeButtonScale());
            }
        }

        private IEnumerator RestoreFreezeButtonScale()
        {
            var elapsed = 0f;
            const float duration = 0.3f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                freezeButton.transform.localScale = Vector3.Lerp(
                    Vector3.one * 1.08f,
                    Vector3.one,
                    Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            freezeButton.transform.localScale = Vector3.one;
            freezePulseRoutine = null;
        }

        private IEnumerator HideStatusToast()
        {
            yield return new WaitForSecondsRealtime(1.25f);
            var elapsed = 0f;
            const float duration = 0.35f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                statusToastCanvasGroup.alpha =
                    1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            statusToast.SetActive(false);
            statusToastCanvasGroup.alpha = 1f;
            toastRoutine = null;
        }

        private void RefreshModalBlocker()
        {
            modalBlocker.SetActive(
                interactionBlocked || IsChoiceVisible || IsRewardVisible);
        }

        private static void PlaceAtTopLeft(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private readonly struct CardFeedbackSnapshot
        {
            public CardFeedbackSnapshot(
                int attack,
                int health,
                bool hasShield,
                bool hasNextCombatShield)
            {
                Attack = attack;
                Health = health;
                HasShield = hasShield;
                HasNextCombatShield = hasNextCombatShield;
            }

            public int Attack { get; }
            public int Health { get; }
            public bool HasShield { get; }
            public bool HasNextCombatShield { get; }
        }

        private readonly struct EventPresentation
        {
            private EventPresentation(
                string label,
                Color color,
                Vector2 position,
                PresentationFxEmphasis emphasis,
                float duration,
                float verticalTravel)
            {
                Label = label;
                Color = color;
                Position = position;
                Emphasis = emphasis;
                Duration = duration;
                VerticalTravel = verticalTravel;
            }

            public string Label { get; }
            public Color Color { get; }
            public Vector2 Position { get; }
            public PresentationFxEmphasis Emphasis { get; }
            public float Duration { get; }
            public float VerticalTravel { get; }

            public static EventPresentation Normal(
                string label,
                Color color,
                Vector2 position)
            {
                return new EventPresentation(
                    label,
                    color,
                    position,
                    PresentationFxEmphasis.Normal,
                    0.62f,
                    72f);
            }

            public static EventPresentation Strong(
                string label,
                Color color,
                Vector2 position)
            {
                return new EventPresentation(
                    label,
                    color,
                    position,
                    PresentationFxEmphasis.Strong,
                    0.78f,
                    86f);
            }

            public static EventPresentation Critical(
                string label,
                Color color,
                Vector2 position)
            {
                return new EventPresentation(
                    label,
                    color,
                    position,
                    PresentationFxEmphasis.Critical,
                    1.02f,
                    104f);
            }
        }
    }
}

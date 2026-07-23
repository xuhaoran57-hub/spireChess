using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SpireChess.Battle;
using SpireChess.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class BattleScreenView : MonoBehaviour
    {
        private static readonly Color AttackerColor =
            new Color(1f, 0.78f, 0.18f, 1f);
        private static readonly Color TargetColor =
            new Color(1f, 0.28f, 0.24f, 1f);
        private static readonly Color FeedbackColor =
            new Color(1f, 0.84f, 0.48f, 1f);

        [Header("Root")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private GameObject standeePrefab;

        [Header("Top bar")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text roundText;
        [SerializeField] private Button startButton;
        [SerializeField] private Text startButtonText;
        [SerializeField] private Button speedButton;
        [SerializeField] private Text speedButtonText;
        [SerializeField] private Button skipButton;
        [SerializeField] private Text skipButtonText;
        [SerializeField] private Button presetButton;
        [SerializeField] private Text presetButtonText;
        [SerializeField] private Button resetButton;
        [SerializeField] private Text resetButtonText;
        [SerializeField] private Button returnButton;
        [SerializeField] private Text returnButtonText;

        [Header("Board")]
        [SerializeField] private BattleSlotView[] enemySlots =
            Array.Empty<BattleSlotView>();
        [SerializeField] private BattleSlotView[] playerSlots =
            Array.Empty<BattleSlotView>();

        [Header("Log")]
        [SerializeField] private ScrollRect logScrollRect;
        [SerializeField] private Text logText;

        [Header("Feedback")]
        [SerializeField] private CanvasGroup feedbackCanvasGroup;
        [SerializeField] private Text feedbackText;

        [Header("Standee detail")]
        [SerializeField] private RectTransform detailLayer;
        [SerializeField] private CardView detailCard;
        [SerializeField] private CanvasGroup detailCanvasGroup;
        [SerializeField] private Text detailModeText;

        private readonly Dictionary<string, BattleStandeeView> standeesById =
            new Dictionary<string, BattleStandeeView>(StringComparer.Ordinal);
        private BattleTestController controller;
        private BattleStandeeView detailOwner;
        private bool detailLocked;
        private bool isBound;

        public int RenderedCardCount { get; private set; }
        public bool IsAnimationPlaying { get; private set; }
        public bool IsLogScrollable => logScrollRect != null &&
                                       logScrollRect.vertical;
        public string LogContents => logText == null ? string.Empty : logText.text;
        public bool IsStandeeDetailVisible => detailCanvasGroup != null &&
                                              detailCanvasGroup.alpha > 0f;
        public bool IsStandeeDetailLocked => detailLocked;
        public string DetailInstanceId => detailOwner == null
            ? string.Empty
            : detailOwner.InstanceId;
        public bool HasCompleteBindings =>
            rootCanvas != null && safeArea != null && standeePrefab != null &&
            titleText != null && statusText != null && roundText != null &&
            startButton != null && startButtonText != null &&
            speedButton != null && speedButtonText != null &&
            skipButton != null && skipButtonText != null &&
            presetButton != null && presetButtonText != null &&
            resetButton != null && resetButtonText != null &&
            returnButton != null && returnButtonText != null &&
            HasSlots(enemySlots) && HasSlots(playerSlots) &&
            logScrollRect != null && logText != null &&
            feedbackCanvasGroup != null && feedbackText != null &&
            detailLayer != null && detailCard != null &&
            detailCard.HasCompleteBindings && detailCanvasGroup != null &&
            detailModeText != null;

        public void Bind(BattleTestController value)
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
                        "BattleScreenView is already bound to another controller.");
                }
                return;
            }

            controller = value;
            startButton.onClick.AddListener(controller.StartBattle);
            speedButton.onClick.AddListener(controller.TogglePlaybackSpeed);
            skipButton.onClick.AddListener(controller.SkipPlayback);
            presetButton.onClick.AddListener(controller.NextPreset);
            resetButton.onClick.AddListener(controller.ResetBattle);
            returnButton.onClick.AddListener(controller.ReturnToFlow);
            isBound = true;
        }

        public void Render(BattleScreenState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }
            if (!HasCompleteBindings)
            {
                throw new InvalidOperationException(
                    "BattleScreenView has missing serialized bindings.");
            }

            titleText.text = state.Title ?? string.Empty;
            statusText.text = state.Status ?? string.Empty;
            roundText.text = state.RoundText ?? string.Empty;
            SetButton(startButton, startButtonText, state.Start);
            SetButton(speedButton, speedButtonText, state.Speed);
            SetButton(skipButton, skipButtonText, state.Skip);
            SetButton(presetButton, presetButtonText, state.Preset);
            SetButton(resetButton, resetButtonText, state.Reset);
            SetButton(returnButton, returnButtonText, state.Return);

            var desiredIds = new HashSet<string>(StringComparer.Ordinal);
            SyncRow(enemySlots, state.EnemyCards, BattleSide.Enemy, desiredIds);
            SyncRow(playerSlots, state.PlayerCards, BattleSide.Player, desiredIds);
            RemoveStaleStandees(desiredIds);
            RenderedCardCount = desiredIds.Count;

            if (detailOwner != null &&
                desiredIds.Contains(detailOwner.InstanceId))
            {
                RenderStandeeDetail(detailOwner, detailOwner.Model);
            }
            else if (detailOwner != null)
            {
                CloseStandeeDetail();
            }

            logText.text = state.LogText ?? string.Empty;
            Canvas.ForceUpdateCanvases();
            logScrollRect.verticalNormalizedPosition = 0f;
        }

        public IEnumerator PlayEvent(
            BattlePlaybackEvent playbackEvent,
            float playbackSpeed)
        {
            if (playbackEvent == null)
            {
                yield break;
            }

            var durationScale = 1f / Mathf.Max(1f, playbackSpeed);
            IsAnimationPlaying = true;
            CloseStandeeDetail();
            ClearHighlights();
            switch (playbackEvent.Kind)
            {
                case BattlePlaybackEventKind.AttackStarted:
                    yield return PlayAttack(playbackEvent, durationScale);
                    break;
                case BattlePlaybackEventKind.DamageApplied:
                    yield return PlayDamage(playbackEvent, durationScale);
                    break;
                case BattlePlaybackEventKind.ShieldGained:
                    PlayShield(playbackEvent, true);
                    yield return Wait(0.16f * durationScale);
                    break;
                case BattlePlaybackEventKind.ShieldLost:
                    PlayShield(playbackEvent, false);
                    yield return Wait(0.16f * durationScale);
                    break;
                case BattlePlaybackEventKind.StatsChanged:
                    PlayStats(playbackEvent);
                    yield return Wait(0.18f * durationScale);
                    break;
                case BattlePlaybackEventKind.UnitDied:
                    yield return PlayDeath(playbackEvent, durationScale);
                    break;
                case BattlePlaybackEventKind.UnitSummoned:
                    yield return PlaySummon(playbackEvent, durationScale);
                    break;
                default:
                    ShowFeedback(playbackEvent.Message);
                    yield return Wait(0.10f * durationScale);
                    break;
            }

            ClearHighlights();
            IsAnimationPlaying = false;
        }

        private void SyncRow(
            IReadOnlyList<BattleSlotView> slots,
            IReadOnlyList<CardViewModel> models,
            BattleSide side,
            ISet<string> desiredIds)
        {
            for (var index = 0; index < BattleBoardState.SlotCount; index++)
            {
                var slot = slots[index];
                slot.Initialize(controller, side, index);
                var model = models != null && index < models.Count
                    ? models[index]
                    : null;
                slot.PrepareForRender(model != null);
                if (model == null)
                {
                    continue;
                }

                desiredIds.Add(model.InstanceId);
                if (!standeesById.TryGetValue(
                        model.InstanceId,
                        out var standee) ||
                    standee == null)
                {
                    var instance = Instantiate(standeePrefab, slot.Content);
                    instance.name = "BattleStandee";
                    standee = instance.GetComponent<BattleStandeeView>();
                    if (standee == null)
                    {
                        throw new InvalidOperationException(
                            "PF_BattleStandee is missing BattleStandeeView.");
                    }
                    standeesById[model.InstanceId] = standee;
                }

                standee.gameObject.SetActive(true);
                standee.transform.SetParent(slot.Content, false);
                var rect = standee.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;
                standee.Initialize(
                    controller,
                    this,
                    rootCanvas,
                    side,
                    index,
                    side == BattleSide.Player);
                standee.Render(model);
            }
        }

        private void RemoveStaleStandees(ISet<string> desiredIds)
        {
            foreach (var pair in standeesById
                         .Where(pair => !desiredIds.Contains(pair.Key))
                         .ToArray())
            {
                if (pair.Value != null)
                {
                    pair.Value.gameObject.SetActive(false);
                    if (Application.isPlaying)
                    {
                        pair.Value.transform.SetParent(null, false);
                        Destroy(pair.Value.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(pair.Value.gameObject);
                    }
                }
                standeesById.Remove(pair.Key);
            }
        }

        private IEnumerator PlayAttack(
            BattlePlaybackEvent playbackEvent,
            float scale)
        {
            var attacker = FindCard(playbackEvent.SourceInstanceId);
            var target = FindCard(playbackEvent.TargetInstanceId);
            SetSlotHighlight(
                playbackEvent.SourceSide,
                playbackEvent.SourceIndex,
                AttackerColor);
            SetSlotHighlight(
                playbackEvent.TargetSide,
                playbackEvent.TargetIndex,
                TargetColor);
            if (attacker == null || target == null)
            {
                yield return Wait(0.20f * scale);
                yield break;
            }

            var rect = attacker.RectTransform;
            var start = rect.anchoredPosition;
            var worldDirection = (target.RectTransform.position - rect.position).normalized;
            var localDirection = rect.parent.InverseTransformVector(worldDirection);
            var direction = new Vector2(localDirection.x, localDirection.y).normalized;
            var destination = start + direction * 46f;
            yield return Animate(0.12f * scale, value =>
                rect.anchoredPosition = Vector2.Lerp(start, destination, Smooth(value)));
            yield return Animate(0.12f * scale, value =>
                rect.anchoredPosition = Vector2.Lerp(destination, start, Smooth(value)));
            rect.anchoredPosition = start;
        }

        private IEnumerator PlayDamage(
            BattlePlaybackEvent playbackEvent,
            float scale)
        {
            var target = FindCard(playbackEvent.TargetInstanceId);
            ShowFeedback(playbackEvent.WasBlocked
                ? "格挡"
                : $"-{playbackEvent.Amount}");
            if (target == null)
            {
                yield return Wait(0.14f * scale);
                yield break;
            }

            target.PlayStatChange(0, playbackEvent.HealthDelta);
            var rect = target.RectTransform;
            var start = rect.anchoredPosition;
            yield return Animate(0.16f * scale, value =>
            {
                var shake = Mathf.Sin(value * Mathf.PI * 6f) *
                            (1f - value) * 8f;
                rect.anchoredPosition = start + Vector2.right * shake;
            });
            rect.anchoredPosition = start;
        }

        private void PlayShield(BattlePlaybackEvent playbackEvent, bool gained)
        {
            var target = FindCard(playbackEvent.TargetInstanceId);
            ShowFeedback(gained ? "获得护盾" : "护盾破裂");
            if (gained)
            {
                target?.SetShieldVisible(true);
            }
            else
            {
                target?.SetShieldVisible(false);
            }
            SetSlotHighlight(
                playbackEvent.TargetSide,
                playbackEvent.TargetIndex,
                gained ? new Color(0.25f, 0.65f, 1f, 1f) : TargetColor);
        }

        private void PlayStats(BattlePlaybackEvent playbackEvent)
        {
            var target = FindCard(playbackEvent.TargetInstanceId);
            target?.PlayStatChange(
                playbackEvent.AttackDelta,
                playbackEvent.HealthDelta);
            ShowFeedback(playbackEvent.Message);
        }

        private IEnumerator PlayDeath(
            BattlePlaybackEvent playbackEvent,
            float scale)
        {
            var target = FindCard(playbackEvent.TargetInstanceId);
            ShowFeedback("死亡");
            if (target == null)
            {
                yield return Wait(0.14f * scale);
                yield break;
            }

            var canvasGroup = target.GetComponent<CanvasGroup>();
            var startScale = target.transform.localScale;
            yield return Animate(0.20f * scale, value =>
            {
                canvasGroup.alpha = 1f - value;
                target.transform.localScale = Vector3.Lerp(
                    startScale,
                    startScale * 0.78f,
                    value);
            });
            canvasGroup.alpha = 1f;
            target.transform.localScale = startScale;
        }

        private IEnumerator PlaySummon(
            BattlePlaybackEvent playbackEvent,
            float scale)
        {
            var target = FindCard(playbackEvent.TargetInstanceId);
            ShowFeedback("召唤");
            if (target == null)
            {
                yield return Wait(0.14f * scale);
                yield break;
            }

            var endScale = target.transform.localScale;
            yield return Animate(0.20f * scale, value =>
                target.transform.localScale = Vector3.Lerp(
                    endScale * 0.72f,
                    endScale,
                    Smooth(value)));
            target.transform.localScale = endScale;
        }

        private BattleStandeeView FindCard(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return null;
            }
            standeesById.TryGetValue(instanceId, out var standee);
            return standee;
        }

        private void SetSlotHighlight(
            BattleSide? side,
            int index,
            Color color)
        {
            if (!side.HasValue || index < 0 ||
                index >= BattleBoardState.SlotCount)
            {
                return;
            }
            var slots = side.Value == BattleSide.Player
                ? playerSlots
                : enemySlots;
            slots[index].SetHighlight(color, new Vector2(4f, -4f));
        }

        private void ClearHighlights()
        {
            foreach (var slot in enemySlots.Concat(playerSlots))
            {
                slot.SetHighlight(Color.clear, Vector2.zero);
            }
        }

        private void ShowFeedback(string message)
        {
            feedbackText.text = message ?? string.Empty;
            feedbackText.color = FeedbackColor;
            feedbackCanvasGroup.alpha =
                string.IsNullOrWhiteSpace(message) ? 0f : 1f;
        }

        public void ShowStandeeDetail(
            BattleStandeeView standee,
            CardViewModel model)
        {
            if (standee == null || model == null ||
                (detailLocked && detailOwner != standee))
            {
                return;
            }

            detailOwner = standee;
            RenderStandeeDetail(standee, model);
        }

        public void HideStandeeDetail(BattleStandeeView standee)
        {
            if (detailLocked || detailOwner != standee)
            {
                return;
            }

            CloseStandeeDetail();
        }

        public void ToggleStandeeDetailLock(
            BattleStandeeView standee,
            CardViewModel model)
        {
            if (standee == null || model == null)
            {
                return;
            }

            if (detailLocked && detailOwner == standee)
            {
                CloseStandeeDetail();
                return;
            }

            detailLocked = true;
            detailOwner = standee;
            RenderStandeeDetail(standee, model);
        }

        public void CloseStandeeDetail()
        {
            detailLocked = false;
            detailOwner = null;
            if (detailCanvasGroup != null)
            {
                detailCanvasGroup.alpha = 0f;
                detailCanvasGroup.blocksRaycasts = false;
                detailCanvasGroup.interactable = false;
            }
            if (detailModeText != null)
            {
                detailModeText.text = string.Empty;
            }
        }

        private void RenderStandeeDetail(
            BattleStandeeView standee,
            CardViewModel model)
        {
            if (detailCard == null || detailLayer == null ||
                standee == null || model == null)
            {
                return;
            }

            detailCard.Render(CloneForDetail(model));
            var detailRect = detailCard.GetComponent<RectTransform>();
            detailRect.anchorMin = Vector2.zero;
            detailRect.anchorMax = Vector2.zero;
            detailRect.pivot = new Vector2(0.5f, 0.5f);
            detailRect.sizeDelta = new Vector2(240f, 360f);
            detailRect.localScale = Vector3.one;

            var screenPoint = RectTransformUtility.WorldToScreenPoint(
                rootCanvas.worldCamera,
                standee.RectTransform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                detailLayer,
                screenPoint,
                rootCanvas.worldCamera,
                out var localPoint);
            var verticalOffset = standee.Side == BattleSide.Player
                ? 260f
                : -260f;
            var target = localPoint + new Vector2(0f, verticalOffset);
            var detailBounds = detailLayer.rect;
            target.x = Mathf.Clamp(
                target.x,
                detailBounds.xMin + 120f,
                detailBounds.xMax - 120f);
            target.y = Mathf.Clamp(
                target.y,
                detailBounds.yMin + 180f,
                detailBounds.yMax - 180f);
            detailRect.anchoredPosition = target;
            detailRect.SetAsLastSibling();

            detailCanvasGroup.alpha = 1f;
            detailCanvasGroup.blocksRaycasts = false;
            detailCanvasGroup.interactable = false;
            detailModeText.text = detailLocked
                ? "已锁定 · 再次点击立牌关闭"
                : "悬停详情 · 点击立牌锁定";
            var modeRect = detailModeText.rectTransform;
            modeRect.anchorMin = Vector2.zero;
            modeRect.anchorMax = Vector2.zero;
            modeRect.pivot = new Vector2(0.5f, 0f);
            modeRect.anchoredPosition = target + new Vector2(0f, 186f);
            modeRect.SetAsLastSibling();
        }

        private static CardViewModel CloneForDetail(CardViewModel source)
        {
            return new CardViewModel
            {
                InstanceId = source.InstanceId,
                ArtId = source.ArtId,
                Name = source.Name,
                Description = source.Description,
                RaceText = source.RaceText,
                AbilityLabels = source.AbilityLabels ?? Array.Empty<string>(),
                ProgressText = source.ProgressText,
                DisabledReason = source.DisabledReason,
                Tier = source.Tier,
                Attack = source.Attack,
                Health = source.Health,
                BaseAttack = source.BaseAttack,
                BaseHealth = source.BaseHealth,
                Cost = source.Cost,
                DisplayMode = CardDisplayMode.Full,
                IsMinion = source.IsMinion,
                ShowCost = false,
                IsGolden = source.IsGolden,
                IsSelected = source.IsSelected,
                IsLegalTarget = source.IsLegalTarget,
                IsInteractable = source.IsInteractable,
                IsAffordable = source.IsAffordable,
                HasShield = source.HasShield,
                HasNextCombatShield = source.HasNextCombatShield,
                IsTemporary = source.IsTemporary,
                Keywords = source.Keywords ?? Array.Empty<string>()
            };
        }

        private static void SetButton(
            Button button,
            Text label,
            BattleButtonState state)
        {
            state = state ?? new BattleButtonState();
            button.gameObject.SetActive(state.IsVisible);
            button.interactable = state.IsInteractable;
            label.text = state.Label ?? string.Empty;
        }

        private static bool HasSlots(IReadOnlyList<BattleSlotView> slots)
        {
            return slots != null &&
                   slots.Count == BattleBoardState.SlotCount &&
                   slots.All(slot => slot != null && slot.HasCompleteBindings);
        }

        private static IEnumerator Animate(float duration, Action<float> update)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                update(Mathf.Clamp01(elapsed / Mathf.Max(0.001f, duration)));
                yield return null;
            }
            update(1f);
        }

        private static IEnumerator Wait(float duration)
        {
            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
            }
        }

        private static float Smooth(float value)
        {
            return value * value * (3f - 2f * value);
        }
    }
}

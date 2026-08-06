using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SpireChess.Audio;
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
        private static readonly Color ShieldColor =
            new Color(0.28f, 0.72f, 1f, 1f);
        private static readonly Color GrowthColor =
            new Color(0.46f, 0.92f, 0.54f, 1f);
        private static readonly Color DeathColor =
            new Color(0.52f, 0.46f, 0.66f, 1f);

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
        [SerializeField] private Sprite backdropOverride;
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
        [SerializeField] private PresentationFxPool feedbackFxPool;
        [SerializeField] private BattleImpactFxLayer impactFxLayer;
        [SerializeField] private RectTransform boardMotionRoot;
        [SerializeField] private CanvasGroup boardPulseCanvasGroup;
        [SerializeField] private Image boardPulseImage;

        [Header("Result")]
        [SerializeField] private GameObject resultLayer;
        [SerializeField] private CanvasGroup resultCanvasGroup;
        [SerializeField] private Text resultTitleText;
        [SerializeField] private Text resultBodyText;

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
        private int presentationEpoch;
        private Image productionBackdrop;
        private Vector2 boardMotionOrigin;
        private bool hasBoardMotionOrigin;

        public int RenderedCardCount { get; private set; }
        public bool IsAnimationPlaying { get; private set; }
        public int ActiveFeedbackFxCount =>
            feedbackFxPool == null ? 0 : feedbackFxPool.ActiveCount;
        public int ActiveImpactFxCount =>
            impactFxLayer == null ? 0 : impactFxLayer.ActiveCount;
        public string LastFeedbackId { get; private set; } = string.Empty;
        public string LastAudioCueId { get; private set; } = string.Empty;
        public bool IsResultVisible => resultLayer != null &&
                                       resultLayer.activeSelf &&
                                       resultCanvasGroup != null &&
                                       resultCanvasGroup.alpha > 0f;
        public string ResultTitle => resultTitleText == null
            ? string.Empty
            : resultTitleText.text;
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
            feedbackFxPool != null && impactFxLayer != null &&
            boardMotionRoot != null && boardPulseCanvasGroup != null &&
            boardPulseImage != null && resultLayer != null &&
            resultCanvasGroup != null && resultTitleText != null &&
            resultBodyText != null &&
            detailLayer != null && detailCard != null &&
            detailCard.HasCompleteBindings && detailCanvasGroup != null &&
            detailModeText != null;

        private void Awake()
        {
            EnsurePresentationFxBindings();
            CacheBoardMotionOrigin();
            var board = safeArea == null
                ? null
                : safeArea.Find("Board");
            if (board == null)
            {
                return;
            }

            productionBackdrop = PresentationArtworkResources.EnsureImage(
                board,
                "ProductionArtwork",
                backdropOverride != null
                    ? backdropOverride
                    : PresentationArtworkResources.LoadBackdrop(
                        PresentationBackdropVariant.Battle),
                new Color(0.72f, 0.72f, 0.68f, 0.86f),
                true);
            if (productionBackdrop == null ||
                productionBackdrop.sprite == null)
            {
                return;
            }

            SetImageAlpha(board.GetComponent<Image>(), 0.20f);
            SetImageAlpha(board.Find("EnemyRow")?.GetComponent<Image>(), 0.42f);
            SetImageAlpha(board.Find("PlayerRow")?.GetComponent<Image>(), 0.36f);
        }

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

        private static void SetImageAlpha(Image image, float alpha)
        {
            if (image == null)
            {
                return;
            }

            var color = image.color;
            color.a = alpha;
            image.color = color;
        }

        public void Render(BattleScreenState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }
            EnsurePresentationFxBindings();
            if (!HasCompleteBindings)
            {
                throw new InvalidOperationException(
                    "BattleScreenView has missing serialized bindings.");
            }

            CacheBoardMotionOrigin();

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
            float playbackSpeed,
            BattleSide? winner = null)
        {
            if (playbackEvent == null)
            {
                yield break;
            }

            var epoch = ++presentationEpoch;
            var durationScale = GetDurationScale(playbackSpeed);
            IsAnimationPlaying = true;
            CloseStandeeDetail();
            ClearHighlights();
            HideTransientBannerAndPulse();
            LastFeedbackId = ResolveFeedbackId(
                playbackEvent.Kind,
                playbackEvent.WasBlocked);
            var targetIsToken =
                FindCard(playbackEvent.TargetInstanceId)?.Model?.IsToken == true;
            LastAudioCueId = ResolveAudioCueId(
                playbackEvent.Kind,
                playbackEvent.WasBlocked,
                playbackEvent.AttackDelta,
                playbackEvent.HealthDelta,
                targetIsToken,
                winner,
                playbackEvent.EffectTrigger) ?? string.Empty;
            if (!string.IsNullOrEmpty(LastAudioCueId))
            {
                AudioService.Instance?.PlayCue(LastAudioCueId);
            }
            try
            {
                switch (playbackEvent.Kind)
                {
                    case BattlePlaybackEventKind.CombatStarted:
                        yield return PlayCombatStarted(
                            playbackEvent,
                            durationScale,
                            epoch);
                        break;
                    case BattlePlaybackEventKind.RoundStarted:
                        yield return PlayRoundStarted(
                            playbackEvent,
                            durationScale,
                            epoch);
                        break;
                    case BattlePlaybackEventKind.EffectTriggered:
                        yield return PlayEffectTriggered(
                            playbackEvent,
                            durationScale,
                            epoch);
                        break;
                    case BattlePlaybackEventKind.AttackStarted:
                        yield return PlayAttack(
                            playbackEvent,
                            durationScale,
                            epoch);
                        break;
                    case BattlePlaybackEventKind.DamageApplied:
                        yield return PlayDamage(
                            playbackEvent,
                            durationScale,
                            epoch);
                        break;
                    case BattlePlaybackEventKind.ShieldGained:
                        yield return PlayShield(
                            playbackEvent,
                            true,
                            durationScale,
                            epoch);
                        break;
                    case BattlePlaybackEventKind.ShieldLost:
                        yield return PlayShield(
                            playbackEvent,
                            false,
                            durationScale,
                            epoch);
                        break;
                    case BattlePlaybackEventKind.StatsChanged:
                        yield return PlayStats(
                            playbackEvent,
                            durationScale,
                            epoch);
                        break;
                    case BattlePlaybackEventKind.UnitDied:
                        yield return PlayDeath(
                            playbackEvent,
                            durationScale,
                            epoch);
                        break;
                    case BattlePlaybackEventKind.UnitSummoned:
                        yield return PlaySummon(
                            playbackEvent,
                            durationScale,
                            epoch);
                        break;
                    case BattlePlaybackEventKind.CombatEnded:
                        yield return PlayCombatEnded(
                            playbackEvent,
                            winner,
                            durationScale,
                            epoch);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(playbackEvent.Kind),
                            playbackEvent.Kind,
                            "Unknown battle presentation event.");
                }
            }
            finally
            {
                if (epoch == presentationEpoch)
                {
                    SnapTransientVisuals();
                    IsAnimationPlaying = false;
                }
            }
        }

        public static float GetDurationScale(float playbackSpeed)
        {
            return 1f / Mathf.Max(1f, playbackSpeed);
        }

        public static string ResolveFeedbackId(
            BattlePlaybackEventKind kind,
            bool wasBlocked = false)
        {
            switch (kind)
            {
                case BattlePlaybackEventKind.CombatStarted:
                    return "battle_start";
                case BattlePlaybackEventKind.RoundStarted:
                    return "battle_round";
                case BattlePlaybackEventKind.EffectTriggered:
                    return "battle_effect";
                case BattlePlaybackEventKind.AttackStarted:
                    return "battle_attack";
                case BattlePlaybackEventKind.DamageApplied:
                    return wasBlocked
                        ? "battle_damage_blocked"
                        : "battle_damage";
                case BattlePlaybackEventKind.ShieldGained:
                    return "battle_shield_gain";
                case BattlePlaybackEventKind.ShieldLost:
                    return "battle_shield_break";
                case BattlePlaybackEventKind.StatsChanged:
                    return "battle_stats";
                case BattlePlaybackEventKind.UnitDied:
                    return "battle_death";
                case BattlePlaybackEventKind.UnitSummoned:
                    return "battle_summon";
                case BattlePlaybackEventKind.CombatEnded:
                    return "battle_end";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        public static string ResolveAudioCueId(
            BattlePlaybackEventKind kind,
            bool wasBlocked = false,
            int attackDelta = 0,
            int healthDelta = 0,
            bool targetIsToken = false,
            BattleSide? winner = null,
            string effectTrigger = null)
        {
            switch (kind)
            {
                case BattlePlaybackEventKind.EffectTriggered:
                    return effectTrigger == "OnPlay"
                        ? PresentationAudioCueIds.ShopPlay
                        : null;
                case BattlePlaybackEventKind.AttackStarted:
                    return PresentationAudioCueIds.BattleAttackLight;
                case BattlePlaybackEventKind.DamageApplied:
                    return wasBlocked
                        ? null
                        : PresentationAudioCueIds.BattleHit;
                case BattlePlaybackEventKind.ShieldGained:
                    return PresentationAudioCueIds.BattleShieldGain;
                case BattlePlaybackEventKind.ShieldLost:
                    return PresentationAudioCueIds.BattleShieldBreak;
                case BattlePlaybackEventKind.StatsChanged:
                    return (attackDelta > 0 || healthDelta > 0) &&
                           attackDelta >= 0 &&
                           healthDelta >= 0
                        ? PresentationAudioCueIds.BattleStatUp
                        : null;
                case BattlePlaybackEventKind.UnitDied:
                    return targetIsToken
                        ? PresentationAudioCueIds.BattleTokenDeath
                        : PresentationAudioCueIds.BattleDeath;
                case BattlePlaybackEventKind.UnitSummoned:
                    return PresentationAudioCueIds.BattleSummon;
                case BattlePlaybackEventKind.CombatEnded:
                    if (!winner.HasValue)
                    {
                        return null;
                    }
                    return winner.Value == BattleSide.Player
                        ? PresentationAudioCueIds.BattleVictory
                        : PresentationAudioCueIds.BattleDefeat;
                default:
                    return null;
            }
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

        private IEnumerator PlayCombatStarted(
            BattlePlaybackEvent playbackEvent,
            float scale,
            int epoch)
        {
            HideResult();
            ShowFeedback("双方就位", FeedbackColor);
            PlayFx(
                "战斗开始",
                FeedbackColor,
                Vector2.zero,
                PresentationFxEmphasis.Critical,
                0.42f * scale,
                34f);
            yield return AnimatePulse(
                FeedbackColor,
                0.22f,
                0.28f * scale,
                epoch);
        }

        private IEnumerator PlayRoundStarted(
            BattlePlaybackEvent playbackEvent,
            float scale,
            int epoch)
        {
            ShowFeedback(
                string.IsNullOrWhiteSpace(playbackEvent.Message)
                    ? "新回合"
                    : playbackEvent.Message,
                ShieldColor);
            PlayFx(
                "新回合",
                ShieldColor,
                Vector2.zero,
                PresentationFxEmphasis.Strong,
                0.34f * scale,
                26f);
            yield return AnimatePulse(
                ShieldColor,
                0.13f,
                0.20f * scale,
                epoch);
        }

        private IEnumerator PlayEffectTriggered(
            BattlePlaybackEvent playbackEvent,
            float scale,
            int epoch)
        {
            var source = FindCard(playbackEvent.SourceInstanceId);
            var target = FindCard(playbackEvent.TargetInstanceId);
            var label = FormatEffectLabel(
                playbackEvent.EffectTrigger,
                playbackEvent.EffectAction);
            var color = ResolveEffectColor(playbackEvent.EffectTrigger);
            var sourcePosition = ResolveFxPosition(
                source,
                playbackEvent.SourceSide,
                playbackEvent.SourceIndex);
            var sourceImpactPosition = ResolveImpactFxPosition(
                source,
                playbackEvent.SourceSide,
                playbackEvent.SourceIndex);
            ShowFeedback(label, color);
            PlayFx(
                label,
                color,
                sourcePosition + Vector2.up * 18f,
                PresentationFxEmphasis.Strong,
                0.36f * scale,
                34f);
            impactFxLayer?.PlayEffectSeal(
                sourceImpactPosition,
                color,
                playbackEvent.EffectTrigger == "OnDeath",
                scale);
            SetSlotHighlight(
                playbackEvent.SourceSide,
                playbackEvent.SourceIndex,
                color);
            if (!string.IsNullOrWhiteSpace(playbackEvent.TargetInstanceId) &&
                playbackEvent.TargetInstanceId != playbackEvent.SourceInstanceId)
            {
                impactFxLayer?.PlayEffectLink(
                    sourceImpactPosition,
                    ResolveImpactFxPosition(
                        target,
                        playbackEvent.TargetSide,
                        playbackEvent.TargetIndex),
                    color,
                    playbackEvent.EffectTrigger == "OnPlay",
                    scale);
                SetSlotHighlight(
                    playbackEvent.TargetSide,
                    playbackEvent.TargetIndex,
                    color);
            }

            yield return AnimatePulse(
                color,
                0.14f,
                0.22f * scale,
                epoch);
        }

        private IEnumerator PlayAttack(
            BattlePlaybackEvent playbackEvent,
            float scale,
            int epoch)
        {
            var attacker = FindCard(playbackEvent.SourceInstanceId);
            var target = FindCard(playbackEvent.TargetInstanceId);
            ShowFeedback(
                playbackEvent.IsImmediateAttack ? "迅捷突进" : "突进",
                AttackerColor);
            PlayFx(
                playbackEvent.IsImmediateAttack ? "立即攻击" : "攻击",
                AttackerColor,
                ResolveFxPosition(
                    attacker,
                    playbackEvent.SourceSide,
                    playbackEvent.SourceIndex),
                PresentationFxEmphasis.Subtle,
                0.30f * scale,
                24f);
            impactFxLayer?.PlayAttackTrail(
                ResolveImpactFxPosition(
                    attacker,
                    playbackEvent.SourceSide,
                    playbackEvent.SourceIndex),
                ResolveImpactFxPosition(
                    target,
                    playbackEvent.TargetSide,
                    playbackEvent.TargetIndex),
                AttackerColor,
                scale,
                playbackEvent.IsImmediateAttack ||
                attacker?.Model?.Keywords.Contains("溅射") == true);
            SetSlotHighlight(
                playbackEvent.SourceSide,
                playbackEvent.SourceIndex,
                AttackerColor);
            SetSlotHighlight(
                playbackEvent.TargetSide,
                playbackEvent.TargetIndex,
                TargetColor);
            SetPulseColor(AttackerColor);
            if (attacker == null || target == null)
            {
                yield return AnimatePulse(
                    AttackerColor,
                    0.10f,
                    0.20f * scale,
                    epoch);
                yield break;
            }

            var rect = attacker.RectTransform;
            var start = rect.anchoredPosition;
            var worldDirection =
                (target.RectTransform.position - rect.position).normalized;
            var localDirection = rect.parent.InverseTransformVector(worldDirection);
            var direction = new Vector2(
                localDirection.x,
                localDirection.y).normalized;
            var startScale = rect.localScale;
            var destination = start + direction * 54f;
            yield return Animate(0.06f * scale, epoch, value =>
            {
                rect.localScale = Vector3.Lerp(
                    startScale,
                    startScale * 0.94f,
                    Smooth(value));
            });
            yield return Animate(0.09f * scale, epoch, value =>
            {
                rect.anchoredPosition = Vector2.Lerp(
                    start,
                    destination,
                    Smooth(value));
                rect.localScale = Vector3.Lerp(
                    startScale * 0.94f,
                    startScale * 1.04f,
                    Smooth(value));
                boardPulseCanvasGroup.alpha =
                    Mathf.Sin(value * Mathf.PI) * 0.10f;
            });
            yield return Animate(0.11f * scale, epoch, value =>
            {
                rect.anchoredPosition = Vector2.Lerp(
                    destination,
                    start,
                    Smooth(value));
                rect.localScale = Vector3.Lerp(
                    startScale * 1.04f,
                    startScale,
                    Smooth(value));
                boardPulseCanvasGroup.alpha =
                    (1f - value) * 0.06f;
            });
            if (epoch == presentationEpoch)
            {
                rect.anchoredPosition = start;
                rect.localScale = startScale;
            }
        }

        private IEnumerator PlayDamage(
            BattlePlaybackEvent playbackEvent,
            float scale,
            int epoch)
        {
            var target = FindCard(playbackEvent.TargetInstanceId);
            var position = ResolveFxPosition(
                target,
                playbackEvent.TargetSide,
                playbackEvent.TargetIndex);
            var impactPosition = ResolveImpactFxPosition(
                target,
                playbackEvent.TargetSide,
                playbackEvent.TargetIndex);
            var targetBaseHealth = target?.Model?.BaseHealth ??
                                   Mathf.Max(1, playbackEvent.Amount * 2);
            var targetIsDefeated = target != null &&
                                  target.Model != null &&
                                  target.Model.Health <= 0;
            var impactEmphasis = ResolveImpactEmphasis(
                playbackEvent.Amount,
                targetBaseHealth,
                targetIsDefeated);
            if (playbackEvent.IsSplashDamage)
            {
                impactFxLayer?.PlayCleaveArc(
                    ResolveImpactFxPosition(
                        FindCard(playbackEvent.SourceInstanceId),
                        playbackEvent.SourceSide,
                        playbackEvent.SourceIndex),
                    impactPosition,
                    AttackerColor,
                    scale);
                yield return Animate(
                    0.04f * scale,
                    epoch,
                    _ => { });
            }
            if (playbackEvent.WasBlocked)
            {
                impactFxLayer?.PlayImpact(
                    impactPosition,
                    ShieldColor,
                    PresentationFxEmphasis.Normal,
                    scale);
                ShowFeedback("格挡", ShieldColor);
                PlayFx(
                    "格挡",
                    ShieldColor,
                    position,
                    PresentationFxEmphasis.Strong,
                    0.32f * scale,
                    30f);
                SetSlotHighlight(
                    playbackEvent.TargetSide,
                    playbackEvent.TargetIndex,
                    ShieldColor);
                yield return AnimatePulse(
                    ShieldColor,
                    0.12f,
                    0.16f * scale,
                    epoch);
                yield break;
            }

            impactFxLayer?.PlayImpact(
                impactPosition,
                TargetColor,
                impactEmphasis,
                scale);
            ShowFeedback($"-{playbackEvent.Amount}", TargetColor);
            PlayFx(
                $"-{playbackEvent.Amount}",
                TargetColor,
                position,
                impactEmphasis,
                0.44f * scale,
                58f,
                false);
            SetSlotHighlight(
                playbackEvent.TargetSide,
                playbackEvent.TargetIndex,
                TargetColor);
            SetPulseColor(TargetColor);
            if (target == null)
            {
                yield return AnimatePulse(
                    TargetColor,
                    0.16f,
                    0.16f * scale,
                    epoch);
                yield break;
            }

            target.PlayStatChange(0, playbackEvent.HealthDelta);
            var rect = target.RectTransform;
            var start = rect.anchoredPosition;
            var shakeDistance = ResolveShakeDistance(impactEmphasis);
            target.SetHitFlash(Color.white, 0f);
            yield return Animate(
                ResolveHitStopSeconds(impactEmphasis) * scale,
                epoch,
                value => target.SetHitFlash(
                    Color.white,
                    Mathf.Lerp(0.18f, 0.92f, Smooth(value))));
            yield return Animate(0.15f * scale, epoch, value =>
            {
                var shake = Mathf.Sin(value * Mathf.PI * 6f) *
                            (1f - value) * shakeDistance * 1.65f;
                rect.anchoredPosition = start + Vector2.right * shake;
                if (boardMotionRoot != null && hasBoardMotionOrigin)
                {
                    var boardShake = new Vector2(
                        Mathf.Sin(value * Mathf.PI * 8f) * shakeDistance,
                        Mathf.Sin(value * Mathf.PI * 5f) *
                        shakeDistance * 0.42f) * (1f - value);
                    boardMotionRoot.anchoredPosition =
                        boardMotionOrigin + boardShake;
                }
                target.SetHitFlash(
                    Color.white,
                    (1f - value) * 0.78f);
                boardPulseCanvasGroup.alpha =
                    Mathf.Sin(value * Mathf.PI) * 0.16f;
            });
            if (epoch == presentationEpoch)
            {
                rect.anchoredPosition = start;
                target.SetHitFlash(Color.white, 0f);
                ResetBoardMotion();
            }
        }

        private IEnumerator PlayShield(
            BattlePlaybackEvent playbackEvent,
            bool gained,
            float scale,
            int epoch)
        {
            var target = FindCard(playbackEvent.TargetInstanceId);
            var color = gained ? ShieldColor : TargetColor;
            var label = gained ? "护盾 +" : "护盾破裂";
            impactFxLayer?.PlayShield(
                ResolveImpactFxPosition(
                    target,
                    playbackEvent.TargetSide,
                    playbackEvent.TargetIndex),
                color,
                gained,
                scale);
            ShowFeedback(label, color);
            PlayFx(
                label,
                color,
                ResolveFxPosition(
                    target,
                    playbackEvent.TargetSide,
                    playbackEvent.TargetIndex),
                PresentationFxEmphasis.Strong,
                0.38f * scale,
                42f);
            target?.SetShieldVisible(gained);
            SetSlotHighlight(
                playbackEvent.TargetSide,
                playbackEvent.TargetIndex,
                color);

            var rect = target == null ? null : target.RectTransform;
            var startScale = rect == null ? Vector3.one : rect.localScale;
            SetPulseColor(color);
            yield return Animate(0.18f * scale, epoch, value =>
            {
                boardPulseCanvasGroup.alpha =
                    Mathf.Sin(value * Mathf.PI) * (gained ? 0.12f : 0.18f);
                if (rect != null)
                {
                    rect.localScale = Vector3.Lerp(
                        startScale,
                        startScale * (gained ? 1.07f : 0.93f),
                        Mathf.Sin(value * Mathf.PI));
                }
            });
            if (epoch == presentationEpoch && rect != null)
            {
                rect.localScale = startScale;
            }
        }

        private IEnumerator PlayStats(
            BattlePlaybackEvent playbackEvent,
            float scale,
            int epoch)
        {
            var target = FindCard(playbackEvent.TargetInstanceId);
            var label = FormatStatDelta(
                playbackEvent.AttackDelta,
                playbackEvent.HealthDelta);
            var positive = playbackEvent.AttackDelta >= 0 &&
                           playbackEvent.HealthDelta >= 0;
            var color = positive ? GrowthColor : TargetColor;
            target?.PlayStatChange(
                playbackEvent.AttackDelta,
                playbackEvent.HealthDelta);
            if (positive)
            {
                impactFxLayer?.PlayStatGrowth(
                    ResolveImpactFxPosition(
                        target,
                        playbackEvent.TargetSide,
                        playbackEvent.TargetIndex),
                    color,
                    scale);
            }
            ShowFeedback("属性变化", color);
            PlayFx(
                label,
                color,
                ResolveFxPosition(
                    target,
                    playbackEvent.TargetSide,
                    playbackEvent.TargetIndex),
                PresentationFxEmphasis.Normal,
                0.42f * scale,
                52f);
            SetSlotHighlight(
                playbackEvent.TargetSide,
                playbackEvent.TargetIndex,
                color);
            yield return AnimatePulse(
                color,
                0.10f,
                0.18f * scale,
                epoch);
        }

        private IEnumerator PlayDeath(
            BattlePlaybackEvent playbackEvent,
            float scale,
            int epoch)
        {
            var target = FindCard(playbackEvent.TargetInstanceId);
            var token = target?.Model?.IsToken == true;
            impactFxLayer?.PlayDeath(
                ResolveImpactFxPosition(
                    target,
                    playbackEvent.TargetSide,
                    playbackEvent.TargetIndex),
                DeathColor,
                token,
                scale);
            var label = token ? "衍生消散" : "阵亡";
            ShowFeedback(label, DeathColor);
            PlayFx(
                label,
                DeathColor,
                ResolveFxPosition(
                    target,
                    playbackEvent.TargetSide,
                    playbackEvent.TargetIndex) + new Vector2(-72f, 38f),
                token
                    ? PresentationFxEmphasis.Normal
                    : PresentationFxEmphasis.Strong,
                0.42f * scale,
                38f,
                false);
            SetSlotHighlight(
                playbackEvent.TargetSide,
                playbackEvent.TargetIndex,
                DeathColor);
            SetPulseColor(DeathColor);
            if (target == null)
            {
                yield return AnimatePulse(
                    DeathColor,
                    0.12f,
                    0.18f * scale,
                    epoch);
                yield break;
            }

            var canvasGroup = target.GetComponent<CanvasGroup>();
            var rect = target.RectTransform;
            var startScale = target.transform.localScale;
            var startPosition = rect.anchoredPosition;
            var startRotation = rect.localEulerAngles;
            yield return Animate((token ? 0.14f : 0.24f) * scale, epoch, value =>
            {
                canvasGroup.alpha = 1f - value;
                target.transform.localScale = Vector3.Lerp(
                    startScale,
                    startScale * (token ? 0.42f : 0.72f),
                    value);
                rect.anchoredPosition = startPosition +
                                        Vector2.up * value *
                                        (token ? 10f : 20f);
                rect.localEulerAngles = Vector3.Lerp(
                    startRotation,
                    startRotation + new Vector3(
                        0f,
                        0f,
                        token ? 5f : 11f),
                    Smooth(value));
                boardPulseCanvasGroup.alpha =
                    Mathf.Sin(value * Mathf.PI) * 0.12f;
            });
            if (epoch == presentationEpoch)
            {
                canvasGroup.alpha = 1f;
                target.transform.localScale = startScale;
                rect.anchoredPosition = startPosition;
                rect.localEulerAngles = startRotation;
            }
        }

        private IEnumerator PlaySummon(
            BattlePlaybackEvent playbackEvent,
            float scale,
            int epoch)
        {
            var target = FindCard(playbackEvent.TargetInstanceId);
            impactFxLayer?.PlaySummonPortal(
                ResolveImpactFxPosition(
                    target,
                    playbackEvent.TargetSide,
                    playbackEvent.TargetIndex),
                GrowthColor,
                scale);
            ShowFeedback("增援入场", GrowthColor);
            PlayFx(
                "召唤",
                GrowthColor,
                ResolveFxPosition(
                    target,
                    playbackEvent.TargetSide,
                    playbackEvent.TargetIndex) + new Vector2(72f, -34f),
                PresentationFxEmphasis.Strong,
                0.40f * scale,
                46f);
            SetSlotHighlight(
                playbackEvent.TargetSide,
                playbackEvent.TargetIndex,
                GrowthColor);
            SetPulseColor(GrowthColor);
            if (target == null)
            {
                yield return AnimatePulse(
                    GrowthColor,
                    0.11f,
                    0.18f * scale,
                    epoch);
                yield break;
            }

            var endScale = target.transform.localScale;
            yield return Animate(0.20f * scale, epoch, value =>
            {
                target.transform.localScale = Vector3.Lerp(
                    endScale * 0.72f,
                    endScale,
                    Smooth(value));
                boardPulseCanvasGroup.alpha =
                    Mathf.Sin(value * Mathf.PI) * 0.11f;
            });
            if (epoch == presentationEpoch)
            {
                target.transform.localScale = endScale;
            }
        }

        private IEnumerator PlayCombatEnded(
            BattlePlaybackEvent playbackEvent,
            BattleSide? winner,
            float scale,
            int epoch)
        {
            ShowCombatResult(winner, playbackEvent.Message);
            var color = GetResultColor(winner);
            PlayFx(
                GetResultTitle(winner),
                color,
                Vector2.zero,
                PresentationFxEmphasis.Critical,
                0.46f * scale,
                18f);
            yield return AnimatePulse(
                color,
                0.20f,
                0.30f * scale,
                epoch);
        }

        private static string FormatEffectLabel(
            string trigger,
            string action)
        {
            var triggerLabel = ResolveEffectTriggerLabel(trigger);
            var actionLabel = ResolveEffectActionLabel(action);
            return string.IsNullOrWhiteSpace(actionLabel)
                ? triggerLabel
                : triggerLabel + " · " + actionLabel;
        }

        private static string ResolveEffectTriggerLabel(string trigger)
        {
            switch (trigger)
            {
                case "OnPlay": return "战吼";
                case "OnDeath": return "亡语";
                case "OnBattleStart": return "战斗开始";
                case "OnSummon": return "召唤响应";
                case "OnEnemySummon": return "敌方召唤响应";
                case "OnShieldLost": return "护盾破裂响应";
                case "OnShieldGained": return "护盾获得响应";
                case "OnAttackBefore": return "攻击前效果";
                case "OnKill": return "击杀效果";
                default: return "效果触发";
            }
        }

        private static string ResolveEffectActionLabel(string action)
        {
            switch (action)
            {
                case "AddShield": return "护盾";
                case "RemoveShield": return "破盾";
                case "ModifyStats": return "属性变化";
                case "SummonToken": return "召唤";
                case "ImmediateAttack": return "立即攻击";
                case "AddKeyword": return "获得关键词";
                case "DealDamage": return "伤害";
                default: return string.Empty;
            }
        }

        private static Color ResolveEffectColor(string trigger)
        {
            switch (trigger)
            {
                case "OnPlay": return FeedbackColor;
                case "OnDeath": return DeathColor;
                case "OnSummon":
                case "OnEnemySummon": return GrowthColor;
                case "OnShieldLost":
                case "OnShieldGained": return ShieldColor;
                default: return AttackerColor;
            }
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
            foreach (var slot in (enemySlots ?? Array.Empty<BattleSlotView>())
                         .Concat(playerSlots ?? Array.Empty<BattleSlotView>()))
            {
                if (slot != null)
                {
                    slot.SetHighlight(Color.clear, Vector2.zero);
                }
            }
        }

        private void ShowFeedback(string message, Color color)
        {
            if (feedbackText == null || feedbackCanvasGroup == null)
            {
                return;
            }
            feedbackText.text = message ?? string.Empty;
            feedbackText.color = color;
            feedbackCanvasGroup.alpha =
                string.IsNullOrWhiteSpace(message) ? 0f : 1f;
        }

        public void ShowCombatResult(BattleSide? winner, string detail)
        {
            if (resultLayer == null || resultCanvasGroup == null ||
                resultTitleText == null || resultBodyText == null)
            {
                return;
            }

            resultTitleText.text = GetResultTitle(winner);
            resultTitleText.color = GetResultColor(winner);
            resultBodyText.text = detail ?? string.Empty;
            resultLayer.SetActive(true);
            resultCanvasGroup.alpha = 1f;
            resultCanvasGroup.interactable = false;
            resultCanvasGroup.blocksRaycasts = false;
        }

        public void SnapAndClear()
        {
            presentationEpoch++;
            IsAnimationPlaying = false;
            CloseStandeeDetail();
            SnapTransientVisuals();
            if (feedbackFxPool != null)
            {
                feedbackFxPool.ClearImmediate();
            }
            impactFxLayer?.ClearImmediate();
            AudioService.Instance?.StopAllTransientCues();
            HideResult();
        }

        private void OnDisable()
        {
            SnapAndClear();
        }

        private void PlayFx(
            string label,
            Color color,
            Vector2 position,
            PresentationFxEmphasis emphasis,
            float duration,
            float verticalTravel,
            bool showBackdrop = true)
        {
            feedbackFxPool?.Play(
                label,
                color,
                position,
                emphasis,
                duration,
                verticalTravel,
                showBackdrop);
        }

        private Vector2 ResolveFxPosition(
            BattleStandeeView standee,
            BattleSide? side,
            int index)
        {
            RectTransform source = null;
            if (standee != null)
            {
                source = standee.RectTransform;
            }
            else if (side.HasValue && index >= 0 &&
                     index < BattleBoardState.SlotCount)
            {
                var slots = side.Value == BattleSide.Player
                    ? playerSlots
                    : enemySlots;
                if (slots != null && index < slots.Length && slots[index] != null)
                {
                    source = slots[index].Content;
                }
            }

            var poolRect = feedbackFxPool == null
                ? null
                : feedbackFxPool.transform as RectTransform;
            if (source == null || poolRect == null)
            {
                return Vector2.zero;
            }

            var world = source.TransformPoint(source.rect.center);
            var local = poolRect.InverseTransformPoint(world);
            return new Vector2(local.x, local.y);
        }

        private Vector2 ResolveImpactFxPosition(
            BattleStandeeView standee,
            BattleSide? side,
            int index)
        {
            RectTransform source = null;
            if (standee != null)
            {
                source = standee.RectTransform;
            }
            else if (side.HasValue && index >= 0 &&
                     index < BattleBoardState.SlotCount)
            {
                var slots = side.Value == BattleSide.Player
                    ? playerSlots
                    : enemySlots;
                if (slots != null && index < slots.Length && slots[index] != null)
                {
                    source = slots[index].Content;
                }
            }

            var layerRect = impactFxLayer == null
                ? null
                : impactFxLayer.transform as RectTransform;
            if (source == null || layerRect == null)
            {
                return Vector2.zero;
            }

            var world = source.TransformPoint(source.rect.center);
            var local = layerRect.InverseTransformPoint(world);
            return new Vector2(local.x, local.y);
        }

        private IEnumerator AnimatePulse(
            Color color,
            float peakAlpha,
            float duration,
            int epoch)
        {
            SetPulseColor(color);
            yield return Animate(duration, epoch, value =>
            {
                if (boardPulseCanvasGroup != null)
                {
                    boardPulseCanvasGroup.alpha =
                        Mathf.Sin(value * Mathf.PI) * peakAlpha;
                }
            });
        }

        private void SetPulseColor(Color color)
        {
            if (boardPulseImage != null)
            {
                boardPulseImage.color = new Color(
                    color.r,
                    color.g,
                    color.b,
                    1f);
            }
        }

        private void HideTransientBannerAndPulse()
        {
            if (feedbackCanvasGroup != null)
            {
                feedbackCanvasGroup.alpha = 0f;
            }
            if (feedbackText != null)
            {
                feedbackText.text = string.Empty;
            }
            if (boardPulseCanvasGroup != null)
            {
                boardPulseCanvasGroup.alpha = 0f;
                boardPulseCanvasGroup.interactable = false;
                boardPulseCanvasGroup.blocksRaycasts = false;
            }
        }

        private void SnapTransientVisuals()
        {
            ClearHighlights();
            HideTransientBannerAndPulse();
            ResetBoardMotion();
            foreach (var standee in standeesById.Values)
            {
                if (standee != null)
                {
                    standee.ResetPresentationState();
                }
            }
        }

        private void CacheBoardMotionOrigin()
        {
            if (boardMotionRoot == null || hasBoardMotionOrigin)
            {
                return;
            }

            boardMotionOrigin = boardMotionRoot.anchoredPosition;
            hasBoardMotionOrigin = true;
        }

        private void EnsurePresentationFxBindings()
        {
            if (safeArea == null)
            {
                return;
            }

            if (boardMotionRoot == null)
            {
                boardMotionRoot = safeArea.Find("Board") as RectTransform;
            }

            if (impactFxLayer == null)
            {
                var vfxLayer = safeArea.Find("VfxLayer");
                if (vfxLayer != null)
                {
                    impactFxLayer =
                        vfxLayer.GetComponent<BattleImpactFxLayer>();
                    if (impactFxLayer == null)
                    {
                        impactFxLayer = vfxLayer.gameObject
                            .AddComponent<BattleImpactFxLayer>();
                    }
                    impactFxLayer.Configure(
                        Resources.GetBuiltinResource<Sprite>(
                            "UI/Skin/UISprite.psd"),
                        32);
                }
            }

            CacheBoardMotionOrigin();
        }

        private void ResetBoardMotion()
        {
            if (boardMotionRoot != null && hasBoardMotionOrigin)
            {
                boardMotionRoot.anchoredPosition = boardMotionOrigin;
            }
        }

        private void HideResult()
        {
            if (resultCanvasGroup != null)
            {
                resultCanvasGroup.alpha = 0f;
                resultCanvasGroup.interactable = false;
                resultCanvasGroup.blocksRaycasts = false;
            }
            if (resultTitleText != null)
            {
                resultTitleText.text = string.Empty;
            }
            if (resultBodyText != null)
            {
                resultBodyText.text = string.Empty;
            }
            if (resultLayer != null)
            {
                resultLayer.SetActive(false);
            }
        }

        private static string GetResultTitle(BattleSide? winner)
        {
            if (!winner.HasValue)
            {
                return "战斗平局";
            }
            return winner.Value == BattleSide.Player
                ? "战斗胜利"
                : "战斗失利";
        }

        private static Color GetResultColor(BattleSide? winner)
        {
            if (!winner.HasValue)
            {
                return new Color(0.70f, 0.78f, 0.90f, 1f);
            }
            return winner.Value == BattleSide.Player
                ? FeedbackColor
                : TargetColor;
        }

        private static string FormatStatDelta(
            int attackDelta,
            int healthDelta)
        {
            if (attackDelta != 0 && healthDelta != 0)
            {
                return $"{FormatDelta(attackDelta)}/{FormatDelta(healthDelta)}";
            }
            if (attackDelta != 0)
            {
                return $"{FormatDelta(attackDelta)} 攻击";
            }
            if (healthDelta != 0)
            {
                return $"{FormatDelta(healthDelta)} 生命";
            }
            return "属性刷新";
        }

        public static PresentationFxEmphasis ResolveImpactEmphasis(
            int damage,
            int targetBaseHealth,
            bool isLethal = false)
        {
            if (isLethal)
            {
                return PresentationFxEmphasis.Critical;
            }

            var safeHealth = Mathf.Max(1, targetBaseHealth);
            var strongThreshold = Mathf.Max(
                2,
                Mathf.CeilToInt(safeHealth * 0.5f));
            return Mathf.Abs(damage) >= strongThreshold
                ? PresentationFxEmphasis.Strong
                : PresentationFxEmphasis.Normal;
        }

        public static float ResolveHitStopSeconds(
            PresentationFxEmphasis emphasis)
        {
            switch (emphasis)
            {
                case PresentationFxEmphasis.Critical: return 0.060f;
                case PresentationFxEmphasis.Strong: return 0.045f;
                default: return 0.025f;
            }
        }

        private static float ResolveShakeDistance(
            PresentationFxEmphasis emphasis)
        {
            switch (emphasis)
            {
                case PresentationFxEmphasis.Critical: return 8f;
                case PresentationFxEmphasis.Strong: return 5f;
                default: return 2.5f;
            }
        }

        private static string FormatDelta(int value)
        {
            return value > 0 ? "+" + value : value.ToString();
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
                ArtworkFallbackId = source.ArtworkFallbackId,
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
                IsToken = source.IsToken,
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

        private IEnumerator Animate(
            float duration,
            int epoch,
            Action<float> update)
        {
            var elapsed = 0f;
            duration = Mathf.Max(0.001f, duration);
            while (elapsed < duration && epoch == presentationEpoch)
            {
                elapsed += Mathf.Clamp(
                    Time.unscaledDeltaTime,
                    0.0001f,
                    0.05f);
                update(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            if (epoch == presentationEpoch)
            {
                update(1f);
            }
        }

        private static float Smooth(float value)
        {
            return value * value * (3f - 2f * value);
        }
    }
}

using System;
using System.Linq;
using SpireChess.Battle;
using SpireChess.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpireChess.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class BattleStandeeView : MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        [Header("Presentation")]
        [SerializeField] private PresentationSpriteCatalog spriteCatalog;
        [SerializeField] private PresentationTheme theme;
        [SerializeField] private Image portrait;
        [SerializeField] private Text portraitFallback;
        [SerializeField] private Image frame;
        [SerializeField] private Image shieldOverlay;
        [SerializeField] private Image tauntBase;
        [SerializeField] private Image deathrattleSeal;
        [SerializeField] private Image splashMark;
        [SerializeField] private Image attackMedallion;
        [SerializeField] private Image healthMedallion;
        [SerializeField] private Text attackText;
        [SerializeField] private Text healthText;
        [SerializeField] private Image targetHighlight;

        private BattleTestController controller;
        private BattleScreenView screenView;
        private Canvas rootCanvas;
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Transform originalParent;
        private Vector2 originalAnchoredPosition;
        private CardViewModel model;
        private bool draggable;
        private bool dropHandled;

        public BattleSide Side { get; private set; }
        public int Index { get; private set; }
        public string InstanceId => model == null ? string.Empty : model.InstanceId;
        public CardViewModel Model => model;
        public RectTransform RectTransform => rectTransform;
        public bool IsShieldVisible => shieldOverlay != null &&
                                       shieldOverlay.gameObject.activeSelf;
        public bool IsTauntVisible => tauntBase != null &&
                                      tauntBase.gameObject.activeSelf;
        public bool IsDeathrattleVisible => deathrattleSeal != null &&
                                            deathrattleSeal.gameObject.activeSelf;
        public bool IsSplashVisible => splashMark != null &&
                                       splashMark.gameObject.activeSelf;
        public bool IsTargetHighlighted => targetHighlight != null &&
                                           targetHighlight.gameObject.activeSelf;
        public bool HasCompleteBindings =>
            spriteCatalog != null && theme != null && portrait != null &&
            portraitFallback != null && frame != null && shieldOverlay != null &&
            tauntBase != null && deathrattleSeal != null && splashMark != null &&
            attackMedallion != null && healthMedallion != null &&
            attackText != null && healthText != null && targetHighlight != null;

        public void Initialize(
            BattleTestController value,
            BattleScreenView owner,
            Canvas canvas,
            BattleSide side,
            int index,
            bool canDrag)
        {
            controller = value;
            screenView = owner;
            rootCanvas = canvas;
            Side = side;
            Index = index;
            draggable = canDrag;
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void Render(CardViewModel value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (!HasCompleteBindings)
            {
                throw new InvalidOperationException(
                    "BattleStandeeView has missing serialized bindings.");
            }

            model = value;
            ApplyCatalogSprites(value);
            ApplyPortrait(value);
            frame.color = value.IsGolden
                ? theme.GoldenFrameTint
                : Color.white;
            attackText.text = value.Attack.ToString();
            healthText.text = value.Health.ToString();
            attackText.color = Color.white;
            healthText.color = Color.white;
            shieldOverlay.gameObject.SetActive(value.HasShield);
            tauntBase.gameObject.SetActive(HasKeyword(value, "嘲讽", "Taunt"));
            deathrattleSeal.gameObject.SetActive(
                HasKeyword(value, "亡语", "Deathrattle"));
            splashMark.gameObject.SetActive(
                HasKeyword(value, "溅射", "Cleave"));
            ApplyTargetState(value);
        }

        public void MarkDropHandled()
        {
            dropHandled = true;
        }

        public void SetShieldVisible(bool visible)
        {
            if (shieldOverlay != null)
            {
                shieldOverlay.gameObject.SetActive(visible);
            }
        }

        public void PlayStatChange(int attackDelta, int healthDelta)
        {
            if (attackDelta != 0)
            {
                attackText.color = attackDelta > 0
                    ? new Color(1f, 0.84f, 0.28f, 1f)
                    : new Color(1f, 0.36f, 0.30f, 1f);
            }
            if (healthDelta != 0)
            {
                healthText.color = healthDelta > 0
                    ? new Color(0.48f, 1f, 0.52f, 1f)
                    : new Color(1f, 0.36f, 0.30f, 1f);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (model != null && originalParent == null)
            {
                screenView?.ShowStandeeDetail(this, model);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            screenView?.HideStandeeDetail(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (model != null && (eventData == null || !eventData.dragging))
            {
                screenView?.ToggleStandeeDetailLock(this, model);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!draggable || controller == null || controller.IsBattleLocked ||
                rootCanvas == null)
            {
                return;
            }

            screenView?.CloseStandeeDetail();
            dropHandled = false;
            originalParent = transform.parent;
            originalAnchoredPosition = rectTransform.anchoredPosition;
            transform.SetParent(rootCanvas.transform, true);
            transform.SetAsLastSibling();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.82f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!draggable || controller == null || controller.IsBattleLocked ||
                originalParent == null || eventData == null)
            {
                return;
            }

            rectTransform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (originalParent == null)
            {
                return;
            }

            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
            if (!dropHandled && transform.parent == rootCanvas.transform)
            {
                transform.SetParent(originalParent, false);
                rectTransform.anchoredPosition = originalAnchoredPosition;
            }

            originalParent = null;
            dropHandled = false;
        }

        private void ApplyCatalogSprites(CardViewModel value)
        {
            ApplySprite(
                frame,
                value.IsGolden
                    ? spriteCatalog.BattleGoldenStandeeFrame
                    : spriteCatalog.BattleNormalStandeeFrame,
                false);
            ApplySprite(attackMedallion,
                spriteCatalog.BattleAttackMedallion, true);
            ApplySprite(healthMedallion,
                spriteCatalog.BattleHealthMedallion, true);
            ApplySprite(shieldOverlay,
                spriteCatalog.BattleShieldOverlay, true);
            ApplySprite(tauntBase, spriteCatalog.BattleTauntBase, false);
            ApplySprite(deathrattleSeal,
                spriteCatalog.BattleDeathrattleSeal, true);
            ApplySprite(splashMark, spriteCatalog.BattleSplashMark, true);
        }

        private void ApplyPortrait(CardViewModel value)
        {
            if (spriteCatalog.TryGetArtwork(value.ArtId, out var sprite))
            {
                portrait.sprite = sprite;
                portrait.color = Color.white;
                portrait.type = Image.Type.Simple;
                portrait.preserveAspect = false;
                portraitFallback.gameObject.SetActive(false);
                return;
            }

            portrait.sprite = null;
            portrait.color = theme.GetPortraitTint(value.RaceText);
            portraitFallback.text = string.IsNullOrWhiteSpace(value.Name)
                ? "?"
                : value.Name.Substring(0, 1);
            portraitFallback.gameObject.SetActive(true);
        }

        private void ApplyTargetState(CardViewModel value)
        {
            var visible = value.IsLegalTarget || value.IsSelected;
            targetHighlight.gameObject.SetActive(visible);
            targetHighlight.color = value.IsSelected
                ? theme.SelectedTargetTint
                : theme.LegalTargetTint;
        }

        private static bool HasKeyword(
            CardViewModel value,
            string localized,
            string canonical)
        {
            return (value.Keywords ?? Array.Empty<string>()).Any(keyword =>
                       string.Equals(keyword, localized, StringComparison.Ordinal) ||
                       string.Equals(keyword, canonical, StringComparison.Ordinal)) ||
                   (value.AbilityLabels ?? Array.Empty<string>()).Any(keyword =>
                       string.Equals(keyword, localized, StringComparison.Ordinal) ||
                       string.Equals(keyword, canonical, StringComparison.Ordinal));
        }

        private static void ApplySprite(
            Image target,
            Sprite sprite,
            bool preserveAspect)
        {
            target.sprite = sprite;
            target.type = Image.Type.Simple;
            target.preserveAspect = preserveAspect;
        }
    }
}

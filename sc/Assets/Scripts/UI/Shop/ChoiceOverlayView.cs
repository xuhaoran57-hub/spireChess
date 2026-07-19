using System;
using SpireChess.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.UI.Shop
{
    [DisallowMultipleComponent]
    public sealed class ChoiceOverlayView : MonoBehaviour
    {
        private const int CandidateCapacity = 4;

        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private GameObject[] candidateRoots =
            Array.Empty<GameObject>();
        [SerializeField] private RectTransform[] cardContents =
            Array.Empty<RectTransform>();
        [SerializeField] private Button[] candidateButtons =
            Array.Empty<Button>();
        [SerializeField] private Text[] candidateLabels = Array.Empty<Text>();
        [SerializeField] private Text[] candidateDescriptions =
            Array.Empty<Text>();
        [SerializeField] private Button cancelButton;
        [SerializeField] private Text cancelButtonText;

        private ShopTestController controller;
        private bool isBound;

        public bool HasCompleteBindings =>
            cardPrefab != null && titleText != null && descriptionText != null &&
            candidateRoots != null &&
            candidateRoots.Length == CandidateCapacity &&
            Array.TrueForAll(candidateRoots, value => value != null) &&
            cardContents != null &&
            cardContents.Length == CandidateCapacity &&
            Array.TrueForAll(cardContents, value => value != null) &&
            candidateButtons != null &&
            candidateButtons.Length == CandidateCapacity &&
            Array.TrueForAll(candidateButtons, value => value != null) &&
            candidateLabels != null &&
            candidateLabels.Length == CandidateCapacity &&
            Array.TrueForAll(candidateLabels, value => value != null) &&
            candidateDescriptions != null &&
            candidateDescriptions.Length == CandidateCapacity &&
            Array.TrueForAll(candidateDescriptions, value => value != null) &&
            cancelButton != null && cancelButtonText != null;

        public int RenderedCandidateCount { get; private set; }
        public bool IsVisible => gameObject.activeSelf;
        public bool CanCancel => cancelButton != null &&
                                 cancelButton.gameObject.activeSelf &&
                                 cancelButton.interactable;

        public void Bind(ShopTestController value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!HasCompleteBindings)
            {
                throw new InvalidOperationException(
                    "ChoiceOverlayView has missing serialized bindings.");
            }

            if (isBound)
            {
                if (!ReferenceEquals(controller, value))
                {
                    throw new InvalidOperationException(
                        "ChoiceOverlayView is already bound to another controller.");
                }

                return;
            }

            controller = value;
            for (var index = 0; index < candidateButtons.Length; index++)
            {
                var candidateIndex = index;
                candidateButtons[index].onClick.AddListener(
                    () => controller.SelectDiscoverCandidate(candidateIndex));
            }

            cancelButton.onClick.AddListener(() => controller.CancelDiscover());
            isBound = true;
        }

        public void Render(ChoiceViewModel model)
        {
            if (!HasCompleteBindings)
            {
                throw new InvalidOperationException(
                    "ChoiceOverlayView has missing serialized bindings.");
            }

            ClearCandidateCards();
            if (model == null)
            {
                RenderedCandidateCount = 0;
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            titleText.text = model.Title ?? string.Empty;
            descriptionText.text = model.Description ?? string.Empty;
            var candidates = model.Candidates ??
                             Array.Empty<ChoiceCandidateViewModel>();
            RenderedCandidateCount = Math.Min(
                candidates.Length,
                CandidateCapacity);
            for (var index = 0; index < CandidateCapacity; index++)
            {
                var visible = index < RenderedCandidateCount;
                candidateRoots[index].SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                var candidate = candidates[index] ??
                                new ChoiceCandidateViewModel();
                candidateButtons[index].interactable = candidate.IsInteractable;
                candidateLabels[index].text = candidate.Label ?? string.Empty;
                candidateDescriptions[index].text =
                    candidate.Description ?? string.Empty;
                if (!candidate.IsCard)
                {
                    cardContents[index].gameObject.SetActive(false);
                    continue;
                }

                cardContents[index].gameObject.SetActive(true);
                var cardObject = Instantiate(
                    cardPrefab,
                    cardContents[index],
                    false);
                var cardRect = cardObject.GetComponent<RectTransform>();
                PlaceAtTopLeft(cardRect);
                var cardView = cardObject.GetComponent<CardView>();
                if (cardView == null)
                {
                    throw new InvalidOperationException(
                        "The choice card prefab has no CardView component.");
                }

                cardView.Render(candidate.Card);
            }

            cancelButton.gameObject.SetActive(true);
            cancelButton.interactable = model.CanCancel;
            cancelButtonText.text = model.CanCancel
                ? "取消"
                : "此选择不可取消";
        }

        private void ClearCandidateCards()
        {
            if (cardContents == null)
            {
                return;
            }

            foreach (var content in cardContents)
            {
                if (content == null)
                {
                    continue;
                }

                for (var index = content.childCount - 1; index >= 0; index--)
                {
                    var child = content.GetChild(index).gameObject;
                    if (Application.isPlaying)
                    {
                        child.transform.SetParent(null, false);
                        Destroy(child);
                    }
                    else
                    {
                        DestroyImmediate(child);
                    }
                }
            }
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
    }
}

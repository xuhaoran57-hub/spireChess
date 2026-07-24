using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.UI.Run
{
    [DisallowMultipleComponent]
    public sealed class RunChoiceOptionView : MonoBehaviour
    {
        [SerializeField] private PresentationSpriteCatalog spriteCatalog;
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text badgeText;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;

        private RunTestController controller;
        private RunChoiceOptionState state;

        public bool HasCompleteBindings => spriteCatalog != null &&
                                           button != null && background != null &&
                                           iconImage != null &&
                                           badgeText != null && titleText != null &&
                                           descriptionText != null;

        public void Bind(RunTestController value)
        {
            controller = value;
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        public void Render(RunChoiceOptionState value)
        {
            state = value;
            RenderIcon(value.IconId);
            badgeText.text = value.Badge ?? string.Empty;
            badgeText.gameObject.SetActive(!string.IsNullOrWhiteSpace(value.Badge));
            titleText.text = value.Label ?? string.Empty;
            descriptionText.text = value.Description ?? string.Empty;
            button.interactable = value.IsInteractable;
            background.color = value.IsInteractable
                ? new Color(0.13f, 0.20f, 0.28f, 1f)
                : new Color(0.11f, 0.12f, 0.15f, 0.95f);
        }

        private void RenderIcon(string iconId)
        {
            if (iconImage == null)
            {
                return;
            }

            iconImage.sprite = null;
            iconImage.gameObject.SetActive(false);
            if (string.IsNullOrWhiteSpace(iconId) || spriteCatalog == null)
            {
                return;
            }

            var resolution = spriteCatalog.ResolveArtwork(
                iconId,
                null,
                out var sprite,
                out _);
            if (resolution == ArtworkResolution.Missing || sprite == null)
            {
                return;
            }

            iconImage.sprite = sprite;
            iconImage.preserveAspect = true;
            iconImage.gameObject.SetActive(true);
        }

        private void HandleClick()
        {
            if (controller != null && state != null && state.IsInteractable)
            {
                controller.ExecuteUiAction(
                    state.Action,
                    state.PrimaryId,
                    state.SecondaryId);
            }
        }
    }
}

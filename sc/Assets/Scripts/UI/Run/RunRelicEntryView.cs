using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.UI.Run
{
    [DisallowMultipleComponent]
    public sealed class RunRelicEntryView : MonoBehaviour
    {
        [SerializeField] private PresentationTheme theme;
        [SerializeField] private PresentationSpriteCatalog spriteCatalog;
        [SerializeField] private Image background;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text gradeText;
        [SerializeField] private Text nameText;
        [SerializeField] private Text metaText;
        [SerializeField] private Text descriptionText;

        public string LastArtId { get; private set; } = string.Empty;
        public ArtworkResolution LastArtworkResolution { get; private set; } =
            ArtworkResolution.Missing;
        public bool HasCompleteBindings => theme != null &&
                                            spriteCatalog != null &&
                                            background != null && iconImage != null &&
                                           gradeText != null &&
                                           nameText != null && metaText != null &&
                                           descriptionText != null;

        public void Render(RunRelicState state)
        {
            RenderIcon(state.IconId);
            gradeText.text = state.GradeText ?? string.Empty;
            nameText.text = state.Name ?? string.Empty;
            metaText.text = string.IsNullOrWhiteSpace(state.ProgressText)
                ? state.CategoryText ?? string.Empty
                : $"{state.CategoryText} · {state.ProgressText}";
            descriptionText.text = state.Description ?? string.Empty;
            background.color = state.GradeText == "冠冕"
                ? new Color(0.28f, 0.22f, 0.08f, 0.98f)
                : new Color(0.10f, 0.22f, 0.28f, 0.98f);
            background.color = string.Equals(
                    state.GradeText,
                    "\u51a0\u5195",
                    System.StringComparison.Ordinal)
                ? Color.Lerp(theme.PanelRaised, theme.Accent, 0.34f)
                : theme.PanelRaised;
            gradeText.color = theme.Accent;
            nameText.color = theme.TextPrimary;
            metaText.color = theme.Success;
            descriptionText.color = theme.TextSecondary;
        }

        private void RenderIcon(string iconId)
        {
            LastArtId = iconId ?? string.Empty;
            LastArtworkResolution = ArtworkResolution.Missing;
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
            LastArtworkResolution = resolution;
            if (resolution == ArtworkResolution.Missing || sprite == null)
            {
                return;
            }

            iconImage.sprite = sprite;
            iconImage.preserveAspect = true;
            iconImage.gameObject.SetActive(true);
        }
    }
}

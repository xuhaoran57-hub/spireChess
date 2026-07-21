using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.UI.Run
{
    [DisallowMultipleComponent]
    public sealed class RunChoiceOptionView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Text badgeText;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;

        private RunTestController controller;
        private RunChoiceOptionState state;

        public bool HasCompleteBindings => button != null && background != null &&
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
            badgeText.text = value.Badge ?? string.Empty;
            badgeText.gameObject.SetActive(!string.IsNullOrWhiteSpace(value.Badge));
            titleText.text = value.Label ?? string.Empty;
            descriptionText.text = value.Description ?? string.Empty;
            button.interactable = value.IsInteractable;
            background.color = value.IsInteractable
                ? new Color(0.13f, 0.20f, 0.28f, 1f)
                : new Color(0.11f, 0.12f, 0.15f, 0.95f);
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

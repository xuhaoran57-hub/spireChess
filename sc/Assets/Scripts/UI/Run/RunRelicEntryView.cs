using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.UI.Run
{
    [DisallowMultipleComponent]
    public sealed class RunRelicEntryView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Text gradeText;
        [SerializeField] private Text nameText;
        [SerializeField] private Text metaText;
        [SerializeField] private Text descriptionText;

        public bool HasCompleteBindings => background != null && gradeText != null &&
                                           nameText != null && metaText != null &&
                                           descriptionText != null;

        public void Render(RunRelicState state)
        {
            gradeText.text = state.GradeText ?? string.Empty;
            nameText.text = state.Name ?? string.Empty;
            metaText.text = string.IsNullOrWhiteSpace(state.ProgressText)
                ? state.CategoryText ?? string.Empty
                : $"{state.CategoryText} · {state.ProgressText}";
            descriptionText.text = state.Description ?? string.Empty;
            background.color = state.GradeText == "冠冕"
                ? new Color(0.28f, 0.22f, 0.08f, 0.98f)
                : new Color(0.10f, 0.22f, 0.28f, 0.98f);
        }
    }
}

using SpireChess.Run;
using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.UI.Run
{
    [DisallowMultipleComponent]
    public sealed class RunMapNodeView : MonoBehaviour
    {
        [SerializeField] private PresentationTheme theme;
        [SerializeField] private Image background;
        [SerializeField] private Outline outline;
        [SerializeField] private Button button;
        [SerializeField] private Image typeIconBackground;
        [SerializeField] private Text typeIconText;
        [SerializeField] private Image stateOverlay;
        [SerializeField] private Image currentPulse;
        [SerializeField] private Text routeText;
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Text statusText;

        private RunTestController controller;
        private string nodeId;
        private string iconId;
        private RunMapPresentationStatus presentationStatus;

        public string NodeId => nodeId;
        public string IconId => iconId;
        public string IconGlyph => typeIconText == null
            ? string.Empty
            : typeIconText.text;
        public RunMapPresentationStatus PresentationStatus => presentationStatus;
        public bool IsCurrentPulseVisible =>
            currentPulse != null && currentPulse.gameObject.activeSelf;
        public bool HasCompleteBindings =>
            theme != null && background != null && outline != null &&
            button != null && typeIconBackground != null &&
            typeIconText != null && stateOverlay != null &&
            currentPulse != null && routeText != null && titleText != null &&
            subtitleText != null && statusText != null;

        public void Bind(RunTestController value)
        {
            controller = value;
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        public void Render(RunMapNodeState state)
        {
            nodeId = state.NodeId;
            iconId = state.IconId;
            presentationStatus = state.PresentationStatus;
            routeText.text = state.RouteText ?? string.Empty;
            routeText.gameObject.SetActive(!string.IsNullOrWhiteSpace(state.RouteText));
            titleText.text = state.Title ?? string.Empty;
            subtitleText.text = state.Subtitle ?? string.Empty;
            statusText.text = ToStatusText(state.PresentationStatus);
            typeIconText.text = ToIconGlyph(state.IconId, state.Type);
            button.interactable = state.IsInteractable;

            background.color = theme.GetMapNodeColor(
                state.Type,
                state.PresentationStatus);
            typeIconBackground.color = theme.GetMapTypeColor(state.Type);
            stateOverlay.color = theme.GetMapStatusOverlayColor(
                state.PresentationStatus);
            outline.effectColor = theme.GetMapStatusColor(
                state.PresentationStatus);
            outline.effectDistance =
                state.PresentationStatus == RunMapPresentationStatus.Locked ||
                state.PresentationStatus == RunMapPresentationStatus.Abandoned
                    ? new Vector2(1f, -1f)
                    : new Vector2(3f, -3f);

            titleText.color = theme.TextPrimary;
            subtitleText.color = theme.TextSecondary;
            routeText.color = theme.Accent;
            statusText.color = theme.GetMapStatusColor(state.PresentationStatus);
            typeIconText.color = theme.TextPrimary;
            currentPulse.color = theme.GetMapStatusColor(
                RunMapPresentationStatus.Current);
            currentPulse.gameObject.SetActive(
                state.PresentationStatus == RunMapPresentationStatus.Current);
        }

        private void Update()
        {
            if (currentPulse == null || !currentPulse.gameObject.activeSelf)
            {
                return;
            }

            var color = currentPulse.color;
            color.a = Mathf.Lerp(
                0.28f,
                0.78f,
                Mathf.PingPong(Time.unscaledTime * 0.85f, 1f));
            currentPulse.color = color;
        }

        private void HandleClick()
        {
            if (controller != null && !string.IsNullOrWhiteSpace(nodeId))
            {
                controller.EnterNode(nodeId);
            }
        }

        private static string ToStatusText(RunMapPresentationStatus status)
        {
            switch (status)
            {
                case RunMapPresentationStatus.Reachable:
                    return "\u53ef\u8fdb\u5165";
                case RunMapPresentationStatus.Current:
                    return "\u5f53\u524d";
                case RunMapPresentationStatus.Resolved:
                    return "\u5df2\u5b8c\u6210";
                case RunMapPresentationStatus.Abandoned:
                    return "\u5df2\u653e\u5f03";
                default:
                    return "\u672a\u89e3\u9501";
            }
        }

        private static string ToIconGlyph(string value, RunNodeType fallbackType)
        {
            switch (value)
            {
                case "icon_map_shop": return "\u5546";
                case "icon_map_normal": return "\u6218";
                case "icon_map_elite": return "\u7cbe";
                case "icon_map_event": return "?";
                case "icon_map_enhance": return "\u953b";
                case "icon_map_rest": return "\u606f";
                case "icon_map_boss": return "\u738b";
                default:
                    switch (fallbackType)
                    {
                        case RunNodeType.Shop: return "\u5546";
                        case RunNodeType.Normal: return "\u6218";
                        case RunNodeType.Elite: return "\u7cbe";
                        case RunNodeType.Event: return "?";
                        case RunNodeType.Enhance: return "\u953b";
                        case RunNodeType.Rest: return "\u606f";
                        case RunNodeType.Boss: return "\u738b";
                        default: return "\u00b7";
                    }
            }
        }
    }
}

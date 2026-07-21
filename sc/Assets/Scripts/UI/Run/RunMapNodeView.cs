using SpireChess.Run;
using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.UI.Run
{
    [DisallowMultipleComponent]
    public sealed class RunMapNodeView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Outline outline;
        [SerializeField] private Button button;
        [SerializeField] private Text routeText;
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Text statusText;

        private RunTestController controller;
        private string nodeId;

        public string NodeId => nodeId;
        public bool HasCompleteBindings =>
            background != null && outline != null && button != null &&
            routeText != null && titleText != null && subtitleText != null &&
            statusText != null;

        public void Bind(RunTestController value)
        {
            controller = value;
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        public void Render(RunMapNodeState state)
        {
            nodeId = state.NodeId;
            routeText.text = state.RouteText ?? string.Empty;
            routeText.gameObject.SetActive(!string.IsNullOrWhiteSpace(state.RouteText));
            titleText.text = state.Title ?? string.Empty;
            subtitleText.text = state.Subtitle ?? string.Empty;
            statusText.text = ToStatusText(state.Status);
            button.interactable = state.IsInteractable;
            background.color = ResolveColor(state.Type, state.Status);
            outline.effectColor = state.Status == RunNodeStatus.Current
                ? new Color(1f, 0.78f, 0.20f, 1f)
                : state.Status == RunNodeStatus.Reachable
                    ? new Color(0.34f, 0.92f, 0.72f, 1f)
                    : new Color(1f, 1f, 1f, 0.12f);
            outline.effectDistance = state.Status == RunNodeStatus.Locked
                ? new Vector2(1f, -1f)
                : new Vector2(3f, -3f);
        }

        private void HandleClick()
        {
            if (controller != null && !string.IsNullOrWhiteSpace(nodeId))
            {
                controller.EnterNode(nodeId);
            }
        }

        private static string ToStatusText(RunNodeStatus status)
        {
            switch (status)
            {
                case RunNodeStatus.Reachable: return "可进入";
                case RunNodeStatus.Current: return "当前";
                case RunNodeStatus.Resolved: return "已完成";
                default: return "未解锁";
            }
        }

        private static Color ResolveColor(RunNodeType type, RunNodeStatus status)
        {
            if (status == RunNodeStatus.Locked)
            {
                return new Color(0.12f, 0.14f, 0.18f, 0.92f);
            }
            var color = type == RunNodeType.Shop
                ? new Color(0.12f, 0.30f, 0.38f, 0.98f)
                : type == RunNodeType.Normal
                    ? new Color(0.32f, 0.18f, 0.20f, 0.98f)
                    : type == RunNodeType.Elite
                        ? new Color(0.46f, 0.20f, 0.12f, 0.98f)
                        : type == RunNodeType.Boss
                            ? new Color(0.48f, 0.12f, 0.18f, 0.98f)
                            : type == RunNodeType.Enhance
                                ? new Color(0.30f, 0.24f, 0.10f, 0.98f)
                                : type == RunNodeType.Rest
                                    ? new Color(0.12f, 0.32f, 0.22f, 0.98f)
                                    : new Color(0.24f, 0.18f, 0.36f, 0.98f);
            if (status == RunNodeStatus.Resolved)
            {
                color *= 0.58f;
                color.a = 0.96f;
            }
            return color;
        }
    }
}

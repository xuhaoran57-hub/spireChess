using UnityEngine;
using SpireChess.Run;
using SpireChess.UI.Run;

namespace SpireChess.UI
{
    [CreateAssetMenu(
        fileName = "PresentationTheme",
        menuName = "Spire Chess/Presentation/Theme")]
    public sealed class PresentationTheme : ScriptableObject
    {
        [Header("Battle standee")]
        [SerializeField] private Color forgeSoulPortraitTint =
            new Color(0.46f, 0.22f, 0.14f, 1f);
        [SerializeField] private Color wildSpiritPortraitTint =
            new Color(0.20f, 0.42f, 0.24f, 1f);
        [SerializeField] private Color starboundPortraitTint =
            new Color(0.20f, 0.30f, 0.56f, 1f);
        [SerializeField] private Color wayfarerPortraitTint =
            new Color(0.42f, 0.34f, 0.22f, 1f);
        [SerializeField] private Color fallbackPortraitTint =
            new Color(0.30f, 0.27f, 0.33f, 1f);
        [SerializeField] private Color normalFrameTint = Color.white;
        [SerializeField] private Color goldenFrameTint =
            new Color(1f, 0.90f, 0.62f, 1f);
        [SerializeField] private Color legalTargetTint =
            new Color(0.38f, 0.82f, 0.58f, 0.78f);
        [SerializeField] private Color selectedTargetTint =
            new Color(0.98f, 0.68f, 0.22f, 0.88f);

        [Header("Cross-screen surfaces")]
        [SerializeField] private Color screenBackground =
            new Color(0.022f, 0.027f, 0.045f, 1f);
        [SerializeField] private Color panelBackground =
            new Color(0.055f, 0.066f, 0.095f, 0.98f);
        [SerializeField] private Color panelRaised =
            new Color(0.082f, 0.098f, 0.135f, 1f);
        [SerializeField] private Color panelBorder =
            new Color(0.56f, 0.46f, 0.30f, 0.72f);
        [SerializeField] private Color buttonNormal =
            new Color(0.14f, 0.22f, 0.29f, 1f);
        [SerializeField] private Color buttonHighlighted =
            new Color(0.20f, 0.34f, 0.41f, 1f);
        [SerializeField] private Color buttonPressed =
            new Color(0.09f, 0.15f, 0.21f, 1f);
        [SerializeField] private Color buttonDisabled =
            new Color(0.08f, 0.09f, 0.12f, 0.88f);
        [SerializeField] private Color textPrimary =
            new Color(0.94f, 0.91f, 0.82f, 1f);
        [SerializeField] private Color textSecondary =
            new Color(0.64f, 0.69f, 0.73f, 1f);
        [SerializeField] private Color accent =
            new Color(0.94f, 0.66f, 0.24f, 1f);
        [SerializeField] private Color success =
            new Color(0.36f, 0.82f, 0.62f, 1f);
        [SerializeField] private Color danger =
            new Color(0.82f, 0.29f, 0.27f, 1f);
        [SerializeField] private Color modalScrim =
            new Color(0.005f, 0.008f, 0.014f, 0.84f);

        [Header("Run map surfaces")]
        [SerializeField] private Color mapCanvasBackground =
            new Color(0.026f, 0.034f, 0.050f, 0.98f);
        [SerializeField] private Color mapDecorationTint =
            new Color(0.64f, 0.48f, 0.26f, 0.18f);

        [Header("Run map node types")]
        [SerializeField] private Color mapShopTint =
            new Color(0.18f, 0.49f, 0.56f, 1f);
        [SerializeField] private Color mapNormalTint =
            new Color(0.54f, 0.27f, 0.27f, 1f);
        [SerializeField] private Color mapEliteTint =
            new Color(0.72f, 0.34f, 0.16f, 1f);
        [SerializeField] private Color mapEventTint =
            new Color(0.43f, 0.31f, 0.61f, 1f);
        [SerializeField] private Color mapEnhanceTint =
            new Color(0.58f, 0.46f, 0.16f, 1f);
        [SerializeField] private Color mapRestTint =
            new Color(0.22f, 0.51f, 0.35f, 1f);
        [SerializeField] private Color mapBossTint =
            new Color(0.72f, 0.16f, 0.22f, 1f);

        [Header("Run map presentation states")]
        [SerializeField] private Color mapLockedTint =
            new Color(0.24f, 0.27f, 0.31f, 0.82f);
        [SerializeField] private Color mapReachableTint =
            new Color(0.37f, 0.86f, 0.72f, 1f);
        [SerializeField] private Color mapCurrentTint =
            new Color(1f, 0.76f, 0.23f, 1f);
        [SerializeField] private Color mapResolvedTint =
            new Color(0.47f, 0.60f, 0.55f, 0.88f);
        [SerializeField] private Color mapAbandonedTint =
            new Color(0.34f, 0.20f, 0.22f, 0.78f);

        [Header("Run map edges")]
        [SerializeField] private Color mapEdgeLocked =
            new Color(0.50f, 0.54f, 0.60f, 0.17f);
        [SerializeField] private Color mapEdgeReachable =
            new Color(0.28f, 0.70f, 0.82f, 0.88f);
        [SerializeField] private Color mapEdgeResolved =
            new Color(0.38f, 0.84f, 0.64f, 0.88f);
        [SerializeField] private Color mapEdgeAbandoned =
            new Color(0.66f, 0.30f, 0.31f, 0.38f);

        public Color NormalFrameTint => normalFrameTint;
        public Color GoldenFrameTint => goldenFrameTint;
        public Color LegalTargetTint => legalTargetTint;
        public Color SelectedTargetTint => selectedTargetTint;
        public Color ScreenBackground => screenBackground;
        public Color PanelBackground => panelBackground;
        public Color PanelRaised => panelRaised;
        public Color PanelBorder => panelBorder;
        public Color ButtonNormal => buttonNormal;
        public Color ButtonHighlighted => buttonHighlighted;
        public Color ButtonPressed => buttonPressed;
        public Color ButtonDisabled => buttonDisabled;
        public Color TextPrimary => textPrimary;
        public Color TextSecondary => textSecondary;
        public Color Accent => accent;
        public Color Success => success;
        public Color Danger => danger;
        public Color ModalScrim => modalScrim;
        public Color MapCanvasBackground => mapCanvasBackground;
        public Color MapDecorationTint => mapDecorationTint;

        public Color GetPortraitTint(string raceText)
        {
            switch (raceText)
            {
                case "铸魂": return forgeSoulPortraitTint;
                case "荒灵": return wildSpiritPortraitTint;
                case "星契": return starboundPortraitTint;
                case "旅团": return wayfarerPortraitTint;
                default: return fallbackPortraitTint;
            }
        }

        public Color GetMapTypeColor(RunNodeType type)
        {
            switch (type)
            {
                case RunNodeType.Shop: return mapShopTint;
                case RunNodeType.Normal: return mapNormalTint;
                case RunNodeType.Elite: return mapEliteTint;
                case RunNodeType.Event: return mapEventTint;
                case RunNodeType.Enhance: return mapEnhanceTint;
                case RunNodeType.Rest: return mapRestTint;
                case RunNodeType.Boss: return mapBossTint;
                default: return panelRaised;
            }
        }

        public Color GetMapStatusColor(RunMapPresentationStatus status)
        {
            switch (status)
            {
                case RunMapPresentationStatus.Reachable: return mapReachableTint;
                case RunMapPresentationStatus.Current: return mapCurrentTint;
                case RunMapPresentationStatus.Resolved: return mapResolvedTint;
                case RunMapPresentationStatus.Abandoned: return mapAbandonedTint;
                default: return mapLockedTint;
            }
        }

        public Color GetMapNodeColor(
            RunNodeType type,
            RunMapPresentationStatus status)
        {
            var typeColor = GetMapTypeColor(type);
            switch (status)
            {
                case RunMapPresentationStatus.Reachable:
                    return Color.Lerp(typeColor, mapReachableTint, 0.16f);
                case RunMapPresentationStatus.Current:
                    return Color.Lerp(typeColor, mapCurrentTint, 0.24f);
                case RunMapPresentationStatus.Resolved:
                    return Color.Lerp(panelBackground, typeColor, 0.43f);
                case RunMapPresentationStatus.Abandoned:
                    return Color.Lerp(mapAbandonedTint, typeColor, 0.10f);
                default:
                    return Color.Lerp(mapLockedTint, typeColor, 0.10f);
            }
        }

        public Color GetMapStatusOverlayColor(RunMapPresentationStatus status)
        {
            var color = GetMapStatusColor(status);
            color.a = status == RunMapPresentationStatus.Current
                ? 0.16f
                : status == RunMapPresentationStatus.Reachable
                    ? 0.09f
                    : status == RunMapPresentationStatus.Abandoned
                        ? 0.30f
                        : status == RunMapPresentationStatus.Locked
                            ? 0.22f
                            : 0.10f;
            return color;
        }

        public Color GetMapEdgeColor(RunMapEdgePresentationStatus status)
        {
            switch (status)
            {
                case RunMapEdgePresentationStatus.Reachable:
                    return mapEdgeReachable;
                case RunMapEdgePresentationStatus.Resolved:
                    return mapEdgeResolved;
                case RunMapEdgePresentationStatus.Abandoned:
                    return mapEdgeAbandoned;
                default:
                    return mapEdgeLocked;
            }
        }
    }
}

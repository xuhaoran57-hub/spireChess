using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.UI.Run
{
    public enum RunMapViewportSegment
    {
        Left,
        Center,
        Right
    }

    public sealed class RunMapViewportSnapshot
    {
        internal RunMapViewportSnapshot(
            float horizontalNormalizedPosition,
            Rect viewportBounds,
            Rect contentBoundsInViewport,
            IReadOnlyList<string> intersectingNodeIds,
            IReadOnlyList<string> fullyVisibleNodeIds)
        {
            HorizontalNormalizedPosition = horizontalNormalizedPosition;
            ViewportBounds = viewportBounds;
            ContentBoundsInViewport = contentBoundsInViewport;
            IntersectingNodeIds = intersectingNodeIds;
            FullyVisibleNodeIds = fullyVisibleNodeIds;
        }

        public float HorizontalNormalizedPosition { get; }
        public Rect ViewportBounds { get; }
        public Rect ContentBoundsInViewport { get; }
        public IReadOnlyList<string> IntersectingNodeIds { get; }
        public IReadOnlyList<string> FullyVisibleNodeIds { get; }
        public IReadOnlyList<string> VisibleNodeIds => FullyVisibleNodeIds;
    }

    [DisallowMultipleComponent]
    public sealed class RunScreenView : MonoBehaviour
    {
        private const float NodeStartX = 120f;
        private const float NodeColumnGap = 180f;
        private const float MapViewportVisibilityTolerance = 0.1f;

        [Header("Root")]
        [SerializeField] private PresentationTheme theme;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private RectTransform safeArea;

        [Header("Top bar")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text resourceText;
        [SerializeField] private Text progressText;
        [SerializeField] private Text statusText;

        [Header("Map")]
        [SerializeField] private Text routeHintText;
        [SerializeField] private ScrollRect mapScrollRect;
        [SerializeField] private RectTransform mapContent;
        [SerializeField] private Image mapBackdrop;
        [SerializeField] private RectTransform edgeLayer;
        [SerializeField] private RectTransform nodeLayer;
        [SerializeField] private GameObject mapNodePrefab;
        [SerializeField] private GameObject mapEdgePrefab;

        [Header("Relics")]
        [SerializeField] private Text relicCountText;
        [SerializeField] private Text relicEmptyText;
        [SerializeField] private ScrollRect relicScrollRect;
        [SerializeField] private RectTransform relicContent;
        [SerializeField] private GameObject relicEntryPrefab;

        [Header("Summary")]
        [SerializeField] private Text summaryText;
        [SerializeField] private Button summaryActionButton;
        [SerializeField] private Text summaryActionText;

        [Header("Choice overlay")]
        [SerializeField] private GameObject choiceOverlay;
        [SerializeField] private Text choiceTitleText;
        [SerializeField] private Text choiceDescriptionText;
        [SerializeField] private ScrollRect choiceScrollRect;
        [SerializeField] private RectTransform choiceContent;
        [SerializeField] private GameObject choiceOptionPrefab;

        private readonly Dictionary<string, RunMapNodeView> nodeViews =
            new Dictionary<string, RunMapNodeView>(StringComparer.Ordinal);
        private RunTestController controller;

        public int RenderedNodeCount { get; private set; }
        public int RenderedEdgeCount { get; private set; }
        public int RenderedRelicCount { get; private set; }
        public int RenderedChoiceCount { get; private set; }
        public bool IsChoiceVisible => choiceOverlay != null && choiceOverlay.activeSelf;
        public float MapHorizontalNormalizedPosition =>
            mapScrollRect == null
                ? 0f
                : mapScrollRect.horizontalNormalizedPosition;
        public bool HasCompleteBindings =>
            theme != null && rootCanvas != null && safeArea != null &&
            titleText != null && resourceText != null && progressText != null &&
            statusText != null && routeHintText != null &&
            mapScrollRect != null && mapContent != null && mapBackdrop != null &&
            mapScrollRect.viewport != null && mapScrollRect.content == mapContent &&
            edgeLayer != null &&
            nodeLayer != null && mapNodePrefab != null && mapEdgePrefab != null &&
            relicCountText != null && relicEmptyText != null &&
            relicScrollRect != null && relicContent != null && relicEntryPrefab != null &&
            summaryText != null && summaryActionButton != null && summaryActionText != null &&
            choiceOverlay != null && choiceTitleText != null &&
            choiceDescriptionText != null && choiceScrollRect != null &&
            choiceContent != null && choiceOptionPrefab != null;

        public void Bind(RunTestController value)
        {
            controller = value ?? throw new ArgumentNullException(nameof(value));
        }

        public void Render(RunScreenState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }
            if (!HasCompleteBindings)
            {
                throw new InvalidOperationException(
                    "RunScreenView has missing serialized bindings.");
            }

            titleText.text = state.Title ?? string.Empty;
            resourceText.text = state.ResourceSummary ?? string.Empty;
            progressText.text = state.ProgressSummary ?? string.Empty;
            statusText.text = state.Status ?? string.Empty;
            var nodeCount = state.Nodes?.Count ?? 0;
            routeHintText.text = nodeCount > 0
                ? $"共 {nodeCount} 个节点 · 左右拖动查看完整路线 · {state.RouteHint}"
                : state.RouteHint ?? string.Empty;
            RenderMap(state);
            RenderRelics(state.Relics);
            RenderSummary(state.Summary);
            RenderChoice(state.Choice);
            Canvas.ForceUpdateCanvases();
        }

        public RunMapNodeView FindNode(string nodeId)
        {
            nodeViews.TryGetValue(nodeId ?? string.Empty, out var view);
            return view;
        }

        public RunMapViewportSnapshot SetMapViewportSegment(
            RunMapViewportSegment segment)
        {
            switch (segment)
            {
                case RunMapViewportSegment.Left:
                    return SetMapViewportNormalizedPosition(0f);
                case RunMapViewportSegment.Center:
                    return SetMapViewportNormalizedPosition(0.5f);
                case RunMapViewportSegment.Right:
                    return SetMapViewportNormalizedPosition(1f);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(segment),
                        segment,
                        "Unknown run map viewport segment.");
            }
        }

        public RunMapViewportSnapshot SetMapViewportNormalizedPosition(
            float normalizedPosition)
        {
            EnsureMapViewportBindings();
            ForceMapLayout();
            mapScrollRect.StopMovement();
            mapScrollRect.horizontalNormalizedPosition =
                Mathf.Clamp01(normalizedPosition);
            ForceMapLayout();
            return CaptureMapViewportWithoutLayoutRefresh();
        }

        public RunMapViewportSnapshot CaptureMapViewport()
        {
            EnsureMapViewportBindings();
            ForceMapLayout();
            return CaptureMapViewportWithoutLayoutRefresh();
        }

        public void SetChoiceViewportNormalizedPosition(
            float normalizedPosition)
        {
            if (choiceScrollRect == null ||
                choiceScrollRect.viewport == null ||
                choiceScrollRect.content == null)
            {
                throw new InvalidOperationException(
                    "Run choice viewport bindings are incomplete.");
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                choiceScrollRect.content);
            choiceScrollRect.StopMovement();
            choiceScrollRect.verticalNormalizedPosition =
                Mathf.Clamp01(normalizedPosition);
            Canvas.ForceUpdateCanvases();
        }

        private void RenderMap(RunScreenState state)
        {
            DestroyChildren(edgeLayer);
            DestroyChildren(nodeLayer);
            nodeViews.Clear();

            var maximumColumn = Math.Max(1, state.MaximumColumn);
            var width = Math.Max(1900f, NodeStartX * 2f + maximumColumn * NodeColumnGap);
            mapContent.sizeDelta = new Vector2(width, 620f);
            mapBackdrop.color = theme.MapCanvasBackground;
            var positions = new Dictionary<string, Vector2>(StringComparer.Ordinal);
            foreach (var node in state.Nodes ?? Array.Empty<RunMapNodeState>())
            {
                var instance = Instantiate(mapNodePrefab, nodeLayer);
                instance.name = "Node_" + node.NodeId;
                var rect = instance.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                var position = new Vector2(
                    NodeStartX + node.Column * NodeColumnGap,
                    node.Row < 0 ? 450f : node.Row > 0 ? 170f : 310f);
                rect.anchoredPosition = position;
                positions[node.NodeId] = position;
                var nodeView = instance.GetComponent<RunMapNodeView>();
                nodeView.Bind(controller);
                nodeView.Render(node);
                nodeViews[node.NodeId] = nodeView;
            }

            foreach (var edge in state.Edges ?? Array.Empty<RunMapEdgeState>())
            {
                if (!positions.TryGetValue(edge.FromNodeId, out var from) ||
                    !positions.TryGetValue(edge.ToNodeId, out var to))
                {
                    continue;
                }
                var instance = Instantiate(mapEdgePrefab, edgeLayer);
                instance.name = $"Edge_{edge.FromNodeId}_{edge.ToNodeId}";
                var rect = instance.GetComponent<RectTransform>();
                var delta = to - from;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = (from + to) * 0.5f;
                rect.sizeDelta = new Vector2(
                    delta.magnitude,
                    ResolveEdgeThickness(edge.PresentationStatus));
                rect.localEulerAngles = new Vector3(
                    0f,
                    0f,
                    Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                instance.GetComponent<Image>().color =
                    theme.GetMapEdgeColor(edge.PresentationStatus);
            }

            RenderedNodeCount = nodeViews.Count;
            RenderedEdgeCount = edgeLayer.childCount;
            var focus = (state.Nodes ?? Array.Empty<RunMapNodeState>())
                .FirstOrDefault(node =>
                    node.PresentationStatus == RunMapPresentationStatus.Current) ??
                        (state.Nodes ?? Array.Empty<RunMapNodeState>())
                .FirstOrDefault(node =>
                    node.PresentationStatus == RunMapPresentationStatus.Reachable);
            if (focus != null && positions.TryGetValue(focus.NodeId, out var focusPosition))
            {
                Canvas.ForceUpdateCanvases();
                var viewportWidth = mapScrollRect.viewport.rect.width;
                var scrollableWidth = Mathf.Max(
                    0f,
                    mapContent.rect.width - viewportWidth);
                var focusOffset = Mathf.Clamp(
                    focusPosition.x - viewportWidth * 0.5f,
                    0f,
                    scrollableWidth);
                mapScrollRect.horizontalNormalizedPosition =
                    scrollableWidth <= 0.01f
                        ? 0f
                        : focusOffset / scrollableWidth;
            }
        }

        private void RenderRelics(IReadOnlyList<RunRelicState> relics)
        {
            DestroyChildren(relicContent);
            relics = relics ?? Array.Empty<RunRelicState>();
            relicCountText.text = $"遗珍 {relics.Count}";
            relicEmptyText.gameObject.SetActive(relics.Count == 0);
            foreach (var relic in relics)
            {
                var instance = Instantiate(relicEntryPrefab, relicContent);
                instance.name = "Relic_" + relic.RelicId;
                instance.GetComponent<RunRelicEntryView>().Render(relic);
            }
            RenderedRelicCount = relics.Count;
            relicScrollRect.verticalNormalizedPosition = 1f;
        }

        private void RenderSummary(RunSummaryState summary)
        {
            summary = summary ?? new RunSummaryState();
            summaryText.text = summary.Text ?? string.Empty;
            summaryActionButton.gameObject.SetActive(summary.IsActionVisible);
            summaryActionButton.interactable = summary.IsActionVisible;
            summaryActionText.text = summary.ActionLabel ?? string.Empty;
            summaryActionButton.onClick.RemoveAllListeners();
            if (summary.IsActionVisible)
            {
                var action = summary.Action;
                summaryActionButton.onClick.AddListener(() =>
                    controller?.ExecuteUiAction(action));
            }
        }

        private void RenderChoice(RunChoiceOverlayState choice)
        {
            DestroyChildren(choiceContent);
            choiceOverlay.SetActive(choice != null);
            if (choice == null)
            {
                RenderedChoiceCount = 0;
                return;
            }

            choiceTitleText.text = choice.Title ?? string.Empty;
            choiceDescriptionText.text = choice.Description ?? string.Empty;
            foreach (var option in choice.Options ?? Array.Empty<RunChoiceOptionState>())
            {
                var instance = Instantiate(choiceOptionPrefab, choiceContent);
                instance.name = "Choice_" + option.Action;
                var view = instance.GetComponent<RunChoiceOptionView>();
                view.Bind(controller);
                view.Render(option);
            }
            RenderedChoiceCount = choice.Options?.Count ?? 0;
            choiceScrollRect.verticalNormalizedPosition = 1f;
        }

        private static float ResolveEdgeThickness(
            RunMapEdgePresentationStatus status)
        {
            switch (status)
            {
                case RunMapEdgePresentationStatus.Reachable:
                    return 5f;
                case RunMapEdgePresentationStatus.Resolved:
                    return 7f;
                case RunMapEdgePresentationStatus.Abandoned:
                    return 3f;
                default:
                    return 2f;
            }
        }

        private RunMapViewportSnapshot CaptureMapViewportWithoutLayoutRefresh()
        {
            var viewport = mapScrollRect.viewport;
            var viewportBounds = viewport.rect;
            var contentBounds = CalculateBoundsIn(viewport, mapContent);
            var nodeBounds = nodeViews
                .Where(pair =>
                    pair.Value != null &&
                    pair.Value.gameObject.activeInHierarchy)
                .Select(pair => new
                {
                    pair.Key,
                    Bounds = CalculateBoundsIn(
                        viewport,
                        pair.Value.GetComponent<RectTransform>()),
                    X = pair.Value.GetComponent<RectTransform>()
                        .anchoredPosition.x
                })
                .OrderBy(pair => pair.X)
                .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                .ToArray();
            var intersectingNodeIds = nodeBounds
                .Where(pair => IntersectsWithTolerance(
                    viewportBounds,
                    pair.Bounds))
                .Select(pair => pair.Key)
                .ToArray();
            var fullyVisibleNodeIds = nodeBounds
                .Where(pair => ContainsWithTolerance(
                    viewportBounds,
                    pair.Bounds))
                .Select(pair => pair.Key)
                .ToArray();
            return new RunMapViewportSnapshot(
                mapScrollRect.horizontalNormalizedPosition,
                viewportBounds,
                contentBounds,
                Array.AsReadOnly(intersectingNodeIds),
                Array.AsReadOnly(fullyVisibleNodeIds));
        }

        private void EnsureMapViewportBindings()
        {
            if (mapScrollRect == null ||
                mapScrollRect.viewport == null ||
                mapScrollRect.content != mapContent)
            {
                throw new InvalidOperationException(
                    "Run map viewport bindings are incomplete.");
            }
        }

        private void ForceMapLayout()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                (RectTransform)mapScrollRect.transform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(mapContent);
            mapScrollRect.Rebuild(CanvasUpdate.PostLayout);
            Canvas.ForceUpdateCanvases();
        }

        private static Rect CalculateBoundsIn(
            RectTransform relativeTo,
            RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            var first = relativeTo.InverseTransformPoint(corners[0]);
            var xMin = first.x;
            var xMax = first.x;
            var yMin = first.y;
            var yMax = first.y;
            for (var index = 1; index < corners.Length; index++)
            {
                var point = relativeTo.InverseTransformPoint(corners[index]);
                xMin = Mathf.Min(xMin, point.x);
                xMax = Mathf.Max(xMax, point.x);
                yMin = Mathf.Min(yMin, point.y);
                yMax = Mathf.Max(yMax, point.y);
            }
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static bool IntersectsWithTolerance(Rect container, Rect target)
        {
            return target.xMax >= container.xMin - MapViewportVisibilityTolerance &&
                   target.xMin <= container.xMax + MapViewportVisibilityTolerance &&
                   target.yMax >= container.yMin - MapViewportVisibilityTolerance &&
                   target.yMin <= container.yMax + MapViewportVisibilityTolerance;
        }

        private static bool ContainsWithTolerance(Rect container, Rect target)
        {
            return target.xMin >= container.xMin - MapViewportVisibilityTolerance &&
                   target.xMax <= container.xMax + MapViewportVisibilityTolerance &&
                   target.yMin >= container.yMin - MapViewportVisibilityTolerance &&
                   target.yMax <= container.yMax + MapViewportVisibilityTolerance;
        }

        private static void DestroyChildren(Transform root)
        {
            for (var index = root.childCount - 1; index >= 0; index--)
            {
                var child = root.GetChild(index).gameObject;
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
}

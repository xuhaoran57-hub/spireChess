using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.UI.Run
{
    [DisallowMultipleComponent]
    public sealed class RunScreenView : MonoBehaviour
    {
        private const float NodeStartX = 120f;
        private const float NodeColumnGap = 180f;

        [Header("Root")]
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
        public bool HasCompleteBindings =>
            rootCanvas != null && safeArea != null &&
            titleText != null && resourceText != null && progressText != null &&
            statusText != null && routeHintText != null &&
            mapScrollRect != null && mapContent != null && edgeLayer != null &&
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
            routeHintText.text = state.RouteHint ?? string.Empty;
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

        private void RenderMap(RunScreenState state)
        {
            DestroyChildren(edgeLayer);
            DestroyChildren(nodeLayer);
            nodeViews.Clear();

            var maximumColumn = Math.Max(1, state.MaximumColumn);
            var width = Math.Max(1900f, NodeStartX * 2f + maximumColumn * NodeColumnGap);
            mapContent.sizeDelta = new Vector2(width, 620f);
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
                rect.sizeDelta = new Vector2(delta.magnitude, 4f);
                rect.localEulerAngles = new Vector3(
                    0f,
                    0f,
                    Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                instance.GetComponent<Image>().color = ResolveEdgeColor(edge);
            }

            RenderedNodeCount = nodeViews.Count;
            RenderedEdgeCount = edgeLayer.childCount;
            var focus = (state.Nodes ?? Array.Empty<RunMapNodeState>())
                .FirstOrDefault(node => node.Status == SpireChess.Run.RunNodeStatus.Current) ??
                        (state.Nodes ?? Array.Empty<RunMapNodeState>())
                .FirstOrDefault(node => node.Status == SpireChess.Run.RunNodeStatus.Reachable);
            if (focus != null)
            {
                mapScrollRect.horizontalNormalizedPosition =
                    Mathf.Clamp01((float)focus.Column / maximumColumn);
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

        private static Color ResolveEdgeColor(RunMapEdgeState edge)
        {
            if (edge.FromStatus == SpireChess.Run.RunNodeStatus.Resolved &&
                (edge.ToStatus == SpireChess.Run.RunNodeStatus.Resolved ||
                 edge.ToStatus == SpireChess.Run.RunNodeStatus.Current ||
                 edge.ToStatus == SpireChess.Run.RunNodeStatus.Reachable))
            {
                return new Color(0.34f, 0.84f, 0.68f, 0.88f);
            }
            if (edge.ToStatus == SpireChess.Run.RunNodeStatus.Reachable)
            {
                return new Color(0.26f, 0.68f, 0.82f, 0.82f);
            }
            return new Color(1f, 1f, 1f, 0.12f);
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

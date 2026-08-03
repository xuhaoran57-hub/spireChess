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
        [SerializeField] private bool suppressProductionBackdrop;
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

        private GameObject journalPageOverlay;
        private Text journalPageTitleText;
        private Text journalPageBodyText;
        private Text journalPageUnlockText;
        private Image journalPageArtwork;
        private Button journalPageActionButton;
        private Text journalPageActionText;
        private readonly Dictionary<string, RunMapNodeView> nodeViews =
            new Dictionary<string, RunMapNodeView>(StringComparer.Ordinal);
        private RunTestController controller;
        private Image choiceArtworkImage;

        public int RenderedNodeCount { get; private set; }
        public int RenderedEdgeCount { get; private set; }
        public int RenderedRelicCount { get; private set; }
        public int RenderedChoiceCount { get; private set; }
        public bool IsChoiceVisible => choiceOverlay != null && choiceOverlay.activeSelf;
        public bool IsJournalPageVisible => journalPageOverlay != null &&
                                            journalPageOverlay.activeSelf;
        public bool IsChoiceArtworkVisible =>
            choiceArtworkImage != null &&
            choiceArtworkImage.gameObject.activeInHierarchy &&
            choiceArtworkImage.sprite != null;
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
            RenderJournalPage(state.JournalPage);
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
            var productionBackdrop = suppressProductionBackdrop
                ? null
                : PresentationArtworkResources.LoadBackdrop(
                    PresentationBackdropVariant.RunMap);
            mapBackdrop.sprite = productionBackdrop;
            mapBackdrop.type = Image.Type.Simple;
            mapBackdrop.preserveAspect = false;
            mapBackdrop.color = productionBackdrop == null
                ? theme.MapCanvasBackground
                : new Color(0.52f, 0.55f, 0.52f, 0.84f);
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
                SetChoiceArtwork(null);
                RenderedChoiceCount = 0;
                return;
            }

            choiceTitleText.text = choice.Title ?? string.Empty;
            choiceDescriptionText.text = choice.Description ?? string.Empty;
            SetChoiceArtwork(choice.ArtworkId);
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

        public void EnsureJournalPageOverlay()
        {
            if (journalPageOverlay != null)
            {
                return;
            }

            var journalParent = safeArea == null
                ? transform
                : (Transform)safeArea;
            var existing = journalParent.Find("JournalPageOverlay");
            if (existing != null)
            {
                journalPageOverlay = existing.gameObject;
                journalPageTitleText = FindJournalText(existing, "JournalPage/Title");
                journalPageBodyText = FindJournalText(existing, "JournalPage/Body");
                journalPageUnlockText = FindJournalText(existing, "JournalPage/UnlockNotice");
                var artwork = existing.Find("JournalPage/NeutralJournalArtwork");
                journalPageArtwork = artwork == null
                    ? null
                    : artwork.GetComponent<Image>();
                var action = existing.Find("JournalPage/JournalActionButton") ??
                             existing.Find("JournalPage/ActionButton");
                journalPageActionButton = action == null
                    ? null
                    : action.GetComponent<Button>();
                journalPageActionText = action == null
                    ? null
                    : action.Find("Label")?.GetComponent<Text>();
                return;
            }

            var font = titleText == null
                ? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                : titleText.font;
            var overlay = CreateJournalImage(
                journalParent,
                "JournalPageOverlay",
                theme.ModalScrim);
            StretchJournal(overlay.rectTransform);
            var page = CreateJournalImage(
                overlay.transform,
                "JournalPage",
                theme.PanelBackground);
            SetJournalRect(
                page.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(1180f, 740f),
                Vector2.zero);
            AddJournalOutline(
                page,
                new Color(0.74f, 0.61f, 0.32f, 0.88f));

            journalPageArtwork = CreateJournalImage(
                page.transform,
                "NeutralJournalArtwork",
                new Color(0.30f, 0.34f, 0.34f, 1f));
            SetJournalRect(
                journalPageArtwork.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(500f, 180f),
                new Vector2(0f, -124f));
            AddJournalOutline(
                journalPageArtwork,
                new Color(0.82f, 0.71f, 0.44f, 0.66f));
            var artworkText = CreateJournalText(
                journalPageArtwork.transform,
                "Label",
                "中性章节插图占位",
                font,
                22,
                FontStyle.Bold);
            StretchJournal(artworkText.rectTransform);

            journalPageTitleText = CreateJournalText(
                page.transform,
                "Title",
                string.Empty,
                font,
                36,
                FontStyle.Bold);
            SetJournalRect(
                journalPageTitleText.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(1000f, 62f),
                new Vector2(0f, -252f));
            journalPageTitleText.color = theme.TextPrimary;

            journalPageBodyText = CreateJournalText(
                page.transform,
                "Body",
                string.Empty,
                font,
                22,
                FontStyle.Normal);
            SetJournalRect(
                journalPageBodyText.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(960f, 156f),
                new Vector2(0f, 24f));
            journalPageBodyText.alignment = TextAnchor.MiddleCenter;
            journalPageBodyText.color = theme.TextSecondary;

            journalPageUnlockText = CreateJournalText(
                page.transform,
                "UnlockNotice",
                string.Empty,
                font,
                24,
                FontStyle.Bold);
            SetJournalRect(
                journalPageUnlockText.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(900f, 60f),
                new Vector2(0f, -116f));
            journalPageUnlockText.color = theme.Success;

            var actionImage = CreateJournalImage(
                page.transform,
                "JournalActionButton",
                theme.ButtonNormal);
            SetJournalRect(
                actionImage.rectTransform,
                new Vector2(0.5f, 0f),
                new Vector2(350f, 74f),
                new Vector2(0f, 72f));
            AddJournalOutline(
                actionImage,
                new Color(0.74f, 0.61f, 0.32f, 0.82f));
            journalPageActionButton = actionImage.gameObject.AddComponent<Button>();
            journalPageActionButton.targetGraphic = actionImage;
            journalPageActionText = CreateJournalText(
                actionImage.transform,
                "Label",
                string.Empty,
                font,
                24,
                FontStyle.Bold);
            StretchJournal(journalPageActionText.rectTransform);
            journalPageActionText.color = theme.TextPrimary;
            journalPageOverlay = overlay.gameObject;
            journalPageOverlay.SetActive(false);
        }

        private void RenderJournalPage(RunJournalPageState page)
        {
            EnsureJournalPageOverlay();
            var isVisible = page != null &&
                            page.Kind != RunJournalPageKind.None;
            journalPageOverlay.SetActive(isVisible);
            if (!isVisible)
            {
                // Remove the old action before hiding the overlay. This makes
                // programmatic/queued duplicate clicks harmless as well as
                // preventing a stale page action from being retained between
                // chapter and map renders.
                journalPageActionButton.onClick.RemoveAllListeners();
                journalPageActionButton.interactable = false;
                return;
            }

            journalPageOverlay.transform.SetAsLastSibling();
            journalPageTitleText.text = page.Title ?? string.Empty;
            journalPageBodyText.text = page.Body ?? string.Empty;
            journalPageUnlockText.text = page.UnlockNotification ?? string.Empty;
            journalPageUnlockText.gameObject.SetActive(
                !string.IsNullOrWhiteSpace(page.UnlockNotification));
            var artworkSprite = page.Kind == RunJournalPageKind.Ending
                ? PresentationArtworkResources.LoadJournalEnding()
                : PresentationArtworkResources.LoadJournalChapter(
                    page.ArtworkId);
            journalPageArtwork.sprite = artworkSprite;
            journalPageArtwork.type = Image.Type.Simple;
            journalPageArtwork.preserveAspect = false;
            journalPageArtwork.color = artworkSprite == null
                ? page.Kind == RunJournalPageKind.Ending
                    ? new Color(0.38f, 0.31f, 0.24f, 1f)
                    : new Color(0.30f, 0.34f, 0.34f, 1f)
                : Color.white;
            var artworkLabel = journalPageArtwork.transform.Find("Label");
            if (artworkLabel != null)
            {
                artworkLabel.gameObject.SetActive(artworkSprite == null);
            }
            journalPageActionText.text = page.ActionLabel ?? string.Empty;
            journalPageActionButton.onClick.RemoveAllListeners();
            journalPageActionButton.interactable =
                !page.IsInputLocked && page.Action != RunUiActionType.None;
            if (journalPageActionButton.interactable)
            {
                var action = page.Action;
                journalPageActionButton.onClick.AddListener(() =>
                    controller?.ExecuteUiAction(action));
            }
        }

        private static Image CreateJournalImage(
            Transform parent,
            string name,
            Color color)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateJournalText(
            Transform parent,
            string name,
            string value,
            Font font,
            int fontSize,
            FontStyle style)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Text));
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.GetComponent<Text>();
            text.font = font ?? Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.text = value;
            return text;
        }

        private static Text FindJournalText(Transform root, string path)
        {
            var target = root.Find(path);
            return target == null ? null : target.GetComponent<Text>();
        }

        private static void StretchJournal(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetJournalRect(
            RectTransform rect,
            Vector2 anchor,
            Vector2 size,
            Vector2 position)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void AddJournalOutline(Image image, Color color)
        {
            var outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(2f, -2f);
        }

        private void SetChoiceArtwork(string artworkId)
        {
            var sprite = PresentationArtworkResources.LoadEvent(artworkId);
            var hasArtwork = sprite != null;
            if (hasArtwork)
            {
                choiceArtworkImage = PresentationArtworkResources.EnsureImage(
                    choiceTitleText.transform.parent,
                    "EventArtwork",
                    sprite,
                    Color.white,
                    false);
                if (choiceArtworkImage != null)
                {
                    var artworkRect = choiceArtworkImage.rectTransform;
                    artworkRect.anchorMin = Vector2.zero;
                    artworkRect.anchorMax = Vector2.zero;
                    artworkRect.pivot = Vector2.zero;
                    artworkRect.anchoredPosition = new Vector2(48f, 42f);
                    artworkRect.sizeDelta = new Vector2(450f, 400f);
                    choiceArtworkImage.preserveAspect = true;
                }
            }
            else if (choiceArtworkImage != null)
            {
                choiceArtworkImage.gameObject.SetActive(false);
            }

            var scrollRect = choiceScrollRect.GetComponent<RectTransform>();
            scrollRect.anchoredPosition =
                new Vector2(hasArtwork ? 520f : 48f, 42f);
            scrollRect.sizeDelta =
                new Vector2(hasArtwork ? 932f : 1404f, 400f);
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

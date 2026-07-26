using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.UI
{
    public enum PresentationBackdropVariant
    {
        MainMenu,
        Shop,
        RunMap,
        Battle,
        Modal
    }

    [DisallowMultipleComponent]
    public sealed class PresentationBackdropGraphic : MaskableGraphic
    {
        [SerializeField] private PresentationBackdropVariant variant;
        [SerializeField] private Color topColor =
            new Color(0.035f, 0.055f, 0.065f, 1f);
        [SerializeField] private Color bottomColor =
            new Color(0.010f, 0.018f, 0.024f, 1f);
        [SerializeField] private Color accentColor =
            new Color(0.72f, 0.55f, 0.28f, 1f);

        public PresentationBackdropVariant Variant => variant;
        public Color TopColor => topColor;
        public Color BottomColor => bottomColor;
        public Color AccentColor => accentColor;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        public void Configure(
            PresentationBackdropVariant value,
            Color top,
            Color bottom,
            Color accent)
        {
            variant = value;
            topColor = top;
            bottomColor = bottom;
            accentColor = accent;
            raycastTarget = false;
            SetVerticesDirty();
            SetMaterialDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            var target = GetPixelAdjustedRect();
            AddGradientQuad(vertexHelper, target, bottomColor, topColor);
            AddBorder(vertexHelper, target);
            AddDiagonalTexture(vertexHelper, target);

            switch (variant)
            {
                case PresentationBackdropVariant.MainMenu:
                    AddSpireSilhouette(vertexHelper, target);
                    break;
                case PresentationBackdropVariant.Shop:
                    AddShopShelves(vertexHelper, target);
                    break;
                case PresentationBackdropVariant.RunMap:
                    AddConstellation(vertexHelper, target);
                    break;
                case PresentationBackdropVariant.Battle:
                    AddBattleSplit(vertexHelper, target);
                    break;
                case PresentationBackdropVariant.Modal:
                    AddModalFocus(vertexHelper, target);
                    break;
            }
        }

        private void AddBorder(VertexHelper vertexHelper, Rect rect)
        {
            var color = WithAlpha(accentColor, 0.16f);
            const float thickness = 3f;
            AddSolidQuad(
                vertexHelper,
                new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness),
                color);
            AddSolidQuad(
                vertexHelper,
                new Rect(rect.xMin, rect.yMin, rect.width, thickness),
                WithAlpha(accentColor, 0.08f));
        }

        private void AddDiagonalTexture(VertexHelper vertexHelper, Rect rect)
        {
            var color = WithAlpha(accentColor, 0.026f);
            var gap = rect.width / 7f;
            for (var index = -2; index < 8; index++)
            {
                var x = rect.xMin + index * gap;
                var a = new Vector2(x, rect.yMin);
                var b = new Vector2(x + rect.height * 0.34f, rect.yMax);
                AddLine(vertexHelper, a, b, 2f, color);
            }
        }

        private void AddSpireSilhouette(VertexHelper vertexHelper, Rect rect)
        {
            var center = rect.center.x;
            var horizon = Mathf.Lerp(rect.yMin, rect.yMax, 0.18f);
            var silhouette = WithAlpha(new Color(0.01f, 0.012f, 0.018f, 1f), 0.72f);
            AddTriangle(
                vertexHelper,
                new Vector2(center - rect.width * 0.22f, horizon),
                new Vector2(center, rect.yMax - rect.height * 0.06f),
                new Vector2(center + rect.width * 0.22f, horizon),
                silhouette);
            AddSolidQuad(
                vertexHelper,
                new Rect(
                    center - rect.width * 0.15f,
                    rect.yMin,
                    rect.width * 0.30f,
                    rect.height * 0.24f),
                silhouette);
            AddLine(
                vertexHelper,
                new Vector2(center - rect.width * 0.25f, horizon),
                new Vector2(center + rect.width * 0.25f, horizon),
                2f,
                WithAlpha(accentColor, 0.22f));
        }

        private void AddShopShelves(VertexHelper vertexHelper, Rect rect)
        {
            var shelf = WithAlpha(accentColor, 0.065f);
            for (var index = 1; index <= 3; index++)
            {
                var y = Mathf.Lerp(rect.yMin, rect.yMax, index / 4f);
                AddSolidQuad(
                    vertexHelper,
                    new Rect(rect.xMin, y, rect.width, 2f),
                    shelf);
            }
        }

        private void AddConstellation(VertexHelper vertexHelper, Rect rect)
        {
            var points = new[]
            {
                new Vector2(0.08f, 0.22f),
                new Vector2(0.19f, 0.68f),
                new Vector2(0.34f, 0.42f),
                new Vector2(0.51f, 0.77f),
                new Vector2(0.68f, 0.34f),
                new Vector2(0.83f, 0.61f),
                new Vector2(0.94f, 0.27f)
            };
            var lineColor = WithAlpha(accentColor, 0.12f);
            var pointColor = WithAlpha(accentColor, 0.28f);
            for (var index = 0; index < points.Length; index++)
            {
                var point = ToRectPoint(rect, points[index]);
                AddSolidQuad(
                    vertexHelper,
                    new Rect(point.x - 2f, point.y - 2f, 4f, 4f),
                    pointColor);
                if (index > 0)
                {
                    AddLine(
                        vertexHelper,
                        ToRectPoint(rect, points[index - 1]),
                        point,
                        1.5f,
                        lineColor);
                }
            }
        }

        private void AddBattleSplit(VertexHelper vertexHelper, Rect rect)
        {
            AddTriangle(
                vertexHelper,
                new Vector2(rect.xMin, rect.yMax),
                new Vector2(rect.xMax, rect.yMax),
                new Vector2(rect.xMax, rect.center.y),
                new Color(0.25f, 0.07f, 0.06f, 0.10f));
            AddTriangle(
                vertexHelper,
                new Vector2(rect.xMin, rect.yMin),
                new Vector2(rect.xMax, rect.yMin),
                new Vector2(rect.xMin, rect.center.y),
                new Color(0.04f, 0.24f, 0.20f, 0.10f));
            AddLine(
                vertexHelper,
                new Vector2(rect.xMin, rect.center.y),
                new Vector2(rect.xMax, rect.center.y),
                3f,
                WithAlpha(accentColor, 0.14f));
        }

        private void AddModalFocus(VertexHelper vertexHelper, Rect rect)
        {
            var focus = new Rect(
                rect.center.x - rect.width * 0.22f,
                rect.center.y - rect.height * 0.30f,
                rect.width * 0.44f,
                rect.height * 0.60f);
            var color = WithAlpha(accentColor, 0.08f);
            AddSolidQuad(
                vertexHelper,
                new Rect(focus.xMin, focus.yMax - 2f, focus.width, 2f),
                color);
            AddSolidQuad(
                vertexHelper,
                new Rect(focus.xMin, focus.yMin, focus.width, 2f),
                color);
        }

        private static Vector2 ToRectPoint(Rect rect, Vector2 normalized)
        {
            return new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, normalized.x),
                Mathf.Lerp(rect.yMin, rect.yMax, normalized.y));
        }

        private static void AddGradientQuad(
            VertexHelper vertexHelper,
            Rect rect,
            Color bottom,
            Color top)
        {
            var start = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, new Vector2(rect.xMin, rect.yMin), bottom);
            AddVertex(vertexHelper, new Vector2(rect.xMin, rect.yMax), top);
            AddVertex(vertexHelper, new Vector2(rect.xMax, rect.yMax), top);
            AddVertex(vertexHelper, new Vector2(rect.xMax, rect.yMin), bottom);
            vertexHelper.AddTriangle(start, start + 1, start + 2);
            vertexHelper.AddTriangle(start + 2, start + 3, start);
        }

        private static void AddSolidQuad(
            VertexHelper vertexHelper,
            Rect rect,
            Color color)
        {
            AddGradientQuad(vertexHelper, rect, color, color);
        }

        private static void AddTriangle(
            VertexHelper vertexHelper,
            Vector2 a,
            Vector2 b,
            Vector2 c,
            Color color)
        {
            var start = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, a, color);
            AddVertex(vertexHelper, b, color);
            AddVertex(vertexHelper, c, color);
            vertexHelper.AddTriangle(start, start + 1, start + 2);
        }

        private static void AddLine(
            VertexHelper vertexHelper,
            Vector2 from,
            Vector2 to,
            float width,
            Color color)
        {
            var direction = (to - from).normalized;
            var normal = new Vector2(-direction.y, direction.x) * width * 0.5f;
            var start = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, from - normal, color);
            AddVertex(vertexHelper, from + normal, color);
            AddVertex(vertexHelper, to + normal, color);
            AddVertex(vertexHelper, to - normal, color);
            vertexHelper.AddTriangle(start, start + 1, start + 2);
            vertexHelper.AddTriangle(start + 2, start + 3, start);
        }

        private static void AddVertex(
            VertexHelper vertexHelper,
            Vector2 position,
            Color color)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            vertexHelper.AddVert(vertex);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}

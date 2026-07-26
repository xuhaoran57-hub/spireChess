using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SpireChess.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.Tests.EditMode
{
    public sealed class CardViewLayoutTests
    {
        private const string PrefabPath =
            "Assets/Prefabs/UI/Common/PF_Card.prefab";

        private static readonly string[] RequiredPaths =
        {
            "Background",
            "RaceSkin",
            "ArtworkMask",
            "ArtworkMask/Artwork",
            "ShieldOverlay",
            "NormalFrame",
            "GoldenFrame",
            "CostBadge/Cost",
            "TierBadge/Tier",
            "NamePlate/Name",
            "RaceOrSpellType",
            "AbilityLabelRow/Label0",
            "AbilityLabelRow/Label1",
            "AbilityLabelRow/Label2",
            "InfoPanel/Description",
            "InfoPanel/Progress/ProgressFill",
            "InfoPanel/Progress/ProgressText",
            "StateBadgeRow/GoldenBadge",
            "StateBadgeRow/ShieldBadge",
            "StateBadgeRow/NextCombatShieldBadge",
            "StateBadgeRow/TemporaryBadge",
            "AttackBadge/Attack",
            "HealthBadge/Health",
            "SpellFooter",
            "GrowthFeedbackRoot",
            "GrowthFeedbackRoot/FeedbackText",
            "SelectionFrame",
            "LegalTargetFrame",
            "DisabledMask/DisabledIcon",
            "DisabledMask/DisabledReason"
        };

        private GameObject instance;
        private RectTransform root;
        private CardView view;

        [SetUp]
        public void SetUp()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null, "PF_Card prefab could not be loaded.");
            instance = UnityEngine.Object.Instantiate(prefab);
            root = instance.GetComponent<RectTransform>();
            view = instance.GetComponent<CardView>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }

        [Test]
        public void Prefab_HasStableHierarchyAndCompleteBindings()
        {
            Assert.That(root, Is.Not.Null);
            Assert.That(instance.GetComponent<Image>(), Is.Not.Null);
            Assert.That(instance.GetComponent<CanvasGroup>(), Is.Not.Null);
            Assert.That(view, Is.Not.Null);
            Assert.That(view.HasCompleteBindings, Is.True);
            foreach (var path in RequiredPaths)
            {
                Assert.That(
                    root.Find(path),
                    Is.Not.Null,
                    "Missing stable PF_Card path: " + path);
            }

            var artworkMask = root.Find("ArtworkMask");
            Assert.That(artworkMask.GetComponent<Mask>(), Is.Not.Null);
            Assert.That(root.Find("ArtworkMask/Artwork").parent,
                Is.SameAs(artworkMask));
            Assert.That(root.Find("ShieldOverlay").IsChildOf(artworkMask), Is.False);
            Assert.That(root.Find("NormalFrame").IsChildOf(artworkMask), Is.False);
            Assert.That(root.Find("StateBadgeRow").IsChildOf(artworkMask), Is.False);
            Assert.That(root.Find("LegalTargetFrame").IsChildOf(artworkMask), Is.False);

            var labelRow = root.Find("AbilityLabelRow");
            Assert.That(labelRow.parent, Is.SameAs(root));
            var typeLine = root.Find("RaceOrSpellType");
            Assert.That(typeLine.parent, Is.SameAs(root));
            Assert.That(typeLine.GetSiblingIndex(),
                Is.GreaterThan(root.Find("NormalFrame").GetSiblingIndex()));
            Assert.That(typeLine.GetSiblingIndex(),
                Is.GreaterThan(root.Find("GoldenFrame").GetSiblingIndex()));
        }

        [Test]
        public void ShopUiPreview_DoesNotSerializeGoldenIdentityText()
        {
            var scenePath = Path.Combine(
                Application.dataPath,
                "Scenes",
                "ShopUiPreview.unity");
            var sceneYaml = File.ReadAllText(scenePath);

            Assert.That(sceneYaml, Does.Not.Contain("\\u91D1\\u8272"));
            Assert.That(sceneYaml, Does.Not.Contain("m_Text: 金色"));

            const string goldenBadgeMarker = "  m_Name: GoldenBadge";
            var searchIndex = 0;
            var goldenBadgeCount = 0;
            while ((searchIndex = sceneYaml.IndexOf(
                       goldenBadgeMarker,
                       searchIndex,
                       StringComparison.Ordinal)) >= 0)
            {
                var componentIndex = sceneYaml.IndexOf(
                    "--- !u!224",
                    searchIndex,
                    StringComparison.Ordinal);
                Assert.That(componentIndex, Is.GreaterThan(searchIndex));
                var gameObjectBlock = sceneYaml.Substring(
                    searchIndex,
                    componentIndex - searchIndex);
                Assert.That(
                    gameObjectBlock,
                    Does.Contain("  m_IsActive: 0"),
                    "Every serialized GoldenBadge must remain inactive.");
                goldenBadgeCount++;
                searchIndex = componentIndex;
            }

            Assert.That(goldenBadgeCount, Is.EqualTo(12));
        }

        [Test]
        public void FullAndCompact_UseExactFrozenGeometryWithoutDrift()
        {
            AssertLayout(CardDisplayMode.Full, 240f, 360f, FullRects());
            AssertLayout(CardDisplayMode.Compact, 160f, 240f, CompactRects());

            for (var iteration = 0; iteration < 20; iteration++)
            {
                view.Render(CreateModel(
                    iteration % 2 == 0
                        ? CardDisplayMode.Full
                        : CardDisplayMode.Compact));
            }

            AssertLayout(CardDisplayMode.Compact, 160f, 240f, CompactRects());
        }

        [TestCase(CardDisplayMode.Full, 30f, 232f, 210f, 256f)]
        [TestCase(CardDisplayMode.Compact, 20f, 152f, 140f, 171f)]
        public void TypeLine_StaysInsideTheFrameSafeOpening(
            CardDisplayMode mode,
            float safeLeft,
            float safeTop,
            float safeRight,
            float safeBottom)
        {
            view.Render(CreateModel(mode));
            var actual = GetRootRect(
                (RectTransform)root.Find("RaceOrSpellType"));

            Assert.That(actual.X, Is.GreaterThanOrEqualTo(safeLeft));
            Assert.That(actual.Y, Is.GreaterThanOrEqualTo(safeTop));
            Assert.That(actual.X + actual.Width,
                Is.LessThanOrEqualTo(safeRight));
            Assert.That(actual.Y + actual.Height,
                Is.LessThanOrEqualTo(safeBottom));
        }

        [Test]
        public void RaycastAndTextSettings_PreserveRootInputOwnership()
        {
            var graphics = instance.GetComponentsInChildren<Graphic>(true);
            foreach (var graphic in graphics)
            {
                var expected = graphic.gameObject == instance;
                Assert.That(
                    graphic.raycastTarget,
                    Is.EqualTo(expected),
                    "Unexpected raycastTarget at " + GetPath(graphic.transform));
            }

            foreach (var text in instance.GetComponentsInChildren<Text>(true))
            {
                Assert.That(text.supportRichText, Is.False,
                    "Rich text must stay disabled at " + GetPath(text.transform));
                Assert.That(text.resizeTextForBestFit, Is.False,
                    "Best Fit must stay disabled at " + GetPath(text.transform));
                Assert.That(text.font, Is.Not.Null,
                    "Pinned font is missing at " + GetPath(text.transform));
            }
        }

        private void AssertLayout(
            CardDisplayMode mode,
            float width,
            float height,
            IReadOnlyDictionary<string, ExpectedRect> expectedRects)
        {
            view.Render(CreateModel(mode));
            Assert.That(root.sizeDelta.x, Is.EqualTo(width).Within(0.01f));
            Assert.That(root.sizeDelta.y, Is.EqualTo(height).Within(0.01f));
            Assert.That(width / height, Is.EqualTo(2f / 3f).Within(0.0001f));
            foreach (var pair in expectedRects)
            {
                var target = root.Find(pair.Key) as RectTransform;
                Assert.That(target, Is.Not.Null, "Missing layout path " + pair.Key);
                var actual = GetRootRect(target);
                Assert.That(actual.X, Is.EqualTo(pair.Value.X).Within(0.05f),
                    pair.Key + " x");
                Assert.That(actual.Y, Is.EqualTo(pair.Value.Y).Within(0.05f),
                    pair.Key + " y");
                Assert.That(actual.Width,
                    Is.EqualTo(pair.Value.Width).Within(0.05f),
                    pair.Key + " width");
                Assert.That(actual.Height,
                    Is.EqualTo(pair.Value.Height).Within(0.05f),
                    pair.Key + " height");
            }
        }

        private ExpectedRect GetRootRect(RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            var topLeft = root.InverseTransformPoint(corners[1]);
            var topRight = root.InverseTransformPoint(corners[2]);
            var bottomLeft = root.InverseTransformPoint(corners[0]);
            return new ExpectedRect(
                topLeft.x,
                -topLeft.y,
                topRight.x - topLeft.x,
                topLeft.y - bottomLeft.y);
        }

        private static CardViewModel CreateModel(CardDisplayMode mode)
        {
            return new CardViewModel
            {
                Name = "测试随从",
                Description = "用于验证共享卡牌固定几何。",
                RaceText = "星契",
                AbilityLabels = new[] { "成长", "刷新" },
                Tier = 3,
                Attack = 4,
                Health = 8,
                BaseAttack = 4,
                BaseHealth = 8,
                Cost = 3,
                DisplayMode = mode,
                IsMinion = true,
                ShowCost = true,
                IsInteractable = true,
                IsAffordable = true
            };
        }

        private static IReadOnlyDictionary<string, ExpectedRect> FullRects()
        {
            return new Dictionary<string, ExpectedRect>
            {
                ["NormalFrame"] = new ExpectedRect(6f, 6f, 228f, 348f),
                ["ArtworkMask"] = new ExpectedRect(12f, 12f, 216f, 184f),
                ["ShieldOverlay"] = new ExpectedRect(15f, 8f, 210f, 344f),
                ["CostBadge"] = new ExpectedRect(13f, 12f, 28f, 29f),
                ["TierBadge"] = new ExpectedRect(198f, 9f, 32f, 40f),
                ["TierBadge/Tier"] =
                    new ExpectedRect(199f, 10f, 30f, 38f),
                ["StateBadgeRow"] = new ExpectedRect(44f, 157f, 152f, 22f),
                ["NamePlate"] = new ExpectedRect(24f, 181f, 192f, 32f),
                ["InfoPanel"] = new ExpectedRect(12f, 199f, 216f, 149f),
                ["RaceOrSpellType"] =
                    new ExpectedRect(36f, 232f, 168f, 24f),
                ["InfoPanel/Description"] =
                    new ExpectedRect(30f, 256f, 180f, 64f),
                ["InfoPanel/Progress"] =
                    new ExpectedRect(62f, 293f, 116f, 18f),
                ["AttackBadge"] = new ExpectedRect(10f, 321f, 68f, 30f),
                ["AttackBadge/Attack"] =
                    new ExpectedRect(27f, 323f, 46f, 26f),
                ["HealthBadge"] = new ExpectedRect(162f, 321f, 68f, 30f),
                ["HealthBadge/Health"] =
                    new ExpectedRect(167f, 323f, 46f, 26f),
                ["SpellFooter"] = new ExpectedRect(80f, 332f, 80f, 16f),
                ["SelectionFrame"] = new ExpectedRect(0f, 0f, 240f, 360f)
            };
        }

        private static IReadOnlyDictionary<string, ExpectedRect> CompactRects()
        {
            return new Dictionary<string, ExpectedRect>
            {
                ["NormalFrame"] = new ExpectedRect(4f, 4f, 152f, 232f),
                ["ArtworkMask"] = new ExpectedRect(8f, 8f, 144f, 112f),
                ["ShieldOverlay"] = new ExpectedRect(10f, 5f, 140f, 230f),
                ["CostBadge"] = new ExpectedRect(9f, 8f, 19f, 20f),
                ["TierBadge"] = new ExpectedRect(132f, 6f, 22f, 28f),
                ["TierBadge/Tier"] =
                    new ExpectedRect(133f, 7f, 20f, 26f),
                ["StateBadgeRow"] = new ExpectedRect(28f, 91f, 104f, 18f),
                ["NamePlate"] = new ExpectedRect(16f, 108f, 128f, 26f),
                ["InfoPanel"] = new ExpectedRect(8f, 122f, 144f, 110f),
                ["RaceOrSpellType"] =
                    new ExpectedRect(24f, 152f, 112f, 19f),
                ["InfoPanel/Description"] =
                    new ExpectedRect(20f, 172f, 120f, 33f),
                ["InfoPanel/Progress"] =
                    new ExpectedRect(44f, 197f, 72f, 14f),
                ["AttackBadge"] = new ExpectedRect(7f, 213f, 46f, 21f),
                ["AttackBadge/Attack"] =
                    new ExpectedRect(18f, 214f, 31f, 19f),
                ["HealthBadge"] = new ExpectedRect(107f, 213f, 46f, 21f),
                ["HealthBadge/Health"] =
                    new ExpectedRect(110f, 214f, 31f, 19f),
                ["SpellFooter"] = new ExpectedRect(55f, 220f, 50f, 13f),
                ["SelectionFrame"] = new ExpectedRect(0f, 0f, 160f, 240f)
            };
        }

        private static string GetPath(Transform value)
        {
            var path = value.name;
            while (value.parent != null)
            {
                value = value.parent;
                path = value.name + "/" + path;
            }

            return path;
        }

        private readonly struct ExpectedRect
        {
            public ExpectedRect(float x, float y, float width, float height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            public float X { get; }
            public float Y { get; }
            public float Width { get; }
            public float Height { get; }
        }
    }
}

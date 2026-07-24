using System;
using System.Collections.Generic;
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
            "NormalFrame",
            "GoldenFrame",
            "CostBadge/Cost",
            "TierBadge/Tier",
            "NamePlate/Name",
            "InfoPanel/RaceOrSpellType",
            "InfoPanel/AbilityLabelRow/Label0",
            "InfoPanel/AbilityLabelRow/Label1",
            "InfoPanel/AbilityLabelRow/Label2",
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
            Assert.That(root.Find("NormalFrame").IsChildOf(artworkMask), Is.False);
            Assert.That(root.Find("StateBadgeRow").IsChildOf(artworkMask), Is.False);
            Assert.That(root.Find("LegalTargetFrame").IsChildOf(artworkMask), Is.False);
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
                ["CostBadge"] = new ExpectedRect(13f, 12f, 28f, 29f),
                ["TierBadge"] = new ExpectedRect(205f, 13f, 21f, 28f),
                ["StateBadgeRow"] = new ExpectedRect(60f, 157f, 120f, 22f),
                ["NamePlate"] = new ExpectedRect(24f, 181f, 192f, 32f),
                ["InfoPanel"] = new ExpectedRect(12f, 199f, 216f, 149f),
                ["InfoPanel/RaceOrSpellType"] =
                    new ExpectedRect(44f, 215f, 152f, 18f),
                ["InfoPanel/AbilityLabelRow"] =
                    new ExpectedRect(20f, 235f, 200f, 20f),
                ["InfoPanel/Description"] =
                    new ExpectedRect(12f, 256f, 216f, 52f),
                ["InfoPanel/Progress"] =
                    new ExpectedRect(62f, 293f, 116f, 18f),
                ["AttackBadge"] = new ExpectedRect(13f, 327f, 55f, 22f),
                ["AttackBadge/Attack"] =
                    new ExpectedRect(27f, 328f, 37f, 20f),
                ["HealthBadge"] = new ExpectedRect(172f, 327f, 55f, 22f),
                ["HealthBadge/Health"] =
                    new ExpectedRect(176f, 328f, 36f, 20f),
                ["SpellFooter"] = new ExpectedRect(58f, 318f, 124f, 22f),
                ["SelectionFrame"] = new ExpectedRect(0f, 0f, 240f, 360f)
            };
        }

        private static IReadOnlyDictionary<string, ExpectedRect> CompactRects()
        {
            return new Dictionary<string, ExpectedRect>
            {
                ["NormalFrame"] = new ExpectedRect(4f, 4f, 152f, 232f),
                ["ArtworkMask"] = new ExpectedRect(8f, 8f, 144f, 112f),
                ["CostBadge"] = new ExpectedRect(9f, 8f, 19f, 20f),
                ["TierBadge"] = new ExpectedRect(137f, 9f, 14f, 19f),
                ["StateBadgeRow"] = new ExpectedRect(42f, 91f, 76f, 18f),
                ["NamePlate"] = new ExpectedRect(16f, 108f, 128f, 26f),
                ["InfoPanel"] = new ExpectedRect(8f, 122f, 144f, 110f),
                ["InfoPanel/RaceOrSpellType"] =
                    new ExpectedRect(28f, 136f, 104f, 14f),
                ["InfoPanel/AbilityLabelRow"] =
                    new ExpectedRect(12f, 154f, 136f, 16f),
                ["InfoPanel/Description"] =
                    new ExpectedRect(12f, 172f, 136f, 33f),
                ["InfoPanel/Progress"] =
                    new ExpectedRect(44f, 197f, 72f, 14f),
                ["AttackBadge"] = new ExpectedRect(9f, 218f, 36f, 15f),
                ["AttackBadge/Attack"] =
                    new ExpectedRect(18f, 219f, 24f, 13f),
                ["HealthBadge"] = new ExpectedRect(115f, 218f, 36f, 15f),
                ["HealthBadge/Health"] =
                    new ExpectedRect(118f, 219f, 23f, 13f),
                ["SpellFooter"] = new ExpectedRect(42f, 211f, 76f, 16f),
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

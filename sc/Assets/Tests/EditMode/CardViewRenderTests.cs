using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using SpireChess.Battle;
using SpireChess.Config;
using SpireChess.UI;
using SpireChess.UI.Battle;
using SpireChess.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.Tests.EditMode
{
    public sealed class CardViewRenderTests
    {
        private const string PrefabPath =
            "Assets/Prefabs/UI/Common/PF_Card.prefab";

        private GameObject instance;
        private RectTransform root;
        private CardView view;

        [SetUp]
        public void SetUp()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);
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
        public void Render_NullModelFailsExplicitly()
        {
            Assert.Throws<ArgumentNullException>(() => view.Render(null));
        }

        [Test]
        public void FourVariants_ReRenderWithoutStateLeakage()
        {
            var golden = CreateMinion(CardDisplayMode.Full);
            golden.IsGolden = true;
            golden.HasShield = true;
            golden.HasNextCombatShield = true;
            golden.IsTemporary = true;
            golden.IsSelected = true;
            golden.IsLegalTarget = true;
            golden.Attack = 12;
            golden.Health = 20;
            view.Render(golden);
            Assert.That(Active("GoldenFrame"), Is.True);
            Assert.That(Active("StateBadgeRow/GoldenBadge"), Is.True);
            Assert.That(TextAt("StateBadgeRow/GoldenBadge"), Is.EqualTo("金色"));
            Assert.That(Active("StateBadgeRow/ShieldBadge"), Is.True);
            Assert.That(Active("SelectionFrame"), Is.True);
            Assert.That(Active("LegalTargetFrame"), Is.True);

            var spell = CreateSpell(CardDisplayMode.Full);
            view.Render(spell);
            Assert.That(Active("GoldenFrame"), Is.False);
            Assert.That(Active("NormalFrame"), Is.True);
            Assert.That(Active("AttackBadge"), Is.False);
            Assert.That(Active("HealthBadge"), Is.False);
            Assert.That(Active("SpellFooter"), Is.True);
            Assert.That(Active("StateBadgeRow/ShieldBadge"), Is.False);
            Assert.That(Active("StateBadgeRow/GoldenBadge"), Is.False);
            Assert.That(Active("StateBadgeRow/NextCombatShieldBadge"), Is.False);
            Assert.That(Active("SelectionFrame"), Is.False);
            Assert.That(Active("LegalTargetFrame"), Is.False);
            Assert.That(Active("CostBadge"), Is.True);

            var ownedMinion = CreateMinion(CardDisplayMode.Compact);
            ownedMinion.ShowCost = false;
            view.Render(ownedMinion);
            Assert.That(Active("CostBadge"), Is.False);
            Assert.That(Active("AttackBadge"), Is.True);
            Assert.That(Active("SpellFooter"), Is.False);

            var ownedSpell = CreateSpell(CardDisplayMode.Compact);
            ownedSpell.ShowCost = false;
            ownedSpell.IsTemporary = true;
            view.Render(ownedSpell);
            Assert.That(Active("CostBadge"), Is.False);
            Assert.That(Active("SpellFooter"), Is.True);
            Assert.That(Active("StateBadgeRow/TemporaryBadge"), Is.True);
        }

        [Test]
        public void CompactOwnedGoldenMinion_HasPersistentTextAndFrameIdentification()
        {
            var model = CreateMinion(CardDisplayMode.Compact);
            model.ShowCost = false;
            model.IsGolden = true;

            view.Render(model);

            Assert.That(Active("GoldenFrame"), Is.True);
            Assert.That(Active("NormalFrame"), Is.False);
            Assert.That(Active("StateBadgeRow/GoldenBadge"), Is.True);
            Assert.That(TextAt("StateBadgeRow/GoldenBadge"), Is.EqualTo("金色"));
            Assert.That(TextComponentAt("NamePlate/Name").color,
                Is.EqualTo((Color)new Color32(0xFF, 0xD2, 0x58, 0xFF)));

            model.IsGolden = false;
            view.Render(model);
            Assert.That(Active("GoldenFrame"), Is.False);
            Assert.That(Active("NormalFrame"), Is.True);
            Assert.That(Active("StateBadgeRow/GoldenBadge"), Is.False);
        }

        [Test]
        public void BattleCardStats_FitLargeValuesAndRestoreBaseFontSize()
        {
            var cardObject = new GameObject(
                "BattleCard",
                typeof(RectTransform),
                typeof(BattleCardView));
            var statsObject = new GameObject(
                "Stats",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            statsObject.transform.SetParent(cardObject.transform, false);
            var statsRect = statsObject.GetComponent<RectTransform>();
            statsRect.sizeDelta = new Vector2(84f, 32f);
            var statsText = statsObject.GetComponent<Text>();
            statsText.font = TextComponentAt("HealthBadge/Health").font;
            statsText.fontSize = 20;
            statsText.horizontalOverflow = HorizontalWrapMode.Overflow;
            statsText.verticalOverflow = VerticalWrapMode.Overflow;
            var battleView = cardObject.GetComponent<BattleCardView>();
            var config = new MinionConfig
            {
                Id = "large_stats",
                Name = "大数值随从",
                Tier = 5,
                Race = "Starbound",
                Attack = 1,
                Health = 1,
                GoldenAttack = 2,
                GoldenHealth = 2
            };

            try
            {
                battleView.Render(new BattleMinionRuntime(
                    config,
                    initialAttack: 999,
                    initialHealth: 1200));

                Assert.That(statsText.text, Is.EqualTo("999/1200"));
                Assert.That(statsText.fontSize, Is.LessThan(20));
                Assert.That(statsText.fontSize, Is.GreaterThanOrEqualTo(10));
                Assert.That(statsText.preferredWidth, Is.LessThanOrEqualTo(76.5f));

                battleView.Render(new BattleMinionRuntime(
                    config,
                    initialAttack: 8,
                    initialHealth: 9));
                Assert.That(statsText.text, Is.EqualTo("8/9"));
                Assert.That(statsText.fontSize, Is.EqualTo(20));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cardObject);
            }
        }

        [Test]
        public void LabelsAndProgress_UseFrozenCapacitiesAndRects()
        {
            var model = CreateMinion(CardDisplayMode.Full);
            model.AbilityLabels = new[] { "一", "二", "三", "四", "五" };
            model.ProgressText = "3/4";
            view.Render(model);
            Assert.That(TextAt("InfoPanel/AbilityLabelRow/Label0"), Is.EqualTo("一"));
            Assert.That(TextAt("InfoPanel/AbilityLabelRow/Label1"), Is.EqualTo("二"));
            Assert.That(TextAt("InfoPanel/AbilityLabelRow/Label2"), Is.EqualTo("+3"));
            Assert.That(Active("InfoPanel/Progress"), Is.True);
            Assert.That(TextAt("InfoPanel/Progress/ProgressText"), Is.EqualTo("3/4"));
            Assert.That(RectAt("InfoPanel/Description").rect.height,
                Is.EqualTo(31f).Within(0.01f));

            model.DisplayMode = CardDisplayMode.Compact;
            model.ProgressText = null;
            view.Render(model);
            Assert.That(TextAt("InfoPanel/AbilityLabelRow/Label0"), Is.EqualTo("一"));
            Assert.That(TextAt("InfoPanel/AbilityLabelRow/Label1"), Is.EqualTo("+4"));
            Assert.That(Active("InfoPanel/AbilityLabelRow/Label2"), Is.False);
            Assert.That(Active("InfoPanel/Progress"), Is.False);
            Assert.That(RectAt("InfoPanel/Description").rect.height,
                Is.EqualTo(33f).Within(0.01f));
        }

        [Test]
        public void StatePriority_DisabledCardSuppressesTargetAndGrowthFeedback()
        {
            var model = CreateMinion(CardDisplayMode.Compact);
            model.Attack = 9;
            model.Health = 12;
            model.HasShield = true;
            model.HasNextCombatShield = true;
            model.IsTemporary = true;
            model.IsSelected = true;
            model.IsLegalTarget = true;
            view.Render(model);
            Assert.That(Active("StateBadgeRow/ShieldBadge"), Is.True);
            Assert.That(Active("StateBadgeRow/NextCombatShieldBadge"), Is.True);
            Assert.That(Active("StateBadgeRow/TemporaryBadge"), Is.True);
            Assert.That(Active("SelectionFrame"), Is.True);
            Assert.That(Active("LegalTargetFrame"), Is.True);
            Assert.That(TextComponentAt("AttackBadge/Attack").color,
                Is.EqualTo((Color)new Color32(0x62, 0xE6, 0xA6, 0xFF)));

            model.IsInteractable = false;
            model.DisabledReason = "没有合法目标";
            view.Render(model);
            Assert.That(Active("DisabledMask"), Is.True);
            Assert.That(Active("LegalTargetFrame"), Is.False);
            Assert.That(Active("SelectionFrame"), Is.True);
            Assert.That(TextAt("DisabledMask/DisabledReason"),
                Is.EqualTo("没有合法目标"));
            Assert.That(TextComponentAt("AttackBadge/Attack").color,
                Is.Not.EqualTo((Color)new Color32(0x62, 0xE6, 0xA6, 0xFF)));
            Assert.That(Active("GrowthFeedbackRoot"), Is.False);
        }

        [Test]
        public void Feedback_FormatsStatDeltaPulsesShieldAndResetsOnRender()
        {
            var model = CreateMinion(CardDisplayMode.Compact);
            model.HasShield = true;
            view.Render(model);

            view.PlayStatChange(2, 3);
            view.PlayShieldGain(false);
            Assert.That(Active("GrowthFeedbackRoot"), Is.True);
            Assert.That(TextAt("GrowthFeedbackRoot/FeedbackText"),
                Is.EqualTo("+2/+3"));
            Assert.That(
                RectAt("StateBadgeRow/ShieldBadge").localScale.x,
                Is.GreaterThan(1f));

            view.Render(model);
            Assert.That(Active("GrowthFeedbackRoot"), Is.False);
            Assert.That(TextAt("GrowthFeedbackRoot/FeedbackText"), Is.Empty);
            Assert.That(
                RectAt("StateBadgeRow/ShieldBadge").localScale,
                Is.EqualTo(Vector3.one));

            view.PlayStatChange(-1, 0);
            Assert.That(TextAt("GrowthFeedbackRoot/FeedbackText"),
                Is.EqualTo("-1 攻击"));
            Assert.That(
                TextComponentAt("GrowthFeedbackRoot/FeedbackText").color.r,
                Is.GreaterThan(
                    TextComponentAt("GrowthFeedbackRoot/FeedbackText").color.g));
        }

        [Test]
        public void FullMode_UsesActualFontAndPreservesCurrentLongestDescriptions()
        {
            var configs = new ConfigService(new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            Assert.That(validation.IsValid, Is.True,
                string.Join("\n", validation.Errors));
            var minion = configs.Minions
                .Where(value => !value.IsToken)
                .OrderByDescending(value => Math.Max(
                    UiTextFormatter.CountTextElements(
                        value.GetPrototypeDescription(false)),
                    UiTextFormatter.CountTextElements(
                        value.GetPrototypeDescription(true))))
                .First();
            var normalMinionLength = UiTextFormatter.CountTextElements(
                minion.GetPrototypeDescription(false));
            var goldenMinionLength = UiTextFormatter.CountTextElements(
                minion.GetPrototypeDescription(true));
            var useGoldenDescription = goldenMinionLength >= normalMinionLength;
            var minionDescription =
                minion.GetPrototypeDescription(useGoldenDescription);
            var tenThousandHoof =
                configs.MinionsById["ten_thousand_hoof_surge"];
            var tenThousandHoofDescription =
                tenThousandHoof.GetPrototypeDescription(false);
            var spell = configs.Spells
                .OrderByDescending(value =>
                    UiTextFormatter.CountTextElements(value.Description))
                .First();
            Assert.That(UiTextFormatter.CountTextElements(minionDescription),
                Is.EqualTo(64));
            Assert.That(UiTextFormatter.CountTextElements(minionDescription),
                Is.LessThanOrEqualTo(
                    UiTextFormatter.CurrentMinionDescriptionLimit));
            Assert.That(
                UiTextFormatter.CountTextElements(tenThousandHoofDescription),
                Is.EqualTo(63));
            Assert.That(UiTextFormatter.CountTextElements(spell.Description),
                Is.EqualTo(45));

            var minionModel = CreateMinion(CardDisplayMode.Full);
            minionModel.Name = minion.Name;
            minionModel.Description = minionDescription;
            minionModel.IsGolden = useGoldenDescription;
            view.Render(minionModel);
            Assert.That(TextAt("InfoPanel/Description"),
                Is.EqualTo(minionDescription));
            Assert.That(TextComponentAt("InfoPanel/Description").fontSize,
                Is.GreaterThanOrEqualTo(11));

            var hoofModel = CreateMinion(CardDisplayMode.Full);
            hoofModel.Name = tenThousandHoof.Name;
            hoofModel.Description = tenThousandHoofDescription;
            view.Render(hoofModel);
            Assert.That(TextAt("InfoPanel/Description"),
                Is.EqualTo(tenThousandHoofDescription));
            Assert.That(TextComponentAt("InfoPanel/Description").fontSize,
                Is.GreaterThanOrEqualTo(11));

            var spellModel = CreateSpell(CardDisplayMode.Full);
            spellModel.Name = spell.Name;
            spellModel.Description = spell.Description;
            view.Render(spellModel);
            Assert.That(TextAt("InfoPanel/Description"),
                Is.EqualTo(spell.Description));
            Assert.That(TextComponentAt("InfoPanel/Description").fontSize,
                Is.GreaterThanOrEqualTo(11));
        }

        [Test]
        public void ScaledCanvas_EllipsizesInLogicalUiUnits()
        {
            var canvasObject = new GameObject(
                "ScaledCanvas",
                typeof(RectTransform),
                typeof(Canvas));
            try
            {
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.scaleFactor = Mathf.Sqrt(1200f / 1080f);
                root.SetParent(canvas.transform, false);

                var model = CreateMinion(CardDisplayMode.Full);
                model.Name = new string('长', 80);

                Assert.DoesNotThrow(() => view.Render(model));
                Assert.That(TextAt("NamePlate/Name"),
                    Does.EndWith(UiTextFormatter.Ellipsis));
            }
            finally
            {
                root.SetParent(null, false);
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void CompactLongDescription_EllipsizesAtUnicodeTextElementBoundary()
        {
            var cluster = "e\u0301😀";
            var value = string.Concat(Enumerable.Repeat(cluster, 80));
            var model = CreateMinion(CardDisplayMode.Compact);
            model.Description = value;
            view.Render(model);

            var rendered = TextAt("InfoPanel/Description");
            Assert.That(rendered, Does.EndWith(UiTextFormatter.Ellipsis));
            var prefix = rendered.Substring(
                0,
                rendered.Length - UiTextFormatter.Ellipsis.Length);
            Assert.That(value.StartsWith(prefix, StringComparison.Ordinal), Is.True);
            var elements = StringInfo.ParseCombiningCharacters(prefix);
            Assert.That(elements.Length, Is.GreaterThan(0));
            Assert.That(elements[elements.Length - 1], Is.LessThan(prefix.Length));
        }

        private bool Active(string path)
        {
            var target = root.Find(path);
            Assert.That(target, Is.Not.Null, "Missing render path " + path);
            return target.gameObject.activeSelf;
        }

        private string TextAt(string path)
        {
            return TextComponentAt(path).text;
        }

        private Text TextComponentAt(string path)
        {
            var target = root.Find(path);
            Assert.That(target, Is.Not.Null, "Missing text path " + path);
            var text = target.GetComponent<Text>();
            Assert.That(text, Is.Not.Null, "Missing Text component at " + path);
            return text;
        }

        private RectTransform RectAt(string path)
        {
            var target = root.Find(path) as RectTransform;
            Assert.That(target, Is.Not.Null, "Missing RectTransform at " + path);
            return target;
        }

        private static CardViewModel CreateMinion(CardDisplayMode mode)
        {
            return new CardViewModel
            {
                InstanceId = "minion_001",
                Name = "天穹契约者",
                Description = "每完成指定次数刷新，使所有友方星契永久获得成长。",
                RaceText = "星契",
                AbilityLabels = new[] { "刷新成长", "永久成长" },
                Tier = 3,
                Attack = 4,
                Health = 8,
                BaseAttack = 4,
                BaseHealth = 8,
                Cost = 3,
                DisplayMode = mode,
                IsMinion = true,
                ShowCost = mode == CardDisplayMode.Full,
                IsInteractable = true,
                IsAffordable = true
            };
        }

        private static CardViewModel CreateSpell(CardDisplayMode mode)
        {
            return new CardViewModel
            {
                InstanceId = "spell_001",
                Name = "高阶发现",
                Description = "发现一个当前酒馆等级的随从。",
                RaceText = "发现",
                AbilityLabels = new[] { "随从发现" },
                Tier = 3,
                Cost = 1,
                DisplayMode = mode,
                ShowCost = mode == CardDisplayMode.Full,
                IsInteractable = true,
                IsAffordable = true
            };
        }
    }
}

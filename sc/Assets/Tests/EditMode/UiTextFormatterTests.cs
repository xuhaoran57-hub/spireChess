using System;
using System.Linq;
using NUnit.Framework;
using SpireChess.UI;

namespace SpireChess.Tests.EditMode
{
    public sealed class UiTextFormatterTests
    {
        [Test]
        public void NormalizeWhitespace_CollapsesSpacesAndPreservesExplicitNewlines()
        {
            var value = "  甲\t\t乙\r\n丙\u00a0\u00a0丁\r戊";

            var result = UiTextFormatter.NormalizeWhitespace(value);

            Assert.That(result, Is.EqualTo(" 甲 乙\n丙 丁\n戊"));
        }

        [Test]
        public void ToSingleLine_ReplacesNewlinesAndKeepsRichTextLiteral()
        {
            var value = "名称\n  <b>不是富文本</b>\t标签";

            var result = UiTextFormatter.ToSingleLine(value);

            Assert.That(result, Is.EqualTo("名称 <b>不是富文本</b> 标签"));
        }

        [Test]
        public void AbilityLabels_UseModeLimitAndCountAllHiddenLabels()
        {
            var labels = new[] { "一", "二", "三", "四", "五" };

            var full = UiTextFormatter.FormatAbilityLabels(
                labels,
                CardDisplayMode.Full);
            var compact = UiTextFormatter.FormatAbilityLabels(
                labels,
                CardDisplayMode.Compact);

            Assert.Multiple(() =>
            {
                Assert.That(full, Is.EqualTo(new[] { "一", "二", "+3" }));
                Assert.That(compact, Is.EqualTo(new[] { "一", "+4" }));
            });
        }

        [Test]
        public void AbilityLabels_NormalizeAndIgnoreEmptyEntries()
        {
            var labels = new[] { "成长\n标签", null, "\t", "护盾  状态" };

            var result = UiTextFormatter.FormatAbilityLabels(
                labels,
                CardDisplayMode.Full);

            Assert.That(result, Is.EqualTo(new[] { "成长 标签", "护盾 状态" }));
        }

        [Test]
        public void DescriptionLineLimits_MatchFrozenLayoutContract()
        {
            Assert.Multiple(() =>
            {
                Assert.That(UiTextFormatter.GetDescriptionMaxLines(
                    CardDisplayMode.Full, true, false), Is.EqualTo(4));
                Assert.That(UiTextFormatter.GetDescriptionMaxLines(
                    CardDisplayMode.Full, true, true), Is.EqualTo(2));
                Assert.That(UiTextFormatter.GetDescriptionMaxLines(
                    CardDisplayMode.Full, false, false), Is.EqualTo(5));
                Assert.That(UiTextFormatter.GetDescriptionMaxLines(
                    CardDisplayMode.Compact, true, false), Is.EqualTo(3));
                Assert.That(UiTextFormatter.GetDescriptionMaxLines(
                    CardDisplayMode.Compact, true, true), Is.EqualTo(2));
                Assert.That(UiTextFormatter.GetDescriptionMaxLines(
                    CardDisplayMode.Compact, false, false), Is.EqualTo(4));
            });
        }

        [Test]
        public void EllipsizeName_UsesCompleteUnicodeTextElements()
        {
            var value = "甲e\u0301😀乙";

            var result = UiTextFormatter.EllipsizeName(
                value,
                candidate => UiTextFormatter.CountTextElements(candidate) <= 3);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo("甲e\u0301…"));
                Assert.That(UiTextFormatter.CountTextElements(result), Is.EqualTo(3));
            });
        }

        [Test]
        public void CompactDescription_TruncatesButFullDescriptionFailsContract()
        {
            var value = "甲乙丙丁戊";
            Func<string, bool> fits = candidate =>
                UiTextFormatter.CountTextElements(candidate) <= 4;

            var compact = UiTextFormatter.EllipsizeDescription(
                value,
                CardDisplayMode.Compact,
                fits);

            Assert.That(compact, Is.EqualTo("甲乙丙…"));
            Assert.Throws<InvalidOperationException>(() =>
                UiTextFormatter.EllipsizeDescription(
                    value,
                    CardDisplayMode.Full,
                    fits));
        }

        [Test]
        public void FullDescriptions_PreserveCurrent66And45CharacterLimits()
        {
            var minionDescription = new string(
                '随',
                UiTextFormatter.CurrentMinionDescriptionLimit);
            var spellDescription = new string(
                '法',
                UiTextFormatter.CurrentSpellDescriptionLimit);

            var minionResult = UiTextFormatter.EllipsizeDescription(
                minionDescription,
                CardDisplayMode.Full,
                candidate => UiTextFormatter.CountTextElements(candidate) <=
                             UiTextFormatter.CurrentMinionDescriptionLimit);
            var spellResult = UiTextFormatter.EllipsizeDescription(
                spellDescription,
                CardDisplayMode.Full,
                candidate => UiTextFormatter.CountTextElements(candidate) <=
                             UiTextFormatter.CurrentSpellDescriptionLimit);

            Assert.Multiple(() =>
            {
                Assert.That(minionResult, Is.EqualTo(minionDescription));
                Assert.That(spellResult, Is.EqualTo(spellDescription));
            });
        }

        [Test]
        public void EllipsizeToFit_RejectsNullPredicateAndImpossibleArea()
        {
            Assert.Throws<ArgumentNullException>(() =>
                UiTextFormatter.EllipsizeToFit("文本", null));
            Assert.Throws<InvalidOperationException>(() =>
                UiTextFormatter.EllipsizeToFit("文本", _ => false));
        }

        [Test]
        public void EllipsizeToFit_ReturnsOriginalWhenItAlreadyFits()
        {
            var value = "完整文本";
            var calls = 0;

            var result = UiTextFormatter.EllipsizeToFit(value, candidate =>
            {
                calls++;
                return candidate == value;
            });

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo(value));
                Assert.That(calls, Is.EqualTo(1));
            });
        }
    }
}

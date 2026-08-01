using System;
using System.Linq;
using NUnit.Framework;
using SpireChess.Editor;

namespace SpireChess.Tests.EditMode
{
    public sealed class LightStorybookArtRefreshV034Tests
    {
        [Test]
        public void Plan_CoversExactlyTheSeventeenApprovedRefreshes()
        {
            var plan = LightStorybookArtRefreshV034Builder.CreatePlan();

            Assert.That(plan.Entries, Has.Length.EqualTo(17));
            Assert.That(
                plan.Entries.Count(value => value.Kind == "Minion"),
                Is.EqualTo(10));
            Assert.That(
                plan.Entries.Count(value => value.Kind == "Spell"),
                Is.EqualTo(4));
            Assert.That(
                plan.Entries.Count(value => value.Kind == "Token"),
                Is.EqualTo(3));
            Assert.That(
                plan.Entries.Select(value => value.ArtId)
                    .Distinct(StringComparer.Ordinal)
                    .Count(),
                Is.EqualTo(17));
            Assert.That(
                plan.Entries.Select(value => value.RuntimePath)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                Is.EqualTo(17));
            Assert.That(
                plan.Entries.All(value =>
                    value.RuntimePath.StartsWith(
                        "Assets/Art/Presentation/Cards/",
                        StringComparison.Ordinal) &&
                    Math.Abs(value.FocalPointY - 0.5f) < 0.0001f),
                Is.True);
        }

        [Test]
        public void Runtime_HasExactApprovedStyleCoverageForAllConfiguredCards()
        {
            var promotionFailures =
                LightStorybookArtRefreshV034Builder
                    .ValidatePromotedState();
            Assert.That(
                promotionFailures,
                Is.Empty,
                string.Join("\n", promotionFailures));

            var coverageFailures =
                LightStorybookArtRefreshV034Builder
                    .ValidateStyleCoverage();
            Assert.That(
                coverageFailures,
                Is.Empty,
                string.Join("\n", coverageFailures));
        }
    }
}

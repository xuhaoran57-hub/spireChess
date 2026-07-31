using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SpireChess.Editor;
using UnityEngine;

namespace SpireChess.Tests.EditMode
{
    public sealed class LightStorybookRuntimePromotionBuilderTests
    {
        [Test]
        public void Plan_CoversProductionManifestAndUsesSafeUniquePaths()
        {
            var plan =
                LightStorybookRuntimePromotionBuilder.CreatePlan();

            Assert.That(plan.Entries.Length, Is.EqualTo(66));
            Assert.That(
                plan.Entries.Count(value => value.SourcePath.StartsWith(
                    "Assets/Art/Presentation/Calibration/" +
                    "LightStorybookFormalCatalogV032/",
                    StringComparison.Ordinal)),
                Is.EqualTo(15));
            Assert.That(
                plan.Entries.Count(value => value.SourcePath.StartsWith(
                    "Assets/Art/Presentation/Calibration/" +
                    "LightStorybookProductionV033Batch",
                    StringComparison.Ordinal)),
                Is.EqualTo(51));
            Assert.That(
                plan.Entries.Select(value => value.RuntimePath)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                Is.EqualTo(plan.Entries.Length));
            Assert.That(
                plan.Entries.All(value =>
                    value.SourcePath.StartsWith(
                        "Assets/Art/Presentation/Calibration/",
                        StringComparison.Ordinal)),
                Is.True);
            Assert.That(
                plan.Entries.All(value =>
                    value.RuntimePath.StartsWith(
                        LightStorybookRuntimePromotionBuilder
                            .RuntimeArtRoot + "/",
                        StringComparison.Ordinal) &&
                    !value.RuntimePath.Contains("..")),
                Is.True);

            var productionArtIds = ReadProductionArtIds();
            var plannedArtIds = new HashSet<string>(
                plan.Entries.Select(value => value.ArtId),
                StringComparer.Ordinal);
            Assert.That(productionArtIds.Count, Is.EqualTo(51));
            Assert.That(
                productionArtIds.All(plannedArtIds.Contains),
                Is.True,
                "Every signed production artwork must be copied.");
        }

        [Test]
        public void CurrentState_IsApprovedPrePromotionOrValidPromotion()
        {
            if (LightStorybookRuntimePromotionBuilder.IsPromoted())
            {
                Assert.That(
                    LightStorybookRuntimePromotionBuilder
                        .ValidatePromotedState(),
                    Is.Empty);
                return;
            }

            var gate = LightStorybookRuntimePromotionGate.Evaluate();
            Assert.That(
                gate.Passed,
                Is.True,
                string.Join("\n", gate.Failures));
        }

        [TestCase("")]
        [TestCase("../escape")]
        [TestCase("unsafe/name")]
        public void RuntimePath_RejectsUnsafeArtId(string artId)
        {
            Assert.Throws<ArgumentException>(() =>
                LightStorybookRuntimePromotionBuilder
                    .GetRuntimeAssetPath(artId));
        }

        private static HashSet<string> ReadProductionArtIds()
        {
            var projectRoot =
                Directory.GetParent(Application.dataPath).FullName;
            var repositoryRoot =
                Directory.GetParent(projectRoot).FullName;
            var path = Path.Combine(
                repositoryRoot,
                "ui-concepts",
                "phase-9c",
                "light-storybook-production-v0.1",
                "PRODUCTION-MANIFEST-v0.3.3.json");
            var manifest = JsonUtility.FromJson<ProductionManifest>(
                File.ReadAllText(path));
            return new HashSet<string>(
                manifest.items.Select(value => value.artId),
                StringComparer.Ordinal);
        }

        [Serializable]
        private sealed class ProductionManifest
        {
            public ProductionManifestItem[] items;
        }

        [Serializable]
        private sealed class ProductionManifestItem
        {
            public string artId;
        }
    }
}

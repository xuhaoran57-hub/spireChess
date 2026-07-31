using System;
using System.Linq;
using NUnit.Framework;
using SpireChess.Editor;

namespace SpireChess.Tests.EditMode
{
    public sealed class LightStorybookRuntimePromotionGateTests
    {
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

            var result = LightStorybookRuntimePromotionGate.Evaluate();

            Assert.That(
                result.TechnicalPassed,
                Is.True,
                string.Join("\n", result.Failures));
            Assert.That(result.ApprovalPassed, Is.True);
            Assert.That(result.Passed, Is.True);
            Assert.That(result.Failures, Is.Empty);
        }

        [Test]
        public void CompleteApproval_PassesApprovalValidation()
        {
            var approval = new RuntimePromotionApproval
            {
                Status = "Approved",
                ApprovedBy = "Project Owner",
                ApprovedAt = "2026-07-31T12:00:00+08:00",
                AccountAgreement =
                    "Personal OpenAI services / Terms of Use",
                InputRightsConfirmed = true,
                AiDisclosureAccepted = true,
                VisualReviewAccepted = true,
                RuntimePromotionAccepted = true
            };

            Assert.That(
                LightStorybookRuntimePromotionGate.ValidateApproval(
                    approval),
                Is.Empty);
        }

        [Test]
        public void PartialApproval_ReportsEveryMissingConfirmation()
        {
            var failures =
                LightStorybookRuntimePromotionGate.ValidateApproval(
                    new RuntimePromotionApproval
                    {
                        Status = "Pending"
                    });

            Assert.That(failures.Length, Is.EqualTo(8));
            Assert.That(
                failures.Any(value =>
                    value.Contains("approvedAt")),
                Is.True);
            Assert.That(
                failures.Any(value =>
                    value.Contains("Runtime promotion")),
                Is.True);
        }

        [Test]
        public void FrozenTargetPolicy_IsAccepted()
        {
            Assert.DoesNotThrow(() =>
                LightStorybookRuntimePromotionGate.ValidateTargetPolicy(
                    new RuntimePromotionTargetPolicy
                    {
                        RuntimeArtRoot =
                            "Assets/Art/Presentation/Runtime/" +
                            "LightStorybookV033",
                        StandaloneTextureFormat = "DXT1",
                        MaxTextureSize = 1024,
                        CompressionQuality = 50,
                        Mipmaps = false,
                        Readable = false,
                        PreserveRuntimeCatalogGuid = true,
                        ForbidCalibrationReferences = true,
                        RequireCleanBuild = true,
                        RequireFullRegression = true,
                        RequireG4VisualReview = true,
                        RequireMemoryEvidence = true
                    }));
        }

        [Test]
        public void UncompressedTargetPolicy_IsRejected()
        {
            var policy = new RuntimePromotionTargetPolicy
            {
                RuntimeArtRoot =
                    "Assets/Art/Presentation/Runtime/" +
                    "LightStorybookV033",
                StandaloneTextureFormat = "RGBA32",
                MaxTextureSize = 2048,
                CompressionQuality = 50,
                PreserveRuntimeCatalogGuid = true,
                ForbidCalibrationReferences = true,
                RequireCleanBuild = true,
                RequireFullRegression = true,
                RequireG4VisualReview = true,
                RequireMemoryEvidence = true
            };

            Assert.Throws<InvalidOperationException>(() =>
                LightStorybookRuntimePromotionGate.ValidateTargetPolicy(
                    policy));
        }
    }
}

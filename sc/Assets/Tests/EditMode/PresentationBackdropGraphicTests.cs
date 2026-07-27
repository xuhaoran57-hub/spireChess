using NUnit.Framework;
using SpireChess.UI;
using UnityEngine;

namespace SpireChess.Tests.EditMode
{
    public sealed class PresentationBackdropGraphicTests
    {
        [Test]
        public void Configure_StoresVariantAndNeverBlocksInput()
        {
            var root = new GameObject(
                "Backdrop",
                typeof(RectTransform),
                typeof(PresentationBackdropGraphic));
            try
            {
                var graphic = root.GetComponent<PresentationBackdropGraphic>();
                var top = new Color(0.1f, 0.2f, 0.3f, 1f);
                var bottom = new Color(0.01f, 0.02f, 0.03f, 1f);
                var accent = new Color(0.8f, 0.6f, 0.2f, 1f);

                graphic.Configure(
                    PresentationBackdropVariant.RunMap,
                    top,
                    bottom,
                    accent);

                Assert.That(
                    graphic.Variant,
                    Is.EqualTo(PresentationBackdropVariant.RunMap));
                Assert.That(graphic.TopColor, Is.EqualTo(top));
                Assert.That(graphic.BottomColor, Is.EqualTo(bottom));
                Assert.That(graphic.AccentColor, Is.EqualTo(accent));
                Assert.That(graphic.raycastTarget, Is.False);
                Assert.That(graphic.HasProductionArtwork, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(PresentationBackdropVariant.MainMenu)]
        [TestCase(PresentationBackdropVariant.Shop)]
        [TestCase(PresentationBackdropVariant.RunMap)]
        [TestCase(PresentationBackdropVariant.Battle)]
        public void ProductionBackdrops_AreImportableSprites(
            PresentationBackdropVariant variant)
        {
            Assert.That(
                PresentationArtworkResources.GetBackdropPath(variant),
                Is.Not.Empty);
            Assert.That(
                PresentationArtworkResources.LoadBackdrop(variant),
                Is.Not.Null);
        }

        [Test]
        public void TranquilGroveEventArtwork_IsImportableSprite()
        {
            Assert.That(
                PresentationArtworkResources.LoadEvent(
                    "event_tranquil_grove"),
                Is.Not.Null);
        }
    }
}

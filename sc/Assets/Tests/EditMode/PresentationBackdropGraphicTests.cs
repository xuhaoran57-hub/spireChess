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
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}

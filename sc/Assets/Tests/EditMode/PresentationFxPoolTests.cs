using NUnit.Framework;
using SpireChess.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SpireChess.Tests.EditMode
{
    public sealed class PresentationFxPoolTests
    {
        private GameObject root;
        private PresentationFxPool pool;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject(
                "FxRoot",
                typeof(RectTransform),
                typeof(PresentationFxPool));
            pool = root.GetComponent<PresentationFxPool>();
            pool.Configure(
                Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),
                3);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Play_UsesFinitePoolAndNeverOwnsInput()
        {
            for (var index = 0; index < 5; index++)
            {
                pool.Play(
                    "反馈 " + index,
                    Color.cyan,
                    new Vector2(index * 10f, 0f));
            }

            Assert.That(pool.Capacity, Is.EqualTo(3));
            Assert.That(pool.ActiveCount, Is.EqualTo(3));
            Assert.That(pool.TotalPlayCount, Is.EqualTo(5));
            foreach (var group in root.GetComponentsInChildren<CanvasGroup>(true))
            {
                Assert.That(group.blocksRaycasts, Is.False);
                Assert.That(group.interactable, Is.False);
            }
            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                Assert.That(graphic.raycastTarget, Is.False);
            }
        }

        [Test]
        public void Advance_CompletesAndReleasesEveryEntry()
        {
            pool.Play(
                "三连",
                Color.yellow,
                Vector2.zero,
                PresentationFxEmphasis.Critical,
                0.5f);

            pool.Advance(0.25f);
            Assert.That(pool.ActiveCount, Is.EqualTo(1));
            pool.Advance(0.25f);
            Assert.That(pool.ActiveCount, Is.Zero);
        }

        [TestCase(0f, 0f)]
        [TestCase(0.12f, 1f)]
        [TestCase(0.50f, 1f)]
        [TestCase(1f, 0f)]
        public void OpacityCurve_IsBoundedAndDeterministic(
            float progress,
            float expected)
        {
            Assert.That(
                PresentationFxPool.EvaluateOpacity(progress),
                Is.EqualTo(expected).Within(0.0001f));
        }
    }
}

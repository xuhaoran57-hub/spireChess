using NUnit.Framework;
using SpireChess.UI.MainMenu;

namespace SpireChess.Tests.EditMode
{
    public sealed class JournalPageFlowTests
    {
        [Test]
        public void CoverToContents_LocksDuplicateInputUntilTheTurnCompletes()
        {
            var flow = new JournalPageFlow(JournalMenuPage.Cover);

            Assert.That(
                flow.TryBeginTransition(JournalMenuPage.Contents),
                Is.True);
            Assert.That(flow.IsInputLocked, Is.True);
            Assert.That(flow.CurrentPage, Is.EqualTo(JournalMenuPage.Cover));
            Assert.That(
                flow.TryBeginTransition(JournalMenuPage.Contents),
                Is.False,
                "A second cover click cannot queue another page turn.");
            Assert.That(flow.CompleteTransition(), Is.True);
            Assert.That(flow.CurrentPage, Is.EqualTo(JournalMenuPage.Contents));
            Assert.That(flow.IsInputLocked, Is.False);
        }

        [Test]
        public void HeroSelection_OnlyOpensFromContentsAndCanRouteToMap()
        {
            var flow = new JournalPageFlow(JournalMenuPage.Cover);

            Assert.That(
                flow.TryBeginTransition(JournalMenuPage.HeroSelection),
                Is.False);
            Assert.That(flow.TryBeginTransition(JournalMenuPage.Contents), Is.True);
            Assert.That(flow.CompleteTransition(), Is.True);
            Assert.That(
                flow.TryBeginTransition(JournalMenuPage.HeroSelection),
                Is.True);
            Assert.That(flow.CompleteTransition(), Is.True);
            Assert.That(flow.TryBeginTransition(JournalMenuPage.Map), Is.True);
            Assert.That(flow.CompleteTransition(), Is.True);
            Assert.That(flow.CurrentPage, Is.EqualTo(JournalMenuPage.Map));
            Assert.That(
                flow.TryBeginTransition(JournalMenuPage.Contents),
                Is.False,
                "Map is a terminal presentation state; return-to-menu creates a new flow.");
        }

        [Test]
        public void CancelledTurn_RestoresTheSourceAndUnlocksInput()
        {
            var flow = new JournalPageFlow(JournalMenuPage.Contents);

            Assert.That(
                flow.TryBeginTransition(JournalMenuPage.HeroSelection),
                Is.True);
            Assert.That(flow.CancelTransition(), Is.True);
            Assert.That(flow.CurrentPage, Is.EqualTo(JournalMenuPage.Contents));
            Assert.That(flow.PendingPage, Is.EqualTo(JournalMenuPage.Contents));
            Assert.That(flow.IsInputLocked, Is.False);
        }
    }
}

using System;
using SpireChess.UI;

namespace SpireChess.UI.Shop
{
    public sealed class ChoiceViewModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public bool CanCancel { get; set; }
        public ChoiceCandidateViewModel[] Candidates { get; set; } =
            Array.Empty<ChoiceCandidateViewModel>();
    }

    public sealed class ChoiceCandidateViewModel
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public CardViewModel Card { get; set; }
        public bool IsInteractable { get; set; } = true;

        public bool IsCard => Card != null;
    }
}

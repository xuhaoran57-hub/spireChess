using UnityEngine;

namespace SpireChess.UI
{
    [CreateAssetMenu(
        fileName = "PresentationTheme",
        menuName = "Spire Chess/Presentation/Theme")]
    public sealed class PresentationTheme : ScriptableObject
    {
        [Header("Battle standee")]
        [SerializeField] private Color forgeSoulPortraitTint =
            new Color(0.46f, 0.22f, 0.14f, 1f);
        [SerializeField] private Color wildSpiritPortraitTint =
            new Color(0.20f, 0.42f, 0.24f, 1f);
        [SerializeField] private Color starboundPortraitTint =
            new Color(0.20f, 0.30f, 0.56f, 1f);
        [SerializeField] private Color wayfarerPortraitTint =
            new Color(0.42f, 0.34f, 0.22f, 1f);
        [SerializeField] private Color fallbackPortraitTint =
            new Color(0.30f, 0.27f, 0.33f, 1f);
        [SerializeField] private Color normalFrameTint = Color.white;
        [SerializeField] private Color goldenFrameTint =
            new Color(1f, 0.90f, 0.62f, 1f);
        [SerializeField] private Color legalTargetTint =
            new Color(0.38f, 0.82f, 0.58f, 0.78f);
        [SerializeField] private Color selectedTargetTint =
            new Color(0.98f, 0.68f, 0.22f, 0.88f);

        public Color NormalFrameTint => normalFrameTint;
        public Color GoldenFrameTint => goldenFrameTint;
        public Color LegalTargetTint => legalTargetTint;
        public Color SelectedTargetTint => selectedTargetTint;

        public Color GetPortraitTint(string raceText)
        {
            switch (raceText)
            {
                case "铸魂": return forgeSoulPortraitTint;
                case "荒灵": return wildSpiritPortraitTint;
                case "星契": return starboundPortraitTint;
                case "旅团": return wayfarerPortraitTint;
                default: return fallbackPortraitTint;
            }
        }
    }
}

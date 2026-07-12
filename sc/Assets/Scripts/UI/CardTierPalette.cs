using UnityEngine;

namespace SpireChess.UI
{
    public static class CardTierPalette
    {
        public static Color GetBackground(int tier)
        {
            switch (tier)
            {
                case 2: return new Color(0.55f, 0.78f, 0.60f, 1f);
                case 3: return new Color(0.52f, 0.68f, 0.90f, 1f);
                case 4: return new Color(0.70f, 0.58f, 0.88f, 1f);
                case 5: return new Color(0.94f, 0.70f, 0.36f, 1f);
                default: return new Color(0.82f, 0.80f, 0.75f, 1f);
            }
        }

        public static Color GetHeader(int tier)
        {
            switch (tier)
            {
                case 2: return new Color(0.14f, 0.32f, 0.18f, 1f);
                case 3: return new Color(0.12f, 0.24f, 0.40f, 1f);
                case 4: return new Color(0.30f, 0.17f, 0.44f, 1f);
                case 5: return new Color(0.43f, 0.26f, 0.08f, 1f);
                default: return new Color(0.22f, 0.21f, 0.19f, 1f);
            }
        }
    }
}

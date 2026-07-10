using System;

namespace SpireChess.Shop
{
    public static class ShopEconomyRules
    {
        public const int MaximumTavernTier = 5;
        public const int BenchSlotCount = 5;
        public const int BattleSlotCount = 5;
        public const int RefreshCost = 1;
        public const int MinionPurchaseCost = 3;
        public const int SpellPurchaseCost = 1;
        public const int MinionSellValue = 1;

        public static int GetRoundBudget(int round)
        {
            if (round < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(round));
            }

            return Math.Min(round + 2, 10);
        }

        public static int GetMinionSlotCount(int tavernTier)
        {
            ValidateTavernTier(tavernTier);
            if (tavernTier <= 2)
            {
                return 2;
            }

            return tavernTier <= 4 ? 3 : 4;
        }

        public static int GetUpgradeBaseCost(int tavernTier)
        {
            ValidateTavernTier(tavernTier);
            switch (tavernTier)
            {
                case 1:
                    return 5;
                case 2:
                    return 7;
                case 3:
                    return 9;
                case 4:
                    return 10;
                default:
                    return 0;
            }
        }

        public static int GetPoolCopiesPerMinion(int minionTier)
        {
            ValidateTavernTier(minionTier);
            return 9 - minionTier;
        }

        private static void ValidateTavernTier(int tavernTier)
        {
            if (tavernTier < 1 || tavernTier > MaximumTavernTier)
            {
                throw new ArgumentOutOfRangeException(nameof(tavernTier));
            }
        }
    }
}

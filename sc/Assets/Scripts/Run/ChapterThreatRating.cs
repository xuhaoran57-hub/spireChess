using System;

namespace SpireChess.Run
{
    public static class ChapterThreatRating
    {
        public const int Minimum = 1;
        public const int Maximum = 5;

        public static int Calculate(
            int floor,
            int combatIndex,
            RunNodeType nodeType,
            string routeTag,
            int damageBonus)
        {
            if (floor < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(floor));
            }
            if (combatIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(combatIndex));
            }
            if (damageBonus < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damageBonus));
            }

            int rating;
            if (nodeType == RunNodeType.Boss)
            {
                rating = floor + 3;
            }
            else if (combatIndex == 4 &&
                     !string.IsNullOrWhiteSpace(routeTag))
            {
                rating = CalculateRouteRating(floor, routeTag);
            }
            else
            {
                switch (combatIndex)
                {
                    case 1:
                        rating = floor;
                        break;
                    case 2:
                        rating = floor + (damageBonus > 0 ? 1 : 0);
                        break;
                    case 3:
                        rating = floor + 1;
                        break;
                    default:
                        rating = floor + 2;
                        break;
                }

                if (nodeType == RunNodeType.Elite)
                {
                    rating = Math.Max(rating, floor + 2);
                }
            }

            return Math.Max(Minimum, Math.Min(Maximum, rating));
        }

        public static string ToStars(int rating)
        {
            if (rating < Minimum || rating > Maximum)
            {
                throw new ArgumentOutOfRangeException(nameof(rating));
            }

            return new string('\u2605', rating);
        }

        private static int CalculateRouteRating(int floor, string routeTag)
        {
            if (string.Equals(
                    routeTag,
                    "Conservative",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Math.Max(2, floor);
            }
            if (string.Equals(
                    routeTag,
                    "Adventure",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Math.Max(3, floor + 1);
            }
            if (string.Equals(
                    routeTag,
                    "Aggressive",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Math.Max(4, floor + 2);
            }
            return floor + 1;
        }
    }
}

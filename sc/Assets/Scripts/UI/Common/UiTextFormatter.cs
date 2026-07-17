using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SpireChess.UI
{
    public static class UiTextFormatter
    {
        public const string Ellipsis = "…";
        public const int FullAbilityLabelLimit = 3;
        public const int CompactAbilityLabelLimit = 2;
        public const int CurrentMinionDescriptionLimit = 66;
        public const int CurrentSpellDescriptionLimit = 45;

        public static string NormalizeWhitespace(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length);
            var previousWasSpace = false;
            for (var i = 0; i < value.Length; i++)
            {
                var character = value[i];
                if (character == '\r')
                {
                    if (i + 1 < value.Length && value[i + 1] == '\n')
                    {
                        i++;
                    }

                    builder.Append('\n');
                    previousWasSpace = false;
                    continue;
                }

                if (character == '\n')
                {
                    builder.Append('\n');
                    previousWasSpace = false;
                    continue;
                }

                if (char.IsWhiteSpace(character))
                {
                    if (!previousWasSpace)
                    {
                        builder.Append(' ');
                        previousWasSpace = true;
                    }

                    continue;
                }

                builder.Append(character);
                previousWasSpace = false;
            }

            return builder.ToString();
        }

        public static string ToSingleLine(string value)
        {
            return NormalizeWhitespace(
                NormalizeWhitespace(value).Replace('\n', ' '));
        }

        public static string[] FormatAbilityLabels(
            IEnumerable<string> labels,
            CardDisplayMode displayMode)
        {
            var normalized = (labels ?? Array.Empty<string>())
                .Select(ToSingleLine)
                .Where(label => !string.IsNullOrWhiteSpace(label))
                .ToArray();
            var limit = displayMode == CardDisplayMode.Full
                ? FullAbilityLabelLimit
                : CompactAbilityLabelLimit;
            if (normalized.Length <= limit)
            {
                return normalized;
            }

            var visible = normalized.Take(limit - 1).ToList();
            visible.Add("+" + (normalized.Length - limit + 1));
            return visible.ToArray();
        }

        public static int GetDescriptionMaxLines(
            CardDisplayMode displayMode,
            bool isMinion,
            bool hasProgress)
        {
            if (!isMinion)
            {
                return displayMode == CardDisplayMode.Full ? 5 : 4;
            }

            if (hasProgress)
            {
                return 2;
            }

            return displayMode == CardDisplayMode.Full ? 4 : 3;
        }

        public static string EllipsizeName(
            string value,
            Func<string, bool> fits)
        {
            return EllipsizeToFit(ToSingleLine(value), fits);
        }

        public static string EllipsizeDescription(
            string value,
            CardDisplayMode displayMode,
            Func<string, bool> fits)
        {
            if (fits == null)
            {
                throw new ArgumentNullException(nameof(fits));
            }

            var normalized = NormalizeWhitespace(value);
            if (displayMode == CardDisplayMode.Full)
            {
                if (!fits(normalized))
                {
                    throw new InvalidOperationException(
                        "Full card description does not fit its layout contract.");
                }

                return normalized;
            }

            return EllipsizeToFit(normalized, fits);
        }

        public static string EllipsizeToFit(
            string value,
            Func<string, bool> fits)
        {
            if (fits == null)
            {
                throw new ArgumentNullException(nameof(fits));
            }

            value = value ?? string.Empty;
            if (fits(value))
            {
                return value;
            }

            if (!fits(Ellipsis))
            {
                throw new InvalidOperationException(
                    "The ellipsis does not fit the target text area.");
            }

            var elementStarts = StringInfo.ParseCombiningCharacters(value);
            var minimum = 0;
            var maximum = Math.Max(0, elementStarts.Length - 1);
            var bestPrefixLength = 0;
            while (minimum <= maximum)
            {
                var candidatePrefixLength = minimum + (maximum - minimum) / 2;
                var candidate = PrefixByTextElements(
                    value,
                    elementStarts,
                    candidatePrefixLength) + Ellipsis;
                if (fits(candidate))
                {
                    bestPrefixLength = candidatePrefixLength;
                    minimum = candidatePrefixLength + 1;
                }
                else
                {
                    maximum = candidatePrefixLength - 1;
                }
            }

            return PrefixByTextElements(
                value,
                elementStarts,
                bestPrefixLength) + Ellipsis;
        }

        public static int CountTextElements(string value)
        {
            return string.IsNullOrEmpty(value)
                ? 0
                : StringInfo.ParseCombiningCharacters(value).Length;
        }

        private static string PrefixByTextElements(
            string value,
            IReadOnlyList<int> elementStarts,
            int elementCount)
        {
            if (elementCount <= 0)
            {
                return string.Empty;
            }

            var endIndex = elementCount < elementStarts.Count
                ? elementStarts[elementCount]
                : value.Length;
            return value.Substring(0, endIndex);
        }
    }
}

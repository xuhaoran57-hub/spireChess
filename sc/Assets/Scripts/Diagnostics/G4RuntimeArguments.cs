using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SpireChess.Diagnostics
{
    public static class G4RuntimeArguments
    {
        public const string AcceptanceFlag = "-g4Acceptance";
        public const string PerformanceFlag = "-g4Perf";
        public const string SaveRootArgument = "-g4SaveRoot";
        public const string OutputArgument = "-g4PerfOutput";
        public const string EvidenceOutputArgument = "-g4EvidenceOutput";
        public const string RunIdArgument = "-g4RunId";
        public const string ResolutionArgument = "-g4Resolution";
        public const string QualityArgument = "-g4Quality";
        public const string DurationArgument = "-g4PerfDuration";
        public const string WarmupArgument = "-g4PerfWarmup";
        public const string SampleIntervalArgument = "-g4PerfSampleInterval";
        public const string AcceptanceSeedArgument = "-g4AcceptanceSeed";
        public const string AutoQuitFlag = "-g4PerfAutoQuit";
        public const string NoScreenshotsFlag = "-g4NoScreenshots";
        public const string IsolationMarkerFileName =
            ".spirechess-g4-isolated-save";
        public const string IsolationMarkerContents =
            "spire-chess-g4-isolated-save-v1";

        public static bool IsAcceptanceRequested =>
            HasFlag(AcceptanceFlag);

        public static bool IsPerformanceRequested =>
            HasFlag(PerformanceFlag) || IsAcceptanceRequested;

        public static bool HasFlag(string name)
        {
            return Environment.GetCommandLineArgs().Any(value =>
                string.Equals(value, name, StringComparison.OrdinalIgnoreCase));
        }

        public static string Read(string name)
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(
                        arguments[index],
                        name,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[index + 1];
                }
            }

            return null;
        }

        public static int ReadInt(string name, int fallback, int minimum, int maximum)
        {
            var raw = Read(name);
            if (!int.TryParse(
                    raw,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var parsed))
            {
                return fallback;
            }

            return Math.Max(minimum, Math.Min(maximum, parsed));
        }

        public static float ReadFloat(
            string name,
            float fallback,
            float minimum,
            float maximum)
        {
            var raw = Read(name);
            if (!float.TryParse(
                    raw,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var parsed) ||
                float.IsNaN(parsed) ||
                float.IsInfinity(parsed))
            {
                return fallback;
            }

            return Math.Max(minimum, Math.Min(maximum, parsed));
        }

        public static bool TryReadResolution(out int width, out int height)
        {
            width = 0;
            height = 0;
            var raw = Read(ResolutionArgument);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            var separator = raw.IndexOf('x');
            if (separator < 0)
            {
                separator = raw.IndexOf('X');
            }

            return separator > 0 &&
                   int.TryParse(
                       raw.Substring(0, separator),
                       NumberStyles.Integer,
                       CultureInfo.InvariantCulture,
                       out width) &&
                   int.TryParse(
                       raw.Substring(separator + 1),
                       NumberStyles.Integer,
                       CultureInfo.InvariantCulture,
                       out height) &&
                   width >= 640 &&
                   width <= 7680 &&
                   height >= 480 &&
                   height <= 4320;
        }

        public static string RequireAbsolutePath(string name)
        {
            var raw = Read(name);
            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new InvalidOperationException(
                    $"G4 argument '{name}' requires a non-empty path.");
            }

            if (!Path.IsPathRooted(raw))
            {
                throw new InvalidOperationException(
                    $"G4 argument '{name}' must be an absolute path: '{raw}'.");
            }

            return Path.GetFullPath(raw);
        }

        public static string RequirePristineIsolatedSaveRoot()
        {
            var resolved = RequireAbsolutePath(SaveRootArgument);
            if (!Directory.Exists(resolved))
            {
                throw new InvalidOperationException(
                    $"G4 isolated save root does not exist: '{resolved}'.");
            }

            var attributes = File.GetAttributes(resolved);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidOperationException(
                    "G4 isolated save root cannot be a symbolic link, " +
                    $"junction, or other reparse point: '{resolved}'.");
            }

            var productionRoot = Path.GetFullPath(
                UnityEngine.Application.persistentDataPath);
            if (string.Equals(
                    TrimDirectorySeparators(resolved),
                    TrimDirectorySeparators(productionRoot),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "G4 isolated save root cannot equal the normal " +
                    $"persistentDataPath: '{resolved}'.");
            }

            var markerPath = Path.Combine(
                resolved,
                IsolationMarkerFileName);
            if (!File.Exists(markerPath) ||
                !string.Equals(
                    File.ReadAllText(markerPath).Trim(),
                    IsolationMarkerContents,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "G4 isolated save root is missing its exact ownership " +
                    $"marker: '{markerPath}'.");
            }

            var unexpectedEntries = Directory
                .EnumerateFileSystemEntries(resolved)
                .Where(path => !string.Equals(
                    Path.GetFileName(path),
                    IsolationMarkerFileName,
                    StringComparison.Ordinal))
                .Take(1)
                .ToArray();
            if (unexpectedEntries.Length > 0)
            {
                throw new InvalidOperationException(
                    "G4 isolated save root must be pristine before startup; " +
                    $"unexpected entry: '{unexpectedEntries[0]}'.");
            }

            return resolved;
        }

        private static string TrimDirectorySeparators(string path)
        {
            return (path ?? string.Empty).TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
        }

        public static string SanitizeFileName(string value, string fallback)
        {
            var candidate = string.IsNullOrWhiteSpace(value)
                ? fallback
                : value.Trim();
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                candidate = candidate.Replace(invalid, '-');
            }

            return string.IsNullOrWhiteSpace(candidate)
                ? fallback
                : candidate;
        }
    }
}

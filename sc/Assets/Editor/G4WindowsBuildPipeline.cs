using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SpireChess.Editor
{
    public static class G4WindowsBuildPipeline
    {
        private const string BuildOutputArgument = "-g4BuildOutput";
        private const string BuildIdArgument = "-g4BuildId";
        private const string GitCommitArgument = "-g4GitCommit";
        private const string GitDirtyArgument = "-g4GitDirty";
        private const string CleanBuildFlag = "-g4CleanBuild";
        private const string AcceptanceCompanyName =
            "SpireChess.G4Validation";
        private const string AcceptanceProductName =
            "SpireChess G4 Acceptance";

        private static readonly string[] RequiredScenes =
        {
            "Assets/Scenes/Boot.unity",
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/RunTest.unity",
            "Assets/Scenes/ShopTest.unity",
            "Assets/Scenes/BattleTest.unity"
        };

        [Serializable]
        private sealed class BuildManifest
        {
            public string schemaVersion;
            public string buildId;
            public string builtAtUtc;
            public string gitCommit;
            public bool sourceTreeDirty;
            public string unityVersion;
            public string applicationVersion;
            public string companyName;
            public string productName;
            public string target;
            public string architecture;
            public bool developmentBuild;
            public bool cleanBuild;
            public string outputPath;
            public string[] scenes;
            public string result;
            public ulong totalSizeBytes;
            public double totalBuildSeconds;
            public string executableSha256;
            public BuildFileRecord[] buildFiles;
        }

        [Serializable]
        private sealed class BuildFileRecord
        {
            public string relativePath;
            public long sizeBytes;
            public string sha256;
        }

        [MenuItem("Spire Chess/G4/Build Windows x64 Development Player")]
        public static void BuildDevelopmentPlayer()
        {
            BuildDevelopmentPlayerInternal();
        }

        public static void BuildDevelopmentPlayerFromCommandLine()
        {
            try
            {
                BuildDevelopmentPlayerInternal();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        private static void BuildDevelopmentPlayerInternal()
        {
            ValidateScenes();
            var outputPath = ResolveOutputPath();
            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new InvalidOperationException(
                    $"Invalid G4 build output path: {outputPath}");
            }
            Directory.CreateDirectory(outputDirectory);

            var cleanBuild = HasFlag(CleanBuildFlag);
            var options = BuildOptions.Development |
                          BuildOptions.StrictMode |
                          BuildOptions.DetailedBuildReport;
            if (cleanBuild)
            {
                options |= BuildOptions.CleanBuildCache;
            }

            var originalCompanyName = PlayerSettings.companyName;
            var originalProductName = PlayerSettings.productName;
            BuildReport report;
            try
            {
                PlayerSettings.companyName = AcceptanceCompanyName;
                PlayerSettings.productName = AcceptanceProductName;
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = RequiredScenes,
                    locationPathName = outputPath,
                    target = BuildTarget.StandaloneWindows64,
                    targetGroup = BuildTargetGroup.Standalone,
                    options = options
                });
            }
            finally
            {
                PlayerSettings.companyName = originalCompanyName;
                PlayerSettings.productName = originalProductName;
            }

            if (report == null ||
                report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                var result = report == null
                    ? "NoBuildReport"
                    : report.summary.result.ToString();
                throw new InvalidOperationException(
                    $"G4 Windows x64 Development Build failed: {result}.");
            }

            var manifestPath = Path.Combine(
                outputDirectory,
                "g4-build-manifest.json");
            var buildFiles = Directory
                .GetFiles(
                    outputDirectory,
                    "*",
                    SearchOption.AllDirectories)
                .Where(path => !string.Equals(
                    Path.GetFullPath(path),
                    Path.GetFullPath(manifestPath),
                    StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(path => new BuildFileRecord
                {
                    relativePath = ToRelativeBuildPath(
                        outputDirectory,
                        path),
                    sizeBytes = new FileInfo(path).Length,
                    sha256 = ComputeSha256(path)
                })
                .ToArray();
            var manifest = new BuildManifest
            {
                schemaVersion = "spire-chess-g4-build-v1",
                buildId = ReadArgument(BuildIdArgument) ??
                          DateTime.UtcNow.ToString(
                              "yyyyMMdd-HHmmss",
                              CultureInfo.InvariantCulture),
                builtAtUtc = DateTime.UtcNow.ToString(
                    "O",
                    CultureInfo.InvariantCulture),
                gitCommit = ReadArgument(GitCommitArgument) ?? string.Empty,
                sourceTreeDirty = string.Equals(
                    ReadArgument(GitDirtyArgument),
                    "true",
                    StringComparison.OrdinalIgnoreCase),
                unityVersion = Application.unityVersion,
                applicationVersion = PlayerSettings.bundleVersion,
                companyName = AcceptanceCompanyName,
                productName = AcceptanceProductName,
                target = BuildTarget.StandaloneWindows64.ToString(),
                architecture = "x86_64",
                developmentBuild = true,
                cleanBuild = cleanBuild,
                outputPath = outputPath,
                scenes = RequiredScenes.ToArray(),
                result = report.summary.result.ToString(),
                totalSizeBytes = report.summary.totalSize,
                totalBuildSeconds = report.summary.totalTime.TotalSeconds,
                executableSha256 = ComputeSha256(outputPath),
                buildFiles = buildFiles
            };
            File.WriteAllText(
                manifestPath,
                JsonUtility.ToJson(manifest, true));
            Debug.Log(
                $"[G4] Windows x64 Development Player built: {outputPath}. " +
                $"Manifest: {manifestPath}.");
        }

        private static void ValidateScenes()
        {
            var missing = RequiredScenes
                .Where(path =>
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                .ToArray();
            if (missing.Length > 0)
            {
                throw new InvalidOperationException(
                    "G4 build is missing required formal scenes: " +
                    string.Join(", ", missing));
            }
        }

        private static string ResolveOutputPath()
        {
            var projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName ??
                Application.dataPath;
            var requested = ReadArgument(BuildOutputArgument);
            var candidate = string.IsNullOrWhiteSpace(requested)
                ? Path.Combine(
                    projectRoot,
                    "Builds",
                    "G4",
                    "Windows-x64",
                    "SpireChess.exe")
                : Path.IsPathRooted(requested)
                    ? requested
                    : Path.Combine(projectRoot, requested);
            var resolved = Path.GetFullPath(candidate);
            if (!string.Equals(
                    Path.GetExtension(resolved),
                    ".exe",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"{BuildOutputArgument} must end with .exe: {resolved}");
            }

            return resolved;
        }

        private static string ReadArgument(string name)
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

        private static bool HasFlag(string name)
        {
            return Environment.GetCommandLineArgs().Any(value =>
                string.Equals(value, name, StringComparison.OrdinalIgnoreCase));
        }

        private static string ToRelativeBuildPath(
            string root,
            string path)
        {
            var prefix = Path.GetFullPath(root).TrimEnd(
                             Path.DirectorySeparatorChar,
                             Path.AltDirectorySeparatorChar) +
                         Path.DirectorySeparatorChar;
            var resolved = Path.GetFullPath(path);
            if (!resolved.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Build file escapes output directory: {resolved}");
            }

            return resolved
                .Substring(prefix.Length)
                .Replace(Path.DirectorySeparatorChar, '/');
        }

        private static string ComputeSha256(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                return string.Concat(
                    sha.ComputeHash(stream)
                        .Select(value => value.ToString(
                            "x2",
                            CultureInfo.InvariantCulture)));
            }
        }
    }
}

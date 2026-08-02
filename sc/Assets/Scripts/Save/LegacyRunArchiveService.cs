using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

namespace SpireChess.Save
{
    public sealed class LegacyRunArchiveResult
    {
        public LegacyRunArchiveResult(
            bool legacyDetected,
            bool archivedNow,
            string archiveRelativePath = null,
            IReadOnlyList<string> archivedFileNames = null,
            string diagnostic = null)
        {
            LegacyDetected = legacyDetected;
            ArchivedNow = archivedNow;
            ArchiveRelativePath = archiveRelativePath ?? string.Empty;
            ArchivedFileNames = archivedFileNames ?? Array.Empty<string>();
            Diagnostic = diagnostic ?? string.Empty;
        }

        public bool LegacyDetected { get; }
        public bool ArchivedNow { get; }
        public string ArchiveRelativePath { get; }
        public IReadOnlyList<string> ArchivedFileNames { get; }
        public string Diagnostic { get; }
        public bool Succeeded =>
            !LegacyDetected || !string.IsNullOrWhiteSpace(ArchiveRelativePath);
    }

    public sealed class LegacyRunArchiveService
    {
        public const string LegacyRootDirectoryName = "Legacy";
        public const string LegacyVersionDirectoryName = "v0.3.3";

        private readonly string saveRootPath;
        private readonly Func<DateTime> utcNow;
        private readonly AtomicFileSaveStorage runStorage;

        public LegacyRunArchiveService(
            string saveRootPath,
            Func<DateTime> utcNow = null)
        {
            if (string.IsNullOrWhiteSpace(saveRootPath))
            {
                throw new ArgumentException(
                    "Save root path is required.",
                    nameof(saveRootPath));
            }

            this.saveRootPath = Path.GetFullPath(saveRootPath);
            this.utcNow = utcNow ?? (() => DateTime.UtcNow);
            runStorage = new AtomicFileSaveStorage(
                this.saveRootPath,
                AtomicFileSaveStorage.LegacySlotFileName,
                this.utcNow);
        }

        public LegacyRunArchiveResult ArchiveIfNeeded(bool alreadyCompleted)
        {
            if (alreadyCompleted)
            {
                return new LegacyRunArchiveResult(false, false);
            }

            var sources = runStorage.EnumerateSlotFiles()
                .Where(File.Exists)
                .OrderBy(Path.GetFileName, StringComparer.Ordinal)
                .ToArray();
            if (sources.Length == 0)
            {
                var existing = FindLatestCompletedArchive();
                return string.IsNullOrWhiteSpace(existing)
                    ? new LegacyRunArchiveResult(false, false)
                    : new LegacyRunArchiveResult(
                        true,
                        false,
                        ToRelativePath(existing),
                        Directory.GetFiles(existing)
                            .Select(Path.GetFileName)
                            .OrderBy(value => value, StringComparer.Ordinal)
                            .ToArray());
            }

            if (!LooksLikeLegacyV033Slot(sources))
            {
                return new LegacyRunArchiveResult(false, false);
            }

            var matching = FindMatchingCompletedArchive(sources);
            if (!string.IsNullOrWhiteSpace(matching))
            {
                return new LegacyRunArchiveResult(
                    true,
                    false,
                    ToRelativePath(matching),
                    sources.Select(Path.GetFileName).ToArray());
            }

            var legacyRoot = Path.Combine(
                saveRootPath,
                LegacyRootDirectoryName,
                LegacyVersionDirectoryName);
            var target = BuildUniqueTargetPath(legacyRoot);
            var staging = target + ".partial";
            try
            {
                Directory.CreateDirectory(staging);
                foreach (var source in sources)
                {
                    File.Copy(
                        source,
                        Path.Combine(staging, Path.GetFileName(source)),
                        false);
                }

                Directory.Move(staging, target);
                return new LegacyRunArchiveResult(
                    true,
                    true,
                    ToRelativePath(target),
                    sources.Select(Path.GetFileName).ToArray());
            }
            catch (Exception exception)
                when (exception is IOException ||
                      exception is UnauthorizedAccessException)
            {
                return new LegacyRunArchiveResult(
                    true,
                    false,
                    diagnostic:
                    $"{exception.GetType().Name}: {exception.Message}");
            }
        }

        private bool LooksLikeLegacyV033Slot(IReadOnlyList<string> sources)
        {
            var mainSchema = ReadRunSchema(runStorage.MainPath);
            if (mainSchema.HasValue)
            {
                return mainSchema.Value < RunSaveDocumentV1.CurrentSchemaVersion;
            }

            var backupSchema = ReadRunSchema(runStorage.BackupPath);
            if (backupSchema.HasValue)
            {
                return backupSchema.Value < RunSaveDocumentV1.CurrentSchemaVersion;
            }

            return sources.Count > 0;
        }

        private static int? ReadRunSchema(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                var document = JObject.Parse(File.ReadAllText(path));
                if (!string.Equals(
                        document.Value<string>("format"),
                        RunSaveDocumentV1.FormatId,
                        StringComparison.Ordinal))
                {
                    return null;
                }

                return document.Value<int?>("schemaVersion");
            }
            catch (Exception exception)
                when (exception is IOException ||
                      exception is UnauthorizedAccessException ||
                      exception is Newtonsoft.Json.JsonException)
            {
                return null;
            }
        }

        private string FindMatchingCompletedArchive(IReadOnlyList<string> sources)
        {
            var root = Path.Combine(
                saveRootPath,
                LegacyRootDirectoryName,
                LegacyVersionDirectoryName);
            if (!Directory.Exists(root))
            {
                return null;
            }

            foreach (var directory in Directory.GetDirectories(root)
                         .Where(value => !value.EndsWith(
                             ".partial",
                             StringComparison.OrdinalIgnoreCase))
                         .OrderByDescending(value => value, StringComparer.Ordinal))
            {
                var matches = sources.All(source =>
                {
                    var archived = Path.Combine(directory, Path.GetFileName(source));
                    return File.Exists(archived) && FilesMatch(source, archived);
                });
                if (matches)
                {
                    return directory;
                }
            }

            return null;
        }

        private string FindLatestCompletedArchive()
        {
            var root = Path.Combine(
                saveRootPath,
                LegacyRootDirectoryName,
                LegacyVersionDirectoryName);
            return !Directory.Exists(root)
                ? null
                : Directory.GetDirectories(root)
                    .Where(value =>
                        !value.EndsWith(
                            ".partial",
                            StringComparison.OrdinalIgnoreCase) &&
                        Directory.GetFiles(value).Length > 0)
                    .OrderByDescending(value => value, StringComparer.Ordinal)
                    .FirstOrDefault();
        }

        private string BuildUniqueTargetPath(string legacyRoot)
        {
            Directory.CreateDirectory(legacyRoot);
            var timestamp = utcNow().ToString("yyyyMMdd-HHmmss-fffffff");
            var target = Path.Combine(legacyRoot, timestamp);
            var suffix = 1;
            while (Directory.Exists(target) || Directory.Exists(target + ".partial"))
            {
                target = Path.Combine(legacyRoot, $"{timestamp}-{suffix:D2}");
                suffix++;
            }

            return target;
        }

        private string ToRelativePath(string path)
        {
            var root = saveRootPath.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var fullPath = Path.GetFullPath(path);
            return fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                ? fullPath.Substring(root.Length)
                : fullPath;
        }

        private static bool FilesMatch(string left, string right)
        {
            var leftInfo = new FileInfo(left);
            var rightInfo = new FileInfo(right);
            if (leftInfo.Length != rightInfo.Length)
            {
                return false;
            }

            using (var algorithm = SHA256.Create())
            using (var leftStream = File.OpenRead(left))
            using (var rightStream = File.OpenRead(right))
            {
                var leftHash = algorithm.ComputeHash(leftStream);
                algorithm.Initialize();
                var rightHash = algorithm.ComputeHash(rightStream);
                return leftHash.SequenceEqual(rightHash);
            }
        }
    }
}

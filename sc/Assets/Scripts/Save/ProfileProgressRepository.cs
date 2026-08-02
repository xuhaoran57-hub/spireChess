using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SpireChess.Run;
using SpireChess.Utils;

namespace SpireChess.Save
{
    public sealed class ProfileProgressRepository
    {
        public const string ProfileFileName = "profile-progress-v1.json";

        private readonly AtomicFileSaveStorage storage;
        private readonly Func<DateTime> utcNow;
        private readonly JsonSerializerSettings settings;

        public ProfileProgressRepository(
            string rootPath,
            Func<DateTime> utcNow = null)
        {
            this.utcNow = utcNow ?? (() => DateTime.UtcNow);
            storage = new AtomicFileSaveStorage(
                rootPath,
                ProfileFileName,
                this.utcNow);
            settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Include
            };
        }

        public AtomicFileSaveStorage Storage => storage;
        public long CurrentRevision { get; private set; }
        public bool Exists => storage.MainExists || storage.BackupExists;

        public ProfileProgressLoadResult LoadOrCreate(
            ProfileProgressV1 initialProgress = null)
        {
            var loaded = Load();
            if (loaded.IsUsable)
            {
                return loaded;
            }

            if (loaded.Status != ProfileProgressLoadStatus.Missing)
            {
                return loaded;
            }

            var document = Save(
                initialProgress ?? ProfileProgressV1.CreateDefault(),
                1);
            return new ProfileProgressLoadResult(
                ProfileProgressLoadStatus.Created,
                document);
        }

        public ProfileProgressLoadResult Load()
        {
            var main = ReadCandidate(true);
            if (main.Status == ProfileProgressLoadStatus.Valid)
            {
                CurrentRevision = main.Document.Revision;
                return main;
            }

            var backup = ReadCandidate(false);
            if (backup.Status == ProfileProgressLoadStatus.Valid)
            {
                CurrentRevision = backup.Document.Revision;
                try
                {
                    storage.RepairMainFromBackup();
                }
                catch (Exception exception)
                    when (exception is IOException ||
                          exception is UnauthorizedAccessException)
                {
                    return new ProfileProgressLoadResult(
                        ProfileProgressLoadStatus.IoFailure,
                        backup.Document,
                        "Profile backup is valid but main repair failed: " +
                        exception.Message);
                }

                return new ProfileProgressLoadResult(
                    ProfileProgressLoadStatus.RecoveredFromBackup,
                    backup.Document,
                    "Recovered profile progress from validated backup.");
            }

            if (main.Status != ProfileProgressLoadStatus.Missing)
            {
                return main;
            }

            return backup.Status == ProfileProgressLoadStatus.Missing
                ? new ProfileProgressLoadResult(ProfileProgressLoadStatus.Missing)
                : backup;
        }

        public ProfileProgressDocumentV1 Save(ProfileProgressV1 progress)
        {
            return Save(progress, Math.Max(1, CurrentRevision + 1));
        }

        public ProfileProgressDocumentV1 Save(
            ProfileProgressV1 progress,
            long revision)
        {
            if (revision < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(revision));
            }

            var normalized = Normalize(progress);
            var errors = Validate(normalized);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join("\n", errors));
            }

            var document = new ProfileProgressDocumentV1
            {
                SavedAtUtc = utcNow(),
                Revision = revision,
                Progress = normalized,
                ProgressSha256 = ComputeProgressHash(normalized)
            };
            storage.WriteAtomic(JsonConvert.SerializeObject(document, settings));
            CurrentRevision = revision;
            return document;
        }

        public static string ComputeProgressHash(ProfileProgressV1 progress)
        {
            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            return CanonicalJson.ComputeTokenSha256(JToken.FromObject(progress));
        }

        public static ProfileProgressV1 Clone(ProfileProgressV1 progress)
        {
            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            return JsonConvert.DeserializeObject<ProfileProgressV1>(
                JsonConvert.SerializeObject(progress));
        }

        private ProfileProgressLoadResult ReadCandidate(bool main)
        {
            var exists = main ? storage.MainExists : storage.BackupExists;
            if (!exists)
            {
                return new ProfileProgressLoadResult(
                    ProfileProgressLoadStatus.Missing);
            }

            ProfileProgressDocumentV1 document;
            try
            {
                document = JsonConvert.DeserializeObject<ProfileProgressDocumentV1>(
                    main ? storage.ReadMain() : storage.ReadBackup(),
                    settings);
            }
            catch (JsonException exception)
            {
                return Failure(ProfileProgressLoadStatus.CorruptJson, exception);
            }
            catch (IOException exception)
            {
                return Failure(ProfileProgressLoadStatus.IoFailure, exception);
            }
            catch (UnauthorizedAccessException exception)
            {
                return Failure(ProfileProgressLoadStatus.IoFailure, exception);
            }

            if (document == null ||
                !string.Equals(
                    document.Format,
                    ProfileProgressDocumentV1.FormatId,
                    StringComparison.Ordinal))
            {
                return new ProfileProgressLoadResult(
                    ProfileProgressLoadStatus.CorruptJson,
                    document,
                    "Profile format id is missing or invalid.");
            }

            if (document.SchemaVersion != ProfileProgressV1.CurrentSchemaVersion ||
                document.Progress?.SchemaVersion !=
                ProfileProgressV1.CurrentSchemaVersion)
            {
                return new ProfileProgressLoadResult(
                    ProfileProgressLoadStatus.UnsupportedSchema,
                    document,
                    $"Unsupported profile schema {document.SchemaVersion}/" +
                    $"{document.Progress?.SchemaVersion}.");
            }

            if (document.Progress == null ||
                !string.Equals(
                    document.ProgressSha256,
                    ComputeProgressHash(document.Progress),
                    StringComparison.OrdinalIgnoreCase))
            {
                return new ProfileProgressLoadResult(
                    ProfileProgressLoadStatus.ChecksumMismatch,
                    document,
                    "Profile progress checksum mismatch.");
            }

            var errors = Validate(document.Progress);
            if (errors.Count > 0)
            {
                return new ProfileProgressLoadResult(
                    ProfileProgressLoadStatus.InvalidProfile,
                    document,
                    string.Join("\n", errors));
            }

            return new ProfileProgressLoadResult(
                ProfileProgressLoadStatus.Valid,
                document);
        }

        private static ProfileProgressV1 Normalize(ProfileProgressV1 progress)
        {
            var value = Clone(progress ?? ProfileProgressV1.CreateDefault());
            value.SchemaVersion = ProfileProgressV1.CurrentSchemaVersion;
            value.LastWrittenVersion = ProfileProgressV1.CurrentWriterVersion;
            value.UnlockedHeroIds = (value.UnlockedHeroIds ?? new List<string>())
                .Where(HeroIds.IsKnown)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (!value.UnlockedHeroIds.Contains(HeroIds.Warrior))
            {
                value.UnlockedHeroIds.Insert(0, HeroIds.Warrior);
            }

            value.DefeatedChapterBossIds =
                (value.DefeatedChapterBossIds ?? new List<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            value.UnreadUnlockNotifications =
                (value.UnreadUnlockNotifications ??
                 new List<ProfileUnlockNotificationV1>())
                .Where(notification => notification != null)
                .GroupBy(notification => notification.Id, StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList();
            value.LegacyV033ArchiveRelativePath =
                value.LegacyV033ArchiveRelativePath ?? string.Empty;
            return value;
        }

        private static IReadOnlyList<string> Validate(ProfileProgressV1 progress)
        {
            var errors = new List<string>();
            if (progress == null)
            {
                errors.Add("Profile progress is missing.");
                return errors;
            }

            if (progress.SchemaVersion != ProfileProgressV1.CurrentSchemaVersion)
            {
                errors.Add($"Unsupported profile schema {progress.SchemaVersion}.");
            }

            if (string.IsNullOrWhiteSpace(progress.LastWrittenVersion))
            {
                errors.Add("Profile last-written version is missing.");
            }

            if (progress.UnlockedHeroIds == null ||
                !progress.UnlockedHeroIds.Contains(HeroIds.Warrior) ||
                progress.UnlockedHeroIds.Any(id => !HeroIds.IsKnown(id)) ||
                progress.UnlockedHeroIds.Distinct(StringComparer.Ordinal).Count() !=
                progress.UnlockedHeroIds.Count)
            {
                errors.Add("Profile unlocked hero ids are invalid.");
            }

            if (progress.DefeatedChapterBossIds == null ||
                progress.DefeatedChapterBossIds.Any(string.IsNullOrWhiteSpace))
            {
                errors.Add("Profile defeated chapter Boss ids are invalid.");
            }

            var notifications = progress.UnreadUnlockNotifications;
            if (notifications == null ||
                notifications.Any(value =>
                    value == null ||
                    string.IsNullOrWhiteSpace(value.Id) ||
                    !HeroIds.IsKnown(value.HeroId) ||
                    string.IsNullOrWhiteSpace(value.SourceMapId) ||
                    value.CreatedAtUtc == default(DateTime)) ||
                notifications
                    .Select(value => value.Id)
                    .Distinct(StringComparer.Ordinal)
                    .Count() != notifications.Count)
            {
                errors.Add("Profile unlock notifications are invalid.");
            }

            if (progress.LegacyV033ArchiveCompleted &&
                string.IsNullOrWhiteSpace(progress.LegacyV033ArchiveRelativePath))
            {
                errors.Add("Completed legacy archive has no relative path.");
            }

            return errors;
        }

        private static ProfileProgressLoadResult Failure(
            ProfileProgressLoadStatus status,
            Exception exception)
        {
            return new ProfileProgressLoadResult(
                status,
                diagnostic: exception.GetType().Name + ": " + exception.Message);
        }
    }
}

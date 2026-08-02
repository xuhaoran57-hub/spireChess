using System;
using System.Collections.Generic;
using System.Linq;
using SpireChess.Run;

namespace SpireChess.Save
{
    public sealed class ProfileProgressV1
    {
        public const int CurrentSchemaVersion = 1;
        public const string CurrentWriterVersion = "0.4.0";

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public string LastWrittenVersion { get; set; } = CurrentWriterVersion;
        public List<string> UnlockedHeroIds { get; set; } = new List<string>();
        public List<string> DefeatedChapterBossIds { get; set; } = new List<string>();
        public List<ProfileUnlockNotificationV1> UnreadUnlockNotifications { get; set; } =
            new List<ProfileUnlockNotificationV1>();
        public bool LegacyV033ArchiveCompleted { get; set; }
        public string LegacyV033ArchiveRelativePath { get; set; } = string.Empty;
        public bool LegacyV033ArchiveNoticePending { get; set; }

        public bool IsHeroUnlocked(string heroId)
        {
            return UnlockedHeroIds != null &&
                   UnlockedHeroIds.Contains(heroId, StringComparer.Ordinal);
        }

        public static ProfileProgressV1 CreateDefault()
        {
            return new ProfileProgressV1
            {
                UnlockedHeroIds = new List<string> { HeroIds.Warrior }
            };
        }
    }

    public sealed class ProfileUnlockNotificationV1
    {
        public string Id { get; set; }
        public string HeroId { get; set; }
        public string SourceMapId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public sealed class ProfileProgressDocumentV1
    {
        public const string FormatId = "travel-journal-profile";

        public string Format { get; set; } = FormatId;
        public int SchemaVersion { get; set; } =
            ProfileProgressV1.CurrentSchemaVersion;
        public DateTime SavedAtUtc { get; set; }
        public long Revision { get; set; }
        public ProfileProgressV1 Progress { get; set; }
        public string ProgressSha256 { get; set; }
    }

    public enum ProfileProgressLoadStatus
    {
        Missing,
        Created,
        Valid,
        RecoveredFromBackup,
        UnsupportedSchema,
        CorruptJson,
        ChecksumMismatch,
        InvalidProfile,
        IoFailure
    }

    public sealed class ProfileProgressLoadResult
    {
        public ProfileProgressLoadResult(
            ProfileProgressLoadStatus status,
            ProfileProgressDocumentV1 document = null,
            string diagnostic = null)
        {
            Status = status;
            Document = document;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public ProfileProgressLoadStatus Status { get; }
        public ProfileProgressDocumentV1 Document { get; }
        public ProfileProgressV1 Progress => Document?.Progress;
        public string Diagnostic { get; }
        public bool IsUsable =>
            Status == ProfileProgressLoadStatus.Created ||
            Status == ProfileProgressLoadStatus.Valid ||
            Status == ProfileProgressLoadStatus.RecoveredFromBackup;
    }
}

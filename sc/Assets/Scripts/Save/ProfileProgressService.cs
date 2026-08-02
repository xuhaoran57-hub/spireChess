using System;
using System.Linq;
using SpireChess.Run;

namespace SpireChess.Save
{
    public sealed class ProfileProgressService
    {
        private readonly ProfileProgressRepository repository;
        private readonly Func<DateTime> utcNow;

        public ProfileProgressService(
            ProfileProgressRepository repository,
            Func<DateTime> utcNow = null)
        {
            this.repository = repository ??
                              throw new ArgumentNullException(nameof(repository));
            this.utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public ProfileProgressV1 Progress { get; private set; }
        public ProfileProgressLoadResult LastLoadResult { get; private set; }
        public bool IsReady => Progress != null;

        public ProfileProgressLoadResult Initialize(
            ProfileProgressV1 initialProgress = null)
        {
            LastLoadResult = repository.LoadOrCreate(initialProgress);
            if (!LastLoadResult.IsUsable || LastLoadResult.Progress == null)
            {
                return LastLoadResult;
            }

            Progress = ProfileProgressRepository.Clone(LastLoadResult.Progress);
            return LastLoadResult;
        }

        public bool IsHeroUnlocked(string heroId)
        {
            return Progress?.IsHeroUnlocked(heroId) == true;
        }

        public bool MarkLegacyArchiveCompleted(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException(
                    "Legacy archive relative path is required.",
                    nameof(relativePath));
            }

            if (Progress == null)
            {
                throw new InvalidOperationException("Profile progress is not initialized.");
            }

            if (Progress.LegacyV033ArchiveCompleted &&
                string.Equals(
                    Progress.LegacyV033ArchiveRelativePath,
                    relativePath,
                    StringComparison.Ordinal))
            {
                return false;
            }

            return SaveMutation(candidate =>
            {
                candidate.LegacyV033ArchiveCompleted = true;
                candidate.LegacyV033ArchiveRelativePath = relativePath;
                candidate.LegacyV033ArchiveNoticePending = true;
            });
        }

        public bool AcknowledgeLegacyArchiveNotice()
        {
            if (Progress?.LegacyV033ArchiveNoticePending != true)
            {
                return false;
            }

            return SaveMutation(candidate =>
                candidate.LegacyV033ArchiveNoticePending = false);
        }

        public bool RecordChapterBossVictory(string mapId)
        {
            if (string.IsNullOrWhiteSpace(mapId))
            {
                throw new ArgumentException("Map id is required.", nameof(mapId));
            }

            if (Progress == null)
            {
                throw new InvalidOperationException("Profile progress is not initialized.");
            }

            var heroId = HeroUnlockedByMap(mapId);
            if (Progress.DefeatedChapterBossIds.Contains(
                    mapId,
                    StringComparer.Ordinal) &&
                (heroId == null || Progress.IsHeroUnlocked(heroId)))
            {
                return false;
            }

            return SaveMutation(candidate =>
            {
                if (!candidate.DefeatedChapterBossIds.Contains(
                        mapId,
                        StringComparer.Ordinal))
                {
                    candidate.DefeatedChapterBossIds.Add(mapId);
                }

                if (heroId == null ||
                    candidate.UnlockedHeroIds.Contains(
                        heroId,
                        StringComparer.Ordinal))
                {
                    return;
                }

                candidate.UnlockedHeroIds.Add(heroId);
                candidate.UnreadUnlockNotifications.Add(
                    new ProfileUnlockNotificationV1
                    {
                        Id = $"hero-unlock:{heroId}:{mapId}",
                        HeroId = heroId,
                        SourceMapId = mapId,
                        CreatedAtUtc = utcNow()
                    });
            });
        }

        public bool MarkUnlockNotificationRead(string notificationId)
        {
            if (string.IsNullOrWhiteSpace(notificationId) ||
                Progress?.UnreadUnlockNotifications.Any(value =>
                    string.Equals(
                        value.Id,
                        notificationId,
                        StringComparison.Ordinal)) != true)
            {
                return false;
            }

            return SaveMutation(candidate =>
                candidate.UnreadUnlockNotifications.RemoveAll(value =>
                    string.Equals(
                        value.Id,
                        notificationId,
                        StringComparison.Ordinal)));
        }

        private bool SaveMutation(Action<ProfileProgressV1> mutation)
        {
            var candidate = ProfileProgressRepository.Clone(Progress);
            mutation(candidate);
            var saved = repository.Save(candidate);
            Progress = ProfileProgressRepository.Clone(saved.Progress);
            return true;
        }

        private static string HeroUnlockedByMap(string mapId)
        {
            switch (mapId)
            {
                case "map_wilderness":
                    return HeroIds.Mage;
                case "map_startrail_highlands":
                    return HeroIds.Rogue;
                default:
                    return null;
            }
        }
    }
}

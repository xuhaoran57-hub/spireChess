using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SpireChess.Run;
using SpireChess.Save;

namespace SpireChess.Tests.EditMode
{
    public sealed class ProfileProgressTests
    {
        private string root;
        private DateTime now;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(
                Path.GetTempPath(),
                "travel-journal-profile-tests",
                Guid.NewGuid().ToString("N"));
            now = new DateTime(
                2026,
                8,
                1,
                10,
                20,
                30,
                DateTimeKind.Utc);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void NewProfile_CreatesAtomicDocumentWithWarriorOnly()
        {
            var repository = new ProfileProgressRepository(root, () => now);

            var result = repository.LoadOrCreate();

            Assert.That(result.Status, Is.EqualTo(ProfileProgressLoadStatus.Created));
            Assert.That(result.Progress.SchemaVersion, Is.EqualTo(1));
            Assert.That(
                result.Progress.LastWrittenVersion,
                Is.EqualTo(ProfileProgressV1.CurrentWriterVersion));
            Assert.That(
                result.Progress.UnlockedHeroIds,
                Is.EqualTo(new[] { HeroIds.Warrior }));
            Assert.That(result.Progress.DefeatedChapterBossIds, Is.Empty);
            Assert.That(result.Progress.UnreadUnlockNotifications, Is.Empty);
            Assert.That(repository.Storage.MainExists, Is.True);
            Assert.That(repository.Storage.TemporaryExists, Is.False);
            Assert.That(repository.Load().Status, Is.EqualTo(
                ProfileProgressLoadStatus.Valid));
        }

        [Test]
        public void ChapterBossProgress_UnlocksHeroesAndNotificationsIdempotently()
        {
            var repository = new ProfileProgressRepository(root, () => now);
            var service = new ProfileProgressService(repository, () => now);
            Assert.That(service.Initialize().IsUsable, Is.True);

            Assert.That(service.RecordChapterBossVictory("map_wilderness"), Is.True);
            Assert.That(service.IsHeroUnlocked(HeroIds.Mage), Is.True);
            Assert.That(service.IsHeroUnlocked(HeroIds.Rogue), Is.False);
            Assert.That(
                service.Progress.DefeatedChapterBossIds,
                Is.EqualTo(new[] { "map_wilderness" }));
            Assert.That(service.Progress.UnreadUnlockNotifications, Has.Count.EqualTo(1));
            Assert.That(
                service.Progress.UnreadUnlockNotifications[0].HeroId,
                Is.EqualTo(HeroIds.Mage));
            var revision = repository.CurrentRevision;

            Assert.That(service.RecordChapterBossVictory("map_wilderness"), Is.False);
            Assert.That(repository.CurrentRevision, Is.EqualTo(revision));
            Assert.That(service.Progress.UnreadUnlockNotifications, Has.Count.EqualTo(1));

            Assert.That(
                service.RecordChapterBossVictory("map_startrail_highlands"),
                Is.True);
            Assert.That(service.IsHeroUnlocked(HeroIds.Rogue), Is.True);
            Assert.That(service.Progress.UnreadUnlockNotifications, Has.Count.EqualTo(2));
            Assert.That(repository.Storage.BackupExists, Is.True);
        }

        [Test]
        public void CorruptMain_RecoversProfileFromValidatedBackup()
        {
            var repository = new ProfileProgressRepository(root, () => now);
            var service = new ProfileProgressService(repository, () => now);
            Assert.That(service.Initialize().IsUsable, Is.True);
            Assert.That(service.RecordChapterBossVictory("map_wilderness"), Is.True);
            File.WriteAllText(repository.Storage.MainPath, "{broken");

            var recoveredRepository = new ProfileProgressRepository(root, () => now);
            var recovered = recoveredRepository.Load();

            Assert.That(
                recovered.Status,
                Is.EqualTo(ProfileProgressLoadStatus.RecoveredFromBackup));
            Assert.That(recovered.Progress.IsHeroUnlocked(HeroIds.Warrior), Is.True);
            Assert.That(recovered.Progress.IsHeroUnlocked(HeroIds.Mage), Is.False);
            Assert.That(recoveredRepository.Storage.MainExists, Is.True);
            Assert.That(
                Directory.GetFiles(
                    root,
                    ProfileProgressRepository.ProfileFileName + ".corrupt-*")
                    .Length,
                Is.EqualTo(1));
        }

        [Test]
        public void LegacyArchive_CopiesEveryRecognizedRunFileWithoutMutation()
        {
            var legacyStorage = new AtomicFileSaveStorage(
                root,
                AtomicFileSaveStorage.LegacySlotFileName,
                () => now);
            legacyStorage.WriteAtomic(
                "{\"format\":\"spire-chess-run\",\"schemaVersion\":1,\"revision\":1}");
            legacyStorage.WriteAtomic(
                "{\"format\":\"spire-chess-run\",\"schemaVersion\":1,\"revision\":2}");
            File.WriteAllText(
                legacyStorage.TemporaryPath,
                "legacy temporary recovery");
            File.WriteAllText(legacyStorage.MainPath + ".repair", "legacy repair");
            File.WriteAllText(
                legacyStorage.MainPath + ".corrupt-manual",
                "legacy corrupt");
            var originals = legacyStorage.EnumerateSlotFiles()
                .Where(File.Exists)
                .ToDictionary(
                    Path.GetFileName,
                    File.ReadAllBytes,
                    StringComparer.Ordinal);

            var service = new LegacyRunArchiveService(root, () => now);
            var archived = service.ArchiveIfNeeded(false);

            Assert.That(archived.LegacyDetected, Is.True);
            Assert.That(archived.ArchivedNow, Is.True);
            Assert.That(archived.Succeeded, Is.True);
            var archivePath = Path.Combine(root, archived.ArchiveRelativePath);
            Assert.That(Directory.Exists(archivePath), Is.True);
            foreach (var original in originals)
            {
                Assert.That(
                    File.ReadAllBytes(
                        Path.Combine(archivePath, original.Key)),
                    Is.EqualTo(original.Value),
                    original.Key);
                Assert.That(
                    File.ReadAllBytes(
                        Path.Combine(root, original.Key)),
                    Is.EqualTo(original.Value),
                    original.Key);
            }

            var repeated = service.ArchiveIfNeeded(false);
            Assert.That(repeated.LegacyDetected, Is.True);
            Assert.That(repeated.ArchivedNow, Is.False);
            Assert.That(
                repeated.ArchiveRelativePath,
                Is.EqualTo(archived.ArchiveRelativePath));
            Assert.That(
                Directory.GetDirectories(
                    Path.Combine(
                        root,
                        LegacyRunArchiveService.LegacyRootDirectoryName,
                        LegacyRunArchiveService.LegacyVersionDirectoryName))
                    .Count(path => !path.EndsWith(".partial")),
                Is.EqualTo(1));

            var currentStorage = new AtomicFileSaveStorage(root, () => now);
            Assert.That(
                currentStorage.MainPath,
                Is.Not.EqualTo(legacyStorage.MainPath));
            currentStorage.WriteAtomic(
                "{\"format\":\"spire-chess-run\",\"schemaVersion\":" +
                RunSaveDocumentV1.CurrentSchemaVersion + "}");
            currentStorage.DeleteAll();
            Assert.That(Directory.Exists(archivePath), Is.True);
            Assert.That(
                Directory.GetFiles(archivePath).Length,
                Is.EqualTo(originals.Count));
            foreach (var original in originals)
            {
                Assert.That(
                    File.ReadAllBytes(Path.Combine(root, original.Key)),
                    Is.EqualTo(original.Value),
                    original.Key);
            }
        }

        [Test]
        public void CurrentSchemaRun_IsNotMistakenForLegacy()
        {
            var storage = new AtomicFileSaveStorage(
                root,
                AtomicFileSaveStorage.LegacySlotFileName,
                () => now);
            storage.WriteAtomic(
                "{\"format\":\"spire-chess-run\",\"schemaVersion\":" +
                RunSaveDocumentV1.CurrentSchemaVersion + "}");

            var result = new LegacyRunArchiveService(root, () => now)
                .ArchiveIfNeeded(false);

            Assert.That(result.LegacyDetected, Is.False);
            Assert.That(result.ArchivedNow, Is.False);
            Assert.That(
                Directory.Exists(
                    Path.Combine(
                        root,
                        LegacyRunArchiveService.LegacyRootDirectoryName)),
                Is.False);
        }
    }
}

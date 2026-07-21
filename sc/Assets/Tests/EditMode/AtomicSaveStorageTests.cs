using System;
using System.IO;
using NUnit.Framework;
using SpireChess.Config;
using SpireChess.Run;
using SpireChess.Save;
using SpireChess.Utils;

namespace SpireChess.Tests.EditMode
{
    public sealed class AtomicSaveStorageTests
    {
        private string root;
        private ConfigService configs;
        private AtomicFileSaveStorage storage;
        private RunSaveRepository repository;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "spire-chess-save-tests", Guid.NewGuid().ToString("N"));
            configs = new ConfigService(new NewtonsoftJsonSerializer());
            var validation = configs.LoadFromResources();
            Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));
            storage = new AtomicFileSaveStorage(root, () => new DateTime(2026, 7, 21, 14, 0, 0, DateTimeKind.Utc));
            repository = new RunSaveRepository(
                configs,
                storage,
                () => new DateTime(2026, 7, 21, 13, 0, 0, DateTimeKind.Utc));
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
        public void SaveTwice_CreatesMainAndPreviousRevisionBackup()
        {
            var run = new RunSession(configs, 4201);
            repository.Save(run, 1);
            var firstText = storage.ReadMain();
            var node = run.State.CurrentMap.StartNodeIds[0];
            Assert.That(run.EnterNode(node).Success, Is.True);

            repository.Save(run, 2);

            Assert.That(storage.MainExists, Is.True);
            Assert.That(storage.BackupExists, Is.True);
            Assert.That(storage.TemporaryExists, Is.False);
            Assert.That(storage.ReadBackup(), Is.EqualTo(firstText));
            var loaded = repository.Load();
            Assert.That(loaded.Status, Is.EqualTo(RunSaveLoadStatus.Valid));
            Assert.That(loaded.Document.Revision, Is.EqualTo(2));
            Assert.That(loaded.Session.State.Phase, Is.EqualTo(RunPhase.Shop));
        }

        [Test]
        public void CorruptMain_UsesBackupAndRepairsWithoutDestroyingGoodBackup()
        {
            var run = new RunSession(configs, 731);
            repository.Save(run, 1);
            Assert.That(run.EnterNode(run.State.CurrentMap.StartNodeIds[0]).Success, Is.True);
            repository.Save(run, 2);
            var goodBackup = storage.ReadBackup();
            File.WriteAllText(storage.MainPath, "{broken", System.Text.Encoding.UTF8);

            var recovered = repository.Load();

            Assert.That(recovered.Status, Is.EqualTo(RunSaveLoadStatus.RecoveredFromBackup));
            Assert.That(recovered.Document.Revision, Is.EqualTo(1));
            var corruptPath = repository.RepairMainFromBackup();
            Assert.That(File.Exists(corruptPath), Is.True);
            Assert.That(storage.ReadMain(), Is.EqualTo(goodBackup));
            Assert.That(storage.ReadBackup(), Is.EqualTo(goodBackup));
            Assert.That(repository.Load().Status, Is.EqualTo(RunSaveLoadStatus.Valid));
        }

        [Test]
        public void TamperedPayload_IsRejectedByChecksum()
        {
            repository.Save(new RunSession(configs, 99317), 1);
            var text = storage.ReadMain();
            var tampered = text.Replace("\"seed\": 99317", "\"seed\": 99318");
            Assert.That(tampered, Is.Not.EqualTo(text));
            File.WriteAllText(storage.MainPath, tampered, System.Text.Encoding.UTF8);

            var result = repository.Load();

            Assert.That(result.Status, Is.EqualTo(RunSaveLoadStatus.ChecksumMismatch));
            Assert.That(result.Session, Is.Null);
        }

        [Test]
        public void IncompatibleContent_IsRejectedWithoutDeletingSave()
        {
            repository.Save(new RunSession(configs, 12), 1);
            var text = storage.ReadMain();
            var tampered = text.Replace(
                configs.Identity.ConfigHash,
                new string('0', configs.Identity.ConfigHash.Length));
            File.WriteAllText(storage.MainPath, tampered, System.Text.Encoding.UTF8);

            var result = repository.Load();

            Assert.That(result.Status, Is.EqualTo(RunSaveLoadStatus.IncompatibleContent));
            Assert.That(storage.MainExists, Is.True);
        }

        [Test]
        public void Delete_RemovesMainBackupTemporaryAndDiagnostics()
        {
            repository.Save(new RunSession(configs, 8), 1);
            File.WriteAllText(storage.TemporaryPath, "temp");
            File.WriteAllText(storage.MainPath + ".corrupt-test", "bad");

            repository.Delete();

            Assert.That(storage.EnumerateSlotFiles(), Is.All.Matches<string>(path => !File.Exists(path)));
            Assert.That(repository.Load().Status, Is.EqualTo(RunSaveLoadStatus.Missing));
        }

        [Test]
        public void CorruptMainAndBackup_NeverStartsANewRun()
        {
            var run = new RunSession(configs, 177);
            repository.Save(run, 1);
            Assert.That(run.EnterNode(run.State.CurrentMap.StartNodeIds[0]).Success, Is.True);
            repository.Save(run, 2);
            File.WriteAllText(storage.MainPath, "bad-main");
            File.WriteAllText(storage.BackupPath, "bad-backup");

            var result = repository.Load();

            Assert.That(result.CanContinue, Is.False);
            Assert.That(result.Session, Is.Null);
            Assert.That(result.Status, Is.EqualTo(RunSaveLoadStatus.CorruptJson));
            Assert.That(storage.MainExists, Is.True);
            Assert.That(storage.BackupExists, Is.True);
        }

        [Test]
        public void OrphanTemporaryFile_IsNotTrustedAsAFormalSave()
        {
            File.WriteAllText(storage.TemporaryPath, "apparently complete");

            var result = repository.Load();

            Assert.That(result.Status, Is.EqualTo(RunSaveLoadStatus.Missing));
            Assert.That(storage.TemporaryExists, Is.True);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SpireChess.Save
{
    public sealed class AtomicFileSaveStorage
    {
        public const string SlotFileName = "run-slot-0.json";

        private readonly string rootPath;
        private readonly Func<DateTime> utcNow;

        public AtomicFileSaveStorage()
            : this(Application.persistentDataPath)
        {
        }

        public AtomicFileSaveStorage(string rootPath, Func<DateTime> utcNow = null)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("Save root path is required.", nameof(rootPath));
            }

            this.rootPath = Path.GetFullPath(rootPath);
            this.utcNow = utcNow ?? (() => DateTime.UtcNow);
            Directory.CreateDirectory(this.rootPath);
        }

        public string MainPath => Path.Combine(rootPath, SlotFileName);
        public string TemporaryPath => MainPath + ".tmp";
        public string BackupPath => MainPath + ".bak";

        public bool MainExists => File.Exists(MainPath);
        public bool BackupExists => File.Exists(BackupPath);
        public bool TemporaryExists => File.Exists(TemporaryPath);

        public string ReadMain()
        {
            return File.ReadAllText(MainPath, Encoding.UTF8);
        }

        public string ReadBackup()
        {
            return File.ReadAllText(BackupPath, Encoding.UTF8);
        }

        public void WriteAtomic(string content)
        {
            Directory.CreateDirectory(rootPath);
            WriteDurable(TemporaryPath, content);
            if (File.Exists(MainPath))
            {
                File.Replace(TemporaryPath, MainPath, BackupPath, true);
            }
            else
            {
                File.Move(TemporaryPath, MainPath);
            }

            if (File.Exists(TemporaryPath))
            {
                throw new IOException("Temporary save file remains after atomic write.");
            }
        }

        public string RepairMainFromBackup()
        {
            if (!File.Exists(BackupPath))
            {
                throw new FileNotFoundException("Run save backup is missing.", BackupPath);
            }

            var repairPath = MainPath + ".repair";
            var corruptPath = MainPath + ".corrupt-" +
                              utcNow().ToString("yyyyMMdd-HHmmss-fffffff");
            var backupText = File.ReadAllText(BackupPath, Encoding.UTF8);
            WriteDurable(repairPath, backupText);
            if (File.Exists(MainPath))
            {
                File.Move(MainPath, corruptPath);
            }

            try
            {
                File.Move(repairPath, MainPath);
            }
            catch
            {
                if (!File.Exists(MainPath) && File.Exists(corruptPath))
                {
                    File.Move(corruptPath, MainPath);
                }
                throw;
            }

            return File.Exists(corruptPath) ? corruptPath : null;
        }

        public void DeleteAll()
        {
            foreach (var path in EnumerateSlotFiles())
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        public IReadOnlyList<string> EnumerateSlotFiles()
        {
            var fixedPaths = new[]
            {
                MainPath,
                TemporaryPath,
                BackupPath,
                MainPath + ".repair"
            };
            var corrupt = Directory.Exists(rootPath)
                ? Directory.GetFiles(rootPath, SlotFileName + ".corrupt-*", SearchOption.TopDirectoryOnly)
                : Array.Empty<string>();
            return fixedPaths.Concat(corrupt).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private static void WriteDurable(string path, string content)
        {
            using (var stream = new FileStream(
                       path,
                       FileMode.Create,
                       FileAccess.Write,
                       FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(content ?? string.Empty);
                writer.Flush();
                stream.Flush(true);
            }
        }
    }
}

using System.IO;
using UnityEngine;

namespace SpireChess.Save
{
    public sealed class FileSaveStorage : ISaveStorage
    {
        private readonly string rootPath;

        public FileSaveStorage()
            : this(Application.persistentDataPath)
        {
        }

        public FileSaveStorage(string rootPath)
        {
            this.rootPath = rootPath;
            Directory.CreateDirectory(rootPath);
        }

        public bool Exists(string key)
        {
            return File.Exists(GetPath(key));
        }

        public string ReadText(string key)
        {
            return File.ReadAllText(GetPath(key));
        }

        public void WriteText(string key, string content)
        {
            var path = GetPath(key);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, content);
        }

        public void Delete(string key)
        {
            var path = GetPath(key);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private string GetPath(string key)
        {
            return Path.Combine(rootPath, key);
        }
    }
}

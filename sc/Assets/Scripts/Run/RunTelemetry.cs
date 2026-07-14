using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace SpireChess.Run
{
    public sealed class RunTelemetry
    {
        public const string SchemaVersion = "1";
        private readonly string outputPath;
        private readonly string contentVersion;
        private readonly int seed;

        public RunTelemetry(string outputPath, string contentVersion, int seed)
        {
            this.outputPath = outputPath ?? throw new ArgumentNullException(nameof(outputPath));
            this.contentVersion = contentVersion ?? string.Empty;
            this.seed = seed;
        }

        public void Record(string eventType, object payload = null)
        {
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var entry = new Dictionary<string, object>
            {
                ["schemaVersion"] = SchemaVersion,
                ["contentVersion"] = contentVersion,
                ["seed"] = seed,
                ["timestampUtc"] = DateTime.UtcNow.ToString("O"),
                ["eventType"] = eventType ?? "Unknown",
                ["payload"] = payload
            };
            File.AppendAllText(
                outputPath,
                JsonConvert.SerializeObject(entry) + Environment.NewLine);
        }
    }
}

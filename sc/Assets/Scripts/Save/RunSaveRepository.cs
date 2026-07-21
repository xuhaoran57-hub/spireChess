using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SpireChess.Config;
using SpireChess.Run;
using SpireChess.Utils;
using UnityEngine;

namespace SpireChess.Save
{
    public sealed class RunSaveRepository
    {
        private readonly ConfigService configs;
        private readonly AtomicFileSaveStorage storage;
        private readonly RunSnapshotMapper mapper;
        private readonly RunSnapshotValidator validator;
        private readonly Func<DateTime> utcNow;
        private readonly JsonSerializerSettings settings;

        public RunSaveRepository(
            ConfigService configs,
            AtomicFileSaveStorage storage = null,
            Func<DateTime> utcNow = null)
        {
            this.configs = configs ?? throw new ArgumentNullException(nameof(configs));
            this.storage = storage ?? new AtomicFileSaveStorage();
            this.utcNow = utcNow ?? (() => DateTime.UtcNow);
            mapper = new RunSnapshotMapper(configs);
            validator = new RunSnapshotValidator(configs);
            settings = CreateSettings(Formatting.Indented);
        }

        public AtomicFileSaveStorage Storage => storage;

        public RunSaveDocumentV1 Save(RunSession session, long revision)
        {
            if (configs.Identity == null)
            {
                throw new InvalidOperationException("Runtime config identity is unavailable.");
            }

            if (revision < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(revision));
            }

            var payload = mapper.Capture(session);
            var validation = validator.ValidateDto(payload);
            if (!validation.IsValid)
            {
                throw new RunSnapshotException(string.Join("\n", validation.Errors));
            }

            var document = new RunSaveDocumentV1
            {
                ContentVersion = configs.Identity.ContentVersion,
                RulesVersion = configs.Identity.RulesVersion,
                ConfigHash = configs.Identity.ConfigHash,
                AppVersion = Application.version,
                GitCommit = string.Empty,
                UnityVersion = Application.unityVersion,
                SavedAtUtc = utcNow(),
                Revision = revision,
                Summary = BuildSummary(payload.RunState),
                Payload = payload
            };
            document.PayloadSha256 = ComputePayloadHash(payload);
            storage.WriteAtomic(JsonConvert.SerializeObject(document, settings));
            return document;
        }

        public RunSaveLoadResult Inspect()
        {
            return LoadInternal(false);
        }

        public RunSaveLoadResult Load()
        {
            return LoadInternal(true);
        }

        public string RepairMainFromBackup()
        {
            var backup = ReadCandidate(false, false);
            if (!backup.CanContinue)
            {
                throw new InvalidOperationException("Backup is not valid and cannot repair the main save.");
            }

            return storage.RepairMainFromBackup();
        }

        public void Delete()
        {
            storage.DeleteAll();
        }

        public static string ComputePayloadHash(RunSavePayloadV1 payload)
        {
            var settings = CreateSettings(Formatting.None);
            var token = JToken.FromObject(payload, JsonSerializer.Create(settings));
            return CanonicalJson.ComputeTokenSha256(token);
        }

        private static JsonSerializerSettings CreateSettings(Formatting formatting)
        {
            var value = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = formatting,
                NullValueHandling = NullValueHandling.Include
            };
            value.Converters.Add(new StringEnumConverter());
            return value;
        }

        private RunSaveLoadResult LoadInternal(bool hydrate)
        {
            var main = ReadCandidate(true, hydrate);
            if (main.CanContinue)
            {
                return main;
            }

            var backup = ReadCandidate(false, hydrate);
            if (backup.CanContinue)
            {
                return new RunSaveLoadResult(
                    RunSaveLoadStatus.RecoveredFromBackup,
                    backup.Document,
                    backup.Session,
                    "Recovered from validated backup.",
                    main.Status);
            }

            if (main.Status != RunSaveLoadStatus.Missing)
            {
                return main;
            }

            return backup.Status == RunSaveLoadStatus.Missing
                ? new RunSaveLoadResult(RunSaveLoadStatus.Missing)
                : backup;
        }

        private RunSaveLoadResult ReadCandidate(bool main, bool hydrate)
        {
            var exists = main ? storage.MainExists : storage.BackupExists;
            if (!exists)
            {
                return new RunSaveLoadResult(RunSaveLoadStatus.Missing);
            }

            RunSaveDocumentV1 document;
            try
            {
                var json = main ? storage.ReadMain() : storage.ReadBackup();
                document = JsonConvert.DeserializeObject<RunSaveDocumentV1>(json, settings);
            }
            catch (JsonException exception)
            {
                return Failure(RunSaveLoadStatus.CorruptJson, exception);
            }
            catch (IOException exception)
            {
                return Failure(RunSaveLoadStatus.IoFailure, exception);
            }
            catch (UnauthorizedAccessException exception)
            {
                return Failure(RunSaveLoadStatus.IoFailure, exception);
            }

            if (document == null || !string.Equals(
                    document.Format,
                    RunSaveDocumentV1.FormatId,
                    StringComparison.Ordinal))
            {
                return new RunSaveLoadResult(
                    RunSaveLoadStatus.CorruptJson,
                    document,
                    diagnostic: "Save format id is missing or invalid.");
            }

            if (document.SchemaVersion != RunSaveDocumentV1.CurrentSchemaVersion)
            {
                return new RunSaveLoadResult(
                    RunSaveLoadStatus.UnsupportedSchema,
                    document,
                    diagnostic: $"Unsupported schema {document.SchemaVersion}.");
            }

            if (document.Payload == null || !string.Equals(
                    document.PayloadSha256,
                    ComputePayloadHash(document.Payload),
                    StringComparison.OrdinalIgnoreCase))
            {
                return new RunSaveLoadResult(
                    RunSaveLoadStatus.ChecksumMismatch,
                    document,
                    diagnostic: "Payload checksum mismatch.");
            }

            if (configs.Identity == null ||
                !string.Equals(document.ContentVersion, configs.Identity.ContentVersion,
                    StringComparison.Ordinal) ||
                !string.Equals(document.RulesVersion, configs.Identity.RulesVersion,
                    StringComparison.Ordinal) ||
                !string.Equals(document.ConfigHash, configs.Identity.ConfigHash,
                    StringComparison.Ordinal))
            {
                return new RunSaveLoadResult(
                    RunSaveLoadStatus.IncompatibleContent,
                    document,
                    diagnostic: "Save content identity does not match runtime content.");
            }

            var validation = validator.ValidateDto(document.Payload);
            if (!validation.IsValid)
            {
                return new RunSaveLoadResult(
                    RunSaveLoadStatus.InvalidDomainState,
                    document,
                    diagnostic: string.Join("\n", validation.Errors));
            }

            document.Summary = BuildSummary(document.Payload.RunState);

            if (!hydrate)
            {
                return new RunSaveLoadResult(RunSaveLoadStatus.Valid, document);
            }

            try
            {
                var session = mapper.Restore(document.Payload);
                var hydratedValidation = validator.ValidateHydratedRun(session);
                if (!hydratedValidation.IsValid)
                {
                    return new RunSaveLoadResult(
                        RunSaveLoadStatus.InvalidDomainState,
                        document,
                        diagnostic: string.Join("\n", hydratedValidation.Errors));
                }

                return new RunSaveLoadResult(
                    RunSaveLoadStatus.Valid,
                    document,
                    session);
            }
            catch (RandomReplayException exception)
            {
                return Failure(RunSaveLoadStatus.RandomReplayMismatch, exception, document);
            }
            catch (RunSnapshotException exception)
            {
                return Failure(RunSaveLoadStatus.InvalidReference, exception, document);
            }
            catch (Exception exception)
            {
                return Failure(RunSaveLoadStatus.InvalidDomainState, exception, document);
            }
        }

        private static RunSaveSummaryV1 BuildSummary(RunStateSnapshotV1 state)
        {
            return new RunSaveSummaryV1
            {
                Floor = state.Floor,
                Health = state.Health,
                MaxHealth = state.MaxHealth,
                ShopTurn = state.ShopTurn,
                Phase = state.Phase
            };
        }

        private static RunSaveLoadResult Failure(
            RunSaveLoadStatus status,
            Exception exception,
            RunSaveDocumentV1 document = null)
        {
            return new RunSaveLoadResult(
                status,
                document,
                diagnostic: exception.GetType().Name + ": " + exception.Message);
        }
    }
}

using System;
using SpireChess.Run;
using SpireChess.Save;
using UnityEngine;

namespace SpireChess.App
{
    public sealed class RunPersistenceCoordinator
    {
        private readonly RunSaveRepository repository;

        public RunPersistenceCoordinator(RunSaveRepository repository, bool enabled = true)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            Enabled = enabled;
        }

        public bool Enabled { get; }
        public long CurrentRevision { get; private set; }
        public long LastSavedRevision { get; private set; }
        public DateTime? LastSavedAtUtc { get; private set; }
        public bool HasUnsavedChanges => CurrentRevision > LastSavedRevision;
        public string LastError { get; private set; } = string.Empty;

        public bool BeginNewRun(RunSession candidate)
        {
            CurrentRevision = 1;
            LastSavedRevision = 0;
            LastSavedAtUtc = null;
            LastError = string.Empty;
            return Save(candidate, true);
        }

        public void AdoptLoadedRun(RunSaveDocumentV1 document)
        {
            var revision = Math.Max(1, document?.Revision ?? 1);
            CurrentRevision = revision;
            LastSavedRevision = revision;
            LastSavedAtUtc = document?.SavedAtUtc;
            LastError = string.Empty;
        }

        public bool CommitSuccessful(RunSession run, string reason)
        {
            if (run == null)
            {
                return false;
            }

            CurrentRevision = Math.Max(CurrentRevision + 1, 1);
            return Save(run, true, reason);
        }

        public bool RetrySave(RunSession run, string reason = "ManualRetry")
        {
            return Save(run, true, reason);
        }

        public void Reset()
        {
            CurrentRevision = 0;
            LastSavedRevision = 0;
            LastSavedAtUtc = null;
            LastError = string.Empty;
        }

        private bool Save(RunSession run, bool force, string reason = "NewRun")
        {
            if (!Enabled)
            {
                LastSavedRevision = CurrentRevision;
                LastSavedAtUtc = DateTime.UtcNow;
                return true;
            }

            if (!force && !HasUnsavedChanges)
            {
                return true;
            }

            try
            {
                var document = repository.Save(run, Math.Max(1, CurrentRevision));
                LastSavedRevision = CurrentRevision;
                LastSavedAtUtc = document.SavedAtUtc;
                LastError = string.Empty;
                Debug.Log(
                    $"[Save] Run saved. reason={reason}, revision={CurrentRevision}, " +
                    $"phase={run.State.Phase}.");
                return true;
            }
            catch (Exception exception)
            {
                LastError = exception.GetType().Name + ": " + exception.Message;
                Debug.LogError(
                    $"[Save] Run save failed. reason={reason}, revision={CurrentRevision}, " +
                    $"error={LastError}");
                return false;
            }
        }
    }
}

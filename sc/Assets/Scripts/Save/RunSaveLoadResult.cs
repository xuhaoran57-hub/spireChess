using SpireChess.Run;

namespace SpireChess.Save
{
    public enum RunSaveLoadStatus
    {
        Missing,
        Valid,
        RecoveredFromBackup,
        CorruptJson,
        ChecksumMismatch,
        UnsupportedSchema,
        IncompatibleContent,
        InvalidReference,
        InvalidDomainState,
        RandomReplayMismatch,
        IoFailure
    }

    public sealed class RunSaveLoadResult
    {
        public RunSaveLoadResult(
            RunSaveLoadStatus status,
            RunSaveDocumentV1 document = null,
            RunSession session = null,
            string diagnostic = null,
            RunSaveLoadStatus? primaryFailure = null)
        {
            Status = status;
            Document = document;
            Session = session;
            Diagnostic = diagnostic ?? string.Empty;
            PrimaryFailure = primaryFailure;
        }

        public RunSaveLoadStatus Status { get; }
        public RunSaveDocumentV1 Document { get; }
        public RunSession Session { get; }
        public string Diagnostic { get; }
        public RunSaveLoadStatus? PrimaryFailure { get; }
        public bool CanContinue => Status == RunSaveLoadStatus.Valid ||
                                   Status == RunSaveLoadStatus.RecoveredFromBackup;
        public bool UsedBackup => Status == RunSaveLoadStatus.RecoveredFromBackup;
    }
}

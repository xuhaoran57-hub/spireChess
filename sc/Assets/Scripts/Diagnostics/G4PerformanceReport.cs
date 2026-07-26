using System;

namespace SpireChess.Diagnostics
{
    [Serializable]
    public sealed class G4PerformanceReport
    {
        public string schemaVersion;
        public string runId;
        public string startedAtUtc;
        public string completedAtUtc;
        public string completionStatus;
        public string completionMessage;
        public bool provisional;
        public string provisionalReason;
        public G4EnvironmentSnapshot environment;
        public G4RunConfiguration configuration;
        public G4AudioSnapshot audio;
        public G4PerformanceSummary overall;
        public G4ScenePerformanceSummary[] scenes;
        public G4SceneLoadRecord[] sceneLoads;
        public G4CheckpointRecord[] checkpoints;
        public G4ArtworkSummary artwork;
        public G4CleanupSnapshot cleanup;
        public string[] unavailableProfilerCounters;
        public string samplesCsvPath;
    }

    [Serializable]
    public sealed class G4EnvironmentSnapshot
    {
        public string machineName;
        public string operatingSystem;
        public string platform;
        public string deviceModel;
        public string processorType;
        public int processorCount;
        public int processorFrequencyMhz;
        public int systemMemoryMb;
        public string graphicsDeviceName;
        public string graphicsDeviceVendor;
        public string graphicsDeviceType;
        public string graphicsDeviceVersion;
        public int graphicsMemoryMb;
        public string unityVersion;
        public string applicationVersion;
        public string buildGuid;
        public string companyName;
        public string productName;
        public bool developmentBuild;
        public string persistentDataPath;
        public string injectedSaveRoot;
    }

    [Serializable]
    public sealed class G4RunConfiguration
    {
        public int requestedWidth;
        public int requestedHeight;
        public int actualWidth;
        public int actualHeight;
        public string fullScreenMode;
        public int refreshRateHz;
        public int qualityLevel;
        public string qualityName;
        public int vSyncCount;
        public int antiAliasing;
        public int textureQualityLimit;
        public int targetFrameRate;
        public string colorSpace;
        public int audioSampleRate;
        public int audioDspBufferSize;
        public string audioSpeakerMode;
        public float warmupSeconds;
        public float requestedDurationSeconds;
        public float objectSampleIntervalSeconds;
        public string acceptanceSeed;
    }

    [Serializable]
    public sealed class G4AudioSnapshot
    {
        public string assetStatus;
        public bool productionStrictReady;
        public bool memoryResultProvisional;
        public int cueCount;
        public int playableCueCount;
        public int clipVariantCount;
        public int pendingCueCount;
        public int placeholderCueCount;
        public int productionApprovedCueCount;
        public int commissioningErrorCount;
        public int commissioningWarningCount;
    }

    [Serializable]
    public sealed class G4PerformanceSummary
    {
        public int sampleCount;
        public float measuredSeconds;
        public G4MetricDistribution frameTimeMs;
        public G4MetricDistribution mainThreadTimeMs;
        public G4MetricDistribution gcAllocatedBytesPerFrame;
        public long peakTotalUsedMemoryBytes;
        public long finalTotalUsedMemoryBytes;
        public long peakGcUsedMemoryBytes;
        public long finalGcUsedMemoryBytes;
        public long peakTextureMemoryBytes;
        public long finalTextureMemoryBytes;
        public long peakAudioMemoryBytes;
        public long finalAudioMemoryBytes;
    }

    [Serializable]
    public sealed class G4ScenePerformanceSummary
    {
        public string sceneName;
        public int sampleCount;
        public float measuredSeconds;
        public G4MetricDistribution frameTimeMs;
        public G4MetricDistribution mainThreadTimeMs;
        public long peakTotalUsedMemoryBytes;
        public long peakGcUsedMemoryBytes;
        public long peakTextureMemoryBytes;
        public long peakAudioMemoryBytes;
    }

    [Serializable]
    public sealed class G4MetricDistribution
    {
        public int sampleCount;
        public double minimum;
        public double average;
        public double p50;
        public double p95;
        public double p99;
        public double maximum;
    }

    [Serializable]
    public sealed class G4SceneLoadRecord
    {
        public int sequence;
        public string sceneName;
        public string requestKind;
        public double requestedAtSeconds;
        public double loadedAtSeconds;
        public double firstFrameAtSeconds;
        public double loadDurationMs;
        public double activationToFirstFrameMs;
        public double firstFrameTimeMs;
        public long totalUsedMemoryAtFirstFrameBytes;
        public long textureMemoryAtFirstFrameBytes;
        public long audioMemoryAtFirstFrameBytes;
        public int activePresentationFxAtFirstFrame;
        public int activeNonLoopingAudioSourcesAtFirstFrame;
    }

    [Serializable]
    public sealed class G4CheckpointRecord
    {
        public int sequence;
        public string checkpoint;
        public string sceneName;
        public double elapsedSeconds;
        public bool passed;
        public string details;
        public string screenshotPath;
        public int activePresentationFx;
        public int activeNonLoopingAudioSources;
        public bool battleAnimationPlaying;
        public int artworkExactCount;
        public int artworkFallbackCount;
        public int artworkDiagnosticCount;
        public int artworkMissingCount;
        public bool sampleScopeExact;
        public G4ArtworkObservation[] artworkObservations;
    }

    [Serializable]
    public sealed class G4ArtworkObservation
    {
        public string source;
        public string artId;
        public string resolution;
        public bool sampleScope;
        public int instanceCount;
    }

    [Serializable]
    public sealed class G4ArtworkSummary
    {
        public int checkpointObservationCount;
        public int exactCount;
        public int fallbackCount;
        public int diagnosticCount;
        public int missingCount;
        public int sampleScopeViolationCount;
        public bool visibleSampleScopeExact;
        public int catalogExpectedCount;
        public int catalogExactCount;
        public string[] catalogMissingArtIds;
        public bool catalogExact;
        public bool sampleScopeExact;
        public string interpretation;
    }

    [Serializable]
    public sealed class G4CleanupSnapshot
    {
        public int maximumActivePresentationFx;
        public int finalActivePresentationFx;
        public int maximumActiveNonLoopingAudioSources;
        public int finalActiveNonLoopingAudioSources;
        public bool finalBattleAnimationPlaying;
        public bool cleanAtCompletion;
        public string interpretation;
    }
}

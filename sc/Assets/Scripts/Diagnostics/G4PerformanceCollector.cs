using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SpireChess.App;
using SpireChess.Audio;
using SpireChess.UI;
using SpireChess.UI.Battle;
using SpireChess.UI.Run;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SpireChess.Diagnostics
{
    [DefaultExecutionOrder(-900)]
    public sealed class G4PerformanceCollector : MonoBehaviour
    {
        private const int MaximumFrameSamples = 300000;
        private const int MaximumRuntimeFailureSummaries = 32;
        private const string ReportSchemaVersion =
            "spire-chess-g4-performance-v2";

        private struct FrameSample
        {
            public double ElapsedSeconds;
            public string SceneName;
            public double FrameTimeMs;
            public long MainThreadNanoseconds;
            public long GcAllocatedBytes;
            public long TotalUsedMemoryBytes;
            public long GcUsedMemoryBytes;
            public long TextureMemoryBytes;
            public long AudioMemoryBytes;
            public int ActivePresentationFx;
            public int ActiveNonLoopingAudioSources;
            public bool BattleAnimationPlaying;
        }

        private sealed class CounterRecorder : IDisposable
        {
            private ProfilerRecorder recorder;

            public CounterRecorder(ProfilerCategory category, string name)
            {
                Name = name;
                try
                {
                    recorder = ProfilerRecorder.StartNew(category, name, 1);
                    Available = recorder.Valid;
                }
                catch (Exception)
                {
                    Available = false;
                }
            }

            public string Name { get; }
            public bool Available { get; }

            public long Read()
            {
                if (!Available || !recorder.Valid)
                {
                    return -1L;
                }

                try
                {
                    return Math.Max(0L, recorder.LastValue);
                }
                catch (InvalidOperationException)
                {
                    return -1L;
                }
            }

            public void Dispose()
            {
                if (recorder.Valid)
                {
                    recorder.Dispose();
                }
            }
        }

        private struct TransientSnapshot
        {
            public int ActivePresentationFx;
            public int ActiveNonLoopingAudioSources;
            public bool BattleAnimationPlaying;
        }

        private sealed class ArtworkAudit
        {
            public G4ArtworkObservation[] Observations;
            public int ExactCount;
            public int FallbackCount;
            public int DiagnosticCount;
            public int MissingCount;
            public int SampleScopeViolationCount;
            public bool SampleScopeExact =>
                SampleScopeViolationCount == 0 && MissingCount == 0;
        }

        private const string MissingArtIdMarker = "<missing-art-id>";

        private static readonly HashSet<string> SampleScopeArtIds =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "placeholder_card_forge_soul_shield_squire",
                "placeholder_card_tempering_mender",
                "placeholder_card_cracked_armor_avenger",
                "placeholder_card_undying_furnace_king",
                "placeholder_card_young_deer_spirit",
                "placeholder_card_rotleaf_heir",
                "placeholder_card_fox_den_matriarch",
                "placeholder_card_ten_thousand_hoof_surge",
                "placeholder_card_astrolabe_calibrator",
                "placeholder_card_secret_page_refractor",
                "placeholder_card_star_map_broker",
                "placeholder_card_sky_covenant_bearer",
                "placeholder_token_young_spirit",
                "placeholder_token_two_tailed_fox_shadow",
                "placeholder_token_swift_young_spirit",
                "placeholder_spell_minor_tempering",
                "placeholder_spell_free_refresh",
                "placeholder_spell_advanced_discovery",
                "placeholder_spell_prebattle_benediction",
                "icon_relic_crown_echo_bell",
                "icon_relic_crown_thousand_shields",
                "icon_relic_curio_refresh_gear"
            };

        private static G4PerformanceCollector instance;

        private readonly List<FrameSample> samples =
            new List<FrameSample>(8192);
        private readonly List<G4SceneLoadRecord> sceneLoads =
            new List<G4SceneLoadRecord>();
        private readonly List<G4CheckpointRecord> checkpoints =
            new List<G4CheckpointRecord>();
        private readonly Dictionary<string, double> pendingSceneLoads =
            new Dictionary<string, double>(StringComparer.Ordinal);
        private readonly List<CounterRecorder> counters =
            new List<CounterRecorder>();
        private readonly List<string> runtimeFailureSummaries =
            new List<string>();
        private readonly object runtimeLogLock = new object();

        private CounterRecorder totalUsedMemory;
        private CounterRecorder gcUsedMemory;
        private CounterRecorder textureMemory;
        private CounterRecorder audioMemory;
        private CounterRecorder gcAllocatedInFrame;
        private CounterRecorder mainThreadTime;
        private DateTime startedAtUtc;
        private double startedAtSeconds;
        private double measuredFromSeconds;
        private double lastTransientScanSeconds;
        private float warmupSeconds;
        private float requestedDurationSeconds;
        private float transientSampleIntervalSeconds;
        private string outputDirectory;
        private string runtimeFailureMarkerPath;
        private string runId;
        private bool autoQuit;
        private bool completed;
        private bool sampleLimitWarningLogged;
        private int pendingFirstFrameIndex = -1;
        private int pendingFirstFrameRequestedOnFrame = -1;
        private int maximumActivePresentationFx;
        private int maximumActiveNonLoopingAudioSources;
        private int runtimeErrorCount;
        private int runtimeExceptionCount;
        private int runtimeAssertCount;
        private TransientSnapshot currentTransient;
        private PresentationFxPool[] trackedFxPools =
            Array.Empty<PresentationFxPool>();
        private BattleScreenView[] trackedBattleScreens =
            Array.Empty<BattleScreenView>();
        private AudioSource[] trackedAudioSources =
            Array.Empty<AudioSource>();
        private bool sampleCatalogAudited;
        private int sampleCatalogExactCount;
        private string[] sampleCatalogMissingArtIds = Array.Empty<string>();
        private string writtenReportPath;

        public static G4PerformanceCollector Instance => instance;
        public string OutputDirectory => outputDirectory;
        public string RunId => runId;
        public string WrittenReportPath => writtenReportPath;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (!G4RuntimeArguments.IsPerformanceRequested || instance != null)
            {
                return;
            }

            var root = new GameObject(nameof(G4PerformanceCollector));
            DontDestroyOnLoad(root);
            root.AddComponent<G4PerformanceCollector>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            ConfigureRun();
            StartCounters();
            startedAtUtc = DateTime.UtcNow;
            startedAtSeconds = Time.realtimeSinceStartupAsDouble;
            measuredFromSeconds = startedAtSeconds + warmupSeconds;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            G4SceneLoadDiagnostics.SceneLoadRequested += OnSceneLoadRequested;
            Application.logMessageReceivedThreaded += OnRuntimeLog;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            G4SceneLoadDiagnostics.SceneLoadRequested -= OnSceneLoadRequested;
            Application.logMessageReceivedThreaded -= OnRuntimeLog;
        }

        private void Update()
        {
            if (completed)
            {
                return;
            }

            var now = Time.realtimeSinceStartupAsDouble;
            if (now - lastTransientScanSeconds >= transientSampleIntervalSeconds)
            {
                currentTransient = CaptureTransientSnapshot();
                lastTransientScanSeconds = now;
                maximumActivePresentationFx = Math.Max(
                    maximumActivePresentationFx,
                    currentTransient.ActivePresentationFx);
                maximumActiveNonLoopingAudioSources = Math.Max(
                    maximumActiveNonLoopingAudioSources,
                    currentTransient.ActiveNonLoopingAudioSources);
            }

            if (now >= measuredFromSeconds)
            {
                CaptureFrameSample(now);
            }

            if (!G4RuntimeArguments.IsAcceptanceRequested &&
                requestedDurationSeconds > 0f &&
                now - measuredFromSeconds >= requestedDurationSeconds)
            {
                var reportPath = CompleteSession(
                    "PerformanceCompleted",
                    "Requested standalone performance duration completed.");
                if (autoQuit)
                {
                    Application.Quit(
                        string.IsNullOrWhiteSpace(reportPath) ? 1 : 0);
                }
            }
        }

        private void LateUpdate()
        {
            if (completed ||
                pendingFirstFrameIndex < 0 ||
                pendingFirstFrameIndex >= sceneLoads.Count ||
                Time.frameCount <= pendingFirstFrameRequestedOnFrame)
            {
                return;
            }

            var record = sceneLoads[pendingFirstFrameIndex];
            record.firstFrameAtSeconds = Time.realtimeSinceStartupAsDouble;
            record.activationToFirstFrameMs = Math.Max(
                0d,
                (record.firstFrameAtSeconds - record.loadedAtSeconds) * 1000d);
            record.firstFrameTimeMs = Time.unscaledDeltaTime * 1000d;
            record.totalUsedMemoryAtFirstFrameBytes = totalUsedMemory.Read();
            record.textureMemoryAtFirstFrameBytes = textureMemory.Read();
            record.audioMemoryAtFirstFrameBytes = audioMemory.Read();
            var transient = CaptureTransientSnapshot();
            record.activePresentationFxAtFirstFrame =
                transient.ActivePresentationFx;
            record.activeNonLoopingAudioSourcesAtFirstFrame =
                transient.ActiveNonLoopingAudioSources;
            pendingFirstFrameIndex = -1;
            pendingFirstFrameRequestedOnFrame = -1;
        }

        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }

            DisposeCounters();
            instance = null;
        }

        private void OnApplicationQuit()
        {
            if (!completed)
            {
                CompleteSession(
                    "Interrupted",
                    "Player quit before the requested G4 session completed.");
            }
        }

        public static void RecordCheckpoint(
            string checkpoint,
            bool passed,
            string details,
            string screenshotPath)
        {
            instance?.RecordCheckpointInternal(
                checkpoint,
                passed,
                details,
                screenshotPath);
        }

        public static bool ValidateCurrentSampleArtwork(out string details)
        {
            var audit = CaptureArtworkAudit();
            details = BuildArtworkAuditDetails(audit);
            return audit.SampleScopeExact;
        }

        public static bool ValidateFullSampleCatalog(out string details)
        {
            if (instance == null)
            {
                details = "G4 performance collector is unavailable.";
                return false;
            }

            var catalogs = Resources
                .FindObjectsOfTypeAll<PresentationSpriteCatalog>()
                .Where(value => value != null)
                .GroupBy(value => value.GetInstanceID())
                .Select(group => group.First())
                .ToArray();
            if (catalogs.Length != 1)
            {
                instance.sampleCatalogAudited = true;
                instance.sampleCatalogExactCount = 0;
                instance.sampleCatalogMissingArtIds =
                    SampleScopeArtIds.OrderBy(
                        value => value,
                        StringComparer.Ordinal).ToArray();
                details =
                    "Expected exactly one loaded PresentationSpriteCatalog " +
                    $"but found {catalogs.Length}.";
                return false;
            }

            var missing = SampleScopeArtIds
                .Where(artId =>
                    !catalogs[0].TryGetArtwork(
                        artId,
                        out var sprite,
                        out _) ||
                    sprite == null)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            instance.sampleCatalogAudited = true;
            instance.sampleCatalogExactCount =
                SampleScopeArtIds.Count - missing.Length;
            instance.sampleCatalogMissingArtIds = missing;
            details =
                $"catalog exact={instance.sampleCatalogExactCount}/" +
                $"{SampleScopeArtIds.Count}; missing=" +
                (missing.Length == 0
                    ? "<none>"
                    : string.Join(", ", missing));
            return missing.Length == 0;
        }

        public static bool ValidateCurrentCleanup(out string details)
        {
            if (instance == null)
            {
                details = "G4 performance collector is unavailable.";
                return false;
            }

            instance.RefreshTransientTargets();
            var transient = instance.CaptureTransientSnapshot();
            details =
                $"activeFx={transient.ActivePresentationFx}, " +
                $"activeNonLoopAudio=" +
                $"{transient.ActiveNonLoopingAudioSources}, " +
                $"battleAnimation={transient.BattleAnimationPlaying}";
            return transient.ActivePresentationFx == 0 &&
                   transient.ActiveNonLoopingAudioSources == 0 &&
                   !transient.BattleAnimationPlaying;
        }

        public static bool ValidateNoRuntimeFailures(out string details)
        {
            if (instance == null)
            {
                details = "G4 performance collector is unavailable.";
                return false;
            }

            var summary = instance.BuildRuntimeLogSummary();
            details =
                $"errors={summary.errorCount}, " +
                $"exceptions={summary.exceptionCount}, " +
                $"asserts={summary.assertCount}" +
                (summary.firstFailures.Length == 0
                    ? string.Empty
                    : "; first=" + summary.firstFailures[0]);
            return summary.clean;
        }

        public static string Complete(
            string completionStatus,
            string completionMessage)
        {
            return instance == null
                ? null
                : instance.CompleteSession(
                    completionStatus,
                    completionMessage);
        }

        private void ConfigureRun()
        {
            warmupSeconds = G4RuntimeArguments.ReadFloat(
                G4RuntimeArguments.WarmupArgument,
                2f,
                0f,
                60f);
            requestedDurationSeconds = G4RuntimeArguments.ReadFloat(
                G4RuntimeArguments.DurationArgument,
                G4RuntimeArguments.IsAcceptanceRequested ? 0f : 60f,
                0f,
                3600f);
            transientSampleIntervalSeconds = G4RuntimeArguments.ReadFloat(
                G4RuntimeArguments.SampleIntervalArgument,
                0.25f,
                0.05f,
                10f);
            autoQuit = G4RuntimeArguments.HasFlag(
                G4RuntimeArguments.AutoQuitFlag);

            var requestedOutput = G4RuntimeArguments.Read(
                G4RuntimeArguments.OutputArgument);
            outputDirectory = string.IsNullOrWhiteSpace(requestedOutput)
                ? Path.Combine(
                    Application.persistentDataPath,
                    "G4Performance")
                : G4RuntimeArguments.RequireAbsolutePath(
                    G4RuntimeArguments.OutputArgument);
            Directory.CreateDirectory(outputDirectory);
            runtimeFailureMarkerPath = Path.Combine(
                outputDirectory,
                "g4-runtime-failures.log");
            if (File.Exists(runtimeFailureMarkerPath))
            {
                File.Delete(runtimeFailureMarkerPath);
            }

            runId = G4RuntimeArguments.SanitizeFileName(
                G4RuntimeArguments.Read(G4RuntimeArguments.RunIdArgument),
                DateTime.UtcNow.ToString(
                    "yyyyMMdd-HHmmss",
                    CultureInfo.InvariantCulture));

            ApplyRequestedQuality();
            if (G4RuntimeArguments.TryReadResolution(
                    out var width,
                    out var height))
            {
                Screen.SetResolution(
                    width,
                    height,
                    FullScreenMode.Windowed);
            }

            Debug.Log(
                $"[G4] Performance collection started. runId={runId}, " +
                $"output={outputDirectory}, saveRoot=" +
                $"{G4RuntimeArguments.Read(G4RuntimeArguments.SaveRootArgument) ?? "<default>"}.");
        }

        private static void ApplyRequestedQuality()
        {
            var raw = G4RuntimeArguments.Read(
                G4RuntimeArguments.QualityArgument);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return;
            }

            var names = QualitySettings.names ?? Array.Empty<string>();
            var index = -1;
            if (int.TryParse(
                    raw,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var parsed))
            {
                index = parsed;
            }
            else
            {
                index = Array.FindIndex(names, value =>
                    string.Equals(
                        value,
                        raw,
                        StringComparison.OrdinalIgnoreCase));
            }

            if (index < 0 || index >= names.Length)
            {
                Debug.LogWarning(
                    $"[G4] Requested quality '{raw}' is unavailable; " +
                    $"using '{QualitySettings.names[QualitySettings.GetQualityLevel()]}'.");
                return;
            }

            QualitySettings.SetQualityLevel(index, true);
        }

        private void StartCounters()
        {
            totalUsedMemory = AddCounter(
                ProfilerCategory.Memory,
                "Total Used Memory");
            gcUsedMemory = AddCounter(
                ProfilerCategory.Memory,
                "GC Used Memory");
            textureMemory = AddCounter(
                ProfilerCategory.Memory,
                "Texture Memory");
            audioMemory = AddCounter(
                ProfilerCategory.Memory,
                "Audio Used Memory");
            gcAllocatedInFrame = AddCounter(
                ProfilerCategory.Memory,
                "GC Allocated In Frame");
            mainThreadTime = AddCounter(
                ProfilerCategory.Internal,
                "Main Thread");
        }

        private CounterRecorder AddCounter(
            ProfilerCategory category,
            string name)
        {
            var value = new CounterRecorder(category, name);
            counters.Add(value);
            if (!value.Available)
            {
                Debug.LogWarning(
                    $"[G4] Profiler counter unavailable: {name}.");
            }

            return value;
        }

        private void DisposeCounters()
        {
            foreach (var counter in counters)
            {
                counter.Dispose();
            }
            counters.Clear();
        }

        private void CaptureFrameSample(double now)
        {
            if (samples.Count >= MaximumFrameSamples)
            {
                if (!sampleLimitWarningLogged)
                {
                    sampleLimitWarningLogged = true;
                    Debug.LogWarning(
                        $"[G4] Frame sample cap {MaximumFrameSamples} reached.");
                }
                return;
            }

            samples.Add(new FrameSample
            {
                ElapsedSeconds = now - startedAtSeconds,
                SceneName = SceneManager.GetActiveScene().name,
                FrameTimeMs = Time.unscaledDeltaTime * 1000d,
                MainThreadNanoseconds = mainThreadTime.Read(),
                GcAllocatedBytes = gcAllocatedInFrame.Read(),
                TotalUsedMemoryBytes = totalUsedMemory.Read(),
                GcUsedMemoryBytes = gcUsedMemory.Read(),
                TextureMemoryBytes = textureMemory.Read(),
                AudioMemoryBytes = audioMemory.Read(),
                ActivePresentationFx =
                    currentTransient.ActivePresentationFx,
                ActiveNonLoopingAudioSources =
                    currentTransient.ActiveNonLoopingAudioSources,
                BattleAnimationPlaying =
                    currentTransient.BattleAnimationPlaying
            });
        }

        private void OnSceneLoadRequested(string sceneName, double requestedAt)
        {
            pendingSceneLoads[sceneName] = requestedAt;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var loadedAt = Time.realtimeSinceStartupAsDouble;
            var hasRequest = pendingSceneLoads.TryGetValue(
                scene.name,
                out var requestedAt);
            if (hasRequest)
            {
                pendingSceneLoads.Remove(scene.name);
            }
            else
            {
                requestedAt = startedAtSeconds > 0d
                    ? startedAtSeconds
                    : 0d;
            }

            sceneLoads.Add(new G4SceneLoadRecord
            {
                sequence = sceneLoads.Count + 1,
                sceneName = scene.name,
                requestKind = hasRequest ? "Routed" : "StartupOrExternal",
                requestedAtSeconds = requestedAt,
                loadedAtSeconds = loadedAt,
                loadDurationMs = Math.Max(
                    0d,
                    (loadedAt - requestedAt) * 1000d)
            });
            pendingFirstFrameIndex = sceneLoads.Count - 1;
            pendingFirstFrameRequestedOnFrame = Time.frameCount;
            RefreshTransientTargets();
        }

        private void RecordCheckpointInternal(
            string checkpoint,
            bool passed,
            string details,
            string screenshotPath)
        {
            var transient = CaptureTransientSnapshot();
            var artwork = CaptureArtworkAudit();
            maximumActivePresentationFx = Math.Max(
                maximumActivePresentationFx,
                transient.ActivePresentationFx);
            maximumActiveNonLoopingAudioSources = Math.Max(
                maximumActiveNonLoopingAudioSources,
                transient.ActiveNonLoopingAudioSources);
            checkpoints.Add(new G4CheckpointRecord
            {
                sequence = checkpoints.Count + 1,
                checkpoint = checkpoint ?? string.Empty,
                sceneName = SceneManager.GetActiveScene().name,
                elapsedSeconds =
                    Time.realtimeSinceStartupAsDouble - startedAtSeconds,
                passed = passed,
                details = details ?? string.Empty,
                screenshotPath = screenshotPath ?? string.Empty,
                activePresentationFx =
                    transient.ActivePresentationFx,
                activeNonLoopingAudioSources =
                    transient.ActiveNonLoopingAudioSources,
                battleAnimationPlaying =
                    transient.BattleAnimationPlaying,
                artworkExactCount = artwork.ExactCount,
                artworkFallbackCount = artwork.FallbackCount,
                artworkDiagnosticCount = artwork.DiagnosticCount,
                artworkMissingCount = artwork.MissingCount,
                sampleScopeExact = artwork.SampleScopeExact,
                artworkObservations = artwork.Observations
            });
        }

        private string CompleteSession(
            string completionStatus,
            string completionMessage)
        {
            if (completed)
            {
                return writtenReportPath;
            }

            try
            {
                currentTransient = CaptureTransientSnapshot();
                maximumActivePresentationFx = Math.Max(
                    maximumActivePresentationFx,
                    currentTransient.ActivePresentationFx);
                maximumActiveNonLoopingAudioSources = Math.Max(
                    maximumActiveNonLoopingAudioSources,
                    currentTransient.ActiveNonLoopingAudioSources);

                var audio = CaptureAudioSnapshot();
                var report = BuildReport(
                    completionStatus,
                    completionMessage,
                    audio);
                var stem =
                    $"g4-performance-{runId}-{Screen.width}x{Screen.height}";
                var csvPath = Path.Combine(outputDirectory, stem + ".csv");
                var jsonPath = Path.Combine(outputDirectory, stem + ".json");
                WriteSamplesCsv(csvPath);
                report.samplesCsvPath = csvPath;
                WriteJsonAtomic(jsonPath, JsonUtility.ToJson(report, true));
                writtenReportPath = jsonPath;
                completed = true;
                DisposeCounters();

                Debug.Log(
                    $"[G4] Performance report written: {jsonPath}. " +
                    $"status={completionStatus}, " +
                    $"provisional={report.provisional}.");
                return jsonPath;
            }
            catch (Exception exception)
            {
                completed = true;
                DisposeCounters();
                Debug.LogError(
                    "[G4] Failed to finalize performance evidence: " +
                    $"{exception.GetType().Name}: {exception.Message}");
                return null;
            }
        }

        private G4PerformanceReport BuildReport(
            string completionStatus,
            string completionMessage,
            G4AudioSnapshot audio)
        {
            var unavailable = counters
                .Where(value => !value.Available)
                .Select(value => value.Name)
                .ToArray();
            var provisional = !audio.productionStrictReady;
            return new G4PerformanceReport
            {
                schemaVersion = ReportSchemaVersion,
                runId = runId,
                startedAtUtc = startedAtUtc.ToString(
                    "O",
                    CultureInfo.InvariantCulture),
                completedAtUtc = DateTime.UtcNow.ToString(
                    "O",
                    CultureInfo.InvariantCulture),
                completionStatus = completionStatus ?? string.Empty,
                completionMessage = completionMessage ?? string.Empty,
                provisional = provisional,
                provisionalReason = provisional
                    ? "Formal audio is not ProductionStrict-ready; audio " +
                      "memory and listening conclusions are provisional."
                    : string.Empty,
                environment = CaptureEnvironment(),
                configuration = CaptureConfiguration(),
                audio = audio,
                overall = BuildOverallSummary(samples),
                scenes = samples
                    .GroupBy(value => value.SceneName ?? string.Empty)
                    .OrderBy(value => value.Key, StringComparer.Ordinal)
                    .Select(BuildSceneSummary)
                    .ToArray(),
                sceneLoads = sceneLoads.ToArray(),
                checkpoints = checkpoints.ToArray(),
                artwork = BuildArtworkSummary(),
                runtimeLogs = BuildRuntimeLogSummary(),
                cleanup = new G4CleanupSnapshot
                {
                    maximumActivePresentationFx =
                        maximumActivePresentationFx,
                    finalActivePresentationFx =
                        currentTransient.ActivePresentationFx,
                    maximumActiveNonLoopingAudioSources =
                        maximumActiveNonLoopingAudioSources,
                    finalActiveNonLoopingAudioSources =
                        currentTransient.ActiveNonLoopingAudioSources,
                    finalBattleAnimationPlaying =
                        currentTransient.BattleAnimationPlaying,
                    cleanAtCompletion =
                        currentTransient.ActivePresentationFx == 0 &&
                        currentTransient.ActiveNonLoopingAudioSources == 0 &&
                        !currentTransient.BattleAnimationPlaying,
                    interpretation =
                        "Inactive pooled entries are intentionally retained. " +
                        "Clean means no active presentation FX, non-looping " +
                        "AudioSource, or battle animation at completion."
                },
                unavailableProfilerCounters = unavailable
            };
        }

        private void OnRuntimeLog(
            string condition,
            string stackTrace,
            LogType type)
        {
            if (type != LogType.Error &&
                type != LogType.Exception &&
                type != LogType.Assert)
            {
                return;
            }

            lock (runtimeLogLock)
            {
                switch (type)
                {
                    case LogType.Error:
                        runtimeErrorCount++;
                        break;
                    case LogType.Exception:
                        runtimeExceptionCount++;
                        break;
                    case LogType.Assert:
                        runtimeAssertCount++;
                        break;
                }

                var summary = string.IsNullOrWhiteSpace(condition)
                    ? type.ToString()
                    : condition.Trim();
                if (summary.Length > 500)
                {
                    summary = summary.Substring(0, 500);
                }
                if (runtimeFailureSummaries.Count <
                    MaximumRuntimeFailureSummaries)
                {
                    runtimeFailureSummaries.Add($"{type}: {summary}");
                }

                try
                {
                    if (!string.IsNullOrWhiteSpace(
                            runtimeFailureMarkerPath))
                    {
                        var singleLine = summary
                            .Replace("\r", " ")
                            .Replace("\n", " ");
                        File.AppendAllText(
                            runtimeFailureMarkerPath,
                            $"{DateTime.UtcNow:O}\t{type}\t{singleLine}" +
                            Environment.NewLine,
                            new UTF8Encoding(false));
                    }
                }
                catch
                {
                    // Do not recursively log from inside Unity's threaded log
                    // callback. The in-memory failure count still prevents an
                    // AcceptancePassed report while the collector is active.
                }
            }
        }

        private G4RuntimeLogSummary BuildRuntimeLogSummary()
        {
            lock (runtimeLogLock)
            {
                var total = runtimeErrorCount +
                            runtimeExceptionCount +
                            runtimeAssertCount;
                return new G4RuntimeLogSummary
                {
                    errorCount = runtimeErrorCount,
                    exceptionCount = runtimeExceptionCount,
                    assertCount = runtimeAssertCount,
                    totalFailureCount = total,
                    firstFailures = runtimeFailureSummaries.ToArray(),
                    clean = total == 0
                };
            }
        }

        private static G4EnvironmentSnapshot CaptureEnvironment()
        {
            var app = GameApp.Instance;
            return new G4EnvironmentSnapshot
            {
                machineName = Environment.MachineName,
                operatingSystem = SystemInfo.operatingSystem,
                platform = Application.platform.ToString(),
                deviceModel = SystemInfo.deviceModel,
                processorType = SystemInfo.processorType,
                processorCount = SystemInfo.processorCount,
                processorFrequencyMhz = SystemInfo.processorFrequency,
                systemMemoryMb = SystemInfo.systemMemorySize,
                graphicsDeviceName = SystemInfo.graphicsDeviceName,
                graphicsDeviceVendor = SystemInfo.graphicsDeviceVendor,
                graphicsDeviceType = SystemInfo.graphicsDeviceType.ToString(),
                graphicsDeviceVersion = SystemInfo.graphicsDeviceVersion,
                graphicsMemoryMb = SystemInfo.graphicsMemorySize,
                unityVersion = Application.unityVersion,
                applicationVersion = Application.version,
                buildGuid = Application.buildGUID,
                companyName = Application.companyName,
                productName = Application.productName,
                developmentBuild = Debug.isDebugBuild,
                persistentDataPath = Application.persistentDataPath,
                injectedSaveRoot = app?.SaveRootPath ??
                    G4RuntimeArguments.Read(
                        G4RuntimeArguments.SaveRootArgument) ??
                    string.Empty
            };
        }

        private G4RunConfiguration CaptureConfiguration()
        {
            G4RuntimeArguments.TryReadResolution(
                out var requestedWidth,
                out var requestedHeight);
            var configuration = AudioSettings.GetConfiguration();
            var qualityLevel = QualitySettings.GetQualityLevel();
            var qualityNames = QualitySettings.names ?? Array.Empty<string>();
            return new G4RunConfiguration
            {
                requestedWidth = requestedWidth,
                requestedHeight = requestedHeight,
                actualWidth = Screen.width,
                actualHeight = Screen.height,
                fullScreenMode = Screen.fullScreenMode.ToString(),
                refreshRateHz = (int)Math.Round(
                    Screen.currentResolution.refreshRateRatio.value),
                qualityLevel = qualityLevel,
                qualityName =
                    qualityLevel >= 0 && qualityLevel < qualityNames.Length
                        ? qualityNames[qualityLevel]
                        : string.Empty,
                vSyncCount = QualitySettings.vSyncCount,
                antiAliasing = QualitySettings.antiAliasing,
                textureQualityLimit = QualitySettings.globalTextureMipmapLimit,
                targetFrameRate = Application.targetFrameRate,
                colorSpace = QualitySettings.activeColorSpace.ToString(),
                audioSampleRate = configuration.sampleRate,
                audioDspBufferSize = configuration.dspBufferSize,
                audioSpeakerMode = configuration.speakerMode.ToString(),
                warmupSeconds = warmupSeconds,
                requestedDurationSeconds = requestedDurationSeconds,
                objectSampleIntervalSeconds =
                    transientSampleIntervalSeconds,
                acceptanceSeed = G4RuntimeArguments.Read(
                    G4RuntimeArguments.AcceptanceSeedArgument) ?? string.Empty
            };
        }

        private static G4AudioSnapshot CaptureAudioSnapshot()
        {
            var catalog = AudioService.Instance?.Catalog ??
                          Resources.Load<PresentationAudioCatalog>(
                              PresentationAudioCatalog.DefaultResourcesPath);
            if (catalog == null)
            {
                return new G4AudioSnapshot
                {
                    assetStatus = "Missing",
                    memoryResultProvisional = true
                };
            }

            var cues = catalog.Cues.Where(value => value != null).ToArray();
            var commissioning = catalog.Validate(
                PresentationAudioCatalogValidationMode.Commissioning);
            var strict = catalog.Validate(
                PresentationAudioCatalogValidationMode.ProductionStrict);
            var pending = cues.Count(value =>
                value.AssetStatus == PresentationAudioCueAssetStatus.Pending);
            var placeholders = cues.Count(value =>
                value.AssetStatus ==
                PresentationAudioCueAssetStatus.Placeholder);
            var approved = cues.Count(value =>
                value.AssetStatus ==
                PresentationAudioCueAssetStatus.ProductionApproved);
            var status = placeholders > 0
                ? "Placeholder"
                : pending > 0
                    ? "Pending"
                    : strict.IsValid
                        ? "ProductionApproved"
                        : "Incomplete";
            return new G4AudioSnapshot
            {
                assetStatus = status,
                productionStrictReady = strict.IsValid,
                memoryResultProvisional = !strict.IsValid,
                cueCount = cues.Length,
                playableCueCount = cues.Count(value => value.HasPlayableClip),
                clipVariantCount = cues.Sum(value => value.Clips.Count),
                pendingCueCount = pending,
                placeholderCueCount = placeholders,
                productionApprovedCueCount = approved,
                commissioningErrorCount = commissioning.Errors.Count,
                commissioningWarningCount = commissioning.Warnings.Count
            };
        }

        private G4PerformanceSummary BuildOverallSummary(
            IReadOnlyList<FrameSample> values)
        {
            return new G4PerformanceSummary
            {
                sampleCount = values.Count,
                measuredSeconds = values.Count == 0
                    ? 0f
                    : (float)Math.Max(
                        0d,
                        values[values.Count - 1].ElapsedSeconds -
                        values[0].ElapsedSeconds),
                frameTimeMs = BuildDistribution(
                    values.Select(value => value.FrameTimeMs)),
                mainThreadTimeMs = BuildDistribution(
                    values
                        .Where(value => value.MainThreadNanoseconds >= 0L)
                        .Select(value =>
                            value.MainThreadNanoseconds / 1000000d)),
                gcAllocatedBytesPerFrame = BuildDistribution(
                    values
                        .Where(value => value.GcAllocatedBytes >= 0L)
                        .Select(value => (double)value.GcAllocatedBytes)),
                peakTotalUsedMemoryBytes = MaximumAvailable(
                    values.Select(value => value.TotalUsedMemoryBytes)),
                finalTotalUsedMemoryBytes = LastAvailable(
                    values.Select(value => value.TotalUsedMemoryBytes)),
                peakGcUsedMemoryBytes = MaximumAvailable(
                    values.Select(value => value.GcUsedMemoryBytes)),
                finalGcUsedMemoryBytes = LastAvailable(
                    values.Select(value => value.GcUsedMemoryBytes)),
                peakTextureMemoryBytes = MaximumAvailable(
                    values.Select(value => value.TextureMemoryBytes)),
                finalTextureMemoryBytes = LastAvailable(
                    values.Select(value => value.TextureMemoryBytes)),
                peakAudioMemoryBytes = MaximumAvailable(
                    values.Select(value => value.AudioMemoryBytes)),
                finalAudioMemoryBytes = LastAvailable(
                    values.Select(value => value.AudioMemoryBytes))
            };
        }

        private G4ScenePerformanceSummary BuildSceneSummary(
            IGrouping<string, FrameSample> group)
        {
            var values = group.ToArray();
            return new G4ScenePerformanceSummary
            {
                sceneName = group.Key,
                sampleCount = values.Length,
                measuredSeconds = values.Length == 0
                    ? 0f
                    : (float)Math.Max(
                        0d,
                        values[values.Length - 1].ElapsedSeconds -
                        values[0].ElapsedSeconds),
                frameTimeMs = BuildDistribution(
                    values.Select(value => value.FrameTimeMs)),
                mainThreadTimeMs = BuildDistribution(
                    values
                        .Where(value => value.MainThreadNanoseconds >= 0L)
                        .Select(value =>
                            value.MainThreadNanoseconds / 1000000d)),
                peakTotalUsedMemoryBytes = MaximumAvailable(
                    values.Select(value => value.TotalUsedMemoryBytes)),
                peakGcUsedMemoryBytes = MaximumAvailable(
                    values.Select(value => value.GcUsedMemoryBytes)),
                peakTextureMemoryBytes = MaximumAvailable(
                    values.Select(value => value.TextureMemoryBytes)),
                peakAudioMemoryBytes = MaximumAvailable(
                    values.Select(value => value.AudioMemoryBytes))
            };
        }

        private static G4MetricDistribution BuildDistribution(
            IEnumerable<double> source)
        {
            var values = source
                .Where(value =>
                    !double.IsNaN(value) &&
                    !double.IsInfinity(value) &&
                    value >= 0d)
                .OrderBy(value => value)
                .ToArray();
            if (values.Length == 0)
            {
                return new G4MetricDistribution();
            }

            return new G4MetricDistribution
            {
                sampleCount = values.Length,
                minimum = values[0],
                average = values.Average(),
                p50 = Percentile(values, 0.50d),
                p95 = Percentile(values, 0.95d),
                p99 = Percentile(values, 0.99d),
                maximum = values[values.Length - 1]
            };
        }

        private static double Percentile(
            IReadOnlyList<double> sorted,
            double percentile)
        {
            if (sorted.Count == 0)
            {
                return 0d;
            }

            var position = (sorted.Count - 1) * percentile;
            var lower = (int)Math.Floor(position);
            var upper = (int)Math.Ceiling(position);
            if (lower == upper)
            {
                return sorted[lower];
            }

            var fraction = position - lower;
            return sorted[lower] +
                   (sorted[upper] - sorted[lower]) * fraction;
        }

        private static long MaximumAvailable(IEnumerable<long> source)
        {
            var values = source.Where(value => value >= 0L).ToArray();
            return values.Length == 0 ? -1L : values.Max();
        }

        private static long LastAvailable(IEnumerable<long> source)
        {
            var result = -1L;
            foreach (var value in source)
            {
                if (value >= 0L)
                {
                    result = value;
                }
            }

            return result;
        }

        private void RefreshTransientTargets()
        {
            trackedFxPools = Object.FindObjectsOfType<PresentationFxPool>();
            trackedBattleScreens =
                Object.FindObjectsOfType<BattleScreenView>();
            trackedAudioSources = Object.FindObjectsOfType<AudioSource>();
        }

        private TransientSnapshot CaptureTransientSnapshot()
        {
            var activeFx = 0;
            foreach (var pool in trackedFxPools)
            {
                if (pool != null)
                {
                    activeFx += pool.ActiveCount;
                }
            }

            var battleAnimation = false;
            foreach (var screen in trackedBattleScreens)
            {
                if (screen != null && screen.IsAnimationPlaying)
                {
                    battleAnimation = true;
                    break;
                }
            }

            var activeNonLoopingAudio = 0;
            foreach (var source in trackedAudioSources)
            {
                if (source != null && source.isPlaying && !source.loop)
                {
                    activeNonLoopingAudio++;
                }
            }

            return new TransientSnapshot
            {
                ActivePresentationFx = activeFx,
                ActiveNonLoopingAudioSources = activeNonLoopingAudio,
                BattleAnimationPlaying = battleAnimation
            };
        }

        private static ArtworkAudit CaptureArtworkAudit()
        {
            var raw = new List<G4ArtworkObservation>();
            foreach (var card in Object.FindObjectsOfType<CardView>())
            {
                if (card == null ||
                    !IsVisibleInHierarchy(card) ||
                    !card.HasCompleteBindings)
                {
                    continue;
                }

                AddBoundArtworkObservation(
                    raw,
                    "CardView",
                    card.LastArtId,
                    card.LastArtworkResolution,
                    null);
            }
            foreach (var standee in
                     Object.FindObjectsOfType<BattleStandeeView>())
            {
                if (standee == null ||
                    !IsVisibleInHierarchy(standee) ||
                    !standee.HasCompleteBindings ||
                    standee.Model == null)
                {
                    continue;
                }

                AddBoundArtworkObservation(
                    raw,
                    "BattleStandeeView",
                    standee.LastArtId,
                    standee.LastArtworkResolution,
                    standee.Model.ArtId);
            }
            foreach (var choice in
                     Object.FindObjectsOfType<RunChoiceOptionView>())
            {
                AddArtworkObservation(
                    raw,
                    "RunChoiceOptionView",
                    choice.LastArtId,
                    choice.LastArtworkResolution);
            }
            foreach (var relic in
                     Object.FindObjectsOfType<RunRelicEntryView>())
            {
                AddArtworkObservation(
                    raw,
                    "RunRelicEntryView",
                    relic.LastArtId,
                    relic.LastArtworkResolution);
            }

            var observations = raw
                .GroupBy(value =>
                    value.source + "\n" +
                    value.artId + "\n" +
                    value.resolution + "\n" +
                    value.sampleScope)
                .Select(group =>
                {
                    var first = group.First();
                    first.instanceCount = group.Count();
                    return first;
                })
                .OrderBy(value => value.source, StringComparer.Ordinal)
                .ThenBy(value => value.artId, StringComparer.Ordinal)
                .ToArray();
            var audit = new ArtworkAudit
            {
                Observations = observations
            };
            foreach (var observation in observations)
            {
                switch (observation.resolution)
                {
                    case nameof(ArtworkResolution.Exact):
                        audit.ExactCount += observation.instanceCount;
                        break;
                    case nameof(ArtworkResolution.Fallback):
                        audit.FallbackCount += observation.instanceCount;
                        break;
                    case nameof(ArtworkResolution.Diagnostic):
                        audit.DiagnosticCount += observation.instanceCount;
                        break;
                    default:
                        audit.MissingCount += observation.instanceCount;
                        break;
                }

                if (observation.sampleScope &&
                    !string.Equals(
                        observation.resolution,
                        nameof(ArtworkResolution.Exact),
                        StringComparison.Ordinal))
                {
                    audit.SampleScopeViolationCount +=
                        observation.instanceCount;
                }
            }

            return audit;
        }

        private static bool IsVisibleInHierarchy(Component component)
        {
            if (component == null || !component.gameObject.activeInHierarchy)
            {
                return false;
            }

            var current = component.transform;
            while (current != null)
            {
                var groups = current.GetComponents<CanvasGroup>();
                foreach (var group in groups)
                {
                    if (group != null && group.enabled && group.alpha <= 0f)
                    {
                        return false;
                    }
                }

                if (groups.Any(group =>
                        group != null &&
                        group.enabled &&
                        group.ignoreParentGroups))
                {
                    break;
                }

                current = current.parent;
            }

            return true;
        }

        private static void AddBoundArtworkObservation(
            ICollection<G4ArtworkObservation> destination,
            string source,
            string artId,
            ArtworkResolution resolution,
            string expectedArtId)
        {
            if (!string.IsNullOrWhiteSpace(artId))
            {
                AddArtworkObservation(
                    destination,
                    source,
                    artId,
                    resolution);
                return;
            }

            AddArtworkObservation(
                destination,
                source,
                string.IsNullOrWhiteSpace(expectedArtId)
                    ? MissingArtIdMarker
                    : expectedArtId,
                ArtworkResolution.Missing);
        }

        private static void AddArtworkObservation(
            ICollection<G4ArtworkObservation> destination,
            string source,
            string artId,
            ArtworkResolution resolution)
        {
            if (string.IsNullOrWhiteSpace(artId))
            {
                return;
            }

            destination.Add(new G4ArtworkObservation
            {
                source = source,
                artId = artId,
                resolution = resolution.ToString(),
                sampleScope = SampleScopeArtIds.Contains(artId),
                instanceCount = 1
            });
        }

        private static string BuildArtworkAuditDetails(ArtworkAudit audit)
        {
            var ids = audit.Observations.Length == 0
                ? "<none>"
                : string.Join(
                    ", ",
                    audit.Observations.Select(value =>
                        $"{value.artId}:{value.resolution}x" +
                        $"{value.instanceCount}" +
                        (value.sampleScope ? "[sample]" : string.Empty)));
            return $"art exact={audit.ExactCount}, " +
                   $"fallback={audit.FallbackCount}, " +
                   $"diagnostic={audit.DiagnosticCount}, " +
                   $"missing={audit.MissingCount}, " +
                   $"sampleViolations={audit.SampleScopeViolationCount}; " +
                   $"ids={ids}";
        }

        private G4ArtworkSummary BuildArtworkSummary()
        {
            var exact = checkpoints.Sum(value => value.artworkExactCount);
            var fallback = checkpoints.Sum(value => value.artworkFallbackCount);
            var diagnostic =
                checkpoints.Sum(value => value.artworkDiagnosticCount);
            var missing = checkpoints.Sum(value => value.artworkMissingCount);
            var sampleViolations = checkpoints
                .Where(value => !value.sampleScopeExact)
                .Sum(value => (value.artworkObservations ??
                               Array.Empty<G4ArtworkObservation>())
                    .Where(observation =>
                        observation.sampleScope &&
                        !string.Equals(
                            observation.resolution,
                            nameof(ArtworkResolution.Exact),
                            StringComparison.Ordinal))
                    .Sum(observation => observation.instanceCount));
            return new G4ArtworkSummary
            {
                checkpointObservationCount =
                    exact + fallback + diagnostic + missing,
                exactCount = exact,
                fallbackCount = fallback,
                diagnosticCount = diagnostic,
                missingCount = missing,
                sampleScopeViolationCount = sampleViolations,
                visibleSampleScopeExact =
                    sampleViolations == 0 && missing == 0,
                catalogExpectedCount = SampleScopeArtIds.Count,
                catalogExactCount = sampleCatalogExactCount,
                catalogMissingArtIds =
                    sampleCatalogMissingArtIds ?? Array.Empty<string>(),
                catalogExact =
                    sampleCatalogAudited &&
                    sampleCatalogExactCount == SampleScopeArtIds.Count &&
                    (sampleCatalogMissingArtIds?.Length ?? 0) == 0,
                sampleScopeExact =
                    sampleCatalogAudited &&
                    sampleCatalogExactCount == SampleScopeArtIds.Count &&
                    (sampleCatalogMissingArtIds?.Length ?? 0) == 0 &&
                    sampleViolations == 0 &&
                    missing == 0,
                interpretation =
                    "The full G2 sample catalog and every visible sample-scope " +
                    "instance must be Exact. Non-sample " +
                    "Fallback/Diagnostic entries are allowed while the " +
                    "4+4 polished fallback set remains deferred, but are " +
                    "reported explicitly and are not formal artwork. Any " +
                    "visible Missing observation blocks the gate."
            };
        }

        private void WriteSamplesCsv(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var writer = new StreamWriter(
                       path,
                       false,
                       new UTF8Encoding(false)))
            {
                writer.WriteLine(
                    "elapsed_seconds,scene,frame_ms,main_thread_ns," +
                    "gc_allocated_bytes,total_used_bytes,gc_used_bytes," +
                    "texture_bytes,audio_bytes,active_fx," +
                    "active_non_loop_audio,battle_animation");
                foreach (var sample in samples)
                {
                    writer.Write(sample.ElapsedSeconds.ToString(
                        "F6",
                        CultureInfo.InvariantCulture));
                    writer.Write(',');
                    writer.Write(EscapeCsv(sample.SceneName));
                    writer.Write(',');
                    writer.Write(sample.FrameTimeMs.ToString(
                        "F6",
                        CultureInfo.InvariantCulture));
                    writer.Write(',');
                    writer.Write(sample.MainThreadNanoseconds);
                    writer.Write(',');
                    writer.Write(sample.GcAllocatedBytes);
                    writer.Write(',');
                    writer.Write(sample.TotalUsedMemoryBytes);
                    writer.Write(',');
                    writer.Write(sample.GcUsedMemoryBytes);
                    writer.Write(',');
                    writer.Write(sample.TextureMemoryBytes);
                    writer.Write(',');
                    writer.Write(sample.AudioMemoryBytes);
                    writer.Write(',');
                    writer.Write(sample.ActivePresentationFx);
                    writer.Write(',');
                    writer.Write(sample.ActiveNonLoopingAudioSources);
                    writer.Write(',');
                    writer.WriteLine(
                        sample.BattleAnimationPlaying ? "1" : "0");
                }
            }
        }

        private static string EscapeCsv(string value)
        {
            var text = value ?? string.Empty;
            if (!text.Contains(",") &&
                !text.Contains("\"") &&
                !text.Contains("\r") &&
                !text.Contains("\n"))
            {
                return text;
            }

            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        private static void WriteJsonAtomic(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporaryPath = path + ".tmp";
            File.WriteAllText(
                temporaryPath,
                content ?? string.Empty,
                new UTF8Encoding(false));
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            File.Move(temporaryPath, path);
        }
    }
}

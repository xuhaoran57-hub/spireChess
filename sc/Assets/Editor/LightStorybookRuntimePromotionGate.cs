using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpireChess.Config;
using SpireChess.UI;
using SpireChess.Utils;
using UnityEditor;
using UnityEngine;

namespace SpireChess.Editor
{
    public static class LightStorybookRuntimePromotionGate
    {
        public const string ContractRelativePath =
            "phase-9c-v0.3.3-runtime-promotion-contract.json";

        private const string EvidenceRelativePath =
            "sc/Logs/Phase9C/RuntimePromotionGate/v0.3.3/" +
            "gate-result.json";

        private const string CalibrationPrefix =
            "Assets/Art/Presentation/Calibration/" +
            "LightStorybookProductionV033Batch";

        private static readonly string[] ProtectedAssetPaths =
        {
            "sc/Assets/Configs/Presentation/" +
            "PresentationSpriteCatalog.asset",
            "sc/Assets/Configs/Presentation/" +
            "PresentationSpriteCatalog.asset.meta",
            "sc/Assets/Configs/Presentation/" +
            "PresentationSpriteCatalog_LightStorybookFormalCatalogV032.asset",
            "sc/Assets/Configs/Presentation/" +
            "PresentationSpriteCatalog_LightStorybookFormalCatalogV032.asset.meta",
            "sc/Assets/Prefabs/UI/Common/PF_Card.prefab",
            "sc/Assets/Prefabs/UI/Common/PF_Card.prefab.meta",
            "sc/Assets/Prefabs/UI/Shop/PF_ShopScreen.prefab",
            "sc/Assets/Prefabs/UI/Shop/PF_ShopScreen.prefab.meta",
            "sc/Assets/Prefabs/UI/Battle/PF_BattleScreen.prefab",
            "sc/Assets/Prefabs/UI/Battle/PF_BattleScreen.prefab.meta"
        };

        [MenuItem(
            "Spire Chess/Release/Validate Phase 9C v0.3.3 " +
            "Runtime Promotion Gate")]
        public static void ValidateFromMenu()
        {
            var result = Evaluate();
            WriteEvidence(result);
            if (!result.Passed)
            {
                throw new InvalidOperationException(
                    "Phase 9C Runtime promotion gate is blocked:\n" +
                    string.Join("\n", result.Failures));
            }

            Debug.Log(
                "[LightStorybook] Phase 9C v0.3.3 Runtime promotion " +
                "gate passed.");
        }

        public static void ValidateFromCommandLine()
        {
            ValidateFromMenu();
        }

        public static RuntimePromotionGateResult Evaluate()
        {
            var technicalFailures = new List<string>();
            var repositoryRoot = ResolveRepositoryRoot();
            RuntimePromotionContract contract = null;

            RunCheck(technicalFailures, "RPG-CONTRACT", () =>
            {
                contract = LoadContract(repositoryRoot);
                ValidateContractIdentity(contract);
            });

            if (contract != null)
            {
                RunCheck(
                    technicalFailures,
                    "RPG-01/RPG-04",
                    () => ValidateCatalogs(repositoryRoot, contract));
                RunCheck(
                    technicalFailures,
                    "RPG-02",
                    () => ValidateProductionManifest(
                        repositoryRoot,
                        contract));
                RunCheck(
                    technicalFailures,
                    "RPG-03",
                    () => ValidateReleaseEvidence(
                        repositoryRoot,
                        contract));
                RunCheck(
                    technicalFailures,
                    "RPG-05",
                    () => ValidateTargetPolicy(contract.TargetPolicy));
            }

            var approvalFailures = ValidateApproval(contract?.Approval)
                .Select(value => "RPG-06: " + value)
                .ToArray();
            return new RuntimePromotionGateResult
            {
                Version = contract?.Version ?? "unknown",
                TechnicalPassed = technicalFailures.Count == 0,
                ApprovalPassed = approvalFailures.Length == 0,
                Failures = technicalFailures
                    .Concat(approvalFailures)
                    .ToArray()
            };
        }

        public static string[] ValidateApproval(
            RuntimePromotionApproval approval)
        {
            var failures = new List<string>();
            if (approval == null)
            {
                failures.Add("approval record is missing.");
                return failures.ToArray();
            }

            if (approval.Status != "Approved")
            {
                failures.Add("approval.status must be Approved.");
            }
            if (string.IsNullOrWhiteSpace(approval.ApprovedBy))
            {
                failures.Add("approval.approvedBy is required.");
            }
            if (!DateTimeOffset.TryParse(
                    approval.ApprovedAt,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out _))
            {
                failures.Add(
                    "approval.approvedAt must be an ISO-8601 timestamp.");
            }
            if (string.IsNullOrWhiteSpace(approval.AccountAgreement))
            {
                failures.Add("approval.accountAgreement is required.");
            }
            if (!approval.InputRightsConfirmed)
            {
                failures.Add("input rights confirmation is required.");
            }
            if (!approval.AiDisclosureAccepted)
            {
                failures.Add("AI disclosure acceptance is required.");
            }
            if (!approval.VisualReviewAccepted)
            {
                failures.Add("visual review acceptance is required.");
            }
            if (!approval.RuntimePromotionAccepted)
            {
                failures.Add("Runtime promotion acceptance is required.");
            }
            return failures.ToArray();
        }

        public static void ValidateTargetPolicy(
            RuntimePromotionTargetPolicy policy)
        {
            if (policy == null)
            {
                throw new InvalidOperationException(
                    "Target Runtime policy is missing.");
            }

            if (policy.RuntimeArtRoot !=
                    "Assets/Art/Presentation/Runtime/" +
                    "LightStorybookV033" ||
                policy.StandaloneTextureFormat != "DXT1" ||
                policy.MaxTextureSize != 1024 ||
                policy.CompressionQuality != 50 ||
                policy.Mipmaps ||
                policy.Readable ||
                !policy.PreserveRuntimeCatalogGuid ||
                !policy.ForbidCalibrationReferences ||
                !policy.RequireCleanBuild ||
                !policy.RequireFullRegression ||
                !policy.RequireG4VisualReview ||
                !policy.RequireMemoryEvidence)
            {
                throw new InvalidOperationException(
                    "Target Runtime policy does not match the frozen " +
                    "v0.3.3 promotion policy.");
            }
        }

        private static RuntimePromotionContract LoadContract(
            string repositoryRoot)
        {
            var path = ResolvePath(repositoryRoot, ContractRelativePath);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "Runtime promotion contract is missing.",
                    path);
            }

            var contract =
                JsonConvert.DeserializeObject<RuntimePromotionContract>(
                    File.ReadAllText(path));
            if (contract == null)
            {
                throw new InvalidOperationException(
                    "Runtime promotion contract could not be parsed.");
            }
            return contract;
        }

        private static void ValidateContractIdentity(
            RuntimePromotionContract contract)
        {
            if (contract.Version != "0.3.3" ||
                contract.GateVersion != "1" ||
                contract.Candidate == null ||
                contract.RuntimeBeforePromotion == null ||
                contract.Candidate.CatalogPath !=
                    LightStorybookProductionBatch6Builder.CatalogPath ||
                contract.Candidate.CatalogGuid !=
                    "1600000000000000000000000000000a" ||
                contract.Candidate.CatalogEntryCount != 86 ||
                contract.Candidate.ConfiguredArtworkCount != 83 ||
                contract.Candidate.ProductionArtworkCount != 51 ||
                contract.Candidate.UnityVersion != "2022.3.62f3c1" ||
                contract.Candidate.EditModePassed != 373 ||
                contract.Candidate.PlayModePassed != 30 ||
                contract.Candidate.ScreenshotCount != 42 ||
                contract.RuntimeBeforePromotion.CatalogPath !=
                    "Assets/Configs/Presentation/" +
                    "PresentationSpriteCatalog.asset" ||
                contract.RuntimeBeforePromotion.CatalogGuid !=
                    "75d638606a8084146524a35a317a2cca" ||
                contract.RuntimeBeforePromotion.CatalogEntryCount != 24 ||
                contract.RuntimeBeforePromotion.ProductionArtworkCount != 0)
            {
                throw new InvalidOperationException(
                    "Runtime promotion contract identity is invalid.");
            }
        }

        private static void ValidateCatalogs(
            string repositoryRoot,
            RuntimePromotionContract contract)
        {
            var candidateSpec = contract.Candidate;
            var runtimeSpec = contract.RuntimeBeforePromotion;
            var candidate =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    candidateSpec.CatalogPath);
            var runtime =
                AssetDatabase.LoadAssetAtPath<PresentationSpriteCatalog>(
                    runtimeSpec.CatalogPath);
            if (candidate == null || runtime == null)
            {
                throw new InvalidOperationException(
                    "Candidate or Runtime catalog is missing.");
            }
            if (AssetDatabase.AssetPathToGUID(candidateSpec.CatalogPath) !=
                    candidateSpec.CatalogGuid ||
                AssetDatabase.AssetPathToGUID(runtimeSpec.CatalogPath) !=
                    runtimeSpec.CatalogGuid)
            {
                throw new InvalidOperationException(
                    "Candidate or Runtime catalog GUID drifted.");
            }

            var candidateIds = ReadCatalogIds(candidate);
            var runtimeIds = ReadCatalogIds(runtime);
            if (candidateIds.Length != candidateSpec.CatalogEntryCount ||
                candidateIds.Distinct(StringComparer.Ordinal).Count() !=
                    candidateIds.Length ||
                runtimeIds.Length != runtimeSpec.CatalogEntryCount ||
                runtimeIds.Distinct(StringComparer.Ordinal).Count() !=
                    runtimeIds.Length)
            {
                throw new InvalidOperationException(
                    "Candidate or Runtime catalog entry count/identity " +
                    "drifted.");
            }

            var configs = new ConfigService(new NewtonsoftJsonSerializer());
            configs.LoadFromResources().ThrowIfInvalid();
            var configuredArtIds = configs.MinionsById.Values
                .Select(value => value.ArtId)
                .Concat(configs.SpellsById.Values.Select(value => value.ArtId))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (configuredArtIds.Length !=
                candidateSpec.ConfiguredArtworkCount)
            {
                throw new InvalidOperationException(
                    "Configured artwork count drifted.");
            }
            foreach (var artId in configuredArtIds)
            {
                if (!candidate.TryGetArtwork(artId, out var sprite) ||
                    sprite == null)
                {
                    throw new InvalidOperationException(
                        "Candidate is missing configured artwork: " + artId);
                }
            }

            var manifest = LoadJson(
                repositoryRoot,
                candidateSpec.ProductionManifestPath);
            var items = RequireArray(manifest, "items");
            if (items.Count != candidateSpec.ProductionArtworkCount)
            {
                throw new InvalidOperationException(
                    "Production artwork count drifted.");
            }
            foreach (var item in items.OfType<JObject>())
            {
                var artId = RequireString(item, "artId");
                var expectedHash = RequireString(item, "sha256");
                if (!candidate.TryGetArtwork(artId, out var sprite) ||
                    sprite == null)
                {
                    throw new InvalidOperationException(
                        "Candidate is missing production artwork: " + artId);
                }
                var assetPath = AssetDatabase.GetAssetPath(sprite);
                if (!assetPath.StartsWith(
                        CalibrationPrefix,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Production candidate escaped Calibration: " +
                        artId);
                }
                var absoluteAssetPath = ResolvePath(
                    Directory.GetParent(Application.dataPath).FullName,
                    assetPath);
                if (!HashesMatch(absoluteAssetPath, expectedHash))
                {
                    throw new InvalidOperationException(
                        "Calibration copy hash drifted: " + artId);
                }
                if (runtime.TryGetArtwork(artId, out _))
                {
                    throw new InvalidOperationException(
                        "Production artwork entered Runtime before " +
                        "promotion approval: " + artId);
                }
            }
            if (runtimeSpec.ProductionArtworkCount != 0)
            {
                throw new InvalidOperationException(
                    "Pre-promotion Runtime production count must be zero.");
            }
        }

        private static void ValidateProductionManifest(
            string repositoryRoot,
            RuntimePromotionContract contract)
        {
            var manifest = LoadJson(
                repositoryRoot,
                contract.Candidate.ProductionManifestPath);
            if ((string)manifest["version"] != contract.Version ||
                !((string)manifest["runtimePolicy"] ?? string.Empty)
                    .Contains("explicitly approved"))
            {
                throw new InvalidOperationException(
                    "Production manifest version or Runtime policy drifted.");
            }

            var counts = manifest["counts"] as JObject;
            if ((int?)counts?["generated"] != 51 ||
                (int?)counts?["pending"] != 0)
            {
                throw new InvalidOperationException(
                    "Production manifest completion count drifted.");
            }

            var items = RequireArray(manifest, "items")
                .OfType<JObject>()
                .ToArray();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var artIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in items)
            {
                var id = RequireString(item, "id");
                var artId = RequireString(item, "artId");
                var artFile = RequireString(item, "artFile");
                var sha256 = RequireString(item, "sha256");
                if ((string)item["status"] != "generated" ||
                    !ids.Add(id) ||
                    !artIds.Add(artId) ||
                    !HashesMatch(
                        ResolvePath(repositoryRoot, artFile),
                        sha256))
                {
                    throw new InvalidOperationException(
                        "Production item identity/hash drifted: " + id);
                }
            }

            var sources = manifest["sources"] as JObject;
            if (sources == null || !sources.Properties().Any())
            {
                throw new InvalidOperationException(
                    "Production manifest sources are missing.");
            }
            foreach (var property in sources.Properties())
            {
                var source = property.Value as JObject;
                var path = RequireString(source, "path");
                var sha256 = RequireString(source, "sha256");
                if (!HashesMatch(
                        ResolvePath(repositoryRoot, path),
                        sha256))
                {
                    throw new InvalidOperationException(
                        "Production source hash drifted: " + property.Name);
                }
            }
        }

        private static void ValidateReleaseEvidence(
            string repositoryRoot,
            RuntimePromotionContract contract)
        {
            var candidate = contract.Candidate;
            var release = LoadJson(
                repositoryRoot,
                candidate.ReleaseManifestPath);
            if ((string)release["version"] != contract.Version ||
                (string)release["status"] != "UNITY_AUTOMATION_PASS" ||
                (string)release["unityVersion"] != candidate.UnityVersion ||
                (string)release["catalogCandidate"] !=
                    Path.GetFileName(candidate.CatalogPath) ||
                (int?)release["catalogEntryCount"] !=
                    candidate.CatalogEntryCount ||
                (int?)release["configuredArtworkCount"] !=
                    candidate.ConfiguredArtworkCount ||
                (int?)release["productionArtworkCount"] !=
                    candidate.ProductionArtworkCount ||
                (int?)release["screenshotCount"] !=
                    candidate.ScreenshotCount)
            {
                throw new InvalidOperationException(
                    "Unity release evidence identity/counts drifted.");
            }

            var candidateIdentity = RequireArray(
                    release,
                    "catalogIdentities")
                .OfType<JObject>()
                .SingleOrDefault(value =>
                    (string)value["path"] ==
                    "sc/" + candidate.CatalogPath);
            if (candidateIdentity == null ||
                (string)candidateIdentity["expectedGuid"] !=
                    candidate.CatalogGuid ||
                (string)candidateIdentity["guidBefore"] !=
                    candidate.CatalogGuid ||
                (string)candidateIdentity["guidAfter"] !=
                    candidate.CatalogGuid ||
                (bool?)candidateIdentity["unchanged"] != true)
            {
                throw new InvalidOperationException(
                    "Unity release candidate GUID evidence drifted.");
            }

            ValidateTestEvidence(
                RequireArray(release, "tests"),
                "EditMode",
                candidate.EditModePassed);
            ValidateTestEvidence(
                RequireArray(release, "tests"),
                "PlayMode",
                candidate.PlayModePassed);

            ValidateProtectedAssets(
                RequireArray(release, "protectedAssets"));

            var capturePath = ResolvePath(
                repositoryRoot,
                candidate.CaptureIndexPath);
            if (!HashesMatch(
                    capturePath,
                    candidate.CaptureIndexSha256))
            {
                throw new InvalidOperationException(
                    "Capture index hash drifted.");
            }
            var capture = JObject.Parse(File.ReadAllText(capturePath));
            if ((string)capture["version"] != contract.Version ||
                (string)capture["status"] != "UNITY_CAPTURE_COMPLETE" ||
                (string)capture["unityVersion"] !=
                    candidate.UnityVersion ||
                (string)capture["catalogPath"] !=
                    candidate.CatalogPath ||
                (string)capture["catalogGuid"] !=
                    candidate.CatalogGuid ||
                (string)capture["runtimeCatalogPath"] !=
                    contract.RuntimeBeforePromotion.CatalogPath ||
                (string)capture["runtimeCatalogGuid"] !=
                    contract.RuntimeBeforePromotion.CatalogGuid ||
                (int?)capture["catalogEntryCount"] !=
                    candidate.CatalogEntryCount ||
                (int?)capture["configuredArtworkCount"] !=
                    candidate.ConfiguredArtworkCount ||
                (int?)capture["productionArtworkCount"] !=
                    candidate.ProductionArtworkCount ||
                (int?)capture["screenshotCount"] !=
                    candidate.ScreenshotCount)
            {
                throw new InvalidOperationException(
                    "Capture index identity/counts drifted.");
            }

            ValidateReleaseEvidenceFiles(
                Path.GetDirectoryName(capturePath),
                RequireArray(release, "evidenceFiles"),
                RequireArray(capture, "screenshots"),
                candidate.ScreenshotCount);
        }

        private static void ValidateProtectedAssets(
            JArray protectedAssets)
        {
            if (protectedAssets.Count != ProtectedAssetPaths.Length)
            {
                throw new InvalidOperationException(
                    "Release protected-asset count drifted.");
            }

            var expected = new HashSet<string>(
                ProtectedAssetPaths,
                StringComparer.Ordinal);
            var actual = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in protectedAssets.OfType<JObject>())
            {
                var path = RequireString(value, "path");
                var before = RequireString(value, "sha256Before");
                var after = RequireString(value, "sha256After");
                if ((bool?)value["unchanged"] != true ||
                    !string.Equals(
                        before,
                        after,
                        StringComparison.OrdinalIgnoreCase) ||
                    !actual.Add(path) ||
                    !expected.Contains(path))
                {
                    throw new InvalidOperationException(
                        "Protected asset drifted: " + path);
                }
            }
            if (!actual.SetEquals(expected))
            {
                throw new InvalidOperationException(
                    "Release protected-asset set drifted.");
            }
        }

        private static void ValidateReleaseEvidenceFiles(
            string evidenceRoot,
            JArray evidenceFiles,
            JArray screenshots,
            int expectedScreenshotCount)
        {
            const int expectedEvidenceFileCount = 48;
            const string nonArchivedCaptureLog =
                "logs/phase9c-capture.log";
            const string nonArchivedCaptureLogSha256 =
                "07918fadccbc42a3b3749439548a0359a804d2093cc0f9927b40a04e24846697";
            const long nonArchivedCaptureLogBytes = 91920;
            if (evidenceFiles.Count != expectedEvidenceFileCount ||
                screenshots.Count != expectedScreenshotCount)
            {
                throw new InvalidOperationException(
                    "Release evidence file count drifted.");
            }

            var evidenceByPath =
                new Dictionary<string, EvidenceFileSnapshot>(
                    StringComparer.Ordinal);
            foreach (var value in evidenceFiles.OfType<JObject>())
            {
                var relativePath = RequireString(value, "path");
                var sha256 = RequireString(value, "sha256");
                var bytes = (long?)value["bytes"];
                var absolutePath = ResolveEvidencePath(
                    evidenceRoot,
                    relativePath);
                if (!bytes.HasValue ||
                    bytes.Value <= 0 ||
                    evidenceByPath.ContainsKey(relativePath))
                {
                    throw new InvalidOperationException(
                        "Release evidence file drifted: " +
                        relativePath);
                }
                var isNonArchivedCaptureLog =
                    relativePath == nonArchivedCaptureLog;
                if (isNonArchivedCaptureLog)
                {
                    if (bytes.Value != nonArchivedCaptureLogBytes ||
                        !string.Equals(
                            sha256,
                            nonArchivedCaptureLogSha256,
                            StringComparison.OrdinalIgnoreCase) ||
                        (File.Exists(absolutePath) &&
                         (new FileInfo(absolutePath).Length !=
                              bytes.Value ||
                          !HashesMatch(absolutePath, sha256))))
                    {
                        throw new InvalidOperationException(
                            "Non-archived capture log identity drifted.");
                    }
                }
                else if (!File.Exists(absolutePath) ||
                         new FileInfo(absolutePath).Length != bytes.Value ||
                         !HashesMatch(absolutePath, sha256))
                {
                    throw new InvalidOperationException(
                        "Archived release evidence file drifted: " +
                        relativePath);
                }
                evidenceByPath.Add(
                    relativePath,
                    new EvidenceFileSnapshot
                    {
                        Sha256 = sha256,
                        Bytes = bytes.Value
                    });
            }
            if (evidenceByPath.Count != expectedEvidenceFileCount)
            {
                throw new InvalidOperationException(
                    "Release evidence file set is incomplete.");
            }
            if (!evidenceByPath.ContainsKey(nonArchivedCaptureLog) ||
                evidenceByPath.Keys.Count(value =>
                    value.StartsWith(
                        "logs/",
                        StringComparison.Ordinal)) != 1)
            {
                throw new InvalidOperationException(
                    "Non-archived capture log record drifted.");
            }

            var screenshotPaths =
                new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in screenshots.OfType<JObject>())
            {
                var relativePath = RequireString(value, "path");
                var sha256 = RequireString(value, "sha256");
                var bytes = (long?)value["bytes"];
                if (!relativePath.StartsWith(
                        "screenshots/",
                        StringComparison.Ordinal) ||
                    !bytes.HasValue ||
                    !screenshotPaths.Add(relativePath) ||
                    !evidenceByPath.TryGetValue(
                        relativePath,
                        out var evidence) ||
                    evidence.Bytes != bytes.Value ||
                    !string.Equals(
                        evidence.Sha256,
                        sha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Capture screenshot evidence drifted: " +
                        relativePath);
                }
            }
            if (screenshotPaths.Count != expectedScreenshotCount ||
                evidenceByPath.Keys.Count(value => value.StartsWith(
                    "screenshots/",
                    StringComparison.Ordinal)) !=
                    expectedScreenshotCount)
            {
                throw new InvalidOperationException(
                    "Capture screenshot set is incomplete.");
            }
        }

        private static string ResolveEvidencePath(
            string evidenceRoot,
            string relativePath)
        {
            if (Path.IsPathRooted(relativePath))
            {
                throw new InvalidOperationException(
                    "Evidence path must be relative: " + relativePath);
            }
            var normalizedRoot = Path.GetFullPath(evidenceRoot)
                .TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            var resolved = ResolvePath(evidenceRoot, relativePath);
            if (!resolved.StartsWith(
                    normalizedRoot,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Evidence path escapes its root: " + relativePath);
            }
            return resolved;
        }

        private static void ValidateTestEvidence(
            JArray tests,
            string platform,
            int expectedPassed)
        {
            var test = tests
                .OfType<JObject>()
                .SingleOrDefault(value =>
                    (string)value["platform"] == platform);
            if (test == null ||
                (string)test["result"] != "Passed" ||
                (int?)test["total"] != expectedPassed ||
                (int?)test["passed"] != expectedPassed ||
                (int?)test["failed"] != 0 ||
                (int?)test["skipped"] != 0 ||
                (int?)test["inconclusive"] != 0)
            {
                throw new InvalidOperationException(
                    platform + " release evidence is not fully passing.");
            }
        }

        private static string[] ReadCatalogIds(
            PresentationSpriteCatalog catalog)
        {
            var serialized = new SerializedObject(catalog);
            var artworks = serialized.FindProperty("artworks");
            if (artworks == null)
            {
                throw new InvalidOperationException(
                    "PresentationSpriteCatalog.artworks is unavailable.");
            }

            var ids = new string[artworks.arraySize];
            for (var index = 0; index < artworks.arraySize; index++)
            {
                var entry = artworks.GetArrayElementAtIndex(index);
                var id = entry.FindPropertyRelative("id").stringValue;
                var sprite = entry.FindPropertyRelative("sprite")
                    .objectReferenceValue as Sprite;
                if (string.IsNullOrWhiteSpace(id) || sprite == null)
                {
                    throw new InvalidOperationException(
                        "Catalog contains an incomplete artwork entry.");
                }
                ids[index] = id;
            }
            return ids;
        }

        private static JObject LoadJson(
            string repositoryRoot,
            string relativePath)
        {
            var path = ResolvePath(repositoryRoot, relativePath);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "Gate input is missing.",
                    path);
            }
            return JObject.Parse(File.ReadAllText(path));
        }

        private static JArray RequireArray(
            JObject value,
            string propertyName)
        {
            var array = value[propertyName] as JArray;
            if (array == null)
            {
                throw new InvalidOperationException(
                    propertyName + " must be an array.");
            }
            return array;
        }

        private static string RequireString(
            JObject value,
            string propertyName)
        {
            var result = (string)value?[propertyName];
            if (string.IsNullOrWhiteSpace(result))
            {
                throw new InvalidOperationException(
                    propertyName + " is required.");
            }
            return result;
        }

        private static void RunCheck(
            ICollection<string> failures,
            string id,
            Action check)
        {
            try
            {
                check();
            }
            catch (Exception exception)
            {
                failures.Add(id + ": " + exception.Message);
            }
        }

        private static bool HashesMatch(
            string path,
            string expectedSha256)
        {
            return File.Exists(path) &&
                   string.Equals(
                       ComputeSha256(path),
                       expectedSha256,
                       StringComparison.OrdinalIgnoreCase);
        }

        private static string ComputeSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var algorithm = SHA256.Create())
            {
                return string.Concat(
                    algorithm.ComputeHash(stream)
                        .Select(value => value.ToString("x2")));
            }
        }

        private static string ResolvePath(
            string root,
            string relativePath)
        {
            return Path.GetFullPath(Path.Combine(
                root,
                relativePath.Replace(
                    '/',
                    Path.DirectorySeparatorChar)));
        }

        private static string ResolveRepositoryRoot()
        {
            var projectRoot =
                Directory.GetParent(Application.dataPath).FullName;
            return Directory.GetParent(projectRoot).FullName;
        }

        private static void WriteEvidence(
            RuntimePromotionGateResult result)
        {
            var repositoryRoot = ResolveRepositoryRoot();
            var path = ResolvePath(repositoryRoot, EvidenceRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var evidence = new
            {
                version = result.Version,
                status = result.Passed ? "PASS" : "BLOCKED",
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                technicalPassed = result.TechnicalPassed,
                approvalPassed = result.ApprovalPassed,
                failures = result.Failures
            };
            File.WriteAllText(
                path,
                JsonConvert.SerializeObject(
                    evidence,
                    Formatting.Indented) + Environment.NewLine,
                new UTF8Encoding(false));
        }

        private sealed class EvidenceFileSnapshot
        {
            public string Sha256 { get; set; }
            public long Bytes { get; set; }
        }
    }

    public sealed class RuntimePromotionGateResult
    {
        public string Version { get; set; }
        public bool TechnicalPassed { get; set; }
        public bool ApprovalPassed { get; set; }
        public string[] Failures { get; set; } = Array.Empty<string>();
        public bool Passed => TechnicalPassed && ApprovalPassed;
    }

    [Serializable]
    public sealed class RuntimePromotionContract
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("gateVersion")]
        public string GateVersion { get; set; }

        [JsonProperty("candidate")]
        public RuntimePromotionCandidate Candidate { get; set; }

        [JsonProperty("runtimeBeforePromotion")]
        public RuntimeBeforePromotion RuntimeBeforePromotion { get; set; }

        [JsonProperty("targetPolicy")]
        public RuntimePromotionTargetPolicy TargetPolicy { get; set; }

        [JsonProperty("approval")]
        public RuntimePromotionApproval Approval { get; set; }
    }

    [Serializable]
    public sealed class RuntimePromotionCandidate
    {
        [JsonProperty("catalogPath")]
        public string CatalogPath { get; set; }

        [JsonProperty("catalogGuid")]
        public string CatalogGuid { get; set; }

        [JsonProperty("catalogEntryCount")]
        public int CatalogEntryCount { get; set; }

        [JsonProperty("configuredArtworkCount")]
        public int ConfiguredArtworkCount { get; set; }

        [JsonProperty("productionArtworkCount")]
        public int ProductionArtworkCount { get; set; }

        [JsonProperty("productionManifestPath")]
        public string ProductionManifestPath { get; set; }

        [JsonProperty("releaseManifestPath")]
        public string ReleaseManifestPath { get; set; }

        [JsonProperty("captureIndexPath")]
        public string CaptureIndexPath { get; set; }

        [JsonProperty("captureIndexSha256")]
        public string CaptureIndexSha256 { get; set; }

        [JsonProperty("unityVersion")]
        public string UnityVersion { get; set; }

        [JsonProperty("editModePassed")]
        public int EditModePassed { get; set; }

        [JsonProperty("playModePassed")]
        public int PlayModePassed { get; set; }

        [JsonProperty("screenshotCount")]
        public int ScreenshotCount { get; set; }
    }

    [Serializable]
    public sealed class RuntimeBeforePromotion
    {
        [JsonProperty("catalogPath")]
        public string CatalogPath { get; set; }

        [JsonProperty("catalogGuid")]
        public string CatalogGuid { get; set; }

        [JsonProperty("catalogEntryCount")]
        public int CatalogEntryCount { get; set; }

        [JsonProperty("productionArtworkCount")]
        public int ProductionArtworkCount { get; set; }
    }

    [Serializable]
    public sealed class RuntimePromotionTargetPolicy
    {
        [JsonProperty("runtimeArtRoot")]
        public string RuntimeArtRoot { get; set; }

        [JsonProperty("standaloneTextureFormat")]
        public string StandaloneTextureFormat { get; set; }

        [JsonProperty("maxTextureSize")]
        public int MaxTextureSize { get; set; }

        [JsonProperty("compressionQuality")]
        public int CompressionQuality { get; set; }

        [JsonProperty("mipmaps")]
        public bool Mipmaps { get; set; }

        [JsonProperty("readable")]
        public bool Readable { get; set; }

        [JsonProperty("preserveRuntimeCatalogGuid")]
        public bool PreserveRuntimeCatalogGuid { get; set; }

        [JsonProperty("forbidCalibrationReferences")]
        public bool ForbidCalibrationReferences { get; set; }

        [JsonProperty("requireCleanBuild")]
        public bool RequireCleanBuild { get; set; }

        [JsonProperty("requireFullRegression")]
        public bool RequireFullRegression { get; set; }

        [JsonProperty("requireG4VisualReview")]
        public bool RequireG4VisualReview { get; set; }

        [JsonProperty("requireMemoryEvidence")]
        public bool RequireMemoryEvidence { get; set; }
    }

    [Serializable]
    public sealed class RuntimePromotionApproval
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("approvedBy")]
        public string ApprovedBy { get; set; }

        [JsonProperty("approvedAt")]
        public string ApprovedAt { get; set; }

        [JsonProperty("accountAgreement")]
        public string AccountAgreement { get; set; }

        [JsonProperty("inputRightsConfirmed")]
        public bool InputRightsConfirmed { get; set; }

        [JsonProperty("aiDisclosureAccepted")]
        public bool AiDisclosureAccepted { get; set; }

        [JsonProperty("visualReviewAccepted")]
        public bool VisualReviewAccepted { get; set; }

        [JsonProperty("runtimePromotionAccepted")]
        public bool RuntimePromotionAccepted { get; set; }
    }
}

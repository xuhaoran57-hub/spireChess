using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpireChess.Run
{
    public sealed class BalanceRunMetadata
    {
        public string TuningRound { get; set; }
        public string CandidateId { get; set; }
        public string ConfigHash { get; set; }
        public string GitCommit { get; set; }
        public string UnityVersion { get; set; }
        public string Tester { get; set; }
        public string IntendedBuildId { get; set; }
        public string FailureReason { get; set; }
        public string BoringMoment { get; set; }
        public string UnfairMoment { get; set; }
        public string DecisionSummaryPath { get; set; }
        public string RawTelemetryPath { get; set; }
    }

    public sealed class BalanceRunSummary
    {
        public string BalanceSchemaVersion { get; set; } = "0.3.0";
        public string TuningRound { get; set; }
        public string CandidateId { get; set; }
        public string ContentVersion { get; set; }
        public string ConfigHash { get; set; }
        public string GitCommit { get; set; }
        public string UnityVersion { get; set; }
        public string CoreClassifierVersion { get; set; } = CoreBuildClassifier.Version;
        public int Seed { get; set; }
        public string Tester { get; set; }
        public string IntendedBuildId { get; set; }
        public string FinalBuildId { get; set; }
        public string FinalBoardJson { get; set; }
        public int FinalPermanentAttack { get; set; }
        public int FinalPermanentHealth { get; set; }
        public bool TurnTenReached { get; set; }
        public string TurnTenBoardJson { get; set; }
        public int TurnTenPermanentAttack { get; set; }
        public int TurnTenPermanentHealth { get; set; }
        public string TurnTenCoreInstanceIds { get; set; }
        public string TurnTenBuildId { get; set; }
        public int TurnTenRefreshesPaid { get; set; }
        public int TurnTenRefreshesFree { get; set; }
        public int TurnTenMinionsBought { get; set; }
        public int TurnTenMinionsSold { get; set; }
        public int TurnTenSpellsUsed { get; set; }
        public int TurnTenTavernUpgrades { get; set; }
        public int TurnTenGoldWasted { get; set; }
        public int TurnTenTriplesFormed { get; set; }
        public string Result { get; set; }
        public int FloorReached { get; set; }
        public int RunTurn { get; set; }
        public double ElapsedMinutes { get; set; }
        public int HealthRemaining { get; set; }
        public int BattlesWon { get; set; }
        public int BattlesNotWon { get; set; }
        public int ElitesAttempted { get; set; }
        public int ElitesDefeated { get; set; }
        public int BossAttempts { get; set; }
        public int BossesDefeated { get; set; }
        public int RefreshesPaid { get; set; }
        public int RefreshesFree { get; set; }
        public int MinionsBought { get; set; }
        public int MinionsSold { get; set; }
        public int SpellsUsed { get; set; }
        public int TavernUpgrades { get; set; }
        public int GoldWasted { get; set; }
        public int? FirstCoreTurn { get; set; }
        public int? SecondCoreTurn { get; set; }
        public int TriplesFormed { get; set; }
        public int TargetedDiscoversUsed { get; set; }
        public bool DualCoreBeforeFloorThreeBoss { get; set; }
        public string RouteNodeIds { get; set; }
        public bool EliteRouteChosen { get; set; }
        public string CoreSurvivorsByBattleJson { get; set; }
        public string PermanentDeltasByInstanceJson { get; set; }
        public string EventChoicesJson { get; set; }
        public string RewardChoicesJson { get; set; }
        public string FailureReason { get; set; }
        public string BoringMoment { get; set; }
        public string UnfairMoment { get; set; }
        public string DecisionSummaryPath { get; set; }
        public string RawTelemetryPath { get; set; }
    }

    public sealed class BalanceCardFunnelRow
    {
        public string BalanceSchemaVersion { get; set; } = "0.2.0";
        public string CandidateId { get; set; }
        public string TuningRound { get; set; }
        public int Tier { get; set; }
        public string CardId { get; set; }
        public int Offered { get; set; }
        public int BoughtOrPicked { get; set; }
        public int Played { get; set; }
        public int Sold { get; set; }
        public int SurvivedToRunEnd { get; set; }
        public int TripleMaterials { get; set; }
        public int TriplesCompleted { get; set; }
        public int DiscoverOffered { get; set; }
        public int DiscoverPicked { get; set; }
        public double OfferToPickRate => Offered <= 0 ? 0d : (double)BoughtOrPicked / Offered;
        public double SmoothedOfferToPickRate => (BoughtOrPicked + 1d) / (Offered + 2d);
        public double PickToPlayRate => BoughtOrPicked <= 0 ? 0d : (double)Played / BoughtOrPicked;
    }

    public sealed class RunTelemetryAggregator
    {
        private readonly Func<string, int> resolveTier;

        public RunTelemetryAggregator(Func<string, int> resolveTier = null)
        {
            this.resolveTier = resolveTier ?? (_ => 0);
        }

        public BalanceRunSummary AggregateRun(
            string ndjson,
            string candidateId,
            string tuningRound,
            string rawTelemetryPath = null)
        {
            return AggregateRun(ndjson, new BalanceRunMetadata
            {
                CandidateId = candidateId,
                TuningRound = tuningRound,
                RawTelemetryPath = rawTelemetryPath
            });
        }

        public BalanceRunSummary AggregateRun(
            string ndjson,
            BalanceRunMetadata metadata)
        {
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));
            var entries = Parse(ndjson);
            if (entries.Count == 0)
            {
                throw new InvalidOperationException("Telemetry contains no valid entries.");
            }

            var first = entries[0];
            var ended = entries.LastOrDefault(value => EventType(value) == "RunEnded");
            if (ended == null)
            {
                throw new InvalidOperationException("Telemetry does not contain RunEnded.");
            }

            var payload = (JObject)ended["payload"];
            var turnTen = entries.LastOrDefault(value => EventType(value) == "Turn10Snapshot");
            var finalBoard = payload["finalBoard"] as JArray ?? new JArray();
            var turnTenBoard = turnTen?["payload"]?["battle"] as JArray;
            var routes = entries.Where(value => EventType(value) == "NodeEntered")
                .Select(value => Value<string>(value, "payload", "nodeId"))
                .Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            var eventChoices = entries.Where(value => EventType(value) == "EventChoiceResolved")
                .Select(value => value["payload"]).ToArray();
            var rewardChoices = entries.Where(value => EventType(value) == "RewardChoiceResolved")
                .Select(value => value["payload"]).ToArray();
            var battles = entries.Where(value => EventType(value) == "BattleCompleted")
                .Select(value => value["payload"] as JObject)
                .Where(value => value != null).ToArray();
            var floorThreeBossTurn = entries
                .Where(value => EventType(value) == "NodeEntered" &&
                    Value<string>(value, "payload", "nodeType") == "Boss" &&
                    Value<int>(value, "payload", "floor") == 3)
                .Select(value => Value<int>(value, "payload", "runTurn"))
                .DefaultIfEmpty(int.MaxValue).Min();
            var secondCoreTurn = payload.Value<int?>("SecondCoreTurn");

            return new BalanceRunSummary
            {
                CandidateId = metadata.CandidateId,
                TuningRound = metadata.TuningRound,
                ContentVersion = first.Value<string>("contentVersion"),
                ConfigHash = metadata.ConfigHash,
                GitCommit = metadata.GitCommit,
                UnityVersion = metadata.UnityVersion,
                Seed = first.Value<int?>("seed") ?? 0,
                Tester = metadata.Tester,
                IntendedBuildId = metadata.IntendedBuildId,
                FinalBuildId = payload.Value<string>("finalBuildId"),
                FinalBoardJson = finalBoard.ToString(Formatting.None),
                FinalPermanentAttack = SumBoard(finalBoard, "attack"),
                FinalPermanentHealth = SumBoard(finalBoard, "health"),
                TurnTenReached = turnTen != null,
                TurnTenBoardJson = turnTenBoard?.ToString(Formatting.None),
                TurnTenPermanentAttack = SumBoard(turnTenBoard, "attack"),
                TurnTenPermanentHealth = SumBoard(turnTenBoard, "health"),
                TurnTenBuildId = turnTen?["payload"]?.Value<string>("buildId") ??
                    "Unclassified",
                TurnTenCoreInstanceIds = string.Join(";", (turnTenBoard ?? new JArray())
                    .OfType<JObject>()
                    .Where(value => CoreBuildClassifier.IsCoreCardId(
                        value.Value<string>("cardId")))
                    .Select(value => value.Value<string>("instanceId"))
                    .Where(value => !string.IsNullOrWhiteSpace(value))),
                TurnTenRefreshesPaid = turnTen?["payload"]?.Value<int?>("RefreshesPaid") ?? 0,
                TurnTenRefreshesFree = turnTen?["payload"]?.Value<int?>("RefreshesFree") ?? 0,
                TurnTenMinionsBought = turnTen?["payload"]?.Value<int?>("MinionsBought") ?? 0,
                TurnTenMinionsSold = turnTen?["payload"]?.Value<int?>("MinionsSold") ?? 0,
                TurnTenSpellsUsed = turnTen?["payload"]?.Value<int?>("SpellsUsed") ?? 0,
                TurnTenTavernUpgrades = turnTen?["payload"]?.Value<int?>("TavernUpgrades") ?? 0,
                TurnTenGoldWasted = turnTen?["payload"]?.Value<int?>("GoldWasted") ?? 0,
                TurnTenTriplesFormed = turnTen?["payload"]?.Value<int?>("TriplesFormed") ?? 0,
                Result = payload.Value<string>("result"),
                FloorReached = payload.Value<int?>("floorReached") ?? 0,
                RunTurn = payload.Value<int?>("runTurn") ?? 0,
                ElapsedMinutes = payload.Value<double?>("elapsedMinutes") ?? 0d,
                HealthRemaining = payload.Value<int?>("healthRemaining") ?? 0,
                BattlesWon = payload.Value<int?>("BattlesWon") ?? 0,
                BattlesNotWon = payload.Value<int?>("BattlesNotWon") ?? 0,
                ElitesAttempted = payload.Value<int?>("ElitesAttempted") ?? 0,
                ElitesDefeated = payload.Value<int?>("ElitesDefeated") ?? 0,
                BossAttempts = payload.Value<int?>("BossAttempts") ?? 0,
                BossesDefeated = payload.Value<int?>("BossesDefeated") ?? 0,
                RefreshesPaid = payload.Value<int?>("RefreshesPaid") ?? 0,
                RefreshesFree = payload.Value<int?>("RefreshesFree") ?? 0,
                MinionsBought = payload.Value<int?>("MinionsBought") ?? 0,
                MinionsSold = payload.Value<int?>("MinionsSold") ?? 0,
                SpellsUsed = payload.Value<int?>("SpellsUsed") ?? 0,
                TavernUpgrades = payload.Value<int?>("TavernUpgrades") ?? 0,
                GoldWasted = payload.Value<int?>("GoldWasted") ?? 0,
                FirstCoreTurn = payload.Value<int?>("FirstCoreTurn"),
                SecondCoreTurn = secondCoreTurn,
                TriplesFormed = payload.Value<int?>("TriplesFormed") ?? 0,
                TargetedDiscoversUsed = payload.Value<int?>("TargetedDiscoversUsed") ?? 0,
                DualCoreBeforeFloorThreeBoss = secondCoreTurn.HasValue &&
                    secondCoreTurn.Value < floorThreeBossTurn,
                RouteNodeIds = string.Join(";", routes),
                EliteRouteChosen = entries.Any(value =>
                    EventType(value) == "NodeEntered" &&
                    Value<string>(value, "payload", "nodeType") == "Elite"),
                CoreSurvivorsByBattleJson = JsonConvert.SerializeObject(battles.Select(value => new
                {
                    runTurn = value.Value<int?>("runTurn") ?? 0,
                    start = value["playerStartInstanceIds"],
                    survivors = value["playerSurvivorInstanceIds"]
                })),
                PermanentDeltasByInstanceJson = JsonConvert.SerializeObject(battles.Select(value => new
                {
                    runTurn = value.Value<int?>("runTurn") ?? 0,
                    deltas = value["permanentDeltas"]
                })),
                EventChoicesJson = JsonConvert.SerializeObject(eventChoices),
                RewardChoicesJson = JsonConvert.SerializeObject(rewardChoices),
                FailureReason = metadata.FailureReason,
                BoringMoment = metadata.BoringMoment,
                UnfairMoment = metadata.UnfairMoment,
                DecisionSummaryPath = metadata.DecisionSummaryPath,
                RawTelemetryPath = metadata.RawTelemetryPath
            };
        }

        public IReadOnlyList<BalanceCardFunnelRow> AggregateCardFunnel(
            IEnumerable<string> ndjsonRuns,
            string candidateId,
            string tuningRound)
        {
            var rows = new Dictionary<string, BalanceCardFunnelRow>();
            foreach (var document in ndjsonRuns ?? Enumerable.Empty<string>())
            {
                foreach (var entry in Parse(document))
                {
                    AccumulateFunnelEntry(entry, rows, candidateId, tuningRound);
                }
            }

            return rows.Values.OrderBy(value => value.Tier)
                .ThenBy(value => value.CardId).ToList().AsReadOnly();
        }

        private void AccumulateFunnelEntry(
            JObject entry,
            IDictionary<string, BalanceCardFunnelRow> rows,
            string candidateId,
            string tuningRound)
        {
            var eventType = EventType(entry);
            var payload = entry["payload"] as JObject;
            if (payload == null)
            {
                return;
            }

            if (eventType == "ShopSnapshot")
            {
                var trigger = payload.Value<string>("trigger");
                if (trigger != "OnShopPhaseStart" && trigger != "OnRefresh") return;
                foreach (var token in payload["minionOfferIds"] as JArray ?? new JArray())
                {
                    Increment(token.Value<string>(), row => row.Offered++);
                }
                Increment(payload.Value<string>("spellOfferId"), row => row.Offered++);
                return;
            }

            if (eventType == "RewardChoiceResolved")
            {
                foreach (var candidate in payload["candidates"] as JArray ?? new JArray())
                {
                    var id = candidate.Type == JTokenType.String
                        ? candidate.Value<string>()
                        : candidate.Value<string>("cardId");
                    Increment(id, row => row.Offered++);
                }
                Increment(payload.Value<string>("selectedCardId"), row => row.BoughtOrPicked++);
                return;
            }

            if (eventType == "RunEnded")
            {
                foreach (var card in payload["finalBoard"] as JArray ?? new JArray())
                {
                    Increment(card?.Value<string>("cardId"), row => row.SurvivedToRunEnd++);
                }
                return;
            }

            if (eventType != "ShopEvent") return;
            var cardId = payload.Value<string>("cardId");
            switch (payload.Value<string>("type"))
            {
                case "OnBuy":
                    Increment(cardId, row => row.BoughtOrPicked++);
                    break;
                case "OnPlay":
                case "OnSpellUsed":
                    Increment(cardId, row => row.Played++);
                    break;
                case "OnSell":
                    Increment(cardId, row => row.Sold++);
                    break;
                case "OnTripleFormed":
                    Increment(cardId, row =>
                    {
                        row.TripleMaterials += 3;
                        row.TriplesCompleted++;
                    });
                    break;
                case "OnDiscoverResolved":
                    Increment(payload.Value<string>("targetCardId"), row =>
                    {
                        row.BoughtOrPicked++;
                        row.DiscoverPicked++;
                    });
                    break;
                case "OnDiscoverStarted":
                    foreach (var token in payload["discoverCandidateIds"] as JArray ?? new JArray())
                    {
                        Increment(token.Value<string>(), row => row.DiscoverOffered++);
                    }
                    break;
            }

            void Increment(string id, Action<BalanceCardFunnelRow> action)
            {
                if (string.IsNullOrWhiteSpace(id)) return;
                if (!rows.TryGetValue(id, out var row))
                {
                    row = new BalanceCardFunnelRow
                    {
                        CandidateId = candidateId,
                        TuningRound = tuningRound,
                        Tier = resolveTier(id),
                        CardId = id
                    };
                    rows.Add(id, row);
                }
                action(row);
            }
        }

        private static List<JObject> Parse(string ndjson)
        {
            var entries = (ndjson ?? string.Empty)
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => JObject.Parse(line))
                .ToList();
            if (entries.Any(value =>
                    value.Value<string>("schemaVersion") != RunTelemetry.SchemaVersion))
            {
                throw new InvalidOperationException("Unsupported telemetry schemaVersion.");
            }
            return entries;
        }

        private static string EventType(JObject entry)
        {
            return entry.Value<string>("eventType");
        }

        private static T Value<T>(JObject value, string objectName, string propertyName)
        {
            var nested = value[objectName] as JObject;
            return nested == null ? default(T) : nested.Value<T>(propertyName);
        }

        private static int SumBoard(JArray board, string propertyName)
        {
            return board == null
                ? 0
                : board.OfType<JObject>().Sum(value => value.Value<int?>(propertyName) ?? 0);
        }
    }

    public static class BalanceTelemetryCsv
    {
        public static string SerializeRunSummaries(IEnumerable<BalanceRunSummary> summaries)
        {
            var builder = new StringBuilder();
            builder.AppendLine("balanceSchemaVersion,tuningRound,candidateId,contentVersion,configHash,gitCommit,unityVersion,coreClassifierVersion,seed,tester,intendedBuildId,finalBuildId,finalBoardJson,finalPermanentAttack,finalPermanentHealth,turnTenReached,turnTenBuildId,turnTenBoardJson,turnTenPermanentAttack,turnTenPermanentHealth,turnTenCoreInstanceIds,turnTenRefreshesPaid,turnTenRefreshesFree,turnTenMinionsBought,turnTenMinionsSold,turnTenSpellsUsed,turnTenTavernUpgrades,turnTenGoldWasted,turnTenTriplesFormed,result,floorReached,runTurn,elapsedMinutes,healthRemaining,battlesWon,battlesNotWon,elitesAttempted,elitesDefeated,bossAttempts,bossesDefeated,coreSurvivorsByBattleJson,permanentDeltasByInstanceJson,refreshesPaid,refreshesFree,minionsBought,minionsSold,spellsUsed,tavernUpgrades,goldWasted,firstCoreTurn,secondCoreTurn,dualCoreBeforeFloorThreeBoss,triplesFormed,targetedDiscoversUsed,routeNodeIds,eliteRouteChosen,eventChoicesJson,rewardChoicesJson,failureReason,boringMoment,unfairMoment,decisionSummaryPath,rawTelemetryPath");
            foreach (var row in summaries ?? Enumerable.Empty<BalanceRunSummary>())
            {
                AppendRow(builder, new object[]
                {
                    row.BalanceSchemaVersion, row.TuningRound, row.CandidateId,
                    row.ContentVersion, row.ConfigHash, row.GitCommit, row.UnityVersion,
                    row.CoreClassifierVersion, row.Seed, row.Tester, row.IntendedBuildId,
                    row.FinalBuildId, row.FinalBoardJson, row.FinalPermanentAttack,
                    row.FinalPermanentHealth, row.TurnTenReached, row.TurnTenBuildId,
                    row.TurnTenBoardJson,
                    row.TurnTenPermanentAttack, row.TurnTenPermanentHealth,
                    row.TurnTenCoreInstanceIds, row.TurnTenRefreshesPaid,
                    row.TurnTenRefreshesFree, row.TurnTenMinionsBought,
                    row.TurnTenMinionsSold, row.TurnTenSpellsUsed,
                    row.TurnTenTavernUpgrades, row.TurnTenGoldWasted,
                    row.TurnTenTriplesFormed, row.Result, row.FloorReached, row.RunTurn,
                    row.ElapsedMinutes.ToString("0.###", CultureInfo.InvariantCulture),
                    row.HealthRemaining, row.BattlesWon, row.BattlesNotWon,
                    row.ElitesAttempted, row.ElitesDefeated, row.BossAttempts,
                    row.BossesDefeated, row.CoreSurvivorsByBattleJson,
                    row.PermanentDeltasByInstanceJson, row.RefreshesPaid, row.RefreshesFree,
                    row.MinionsBought, row.MinionsSold, row.SpellsUsed,
                    row.TavernUpgrades, row.GoldWasted, row.FirstCoreTurn,
                    row.SecondCoreTurn, row.DualCoreBeforeFloorThreeBoss,
                    row.TriplesFormed, row.TargetedDiscoversUsed,
                    row.RouteNodeIds, row.EliteRouteChosen,
                    row.EventChoicesJson, row.RewardChoicesJson, row.FailureReason,
                    row.BoringMoment, row.UnfairMoment, row.DecisionSummaryPath,
                    row.RawTelemetryPath
                });
            }
            return builder.ToString();
        }

        public static string SerializeCardFunnel(IEnumerable<BalanceCardFunnelRow> rows)
        {
            var builder = new StringBuilder();
            builder.AppendLine("balanceSchemaVersion,tuningRound,candidateId,tier,cardId,offered,boughtOrPicked,played,sold,survivedToRunEnd,tripleMaterials,triplesCompleted,discoverOffered,discoverPicked,offerToPickRate,smoothedOfferToPickRate,pickToPlayRate");
            foreach (var row in rows ?? Enumerable.Empty<BalanceCardFunnelRow>())
            {
                AppendRow(builder, new object[]
                {
                    row.BalanceSchemaVersion, row.TuningRound, row.CandidateId,
                    row.Tier, row.CardId, row.Offered, row.BoughtOrPicked,
                    row.Played, row.Sold, row.SurvivedToRunEnd,
                    row.TripleMaterials, row.TriplesCompleted, row.DiscoverOffered,
                    row.DiscoverPicked,
                    row.OfferToPickRate.ToString("0.######", CultureInfo.InvariantCulture),
                    row.SmoothedOfferToPickRate.ToString("0.######", CultureInfo.InvariantCulture),
                    row.PickToPlayRate.ToString("0.######", CultureInfo.InvariantCulture)
                });
            }
            return builder.ToString();
        }

        private static void AppendRow(StringBuilder builder, IEnumerable<object> values)
        {
            builder.AppendLine(string.Join(",", values.Select(Escape)));
        }

        private static string Escape(object value)
        {
            if (value == null) return string.Empty;
            var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            return text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
                ? text
                : $"\"{text.Replace("\"", "\"\"")}\"";
        }
    }
}

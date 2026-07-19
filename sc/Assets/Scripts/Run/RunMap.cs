using System;
using System.Collections.Generic;
using System.Linq;
using SpireChess.Config;

namespace SpireChess.Run
{
    public sealed class MapRequest
    {
        public MapRequest(int seed, int floor)
        {
            Seed = seed;
            Floor = floor;
        }

        public int Seed { get; }
        public int Floor { get; }
    }

    public sealed class MapNodeDefinition
    {
        public MapNodeDefinition(
            string id,
            RunNodeType type,
            int column,
            int row,
            string payloadId,
            int combatIndex,
            IEnumerable<string> nextNodeIds)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Type = type;
            Column = column;
            Row = row;
            PayloadId = payloadId;
            CombatIndex = combatIndex;
            NextNodeIds = new List<string>(nextNodeIds ?? Array.Empty<string>()).AsReadOnly();
        }

        public string Id { get; }
        public RunNodeType Type { get; }
        public int Column { get; }
        public int Row { get; }
        public string PayloadId { get; }
        public int CombatIndex { get; }
        public IReadOnlyList<string> NextNodeIds { get; }
    }

    public sealed class MapDefinition
    {
        private readonly Dictionary<string, MapNodeDefinition> nodesById;

        public MapDefinition(
            string id,
            int floor,
            IEnumerable<MapNodeDefinition> nodes,
            IEnumerable<string> startNodeIds)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Floor = floor;
            Nodes = new List<MapNodeDefinition>(nodes ?? throw new ArgumentNullException(nameof(nodes)))
                .AsReadOnly();
            StartNodeIds = new List<string>(startNodeIds ?? Array.Empty<string>()).AsReadOnly();
            nodesById = Nodes
                .Where(node => node != null && !string.IsNullOrWhiteSpace(node.Id))
                .GroupBy(node => node.Id)
                .ToDictionary(group => group.Key, group => group.First());
        }

        public string Id { get; }
        public int Floor { get; }
        public IReadOnlyList<MapNodeDefinition> Nodes { get; }
        public IReadOnlyList<string> StartNodeIds { get; }

        public bool TryGetNode(string id, out MapNodeDefinition node)
        {
            return nodesById.TryGetValue(id ?? string.Empty, out node);
        }
    }

    public interface IMapProvider
    {
        MapDefinition CreateMap(MapRequest request);
    }

    public sealed class FixedMapProvider : IMapProvider
    {
        private readonly IReadOnlyList<RunMapConfig> configs;

        public FixedMapProvider(IReadOnlyList<RunMapConfig> configs)
        {
            this.configs = configs ?? throw new ArgumentNullException(nameof(configs));
        }

        public MapDefinition CreateMap(MapRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var config = configs.FirstOrDefault(map => map != null && map.Floor == request.Floor);
            if (config == null)
            {
                throw new InvalidOperationException($"Missing fixed map for floor {request.Floor}.");
            }

            var nodes = (config.Nodes ?? new List<RunMapNodeConfig>()).Select(node =>
            {
                if (!Enum.TryParse(node.Type, true, out RunNodeType type))
                {
                    throw new InvalidOperationException(
                        $"Unknown node type {node.Type} in map {config.Id}.");
                }

                return new MapNodeDefinition(
                    node.Id,
                    type,
                    node.Column,
                    node.Row,
                    node.PayloadId,
                    node.CombatIndex,
                    node.NextNodeIds);
            });

            var definition = new MapDefinition(
                config.Id,
                config.Floor,
                nodes,
                config.StartNodeIds);
            var validation = MapValidator.Validate(definition);
            validation.ThrowIfInvalid();
            return definition;
        }
    }

    public static class MapValidator
    {
        public static ConfigValidationResult Validate(MapDefinition map)
        {
            var result = new ConfigValidationResult();
            if (map == null)
            {
                result.AddError("Run map is missing.");
                return result;
            }

            if (map.Nodes.Count == 0)
            {
                result.AddError($"Map {map.Id} has no nodes.");
                return result;
            }

            foreach (var duplicate in map.Nodes
                .Where(node => node != null)
                .GroupBy(node => node.Id)
                .Where(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1))
            {
                result.AddError($"Map {map.Id} has an empty or duplicate node id: {duplicate.Key}.");
            }

            var ids = new HashSet<string>(map.Nodes.Where(node => node != null).Select(node => node.Id));
            foreach (var startId in map.StartNodeIds)
            {
                if (!ids.Contains(startId))
                {
                    result.AddError($"Map {map.Id} references missing start node {startId}.");
                }
            }

            foreach (var node in map.Nodes.Where(node => node != null))
            {
                foreach (var nextId in node.NextNodeIds)
                {
                    if (!ids.Contains(nextId))
                    {
                        result.AddError($"Node {node.Id} references missing successor {nextId}.");
                    }
                }
            }

            if (map.StartNodeIds.Count == 0)
            {
                result.AddError($"Map {map.Id} has no start node.");
            }

            foreach (var startId in map.StartNodeIds)
            {
                if (map.TryGetNode(startId, out var start) && start.Type != RunNodeType.Shop)
                {
                    result.AddError($"Map {map.Id} must start at a Shop node, got {start.Type} ({startId}).");
                }
            }

            var bosses = map.Nodes.Where(node => node?.Type == RunNodeType.Boss).ToList();
            if (bosses.Count != 1)
            {
                result.AddError($"Map {map.Id} must have exactly one Boss, got {bosses.Count}.");
            }
            else if (bosses[0].NextNodeIds.Count > 0)
            {
                result.AddError($"Boss node {bosses[0].Id} must not have a successor.");
            }

            var visiting = new HashSet<string>();
            var visited = new HashSet<string>();
            foreach (var startId in map.StartNodeIds)
            {
                DetectCycle(map, startId, visiting, visited, result);
            }

            if (bosses.Count == 1)
            {
                foreach (var startId in map.StartNodeIds)
                {
                    if (!CanReach(map, startId, bosses[0].Id, new HashSet<string>()))
                    {
                        result.AddError($"Start node {startId} cannot reach Boss {bosses[0].Id}.");
                    }
                }

                foreach (var path in EnumerateBossPaths(map, bosses[0].Id))
                {
                    ValidateShopCombatCadence(map, path, result);
                }
            }

            return result;
        }

        private static IEnumerable<IReadOnlyList<MapNodeDefinition>> EnumerateBossPaths(
            MapDefinition map,
            string bossId)
        {
            foreach (var startId in map.StartNodeIds)
            {
                foreach (var path in EnumerateBossPaths(
                             map,
                             startId,
                             bossId,
                             new List<MapNodeDefinition>()))
                {
                    yield return path;
                }
            }
        }

        private static IEnumerable<IReadOnlyList<MapNodeDefinition>> EnumerateBossPaths(
            MapDefinition map,
            string nodeId,
            string bossId,
            List<MapNodeDefinition> prefix)
        {
            if (!map.TryGetNode(nodeId, out var node))
            {
                yield break;
            }

            if (prefix.Any(value => value.Id == nodeId))
            {
                yield break;
            }

            prefix.Add(node);
            if (nodeId == bossId)
            {
                yield return prefix.ToArray();
            }
            else
            {
                foreach (var nextId in node.NextNodeIds)
                {
                    foreach (var path in EnumerateBossPaths(
                                 map,
                                 nextId,
                                 bossId,
                                 new List<MapNodeDefinition>(prefix)))
                    {
                        yield return path;
                    }
                }
            }
        }

        private static void ValidateShopCombatCadence(
            MapDefinition map,
            IReadOnlyList<MapNodeDefinition> path,
            ConfigValidationResult result)
        {
            var significant = path.Where(node =>
                    node.Type == RunNodeType.Shop || IsCombat(node.Type))
                .ToArray();
            if (significant.Length != 10)
            {
                result.AddError(
                    $"Map {map.Id} path {PathText(path)} must contain exactly 5 Shops and 5 combats.");
                return;
            }

            for (var index = 0; index < significant.Length; index++)
            {
                var expectedShop = index % 2 == 0;
                var node = significant[index];
                if (expectedShop && node.Type != RunNodeType.Shop)
                {
                    result.AddError(
                        $"Map {map.Id} path {PathText(path)} must alternate Shop and combat nodes.");
                    return;
                }

                if (!expectedShop && !IsCombat(node.Type))
                {
                    result.AddError(
                        $"Map {map.Id} path {PathText(path)} must alternate Shop and combat nodes.");
                    return;
                }

                if (!expectedShop && node.CombatIndex != (index + 1) / 2)
                {
                    result.AddError(
                        $"Map {map.Id} combat {node.Id} must use combatIndex {(index + 1) / 2}, got {node.CombatIndex}.");
                }
            }

            if (significant[significant.Length - 1].Type != RunNodeType.Boss)
            {
                result.AddError($"Map {map.Id} path {PathText(path)} must end at a Boss.");
            }
        }

        private static bool IsCombat(RunNodeType type)
        {
            return type == RunNodeType.Normal ||
                   type == RunNodeType.Elite ||
                   type == RunNodeType.Boss;
        }

        private static string PathText(IEnumerable<MapNodeDefinition> path)
        {
            return string.Join(" -> ", path.Select(node => node.Id));
        }

        private static void DetectCycle(
            MapDefinition map,
            string id,
            ISet<string> visiting,
            ISet<string> visited,
            ConfigValidationResult result)
        {
            if (visited.Contains(id) || !map.TryGetNode(id, out var node))
            {
                return;
            }

            if (!visiting.Add(id))
            {
                result.AddError($"Map {map.Id} contains a cycle at {id}.");
                return;
            }

            foreach (var nextId in node.NextNodeIds)
            {
                DetectCycle(map, nextId, visiting, visited, result);
            }

            visiting.Remove(id);
            visited.Add(id);
        }

        private static bool CanReach(
            MapDefinition map,
            string from,
            string target,
            ISet<string> visited)
        {
            if (from == target)
            {
                return true;
            }

            if (!visited.Add(from) || !map.TryGetNode(from, out var node))
            {
                return false;
            }

            return node.NextNodeIds.Any(next => CanReach(map, next, target, visited));
        }
    }

    public sealed class MapProgressState
    {
        private readonly MapDefinition map;
        private readonly Dictionary<string, RunNodeStatus> statusById;

        public MapProgressState(MapDefinition map)
        {
            this.map = map ?? throw new ArgumentNullException(nameof(map));
            statusById = map.Nodes.ToDictionary(node => node.Id, node => RunNodeStatus.Locked);
            foreach (var startId in map.StartNodeIds)
            {
                statusById[startId] = RunNodeStatus.Reachable;
            }
        }

        public RunNodeStatus GetStatus(string nodeId)
        {
            return statusById.TryGetValue(nodeId ?? string.Empty, out var status)
                ? status
                : RunNodeStatus.Locked;
        }

        internal bool TryEnter(string nodeId)
        {
            if (GetStatus(nodeId) != RunNodeStatus.Reachable)
            {
                return false;
            }

            foreach (var id in statusById.Keys.ToList())
            {
                if (statusById[id] == RunNodeStatus.Reachable)
                {
                    statusById[id] = RunNodeStatus.Locked;
                }
            }

            statusById[nodeId] = RunNodeStatus.Current;
            return true;
        }

        internal void Resolve(string nodeId)
        {
            if (!map.TryGetNode(nodeId, out var node))
            {
                throw new InvalidOperationException($"Unknown node {nodeId}.");
            }

            statusById[nodeId] = RunNodeStatus.Resolved;
            foreach (var nextId in node.NextNodeIds)
            {
                if (statusById[nextId] == RunNodeStatus.Locked)
                {
                    statusById[nextId] = RunNodeStatus.Reachable;
                }
            }
        }
    }
}

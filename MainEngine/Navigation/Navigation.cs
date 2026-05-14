using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MainEngine.Navigation
{
    public sealed class NavigationPath
    {
        public static NavigationPath Empty { get; } = new NavigationPath(Array.Empty<int>(), Array.Empty<int>(), 0f);

        public IReadOnlyList<int> NodeIds { get; }
        public IReadOnlyList<int> HighwayIds { get; }
        public float TotalCost { get; }

        public bool IsEmpty => NodeIds.Count == 0;

        public NavigationPath(IReadOnlyList<int> nodeIds, IReadOnlyList<int> highwayIds, float totalCost)
        {
            NodeIds = nodeIds;
            HighwayIds = highwayIds;
            TotalCost = totalCost;
        }
        
    }
    public class Navigation
    {
        private readonly Dictionary<int, NavNode> _nodes = new();
        private readonly Dictionary<int, Highway> _highwaysById = new();
        private readonly Dictionary<int, List<Highway>> _outgoingByNodeId = new();

        public IReadOnlyDictionary<int, NavNode> Nodes => _nodes;
        public IReadOnlyDictionary<int, Highway> Highways => _highwaysById;

        public void Clear()
        {
            _nodes.Clear();
            _highwaysById.Clear();
            _outgoingByNodeId.Clear();
        }

        public void AddNode(NavNode node)
        {
            if (_nodes.ContainsKey(node.Id))
                throw new InvalidOperationException($"Node id {node.Id} already exists.");

            _nodes[node.Id] = node;
            _outgoingByNodeId[node.Id] = new List<Highway>();
        }

        public void AddHighway(Highway highway)
        {
            if (!_nodes.ContainsKey(highway.FromId))
                throw new InvalidOperationException($"Highway {highway.Id} has unknown FromId {highway.FromId}.");

            if (!_nodes.ContainsKey(highway.ToId))
                throw new InvalidOperationException($"Highway {highway.Id} has unknown ToId {highway.ToId}.");

            if (_highwaysById.ContainsKey(highway.Id))
                throw new InvalidOperationException($"Highway id {highway.Id} already exists.");
            
            _highwaysById[highway.Id] = highway;
            _outgoingByNodeId[highway.FromId].Add(highway);
            if (highway.IsTwoWay)
            {
                _outgoingByNodeId[highway.ToId].Add(highway);
            }
        }

        public bool TryGetNode(int nodeId, out NavNode node)
        {
            return _nodes.TryGetValue(nodeId, out node);
        }

        public IReadOnlyList<Highway> GetOutgoingHighways(int nodeId)
        {
            if (_outgoingByNodeId.TryGetValue(nodeId, out List<Highway> highways))
                return highways;
            
            return Array.Empty<Highway>();
        }

        public bool TryGetNearestNode(Vector2 worldPosition, out int nodeId, float maxDistance = float.PositiveInfinity)
        {
            nodeId = -1;
            
            if (_nodes.Count == 0)
                return false;

            float maxDistanceSq = float.IsPositiveInfinity(maxDistance) ? float.PositiveInfinity : maxDistance * maxDistance;

            float bestDistanceSq = float.PositiveInfinity;

            foreach (var pair in _nodes)
            {
                float distSq = Vector2.DistanceSquared(worldPosition, pair.Value.Position);
                if (distSq <= maxDistanceSq && distSq < bestDistanceSq)
                {
                    bestDistanceSq = distSq;
                    nodeId = pair.Key;
                }
            }
            return nodeId >= 0;
        }

        public bool TryFindPath(int startNodeId, int goalNodeId, out NavigationPath path)
        {
            path = NavigationPath.Empty;

            if (!_nodes.ContainsKey(startNodeId) || !_nodes.ContainsKey(goalNodeId))
                return false;
            
            if (startNodeId == goalNodeId)
            {
                path = new NavigationPath(new[] { startNodeId }, Array.Empty<int>(), 0f);
                return true;
            }

            float maxSpeed = GetMaxSpeedLimit();
            var open = new PriorityQueue<int, float>();
            var closed = new HashSet<int>();
            var cameFromHighway = new Dictionary<int, Highway>();
            var gScore = new Dictionary<int, float> { [startNodeId] = 0f };

            open.Enqueue(startNodeId, HeuristicTime(startNodeId, goalNodeId, maxSpeed));

            while (open.Count > 0)
            {
                int current = open.Dequeue();

                if (closed.Contains(current))
                    continue;

                if (current == goalNodeId)
                {
                    path = ReconstructPath(startNodeId, goalNodeId, cameFromHighway, gScore);
                    return !path.IsEmpty;
                }

                closed.Add(current);

                if (!_outgoingByNodeId.TryGetValue(current, out List<Highway> neighbors))
                    continue;

                foreach (Highway edge in neighbors)
                {
                    if (edge.IsBlocked || float.IsInfinity(edge.Cost))
                        continue;

                    int next = edge.FromId == current ? edge.ToId : edge.FromId;
                    if (closed.Contains(next))
                        continue;

                    float tentativeG = gScore[current] + edge.Cost;

                    if (!gScore.TryGetValue(next, out float knownG) || tentativeG < knownG)
                    {
                        gScore[next] = tentativeG;
                        cameFromHighway[next] = edge;

                        float f = tentativeG + HeuristicTime(next, goalNodeId, maxSpeed);
                        open.Enqueue(next, f);
                    }
                }
            }
            return false;
        }

        private float HeuristicTime(int nodeId, int goalNodeId, float maxSpeed)
        {
            Vector2 from = _nodes[nodeId].Position;
            Vector2 to = _nodes[goalNodeId].Position;
            float distance = Vector2.Distance(from, to);
            return distance / MathF.Max(0.001f, maxSpeed);
        }

        private float GetMaxSpeedLimit()
        {
            float max = 1f;

            foreach (var h in _highwaysById.Values)
            {
                if (!h.IsBlocked && h.SpeedLimit > max)
                    max = h.SpeedLimit;
            }
            return max;
        }

        private static NavigationPath ReconstructPath(
            int startNodeId,
            int goalNodeId,
            Dictionary<int, Highway> cameFromHighway,
            Dictionary<int, float> gScore)
        {
            var nodeIds = new List<int> { goalNodeId };
            var highwayIds = new List<int>();

            int cursor = goalNodeId;

            while (cursor != startNodeId)
            {
                if (!cameFromHighway.TryGetValue(cursor, out Highway edge))
                    return NavigationPath.Empty;

                highwayIds.Add(edge.Id);
                cursor = edge.FromId == cursor ? edge.ToId : edge.FromId;
                nodeIds.Add(cursor);
            }

            nodeIds.Reverse();
            highwayIds.Reverse();

            return new NavigationPath(nodeIds, highwayIds, gScore[goalNodeId]);
        }
    }
}
using Microsoft.Xna.Framework;

namespace MainEngine.Navigation
{
    public class NavigationFollower
    {
        private readonly Navigation _navigation;
        public NavigationPath _path = NavigationPath.Empty;
        private int _nextNodeIndex = -1;

        public float ArrivalDistance { get; set; } = 10f;

        public bool HasPath =>
            !_path.IsEmpty&&
            _nextNodeIndex >= 0 &&
            _nextNodeIndex < _path.NodeIds.Count;

        public bool IsFinished => !HasPath;

        public NavigationPath CurrentPath => _path;

        public NavigationFollower(Navigation navigation)
        {
            _navigation = navigation;
        }

        public bool TryPlanFromWorld(Vector2 startWorld, Vector2 goalWorld, float snapDistance = float.PositiveInfinity)
        {
            if (!_navigation.TryGetNearestNode(startWorld, out int startNodeId, snapDistance))
                return false;

            if (!_navigation.TryGetNearestNode(goalWorld, out int goalNodeId, snapDistance))
                return false;

            if (!_navigation.TryFindPath(startNodeId, goalNodeId, out _path))
                return false;

            _nextNodeIndex = _path.NodeIds.Count > 1 ? 1 : 0;
            return true;
        }

        public Vector2 GetSteeringTarget(Vector2 currentWorld)
        {
            if (!HasPath)
                return currentWorld;

            float arriveSq = ArrivalDistance * ArrivalDistance;

            while (HasPath)
            {
                int nodeId = _path.NodeIds[_nextNodeIndex];

                if (!_navigation.TryGetNode(nodeId, out NavNode node))
                {
                    Clear();
                    return currentWorld;
                }

                if (Vector2.DistanceSquared(currentWorld, node.Position) <= arriveSq)
                {
                    _nextNodeIndex++;
                    continue;
                }

                return node.Position;
            }

            return currentWorld;
        }
        
        public void Clear()
        {
            _path = NavigationPath.Empty;
            _nextNodeIndex = -1;
        }
    }
}
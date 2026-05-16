using MainEngine.Entities;
using MainEngine.Navigation;
using Microsoft.Xna.Framework;

namespace LILITH.Core.Enemies
{
    /// <summary>
    /// Shared helper that any FSM state can call.
    /// Far from target → follow highways.  Close → direct pursuit.
    /// </summary>
    public static class EnemyNavigation
    {
        public static void NavigateOrPursue(
            Enemy enemy,
            NavigationFollower follower,
            Vector2 target,
            float dt,
            float speed,
            float directPursuitRadius,
            ref float repathTimer,
            float repathInterval = 0.25f)
        {
            float dist = Vector2.Distance(enemy.Position, target);

            // Close enough → beeline, DetectionRadius check in MoveToward is fine here
            // because the target (player) is within range by definition.
            if (follower == null || dist <= directPursuitRadius)
            {
                follower?.Clear();
                enemy.MoveToward(target, dt, speed);
                return;
            }

            // Repath on timer
            repathTimer -= dt;
            if (repathTimer <= 0f)
            {
                repathTimer = repathInterval;
                if (!follower.TryPlanFromWorld(enemy.Position, target))
                    follower.Clear();
            }

            Vector2 steerTarget = follower.HasPath
                ? follower.GetSteeringTarget(enemy.Position)
                : target;

            // Move toward the nav waypoint directly — NOT through MoveToward,
            // because MoveToward refuses to move if the target is beyond
            // DetectionRadius, and waypoints can be arbitrarily far apart.
            MoveDirect(enemy, steerTarget, dt, speed);
        }

        /// <summary>
        /// Moves the enemy toward a position without the DetectionRadius
        /// gate that Enemy.MoveToward enforces. Used for navigation waypoints.
        /// </summary>
        private static void MoveDirect(Enemy enemy, Vector2 target, float dt, float speed)
        {
            Vector2 toTarget = target - enemy.Position;
            float dist = toTarget.Length();

            if (dist <= 0.001f)
            {
                enemy.CurrentSpeed = 0f;
                return;
            }

            enemy.CurrentSpeed = speed;
            Vector2 direction = toTarget / dist;
            enemy.Position += direction * speed * dt;
        }
    }
}
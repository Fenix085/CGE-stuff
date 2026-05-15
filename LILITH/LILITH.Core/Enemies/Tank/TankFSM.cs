using MainEngine.FSM;
using Microsoft.Xna.Framework;

namespace LILITH.Core.Enemies.Tank
{
    public sealed class TankContext
    {
        public Tank Tank { get; }
        public Vector2 PlayerPosition { get; set; }
        public bool PlayerIsDead { get; set; }

        // ── FSM-driven outputs read by MainScene ──
        public bool ShouldSpawnAgents { get; set; }
        public bool ShouldFlockAttackPlayer { get; set; }
        public float FlockSpeed { get; set; }
        public float TankMoveSpeed { get; set; }
        public bool ApplyPlayerForce { get; set; }
        public float PlayerForceRadius { get; set; }
        public float PlayerForceStrength { get; set; }

        public TankContext(Tank tank)
        {
            Tank = tank;
        }

        public float DistanceToPlayer() =>
            Vector2.Distance(Tank.Position, PlayerPosition);
    }

    public sealed class TankFSM
    {
        private readonly TankContext _context;
        private readonly FiniteStateMachine<TankContext> _fsm = new();

        // ── Public read-outs for MainScene ──
        public bool ShouldSpawnAgents => _context.ShouldSpawnAgents;
        public bool ShouldFlockAttackPlayer => _context.ShouldFlockAttackPlayer;
        public float FlockSpeed => _context.FlockSpeed;
        public float TankMoveSpeed => _context.TankMoveSpeed;
        public bool ApplyPlayerForce => _context.ApplyPlayerForce;
        public float PlayerForceRadius => _context.PlayerForceRadius;
        public float PlayerForceStrength => _context.PlayerForceStrength;

        public TankFSM(Tank tank)
        {
            _context = new TankContext(tank);

            var idle = new IdleState();
            var investigate = new InvestigateState();
            var attack = new AttackState();
            var dead = new DeadState();

            // ── Idle transitions ──
            idle.AddTransition(new FSMTransition<TankContext>(
                investigate,
                ctx => !ctx.PlayerIsDead
                       && ctx.DistanceToPlayer() <= ctx.Tank.DetectionRadius));

            idle.AddTransition(new FSMTransition<TankContext>(
                dead,
                ctx => ctx.Tank.Health.IsDead));

            // ── Investigate transitions ──
            investigate.AddTransition(new FSMTransition<TankContext>(
                attack,
                ctx => !ctx.PlayerIsDead
                       && ctx.DistanceToPlayer() <= ctx.Tank.FollowRadius));

            investigate.AddTransition(new FSMTransition<TankContext>(
                idle,
                ctx => ctx.PlayerIsDead
                       || ctx.DistanceToPlayer() > ctx.Tank.DetectionRadius));

            investigate.AddTransition(new FSMTransition<TankContext>(
                dead,
                ctx => ctx.Tank.Health.IsDead));

            // ── Attack transitions ──
            attack.AddTransition(new FSMTransition<TankContext>(
                investigate,
                ctx => ctx.DistanceToPlayer() > ctx.Tank.FollowRadius));

            attack.AddTransition(new FSMTransition<TankContext>(
                idle,
                ctx => ctx.PlayerIsDead));

            attack.AddTransition(new FSMTransition<TankContext>(
                dead,
                ctx => ctx.Tank.Health.IsDead));

            _fsm.SetInitialState(idle, _context);
        }

        public void Update(Vector2 playerPosition, bool playerIsDead, GameTime gameTime)
        {
            _context.PlayerPosition = playerPosition;
            _context.PlayerIsDead = playerIsDead;
            _fsm.Update(_context, gameTime);
        }

        // ────────────────────────────────────────
        //  States
        // ────────────────────────────────────────

        /// <summary>
        /// Player is far away. Tank sits still, no spawning,
        /// agents drift lazily around the tank.
        /// </summary>
        private sealed class IdleState : FSMState<TankContext>
        {
            public override void OnEnter(TankContext ctx)
            {
                ctx.ShouldSpawnAgents = true;
                ctx.ShouldFlockAttackPlayer = false;
                ctx.FlockSpeed = 20f;
                ctx.TankMoveSpeed = 0f;
                ctx.ApplyPlayerForce = false;
                ctx.PlayerForceRadius = 0f;
                ctx.PlayerForceStrength = 0f;
            }

            public override void OnUpdate(TankContext ctx, GameTime gameTime)
            {
                ctx.Tank.CurrentSpeed = 0f;
            }
        }

        /// <summary>
        /// Player entered detection range. Tank begins moving,
        /// agents spawn and flock at moderate speed.
        /// </summary>
        private sealed class InvestigateState : FSMState<TankContext>
        {
            public override void OnEnter(TankContext ctx)
            {
                ctx.ShouldSpawnAgents = true;
                ctx.ShouldFlockAttackPlayer = false;
                ctx.FlockSpeed = 60f;
                ctx.TankMoveSpeed = 40f;
                ctx.ApplyPlayerForce = false;
                ctx.PlayerForceRadius = 0f;
                ctx.PlayerForceStrength = 0f;
            }

            public override void OnUpdate(TankContext ctx, GameTime gameTime)
            {
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                ctx.Tank.MoveToward(ctx.PlayerPosition, dt, ctx.TankMoveSpeed);
            }
        }

        /// <summary>
        /// Player is close. Agents actively chase and repel off
        /// the player; tank pushes in at full speed.
        /// </summary>
        private sealed class AttackState : FSMState<TankContext>
        {
            public override void OnEnter(TankContext ctx)
            {
                ctx.ShouldSpawnAgents = true;
                ctx.ShouldFlockAttackPlayer = true;
                ctx.FlockSpeed = 90f;
                ctx.TankMoveSpeed = 60f;
                ctx.ApplyPlayerForce = true;
                ctx.PlayerForceRadius = 300f;
                ctx.PlayerForceStrength = 12f;
            }

            public override void OnUpdate(TankContext ctx, GameTime gameTime)
            {
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                ctx.Tank.MoveToward(ctx.PlayerPosition, dt, ctx.TankMoveSpeed);
            }
        }

        /// <summary>
        /// Tank is dead. Everything stops.
        /// </summary>
        private sealed class DeadState : FSMState<TankContext>
        {
            public override void OnEnter(TankContext ctx)
            {
                ctx.ShouldSpawnAgents = false;
                ctx.ShouldFlockAttackPlayer = false;
                ctx.FlockSpeed = 0f;
                ctx.TankMoveSpeed = 0f;
                ctx.ApplyPlayerForce = false;

                if (!ctx.Tank.IsDead)
                    ctx.Tank.ApplyDeath();
            }
        }
    }
}
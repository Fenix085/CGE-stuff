using MainEngine.FSM;
using Microsoft.Xna.Framework;

namespace LILITH.Core.Enemies.Shooter
{
    public sealed class ShooterContext
    {
        public Shooter Shooter { get; }
        public Vector2 PlayerPosition { get; set; }
        public bool PlayerIsDead { get; set; }
        public float RepathTimer { get; set; }

        public float MoveSpeed { get; set; }
        public bool ShouldShoot { get; set; }

        /// <summary>
        /// If the player gets closer than this, the shooter retreats.
        /// </summary>
        public float RetreatRadius { get; set; } = 120f;

        public ShooterContext(Shooter shooter) { Shooter = shooter; }

        public float DistanceToPlayer() =>
            Vector2.Distance(Shooter.Position, PlayerPosition);
    }

    public sealed class ShooterFSM
    {
        private readonly ShooterContext _context;
        private readonly FiniteStateMachine<ShooterContext> _fsm = new();

        public bool ShouldShoot => _context.ShouldShoot;

        public ShooterFSM(Shooter shooter)
        {
            _context = new ShooterContext(shooter);

            var idle = new IdleState();
            var investigate = new InvestigateState();
            var attack = new AttackState();
            var dead = new DeadState();

            idle.AddTransition(new FSMTransition<ShooterContext>(
                investigate,
                ctx => !ctx.PlayerIsDead
                       && ctx.DistanceToPlayer() <= ctx.Shooter.DetectionRadius));
            idle.AddTransition(new FSMTransition<ShooterContext>(
                dead, ctx => ctx.Shooter.Health.IsDead));

            investigate.AddTransition(new FSMTransition<ShooterContext>(
                attack,
                ctx => !ctx.PlayerIsDead
                       && ctx.DistanceToPlayer() <= ctx.Shooter.FollowRadius));
            investigate.AddTransition(new FSMTransition<ShooterContext>(
                idle,
                ctx => ctx.PlayerIsDead
                       || ctx.DistanceToPlayer() > ctx.Shooter.DetectionRadius));
            investigate.AddTransition(new FSMTransition<ShooterContext>(
                dead, ctx => ctx.Shooter.Health.IsDead));

            attack.AddTransition(new FSMTransition<ShooterContext>(
                investigate,
                ctx => ctx.DistanceToPlayer() > ctx.Shooter.FollowRadius));
            attack.AddTransition(new FSMTransition<ShooterContext>(
                idle, ctx => ctx.PlayerIsDead));
            attack.AddTransition(new FSMTransition<ShooterContext>(
                dead, ctx => ctx.Shooter.Health.IsDead));

            _fsm.SetInitialState(idle, _context);
        }

        public void Update(Vector2 playerPosition, bool playerIsDead, GameTime gameTime)
        {
            _context.PlayerPosition = playerPosition;
            _context.PlayerIsDead = playerIsDead;
            _fsm.Update(_context, gameTime);
        }

        private sealed class IdleState : FSMState<ShooterContext>
        {
            public override void OnEnter(ShooterContext ctx)
            {
                ctx.MoveSpeed = 0f;
                ctx.ShouldShoot = false;
                ctx.Shooter.CanShoot = false;
            }

            public override void OnUpdate(ShooterContext ctx, GameTime gameTime)
            {
                ctx.Shooter.CurrentSpeed = 0f;
            }
        }

        /// <summary>
        /// Navigate toward the player until within firing range.
        /// </summary>
        private sealed class InvestigateState : FSMState<ShooterContext>
        {
            public override void OnEnter(ShooterContext ctx)
            {
                ctx.MoveSpeed = 55f;
                ctx.ShouldShoot = false;
                ctx.Shooter.CanShoot = false;
                ctx.RepathTimer = 0f;
            }

            public override void OnUpdate(ShooterContext ctx, GameTime gameTime)
            {
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                float timer = ctx.RepathTimer;

                EnemyNavigation.NavigateOrPursue(
                    ctx.Shooter, ctx.Shooter.NavFollower,
                    ctx.PlayerPosition, dt, ctx.MoveSpeed,
                    directPursuitRadius: ctx.Shooter.FollowRadius,
                    repathTimer: ref timer);

                ctx.RepathTimer = timer;
            }
        }

        /// <summary>
        /// In range — shoot. If the player rushes in too close, back away.
        /// </summary>
        private sealed class AttackState : FSMState<ShooterContext>
        {
            public override void OnEnter(ShooterContext ctx)
            {
                ctx.MoveSpeed = 40f;
                ctx.ShouldShoot = true;
                ctx.Shooter.CanShoot = true;
                ctx.Shooter.NavFollower?.Clear();
            }

            public override void OnUpdate(ShooterContext ctx, GameTime gameTime)
            {
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                float dist = ctx.DistanceToPlayer();

                if (dist < ctx.RetreatRadius && dist > 0.001f)
                {
                    // Back away from the player
                    Vector2 away = ctx.Shooter.Position - ctx.PlayerPosition;
                    away /= dist;
                    ctx.Shooter.Position += away * ctx.MoveSpeed * dt;
                }
                // Otherwise hold position and keep shooting
            }
        }

        private sealed class DeadState : FSMState<ShooterContext>
        {
            public override void OnEnter(ShooterContext ctx)
            {
                ctx.MoveSpeed = 0f;
                ctx.ShouldShoot = false;
                ctx.Shooter.CanShoot = false;
                if (!ctx.Shooter.IsDead) ctx.Shooter.ApplyDeath();
            }
        }
    }
}
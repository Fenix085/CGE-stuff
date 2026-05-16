using MainEngine.FSM;
using Microsoft.Xna.Framework;

namespace LILITH.Core.Enemies.Walker
{
    public sealed class WalkerContext
    {
        public Walker Walker { get; }
        public Vector2 PlayerPosition { get; set; }
        public bool PlayerIsDead { get; set; }
        public float RepathTimer { get; set; }

        public float MoveSpeed { get; set; }
        public bool IsAttacking { get; set; }

        public WalkerContext(Walker walker) { Walker = walker; }

        public float DistanceToPlayer() =>
            Vector2.Distance(Walker.Position, PlayerPosition);
    }

    public sealed class WalkerFSM
    {
        private readonly WalkerContext _context;
        private readonly FiniteStateMachine<WalkerContext> _fsm = new();

        public float MoveSpeed => _context.MoveSpeed;
        public bool IsAttacking => _context.IsAttacking;

        public WalkerFSM(Walker walker)
        {
            _context = new WalkerContext(walker);

            var idle = new IdleState();
            var investigate = new InvestigateState();
            var attack = new AttackState();
            var dead = new DeadState();

            idle.AddTransition(new FSMTransition<WalkerContext>(
                investigate,
                ctx => !ctx.PlayerIsDead
                       && ctx.DistanceToPlayer() <= ctx.Walker.DetectionRadius));
            idle.AddTransition(new FSMTransition<WalkerContext>(
                dead, ctx => ctx.Walker.Health.IsDead));

            investigate.AddTransition(new FSMTransition<WalkerContext>(
                attack,
                ctx => !ctx.PlayerIsDead
                       && ctx.DistanceToPlayer() <= ctx.Walker.FollowRadius));
            investigate.AddTransition(new FSMTransition<WalkerContext>(
                idle,
                ctx => ctx.PlayerIsDead
                       || ctx.DistanceToPlayer() > ctx.Walker.DetectionRadius));
            investigate.AddTransition(new FSMTransition<WalkerContext>(
                dead, ctx => ctx.Walker.Health.IsDead));

            attack.AddTransition(new FSMTransition<WalkerContext>(
                investigate,
                ctx => ctx.DistanceToPlayer() > ctx.Walker.FollowRadius));
            attack.AddTransition(new FSMTransition<WalkerContext>(
                idle, ctx => ctx.PlayerIsDead));
            attack.AddTransition(new FSMTransition<WalkerContext>(
                dead, ctx => ctx.Walker.Health.IsDead));

            _fsm.SetInitialState(idle, _context);
        }

        public void Update(Vector2 playerPosition, bool playerIsDead, GameTime gameTime)
        {
            _context.PlayerPosition = playerPosition;
            _context.PlayerIsDead = playerIsDead;
            _fsm.Update(_context, gameTime);
        }

        private sealed class IdleState : FSMState<WalkerContext>
        {
            public override void OnEnter(WalkerContext ctx)
            {
                ctx.MoveSpeed = 0f;
                ctx.IsAttacking = false;
            }

            public override void OnUpdate(WalkerContext ctx, GameTime gameTime)
            {
                ctx.Walker.CurrentSpeed = 0f;
            }
        }

        private sealed class InvestigateState : FSMState<WalkerContext>
        {
            public override void OnEnter(WalkerContext ctx)
            {
                ctx.MoveSpeed = 70f;
                ctx.IsAttacking = false;
                ctx.RepathTimer = 0f;
            }

            public override void OnUpdate(WalkerContext ctx, GameTime gameTime)
            {
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                float timer = ctx.RepathTimer;

                EnemyNavigation.NavigateOrPursue(
                    ctx.Walker, ctx.Walker.NavFollower,
                    ctx.PlayerPosition, dt, ctx.MoveSpeed,
                    directPursuitRadius: ctx.Walker.FollowRadius,
                    repathTimer: ref timer);

                ctx.RepathTimer = timer;
            }
        }

        private sealed class AttackState : FSMState<WalkerContext>
        {
            public override void OnEnter(WalkerContext ctx)
            {
                ctx.MoveSpeed = 90f;
                ctx.IsAttacking = true;
                ctx.Walker.NavFollower?.Clear();
            }

            public override void OnUpdate(WalkerContext ctx, GameTime gameTime)
            {
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                ctx.Walker.MoveToward(ctx.PlayerPosition, dt, ctx.MoveSpeed);
            }
        }

        private sealed class DeadState : FSMState<WalkerContext>
        {
            public override void OnEnter(WalkerContext ctx)
            {
                ctx.MoveSpeed = 0f;
                ctx.IsAttacking = false;
                if (!ctx.Walker.IsDead) ctx.Walker.ApplyDeath();
            }
        }
    }
}
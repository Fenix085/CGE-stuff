using MainEngine.FSM;
using Microsoft.Xna.Framework;

namespace LILITH.Core.Enemies.Runner
{
    public sealed class RunnerContext
    {
        public Runner Runner { get; }
        public Vector2 PlayerPosition { get; set; }
        public bool PlayerIsDead { get; set; }
        public float RepathTimer { get; set; }

        public float MoveSpeed { get; set; }
        public bool IsAttacking { get; set; }

        public RunnerContext(Runner runner) { Runner = runner; }

        public float DistanceToPlayer() =>
            Vector2.Distance(Runner.Position, PlayerPosition);
    }

    public sealed class RunnerFSM
    {
        private readonly RunnerContext _context;
        private readonly FiniteStateMachine<RunnerContext> _fsm = new();

        public float MoveSpeed => _context.MoveSpeed;
        public bool IsAttacking => _context.IsAttacking;

        public RunnerFSM(Runner runner)
        {
            _context = new RunnerContext(runner);

            var idle = new IdleState();
            var investigate = new InvestigateState();
            var attack = new AttackState();
            var dead = new DeadState();

            idle.AddTransition(new FSMTransition<RunnerContext>(
                investigate,
                ctx => !ctx.PlayerIsDead
                       && ctx.DistanceToPlayer() <= ctx.Runner.DetectionRadius));
            idle.AddTransition(new FSMTransition<RunnerContext>(
                dead, ctx => ctx.Runner.Health.IsDead));

            investigate.AddTransition(new FSMTransition<RunnerContext>(
                attack,
                ctx => !ctx.PlayerIsDead
                       && ctx.DistanceToPlayer() <= ctx.Runner.FollowRadius));
            investigate.AddTransition(new FSMTransition<RunnerContext>(
                idle,
                ctx => ctx.PlayerIsDead
                       || ctx.DistanceToPlayer() > ctx.Runner.DetectionRadius));
            investigate.AddTransition(new FSMTransition<RunnerContext>(
                dead, ctx => ctx.Runner.Health.IsDead));

            attack.AddTransition(new FSMTransition<RunnerContext>(
                investigate,
                ctx => ctx.DistanceToPlayer() > ctx.Runner.FollowRadius));
            attack.AddTransition(new FSMTransition<RunnerContext>(
                idle, ctx => ctx.PlayerIsDead));
            attack.AddTransition(new FSMTransition<RunnerContext>(
                dead, ctx => ctx.Runner.Health.IsDead));

            _fsm.SetInitialState(idle, _context);
        }

        public void Update(Vector2 playerPosition, bool playerIsDead, GameTime gameTime)
        {
            _context.PlayerPosition = playerPosition;
            _context.PlayerIsDead = playerIsDead;
            _fsm.Update(_context, gameTime);
        }

        private sealed class IdleState : FSMState<RunnerContext>
        {
            public override void OnEnter(RunnerContext ctx)
            {
                ctx.MoveSpeed = 0f;
                ctx.IsAttacking = false;
            }

            public override void OnUpdate(RunnerContext ctx, GameTime gameTime)
            {
                ctx.Runner.CurrentSpeed = 0f;
            }
        }

        /// <summary>
        /// Runners are fast even while investigating.
        /// </summary>
        private sealed class InvestigateState : FSMState<RunnerContext>
        {
            public override void OnEnter(RunnerContext ctx)
            {
                ctx.MoveSpeed = 120f;
                ctx.IsAttacking = false;
                ctx.RepathTimer = 0f;
            }

            public override void OnUpdate(RunnerContext ctx, GameTime gameTime)
            {
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                float timer = ctx.RepathTimer;

                EnemyNavigation.NavigateOrPursue(
                    ctx.Runner, ctx.Runner.NavFollower,
                    ctx.PlayerPosition, dt, ctx.MoveSpeed,
                    directPursuitRadius: ctx.Runner.FollowRadius,
                    repathTimer: ref timer);

                ctx.RepathTimer = timer;
            }
        }

        /// <summary>
        /// Sprint at the player.
        /// </summary>
        private sealed class AttackState : FSMState<RunnerContext>
        {
            public override void OnEnter(RunnerContext ctx)
            {
                ctx.MoveSpeed = 160f;
                ctx.IsAttacking = true;
                ctx.Runner.NavFollower?.Clear();
            }

            public override void OnUpdate(RunnerContext ctx, GameTime gameTime)
            {
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                ctx.Runner.MoveToward(ctx.PlayerPosition, dt, ctx.MoveSpeed);
            }
        }

        private sealed class DeadState : FSMState<RunnerContext>
        {
            public override void OnEnter(RunnerContext ctx)
            {
                ctx.MoveSpeed = 0f;
                ctx.IsAttacking = false;
                if (!ctx.Runner.IsDead) ctx.Runner.ApplyDeath();
            }
        }
    }
}
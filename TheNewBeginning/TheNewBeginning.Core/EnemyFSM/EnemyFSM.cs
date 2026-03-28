using MainEngine.FSM;
using MainEngine.Entities;
using Microsoft.Xna.Framework;

// STATES: [Idle/Patrol] (see player)->   <-(player dead) [Attack] <-(see player) [Investigate]
//               (alerted)--------------------------------------------------------------^

namespace TheNewBeginning.Core.EnemyFSM
{
    public sealed class EnemyContext
    {
        public Enemy Enemy { get; }
        public Vector2 PlayerPosition { get; set; }
        public bool PlayerIsDead { get; set; }
        public bool ShouldFlockAttackPlayer { get; set; }
        public float FlockSpeed { get; set; }
        
        public EnemyContext(Enemy enemy)
        {
            Enemy = enemy;
        }

        public float DistanceToPlayer() => Vector2.Distance(Enemy.Position, PlayerPosition);
    }

    public sealed class EnemyFSM
    {
        private readonly EnemyContext _context;
        private readonly FiniteStateMachine<EnemyContext> _fsm = new();

        public bool ShouldFlockAttackPlayer => _context.ShouldFlockAttackPlayer;
        public float FlockSpeed => _context.FlockSpeed;

        public EnemyFSM(Enemy enemy)
        {
            _context = new EnemyContext(enemy);

            var idle = new IdleState();
            var attack = new AttackState();
            var investigate = new InvestigateState();
            var dead = new DeadState();

            idle.AddTransition(new FSMTransition<EnemyContext>(
                investigate,
                context => !context.PlayerIsDead && context.DistanceToPlayer() <= context.Enemy.DetectionRadius
            ));

            idle.AddTransition(new FSMTransition<EnemyContext>(
                dead,
                context => context.Enemy.Health.IsDead
            ));

            investigate.AddTransition(new FSMTransition<EnemyContext>(
                attack,
                context => !context.PlayerIsDead && context.DistanceToPlayer() <= context.Enemy.FollowRadius
            ));

            investigate.AddTransition(new FSMTransition<EnemyContext>(
                idle,
                context => context.PlayerIsDead || context.DistanceToPlayer() > context.Enemy.DetectionRadius
            ));

            investigate.AddTransition(new FSMTransition<EnemyContext>(
                dead,
                context => context.Enemy.Health.IsDead
            ));

            attack.AddTransition(new FSMTransition<EnemyContext>(
                investigate,
                context => context.DistanceToPlayer() > context.Enemy.FollowRadius
            ));

            attack.AddTransition(new FSMTransition<EnemyContext>(
                idle,
                context => context.PlayerIsDead
            ));

            attack.AddTransition(new FSMTransition<EnemyContext>(
                dead,
                context => context.Enemy.Health.IsDead
            ));

            _fsm.SetInitialState(idle, _context);
        }

        public void Update(Vector2 playerPosition, bool playerIsDead, GameTime gameTime)
        {
            _context.PlayerPosition = playerPosition;
            _context.PlayerIsDead = playerIsDead;
            _fsm.Update(_context, gameTime);
        }

        private sealed class IdleState : FSMState<EnemyContext>
        {
            public override void OnEnter(EnemyContext context)
            {
                context.ShouldFlockAttackPlayer = false;
                context.FlockSpeed = 20f;
            }

            public override void OnUpdate(EnemyContext context, GameTime gameTime)
            {
                context.Enemy.CurrentSpeed = 0f;
            }
        }

        private sealed class AttackState : FSMState<EnemyContext>
        {
            public override void OnEnter(EnemyContext context)
            {
                context.ShouldFlockAttackPlayer = true;
                context.FlockSpeed = 110f;
            }

            public override void OnUpdate(EnemyContext context, GameTime gameTime)
            {
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                context.Enemy.MoveToward(context.PlayerPosition, dt, 90f);
            }
        }

        private sealed class InvestigateState : FSMState<EnemyContext>
        {
            public override void OnEnter(EnemyContext context)
            {
                context.ShouldFlockAttackPlayer = true;
                context.FlockSpeed = 70f;
            }

            public override void OnUpdate(EnemyContext context, GameTime gameTime)
            {
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                context.Enemy.MoveToward(context.PlayerPosition, dt, 60f);
            }
        }

        private sealed class DeadState : FSMState<EnemyContext>
        {
            public override void OnEnter(EnemyContext context)
            {
                context.ShouldFlockAttackPlayer = false;
                context.FlockSpeed = 0f;
                if (!context.Enemy.IsDead)
                    context.Enemy.ApplyDeath();
            }
        }
    }
}
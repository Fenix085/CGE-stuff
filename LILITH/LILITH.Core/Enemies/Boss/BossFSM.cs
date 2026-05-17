using System;
using System.Collections.Generic;
using MainEngine;
using MainEngine.FlockEnemy;
using MainEngine.FSM;
using Microsoft.Xna.Framework;

namespace LILITH.Core.Enemies.Boss
{
    public sealed class BossContext
    {
        public Boss Boss { get; }
        public Vector2 PlayerPosition { get; set; }
        public bool PlayerIsDead { get; set; }
        public Circle PlayerBounds { get; set; }

        // Outputs the scene reads after each Update
        public float MoveSpeed { get; set; }
        public bool IsAttacking { get; set; }
        public List<ForceSource> ActiveForceSources { get; } = new();

        /// <summary>
        /// Warning circles the scene should draw this frame (telegraphs).
        /// Cleared each frame by ClearFrame().
        /// </summary>
        public List<Circle> WarningZones { get; } = new();

        // Used by EngageState to tell the FSM which attack was chosen
        internal int ChosenAttack { get; set; } = -1;

        // Accumulated damage dealt this frame
        public int PendingPlayerDamage { get; private set; }

        public BossContext(Boss boss)
        {
            Boss = boss;
        }

        public float DistanceToPlayer()
            => Vector2.Distance(Boss.Position, PlayerPosition);

        public void DamagePlayer(int amount)
            => PendingPlayerDamage += amount;

        public void ClearFrame()
        {
            PendingPlayerDamage = 0;
            ActiveForceSources.Clear();
            WarningZones.Clear();
        }
    }

    public sealed class BossFSM
    {
        private readonly BossContext _context;
        private readonly FiniteStateMachine<BossContext> _fsm = new();

        public float MoveSpeed => _context.MoveSpeed;
        public bool IsAttacking => _context.IsAttacking;
        public BossContext Context => _context;

        public BossFSM(Boss boss)
        {
            _context = new BossContext(boss);

            var idle = new IdleState();
            var engage = new EngageState();
            var bigExplosion = new BigExplosionState();
            var lineExplosion = new LineExplosionState();
            var dead = new DeadState();

            // --- Idle ---
            idle.AddTransition(new FSMTransition<BossContext>(
                engage,
                ctx => !ctx.PlayerIsDead
                       && ctx.DistanceToPlayer() <= ctx.Boss.DetectionRadius));
            idle.AddTransition(new FSMTransition<BossContext>(
                dead,
                ctx => ctx.Boss.Health.IsDead));

            // --- Engage (picks next attack after a cooldown) ---
            engage.AddTransition(new FSMTransition<BossContext>(
                bigExplosion,
                ctx => ctx.ChosenAttack == 0));
            engage.AddTransition(new FSMTransition<BossContext>(
                lineExplosion,
                ctx => ctx.ChosenAttack == 1));
            engage.AddTransition(new FSMTransition<BossContext>(
                idle,
                ctx => ctx.PlayerIsDead
                       || ctx.DistanceToPlayer() > ctx.Boss.DetectionRadius));
            engage.AddTransition(new FSMTransition<BossContext>(
                dead,
                ctx => ctx.Boss.Health.IsDead));

            // --- Big Explosion → back to Engage ---
            bigExplosion.AddTransition(new FSMTransition<BossContext>(
                engage,
                _ => bigExplosion.IsFinished));
            bigExplosion.AddTransition(new FSMTransition<BossContext>(
                dead,
                ctx => ctx.Boss.Health.IsDead));

            // --- Line Explosion → back to Engage ---
            lineExplosion.AddTransition(new FSMTransition<BossContext>(
                engage,
                _ => lineExplosion.IsFinished));
            lineExplosion.AddTransition(new FSMTransition<BossContext>(
                dead,
                ctx => ctx.Boss.Health.IsDead));

            _fsm.SetInitialState(idle, _context);
        }

        public void Update(Vector2 playerPosition, Circle playerBounds, bool playerIsDead, GameTime gameTime)
        {
            _context.PlayerPosition = playerPosition;
            _context.PlayerBounds = playerBounds;
            _context.PlayerIsDead = playerIsDead;
            _context.ClearFrame();
            _fsm.Update(_context, gameTime);
        }
    }

    // ----------------------------------------------------------------
    //  States
    // ----------------------------------------------------------------

    internal sealed class IdleState : FSMState<BossContext>
    {
        public override void OnEnter(BossContext ctx)
        {
            ctx.MoveSpeed = 0f;
            ctx.IsAttacking = false;
            ctx.ChosenAttack = -1;
        }
    }

    internal sealed class EngageState : FSMState<BossContext>
    {
        private const float AttackCooldown = 1.2f;
        private const int AttackCount = 2; // 0 = big explosion, 1 = line
        private float _cooldownTimer;

        public override void OnEnter(BossContext ctx)
        {
            ctx.IsAttacking = false;
            ctx.ChosenAttack = -1;
            _cooldownTimer = AttackCooldown;
        }

        public override void OnUpdate(BossContext ctx, GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            ctx.MoveSpeed = 60f;
            ctx.Boss.MoveToward(ctx.PlayerPosition, dt, ctx.MoveSpeed);

            _cooldownTimer -= dt;
            if (_cooldownTimer <= 0f)
                ctx.ChosenAttack = Random.Shared.Next(0, AttackCount);
        }

        public override void OnExit(BossContext ctx)
        {
            ctx.ChosenAttack = -1;
        }
    }

    // ----------------------------------------------------------------
    //  Big Explosion — targeted at the player's current position
    //
    //  1. Locks onto where the player is standing when the attack begins
    //  2. Shows a growing warning circle at THAT spot for 1 second
    //  3. Detonates — damages if the player didn't leave the area
    // ----------------------------------------------------------------

    internal sealed class BigExplosionState : FSMState<BossContext>
    {
        private const float TelegraphTime = 1.0f;
        private const float ExplosionRadius = 120f;
        private const float DamageRadius = 110f;
        private const float TotalDuration = 1.3f;
        private const int Damage = 3;

        private float _elapsed;
        private bool _hasDamaged;
        private Vector2 _targetPos;

        public bool IsFinished => _elapsed >= TotalDuration;

        public override void OnEnter(BossContext ctx)
        {
            _elapsed = 0f;
            _hasDamaged = false;
            ctx.IsAttacking = true;
            ctx.MoveSpeed = 0f;

            // Lock onto where the player is right now
            _targetPos = ctx.PlayerPosition;
        }

        public override void OnUpdate(BossContext ctx, GameTime gameTime)
        {
            _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Growing warning circle at the target position
            if (_elapsed < TelegraphTime)
            {
                float progress = _elapsed / TelegraphTime;
                ctx.WarningZones.Add(new Circle(
                    (int)_targetPos.X,
                    (int)_targetPos.Y,
                    (int)(ExplosionRadius * progress)));
            }

            // Detonation
            if (!_hasDamaged && _elapsed >= TelegraphTime)
            {
                _hasDamaged = true;

                Circle blast = new Circle(
                    (int)_targetPos.X,
                    (int)_targetPos.Y,
                    (int)DamageRadius);

                if (blast.Intersects(ctx.PlayerBounds))
                    ctx.DamagePlayer(Damage);

                ctx.ActiveForceSources.Add(
                    new ForceSource(_targetPos, ExplosionRadius, -50f));
            }

            // Brief flash after detonation
            if (_hasDamaged && _elapsed < TotalDuration)
            {
                ctx.WarningZones.Add(new Circle(
                    (int)_targetPos.X,
                    (int)_targetPos.Y,
                    (int)ExplosionRadius));
            }
        }

        public override void OnExit(BossContext ctx)
        {
            ctx.IsAttacking = false;
        }
    }

    // ----------------------------------------------------------------
    //  Line Explosion — 6 small blasts cascading from boss → player
    //
    //  On enter: calculates 6 positions evenly spaced along the
    //  straight line from the boss to the player's current position.
    //  Each explosion telegraphs briefly, then detonates in sequence,
    //  creating a ripple effect the player must sidestep.
    // ----------------------------------------------------------------

    internal sealed class LineExplosionState : FSMState<BossContext>
    {
        private const int Count = 6;
        private const float StaggerDelay = 0.15f;  // gap between each explosion starting
        private const float TelegraphTime = 0.3f;   // warning visible before each detonation
        private const float SmallRadius = 45f;
        private const float SmallDamageRadius = 40f;
        private const int Damage = 1;

        private float _elapsed;
        private readonly Vector2[] _positions = new Vector2[Count];
        private readonly bool[] _hasDamaged = new bool[Count];

        private const float TotalDuration =
            (Count - 1) * StaggerDelay + TelegraphTime + 0.2f;

        public bool IsFinished => _elapsed >= TotalDuration;

        public override void OnEnter(BossContext ctx)
        {
            _elapsed = 0f;
            Array.Clear(_hasDamaged);

            ctx.IsAttacking = true;
            ctx.MoveSpeed = 0f;

            Vector2 start = ctx.Boss.Position;
            Vector2 end = ctx.PlayerPosition;

            for (int i = 0; i < Count; i++)
            {
                float t = Count > 1 ? (float)i / (Count - 1) : 0f;
                _positions[i] = Vector2.Lerp(start, end, t);
            }
        }

        public override void OnUpdate(BossContext ctx, GameTime gameTime)
        {
            _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

            for (int i = 0; i < Count; i++)
            {
                float activateAt = i * StaggerDelay;
                float detonateAt = activateAt + TelegraphTime;

                // Not started yet
                if (_elapsed < activateAt)
                    continue;

                // Telegraph phase — growing warning circle
                if (_elapsed < detonateAt)
                {
                    float progress = (_elapsed - activateAt) / TelegraphTime;
                    ctx.WarningZones.Add(new Circle(
                        (int)_positions[i].X,
                        (int)_positions[i].Y,
                        (int)(SmallRadius * progress)));
                    continue;
                }

                // Detonation
                if (!_hasDamaged[i])
                {
                    _hasDamaged[i] = true;

                    Circle blast = new Circle(
                        (int)_positions[i].X,
                        (int)_positions[i].Y,
                        (int)SmallDamageRadius);

                    if (blast.Intersects(ctx.PlayerBounds))
                        ctx.DamagePlayer(Damage);

                    ctx.ActiveForceSources.Add(
                        new ForceSource(_positions[i], SmallRadius, -15f));
                }

                // Brief flash after detonation
                float flashEnd = detonateAt + 0.15f;
                if (_elapsed < flashEnd)
                {
                    ctx.WarningZones.Add(new Circle(
                        (int)_positions[i].X,
                        (int)_positions[i].Y,
                        (int)SmallRadius));
                }
            }
        }

        public override void OnExit(BossContext ctx)
        {
            ctx.IsAttacking = false;
        }
    }

    internal sealed class DeadState : FSMState<BossContext>
    {
        public override void OnEnter(BossContext ctx)
        {
            ctx.MoveSpeed = 0f;
            ctx.IsAttacking = false;
            if (!ctx.Boss.IsDead)
                ctx.Boss.ApplyDeath();
        }
    }
}
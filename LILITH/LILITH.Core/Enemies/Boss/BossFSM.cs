using System;
using System.Collections.Generic;
using MainEngine;
using MainEngine.FlockEnemy;
using MainEngine.FSM;
using MainEngine.Graphics;
using MainEngine.Projectile;
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
        public List<Projectile> BossProjectiles { get; } = new();

        // Asset the wall attack stamps onto spawned projectiles
        public TextureRegion WallProjectileRegion { get; set; }

        // Used by EngageState to tell the FSM which attack was chosen
        internal int ChosenAttack { get; set; } = -1;

        // Accumulated damage dealt this frame — scene reads then clears
        public int PendingPlayerDamage { get; private set; }

        public BossContext(Boss boss)
        {
            Boss = boss;
        }

        public float DistanceToPlayer()
            => Vector2.Distance(Boss.Position, PlayerPosition);

        public void DamagePlayer(int amount)
            => PendingPlayerDamage += amount;

        /// <summary>
        /// Call once per frame after reading outputs.
        /// </summary>
        public void ClearFrame()
        {
            PendingPlayerDamage = 0;
            ActiveForceSources.Clear();
        }
    }

    public sealed class BossFSM
    {
        private readonly BossContext _context;
        private readonly FiniteStateMachine<BossContext> _fsm = new();

        // Expose the explosion state so the scene can read WarningZone for drawing
        private readonly BigExplosionState _bigExplosion;

        public float MoveSpeed => _context.MoveSpeed;
        public bool IsAttacking => _context.IsAttacking;
        public BossContext Context => _context;

        /// <summary>
        /// If the boss is currently charging a big explosion, returns the
        /// warning circle; otherwise null.
        /// </summary>
        public Circle? ActiveWarningZone =>
            _fsm.ActiveState == _bigExplosion && !_bigExplosion.IsFinished
                ? _bigExplosion.WarningZone(_context.Boss.Position)
                : null;

        public BossFSM(Boss boss)
        {
            _context = new BossContext(boss);

            var idle = new IdleState();
            var engage = new EngageState();
            _bigExplosion = new BigExplosionState();
            var explosionWall = new ExplosionWallState();
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
                _bigExplosion,
                ctx => ctx.ChosenAttack == 0));
            engage.AddTransition(new FSMTransition<BossContext>(
                explosionWall,
                ctx => ctx.ChosenAttack == 1));
            engage.AddTransition(new FSMTransition<BossContext>(
                idle,
                ctx => ctx.PlayerIsDead
                       || ctx.DistanceToPlayer() > ctx.Boss.DetectionRadius));
            engage.AddTransition(new FSMTransition<BossContext>(
                dead,
                ctx => ctx.Boss.Health.IsDead));

            // --- Big Explosion → back to Engage ---
            _bigExplosion.AddTransition(new FSMTransition<BossContext>(
                engage,
                _ => _bigExplosion.IsFinished));
            _bigExplosion.AddTransition(new FSMTransition<BossContext>(
                dead,
                ctx => ctx.Boss.Health.IsDead));

            // --- Explosion Wall → back to Engage ---
            explosionWall.AddTransition(new FSMTransition<BossContext>(
                engage,
                _ => explosionWall.IsFinished));
            explosionWall.AddTransition(new FSMTransition<BossContext>(
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
        private const int AttackCount = 2; // 0 = big explosion, 1 = wall
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

            // Slowly approach the player while deciding
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

    internal sealed class BigExplosionState : FSMState<BossContext>
    {
        private const float ChargeTime = 1.5f;
        private const float ExplosionRadius = 200f;
        private const float DamageRadius = 180f;
        private const float TotalDuration = 1.8f;

        private float _elapsed;
        private bool _hasDamaged;

        public bool IsFinished => _elapsed >= TotalDuration;

        /// <summary>
        /// Growing warning circle the scene can draw during the charge phase.
        /// </summary>
        public Circle WarningZone(Vector2 bossPos) => new Circle(
            (int)bossPos.X, (int)bossPos.Y,
            (int)(ExplosionRadius * MathHelper.Clamp(_elapsed / ChargeTime, 0f, 1f)));

        public override void OnEnter(BossContext ctx)
        {
            _elapsed = 0f;
            _hasDamaged = false;
            ctx.IsAttacking = true;
            ctx.MoveSpeed = 0f; // stand still while charging
        }

        public override void OnUpdate(BossContext ctx, GameTime gameTime)
        {
            _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (!_hasDamaged && _elapsed >= ChargeTime)
            {
                _hasDamaged = true;

                Circle blast = new Circle(
                    (int)ctx.Boss.Position.X,
                    (int)ctx.Boss.Position.Y,
                    (int)DamageRadius);

                if (blast.Intersects(ctx.PlayerBounds))
                    ctx.DamagePlayer(3);

                // Shockwave pushes nearby agents outward
                ctx.ActiveForceSources.Add(
                    new ForceSource(ctx.Boss.Position, ExplosionRadius, -50f));
            }
        }

        public override void OnExit(BossContext ctx)
        {
            ctx.IsAttacking = false;
        }
    }

    internal sealed class ExplosionWallState : FSMState<BossContext>
    {
        private const int WallSegments = 8;
        private const float Duration = 0.3f;

        private float _elapsed;

        public bool IsFinished => _elapsed >= Duration;

        public override void OnEnter(BossContext ctx)
        {
            _elapsed = 0f;
            ctx.IsAttacking = true;
            ctx.MoveSpeed = 0f;

            int gap = Random.Shared.Next(0, WallSegments);
            float spacing = 720f / WallSegments;

            Vector2 toPlayer = ctx.PlayerPosition - ctx.Boss.Position;
            Vector2 dir = toPlayer.LengthSquared() > 0.001f
                ? Vector2.Normalize(toPlayer)
                : Vector2.UnitX;

            for (int i = 0; i < WallSegments; i++)
            {
                if (i == gap) continue;

                Projectile proj = new Projectile
                {
                    Position = ctx.Boss.Position + new Vector2(0, i * spacing - 360f),
                    Direction = dir,
                    Speed = 300f
                };

                if (ctx.WallProjectileRegion != null)
                    proj.Region = ctx.WallProjectileRegion;

                ctx.BossProjectiles.Add(proj);
            }
        }

        public override void OnUpdate(BossContext ctx, GameTime gameTime)
        {
            _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
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
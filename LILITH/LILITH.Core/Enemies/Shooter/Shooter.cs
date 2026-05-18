using System;
using System.Collections.Generic;
using MainEngine.Entities;
using MainEngine.Graphics;
using MainEngine.Navigation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LILITH.Core.Enemies.Shooter
{
    public class Shooter : Enemy
    {
        public int Damage { get; set; } = 1;

        /// <summary>
        /// Seconds between shots.
        /// </summary>
        public float ShootCooldown { get; set; } = 1.5f;

        /// <summary>
        /// Set by the FSM each frame.
        /// </summary>
        public bool CanShoot { get; set; }

        /// <summary>
        /// Factory that creates a projectile given a spawn position and direction.
        /// Assigned by the scene so the Shooter doesn't need to know about textures.
        /// </summary>
        public Func<Vector2, Vector2, MainEngine.Projectile.Projectile> ProjectileFactory { get; set; }

        public NavigationFollower NavFollower { get; set; }

        private readonly ShooterFSM _fsm;
        private readonly List<MainEngine.Projectile.Projectile> _projectiles = new();
        public IReadOnlyList<MainEngine.Projectile.Projectile> Projectiles => _projectiles;

        private float _shootTimer;

        public Shooter(AnimatedSprite sprite, Vector2 position)
            : base(sprite, position, hp: 20)
        {
            DetectionRadius = 450f;
            FollowRadius = 300f;
            CurrentSpeed = 0f;
            _fsm = new ShooterFSM(this);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (_shootTimer > 0f)
                _shootTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        public void UpdateWithFSM(GameTime gameTime, Vector2 playerPosition, bool playerIsDead)
        {
            _fsm.Update(playerPosition, playerIsDead, gameTime);
            Update(gameTime);

            // Shoot when FSM allows
            if (CanShoot && _shootTimer <= 0f && ProjectileFactory != null && !IsDead)
            {
                _shootTimer = ShootCooldown;

                Vector2 direction = playerPosition - Position;
                float len = direction.Length();
                if (len > 0.001f)
                {
                    direction /= len;
                    var p = ProjectileFactory(Position, direction);
                    if (p != null)
                        _projectiles.Add(p);
                }
            }

            // Update and cull projectiles
            foreach (var p in _projectiles)
                p.Update(gameTime);

            _projectiles.RemoveAll(p => p.IsDead);
        }

        public void DrawWithProjectiles(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (!IsDead)
                Draw(gameTime, spriteBatch);

            foreach (var p in _projectiles)
                p.Draw(gameTime, spriteBatch);
        }
    }
}
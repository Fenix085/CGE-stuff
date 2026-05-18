using MainEngine.Entities;
using MainEngine.Graphics;
using MainEngine.Navigation;
using Microsoft.Xna.Framework;

namespace LILITH.Core.Enemies.Runner
{
    public class Runner : Enemy
    {
        public int Damage { get; set; } = 1;
        public float AttackRange { get; set; } = 25f;
        public float AttackCooldown { get; set; } = 0.6f;

        private float _attackTimer;
        private readonly RunnerFSM _fsm;

        public bool CanAttack => _attackTimer <= 0f;
        public bool IsAttacking => _fsm.IsAttacking;
        public NavigationFollower NavFollower { get; set; }

        public Runner(AnimatedSprite sprite, Vector2 position)
            : base(sprite, position, hp: 5)
        {
            DetectionRadius = 500f;
            FollowRadius = 500f;
            CurrentSpeed = 0f;
            _fsm = new RunnerFSM(this);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (_attackTimer > 0f)
                _attackTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        public void UpdateWithFSM(GameTime gameTime, Vector2 playerPosition, bool playerIsDead)
        {
            _fsm.Update(playerPosition, playerIsDead, gameTime);
            Update(gameTime);
        }

        public bool TryAttack()
        {
            if (_attackTimer > 0f) return false;
            _attackTimer = AttackCooldown;
            return true;
        }
    }
}
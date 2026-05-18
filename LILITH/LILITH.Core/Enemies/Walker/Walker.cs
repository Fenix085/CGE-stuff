using MainEngine.Entities;
using MainEngine.Graphics;
using MainEngine.Navigation;
using Microsoft.Xna.Framework;

namespace LILITH.Core.Enemies.Walker
{
    public class Walker : Enemy
    {
        public int Damage { get; set; } = 1;
        public float AttackRange { get; set; } = 30f;
        public float AttackCooldown { get; set; } = 1f;

        private float _attackTimer;
        private readonly WalkerFSM _fsm;

        public bool CanAttack => _attackTimer <= 0f;
        public bool IsAttacking => _fsm.IsAttacking;
        public NavigationFollower NavFollower { get; set; }

        public Walker(AnimatedSprite sprite, Vector2 position)
            : base(sprite, position, hp: 10)
        {
            DetectionRadius = 1000f;
            FollowRadius = 1000f;
            CurrentSpeed = 0f;
            _fsm = new WalkerFSM(this);
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
using MainEngine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MainEngine.Entities;

public class Enemy : Sprite
{
    public AnimatedSprite Sprite { get; private set; }
    public Health Health;
    public bool IsDead = false;
    public Vector2 Velocity { get; private set; }
    public float CurrentSpeed { get; set; } = 0f;
    public float DetectionRadius {get; set;} = 1000f;
    public float FollowRadius {get; set;} = 1000f;
    
    private float _hitFlashTimer = 0f;
    private const float HIT_FLASH_DURATION = 0.12f;

    public Enemy(AnimatedSprite sprite, Vector2 position, int hp)
    {
        Sprite = sprite;
        Position = position;
        Health = new Health(hp);
    }

    public void TriggerHitFlash()
    {
        _hitFlashTimer = HIT_FLASH_DURATION;
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_hitFlashTimer > 0f)
        {
            _hitFlashTimer -= dt;
        }
        Sprite.Update(gameTime);
    }

    public Circle Bounds => new Circle(
    (int)Position.X,
    (int)Position.Y,
    (int)(Sprite.Width * 0.4f)
);

    #region Moving

    public void MoveToward(Vector2 targetPosition, float dt, float activeSpeed)
    {
        Vector2 toTarget = targetPosition - Position;
        float distance = toTarget.Length();
        bool infiniteDetection = DetectionRadius < 0f;
        if (!infiniteDetection && distance > DetectionRadius)
        {
            CurrentSpeed = 0f;
            Velocity = Vector2.Zero;
            return;
        }

        if (distance <= 0.001f)
        {
            CurrentSpeed = 0f;
            Velocity = Vector2.Zero;
            return;
        }

        CurrentSpeed = activeSpeed;
        Vector2 direction = toTarget / distance;
        Velocity = direction * activeSpeed;
        Position += Velocity * dt;
    }

    #endregion

    public override void ApplyDeath()
    {
        IsDead = true;
    }

    public Circle GetBounds()
    {
        return IsDead ? Circle.Empty : Bounds;
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (Velocity.X < 0)
        {
            Sprite.Effects = SpriteEffects.FlipHorizontally;
        }
        else if (Velocity.X > 0)
            Sprite.Effects = SpriteEffects.None;

        Sprite.Color    = _hitFlashTimer > 0f ? Color.Red : Color.White;
        Sprite.Position = Position;
        Sprite.Draw(gameTime, spriteBatch);
    }
}
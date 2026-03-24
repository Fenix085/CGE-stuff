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
    public float CurrentSpeed { get; private set; } = 0f;
    public float DetectionRadius {get; set;} = 300f;
    public float FollowRadius {get; set;} = 500f;

    public Enemy(AnimatedSprite sprite, Vector2 position, int hp)
    {
        Sprite = sprite;
        Position = position;
        Health = new Health(hp);
    }

    public override void Update(GameTime gameTime)
    {
        Sprite.Update(gameTime);
    }

    public Circle Bounds => new Circle(
        (int)(Position.X + Sprite.Width * 0.1f),
        (int)(Position.Y + Sprite.Height * 0.1f),
        (int)(Sprite.Width * 0.1f)
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
        Sprite.Position = Position;
        Sprite.Draw(gameTime, spriteBatch);
    }
}
using MainEngine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MainEngine.Entities;

public class Enemy : Sprite
{
    public AnimatedSprite Sprite { get; private set; }
    public Health Health;
    public bool IsDead = false;
    public const float MOVEMENT_SPEED = 3f;

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

    public Circle GetBounds()
    {
        return Bounds;
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Sprite.Position = Position;
        Sprite.Draw(gameTime, spriteBatch);
    }
}
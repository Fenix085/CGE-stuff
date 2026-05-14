using MainEngine.Graphics;
using Microsoft.Xna.Framework;
using MainEngine.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
namespace MainEngine.Entities;

public class Player : Sprite
{
    public AnimatedSprite Sprite { get; private set; }
    public Health Health;
    public const float MOVEMENT_SPEED = 5f;

    public Player(AnimatedSprite sprite, Vector2 position, int hp)
    {
        Sprite = sprite;
        Position = position;
        Health = new Health(hp);
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Move(dt);
    }

    public void Move(float timeStep)
    {
        HandleKeyboard();
        HandleGamepad();
    }
    private void HandleKeyboard()
    {
        float speed = MOVEMENT_SPEED;
        Vector2 pos = Position;

        if (HQ.Input.Keyboard.IsKeyDown(Keys.Space))
            speed *= 1.5f;

        if (HQ.Input.Keyboard.IsKeyDown(Keys.W) || HQ.Input.Keyboard.IsKeyDown(Keys.Up))
            pos.Y -= speed;

        if (HQ.Input.Keyboard.IsKeyDown(Keys.S) || HQ.Input.Keyboard.IsKeyDown(Keys.Down))
            pos.Y += speed;

        if (HQ.Input.Keyboard.IsKeyDown(Keys.A) || HQ.Input.Keyboard.IsKeyDown(Keys.Left))
            pos.X -= speed;

        if (HQ.Input.Keyboard.IsKeyDown(Keys.D) || HQ.Input.Keyboard.IsKeyDown(Keys.Right))
            pos.X += speed;

        Position = pos;
    }

    private void HandleGamepad()
    {
        GamePadInfo pad = HQ.Input.GamePads[(int)PlayerIndex.One];

        float speed = MOVEMENT_SPEED;
        Vector2 pos = Position;

        if (pad.IsButtonDown(Buttons.A))
        {
            speed *= 1.5f;
            pad.SetVibration(1.0f, TimeSpan.FromSeconds(1));
        }
        else
        {
            pad.StopVibration();
        }

        if (pad.LeftThumbStick != Vector2.Zero)
        {
            pos.X += pad.LeftThumbStick.X * speed;
            pos.Y -= pad.LeftThumbStick.Y * speed;
        }

        Position = pos;
    }

    public Circle GetBounds()
    {
        return new Circle(
            (int)(Position.X + Sprite.Width * 0.1f),
            (int)(Position.Y + Sprite.Height * 0.1f),
            (int)(Sprite.Width * 0.1f)
        );
    }

    public override void ApplyDeath()
    {
        // Player death logic here (e.g., respawn, game over screen, etc.)
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Sprite.Position = Position;
        Sprite.Draw(gameTime, spriteBatch);
    }
}
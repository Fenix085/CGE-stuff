using MainEngine.Graphics;
using Microsoft.Xna.Framework;
using MainEngine.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
namespace MainEngine;

public class Player
{
    public AnimatedSprite Sprite {get; private set;}
    public Vector2 Position;
    public Health Health;
    public const float MOVEMENT_SPEED = 5f;

    public Player(AnimatedSprite sprite,Vector2 position, int hp)
    {
        Sprite = sprite;
        Position = position;
        Health = new Health(hp);
    }

    public void Update(GameTime gameTime)
    {
        Sprite.Update(gameTime);
        HandleKeyboard();
        HandleGamepad();
        
    }

    private void HandleKeyboard()
    {
        float speed = MOVEMENT_SPEED;

        if (HQ.Input.Keyboard.IsKeyDown(Keys.Space))
            speed *= 1.5f;

        if (HQ.Input.Keyboard.IsKeyDown(Keys.W) || HQ.Input.Keyboard.IsKeyDown(Keys.Up))
            Position.Y -= speed;

        if (HQ.Input.Keyboard.IsKeyDown(Keys.S) || HQ.Input.Keyboard.IsKeyDown(Keys.Down))
            Position.Y += speed;

        if (HQ.Input.Keyboard.IsKeyDown(Keys.A) || HQ.Input.Keyboard.IsKeyDown(Keys.Left))
            Position.X -= speed;

        if (HQ.Input.Keyboard.IsKeyDown(Keys.D) || HQ.Input.Keyboard.IsKeyDown(Keys.Right))
            Position.X += speed;
    }

    private void HandleGamepad()
    {
        GamePadInfo pad = HQ.Input.GamePads[(int)PlayerIndex.One];

        float speed = MOVEMENT_SPEED;

        if (pad.IsButtonDown(Buttons.A))
        {
            speed *= 1.5f;
            pad.SetVibration(1.0f, TimeSpan.FromSeconds(1));
        }

        if (pad.LeftThumbStick != Vector2.Zero)
        {
            Position.X += pad.LeftThumbStick.X * speed;
            Position.Y -= pad.LeftThumbStick.Y * speed;
        }
    }

    public Circle GetBounds()
    {
        return new Circle(
            (int)(Position.X + Sprite.Width * 0.1f),
            (int)(Position.Y + Sprite.Height * 0.1f),
            (int)(Sprite.Width * 0.1f)
        );
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        Sprite.Draw(spriteBatch, Position);
    }
}
using MainEngine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MainEngine.Input;
using Microsoft.Xna.Framework.Input;
using System;

namespace MainEngine.Entities;

public class Player : Sprite
{
    public AnimatedSprite? Sprite { get; private set; }
    public Health Health;
    public const float MOVEMENT_SPEED = 5f;

    // Debug-заглушка: зелёный квадрат пока нет спрайта
    private readonly Texture2D? _pixel;
    private const int DEBUG_SIZE = 32;

    // ── Система опыта ─────────────────────────────────────────────────────

    public int Level      { get; private set; } = 1;
    public int CurrentXp  { get; private set; } = 0;
    public int RequiredXp { get; private set; } = 100;

    public event Action? OnLevelUp;

    private const float XP_SCALE = 1.4f;

    // ── Конструкторы ──────────────────────────────────────────────────────

    /// <summary>Конструктор с реальным спрайтом.</summary>
    public Player(AnimatedSprite sprite, Vector2 position, int hp)
    {
        Sprite   = sprite;
        Position = position;
        Health   = new Health(hp);
    }

    /// <summary>Конструктор-заглушка: игрок = зелёный квадрат.</summary>
    public Player(Vector2 position, int hp, Texture2D pixel)
    {
        Sprite   = null;
        Position = position;
        Health   = new Health(hp);
        _pixel   = pixel;
    }

    // ── Update ────────────────────────────────────────────────────────────

    public override void Update(GameTime gameTime)
    {
        Move((float)gameTime.ElapsedGameTime.TotalSeconds);
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

    // ── Опыт ──────────────────────────────────────────────────────────────

    public bool AddExperience(int amount)
    {
        CurrentXp += amount;

        if (CurrentXp >= RequiredXp)
        {
            CurrentXp -= RequiredXp;
            Level++;
            RequiredXp = (int)(RequiredXp * XP_SCALE);
            OnLevelUp?.Invoke();
            return true;
        }
        return false;
    }

    // ── Геометрия ─────────────────────────────────────────────────────────

    public Vector2 Center =>
        Sprite != null
            ? new Vector2(Position.X + Sprite.Width  * 0.5f, Position.Y + Sprite.Height * 0.5f)
            : new Vector2(Position.X + DEBUG_SIZE * 0.5f,    Position.Y + DEBUG_SIZE * 0.5f);

    public Circle GetBounds()
    {
        if (Sprite != null)
            return new Circle(
                (int)(Position.X + Sprite.Width  * 0.1f),
                (int)(Position.Y + Sprite.Height * 0.1f),
                (int)(Sprite.Width * 0.1f));

        return new Circle(
            (int)(Position.X + DEBUG_SIZE * 0.5f),
            (int)(Position.Y + DEBUG_SIZE * 0.5f),
            DEBUG_SIZE / 2);
    }

    // ── Draw ──────────────────────────────────────────────────────────────

    public override void ApplyDeath() { }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (Sprite != null)
        {
            Sprite.Position = Position;
            Sprite.Draw(gameTime, spriteBatch);
            return;
        }

        // Заглушка — зелёный квадрат
        if (_pixel != null)
        {
            spriteBatch.Draw(
                _pixel,
                new Rectangle((int)Position.X, (int)Position.Y, DEBUG_SIZE, DEBUG_SIZE),
                Color.LimeGreen);
        }
    }
}

using MainEngine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MainEngine.Input;
using Microsoft.Xna.Framework.Input;
using System;
using Microsoft.Xna.Framework.Audio;

namespace MainEngine.Entities;

public class Player : Sprite
{
    public AnimatedSprite? Sprite { get; set; }
    public Health Health;
    public const float MOVEMENT_SPEED = 3f;

    private readonly Texture2D? _pixel;
    private const int DEBUG_SIZE = 32;

    private SoundEffect? _footstepSound;
    private SoundEffectInstance? _footstepInstance;

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

    public Vector2 LastMoveDirection { get; private set; } = Vector2.UnitX;

    // ── Update ────────────────────────────────────────────────────────────

    public override void Update(GameTime gameTime)
    {
        Move((float)gameTime.ElapsedGameTime.TotalSeconds);
    }

    public void Move(float timeStep)
    {
        Vector2 oldPos = Position;

        HandleKeyboard();
        HandleGamepad();

        bool isMoving = Vector2.DistanceSquared(oldPos, Position) > 0.01f;

        UpdateFootsteps(isMoving);
    }

    private void HandleKeyboard()
{
    float   speed = MOVEMENT_SPEED;
    Vector2 pos   = Position;
    Vector2 dir   = Vector2.Zero;

    if (HQ.Input.Keyboard.IsKeyDown(Keys.Space))
        speed *= 1.5f;

    if (HQ.Input.Keyboard.IsKeyDown(Keys.W) || HQ.Input.Keyboard.IsKeyDown(Keys.Up))
        { pos.Y -= speed; dir.Y -= 1f; }

    if (HQ.Input.Keyboard.IsKeyDown(Keys.S) || HQ.Input.Keyboard.IsKeyDown(Keys.Down))
        { pos.Y += speed; dir.Y += 1f; }

    if (HQ.Input.Keyboard.IsKeyDown(Keys.A) || HQ.Input.Keyboard.IsKeyDown(Keys.Left))
        { pos.X -= speed; dir.X -= 1f; }

    if (HQ.Input.Keyboard.IsKeyDown(Keys.D) || HQ.Input.Keyboard.IsKeyDown(Keys.Right))
        { pos.X += speed; dir.X += 1f; }

    if (dir != Vector2.Zero)
        LastMoveDirection = Vector2.Normalize(dir);

    Position = pos;
}

private void HandleGamepad()
{
    GamePadInfo pad   = HQ.Input.GamePads[(int)PlayerIndex.One];
    float       speed = MOVEMENT_SPEED;
    Vector2     pos   = Position;

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
        Vector2 stick = pad.LeftThumbStick;
        pos.X += stick.X * speed;
        pos.Y -= stick.Y * speed;
        LastMoveDirection = Vector2.Normalize(new Vector2(stick.X, -stick.Y));
    }

    Position = pos;
}

public void SetFootstepSound(SoundEffect sound)
{
    _footstepSound = sound;

    _footstepInstance = sound.CreateInstance();
    _footstepInstance.IsLooped = true;
    _footstepInstance.Volume = 0.35f;
}

private void UpdateFootsteps(bool isMoving)
{
    if (_footstepInstance == null)
        return;

    if (isMoving)
    {
        if (_footstepInstance.State != SoundState.Playing)
            _footstepInstance.Play();
    }
    else
    {
        if (_footstepInstance.State == SoundState.Playing)
            _footstepInstance.Pause();
    }
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
        ? Position
        : new Vector2(Position.X + DEBUG_SIZE * 0.5f, Position.Y + DEBUG_SIZE * 0.5f);

    public Circle GetBounds()
    {
        if (Sprite != null)
            return new Circle(
                (int)Position.X,
                (int)Position.Y,
                (int)(Sprite.Width * 0.3f));

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
            if (LastMoveDirection.X < 0)
                Sprite.Effects = SpriteEffects.FlipHorizontally;
            else if (LastMoveDirection.X > 0)
                Sprite.Effects = SpriteEffects.None;

            Sprite.Position = Position;
            Sprite.Draw(gameTime, spriteBatch);
            return;
        }

        if (_pixel != null)
        {
            spriteBatch.Draw(
                _pixel,
                new Rectangle((int)Position.X, (int)Position.Y, DEBUG_SIZE, DEBUG_SIZE),
                Color.LimeGreen);
        }
    }
}

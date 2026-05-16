using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LILITH.Items;

/// <summary>
/// Частица опыта — зелёный пульсирующий кружок.
/// </summary>
public class ExperienceOrb
{
    public Vector2 Position   { get; set; }
    public int     Value       { get; private set; }
    public bool    IsCollected { get; private set; }

    private const int   RADIUS       = 8;
    private const float PICKUP_RADIUS = 30f;
    private const float PULSE_SPEED  = 2f;

    private float _pulseTimer;

    public ExperienceOrb(Vector2 position, int value = 10)
    {
        Position    = position;
        Value       = value;
        IsCollected = false;
    }

    public void Update(GameTime gameTime)
    {
        if (IsCollected) return;
        _pulseTimer += (float)gameTime.ElapsedGameTime.TotalSeconds * PULSE_SPEED;
    }

    public bool TryCollect(Vector2 playerCenter)
    {
        if (IsCollected) return false;
        if (Vector2.Distance(Position, playerCenter) <= PICKUP_RADIUS)
        {
            IsCollected = true;
            return true;
        }
        return false;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (IsCollected) return;

        float pulse = 1f + 0.15f * MathF.Sin(_pulseTimer);
        int   r     = (int)(RADIUS * pulse);

        DrawFilledCircle(spriteBatch, pixel, Position, r, new Color(50, 220, 80));
        DrawFilledCircle(spriteBatch, pixel,
            Position - new Vector2(r * 0.25f, r * 0.25f),
            r / 3,
            new Color(180, 255, 180, 180));
    }

    private static void DrawFilledCircle(SpriteBatch sb, Texture2D pixel, Vector2 center, int r, Color color)
    {
        int r2 = r * r;
        for (int dy = -r; dy <= r; dy++)
        {
            int dx = (int)MathF.Sqrt(r2 - dy * dy);
            sb.Draw(pixel,
                new Rectangle((int)center.X - dx, (int)center.Y + dy, dx * 2, 1),
                color);
        }
    }
}

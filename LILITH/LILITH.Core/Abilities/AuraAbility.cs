using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LILITH.Abilities;

public class AuraAbility : IAbility
{
    public string Name        => "Aura";
    public string Description => "Produce an aura around\nthe player. Damages enemies on contact.";

    private float _radius;
    private float _pulseTimer;
    private Vector2 _lastPlayerCenter;

    private const float PULSE_SPEED        = 2.0f;
    private const float BASE_RADIUS        = 60f;
    private const float UPGRADE_RADIUS_GAIN = 20f;
    private const float PULSE_AMPLITUDE    = 4f;   
    private const int   OUTLINE_THICKNESS  = 2;

    public AuraAbility()
    {
        _radius = BASE_RADIUS;
    }

    public void Upgrade()
    {
        _radius += UPGRADE_RADIUS_GAIN;
    }

    public void Update(GameTime gameTime, Vector2 playerCenter, Vector2 cursorWorld)
    {
        float dt        = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _pulseTimer    += dt * PULSE_SPEED;
        _lastPlayerCenter = playerCenter;
    }

    public void Draw(GameTime gameTime, SpriteBatch sb, Texture2D pixel)
    {
        float pulse  = MathF.Sin(_pulseTimer);
        float r      = _radius + pulse * PULSE_AMPLITUDE;
        int   ri     = (int)r;

        // Заполненный полупрозрачный круг
        DrawFilledCircle(sb, pixel, _lastPlayerCenter, ri,
    new Color(80, 120, 255, 15));

        DrawCircleOutline(sb, pixel, _lastPlayerCenter, ri,
    OUTLINE_THICKNESS, new Color(100, 160, 255, 60 + (int)(20 * pulse)));
    }

    private static void DrawFilledCircle(SpriteBatch sb, Texture2D pixel,
                                         Vector2 center, int r, Color color)
    {
        int r2 = r * r;
        for (int dy = -r; dy <= r; dy++)
        {
            int dx = (int)MathF.Sqrt(MathF.Max(0f, r2 - dy * dy));
            sb.Draw(pixel,
                new Rectangle((int)center.X - dx, (int)center.Y + dy, dx * 2, 1),
                color);
        }
    }

    private static void DrawCircleOutline(SpriteBatch sb, Texture2D pixel,
                                          Vector2 center, int r, int thickness, Color color)
    {
        for (int t = 0; t < thickness; t++)
        {
            int rt  = r + t;
            int rt2 = rt * rt;
            int ri2 = (rt - 1) * (rt - 1);

            int r2 = rt * rt;
            for (int dy = -rt; dy <= rt; dy++)
            {
                int dxOuter = (int)MathF.Sqrt(MathF.Max(0f, rt2 - dy * dy));
                int dxInner = (int)MathF.Sqrt(MathF.Max(0f, ri2 - dy * dy));

                // Левая полоска контура
                sb.Draw(pixel,
                    new Rectangle((int)center.X - dxOuter, (int)center.Y + dy,
                                  dxOuter - dxInner, 1),
                    color);

                // Правая полоска контура
                sb.Draw(pixel,
                    new Rectangle((int)center.X + dxInner, (int)center.Y + dy,
                                  dxOuter - dxInner, 1),
                    color);
            }
        }
    }
}
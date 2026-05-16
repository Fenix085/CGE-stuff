using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LILITH.Abilities;

public class SatelliteProjectile
{
    public Vector2 Position  { get; private set; }
    public bool    IsExpired { get; private set; }
    public int     Damage    { get; }

    private readonly Vector2 _direction;
    private readonly float   _speed;
    private readonly float   _lifetime;
    private          float   _age;

    private const int RADIUS = 5;

    public SatelliteProjectile(Vector2 startPosition, Vector2 direction,
                                float speed, float lifetime, int damage)
    {
        Position   = startPosition;
        _direction = Vector2.Normalize(direction);
        _speed     = speed;
        _lifetime  = lifetime;
        Damage     = damage;
    }

    public void Update(GameTime gameTime)
    {
        if (IsExpired) return;
        float dt  = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _age     += dt;
        Position += _direction * _speed * dt;
        if (_age >= _lifetime) IsExpired = true;
    }

    public void Kill() => IsExpired = true;

    public bool Hits(Vector2 targetCenter, float targetRadius)
        => !IsExpired && Vector2.Distance(Position, targetCenter) <= targetRadius + RADIUS;

    public void Draw(SpriteBatch sb, Texture2D pixel)
    {
        if (IsExpired) return;
        DrawCircle(sb, pixel, Position, RADIUS, new Color(180, 240, 255));
        DrawCircle(sb, pixel, Position - Vector2.One, 2, Color.White);
    }

    private static void DrawCircle(SpriteBatch sb, Texture2D pixel,
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
}
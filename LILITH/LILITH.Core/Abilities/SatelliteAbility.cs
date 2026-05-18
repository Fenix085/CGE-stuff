using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MainEngine;

namespace LILITH.Abilities;

public class SatelliteAbility : IAbility
{
    private int   _satelliteCount;
    private float _orbitRadius;
    private float _orbitSpeed;
    private float _fireInterval;
    private int   _projectileDamage;
    private float _projectileSpeed;
    private float _projectileLifetime;

    private float   _orbitAngle;
    private float   _fireCooldown;
    private Vector2 _lastPlayerCenter;

    private readonly List<SatelliteProjectile> _projectiles = new();

    private const int SAT_RADIUS = 7;
    public string Name        => "Satellite";
    public string Description => "Orbital satellite,\nshooting at the cursor.";
    public int Damage => _projectileDamage;

    public SatelliteAbility()
    {
        _satelliteCount     = 1;
        _orbitRadius        = 60f;
        _orbitSpeed         = 2.0f;
        _fireInterval       = 1.2f;
        _projectileDamage   = 5;
        _projectileSpeed    = 420f;
        _projectileLifetime = 2.0f;
        _fireCooldown       = 0f;
    }

    public void Upgrade()
    {
        _satelliteCount++;
        _projectileDamage += 2;
        _fireInterval      = MathF.Max(0.4f, _fireInterval - 0.1f);
    }

    public void Update(GameTime gameTime, Vector2 playerCenter, Vector2 cursorWorld)
    {
        float dt          = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _lastPlayerCenter = playerCenter;
        _orbitAngle      += _orbitSpeed * dt;
        _fireCooldown    -= dt;

        if (_fireCooldown <= 0f)
        {
            FireToward(playerCenter, cursorWorld);
            _fireCooldown = _fireInterval;
        }

        for (int i = _projectiles.Count - 1; i >= 0; i--)
        {
            _projectiles[i].Update(gameTime);
            if (_projectiles[i].IsExpired)
                _projectiles.RemoveAt(i);
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch sb, Texture2D pixel)
    {
        for (int i = 0; i < _satelliteCount; i++)
        {
            Vector2 pos = GetSatellitePosition(_lastPlayerCenter, i);
            DrawCircle(sb, pixel, pos, SAT_RADIUS, new Color(100, 200, 255));
            DrawCircle(sb, pixel, pos - Vector2.One * 2, 3, Color.White);
        }

        foreach (var p in _projectiles)
            p.Draw(sb, pixel);
    }

    private void FireToward(Vector2 playerCenter, Vector2 cursorWorld)
    {
        Vector2 direction = cursorWorld - playerCenter;
        if (direction == Vector2.Zero) direction = Vector2.UnitX;

        for (int i = 0; i < _satelliteCount; i++)
        {
            Vector2 satPos = GetSatellitePosition(playerCenter, i);
            _projectiles.Add(new SatelliteProjectile(
                satPos, direction,
                _projectileSpeed,
                _projectileLifetime,
                _projectileDamage));
        }
    }

    public IReadOnlyList<Circle> GetHitCircles()
    {
        var circles = new List<Circle>();
        foreach (var p in _projectiles)
        {
            if (!p.IsExpired)
                circles.Add(new Circle((int)p.Position.X, (int)p.Position.Y, 5));
        }
        return circles;
    }
    public void NotifyHit(Circle hitCircle)
    {
        foreach (var p in _projectiles)
            if (!p.IsExpired &&
                new Circle((int)p.Position.X, (int)p.Position.Y, 5).Intersects(hitCircle))
                p.Kill();
    }

    private Vector2 GetSatellitePosition(Vector2 center, int index)
    {
        float angle = _orbitAngle + (MathF.Tau / _satelliteCount) * index;
        return center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * _orbitRadius;
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
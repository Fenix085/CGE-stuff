using System;
using System.Collections.Generic;
using MainEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LILITH.Abilities;

public class AutoShootAbility : IAbility
{
    public string Name        => "Auto Shot";
    public string Description => "Automatically shoots projectiles\nin the enemy direction.";

    // ── Parameters ─────────────────────────────────────────────────────────

    public int    Damage            { get; private set; } = 2;
    private float _fireInterval     = 1.0f;
    private float _projectileSpeed  = 350f;
    private float _projectileLife   = 2.0f;
    private float _fireCooldown     = 0f;
    public bool    IsExpired { get; private set; }

    private readonly List<AutoProjectile> _projectiles = new();

    // ── Upgrade ───────────────────────────────────────────────────────────

    public void Upgrade()
    {
        Damage          += 2;
        _fireInterval    = MathF.Max(0.3f, _fireInterval - 0.15f);
        _projectileSpeed += 20f;
    }

    // ── Update ────────────────────────────────────────────────────────────

    public void Update(GameTime gameTime, Vector2 playerCenter, Vector2 aimDirection)
    {
        float dt      = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _fireCooldown -= dt;

        
        if (_fireCooldown <= 0f && aimDirection != Vector2.Zero)
        {
            _projectiles.Add(new AutoProjectile(
                playerCenter,
                aimDirection,
                _projectileSpeed,
                _projectileLife));

            _fireCooldown = _fireInterval;
        }

        for (int i = _projectiles.Count - 1; i >= 0; i--)
        {
            _projectiles[i].Update(gameTime);
            if (_projectiles[i].IsExpired)
                _projectiles.RemoveAt(i);
        }
    }

    // ── Draw ──────────────────────────────────────────────────────────────

    public void Draw(GameTime gameTime, SpriteBatch sb, Texture2D pixel)
    {
        foreach (var p in _projectiles)
            p.Draw(sb, pixel);
    }

    // ── Hitboxes ──────────────────────────────────────────────────────────
    
    public IReadOnlyList<Circle> GetHitCircles()
    {
        var circles = new List<Circle>();
        foreach (var p in _projectiles)
            if (!p.IsExpired)
                circles.Add(new Circle((int)p.Position.X, (int)p.Position.Y, 4));
        return circles;
    }
    public void NotifyHit(Circle hitCircle)
    {
        foreach (var p in _projectiles)
            if (!p.IsExpired &&
                new Circle((int)p.Position.X, (int)p.Position.Y, 4).Intersects(hitCircle))
                p.Kill();
    }

    // ── AutoProjectile ────────────────────────────────────────────

    private class AutoProjectile
    {
        public Vector2 Position   { get; private set; }
        public bool    IsExpired  { get; private set; }

        private readonly Vector2 _direction;
        private readonly float   _speed;
        private readonly float   _lifetime;
        private          float   _age;

        private const int RADIUS = 4;
        public void Kill() => IsExpired = true;
        public AutoProjectile(Vector2 start, Vector2 direction, float speed, float lifetime)
        {
            Position   = start;
            _direction = Vector2.Normalize(direction);
            _speed     = speed;
            _lifetime  = lifetime;
        }

        public void Update(GameTime gameTime)
        {
            float dt  = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _age     += dt;
            Position += _direction * _speed * dt;
            if (_age >= _lifetime) IsExpired = true;
        }

        public void Draw(SpriteBatch sb, Texture2D pixel)
        {
            if (IsExpired) return;
            DrawCircle(sb, pixel, Position, RADIUS,     new Color(255, 220, 80));
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
}
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LILITH.Abilities;

public class TrailAbility : IAbility
{
    public string Name        => "Trail";
    public string Description => "Left a trail while\nmoving. Damages enemies on contact.";

    // ── Параметры ─────────────────────────────────────────────────────────

    private float _dropRadius;
    private float _dropLifetime;
    private float _spawnInterval;   // как часто появляется новая капля
    private float _spawnTimer;

    private const float BASE_RADIUS      = 6f;
    private const float UPGRADE_RADIUS   = 3f;
    private const float BASE_LIFETIME    = 2.0f;
    private const float BASE_INTERVAL    = 0.08f;
    private const float MIN_MOVE_DIST    = 4f;   // минимальное смещение для спавна капли

    private Vector2 _lastPlayerCenter;
    private bool    _initialized;

    // ── Капли ─────────────────────────────────────────────────────────────

    private readonly List<TrailDrop> _drops = new();

    private struct TrailDrop
    {
        public Vector2 Position;
        public float   Age;
        public float   Lifetime;
        public float   Radius;

        public float Alpha => 1f - Age / Lifetime;
        public bool  IsExpired => Age >= Lifetime;
    }

    // ── Конструктор ───────────────────────────────────────────────────────

    public TrailAbility()
    {
        _dropRadius    = BASE_RADIUS;
        _dropLifetime  = BASE_LIFETIME;
        _spawnInterval = BASE_INTERVAL;
    }

    public void Upgrade()
    {
        _dropRadius   += UPGRADE_RADIUS;
        _dropLifetime += 0.3f;
    }

    // ── Update ────────────────────────────────────────────────────────────

    public void Update(GameTime gameTime, Vector2 playerCenter, Vector2 cursorWorld)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Первый кадр — просто запоминаем позицию
        if (!_initialized)
        {
            _lastPlayerCenter = playerCenter;
            _initialized      = true;
        }

        // Спавним каплю только если игрок достаточно сдвинулся
        bool moved = Vector2.Distance(playerCenter, _lastPlayerCenter) >= MIN_MOVE_DIST;

        _spawnTimer += dt;
        if (moved && _spawnTimer >= _spawnInterval)
        {
            _drops.Add(new TrailDrop
            {
                Position = _lastPlayerCenter,
                Age      = 0f,
                Lifetime = _dropLifetime,
                Radius   = _dropRadius
            });
            _spawnTimer = 0f;
        }

        // Обновляем возраст капель, удаляем истёкшие
        for (int i = _drops.Count - 1; i >= 0; i--)
        {
            var drop = _drops[i];
            drop.Age += dt;
            if (drop.IsExpired)
                _drops.RemoveAt(i);
            else
                _drops[i] = drop;
        }

        _lastPlayerCenter = playerCenter;
    }

    // ── Draw ──────────────────────────────────────────────────────────────

    public void Draw(GameTime gameTime, SpriteBatch sb, Texture2D pixel)
    {
        foreach (var drop in _drops)
        {
            int   r     = (int)drop.Radius;
            float alpha = drop.Alpha;

            // Внешний круг — полупрозрачный
            DrawFilledCircle(sb, pixel, drop.Position, r,
                new Color(180, 60, 255, (int)(60 * alpha)));

            // Внутреннее ядро — ярче
            DrawFilledCircle(sb, pixel, drop.Position, Math.Max(1, r / 2),
                new Color(220, 120, 255, (int)(120 * alpha)));
        }
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
}
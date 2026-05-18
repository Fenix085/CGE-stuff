using System;
using System.Collections.Generic;
using MainEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LILITH.Abilities;

public class SlashAbility : IAbility
{
    public string Name        => "Slash";
    public string Description => "Auto sword slash\n in the direction of movement.";
    public int Damage { get; private set; } = 10;

    // ── Параметры ─────────────────────────────────────────────────────────

    private float _arcAngle;       // угол дуги взмаха в радианах
    private float _arcRadius;      // длина удара
    private float _attackInterval; // секунд между ударами
    private float _swingDuration;  // длительность анимации взмаха

    private const float BASE_ARC_ANGLE    = MathF.PI * 0.6f; // 108 градусов
    private const float BASE_ARC_RADIUS   = 55f;
    private const float BASE_INTERVAL     = 1.0f;
    private const float BASE_SWING_DUR    = 0.22f;

    // ── Состояние ─────────────────────────────────────────────────────────

    private float   _attackTimer;   // таймер до следующего удара
    private float   _swingTimer;    // прогресс текущего взмаха (0..swingDuration)
    private bool    _isSwinging;
    private Vector2 _swingDirection; // направление в момент удара
    private Vector2 _lastPlayerCenter;

    public SlashAbility()
    {
        _arcAngle       = BASE_ARC_ANGLE;
        _arcRadius      = BASE_ARC_RADIUS;
        _attackInterval = BASE_INTERVAL;
        _swingDuration  = BASE_SWING_DUR;
        _attackTimer    = BASE_INTERVAL; // первый удар сразу
    }

    public void Upgrade()
    {
        _arcRadius      += 15f;
        _arcAngle        = MathF.Min(_arcAngle + 0.15f, MathF.PI * 0.9f);
        _attackInterval  = MathF.Max(0.4f, _attackInterval - 0.1f);
    }

    public void NotifyHit(Circle hitCircle) { }

    // ── Update ────────────────────────────────────────────────────────────

    public void Update(GameTime gameTime, Vector2 playerCenter, Vector2 aimDirection)
    {
        float dt          = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _lastPlayerCenter = playerCenter;

        // Таймер атаки
        _attackTimer -= dt;
        if (_attackTimer <= 0f)
        {
            _attackTimer    = _attackInterval;
            _isSwinging     = true;
            _swingTimer     = 0f;
            _swingDirection = aimDirection;
        }

        // Таймер анимации взмаха
        if (_isSwinging)
        {
            _swingTimer += dt;
            if (_swingTimer >= _swingDuration)
                _isSwinging = false;
        }
    }

    // ── Draw ──────────────────────────────────────────────────────────────

    public void Draw(GameTime gameTime, SpriteBatch sb, Texture2D pixel)
    {
        if (!_isSwinging) return;

        // Прогресс взмаха от 0 до 1
        float t = _swingTimer / _swingDuration;

        // Дуга начинается с одной стороны и заметает до другой
        float halfArc   = _arcAngle * 0.5f;
        float baseAngle = MathF.Atan2(_swingDirection.Y, _swingDirection.X);

        // Текущий угол конца дуги — линейно проходит от -halfArc до +halfArc
        float sweepEnd = MathHelper.Lerp(-halfArc, halfArc, t);

        // Яркость и прозрачность — пик в середине, затухание к концу
        float brightness = 1f - MathF.Abs(t - 0.5f) * 2f;

        DrawArc(sb, pixel, _lastPlayerCenter, _arcRadius,
                baseAngle - halfArc, baseAngle + sweepEnd,
                brightness);
    }

    private static void DrawArc(SpriteBatch sb, Texture2D pixel,
                                 Vector2 center, float radius,
                                 float angleFrom, float angleTo,
                                 float brightness)
    {
        const int SEGMENTS = 24;
        float     step     = (angleTo - angleFrom) / SEGMENTS;

        for (int i = 0; i < SEGMENTS; i++)
        {
            float a = angleFrom + step * i;

            // Рисуем несколько линий от центра — создаёт эффект толщины дуги
            for (int layer = 0; layer < 3; layer++)
            {
                float layerT      = layer / 3f;
                float innerRadius = radius * (0.4f + layerT * 0.6f);
                float outerRadius = radius * (0.5f + layerT * 0.5f + 0.1f);
                float alpha       = brightness * (1f - layerT * 0.5f);

                Vector2 inner = center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * innerRadius;
                Vector2 outer = center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * outerRadius;

                Color color = new Color(
                    1f,
                    0.85f - layerT * 0.3f,
                    0.2f - layerT * 0.2f,
                    alpha);

                DrawLine(sb, pixel, inner, outer, color, 2);
            }
        }

        // Яркая кромка по краю дуги
        for (int i = 0; i < SEGMENTS; i++)
        {
            float   a   = angleFrom + step * i;
            float   a2  = a + step;
            Vector2 p1  = center + new Vector2(MathF.Cos(a),  MathF.Sin(a))  * radius;
            Vector2 p2  = center + new Vector2(MathF.Cos(a2), MathF.Sin(a2)) * radius;
            Color   col = new Color(1f, 1f, 0.8f, brightness * 0.9f);
            DrawLine(sb, pixel, p1, p2, col, 2);
        }
    }

    private static void DrawLine(SpriteBatch sb, Texture2D pixel,
                                  Vector2 start, Vector2 end, Color color, int thickness)
    {
        Vector2 delta  = end - start;
        float   length = delta.Length();
        if (length < 0.001f) return;

        float angle = MathF.Atan2(delta.Y, delta.X);
        sb.Draw(pixel, start, null, color, angle,
                Vector2.Zero, new Vector2(length, thickness),
                SpriteEffects.None, 0f);
    }

    public IReadOnlyList<Circle> GetHitCircles()
{
    if (!_isSwinging) return Array.Empty<Circle>();

    var circles = new List<Circle>();
    float halfArc   = _arcAngle * 0.5f;
    float baseAngle = MathF.Atan2(_swingDirection.Y, _swingDirection.X);
    float t         = _swingTimer / _swingDuration;
    float sweepEnd  = MathHelper.Lerp(-halfArc, halfArc, t);

    const int STEPS = 8;
    for (int i = 0; i <= STEPS; i++)
    {
        float   a   = baseAngle - halfArc + (sweepEnd - (-halfArc)) * i / STEPS;
        Vector2 pos = _lastPlayerCenter +
                      new Vector2(MathF.Cos(a), MathF.Sin(a)) * _arcRadius * 0.7f;
        circles.Add(new Circle((int)pos.X, (int)pos.Y, 12));
    }

    return circles;
}
}
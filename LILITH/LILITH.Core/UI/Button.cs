using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace LILITH.UI;

/// <summary>
/// Красивая кнопка со скруглёнными углами и цветовыми состояниями.
/// </summary>
public class Button
{
    public Rectangle Bounds    { get; set; }
    public string    Label     { get; set; }
    public bool      IsHovered { get; private set; }
    public bool ForceHover { get; set; } 
    public event Action? OnClick;

    private MouseState _prevMouse;
    private bool       _isPressed;
    private float      _spawnDelay = 0.15f;

    // Цветовые схемы: нормальное / ховер / нажатое
    public Color ColorNormal  = new Color(70,  130, 220);
    public Color ColorHover   = new Color(100, 170, 255);
    public Color ColorPressed = new Color(40,  90,  180);
    public Color ColorText    = Color.White;
    public Color ColorShadow  = new Color(0, 0, 0, 80);

    private const int CORNER_RADIUS = 10;

    public Button(Rectangle bounds, string label)
    {
        Bounds = bounds;
        Label  = label;
    }

    public void Update(GameTime gameTime)
    {
        if (_spawnDelay > 0f)
        {
            _spawnDelay -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            _prevMouse   = Mouse.GetState();
            return;
        }

        MouseState mouse = Mouse.GetState();
        IsHovered  = Bounds.Contains(mouse.Position) || ForceHover;
        _isPressed = IsHovered && mouse.LeftButton == ButtonState.Pressed;

        bool justClicked = IsHovered
                        && mouse.LeftButton      == ButtonState.Pressed
                        && _prevMouse.LeftButton  == ButtonState.Released;

        if (justClicked)
            OnClick?.Invoke();

        _prevMouse = mouse;
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, SpriteFont? font)
    {
        Color bg = _isPressed ? ColorPressed : (IsHovered ? ColorHover : ColorNormal);

        // Смещение вверх при ховере
        int offsetY = (IsHovered && !_isPressed) ? -3 : 0;
        var r = new Rectangle(Bounds.X, Bounds.Y + offsetY, Bounds.Width, Bounds.Height);

        // Тень (чуть ниже и правее)
        if (!_isPressed)
            DrawRoundedRect(sb, pixel,
                new Rectangle(r.X + 3, r.Y + 5, r.Width, r.Height),
                CORNER_RADIUS, ColorShadow);

        // Основная кнопка
        DrawRoundedRect(sb, pixel, r, CORNER_RADIUS, bg);

        // Блик сверху (светлая полоска)
        Color gloss = Color.Lerp(bg, Color.White, 0.3f);
        gloss.A = 120;
        DrawRoundedRect(sb, pixel,
            new Rectangle(r.X + 4, r.Y + 4, r.Width - 8, r.Height / 2 - 4),
            CORNER_RADIUS - 2, gloss);

        // Текст по центру
        if (font != null)
        {
            Vector2 size = font.MeasureString(Label);
            Vector2 pos  = new Vector2(
                r.X + (r.Width  - size.X) * 0.5f,
                r.Y + (r.Height - size.Y) * 0.5f);

            // Тень текста
            sb.DrawString(font, Label, pos + new Vector2(1, 2), Color.Black * 0.4f);
            sb.DrawString(font, Label, pos, ColorText);
        }
    }

    // ── Скруглённый прямоугольник ─────────────────────────────────────────

    private static void DrawRoundedRect(SpriteBatch sb, Texture2D pixel,
                                         Rectangle rect, int radius, Color color)
    {
        if (rect.Width <= 0 || rect.Height <= 0) return;
        radius = Math.Min(radius, Math.Min(rect.Width / 2, rect.Height / 2));

        // Центральная полоска (вертикальная)
        sb.Draw(pixel, new Rectangle(rect.X, rect.Y + radius,
            rect.Width, rect.Height - radius * 2), color);

        // Верхняя и нижняя горизонтальные полоски (между углами)
        sb.Draw(pixel, new Rectangle(rect.X + radius, rect.Y,
            rect.Width - radius * 2, radius), color);
        sb.Draw(pixel, new Rectangle(rect.X + radius, rect.Y + rect.Height - radius,
            rect.Width - radius * 2, radius), color);

        // Четыре скруглённых угла
        DrawFilledCorner(sb, pixel, rect.X + radius,              rect.Y + radius,              radius, color, MathF.PI,        1.5f * MathF.PI);
        DrawFilledCorner(sb, pixel, rect.X + rect.Width - radius, rect.Y + radius,              radius, color, 1.5f * MathF.PI, 2f   * MathF.PI);
        DrawFilledCorner(sb, pixel, rect.X + radius,              rect.Y + rect.Height - radius, radius, color, MathF.PI / 2f,  MathF.PI);
        DrawFilledCorner(sb, pixel, rect.X + rect.Width - radius, rect.Y + rect.Height - radius, radius, color, 0f,            MathF.PI / 2f);
    }

    private static void DrawFilledCorner(SpriteBatch sb, Texture2D pixel,
                                          int cx, int cy, int radius,
                                          Color color, float startAngle, float endAngle)
    {
        int segments = 10;
        float step   = (endAngle - startAngle) / segments;

        for (int i = 0; i < segments; i++)
        {
            float a0 = startAngle + step * i;
            float a1 = startAngle + step * (i + 1);

            int x0 = (int)(MathF.Cos(a0) * radius);
            int y0 = (int)(MathF.Sin(a0) * radius);
            int x1 = (int)(MathF.Cos(a1) * radius);
            int y1 = (int)(MathF.Sin(a1) * radius);

            // Заполняем треугольник из центра угла через две точки на дуге
            FillTriangle(sb, pixel,
                new Vector2(cx, cy),
                new Vector2(cx + x0, cy + y0),
                new Vector2(cx + x1, cy + y1),
                color);
        }
    }

    private static void FillTriangle(SpriteBatch sb, Texture2D pixel,
                                      Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        // Простая растеризация треугольника через горизонтальные линии
        Vector2[] pts = { a, b, c };
        Array.Sort(pts, (p, q) => p.Y.CompareTo(q.Y));

        int y0 = (int)pts[0].Y;
        int y2 = (int)pts[2].Y;

        for (int y = y0; y <= y2; y++)
        {
            float t01 = y2 == y0 ? 1f : (float)(y - y0) / (y2 - y0);
            float x0  = pts[0].X + (pts[2].X - pts[0].X) * t01;

            float x1;
            if (y < (int)pts[1].Y)
            {
                float t = pts[1].Y == pts[0].Y ? 1f : (float)(y - pts[0].Y) / (pts[1].Y - pts[0].Y);
                x1 = pts[0].X + (pts[1].X - pts[0].X) * t;
            }
            else
            {
                float t = pts[2].Y == pts[1].Y ? 1f : (float)(y - pts[1].Y) / (pts[2].Y - pts[1].Y);
                x1 = pts[1].X + (pts[2].X - pts[1].X) * t;
            }

            int left  = (int)Math.Min(x0, x1);
            int right = (int)Math.Max(x0, x1);
            if (right > left)
                sb.Draw(pixel, new Rectangle(left, y, right - left, 1), color);
        }
    }
}

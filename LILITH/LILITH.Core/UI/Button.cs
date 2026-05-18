using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace LILITH.UI;

/// <summary>
/// Простая кнопка без текстур — прямоугольник с текстом.
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

    // Игнорируем клики первые 150мс после создания кнопки
    // (чтобы клик на предыдущей сцене не засчитался здесь)
    private float _spawnDelay = 0.15f;

    private static readonly Color BgNormal  = new Color(40,  40,  60,  220);
    private static readonly Color BgHover   = new Color(70,  70, 110,  255);
    private static readonly Color BgPress   = new Color(30,  30,  50,  255);
    private static readonly Color BorderCol = new Color(160, 160, 220, 200);
    private static readonly Color BorderHov = new Color(100, 220, 120, 255);

    public Button(Rectangle bounds, string label)
    {
        Bounds = bounds;
        Label  = label;
    }

    public void Update(GameTime gameTime)
    {
        // Пропускаем первые 150мс
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
        Color bg = _isPressed ? BgPress : (IsHovered ? BgHover : BgNormal);

        // Тень
        sb.Draw(pixel,
            new Rectangle(Bounds.X + 3, Bounds.Y + 3, Bounds.Width, Bounds.Height),
            new Color(0, 0, 0, 100));

        // Фон
        sb.Draw(pixel, Bounds, bg);

        // Рамка
        Color border = IsHovered ? BorderHov : BorderCol;
        const int B = 2;
        sb.Draw(pixel, new Rectangle(Bounds.X,                    Bounds.Y,                     Bounds.Width, B),           border);
        sb.Draw(pixel, new Rectangle(Bounds.X,                    Bounds.Y + Bounds.Height - B, Bounds.Width, B),           border);
        sb.Draw(pixel, new Rectangle(Bounds.X,                    Bounds.Y,                     B, Bounds.Height),          border);
        sb.Draw(pixel, new Rectangle(Bounds.X + Bounds.Width - B, Bounds.Y,                     B, Bounds.Height),          border);

        // Текст по центру
        if (font != null)
        {
            Vector2 size = font.MeasureString(Label);
            Vector2 pos  = new Vector2(
                Bounds.X + (Bounds.Width  - size.X) * 0.5f,
                Bounds.Y + (Bounds.Height - size.Y) * 0.5f);

            sb.DrawString(font, Label, pos + new Vector2(1, 1), Color.Black * 0.8f);
            sb.DrawString(font, Label, pos, IsHovered ? Color.Yellow : Color.White);
        }
    }
}

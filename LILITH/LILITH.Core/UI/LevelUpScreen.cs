using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace LILITH.UI;

/// <summary>
/// Экран выбора улучшения при повышении уровня.
/// Показывает 3 карточки, игрок кликает — экран закрывается.
/// </summary>
public class LevelUpScreen
{
    public bool IsVisible { get; private set; }

    public event Action<int>? OnCardChosen;

    private const int CARD_COUNT  = 3;
    private const int CARD_WIDTH  = 160;
    private const int CARD_HEIGHT = 220;
    private const int CARD_GAP    = 30;

    private readonly Rectangle[] _cardRects = new Rectangle[CARD_COUNT];
    private int _hoveredCard = -1;

    private float _showTimer;
    private const float SHOW_DURATION = 0.35f;

    private static readonly Color OverlayColor = new Color(0,   0,   0,   160);
    private static readonly Color CardBg        = new Color(30,  30,  50,  240);
    private static readonly Color CardHover     = new Color(50,  50,  80,  255);
    private static readonly Color CardBorder    = new Color(150, 150, 200, 200);
    private static readonly Color CardBorderHov = new Color(100, 220, 120, 255);

    private MouseState _prevMouse;

    public void Show(Viewport viewport)
    {
        IsVisible  = true;
        _showTimer = 0f;

        int totalWidth = CARD_COUNT * CARD_WIDTH + (CARD_COUNT - 1) * CARD_GAP;
        int startX     = (viewport.Width  - totalWidth) / 2;
        int startY     = (viewport.Height - CARD_HEIGHT) / 2;

        for (int i = 0; i < CARD_COUNT; i++)
            _cardRects[i] = new Rectangle(startX + i * (CARD_WIDTH + CARD_GAP), startY, CARD_WIDTH, CARD_HEIGHT);
    }

    public void Hide() => IsVisible = false;

    public void Update(GameTime gameTime, Viewport viewport)
    {
        if (!IsVisible) return;

        if (_showTimer < SHOW_DURATION)
            _showTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

        MouseState mouse = Mouse.GetState();

        _hoveredCard = -1;
        for (int i = 0; i < CARD_COUNT; i++)
            if (_cardRects[i].Contains(mouse.Position))
            { _hoveredCard = i; break; }

        bool justClicked = mouse.LeftButton     == ButtonState.Pressed
                        && _prevMouse.LeftButton == ButtonState.Released;

        if (justClicked && _hoveredCard >= 0)
        {
            OnCardChosen?.Invoke(_hoveredCard);
            Hide();
        }

        _prevMouse = mouse;
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, SpriteFont? font, Viewport viewport)
    {
        if (!IsVisible) return;

        float alpha = MathHelper.Clamp(_showTimer / SHOW_DURATION, 0f, 1f);

        // Затемнение фона
        sb.Draw(pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), OverlayColor * alpha);

        for (int i = 0; i < CARD_COUNT; i++)
        {
            bool hovered = _hoveredCard == i;
            var  rect    = _cardRects[i];
            if (hovered) rect.Y -= 8;

            // Тень
            sb.Draw(pixel, new Rectangle(rect.X + 4, rect.Y + 4, rect.Width, rect.Height),
                new Color(0, 0, 0, 120) * alpha);

            // Фон
            sb.Draw(pixel, rect, (hovered ? CardHover : CardBg) * alpha);

            // Рамка
            Color border = hovered ? CardBorderHov : CardBorder;
            const int B = 2;
            sb.Draw(pixel, new Rectangle(rect.X,              rect.Y,                   rect.Width, B),  border * alpha);
            sb.Draw(pixel, new Rectangle(rect.X,              rect.Y + rect.Height - B, rect.Width, B),  border * alpha);
            sb.Draw(pixel, new Rectangle(rect.X,              rect.Y,                   B, rect.Height), border * alpha);
            sb.Draw(pixel, new Rectangle(rect.X + rect.Width - B, rect.Y,               B, rect.Height), border * alpha);

            // Иконка-заглушка
            Color iconColor = i switch
            {
                0 => new Color(80,  160, 255),
                1 => new Color(255, 100, 80),
                _ => new Color(180, 80,  255)
            };
            sb.Draw(pixel, new Rectangle(rect.X + 30, rect.Y + 20, CARD_WIDTH - 60, 80), iconColor * alpha);

            if (font != null)
            {
                sb.DrawString(font, $"КАРТОЧКА {i + 1}", new Vector2(rect.X + 10, rect.Y + 115), Color.Yellow * alpha);
                sb.DrawString(font, "Улучшение",         new Vector2(rect.X + 10, rect.Y + 145), Color.White  * alpha);
            }
        }
    }
}

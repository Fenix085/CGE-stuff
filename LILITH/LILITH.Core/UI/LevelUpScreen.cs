using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using LILITH.Abilities;

namespace LILITH.UI;

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

    // Данные карточек — заполняются при Show()
    private readonly string[] _cardNames        = new string[CARD_COUNT];
    private readonly string[] _cardDescriptions = new string[CARD_COUNT];
    private readonly Texture2D[] _cardIcons = new Texture2D[CARD_COUNT];

    private static readonly Color OverlayColor = new Color(0,   0,   0,   160);
    private static readonly Color CardBg        = new Color(30,  30,  50,  240);
    private static readonly Color CardHover     = new Color(50,  50,  80,  255);
    private static readonly Color CardBorder    = new Color(150, 150, 200, 200);
    private static readonly Color CardBorderHov = new Color(100, 220, 120, 255);

    private MouseState _prevMouse;

    /// <param name="cards">Список способностей для отображения (до 3 штук).</param>
    public void Show(Viewport viewport, IReadOnlyList<IAbility> cards)
    {
        IsVisible  = true;
        _showTimer = 0f;

        int totalWidth = CARD_COUNT * CARD_WIDTH + (CARD_COUNT - 1) * CARD_GAP;
        int startX     = (viewport.Width  - totalWidth) / 2;
        int startY     = (viewport.Height - CARD_HEIGHT) / 2;

        for (int i = 0; i < CARD_COUNT; i++)
        {
            _cardRects[i] = new Rectangle(
                startX + i * (CARD_WIDTH + CARD_GAP), startY, CARD_WIDTH, CARD_HEIGHT);

            if (i < cards.Count)
            {
                _cardNames[i]        = cards[i].Name;
                _cardDescriptions[i] = cards[i].Description;
                _cardIcons[i] = cards[i].Icon;
            }
            else
            {
                _cardNames[i]        = "???";
                _cardDescriptions[i] = "";
                _cardIcons[i] = null!;
            }
        }
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

        bool justClicked = mouse.LeftButton      == ButtonState.Pressed
                        && _prevMouse.LeftButton  == ButtonState.Released;

        if (justClicked && _hoveredCard >= 0)
        {
            OnCardChosen?.Invoke(_hoveredCard);
            Hide();
        }

        _prevMouse = mouse;
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, SpriteFont? font, Viewport viewport, int levelUpCardIndex)
    {
        if (!IsVisible) return;

        float alpha = MathHelper.Clamp(_showTimer / SHOW_DURATION, 0f, 1f);

        sb.Draw(pixel,
            new Rectangle(0, 0, viewport.Width, viewport.Height),
            OverlayColor * alpha);

        for (int i = 0; i < CARD_COUNT; i++)
        {
            bool usingMouse = _hoveredCard != -1;

            bool hovered =
                usingMouse
                    ? _hoveredCard == i
                    : levelUpCardIndex == i;
            var  rect    = _cardRects[i];
            if (hovered) rect.Y -= 8;

            // Тень
            sb.Draw(pixel,
                new Rectangle(rect.X + 4, rect.Y + 4, rect.Width, rect.Height),
                new Color(0, 0, 0, 120) * alpha);

            // Фон
            sb.Draw(pixel, rect, (hovered ? CardHover : CardBg) * alpha);

            // Рамка
            Color border = hovered ? CardBorderHov : CardBorder;
            const int B = 2;
            sb.Draw(pixel, new Rectangle(rect.X,                  rect.Y,                   rect.Width, B),  border * alpha);
            sb.Draw(pixel, new Rectangle(rect.X,                  rect.Y + rect.Height - B, rect.Width, B),  border * alpha);
            sb.Draw(pixel, new Rectangle(rect.X,                  rect.Y,                   B, rect.Height), border * alpha);
            sb.Draw(pixel, new Rectangle(rect.X + rect.Width - B, rect.Y,                   B, rect.Height), border * alpha);
            Rectangle iconRect = new Rectangle(
            rect.X + 48,
            rect.Y + 20,
            64,
            64);

            if (_cardIcons[i] != null)
            {
                sb.Draw(
                    _cardIcons[i],
                    iconRect,
                    Color.White * alpha);
            }
            if (font != null)
            {
                // Header
                Vector2 titleSize = font.MeasureString(_cardNames[i]);
                Vector2 titlePos  = new Vector2(
                    rect.X + (CARD_WIDTH - titleSize.X) / 2f,
                    rect.Y + 110f);

                sb.DrawString(font, _cardNames[i], titlePos, Color.Yellow * alpha);

                // Separator
                sb.Draw(pixel,
                    new Rectangle(rect.X + 10, (int)titlePos.Y + (int)titleSize.Y + 4,
                                  CARD_WIDTH - 20, 1),
                    Color.Gray * alpha);

                // Описание — мелким шрифтом, с переносами
                DrawWrappedText(sb, font, _cardDescriptions[i],
                    new Vector2(rect.X + 10, titlePos.Y + titleSize.Y + 10),
                    CARD_WIDTH - 20,
                    Color.LightGray * alpha * 0.85f);
            }
        }
    }

    /// <summary>Рисует текст с переносом по ширине.</summary>
    private static void DrawWrappedText(SpriteBatch sb, SpriteFont font,
                                        string text, Vector2 position,
                                        float maxWidth, Color color)
    {
        string[] words   = text.Split(' ');
        string   line    = "";
        float    lineY   = position.Y;
        float    lineH   = font.MeasureString("A").Y + 2f;

        foreach (string word in words)
        {
            // Учитываем явные переносы \n
            if (word.Contains('\n'))
            {
                string[] parts = word.Split('\n');
                foreach (string part in parts)
                {
                    string test = line.Length > 0 ? line + " " + part : part;
                    if (font.MeasureString(test).X > maxWidth && line.Length > 0)
                    {
                        sb.DrawString(font, line, new Vector2(position.X, lineY), color);
                        lineY += lineH;
                        line   = part;
                    }
                    else
                    {
                        line = test;
                    }
                    // Явный перенос строки
                    if (part != parts[^1])
                    {
                        sb.DrawString(font, line, new Vector2(position.X, lineY), color);
                        lineY += lineH;
                        line   = "";
                    }
                }
            }
            else
            {
                string test = line.Length > 0 ? line + " " + word : word;
                if (font.MeasureString(test).X > maxWidth && line.Length > 0)
                {
                    sb.DrawString(font, line, new Vector2(position.X, lineY), color);
                    lineY += lineH;
                    line   = word;
                }
                else
                {
                    line = test;
                }
            }
        }

        if (line.Length > 0)
            sb.DrawString(font, line, new Vector2(position.X, lineY), color);
    }
}
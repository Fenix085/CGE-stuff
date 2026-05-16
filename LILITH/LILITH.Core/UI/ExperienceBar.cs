using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LILITH.UI;

/// <summary>
/// Полоска опыта в верхней части экрана.
/// </summary>
public class ExperienceBar
{
    private const int BAR_HEIGHT  = 18;
    private const int BAR_MARGIN  = 6;
    private const int BORDER_SIZE = 2;

    private static readonly Color BackColor   = new Color(40,  40,  40,  200);
    private static readonly Color FillColor   = new Color(60,  220, 80);
    private static readonly Color BorderColor = new Color(200, 200, 200, 180);

    private float _flashTimer;
    private const float FLASH_DURATION = 0.15f;

    public void TriggerFlash() => _flashTimer = FLASH_DURATION;

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_flashTimer > 0f) _flashTimer -= dt;
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, Viewport viewport,
                     int currentXp, int requiredXp, int level)
    {
        int barWidth = viewport.Width - BAR_MARGIN * 2;
        int barX     = BAR_MARGIN;
        int barY     = BAR_MARGIN;

        // Фон
        sb.Draw(pixel, new Rectangle(barX, barY, barWidth, BAR_HEIGHT), BackColor);

        // Заполнение
        float ratio     = requiredXp > 0 ? (float)currentXp / requiredXp : 0f;
        int   fillWidth = (int)(barWidth * ratio);

        Color fill = _flashTimer > 0f
            ? Color.Lerp(FillColor, Color.White, _flashTimer / FLASH_DURATION * 0.5f)
            : FillColor;

        if (fillWidth > 0)
            sb.Draw(pixel, new Rectangle(barX, barY, fillWidth, BAR_HEIGHT), fill);

        // Рамка
        sb.Draw(pixel, new Rectangle(barX, barY, barWidth, BORDER_SIZE), BorderColor);
        sb.Draw(pixel, new Rectangle(barX, barY + BAR_HEIGHT - BORDER_SIZE, barWidth, BORDER_SIZE), BorderColor);
        sb.Draw(pixel, new Rectangle(barX, barY, BORDER_SIZE, BAR_HEIGHT), BorderColor);
        sb.Draw(pixel, new Rectangle(barX + barWidth - BORDER_SIZE, barY, BORDER_SIZE, BAR_HEIGHT), BorderColor);

        // Маркеры четвертей
        for (int i = 1; i < 4; i++)
        {
            int mx = barX + barWidth * i / 4;
            sb.Draw(pixel,
                new Rectangle(mx, barY + BAR_HEIGHT / 4, BORDER_SIZE, BAR_HEIGHT / 2),
                new Color(255, 255, 255, 60));
        }
    }
}

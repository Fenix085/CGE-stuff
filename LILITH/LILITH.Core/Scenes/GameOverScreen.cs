using System;
using LILITH.Core.Scenes;
using MainEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LILITH.UI;

public class GameOverScreen
{
    public bool IsVisible { get; private set; }

    private Button      _btnMainMenu = null!;
    private float       _showTimer;
    private SpriteFont? _font;

    private const float SHOW_DURATION = 0.5f;
    private float _alpha;

    private MouseState _prevMouse;

    public void Show(Viewport viewport, SpriteFont? font)
    {
        IsVisible  = true;
        _showTimer = 0f;
        _font      = font;

        int cx = viewport.Width  / 2;
        int cy = viewport.Height / 2;

        _btnMainMenu = new Button(
            new Rectangle(cx - 110, cy + 60, 220, 50),
            "MAIN MENU");

        _btnMainMenu.OnClick += () => HQ.ChangeScene(new MainMenuScene());
    }

    public void Update(GameTime gameTime, Viewport viewport)
    {
        if (!IsVisible) return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _alpha = MathF.Min(1f, _alpha + dt * 1.4f);

        if (_showTimer < SHOW_DURATION)
            _showTimer += dt;

        _btnMainMenu.Update(gameTime);
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, Viewport viewport)
{
    if (!IsVisible) return;

    float alpha = MathHelper.Clamp(_showTimer / SHOW_DURATION, 0f, 1f);

    // ── Dark cinematic overlay ─────────────────────────────
    sb.Draw(pixel,
        new Rectangle(0, 0, viewport.Width, viewport.Height),
        new Color(0, 0, 0, 220) * alpha);

    // ── Main gothic panel ──────────────────────────────────
    int pw = 520;
    int ph = 320;

    int px = (viewport.Width - pw) / 2;
    int py = (viewport.Height - ph) / 2;

    // Outer frame
    sb.Draw(pixel,
        new Rectangle(px, py, pw, ph),
        new Color(122, 85, 144) * alpha);

    // Inner background
    sb.Draw(pixel,
        new Rectangle(px + 2, py + 2, pw - 4, ph - 4),
        new Color(18, 10, 30, 245) * alpha);

    // Inner border
    const int B = 6;

    Color innerBorder = new Color(90, 53, 112);

    sb.Draw(pixel,
        new Rectangle(px + B, py + B, pw - B * 2, 1),
        innerBorder * alpha);

    sb.Draw(pixel,
        new Rectangle(px + B, py + ph - B, pw - B * 2, 1),
        innerBorder * alpha);

    sb.Draw(pixel,
        new Rectangle(px + B, py + B, 1, ph - B * 2),
        innerBorder * alpha);

    sb.Draw(pixel,
        new Rectangle(px + pw - B, py + B, 1, ph - B * 2),
        innerBorder * alpha);

    // ── Decorative diamonds ────────────────────────────────
    DrawDiamond(sb, pixel, px, py, 5,
        new Color(147, 112, 168, 180) * alpha);

    DrawDiamond(sb, pixel, px + pw, py, 5,
        new Color(147, 112, 168, 180) * alpha);

    DrawDiamond(sb, pixel, px, py + ph, 5,
        new Color(147, 112, 168, 180) * alpha);

    DrawDiamond(sb, pixel, px + pw, py + ph, 5,
        new Color(147, 112, 168, 180) * alpha);

    if (_font != null)
    {
        // ── Title ──────────────────────────────────────────

        float pulse =
            1f + MathF.Sin((float)DateTime.Now.TimeOfDay.TotalSeconds * 3f) * 0.03f;

        string title = "YOU WERE EATEN";

        Vector2 titleSize =
            _font.MeasureString(title) * pulse;

        Vector2 titlePos = new Vector2(
            (viewport.Width - titleSize.X) * 0.5f,
            py + 55f);

        // Shadow
        sb.DrawString(
            _font,
            title,
            titlePos + new Vector2(3, 3),
            new Color(20, 0, 0) * alpha,
            0f,
            Vector2.Zero,
            pulse,
            SpriteEffects.None,
            0f);

        // Main text
        sb.DrawString(
            _font,
            title,
            titlePos,
            new Color(220, 170, 190) * alpha,
            0f,
            Vector2.Zero,
            pulse,
            SpriteEffects.None,
            0f);

        // ── Divider line ───────────────────────────────────

        sb.Draw(pixel,
            new Rectangle(px + 40, py + 120, pw - 80, 1),
            new Color(122, 85, 144, 180) * alpha);

        // ── Subtitle ───────────────────────────────────────

        string sub = "The bananas consumed your soul";

        Vector2 subSize = _font.MeasureString(sub);

        Vector2 subPos = new Vector2(
            (viewport.Width - subSize.X) * 0.5f,
            py + 150f);

        sb.DrawString(
            _font,
            sub,
            subPos,
            new Color(170, 150, 170) * alpha);

        // ── Hint ───────────────────────────────────────────

        string hint = "Return to the main menu";

        Vector2 hintSize = _font.MeasureString(hint);

        Vector2 hintPos = new Vector2(
            (viewport.Width - hintSize.X) * 0.5f,
            py + ph - 85f);

        sb.DrawString(
            _font,
            hint,
            hintPos,
            new Color(120, 100, 120) * alpha);
    }

    DrawGothicButton(sb, pixel, _btnMainMenu);
}
    private void DrawDiamond(
        SpriteBatch sb,
        Texture2D pixel,
        int cx,
        int cy,
        int size,
        Color color)
    {
        for (int dy = -size; dy <= size; dy++)
        {
            int dx = size - Math.Abs(dy);

            sb.Draw(pixel,
                new Rectangle(cx - dx, cy + dy, dx * 2, 1),
                color);
        }
    }

    private void DrawGothicButton(
    SpriteBatch sb,
    Texture2D pixel,
    Button btn)
{
    var r = btn.Bounds;

    sb.Draw(pixel,
        new Rectangle(r.X, r.Y, r.Width, r.Height),
        new Color(122, 85, 144));

    sb.Draw(pixel,
        new Rectangle(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2),
        btn.IsHovered
            ? new Color(50, 25, 70, 240)
            : new Color(26, 13, 40, 230));

    const int B = 4;

    Color inner = new Color(90, 53, 112);

    sb.Draw(pixel,
        new Rectangle(r.X + B, r.Y + B, r.Width - B * 2, 1),
        inner);

    sb.Draw(pixel,
        new Rectangle(r.X + B, r.Y + r.Height - B, r.Width - B * 2, 1),
        inner);

    if (_font != null)
    {
        Vector2 size = _font.MeasureString(btn.Label);

        Vector2 pos = new Vector2(
            r.X + (r.Width - size.X) * 0.5f,
            r.Y + (r.Height - size.Y) * 0.5f);

        sb.DrawString(
            _font,
            btn.Label,
            pos + new Vector2(1, 2),
            new Color(0, 0, 0) * 0.5f);

        sb.DrawString(
            _font,
            btn.Label,
            pos,
            btn.IsHovered
                ? new Color(240, 210, 255)
                : new Color(200, 168, 220));
    }
}
}
using System;
using LILITH.Audio;
using MainEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LILITH.UI;

public class SettingsPanel
{
    private readonly Microsoft.Xna.Framework.Graphics.Texture2D _pixel;
    private readonly Microsoft.Xna.Framework.Graphics.SpriteFont _font;
    private readonly Action _onBack;

    private Button _btnMusicUp = null!;
    private Button _btnMusicDown = null!;
    private Button _btnSfxUp = null!;
    private Button _btnSfxDown = null!;
    private Button _btnBack = null!;

    public SettingsPanel(
        Microsoft.Xna.Framework.Graphics.Texture2D pixel,
        Microsoft.Xna.Framework.Graphics.SpriteFont font,
        Action onBack)
    {
        _pixel = pixel;
        _font = font;
        _onBack = onBack;
    }

    public void Initialize(Viewport vp)
    {
        int cx = vp.Width / 2;
        int cy = vp.Height / 2;

        int obw = 52;
        int obh = 40;

        int barLeft = cx - 55;
        int plusX = barLeft + 140;
        int minusX = barLeft - obw - 10;

        int musicY = cy - 20;
        int sfxY = cy + 50;

        _btnMusicDown = new Button(new Rectangle(minusX, musicY, obw, obh), "-");
        _btnMusicUp   = new Button(new Rectangle(plusX,   musicY, obw, obh), "+");
        _btnSfxDown   = new Button(new Rectangle(minusX, sfxY,   obw, obh), "-");
        _btnSfxUp     = new Button(new Rectangle(plusX,   sfxY,   obw, obh), "+");
        _btnBack      = new Button(new Rectangle(cx - 130, cy + 110, 260, 52), "BACK");

        StyleButton(_btnMusicDown);
        StyleButton(_btnMusicUp);
        StyleButton(_btnSfxDown);
        StyleButton(_btnSfxUp);
        StyleButton(_btnBack);

        _btnMusicUp.OnClick += () =>
        {
            HQ.Audio.SongVolume = MathHelper.Clamp(HQ.Audio.SongVolume + 0.1f, 0f, 1f);
        };

        _btnMusicDown.OnClick += () =>
        {
            HQ.Audio.SongVolume = MathHelper.Clamp(HQ.Audio.SongVolume - 0.1f, 0f, 1f);
        };

        _btnSfxUp.OnClick += () =>
        {
            HQ.Audio.SoundEffectVolume = MathHelper.Clamp(HQ.Audio.SoundEffectVolume + 0.1f, 0f, 1f);
        };

        _btnSfxDown.OnClick += () =>
        {
            HQ.Audio.SoundEffectVolume = MathHelper.Clamp(HQ.Audio.SoundEffectVolume - 0.1f, 0f, 1f);
        };

        _btnBack.OnClick += () =>
        {
            HQ.Audio.PlaySoundEffect(AudioAssets.PauseClose);
            _onBack?.Invoke();
        };
    }

    public void Update(GameTime gameTime)
    {
        _btnMusicUp.Update(gameTime);
        _btnMusicDown.Update(gameTime);
        _btnSfxUp.Update(gameTime);
        _btnSfxDown.Update(gameTime);
        _btnBack.Update(gameTime);
    }

    public void Draw(Viewport vp)
    {
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), new Color(0, 0, 0, 210));

        int pw = 420;
        int ph = 320;
        int px = (vp.Width - pw) / 2;
        int py = (vp.Height - ph) / 2;

        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px, py, pw, ph), new Color(122, 85, 144));
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px + 2, py + 2, pw - 4, ph - 4), new Color(18, 10, 30, 245));

        const int B = 4;
        Color innerBorder = new Color(90, 53, 112);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px + B, py + B, pw - B * 2, 1), innerBorder);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px + B, py + ph - B, pw - B * 2, 1), innerBorder);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px + B, py + B, 1, ph - B * 2), innerBorder);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px + pw - B, py + B, 1, ph - B * 2), innerBorder);

        DrawDiamond(px, py, 4, new Color(147, 112, 168, 180));
        DrawDiamond(px + pw, py, 4, new Color(147, 112, 168, 180));
        DrawDiamond(px, py + ph, 4, new Color(147, 112, 168, 180));
        DrawDiamond(px + pw, py + ph, 4, new Color(147, 112, 168, 180));

        string title = "SETTINGS";
        Vector2 titleSize = _font.MeasureString(title);
        Vector2 titlePos = new Vector2((vp.Width - titleSize.X) * 0.5f, py + 28f);

        HQ.SpriteBatch.DrawString(_font, title, titlePos + new Vector2(2, 2), new Color(0, 0, 0) * 0.6f);
        HQ.SpriteBatch.DrawString(_font, title, titlePos, new Color(212, 184, 224));

        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px + 24, py + 80, pw - 48, 1), new Color(122, 85, 144, 160));

        int cx = vp.Width / 2;
        int barLeft = cx - 55;
        int musicY = py + 115;
        int sfxY = py + 185;

        HQ.SpriteBatch.DrawString(_font, "MUSIC", new Vector2(px + 30, musicY + 8), new Color(200, 168, 220));
        DrawVolumeBar(barLeft, musicY + 10, 130, 22, HQ.Audio.SongVolume);
        DrawGothicButton(_btnMusicDown);
        DrawGothicButton(_btnMusicUp);

        HQ.SpriteBatch.DrawString(_font, "SFX", new Vector2(px + 30, sfxY + 8), new Color(200, 168, 220));
        DrawVolumeBar(barLeft, sfxY + 10, 130, 22, HQ.Audio.SoundEffectVolume);
        DrawGothicButton(_btnSfxDown);
        DrawGothicButton(_btnSfxUp);

        DrawGothicButton(_btnBack);
    }

    private void StyleButton(Button btn)
    {
        btn.ColorNormal  = new Color(26, 13, 40, 230);
        btn.ColorHover   = new Color(50, 25, 70, 240);
        btn.ColorPressed = new Color(15, 8, 25, 255);
        btn.ColorText    = new Color(200, 168, 220);
        btn.ColorShadow  = new Color(0, 0, 0, 0);
    }

    private void DrawVolumeBar(int x, int y, int w, int h, float ratio)
    {
        ratio = MathHelper.Clamp(ratio, 0f, 1f);

        HQ.SpriteBatch.Draw(_pixel, new Rectangle(x, y, w, h), new Color(10, 6, 18));
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(x, y, w, 1), new Color(90, 53, 112));
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(x, y + h - 1, w, 1), new Color(90, 53, 112));
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(x, y, 1, h), new Color(90, 53, 112));
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(x + w - 1, y, 1, h), new Color(90, 53, 112));

        int fillW = (int)((w - 4) * ratio);
        if (fillW > 0)
            HQ.SpriteBatch.Draw(_pixel, new Rectangle(x + 2, y + 2, fillW, h - 4), new Color(147, 112, 168));

        string pct = $"{(int)(ratio * 100)}%";
        Vector2 size = _font.MeasureString(pct);
        float scale = 0.6f;
        Vector2 pos = new Vector2(x + (w - size.X * scale) * 0.5f, y + (h - size.Y * scale) * 0.5f);

        HQ.SpriteBatch.DrawString(_font, pct, pos, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private void DrawGothicButton(Button btn)
    {
        var r = btn.Bounds;

        HQ.SpriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y, r.Width, r.Height), new Color(122, 85, 144));
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2),
            btn.IsHovered ? new Color(50, 25, 70, 240) : new Color(26, 13, 40, 230));

        const int B = 4;
        Color inner = new Color(90, 53, 112);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(r.X + B, r.Y + B, r.Width - B * 2, 1), inner);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(r.X + B, r.Y + r.Height - B, r.Width - B * 2, 1), inner);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(r.X + B, r.Y + B, 1, r.Height - B * 2), inner);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(r.X + r.Width - B, r.Y + B, 1, r.Height - B * 2), inner);

        Vector2 size = _font.MeasureString(btn.Label);
        Vector2 pos = new Vector2(r.X + (r.Width - size.X) * 0.5f, r.Y + (r.Height - size.Y) * 0.5f);

        HQ.SpriteBatch.DrawString(_font, btn.Label, pos + new Vector2(1, 2), new Color(0, 0, 0) * 0.5f);
        HQ.SpriteBatch.DrawString(_font, btn.Label, pos,
            btn.IsHovered ? new Color(240, 210, 255) : new Color(200, 168, 220));
    }

    private void DrawDiamond(int cx, int cy, int size, Color color)
    {
        for (int dy = -size; dy <= size; dy++)
        {
            int dx = size - Math.Abs(dy);
            HQ.SpriteBatch.Draw(_pixel, new Rectangle(cx - dx, cy + dy, dx * 2, 1), color);
        }
    }
}
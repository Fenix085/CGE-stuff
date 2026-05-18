using System;
using LILITH.UI;
using LILITH.Audio;
using MainEngine;
using MainEngine.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LILITH.Core.Scenes;

public class OptionsScene : Scene
{
    private readonly Func<Scene> _returnSceneFactory;

    private Texture2D  _pixel = null!;
    private SpriteFont _font  = null!;

    private Button _btnMusicUp    = null!;
    private Button _btnMusicDown  = null!;
    private Button _btnSfxUp      = null!;
    private Button _btnSfxDown    = null!;
    private Button _btnBack       = null!;

    // ── Stars background ───────────────────────────────────────────────────
    private (Vector2 pos, float size, float brightness)[] _stars = Array.Empty<(Vector2, float, float)>();
    private float _time;

    /// <summary>
    /// Creates an options scene that returns to the scene produced by
    /// <paramref name="returnSceneFactory"/> when the player clicks BACK.
    /// </summary>
    public OptionsScene(Func<Scene> returnSceneFactory)
    {
        _returnSceneFactory = returnSceneFactory;
    }

    // ── Initialization ────────────────────────────────────────────────────

    public override void Initialize()
    {
        HQ.ExitOnEscape = false;
        base.Initialize();
    }

    public override void LoadContent()
    {
        _pixel = new Texture2D(HQ.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _font = Content.Load<SpriteFont>("DefaultFont");

        if (AudioAssets.ButtonClick == null)
            AudioAssets.ButtonClick = HQ.Content.Load<SoundEffect>("audio/buttons");

        var vp = HQ.GraphicsDevice.Viewport;
        int vw = vp.Width;
        int vh = vp.Height;
        int cx = vw / 2;
        int cy = vh / 2;

        // ── Volume buttons ──
        int obw = 52, obh = 40;
        int barLeft = cx - 55;
        int plusX    = barLeft + 140;
        int minusX   = barLeft - obw - 10;
        int musicY  = cy - 30;
        int sfxY    = cy + 40;

        _btnMusicDown = new Button(new Rectangle(minusX, musicY, obw, obh), "-")
        {
            ColorNormal  = new Color(26,  13,  40,  230),
            ColorHover   = new Color(50,  25,  70,  240),
            ColorPressed = new Color(15,  8,   25,  255),
            ColorText    = new Color(200, 168, 220),
            ColorShadow  = new Color(0,   0,   0,   0),
        };
        _btnMusicUp = new Button(new Rectangle(plusX, musicY, obw, obh), "+")
        {
            ColorNormal  = new Color(26,  13,  40,  230),
            ColorHover   = new Color(50,  25,  70,  240),
            ColorPressed = new Color(15,  8,   25,  255),
            ColorText    = new Color(200, 168, 220),
            ColorShadow  = new Color(0,   0,   0,   0),
        };
        _btnSfxDown = new Button(new Rectangle(minusX, sfxY, obw, obh), "-")
        {
            ColorNormal  = new Color(26,  13,  40,  230),
            ColorHover   = new Color(50,  25,  70,  240),
            ColorPressed = new Color(15,  8,   25,  255),
            ColorText    = new Color(200, 168, 220),
            ColorShadow  = new Color(0,   0,   0,   0),
        };
        _btnSfxUp = new Button(new Rectangle(plusX, sfxY, obw, obh), "+")
        {
            ColorNormal  = new Color(26,  13,  40,  230),
            ColorHover   = new Color(50,  25,  70,  240),
            ColorPressed = new Color(15,  8,   25,  255),
            ColorText    = new Color(200, 168, 220),
            ColorShadow  = new Color(0,   0,   0,   0),
        };

        _btnBack = new Button(new Rectangle(cx - 130, cy + 110, 260, 52), "BACK")
        {
            ColorNormal  = new Color(26,  13,  40,  230),
            ColorHover   = new Color(50,  25,  70,  240),
            ColorPressed = new Color(15,  8,   25,  255),
            ColorText    = new Color(200, 168, 220),
            ColorShadow  = new Color(0,   0,   0,   0),
        };

        // ── Click handlers ──
        _btnMusicUp.OnClick   += () => { HQ.Audio.SongVolume        = MathHelper.Clamp(HQ.Audio.SongVolume + 0.1f, 0f, 1f);        PlayClick(); };
        _btnMusicDown.OnClick += () => { HQ.Audio.SongVolume        = MathHelper.Clamp(HQ.Audio.SongVolume - 0.1f, 0f, 1f);        PlayClick(); };
        _btnSfxUp.OnClick     += () => { HQ.Audio.SoundEffectVolume = MathHelper.Clamp(HQ.Audio.SoundEffectVolume + 0.1f, 0f, 1f); PlayClick(); };
        _btnSfxDown.OnClick   += () => { HQ.Audio.SoundEffectVolume = MathHelper.Clamp(HQ.Audio.SoundEffectVolume - 0.1f, 0f, 1f); PlayClick(); };
        _btnBack.OnClick      += () => { PlayClick(); HQ.ChangeScene(_returnSceneFactory()); };

        // ── Stars ──
        var rng = new Random(77);
        _stars = new (Vector2, float, float)[60];
        for (int i = 0; i < _stars.Length; i++)
        {
            _stars[i] = (
                new Vector2(rng.Next(vw), rng.Next(vh)),
                (float)(rng.NextDouble() * 1.5 + 0.5f),
                (float)(rng.NextDouble() * 0.6 + 0.4f)
            );
        }
    }

    // ── Update ────────────────────────────────────────────────────────────

    public override void Update(GameTime gameTime)
    {
        _time += (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Esc → back
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            PlayClick();
            HQ.ChangeScene(_returnSceneFactory());
            return;
        }

        _btnMusicUp.Update(gameTime);
        _btnMusicDown.Update(gameTime);
        _btnSfxUp.Update(gameTime);
        _btnSfxDown.Update(gameTime);
        _btnBack.Update(gameTime);
    }

    // ── Draw ──────────────────────────────────────────────────────────────

    public override void Draw(GameTime gameTime)
    {
        var vp = HQ.GraphicsDevice.Viewport;
        int vw = vp.Width;
        int vh = vp.Height;
        int cx = vw / 2;
        int cy = vh / 2;

        HQ.GraphicsDevice.Clear(new Color(10, 6, 20));

        HQ.SpriteBatch.Begin(
            sortMode:     SpriteSortMode.Deferred,
            blendState:   BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp);

        // ── Gradient ──
        int bands = 60;
        for (int i = 0; i < bands; i++)
        {
            float t  = (float)i / bands;
            int   bY = (int)(t * vh);
            int   bH = (int)(vh / (float)bands) + 2;
            byte  r  = (byte)MathHelper.Lerp(38,  6,  t);
            byte  g  = (byte)MathHelper.Lerp(22,  4,  t);
            byte  b  = (byte)MathHelper.Lerp(58,  12, t);
            HQ.SpriteBatch.Draw(_pixel, new Rectangle(0, bY, vw, bH), new Color(r, g, b));
        }

        // ── Stars ──
        foreach (var (pos, size, brightness) in _stars)
        {
            float twinkle = brightness * (0.7f + 0.3f * MathF.Sin(_time * 1.5f + pos.X));
            int   sz      = (int)MathF.Ceiling(size);
            HQ.SpriteBatch.Draw(_pixel,
                new Rectangle((int)pos.X, (int)pos.Y, sz, sz),
                new Color(232, 213, 240) * twinkle);
        }

        // ── Panel ──
        int pw = 400, ph = 300;
        int px = (vw - pw) / 2;
        int py = (vh - ph) / 2 - 20;

        DrawRect(px, py, pw, ph, new Color(122, 85, 144));
        DrawRect(px + 1, py + 1, pw - 2, ph - 2, new Color(18, 10, 30, 245));

        // Inner border
        const int B = 4;
        Color innerBorder = new Color(90, 53, 112);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px + B, py + B, pw - B * 2, 1), innerBorder);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px + B, py + ph - B, pw - B * 2, 1), innerBorder);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px + B, py + B, 1, ph - B * 2), innerBorder);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px + pw - B, py + B, 1, ph - B * 2), innerBorder);

        // Corner diamonds
        DrawDiamond(px,      py,      4, new Color(147, 112, 168, 180));
        DrawDiamond(px + pw, py,      4, new Color(147, 112, 168, 180));
        DrawDiamond(px,      py + ph, 4, new Color(147, 112, 168, 180));
        DrawDiamond(px + pw, py + ph, 4, new Color(147, 112, 168, 180));

        // Header
        string  title    = "OPTIONS";
        Vector2 titleSz  = _font.MeasureString(title);
        Vector2 titlePos = new Vector2((vw - titleSz.X) * 0.5f, py + 22);
        HQ.SpriteBatch.DrawString(_font, title, titlePos + new Vector2(2, 2), new Color(0, 0, 0) * 0.6f);
        HQ.SpriteBatch.DrawString(_font, title, titlePos, new Color(212, 184, 224));

        // Line under header
        HQ.SpriteBatch.Draw(_pixel,
            new Rectangle(px + 20, (int)(py + 22 + titleSz.Y + 4), pw - 40, 1),
            new Color(122, 85, 144, 160));

        // ── Music row ──
        int barLeft = cx - 55;
        int musicY  = cy - 30;
        int sfxY    = cy + 40;

        string musicLabel = "MUSIC";
        Vector2 mSize = _font.MeasureString(musicLabel);
        HQ.SpriteBatch.DrawString(_font, musicLabel,
            new Vector2(px + 25, musicY + (40 - mSize.Y) * 0.5f),
            new Color(200, 168, 220));
        DrawVolumeBar(barLeft, musicY + 8, 130, 22, HQ.Audio.SongVolume);
        DrawGothicButton(_btnMusicDown);
        DrawGothicButton(_btnMusicUp);

        // ── SFX row ──
        string sfxLabel = "SFX";
        Vector2 sSize = _font.MeasureString(sfxLabel);
        HQ.SpriteBatch.DrawString(_font, sfxLabel,
            new Vector2(px + 25, sfxY + (40 - sSize.Y) * 0.5f),
            new Color(200, 168, 220));
        DrawVolumeBar(barLeft, sfxY + 8, 130, 22, HQ.Audio.SoundEffectVolume);
        DrawGothicButton(_btnSfxDown);
        DrawGothicButton(_btnSfxUp);

        // ── Back button ──
        DrawGothicButton(_btnBack);

        // ── Ornaments above and below panel ──
        DrawDiamond(cx, py - 8,      6, new Color(147, 112, 168, 140));
        DrawDiamond(cx, py + ph + 8, 6, new Color(147, 112, 168, 140));

        HQ.SpriteBatch.End();
    }

    // ── Drawing helpers ───────────────────────────────────────────────────

    private void DrawVolumeBar(int x, int y, int w, int h, float ratio)
    {
        ratio = MathHelper.Clamp(ratio, 0f, 1f);

        // Background
        DrawRect(x, y, w, h, new Color(10, 6, 18));

        // Border
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(x, y, w, 1), new Color(90, 53, 112));
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(x, y + h - 1, w, 1), new Color(90, 53, 112));
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(x, y, 1, h), new Color(90, 53, 112));
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(x + w - 1, y, 1, h), new Color(90, 53, 112));

        // Fill
        int fillW = (int)((w - 4) * ratio);
        if (fillW > 0)
            DrawRect(x + 2, y + 2, fillW, h - 4, new Color(147, 112, 168));

        // Percentage
        string  pct   = $"{(int)(ratio * 100)}%";
        Vector2 size  = _font.MeasureString(pct);
        float   scale = 0.6f;
        Vector2 pos   = new Vector2(
            x + (w - size.X * scale) * 0.5f,
            y + (h - size.Y * scale) * 0.5f);
        HQ.SpriteBatch.DrawString(_font, pct, pos, Color.White,
            0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private void DrawGothicButton(Button btn)
    {
        var r = btn.Bounds;
        DrawRect(r.X, r.Y, r.Width, r.Height, new Color(122, 85, 144));
        DrawRect(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2,
            btn.IsHovered ? new Color(50, 25, 70, 240) : new Color(26, 13, 40, 230));

        const int B2 = 4;
        Color inner = new Color(90, 53, 112);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(r.X + B2, r.Y + B2, r.Width - B2 * 2, 1), inner);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(r.X + B2, r.Y + r.Height - B2, r.Width - B2 * 2, 1), inner);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(r.X + B2, r.Y + B2, 1, r.Height - B2 * 2), inner);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(r.X + r.Width - B2, r.Y + B2, 1, r.Height - B2 * 2), inner);

        int mid = r.Y + r.Height / 2;
        DrawDiamond(r.X + 15,           mid, 5, new Color(147, 112, 168, 180));
        DrawDiamond(r.X + r.Width - 15, mid, 5, new Color(147, 112, 168, 180));

        Vector2 size = _font.MeasureString(btn.Label);
        Vector2 pos  = new Vector2(
            r.X + (r.Width  - size.X) * 0.5f,
            r.Y + (r.Height - size.Y) * 0.5f);
        HQ.SpriteBatch.DrawString(_font, btn.Label, pos + new Vector2(1, 2), new Color(0, 0, 0) * 0.5f);
        HQ.SpriteBatch.DrawString(_font, btn.Label, pos,
            btn.IsHovered ? new Color(240, 210, 255) : new Color(200, 168, 220));
    }

    private void DrawRect(int x, int y, int w, int h, Color color) =>
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(x, y, w, h), color);

    private void DrawDiamond(int cx, int cy, int size, Color color)
    {
        for (int dy = -size; dy <= size; dy++)
        {
            int dx = size - Math.Abs(dy);
            HQ.SpriteBatch.Draw(_pixel, new Rectangle(cx - dx, cy + dy, dx * 2, 1), color);
        }
    }

    private void PlayClick()
    {
        if (AudioAssets.ButtonClick != null)
            HQ.Audio.PlaySoundEffect(AudioAssets.ButtonClick, 0.45f, 0f, 0f, false);
    }

    // ── Cleanup ───────────────────────────────────────────────────────────

    protected override void Dispose(bool disposing)
    {
        if (disposing) _pixel?.Dispose();
        base.Dispose(disposing);
    }
}
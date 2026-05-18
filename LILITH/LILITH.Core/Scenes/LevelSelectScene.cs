using System;
using LILITH.UI;
using MainEngine;
using MainEngine.Input;
using MainEngine.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Microsoft.Xna.Framework.Audio;
using LILITH.Audio;
using Microsoft.Xna.Framework.Media;

namespace LILITH.Core.Scenes;

public class LevelSelectScene : Scene
{
    private Texture2D   _pixel = null!;
    private SpriteFont? _font;

    private Button _btnLevel1 = null!;
    private Button _btnLevel2 = null!;
    private Button _btnBack   = null!;
    private bool _pendingSceneChange;
    private float _sceneChangeTimer;
    private Action? _nextAction;

    private int _menuIndex = 0;
    private bool _usingGamepad = false;

    // Stars
    private (Vector2 pos, float size, float brightness)[] _stars = Array.Empty<(Vector2, float, float)>();
    private float _time;

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

        
        
        var vp = HQ.GraphicsDevice.Viewport;
        int vw = vp.Width;
        int vh = vp.Height;
        int cx = vw / 2;
        int cy = vh / 2;
        int bw = 260, bh = 52, gap = 16;

        _btnLevel1 = new Button(new Rectangle(cx - bw / 2, cy - bh / 2, bw, bh), "WAVES MODE")
        {
            ColorNormal  = new Color(26,  13,  40,  230),
            ColorHover   = new Color(50,  25,  70,  240),
            ColorPressed = new Color(15,  8,   25,  255),
            ColorText    = new Color(200, 168, 220),
            ColorShadow  = new Color(0,   0,   0,   0),
        };

        _btnLevel2 = new Button(new Rectangle(cx - bw / 2, cy + bh / 2 + gap, bw, bh), "ENDLESS MODE")
        {
            ColorNormal  = new Color(26,  13,  40,  230),
            ColorHover   = new Color(50,  25,  70,  240),
            ColorPressed = new Color(15,  8,   25,  255),
            ColorText    = new Color(200, 168, 220),
            ColorShadow  = new Color(0,   0,   0,   0),
        };

        _btnBack = new Button(new Rectangle(cx - bw / 2, cy + bh / 2 + gap + bh + gap, bw, bh), "BACK")
        {
            ColorNormal  = new Color(26,  13,  40,  230),
            ColorHover   = new Color(50,  25,  70,  240),
            ColorPressed = new Color(15,  8,   25,  255),
            ColorText    = new Color(200, 168, 220),
            ColorShadow  = new Color(0,   0,   0,   0),
        };

        _btnLevel1.OnClick += () =>
        {
            HQ.Audio.PlaySoundEffect(
                AudioAssets.ButtonClick,
                0.45f,
                0f,
                0f,
                false);

            _pendingSceneChange = true;
            _sceneChangeTimer = 0.12f;

            _nextAction = () =>
            {
                HQ.ChangeScene(new GameScene());
            };
        };

        _btnLevel2.OnClick += () =>
        {
            HQ.Audio.PlaySoundEffect(
                AudioAssets.ButtonClick,
                0.45f,
                0f,
                0f,
                false);

            _pendingSceneChange = true;
            _sceneChangeTimer = 0.12f;

            _nextAction = () =>
            {
                HQ.ChangeScene(new EndlessScene());
            };
        };

        _btnBack.OnClick += () =>
        {
            HQ.Audio.PlaySoundEffect(
                AudioAssets.ButtonClick,
                0.45f,
                0f,
                0f,
                false);

            _pendingSceneChange = true;
            _sceneChangeTimer = 0.12f;

            _nextAction = () =>
            {
                HQ.ChangeScene(new MainMenuScene());
            };
        };

        // Stars
        var rng = new Random(99);
        _stars = new (Vector2, float, float)[80];
        for (int i = 0; i < _stars.Length; i++)
        {
            _stars[i] = (
                new Vector2(rng.Next(vw), rng.Next(vh / 2)),
                (float)(rng.NextDouble() * 1.5 + 0.5f),
                (float)(rng.NextDouble() * 0.6 + 0.4f)
            );
        }
        HQ.Audio.PlaySong(AudioAssets.MainMenuMusic);
    }

    public override void Update(GameTime gameTime)
    {
        _time += (float)gameTime.ElapsedGameTime.TotalSeconds;

        _btnLevel1.Update(gameTime);
        _btnLevel2.Update(gameTime);
        _btnBack.Update(gameTime);
        
        if (_pendingSceneChange)
        {
            _sceneChangeTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_sceneChangeTimer <= 0f)
            {
                _pendingSceneChange = false;
                _nextAction?.Invoke();
                _nextAction = null;
            }
        }
        
        KeyboardState keys = Keyboard.GetState();
        if (keys.IsKeyDown(Keys.Escape) && _prevKeys.IsKeyUp(Keys.Escape))
        var pad = HQ.Input.GamePads[0];

        if (pad.WasButtonJustPressed(Buttons.DPadUp)
            || pad.WasButtonJustPressed(Buttons.DPadDown)
            || pad.WasButtonJustPressed(Buttons.A)
            || pad.WasButtonJustPressed(Buttons.B))
            _usingGamepad = true;

        if (HQ.Input.Mouse.WasMoved
            || HQ.Input.Mouse.WasButtonJustPressed(MouseButton.Left))
            _usingGamepad = false;

        if (_usingGamepad)
        {
            if (pad.WasButtonJustPressed(Buttons.DPadUp)
                || pad.WasButtonJustPressed(Buttons.LeftThumbstickUp))
                _menuIndex = Math.Max(0, _menuIndex - 1);

            if (pad.WasButtonJustPressed(Buttons.DPadDown)
                || pad.WasButtonJustPressed(Buttons.LeftThumbstickDown))
                _menuIndex = Math.Min(1, _menuIndex + 1);

            if (pad.WasButtonJustPressed(Buttons.A))
            {
                if (_menuIndex == 0) HQ.ChangeScene(new GameScene());
                else                 HQ.ChangeScene(new MainMenuScene());
            }

            if (pad.WasButtonJustPressed(Buttons.B))
                HQ.ChangeScene(new MainMenuScene());
        }

        _btnLevel1.ForceHover = _usingGamepad && _menuIndex == 0;
        _btnBack.ForceHover   = _usingGamepad && _menuIndex == 1;

        _btnLevel1.Update(gameTime);
        _btnBack.Update(gameTime);

        // Escape — back (replaces raw Keyboard.GetState)
        if (HQ.Input.Keyboard.WasKeyJustPressed(Keys.Escape))
            HQ.ChangeScene(new MainMenuScene());
    }

    public override void Draw(GameTime gameTime)
    {
        var vp = HQ.GraphicsDevice.Viewport;
        int vw = vp.Width;
        int vh = vp.Height;
        int cx = vw / 2;

        HQ.GraphicsDevice.Clear(new Color(10, 6, 20));

        HQ.SpriteBatch.Begin(
            sortMode:     SpriteSortMode.Deferred,
            blendState:   BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp);

        // ── Gradient Sky ─────────────────────────────────────────────────
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

        // ── Stars ────────────────────────────────────────────────────────
        foreach (var (pos, size, brightness) in _stars)
        {
            float twinkle = brightness * (0.7f + 0.3f * MathF.Sin(_time * 1.5f + pos.X));
            int   sz      = (int)MathF.Ceiling(size);
            HQ.SpriteBatch.Draw(_pixel,
                new Rectangle((int)pos.X, (int)pos.Y, sz, sz),
                new Color(232, 213, 240) * twinkle);
        }

        // ── Clouds ────────────────────────────────────────────────────────
        Color cloud = new Color(30, 18, 50);
        DrawFogEllipse(cx, (int)(vh * 0.12f), 120, 18, cloud * 0.5f);
        DrawFogEllipse((int)(vw * 0.18f), (int)(vh * 0.10f), 80, 12, cloud * 0.4f);
        DrawFogEllipse((int)(vw * 0.78f), (int)(vh * 0.15f), 110, 16, cloud * 0.5f);
        DrawFogEllipse((int)(vw * 0.08f), (int)(vh * 0.22f), 90, 14, cloud * 0.35f);
        DrawFogEllipse((int)(vw * 0.90f), (int)(vh * 0.20f), 100, 15, cloud * 0.35f);

        // ── Far Hills ─────────────────────────────────────────────────
        int   horizon = (int)(vh * 0.62f);
        Color far     = new Color(18, 10, 32);
        DrawFilledTriangle(new Vector2(vw * 0.05f, horizon), new Vector2(vw * 0.22f, horizon - 80), new Vector2(vw * 0.38f, horizon), far);
        DrawFilledTriangle(new Vector2(vw * 0.35f, horizon), new Vector2(vw * 0.50f, horizon - 55), new Vector2(vw * 0.65f, horizon), far);
        DrawFilledTriangle(new Vector2(vw * 0.62f, horizon), new Vector2(vw * 0.78f, horizon - 70), new Vector2(vw * 0.95f, horizon), far);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(0, horizon, vw, vh - horizon), far);

        // ── Mountains ─────────────────────────────────────────────────
        Color mtn      = new Color(8, 5, 14);
        int   mtnLine  = (int)(vh * 0.68f);
        DrawFilledTriangle(new Vector2(0,          mtnLine), new Vector2(0,          vh),           new Vector2(vw * 0.28f, vh),       mtn);
        DrawFilledTriangle(new Vector2(0,          mtnLine), new Vector2(vw * 0.15f, mtnLine - 60), new Vector2(vw * 0.32f, mtnLine),  mtn);
        DrawFilledTriangle(new Vector2(vw * 0.15f, mtnLine - 60), new Vector2(vw * 0.28f, mtnLine), new Vector2(vw * 0.42f, mtnLine),  mtn);
        DrawFilledTriangle(new Vector2(vw,         mtnLine), new Vector2(vw,          vh),           new Vector2(vw * 0.72f, vh),       mtn);
        DrawFilledTriangle(new Vector2(vw,         mtnLine), new Vector2(vw * 0.85f, mtnLine - 70), new Vector2(vw * 0.68f, mtnLine),  mtn);
        DrawFilledTriangle(new Vector2(vw * 0.85f, mtnLine - 70), new Vector2(vw * 0.72f, mtnLine), new Vector2(vw * 0.58f, mtnLine),  mtn);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(0, mtnLine, vw, vh - mtnLine + 2), mtn);

        // ── Castle ─────────────────────────────────────────────────────────
        Color cc  = new Color(9, 6, 18);
        int   bx  = (int)(vw * 0.72f);
        int   by  = mtnLine - 100;
        DrawRect(bx,      by - 30, 8, 40 + (vh - mtnLine), cc);
        DrawRect(bx + 16, by - 45, 8, 55 + (vh - mtnLine), cc);
        DrawRect(bx + 32, by - 25, 8, 35 + (vh - mtnLine), cc);
        DrawRect(bx,      by - 38, 3, 10, cc); DrawRect(bx + 5,  by - 38, 3, 10, cc);
        DrawRect(bx + 16, by - 53, 3, 10, cc); DrawRect(bx + 21, by - 53, 3, 10, cc);
        DrawRect(bx + 32, by - 33, 3, 10, cc); DrawRect(bx + 37, by - 33, 3, 10, cc);
        DrawRect(bx - 5,  by + 15, 52, vh - by, cc);

        // ── Columns ───────────────────────────────────────────────────────
        DrawColumn(70,       195, vh);
        DrawColumn(vw - 98,  195, vh);

        // ── Fog ─────────────────────────────────────────────────────────
        for (int i = 0; i < 5; i++)
        {
            float fy    = vh * 0.82f + i * 18;
            float alpha = 0.15f + i * 0.06f;
            HQ.SpriteBatch.Draw(_pixel,
                new Rectangle(0, (int)fy, vw, 22),
                new Color(20, 10, 35) * alpha);
        }
        DrawFogEllipse(cx - 250, (int)(vh * 0.88f), 300, 45, new Color(25, 12, 40) * 0.35f);
        DrawFogEllipse(cx + 100, (int)(vh * 0.85f), 260, 38, new Color(25, 12, 40) * 0.28f);

        // ── Plants ──────────────────────────────────────────────────────
        DrawPlants(30,       vh);
        DrawPlants(vw - 30,  vh, mirror: true);

        // ── Header SELECT LEVEL ────────────────────────────────────────
        if (_font != null)
        {
            string  title = "SELECT LEVEL";
            Vector2 size  = _font.MeasureString(title);
            float   scale = 2.0f;
            Vector2 pos   = new Vector2((vw - size.X * scale) * 0.5f, vh * 0.10f);

            HQ.SpriteBatch.DrawString(_font, title, pos + new Vector2(3, 4),
                new Color(0, 0, 0) * 0.6f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            HQ.SpriteBatch.DrawString(_font, title, pos, new Color(212, 184, 224),
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            // Decorative line under the header
            int lineW = (int)(size.X * scale + 40);
            HQ.SpriteBatch.Draw(_pixel,
                new Rectangle((int)((vw - lineW) * 0.5f), (int)(vh * 0.10f + size.Y * scale + 6), lineW, 1),
                new Color(122, 85, 144, 160));
        }

        // ── Buttons ────────────────────────────────────────────────────────
        DrawGothicButton(_btnLevel1);
        DrawGothicButton(_btnLevel2);
        DrawGothicButton(_btnBack);

        HQ.SpriteBatch.End();
    }

    // ── Gothic Button ─────────────────────────────────────────────────

    private void DrawGothicButton(Button btn)
    {
        var r = btn.Bounds;
        DrawRect(r.X, r.Y, r.Width, r.Height, new Color(122, 85, 144));
        DrawRect(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2,
            btn.IsHovered ? new Color(50, 25, 70, 240) : new Color(26, 13, 40, 230));
        DrawRectOutline(r.X + 4, r.Y + 4, r.Width - 8, r.Height - 8, new Color(90, 53, 112));

        int mid = r.Y + r.Height / 2;
        DrawDiamond(r.X + 15, mid, 5, new Color(147, 112, 168, 180));
        DrawDiamond(r.X + r.Width - 15, mid, 5, new Color(147, 112, 168, 180));

        if (_font != null)
        {
            Vector2 size = _font.MeasureString(btn.Label);
            Vector2 pos  = new Vector2(
                r.X + (r.Width  - size.X) * 0.5f,
                r.Y + (r.Height - size.Y) * 0.5f);
            HQ.SpriteBatch.DrawString(_font, btn.Label, pos + new Vector2(1, 2), new Color(0, 0, 0) * 0.5f);
            HQ.SpriteBatch.DrawString(_font, btn.Label, pos,
                btn.IsHovered ? new Color(240, 210, 255) : new Color(200, 168, 220));
        }
    }

    // ── Geometry ─────────────────────────────────────────────────────────

    private void DrawRect(int x, int y, int w, int h, Color color) =>
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(x, y, w, h), color);

    private void DrawRectOutline(int x, int y, int w, int h, Color color)
    {
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(x,     y,     w, 1), color);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(x,     y+h-1, w, 1), color);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(x,     y,     1, h), color);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(x+w-1, y,     1, h), color);
    }

    private void DrawDiamond(int cx, int cy, int size, Color color)
    {
        for (int dy = -size; dy <= size; dy++)
        {
            int dx = size - Math.Abs(dy);
            HQ.SpriteBatch.Draw(_pixel, new Rectangle(cx - dx, cy + dy, dx * 2, 1), color);
        }
    }

    private void DrawFogEllipse(int cx, int cy, int rx, int ry, Color color)
    {
        for (int dy = -ry; dy <= ry; dy++)
        {
            float ratio = (float)Math.Sqrt(1.0 - (double)(dy * dy) / (ry * ry));
            int   dx    = (int)(rx * ratio);
            float alpha = 1f - MathF.Abs((float)dy / ry);
            HQ.SpriteBatch.Draw(_pixel, new Rectangle(cx - dx, cy + dy, dx * 2, 1), color * alpha);
        }
    }

    private void DrawColumn(int x, int topY, int vh)
    {
        Color c = new Color(10, 6, 18);
        DrawRect(x - 22, topY,      44, 10, c);
        DrawRect(x - 18, topY + 10, 36, 14, c);
        DrawRect(x - 15, topY + 24, 30, 8,  c);
        DrawRect(x - 14, topY + 32, 28, vh - topY - 32, c);
        Color groove = new Color(20, 12, 35) * 0.5f;
        for (int i = -10; i <= 10; i += 7)
            DrawRect(x + i, topY + 35, 1, vh - topY - 60, groove);
    }

    private void DrawPlants(int baseX, int vh, bool mirror = false)
    {
        Color c = new Color(6, 4, 10);
        int   m = mirror ? -1 : 1;
        for (int i = 0; i < 5; i++)
        {
            int stemX = baseX + m * (i * 14 - 28);
            int stemH = 25 + i * 8;
            int stemY = vh - stemH - 10;
            DrawRect(stemX, stemY, 2, stemH, c);
            for (int j = 0; j < 3; j++)
            {
                int lx = stemX + m * (j - 1) * 3;
                int ly = stemY + j * (stemH / 3);
                DrawRect(lx, ly, 2 + j, 1, c);
            }
        }
        DrawRect(baseX + m * (-35), vh - 15, 70, 20, c);
    }

    private void DrawFilledTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
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
                HQ.SpriteBatch.Draw(_pixel, new Rectangle(left, y, right - left, 1), color);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _pixel?.Dispose();
        base.Dispose(disposing);
    }
}

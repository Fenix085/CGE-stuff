using System;
using LILITH.UI;
using MainEngine;
using MainEngine.Input;
using MainEngine.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LILITH.Core.Scenes;

/// <summary>
/// Экран выбора уровня. Пока один уровень — Level 1.
/// </summary>
public class LevelSelectScene : Scene
{
    private Texture2D   _pixel = null!;
    private SpriteFont? _font;

    private Button _btnLevel1 = null!;
    private Button _btnBack   = null!;

    private int _menuIndex = 0;
    private bool _usingGamepad = false;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void LoadContent()
    {
        _pixel = new Texture2D(HQ.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _font = Content.Load<SpriteFont>("DefaultFont");

        int cx  = HQ.GraphicsDevice.Viewport.Width  / 2;
        int cy  = HQ.GraphicsDevice.Viewport.Height / 2;
        int bw  = 220;
        int bh  = 55;

        _btnLevel1 = new Button(new Rectangle(cx - bw / 2, cy - bh / 2, bw, bh), "LEVEL 1");
        _btnBack   = new Button(new Rectangle(cx - bw / 2, cy + bh + 20, bw, 45), "BACK");

        _btnLevel1.OnClick += () => HQ.ChangeScene(new GameScene());
        _btnBack.OnClick   += () => HQ.ChangeScene(new MainMenuScene());
    }

    public override void Update(GameTime gameTime)
    {
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
        HQ.GraphicsDevice.Clear(new Color(15, 15, 25));

        HQ.SpriteBatch.Begin(
            sortMode:     SpriteSortMode.Deferred,
            blendState:   BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp);

        // Заголовок
        if (_font != null)
        {
            string   title    = "SELECT LEVEL";
            Vector2  size     = _font.MeasureString(title);
            Vector2  pos      = new Vector2(
                (HQ.GraphicsDevice.Viewport.Width - size.X) * 0.5f,
                HQ.GraphicsDevice.Viewport.Height * 0.2f);
            HQ.SpriteBatch.DrawString(_font, title, pos + new Vector2(2, 2), Color.Black);
            HQ.SpriteBatch.DrawString(_font, title, pos, Color.White);
        }

        _btnLevel1.Draw(HQ.SpriteBatch, _pixel, _font);
        _btnBack.Draw(HQ.SpriteBatch, _pixel, _font);

        HQ.SpriteBatch.End();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _pixel?.Dispose();
        base.Dispose(disposing);
    }
}

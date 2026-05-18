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
/// Главное меню игры. Кнопки: Play (→ выбор уровня), Exit.
/// </summary>
public class MainMenuScene : Scene
{
    private Texture2D   _pixel = null!;
    private SpriteFont? _font;

    private Button _btnPlay = null!;
    private Button _btnExit = null!;

    private int _menuIndex = 0;
    private bool _usingGamepad;

    public override void Initialize()
    {
        HQ.ExitOnEscape = false; // чтобы Esc не закрывал игру
        base.Initialize();
    }

    public override void LoadContent()
    {
        _pixel = new Texture2D(HQ.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _font = Content.Load<SpriteFont>("DefaultFont");

        int cx  = HQ.GraphicsDevice.Viewport.Width  / 2;
        int cy  = HQ.GraphicsDevice.Viewport.Height / 2;
        int bw  = 200;
        int bh  = 50;
        int gap = 20;

        _btnPlay = new Button(new Rectangle(cx - bw / 2, cy - bh - gap / 2, bw, bh), "PLAY");
        _btnExit = new Button(new Rectangle(cx - bw / 2, cy + gap / 2,       bw, bh), "EXIT");

        _btnPlay.OnClick += () => HQ.ChangeScene(new LevelSelectScene());
        _btnExit.OnClick += () => HQ.Instance.Exit();
    }

    public override void Update(GameTime gameTime)
    {
        var pad = HQ.Input.GamePads[0];

        if (pad.WasButtonJustPressed(Buttons.DPadUp)
            || pad.WasButtonJustPressed(Buttons.DPadDown)
            || pad.WasButtonJustPressed(Buttons.A))
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
                if (_menuIndex == 0) HQ.ChangeScene(new LevelSelectScene());
                else                 HQ.Instance.Exit();
            }
        }

        _btnPlay.ForceHover = _usingGamepad && _menuIndex == 0;
        _btnExit.ForceHover = _usingGamepad && _menuIndex == 1;

        _btnPlay.Update(gameTime);
        _btnExit.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        HQ.GraphicsDevice.Clear(new Color(15, 15, 25));

        HQ.SpriteBatch.Begin(
            sortMode:     SpriteSortMode.Deferred,
            blendState:   BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp);

        if (_font != null)
        {
            string  title    = "LILITH";
            Vector2 size     = _font.MeasureString(title);
            Vector2 titlePos = new Vector2(
                (HQ.GraphicsDevice.Viewport.Width  - size.X) * 0.5f,
                HQ.GraphicsDevice.Viewport.Height  * 0.25f);

            HQ.SpriteBatch.DrawString(_font, title, titlePos + new Vector2(2, 2), Color.Black);
            HQ.SpriteBatch.DrawString(_font, title, titlePos, new Color(200, 100, 255));
        }

        _btnPlay.Draw(HQ.SpriteBatch, _pixel, _font);
        _btnExit.Draw(HQ.SpriteBatch, _pixel, _font);

        HQ.SpriteBatch.End();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _pixel?.Dispose();
        base.Dispose(disposing);
    }
}

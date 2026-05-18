using System;
using LILITH.Core.Scenes;
using MainEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LILITH.UI;

public class VictoryScreen
{
    public bool IsVisible { get; private set; }

    private Button      _btnMainMenu = null!;
    private float       _showTimer;
    private SpriteFont? _font;

    private const float SHOW_DURATION = 0.5f;

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

        if (_showTimer < SHOW_DURATION)
            _showTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

        _btnMainMenu.Update(gameTime);
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, Viewport viewport)
    {
        if (!IsVisible) return;

        float alpha = MathHelper.Clamp(_showTimer / SHOW_DURATION, 0f, 1f);

        
        sb.Draw(pixel,
            new Rectangle(0, 0, viewport.Width, viewport.Height),
            new Color(0, 0, 0, 180) * alpha);

        
        int pw = 340, ph = 200;
        int px = (viewport.Width  - pw) / 2;
        int py = (viewport.Height - ph) / 2;

        sb.Draw(pixel,
            new Rectangle(px, py, pw, ph),
            new Color(20, 10, 10, 240) * alpha);

        
        const int B      = 2;
        Color     border = new Color(200, 60, 60, 220);
        sb.Draw(pixel, new Rectangle(px,        py,        pw, B),  border * alpha);
        sb.Draw(pixel, new Rectangle(px,        py+ph-B,   pw, B),  border * alpha);
        sb.Draw(pixel, new Rectangle(px,        py,        B,  ph), border * alpha);
        sb.Draw(pixel, new Rectangle(px+pw-B,   py,        B,  ph), border * alpha);

        if (_font != null)
        {
            
            string  title    = "YOU WERE EATEN";
            Vector2 titleSz  = _font.MeasureString(title);
            Vector2 titlePos = new Vector2(
                (viewport.Width - titleSz.X) * 0.5f,
                py + 40f);

            sb.DrawString(_font, title, titlePos + new Vector2(2, 2),
                Color.Black * alpha);
            sb.DrawString(_font, title, titlePos,
                new Color(255, 80, 80) * alpha);

            
            string  sub    = "Better luck next time...";
            Vector2 subSz  = _font.MeasureString(sub);
            Vector2 subPos = new Vector2(
                (viewport.Width - subSz.X) * 0.5f,
                py + 40f + titleSz.Y + 10f);

            sb.DrawString(_font, sub, subPos,
                Color.Gray * alpha);
        }

        _btnMainMenu.Draw(sb, pixel, _font);
    }
}
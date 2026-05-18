using LILITH.UI;
using MainEngine;
using MainEngine.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LILITH.Core.Scenes;

public class LevelSelectScene : Scene
{
    private Texture2D   _pixel = null!;
    private SpriteFont? _font;

    private Button _btnLevel1 = null!;
    private Button _btnLevel2 = null!;
    private Button _btnBack   = null!;

    private KeyboardState _prevKeys;

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

        _btnLevel1 = new Button(new Rectangle(cx - bw / 2, cy - bh - 10,      bw, bh), "WAVES CLEAR");
        _btnLevel2 = new Button(new Rectangle(cx - bw / 2, cy,                 bw, bh), "ENDLESS");
        _btnBack   = new Button(new Rectangle(cx - bw / 2, cy + bh + 10,       bw, 45), "BACK");
        
        _btnLevel1.OnClick += () => HQ.ChangeScene(new GameScene());
        _btnBack.OnClick   += () => HQ.ChangeScene(new MainMenuScene());
        _btnLevel2.OnClick += () => HQ.ChangeScene(new EndlessScene());     
        
        
    }

    public override void Update(GameTime gameTime)
    {
        _btnLevel1.Update(gameTime);
        _btnLevel2.Update(gameTime);
        _btnBack.Update(gameTime);

        // ESC back to menu
        KeyboardState keys = Keyboard.GetState();
        if (keys.IsKeyDown(Keys.Escape) && _prevKeys.IsKeyUp(Keys.Escape))
            HQ.ChangeScene(new MainMenuScene());
        _prevKeys = keys;
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
        _btnLevel2.Draw(HQ.SpriteBatch, _pixel, _font);
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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MainEngine;
using MainEngine.Scenes;

namespace TheNewBeginning.Scenes;

public class TitleScene : Scene
{
    private const string PROJECT_TEXT = "Project";
private const string NAME_TEXT = "Name";
private const string PRESS_ENTER_TEXT = "Press Enter To Start";

// The font to use to render normal text.
private SpriteFont _font;

// The font used to render the title text.
private SpriteFont _font5x;

// The position to draw the dungeon text at.
private Vector2 _projectTextPos;

// The origin to set for the dungeon text.
private Vector2 _projectTextOrigin;

// The position to draw the slime text at.
private Vector2 _nameTextPos;

// The origin to set for the slime text.
private Vector2 _nameTextOrigin;

// The position to draw the press enter text at.
private Vector2 _pressEnterPos;

// The origin to set for the press enter text when drawing it.
private Vector2 _pressEnterOrigin;

public override void Initialize()
{
    // LoadContent is called during base.Initialize().
    base.Initialize();

    // While on the title screen, we can enable exit on escape so the player
    // can close the game by pressing the escape key.
    HQ.ExitOnEscape = true;

    // Set the position and origin for the Dungeon text.
    Vector2 size = _font5x.MeasureString(PROJECT_TEXT);
    _projectTextPos = new Vector2(640, 100);
    _projectTextOrigin = size * 0.5f;

    // Set the position and origin for the Slime text.
    size = _font5x.MeasureString(NAME_TEXT);
    _nameTextPos = new Vector2(757, 207);
    _nameTextOrigin = size * 0.5f;

    // Set the position and origin for the press enter text.
    size = _font.MeasureString(PRESS_ENTER_TEXT);
    _pressEnterPos = new Vector2(640, 620);
    _pressEnterOrigin = size * 0.5f;
}

public override void LoadContent()
{
    // Load the font for the standard text.
    _font = HQ.Content.Load<SpriteFont>("fonts/04B_30");

    // Load the font for the title text.
    _font5x = Content.Load<SpriteFont>("fonts/04B_30_5x");
}

public override void Update(GameTime gameTime)
{
    // If the user presses enter, switch to the game scene.
    if (HQ.Input.Keyboard.WasKeyJustPressed(Keys.Enter))
    {
        HQ.ChangeScene(new GameScene());
    }
}
public override void Draw(GameTime gameTime)
{
    HQ.GraphicsDevice.Clear(new Color(32, 40, 78, 255));

    // Begin the sprite batch to prepare for rendering.
    HQ.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

    // The color to use for the drop shadow text.
    Color dropShadowColor = Color.Black * 0.5f;

    // Draw the Dungeon text slightly offset from it is original position and
    // with a transparent color to give it a drop shadow.
    HQ.SpriteBatch.DrawString(_font5x, PROJECT_TEXT, _projectTextPos + new Vector2(10, 10), dropShadowColor, 0.0f, _projectTextOrigin, 1.0f, SpriteEffects.None, 1.0f);

    // Draw the Dungeon text on top of that at its original position.
    HQ.SpriteBatch.DrawString(_font5x, PROJECT_TEXT, _projectTextPos, Color.White, 0.0f, _projectTextOrigin, 1.0f, SpriteEffects.None, 1.0f);

    // Draw the Slime text slightly offset from it is original position and
    // with a transparent color to give it a drop shadow.
    HQ.SpriteBatch.DrawString(_font5x, NAME_TEXT, _nameTextPos + new Vector2(10, 10), dropShadowColor, 0.0f,  _nameTextOrigin, 1.0f, SpriteEffects.None, 1.0f);

    // Draw the Slime text on top of that at its original position.
    HQ.SpriteBatch.DrawString(_font5x, NAME_TEXT, _nameTextPos, Color.White, 0.0f,  _nameTextOrigin, 1.0f, SpriteEffects.None, 1.0f);

    // Draw the press enter text.
    HQ.SpriteBatch.DrawString(_font, PRESS_ENTER_TEXT, _pressEnterPos, Color.White, 0.0f, _pressEnterOrigin, 1.0f, SpriteEffects.None, 0.0f);

    // Always end the sprite batch when finished.
    HQ.SpriteBatch.End();
}

}

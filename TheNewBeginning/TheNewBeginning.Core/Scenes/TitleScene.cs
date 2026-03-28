using System;
using TheNewBeginning.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using MainEngine;
using MainEngine.Scenes;
using MainEngine.Graphics;
using MonoGameGum;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;

namespace TheNewBeginning.Scenes;

public class TitleScene : Scene
{
    private const string PROJECT_TEXT1 = "TheNew";
private const string PROJECT_TEXT2 = "Beginning";
private const string PRESS_ENTER_TEXT = "Press Enter To Start";

private SpriteFont _font;

private SpriteFont _font5x;

private Vector2 _backgroundOffset;
private Rectangle _backgroundDestination;
private Vector2 _project1TextPos;

private Vector2 _project1TextOrigin;

private Vector2 _project2TextPos;

private Vector2 _project2TextOrigin;

private SoundEffect _uiSoundEffect;
private Panel _titleScreenButtonsPanel;
private Panel _optionsPanel;
private AnimatedButton _optionsButton;

private AnimatedButton _optionsBackButton;

private TextureAtlas _atlas;


public override void Initialize()
{
    base.Initialize();

    HQ.ExitOnEscape = true;

    Vector2 size = _font5x.MeasureString(PROJECT_TEXT1);
    _project1TextPos = new Vector2(640, 100);
    _project1TextOrigin = size * 0.5f;

    size = _font5x.MeasureString(PROJECT_TEXT2);
    _project2TextPos = new Vector2(757, 207);
    _project2TextOrigin = size * 0.5f;

    _backgroundOffset = Vector2.Zero;

    _backgroundDestination = HQ.GraphicsDevice.PresentationParameters.Bounds;

    InitializeUI();
}

public override void LoadContent()
{
   _font = HQ.Content.Load<SpriteFont>("fonts/04B_30");

   _font5x = Content.Load<SpriteFont>("fonts/04B_30_5x");

//    Load the background pattern texture.
//    _backgroundPattern = Content.Load<Texture2D>("images/background-pattern");

   _uiSoundEffect = HQ.Content.Load<SoundEffect>("audio/ui");

    _atlas = TextureAtlas.FromFile(HQ.Content, "images/atlas-definition.xml");
}

public override void Update(GameTime gameTime)
{
    // Update the offsets for the background pattern wrapping so that it
    // scrolls down and to the right.
    // float offset = _scrollSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
    // _backgroundOffset.X -= offset;
    // _backgroundOffset.Y -= offset;

    // Ensure that the offsets do not go beyond the texture bounds so it is
    // a seamless wrap
    // _backgroundOffset.X %= _backgroundPattern.Width;
    // _backgroundOffset.Y %= _backgroundPattern.Height;

    GumService.Default.Update(gameTime);
}

public override void Draw(GameTime gameTime)
{
    HQ.GraphicsDevice.Clear(new Color(32, 40, 78, 255));

    // Draw the background pattern first using the PointWrap sampler state.
    // HQ.SpriteBatch.Begin(samplerState: SamplerState.PointWrap);
    // HQ.SpriteBatch.Draw(_backgroundPattern, _backgroundDestination, new Rectangle(_backgroundOffset.ToPoint(), _backgroundDestination.Size), Color.White * 0.5f);
    // HQ.SpriteBatch.End();

    if (_titleScreenButtonsPanel.IsVisible)
    {
        HQ.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        Color dropShadowColor = Color.Black * 0.5f;

        HQ.SpriteBatch.DrawString(_font5x, PROJECT_TEXT1, _project1TextPos + new Vector2(10, 10), dropShadowColor, 0.0f, _project1TextOrigin, 1.0f, SpriteEffects.None, 1.0f);

        HQ.SpriteBatch.DrawString(_font5x, PROJECT_TEXT1, _project1TextPos, Color.White, 0.0f, _project1TextOrigin, 1.0f, SpriteEffects.None, 1.0f);

        HQ.SpriteBatch.DrawString(_font5x, PROJECT_TEXT2, _project2TextPos + new Vector2(10, 10), dropShadowColor, 0.0f, _project2TextOrigin, 1.0f, SpriteEffects.None, 1.0f);

        HQ.SpriteBatch.DrawString(_font5x, PROJECT_TEXT2, _project2TextPos, Color.White, 0.0f, _project2TextOrigin, 1.0f, SpriteEffects.None, 1.0f);
        
        HQ.SpriteBatch.End();
    }

    GumService.Default.Draw();
}

private void CreateTitlePanel()
{
    _titleScreenButtonsPanel = new Panel();
    _titleScreenButtonsPanel.Dock(Gum.Wireframe.Dock.Fill);
    _titleScreenButtonsPanel.AddToRoot();

    AnimatedButton startButton = new AnimatedButton(_atlas);
    startButton.Anchor(Gum.Wireframe.Anchor.BottomLeft);
    startButton.X = 50;
    startButton.Y = -12;
    startButton.Text = "Start";
    startButton.Click += HandleStartClicked;
    _titleScreenButtonsPanel.AddChild(startButton);

    _optionsButton = new AnimatedButton(_atlas);
    _optionsButton.Anchor(Gum.Wireframe.Anchor.BottomRight);
    _optionsButton.X = -50;
    _optionsButton.Y = -12;
    _optionsButton.Text = "Options";
    _optionsButton.Click += HandleOptionsClicked;
    _titleScreenButtonsPanel.AddChild(_optionsButton);

    startButton.IsFocused = true;
}

private void CreateOptionsPanel()
{
    _optionsPanel = new Panel();
    _optionsPanel.Dock(Gum.Wireframe.Dock.Fill);
    _optionsPanel.IsVisible = false;
    _optionsPanel.AddToRoot();

    TextRuntime optionsText = new TextRuntime();
    optionsText.X = 10;
    optionsText.Y = 10;
    optionsText.Text = "OPTIONS";
    optionsText.UseCustomFont = true;
    optionsText.FontScale = 0.5f;
    optionsText.CustomFontFile = @"fonts/04b_30.fnt";
    _optionsPanel.AddChild(optionsText);

    OptionsSlider musicSlider = new OptionsSlider(_atlas);
    musicSlider.Name = "MusicSlider";
    musicSlider.Text = "MUSIC";
    musicSlider.Anchor(Gum.Wireframe.Anchor.Top);
    musicSlider.Y = 30f;
    musicSlider.Minimum = 0;
    musicSlider.Maximum = 1;
    musicSlider.Value = HQ.Audio.SongVolume;
    musicSlider.SmallChange = .1;
    musicSlider.LargeChange = .2;
    musicSlider.ValueChanged += HandleMusicSliderValueChanged;
    musicSlider.ValueChangeCompleted += HandleMusicSliderValueChangeCompleted;
    _optionsPanel.AddChild(musicSlider);

    OptionsSlider sfxSlider = new OptionsSlider(_atlas);
    sfxSlider.Name = "SfxSlider";
    sfxSlider.Text = "SFX";
    sfxSlider.Anchor(Gum.Wireframe.Anchor.Top);
    sfxSlider.Y = 93;
    sfxSlider.Minimum = 0;
    sfxSlider.Maximum = 1;
    sfxSlider.Value = HQ.Audio.SoundEffectVolume;
    sfxSlider.SmallChange = .1;
    sfxSlider.LargeChange = .2;
    sfxSlider.ValueChanged += HandleSfxSliderChanged;
    sfxSlider.ValueChangeCompleted += HandleSfxSliderChangeCompleted;
    _optionsPanel.AddChild(sfxSlider);

    _optionsBackButton = new AnimatedButton(_atlas);
    _optionsBackButton.Text = "BACK";
    _optionsBackButton.Anchor(Gum.Wireframe.Anchor.BottomRight);
    _optionsBackButton.X = -28f;
    _optionsBackButton.Y = -10f;
    _optionsBackButton.Click += HandleOptionsButtonBack;
    _optionsPanel.AddChild(_optionsBackButton);
}
private void HandleStartClicked(object sender, EventArgs e)
{
    HQ.Audio.PlaySoundEffect(_uiSoundEffect);

    HQ.ChangeScene(new GameScene());
}

private void HandleOptionsClicked(object sender, EventArgs e)
{
    HQ.Audio.PlaySoundEffect(_uiSoundEffect);
    
    _titleScreenButtonsPanel.IsVisible = false;

    _optionsPanel.IsVisible = true;

    _optionsBackButton.IsFocused = true;
}

private void HandleSfxSliderChanged(object sender, EventArgs args)
{
    var slider = (Slider)sender;

    HQ.Audio.SoundEffectVolume = (float)slider.Value;
}

private void HandleSfxSliderChangeCompleted(object sender, EventArgs e)
{
    HQ.Audio.PlaySoundEffect(_uiSoundEffect);
}

private void HandleMusicSliderValueChanged(object sender, EventArgs args)
{
    var slider = (Slider)sender;

    HQ.Audio.SongVolume = (float)slider.Value;
}

private void HandleMusicSliderValueChangeCompleted(object sender, EventArgs args)
{
    HQ.Audio.PlaySoundEffect(_uiSoundEffect);
}

private void HandleOptionsButtonBack(object sender, EventArgs e)
{
    HQ.Audio.PlaySoundEffect(_uiSoundEffect);

    _titleScreenButtonsPanel.IsVisible = true;

    _optionsPanel.IsVisible = false;

    _optionsButton.IsFocused = true;
}

private void InitializeUI()
{
    GumService.Default.Root.Children.Clear();

    CreateTitlePanel();
    CreateOptionsPanel();
}

}

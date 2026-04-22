using System;
using TheNewBeginning.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MainEngine;
using MainEngine.Scenes;
using MainEngine.Graphics;
using MonoGameGum;
using Gum.Forms.Controls;

namespace TheNewBeginning.Scenes;

public class TitleScene : Scene
{
private const string PROJECT_TEXT1 = "The New";
private const string PROJECT_TEXT2 = "Beginning";

private SpriteFont _font5x;
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
private TitleSceneEffectsRenderer _effectsRenderer;


public override void Initialize()
{
    base.Initialize();

    HQ.ExitOnEscape = false;

    Vector2 size = _font5x.MeasureString(PROJECT_TEXT1);
    _project1TextPos = new Vector2(640, 100);
    _project1TextOrigin = size * 0.5f;

    size = _font5x.MeasureString(PROJECT_TEXT2);
    _project2TextPos = new Vector2(757, 207);
    _project2TextOrigin = size * 0.5f;

    InitializeUI();
}

public override void LoadContent()
{
   _font5x = Content.Load<SpriteFont>("fonts/04B_30_5x");

   _uiSoundEffect = HQ.Content.Load<SoundEffect>("audio/ui");

    _atlas = TextureAtlas.FromFile(HQ.Content, "images/atlas-definition.xml");

    _effectsRenderer = new TitleSceneEffectsRenderer();
}

public override void Update(GameTime gameTime)
{
    _effectsRenderer.Update(gameTime);

    GumService.Default.Update(gameTime);
}

public override void Draw(GameTime gameTime)
{
    _effectsRenderer.Draw();

    if (_titleScreenButtonsPanel.IsVisible)
    {
        HQ.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        DrawTitleText();
        HQ.SpriteBatch.End();
    }

    GumService.Default.Draw();
}

private void DrawTitleText()
{
    DrawShadowedString(PROJECT_TEXT1, _project1TextPos, Color.White, _project1TextOrigin);
    DrawShadowedString(PROJECT_TEXT2, _project2TextPos, new Color(235, 236, 220), _project2TextOrigin);
}

private void DrawShadowedString(string text, Vector2 position, Color color, Vector2 origin)
{
    HQ.SpriteBatch.DrawString(_font5x, text, position + new Vector2(10, 10), Color.Black * 0.55f, 0.0f, origin, 1.0f, SpriteEffects.None, 1.0f);
    HQ.SpriteBatch.DrawString(_font5x, text, position, color, 0.0f, origin, 1.0f, SpriteEffects.None, 1.0f);
}

private void CreateTitlePanel()
{
    _titleScreenButtonsPanel = new Panel();
    _titleScreenButtonsPanel.Dock(Gum.Wireframe.Dock.Fill);
    _titleScreenButtonsPanel.AddToRoot();

    AnimatedButton startButton = new AnimatedButton(_atlas);
    startButton.X = 22f;
    startButton.Y = 110f;
    startButton.Text = "START";
    startButton.SetPalette(new Color(255, 221, 42), new Color(56, 47, 28));
    startButton.SetVisualSize(23f, 0.43f);
    startButton.Click += HandleStartClicked;
    _titleScreenButtonsPanel.AddChild(startButton);

    _optionsButton = new AnimatedButton(_atlas);
    _optionsButton.X = 22f;
    _optionsButton.Y = 135f;
    _optionsButton.Text = "SETTINGS";
    _optionsButton.Click += HandleOptionsClicked;
    _titleScreenButtonsPanel.AddChild(_optionsButton);

    AnimatedButton exitButton = new AnimatedButton(_atlas);
    exitButton.X = 22f;
    exitButton.Y = 153f;
    exitButton.Text = "EXIT";
    exitButton.Click += HandleExitClicked;
    _titleScreenButtonsPanel.AddChild(exitButton);

    startButton.IsFocused = true;
}

private void CreateOptionsPanel()
{
    _optionsPanel = new Panel();
    _optionsPanel.Dock(Gum.Wireframe.Dock.Fill);
    _optionsPanel.IsVisible = false;
    _optionsPanel.AddToRoot();

    OptionsSlider musicSlider = new OptionsSlider(_atlas);
    musicSlider.Name = "MusicSlider";
    musicSlider.Text = "MUSIC";
    musicSlider.Anchor(Gum.Wireframe.Anchor.Center);
    musicSlider.Y = -34f;
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
    sfxSlider.Anchor(Gum.Wireframe.Anchor.Center);
    sfxSlider.Y = 34f;
    sfxSlider.Minimum = 0;
    sfxSlider.Maximum = 1;
    sfxSlider.Value = HQ.Audio.SoundEffectVolume;
    sfxSlider.SmallChange = .1;
    sfxSlider.LargeChange = .2;
    sfxSlider.ValueChanged += HandleSfxSliderChanged;
    sfxSlider.ValueChangeCompleted += HandleSfxSliderChangeCompleted;
    _optionsPanel.AddChild(sfxSlider);

    _optionsBackButton = new AnimatedButton(_atlas);
    _optionsBackButton.Text = "X";
    _optionsBackButton.Anchor(Gum.Wireframe.Anchor.TopRight);
    _optionsBackButton.X = -10f;
    _optionsBackButton.Y = 8f;
    _optionsBackButton.Click += HandleOptionsButtonBack;
    _optionsPanel.AddChild(_optionsBackButton);
}

private void HandleStartClicked(object sender, EventArgs e)
{
    HQ.Audio.PlaySoundEffect(_uiSoundEffect);

    HQ.ChangeScene(new GameScene());
}

private void HandleExitClicked(object sender, EventArgs e)
{
    HQ.Audio.PlaySoundEffect(_uiSoundEffect);

    HQ.Instance.Exit();
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

protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        _effectsRenderer?.Dispose();
        _effectsRenderer = null;
    }

    base.Dispose(disposing);
}

}

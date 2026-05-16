using LILITH.Abilities;
using LILITH.Core;
using LILITH.Items;
using LILITH.UI;
using MainEngine;
using MainEngine.Camera;
using MainEngine.Entities;
using MainEngine.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LILITH.Core.Scenes;

public class GameScene : Scene
{
    private PlayerController  _controller = null!;
    private Camera            _camera     = null!;
    private ExperienceSpawner _spawner    = null!;
    private ExperienceBar     _xpBar      = null!;
    private LevelUpScreen     _levelUp    = null!;

    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;
    private bool      _isPaused;

    private const float CAMERA_LERP = 0.1f;

    public override void Initialize() => base.Initialize();

    public override void LoadContent()
    {
        _pixel = new Texture2D(HQ.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");

        var player = new Player(new Vector2(640, 360), hp: 100, pixel: _pixel);

        _controller = new PlayerController(player, _pixel);

        _camera     = new Camera();
        _camera.Pos = player.Center;

        _spawner = new ExperienceSpawner();
        _xpBar   = new ExperienceBar();
        _levelUp = new LevelUpScreen();

        player.OnLevelUp     += HandleLevelUp;
        _levelUp.OnCardChosen += HandleCardChosen;

        _spawner.SpawnInitial(player.Center, HQ.GraphicsDevice.Viewport);
    }

    public override void Update(GameTime gameTime)
    {
        _xpBar.Update(gameTime);
        _levelUp.Update(gameTime, HQ.GraphicsDevice.Viewport);

        if (_isPaused) return;

        Vector2 cursorWorld = GetCursorWorld();
        _controller.Update(gameTime, cursorWorld);

        _camera.Pos = Vector2.Lerp(_camera.Pos, _controller.Player.Center, CAMERA_LERP);

        _spawner.Update(gameTime, _camera.Pos, HQ.GraphicsDevice.Viewport);

        int gained = _spawner.CollectOrbs(_controller.Player.Center);
        if (gained > 0)
        {
            _xpBar.TriggerFlash();
            _controller.Player.AddExperience(gained);
        }
    }

    public override void Draw(GameTime gameTime)
    {
        HQ.GraphicsDevice.Clear(new Color(20, 20, 30));

        Matrix cameraMatrix = _camera.get_transformation(HQ.GraphicsDevice);

        HQ.SpriteBatch.Begin(
            sortMode:        SpriteSortMode.Deferred,
            blendState:      BlendState.AlphaBlend,
            samplerState:    SamplerState.PointClamp,
            transformMatrix: cameraMatrix);

        _spawner.Draw(HQ.SpriteBatch, _pixel);
        _controller.Draw(gameTime, HQ.SpriteBatch);

        HQ.SpriteBatch.End();

        HQ.SpriteBatch.Begin(
            sortMode:     SpriteSortMode.Deferred,
            blendState:   BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp);

        _xpBar.Draw(HQ.SpriteBatch, _pixel, HQ.GraphicsDevice.Viewport,
                    _controller.Player.CurrentXp,
                    _controller.Player.RequiredXp,
                    _controller.Player.Level);

        _levelUp.Draw(HQ.SpriteBatch, _pixel, _font, HQ.GraphicsDevice.Viewport);
        

        HQ.SpriteBatch.End();
    }

    private Vector2 GetCursorWorld()
    {
        MouseState mouse  = Mouse.GetState();
        Matrix     inv    = Matrix.Invert(_camera.get_transformation(HQ.GraphicsDevice));
        return Vector2.Transform(new Vector2(mouse.X, mouse.Y), inv);
    }

    private void HandleLevelUp()
{
    _isPaused = true;

    var cards = new IAbility[]
    {
        new SatelliteAbility(),
        new AuraAbility(),
        new SatelliteAbility()
    };

    _levelUp.Show(HQ.GraphicsDevice.Viewport, cards);
}

    private void HandleCardChosen(int cardIndex)
{
    IAbility[] cards = new IAbility[]
    {
        new SatelliteAbility(),
        new AuraAbility(),
        new SatelliteAbility()
    };

    IAbility chosen = cards[cardIndex];

    
    var existing = _controller.GetAbility<IAbility>();

    // Проверяем по типу
    foreach (var ability in _controller.GetAllAbilities())
    {
        if (ability.GetType() == chosen.GetType())
        {
            ability.Upgrade();
            _isPaused = false;
            return;
        }
    }

    // Способности ещё нет — добавляем
    _controller.AddAbility(chosen);
    _isPaused = false;
}

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _controller.Player.OnLevelUp  -= HandleLevelUp;
            _levelUp.OnCardChosen         -= HandleCardChosen;
            _pixel?.Dispose();
        }
        base.Dispose(disposing);
    }
}
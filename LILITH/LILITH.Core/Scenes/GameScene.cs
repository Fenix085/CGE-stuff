using System;
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

    private Texture2D  _pixel = null!;
    private SpriteFont _font  = null!;
    private bool       _isPaused;

    private const int   CARD_COUNT  = 4;
    private const float CAMERA_LERP = 0.1f;

    private IAbility[] _currentCards = Array.Empty<IAbility>();
    private readonly Random _random  = new();

    // ── Инициализация ─────────────────────────────────────────────────────

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

        player.OnLevelUp      += HandleLevelUp;
        _levelUp.OnCardChosen += HandleCardChosen;

        _spawner.SpawnInitial(player.Center, HQ.GraphicsDevice.Viewport);
    }

    // ── Update ────────────────────────────────────────────────────────────

    public override void Update(GameTime gameTime)
    {
        _xpBar.Update(gameTime);
        _levelUp.Update(gameTime, HQ.GraphicsDevice.Viewport);

        if (_isPaused) return;

        Vector2 cursorWorld = GetCursorWorld();
        _controller.Update(gameTime, _controller.Player.LastMoveDirection, GetCursorWorld());

        _camera.Pos = Vector2.Lerp(_camera.Pos, _controller.Player.Center, CAMERA_LERP);

        _spawner.Update(gameTime, _camera.Pos, HQ.GraphicsDevice.Viewport);

        int gained = _spawner.CollectOrbs(_controller.Player.Center);
        if (gained > 0)
        {
            _xpBar.TriggerFlash();
            _controller.Player.AddExperience(gained);
        }
    }

    // ── Draw ──────────────────────────────────────────────────────────────

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

    // ── Карточки способностей ─────────────────────────────────────────────

    private IAbility[] CreatePool() => new IAbility[]
    {
        new SatelliteAbility(),
        new AuraAbility(),
        new TrailAbility(),
        new SlashAbility()
    };

    private IAbility[] GetRandomCards()
    {
        IAbility[] pool   = CreatePool();
        IAbility[] result = new IAbility[CARD_COUNT];

        // Перемешиваем пул (Fisher-Yates)
        for (int i = pool.Length - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        for (int i = 0; i < CARD_COUNT; i++)
            result[i] = pool[i];

        return result;
    }

    // ── События ───────────────────────────────────────────────────────────

    private void HandleLevelUp()
    {
        _isPaused     = true;
        _currentCards = GetRandomCards();
        _levelUp.Show(HQ.GraphicsDevice.Viewport, _currentCards);
    }

    private void HandleCardChosen(int cardIndex)
    {
        IAbility chosen = _currentCards[cardIndex];

        foreach (var ability in _controller.GetAllAbilities())
        {
            if (ability.GetType() == chosen.GetType())
            {
                ability.Upgrade();
                _isPaused = false;
                return;
            }
        }

        _controller.AddAbility(chosen);
        _isPaused = false;
    }

    // ── Вспомогательное ───────────────────────────────────────────────────

    private Vector2 GetCursorWorld()
    {
        MouseState mouse = Mouse.GetState();
        Matrix     inv   = Matrix.Invert(_camera.get_transformation(HQ.GraphicsDevice));
        return Vector2.Transform(new Vector2(mouse.X, mouse.Y), inv);
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
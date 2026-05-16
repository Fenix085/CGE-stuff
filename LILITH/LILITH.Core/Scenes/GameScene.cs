using LILITH.Items;
using LILITH.UI;
using MainEngine;
using MainEngine.Camera;
using MainEngine.Entities;
using MainEngine.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LILITH.Core.Scenes;

/// <summary>
/// Главная игровая сцена.
/// Наследуется от Scene — движок вызывает Initialize, Update, Draw автоматически.
/// </summary>
public class GameScene : Scene
{
    private Player            _player  = null!;
    private Camera            _camera  = null!;
    private ExperienceSpawner _spawner = null!;
    private ExperienceBar     _xpBar   = null!;
    private LevelUpScreen     _levelUp = null!;

    // 1×1 белая текстура для рисования примитивов
    private Texture2D _pixel = null!;

    private bool _isPaused;

    private const float CAMERA_LERP = 0.1f;

    // ── Инициализация ─────────────────────────────────────────────────────

    public override void Initialize()
    {
        base.Initialize(); // вызывает LoadContent()
    }

    public override void LoadContent()
    {
        // 1×1 белый пиксель — используется везде для рисования прямоугольников и кружков
        _pixel = new Texture2D(HQ.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        // Создаём игрока (зелёный квадрат пока нет спрайта)
        _player = new Player(new Vector2(640, 360), hp: 100, pixel: _pixel);

        // Камера сразу на игроке
        _camera     = new Camera();
        _camera.Pos = _player.Center;

        // Системы опыта и UI
        _spawner = new ExperienceSpawner();
        _xpBar   = new ExperienceBar();
        _levelUp = new LevelUpScreen();

        // Подписки на события
        _player.OnLevelUp     += HandleLevelUp;
        _levelUp.OnCardChosen += HandleCardChosen;

        // Начальный спавн орбов вокруг стартовой позиции
        _spawner.SpawnInitial(_player.Center, HQ.GraphicsDevice.Viewport);
    }

    // ── Update ────────────────────────────────────────────────────────────

    public override void Update(GameTime gameTime)
    {
        // UI обновляется всегда (даже на паузе)
        _xpBar.Update(gameTime);
        _levelUp.Update(gameTime, HQ.GraphicsDevice.Viewport);

        if (_isPaused) return;

        _player.Update(gameTime);

        // Камера плавно следует за игроком
        _camera.Pos = Vector2.Lerp(_camera.Pos, _player.Center, CAMERA_LERP);

        // Спавнер ориентируется по позиции камеры
        _spawner.Update(gameTime, _camera.Pos, HQ.GraphicsDevice.Viewport);

        // Подбор орбов
        int gained = _spawner.CollectOrbs(_player.Center);
        if (gained > 0)
        {
            _xpBar.TriggerFlash();
            _player.AddExperience(gained);
        }
    }

    // ── Draw ──────────────────────────────────────────────────────────────

    public override void Draw(GameTime gameTime)
    {
        HQ.GraphicsDevice.Clear(new Color(20, 20, 30));

        Matrix cameraMatrix = _camera.get_transformation(HQ.GraphicsDevice);

        // Мировой слой — с матрицей камеры
        HQ.SpriteBatch.Begin(
            sortMode:          SpriteSortMode.Deferred,
            blendState:        BlendState.AlphaBlend,
            samplerState:      SamplerState.PointClamp,
            depthStencilState: null,
            rasterizerState:   null,
            effect:            null,
            transformMatrix:   cameraMatrix);

        _spawner.Draw(HQ.SpriteBatch, _pixel);
        _player.Draw(gameTime, HQ.SpriteBatch);

        HQ.SpriteBatch.End();

        // UI слой — без матрицы (экранные координаты)
        HQ.SpriteBatch.Begin(
            sortMode:     SpriteSortMode.Deferred,
            blendState:   BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp);

        _xpBar.Draw(HQ.SpriteBatch, _pixel, HQ.GraphicsDevice.Viewport,
                    _player.CurrentXp, _player.RequiredXp, _player.Level);

        _levelUp.Draw(HQ.SpriteBatch, _pixel, null, HQ.GraphicsDevice.Viewport);

        HQ.SpriteBatch.End();
    }

    // ── События ───────────────────────────────────────────────────────────

    private void HandleLevelUp()
    {
        _isPaused = true;
        _levelUp.Show(HQ.GraphicsDevice.Viewport);
    }

    private void HandleCardChosen(int cardIndex)
    {
        // TODO: применить улучшение по индексу (0, 1, 2)
        _isPaused = false;
    }

    // ── Очистка ───────────────────────────────────────────────────────────

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _player.OnLevelUp     -= HandleLevelUp;
            _levelUp.OnCardChosen -= HandleCardChosen;
            _pixel?.Dispose();
        }
        base.Dispose(disposing);
    }
}

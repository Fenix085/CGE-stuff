using System;
using System.Collections.Generic;
using LILITH.Abilities;
using LILITH.Core.Enemies.Boss;
using LILITH.Core.Tools;
using LILITH.Items;
using LILITH.UI;
using MainEngine;
using MainEngine.Camera;
using MainEngine.Entities;
using MainEngine.FlockEnemy;
using MainEngine.Graphics;
using MainEngine.Navigation;
using MainEngine.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using LILITH.Audio;

namespace LILITH.Core.Scenes;

public class EndlessScene : Scene
{
    // ── Core ──────────────────────────────────────────────────────────────

    private PlayerController _controller = null!;
    private Camera           _camera     = null!;
    private Texture2D        _pixel      = null!;
    private SpriteFont       _font       = null!;
    private bool _deathSoundPlayed;
    

    private const float CAMERA_LERP = 0.1f;

    // ── UI ────────────────────────────────────────────────────────────────

    private ExperienceSpawner _xpSpawner  = null!;
    private ExperienceBar     _xpBar      = null!;
    private LevelUpScreen     _levelUp    = null!;
    private GameOverScreen    _gameOver   = null!;
    private bool              _isPaused;
    private bool              _isGameOver;
    private float             _deathTimer;
    private bool _pauseKeyReleased = true;

    private const int  CARD_COUNT    = 3;
    private IAbility[] _currentCards = Array.Empty<IAbility>();
    private readonly Random _random  = new();

    // ── Pause ─────────────────────────────────────────────────────────────

    private bool          _isPauseMenu;
    private Button        _btnResume   = null!;
    private Button        _btnOptions  = null!;
    private Button        _btnMainMenu = null!;
    private KeyboardState _prevKeys;

    // ── Enemies ───────────────────────────────────────────────────────────

    private ContinuousSpawner            _enemySpawner = null!;
    private AgentConfig                  _agentConfig  = null!;
    private TextureRegion                _agentRegion  = null!;
    private readonly List<ForceSource>   _forceSources = new();
    private Navigation                   _nav          = null!;

    // ── Difficulty ────────────────────────────────────────────────────────

    private float _elapsed          = 0f;
    private float _difficultyTimer  = 0f; 
    private const float DIFFICULTY_INTERVAL = 30f;
    private int   _difficultyLevel  = 0;

    // ── Boss ──────────────────────────────────────────────────────────────

    private Boss  _boss            = null!;
    private bool  _bossAlive       = false;
    private float _bossTimer       = 0f;
    private const float BOSS_INTERVAL = 120f;

    // ── Initialization ────────────────────────────────────────────────────

    public override void Initialize()
    {
        HQ.ExitOnEscape = false;
        base.Initialize();
    }

    public override void LoadContent()
    {
        _pixel = new Texture2D(HQ.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font  = Content.Load<SpriteFont>("DefaultFont");

        AudioAssets.PauseOpen =
        Content.Load<SoundEffect>("audio/pause_in");

        AudioAssets.PauseClose =
        Content.Load<SoundEffect>("audio/pause_out");

        AudioAssets.Footsteps =
        Content.Load<SoundEffect>("audio/bananas_movement");

        AudioAssets.PlayerDeath =
        Content.Load<SoundEffect>("audio/bananas_death");

        AudioAssets.Shoot =
        Content.Load<SoundEffect>("audio/bananas_shoot");
        
        AudioAssets.GameMusic =
        Content.Load<Song>("audio/gameplaymusic");

        // ── Player ──
        var atlas = TextureAtlas.FromFile(Content, "player.xml");
        var idle  = atlas.CreateAnimatedSprite("idle");
        var walk  = atlas.CreateAnimatedSprite("walk");
        var death = atlas.CreateAnimatedSprite("death");
        idle.CenterOrigin(); walk.CenterOrigin(); death.CenterOrigin();

        var player  = new Player(idle, new Vector2(400, 300), hp: 50);
        _controller = new PlayerController(player, _pixel, idle, walk, death);
        _controller.AddAbility(new AutoShootAbility());

        AudioAssets.Footsteps =
        Content.Load<SoundEffect>("audio/bananas_movement");

        player.SetFootstepSound(AudioAssets.Footsteps);

        // ── Camera ──
        _camera     = new Camera();
        _camera.Pos = player.Center;

        // ── UI ──
        _xpSpawner = new ExperienceSpawner();
        _xpBar     = new ExperienceBar();
        _levelUp   = new LevelUpScreen();
        _gameOver  = new GameOverScreen();

        player.OnLevelUp      += HandleLevelUp;
        _levelUp.OnCardChosen += HandleCardChosen;
        _xpSpawner.SpawnInitial(player.Center, HQ.GraphicsDevice.Viewport);

        // ── Pause menu buttons ──
        int cx = HQ.GraphicsDevice.Viewport.Width  / 2;
        int cy = HQ.GraphicsDevice.Viewport.Height / 2;

        _btnResume   = new Button(new Rectangle(cx - 130, cy - 95, 260, 52), "RESUME")
        {
            ColorNormal  = new Color(26,  13,  40,  230),
            ColorHover   = new Color(50,  25,  70,  240),
            ColorPressed = new Color(15,  8,   25,  255),
            ColorText    = new Color(200, 168, 220),
            ColorShadow  = new Color(0,   0,   0,   0),
        };

        _btnOptions = new Button(new Rectangle(cx - 130, cy - 27, 260, 52), "OPTIONS")
        {
            ColorNormal  = new Color(26,  13,  40,  230),
            ColorHover   = new Color(50,  25,  70,  240),
            ColorPressed = new Color(15,  8,   25,  255),
            ColorText    = new Color(200, 168, 220),
            ColorShadow  = new Color(0,   0,   0,   0),
        };

        _btnMainMenu = new Button(new Rectangle(cx - 130, cy + 41, 260, 52), "MAIN MENU")
        {
            ColorNormal  = new Color(26,  13,  40,  230),
            ColorHover   = new Color(50,  25,  70,  240),
            ColorPressed = new Color(15,  8,   25,  255),
            ColorText    = new Color(200, 168, 220),
            ColorShadow  = new Color(0,   0,   0,   0),
        };
        
        _btnResume.OnClick += () =>
        {
            HQ.Audio.PlaySoundEffect(AudioAssets.PauseClose);
            _isPauseMenu = false;
        };
        _btnOptions.OnClick += () =>
        {
            HQ.Audio.PlaySoundEffect(AudioAssets.PauseClose);
            HQ.ChangeScene(new OptionsScene(() => new MainMenuScene()));
        };
        _btnMainMenu.OnClick += () =>
        {
            HQ.Audio.PlaySoundEffect(AudioAssets.PauseClose);
            HQ.ChangeScene(new MainMenuScene());
        };

        // ── Enemies ──
        _agentRegion = MakeSolidRegion(8, 8, Color.White);
        _agentConfig = new AgentConfig
        {
            AgentSpeed       = 65f,
            RepulsionRadius  = 50f,
            AlignmentRadius  = 100f,
            AttractionRadius = 200f,
            AttractionAngle  = MathHelper.ToRadians(70f),
            RepulsionForce   = 10f,
            AlignmentForce   = 5f,
            AttractionForce  = 2f,
            GravitationForce = 0.5f,
            DebugVisible     = false
        };

        _nav = new Navigation();
        BuildNavGraph();

        _enemySpawner = new ContinuousSpawner(new SpawnZoneConfig
        {
            Shape            = SpawnShape.Circle,
            Radius           = 600f,
            ViewPadding      = 100f,
            AllowSpawnInView = false
        });
        _enemySpawner.SetNavigation(_nav);

        RegisterEnemyFactories();

        // Walker base weight
        _enemySpawner.AddWeight(EnemyType.Walker, 5f);
        _enemySpawner.Interval  = 2f;
        _enemySpawner.BatchSize = 1;
        _enemySpawner.MaxAlive  = 10;

        // Boss spawn timer
        _bossTimer = BOSS_INTERVAL;
        HQ.Audio.PlaySong(AudioAssets.GameMusic);
    }

    // ── Update ────────────────────────────────────────────────────────────

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        // Esc for pause
        KeyboardState keys = Keyboard.GetState();
        if (keys.IsKeyDown(Keys.Escape) && _pauseKeyReleased)
        {
            _pauseKeyReleased = false;

            _isPauseMenu = !_isPauseMenu;

            if (_isPauseMenu)
            {
                HQ.Audio.PlaySoundEffect(
                    AudioAssets.PauseOpen,
                    0.45f,
                    0f,
                    0f,
                    false);
            }
            else
            {
                HQ.Audio.PlaySoundEffect(
                    AudioAssets.PauseClose,
                    0.45f,
                    0f,
                    0f,
                    false);
            }
        }
        if (keys.IsKeyUp(Keys.Escape))
        {
            _pauseKeyReleased = true;
        }

        // Pause menu has priority over game pause
        if (_isPauseMenu)
        {
            _btnResume.Update(gameTime);
            _btnOptions.Update(gameTime);
            _btnMainMenu.Update(gameTime);
            return;
        }

        _xpBar.Update(gameTime);
        _levelUp.Update(gameTime, HQ.GraphicsDevice.Viewport);

        if (_isPaused) return;

        // Game Over
        if (_isGameOver)
        {
            _deathTimer += dt;
            if (_deathTimer >= 3f && !_gameOver.IsVisible)
                _gameOver.Show(HQ.GraphicsDevice.Viewport, _font);
            _gameOver.Update(gameTime, HQ.GraphicsDevice.Viewport);
            _controller.UpdateDeathAnimation(gameTime);
            return;
        }

        // ── Gameplay ──
        _elapsed         += dt;
        _difficultyTimer += dt;
        _bossTimer       -= dt;

        // Increase difficulty every 30 seconds
        if (_difficultyTimer >= DIFFICULTY_INTERVAL)
        {
            _difficultyTimer = 0f;
            RampDifficulty();
        }

        // Boss spawn every 2 minutes
        if (_bossTimer <= 0f && !_bossAlive)
        {
            _bossTimer = BOSS_INTERVAL;
            SpawnBoss();
        }

        Vector2 nearestDir  = GetNearestEnemyDirection();
        Vector2 cursorWorld = GetCursorWorld();
        _controller.Update(gameTime, nearestDir, cursorWorld);

        if (_controller.Player.Health.IsDead)
        {
            _isGameOver  = true;
            _deathTimer  = 0f;
            if (!_deathSoundPlayed && _controller.Player.Health.IsDead)
            {
                _deathSoundPlayed = true;

                HQ.Audio.PlaySoundEffect(
                    AudioAssets.PlayerDeath,
                    0.7f,
                    0f,
                    0f,
                    false);
            }
        }

        CheckAbilityHits();
        ResolveEnemySeparation();

        _camera.Pos = Vector2.Lerp(_camera.Pos, _controller.Player.Center, CAMERA_LERP);

        // Update spawners and enemies
        _enemySpawner.Zone.Position = _controller.Player.Position;

        _xpSpawner.Update(gameTime, _camera.Pos, HQ.GraphicsDevice.Viewport);
        int gained = _xpSpawner.CollectOrbs(_controller.Player.Center);
        if (gained > 0)
        {
            _xpBar.TriggerFlash();
            _controller.Player.AddExperience(gained);
        }

        var player = _controller.Player;
        var vp     = HQ.GraphicsDevice.Viewport;

        _enemySpawner.Update(gameTime, player.Position,
            new Vector2(vp.Width, vp.Height), player.Health.IsDead);
        _enemySpawner.UpdateTanks(gameTime, player.Position,
            player.Health.IsDead, _agentConfig, _forceSources);

        // Boss
        if (_bossAlive && !_boss.IsDead)
        {
            _boss.Update(gameTime, player.Position,
                player.GetBounds(), player.Health.IsDead);
            if (_boss.PendingPlayerDamage > 0)
                player.Health.TakeDamage(_boss.PendingPlayerDamage);
        }
        else if (_bossAlive && _boss.IsDead)
        {
            _bossAlive = false;
            _xpSpawner.SpawnOrb(_boss.Position, value: 100);
        }

        CheckMeleeHits(_enemySpawner.Walkers);
        CheckMeleeHits(_enemySpawner.Runners);

        float playerRadius = player.GetBounds().Radius;
        foreach (var tank in _enemySpawner.Tanks)
        {
            int dmg = tank.ProcessAgentHits(player.Position, playerRadius);
            if (dmg > 0) player.Health.TakeDamage(dmg);
        }

        Circle playerBounds = player.GetBounds();
        foreach (var shooter in _enemySpawner.Shooters)
        {
            foreach (var p in shooter.Projectiles)
            {
                if (!p.IsDead && p.Bounds.Intersects(playerBounds))
                {
                    player.Health.TakeDamage(shooter.Damage);
                    p.Hit = true;
                }
            }
        }
    }

    // ── Difficulty Ramp ───────────────────────────────────────────────────

    private void RampDifficulty()
    {
        _difficultyLevel++;

        switch (_difficultyLevel)
        {
            case 1: 
                _enemySpawner.SetWeights(
                    new EnemyWeight(EnemyType.Walker, 5f),
                    new EnemyWeight(EnemyType.Runner, 2f));
                _enemySpawner.MaxAlive  = 15;
                break;

            case 2: 
                _enemySpawner.Interval  = 1.5f;
                _enemySpawner.BatchSize = 2;
                _enemySpawner.MaxAlive  = 20;
                break;

            case 3: 
                _enemySpawner.SetWeights(
                    new EnemyWeight(EnemyType.Walker,  4f),
                    new EnemyWeight(EnemyType.Runner,  3f),
                    new EnemyWeight(EnemyType.Shooter, 2f));
                _enemySpawner.MaxAlive  = 25;
                break;

            case 4: 
                _enemySpawner.SetWeights(
                    new EnemyWeight(EnemyType.Walker,  4f),
                    new EnemyWeight(EnemyType.Runner,  3f),
                    new EnemyWeight(EnemyType.Shooter, 2f),
                    new EnemyWeight(EnemyType.Tank,    1f));
                _enemySpawner.Interval  = 1.0f;
                _enemySpawner.MaxAlive  = 30;
                break;

            default: 
                _enemySpawner.Interval  = MathF.Max(0.4f, _enemySpawner.Interval - 0.1f);
                _enemySpawner.MaxAlive  = Math.Min(50, _enemySpawner.MaxAlive + 5);
                _enemySpawner.BatchSize = Math.Min(4, _enemySpawner.BatchSize + 1);
                break;
        }
    }

    // ── Draw ──────────────────────────────────────────────────────────────

    public override void Draw(GameTime gameTime)
    {
        HQ.GraphicsDevice.Clear(new Color(20, 20, 30));

        Matrix cam = _camera.get_transformation(HQ.GraphicsDevice);

        HQ.SpriteBatch.Begin(
            sortMode:        SpriteSortMode.Deferred,
            blendState:      BlendState.AlphaBlend,
            samplerState:    SamplerState.PointClamp,
            transformMatrix: cam);

        _xpSpawner.Draw(HQ.SpriteBatch, _pixel);
        _controller.Draw(gameTime, HQ.SpriteBatch);
        _enemySpawner.Draw(gameTime, HQ.SpriteBatch);

        if (_bossAlive && !_boss.IsDead)
            _boss.Draw(gameTime, HQ.SpriteBatch);

        HQ.SpriteBatch.End();

        HQ.SpriteBatch.Begin(
            sortMode:     SpriteSortMode.Deferred,
            blendState:   BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp);

        _xpBar.Draw(HQ.SpriteBatch, _pixel, HQ.GraphicsDevice.Viewport,
                    _controller.Player.CurrentXp,
                    _controller.Player.RequiredXp,
                    _controller.Player.Level);

        DrawPlayerHp();
        DrawTimer();

        _levelUp.Draw(HQ.SpriteBatch, _pixel, _font, HQ.GraphicsDevice.Viewport);

        if (_isPauseMenu) DrawPauseMenu();

        _gameOver.Draw(HQ.SpriteBatch, _pixel, HQ.GraphicsDevice.Viewport);

        HQ.SpriteBatch.End();
    }

    // ── UI Drawing ────────────────────────────────────────────────────────

    private void DrawTimer()
    {
        if (_font == null) return;

        int    minutes = (int)_elapsed / 60;
        int    seconds = (int)_elapsed % 60;
        string text    = $"{minutes:00}:{seconds:00}";
        Vector2 size   = _font.MeasureString(text);
        Vector2 pos    = new Vector2(
            (HQ.GraphicsDevice.Viewport.Width - size.X) * 0.5f, 10f);

        HQ.SpriteBatch.DrawString(_font, text, pos + new Vector2(1, 1), Color.Black);
        HQ.SpriteBatch.DrawString(_font, text, pos, Color.White);
    }

    private void DrawPlayerHp()
    {
        if (_font == null) return;
        var    player = _controller.Player;
        string text   = $"HP: {player.Health.CurrentHealth}/{player.Health.MaxHealth}";
        HQ.SpriteBatch.DrawString(_font, text,
            new Vector2(10, 30), Color.LimeGreen);
    }

    private void DrawPauseMenu()
    {
        var vp = HQ.GraphicsDevice.Viewport;

        // Screen darken
        HQ.SpriteBatch.Draw(_pixel,
            new Rectangle(0, 0, vp.Width, vp.Height),
            new Color(0, 0, 0, 160));

        // Menu panel
        int pw = 360, ph = 300;
        int px = (vp.Width  - pw) / 2;
        int py = (vp.Height - ph) / 2 - 20;

        // Gothic-style panel with double border and corner diamonds
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px, py, pw, ph), new Color(122, 85, 144));
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px + 1, py + 1, pw - 2, ph - 2), new Color(18, 10, 30, 245));
        
        // Inner border
        const int B = 4;
        Color innerBorder = new Color(90, 53, 112);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px + B, py + B, pw - B * 2, 1), innerBorder);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px + B, py + ph - B, pw - B * 2, 1), innerBorder);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px + B, py + B, 1, ph - B * 2), innerBorder);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px + pw - B, py + B, 1, ph - B * 2), innerBorder);

        // Corner diamonds
        DrawDiamond(px,      py,      4, new Color(147, 112, 168, 180));
        DrawDiamond(px + pw, py,      4, new Color(147, 112, 168, 180));
        DrawDiamond(px,      py + ph, 4, new Color(147, 112, 168, 180));
        DrawDiamond(px + pw, py + ph, 4, new Color(147, 112, 168, 180));

        // Header PAUSED
        if (_font != null)
        {
            string  title = "PAUSED";
            Vector2 size  = _font.MeasureString(title);
            Vector2 pos = new Vector2((vp.Width - size.X) * 0.5f, py + 25);
            HQ.SpriteBatch.DrawString(_font, title, pos + new Vector2(2, 2), new Color(0, 0, 0) * 0.6f);
            HQ.SpriteBatch.DrawString(_font, title, pos, new Color(212, 184, 224));

            // Line under header
            HQ.SpriteBatch.Draw(_pixel,
               new Rectangle(px + 20, (int)(py + 25 + size.Y + 4), pw - 40, 1),
                new Color(122, 85, 144, 160));
        }

        DrawGothicButton(_btnResume);
        DrawGothicButton(_btnOptions);
        DrawGothicButton(_btnMainMenu);
    }

    private void DrawDiamond(int cx, int cy, int size, Color color)
    {
        for (int dy = -size; dy <= size; dy++)
        {
            int dx = size - Math.Abs(dy);
            HQ.SpriteBatch.Draw(_pixel, new Rectangle(cx - dx, cy + dy, dx * 2, 1), color);
        }
    }

    private void DrawGothicButton(Button btn)
    {
        var r = btn.Bounds;
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y, r.Width, r.Height), new Color(122, 85, 144));
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2),
            btn.IsHovered ? new Color(50, 25, 70, 240) : new Color(26, 13, 40, 230));

        const int B = 4;
        Color inner = new Color(90, 53, 112);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(r.X + B, r.Y + B, r.Width - B * 2, 1), inner);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(r.X + B, r.Y + r.Height - B, r.Width - B * 2, 1), inner);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(r.X + B, r.Y + B, 1, r.Height - B * 2), inner);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(r.X + r.Width - B, r.Y + B, 1, r.Height - B * 2), inner);

        int mid = r.Y + r.Height / 2;
        DrawDiamond(r.X + 15,           mid, 5, new Color(147, 112, 168, 180));
        DrawDiamond(r.X + r.Width - 15, mid, 5, new Color(147, 112, 168, 180));

        if (_font != null)
        {
            Vector2 size = _font.MeasureString(btn.Label);
            Vector2 pos  = new Vector2(
                r.X + (r.Width  - size.X) * 0.5f,
                r.Y + (r.Height - size.Y) * 0.5f);
            HQ.SpriteBatch.DrawString(_font, btn.Label, pos + new Vector2(1, 2), new Color(0, 0, 0) * 0.5f);
            HQ.SpriteBatch.DrawString(_font, btn.Label, pos,
                btn.IsHovered ? new Color(240, 210, 255) : new Color(200, 168, 220));
        }
    }

    // ── Abilities ─────────────────────────────────────────────────────────

    private IAbility[] CreatePool() => new IAbility[]
    {
        new SatelliteAbility(),
        new AuraAbility(),
        new TrailAbility(),
        new SlashAbility(),
        new AutoShootAbility()
    };

    private IAbility[] GetRandomCards()
    {
        IAbility[] pool   = CreatePool();
        IAbility[] result = new IAbility[CARD_COUNT];
        for (int i = pool.Length - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        for (int i = 0; i < CARD_COUNT; i++)
            result[i] = pool[i];
        return result;
    }

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

    // ── Boss ──────────────────────────────────────────────────────────────

    private void SpawnBoss()
    {
        var bossRegion = MakeSolidRegion(40, 40, Color.DarkRed);
        var bossSprite = MakeAnimSprite(bossRegion);
        bossSprite.Scale = new Vector2(2f);

        Vector2 spawnPos = _controller.Player.Position + new Vector2(500f, 0f);
        _boss      = new Boss(bossSprite, spawnPos, hp: 200);
        _bossAlive = true;
    }

    // ── Collisions ────────────────────────────────────────────────────────

    private void CheckAbilityHits()
    {
        var allEnemies = new List<Enemy>();
        allEnemies.AddRange(_enemySpawner.Walkers);
        allEnemies.AddRange(_enemySpawner.Runners);
        allEnemies.AddRange(_enemySpawner.Shooters);
        allEnemies.AddRange(_enemySpawner.Tanks);

        foreach (var ability in _controller.GetAllAbilities())
        {
            IReadOnlyList<Circle> hitCircles = ability.GetHitCircles();
            if (hitCircles.Count == 0) continue;

            foreach (var enemy in allEnemies)
            {
                if (enemy.IsDead) continue;
                Circle bounds = enemy.GetBounds();
                foreach (Circle hit in hitCircles)
                {
                    if (hit.Intersects(bounds))
                    {
                        enemy.Health.TakeDamage(ability.Damage);
                        ability.NotifyHit(hit);
                        if (enemy.Health.IsDead)
                        {
                            enemy.ApplyDeath();
                            _xpSpawner.SpawnOrb(enemy.Position);
                        }
                        break;
                    }
                }
            }

            if (_bossAlive && !_boss.IsDead)
            {
                Circle bossBounds = _boss.GetBounds();
                foreach (Circle hit in hitCircles)
                {
                    if (hit.Intersects(bossBounds))
                    {
                        _boss.Health.TakeDamage(ability.Damage);
                        ability.NotifyHit(hit);
                        break;
                    }
                }
            }
        }
    }

    private void CheckMeleeHits<T>(IReadOnlyList<T> enemies) where T : Enemy
    {
        var player = _controller.Player;
        foreach (var enemy in enemies)
        {
            if (enemy.IsDead) continue;
            if (enemy is Enemies.Walker.Walker w && w.IsAttacking && w.CanAttack)
                if (Vector2.Distance(w.Position, player.Position) <= w.AttackRange && w.TryAttack())
                    player.Health.TakeDamage(w.Damage);
            if (enemy is Enemies.Runner.Runner r && r.IsAttacking && r.CanAttack)
                if (Vector2.Distance(r.Position, player.Position) <= r.AttackRange && r.TryAttack())
                    player.Health.TakeDamage(r.Damage);
        }
    }

    private void ResolveEnemySeparation()
    {
        var all = new List<Enemy>();
        all.AddRange(_enemySpawner.Walkers);
        all.AddRange(_enemySpawner.Runners);
        all.AddRange(_enemySpawner.Shooters);
        all.AddRange(_enemySpawner.Tanks);

        const float SEP_RADIUS  = 32f;
        const float SEP_PUSH    = 0.5f;

        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].IsDead) continue;
            for (int j = i + 1; j < all.Count; j++)
            {
                if (all[j].IsDead) continue;
                Vector2 delta = all[i].Position - all[j].Position;
                float   dist  = delta.Length();
                if (dist < SEP_RADIUS && dist > 0.001f)
                {
                    Vector2 push = delta / dist * (SEP_RADIUS - dist) * SEP_PUSH;
                    all[i].Position += push;
                    all[j].Position -= push;
                }
            }
        }
    }

    // ── Navigation ────────────────────────────────────────────────────────

    private void BuildNavGraph()
    {
        var a = new NavNode { Id = 1, Position = new Vector2(100, 100) };
        var b = new NavNode { Id = 2, Position = new Vector2(400, 100) };
        var c = new NavNode { Id = 3, Position = new Vector2(700, 100) };
        var d = new NavNode { Id = 4, Position = new Vector2(400, 300) };
        var e = new NavNode { Id = 5, Position = new Vector2(100, 500) };
        var f = new NavNode { Id = 6, Position = new Vector2(700, 500) };

        _nav.AddNode(a); _nav.AddNode(b); _nav.AddNode(c);
        _nav.AddNode(d); _nav.AddNode(e); _nav.AddNode(f);

        int id = 1;
        _nav.AddHighway(Highway.Create(id++, 1, 2, a.Position, b.Position, 90f));
        _nav.AddHighway(Highway.Create(id++, 2, 3, b.Position, c.Position, 90f));
        _nav.AddHighway(Highway.Create(id++, 2, 4, b.Position, d.Position, 90f));
        _nav.AddHighway(Highway.Create(id++, 4, 5, d.Position, e.Position, 90f));
        _nav.AddHighway(Highway.Create(id++, 4, 6, d.Position, f.Position, 90f));
        _nav.AddHighway(Highway.Create(id++, 1, 5, a.Position, e.Position, 90f));
        _nav.AddHighway(Highway.Create(id++, 3, 6, c.Position, f.Position, 90f));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private Vector2 GetNearestEnemyDirection()
    {
        var     player   = _controller.Player;
        float   bestDist = float.MaxValue;
        Vector2 bestDir  = Vector2.Zero;

        var all = new List<Enemy>();
        all.AddRange(_enemySpawner.Walkers);
        all.AddRange(_enemySpawner.Runners);
        all.AddRange(_enemySpawner.Shooters);
        all.AddRange(_enemySpawner.Tanks);
        if (_bossAlive && !_boss.IsDead) all.Add(_boss);

        foreach (var e in all)
        {
            if (e.IsDead) continue;
            float dist = Vector2.Distance(player.Position, e.Position);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestDir  = Vector2.Normalize(e.Position - player.Position);
            }
        }
        return bestDir;
    }

    private Vector2 GetCursorWorld()
    {
        MouseState mouse = Mouse.GetState();
        Matrix     inv   = Matrix.Invert(_camera.get_transformation(HQ.GraphicsDevice));
        return Vector2.Transform(new Vector2(mouse.X, mouse.Y), inv);
    }

    private TextureRegion MakeSolidRegion(int w, int h, Color color)
    {
        var tex  = new Texture2D(HQ.GraphicsDevice, w, h);
        var data = new Color[w * h];
        Array.Fill(data, color);
        tex.SetData(data);
        return new TextureRegion(tex, 0, 0, w, h);
    }

    private AnimatedSprite MakeAnimSprite(TextureRegion region)
    {
        var anim   = new Animation(new List<TextureRegion> { region },
                                   TimeSpan.FromMilliseconds(100));
        var sprite = new AnimatedSprite(anim);
        sprite.CenterOrigin();
        return sprite;
    }

    private void RegisterEnemyFactories()
    {
        var skeletonAtlas = TextureAtlas.FromFile(Content, "skeleton.xml");
        var patrickAtlas  = TextureAtlas.FromFile(Content, "patrick.xml");
        var orcAtlas      = TextureAtlas.FromFile(Content, "orc.xml");

        _enemySpawner.RegisterFactory(EnemyType.Walker, pos =>
        {
            var s = skeletonAtlas.CreateAnimatedSprite("walk");
            s.CenterOrigin();
            return new Enemies.Walker.Walker(s, pos);
        });

        _enemySpawner.RegisterFactory(EnemyType.Runner, pos =>
        {
            var s = skeletonAtlas.CreateAnimatedSprite("walk");
            s.CenterOrigin();
            return new Enemies.Runner.Runner(s, pos);
        });

        _enemySpawner.RegisterFactory(EnemyType.Shooter, pos =>
        {
            var s = patrickAtlas.CreateAnimatedSprite("walk");
            s.CenterOrigin();
            return new Enemies.Shooter.Shooter(s, pos)
            {
                ProjectileFactory = (pPos, dir) => new MainEngine.Projectile.Projectile
                {
                    Position  = pPos,
                    Direction = dir,
                    Region    = MakeSolidRegion(6, 3, Color.Yellow),
                    Speed     = 200f,
                    LifeTime  = 3f
                }
            };
        });

        _enemySpawner.RegisterFactory(EnemyType.Tank, pos =>
        {
            var s = orcAtlas.CreateAnimatedSprite("walk");
            s.CenterOrigin();
            return new Enemies.Tank.Tank(s, pos)
            {
                AgentSpawnIntervalSeconds = 2f,
                MaxAgents                = 15,
                AgentDamage              = 1,
                AgentHitRadius           = 10f,
                AgentFactory             = aPos =>
                {
                    var agent = new Agent(_agentRegion, aPos);
                    agent.Scale = new Vector2(1f);
                    return agent;
                }
            };
        });
    }

    // ── Cleanup ───────────────────────────────────────────────────────────

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _controller.Player.OnLevelUp -= HandleLevelUp;
            _levelUp.OnCardChosen        -= HandleCardChosen;
            _pixel?.Dispose();
        }
        base.Dispose(disposing);
    }
}
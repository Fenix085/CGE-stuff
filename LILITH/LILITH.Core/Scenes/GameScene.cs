using System;
using System.Collections.Generic;
using LILITH.Abilities;
using LILITH.Core.Enemies.Boss;
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
using LILITH.Core.Tools;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using LILITH.Audio;

namespace LILITH.Core.Scenes;

public class GameScene : Scene
{
    // ── Core ──────────────────────────────────────────────────────────────
    private bool _usingGamepad = false;
    private PlayerController _controller = null!;
    private Camera           _camera     = null!;
    private Texture2D        _pixel      = null!;
    private SpriteFont       _font       = null!;
    private TextureAtlas _bossAtlas = null!;
    private Texture2D _mapTexture = null!;
    private readonly Rectangle _mapBounds = new Rectangle(400, 200, 1800, 1000);

    private const float CAMERA_LERP = 0.1f;
    private float _isDeathTimer;
    private bool _deathSoundPlayed;
    
    

    // ── Exp and UI ─────────────────────────────────────────────────────────

    private ExperienceSpawner _xpSpawner = null!;
    private ExperienceBar     _xpBar     = null!;
    private LevelUpScreen     _levelUp   = null!;
    private bool              _isPaused;

    private const int  CARD_COUNT    = 3;
    private IAbility[] _currentCards = Array.Empty<IAbility>();
    private int _levelUpCardIndex = 0;
    private readonly Random _random  = new();
    private GameOverScreen _gameOver = null!;
    private bool           _isGameOver;
    private bool _isOptionsMenu;
    private VictoryScreen _victoryScreen = null!;
    private bool _isVictory;
    private float _victoryTimer;

    // ── Pause Menu ────────────────────────────────────────────────────────

    private bool          _isPauseMenu = false;
    private Button        _btnResume   = null!;
    private Button        _btnOptions  = null!;
    private Button        _btnMainMenu = null!;
    private int _pauseMenuIndex = 0;


    // ── Enemies ──────────────────────────────────────────────────────────

    private WaveSpawner               _enemySpawner  = null!;
    private Navigation                 _nav           = null!;
    private AgentConfig                _agentConfig   = null!;
    private TextureRegion              _agentRegion   = null!;
    private readonly List<ForceSource> _forceSources  = new();

    // ── Boss ──────────────────────────────────────────────────────────────

    private Boss _boss        = null!;
    private bool _bossSpawned = false;

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
        _gameOver = new GameOverScreen();
        _victoryScreen = new VictoryScreen();

        _font = Content.Load<SpriteFont>("DefaultFont");

        _mapTexture = Content.Load<Texture2D>("map");
        
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
        var atlas  = TextureAtlas.FromFile(Content, "player.xml");
        var idle   = atlas.CreateAnimatedSprite("idle");
        var walk   = atlas.CreateAnimatedSprite("walk");
        var death  = atlas.CreateAnimatedSprite("death");
        
        
        idle.CenterOrigin();
        walk.CenterOrigin();
        death.CenterOrigin();
        var player = new Player(idle, new Vector2(400, 300), hp: 50);
        _controller = new PlayerController(player, _pixel, idle, walk, death);
        _controller.AddAbility(new AutoShootAbility());

        AudioAssets.Footsteps =
        Content.Load<SoundEffect>("audio/bananas_movement");

        player.SetFootstepSound(AudioAssets.Footsteps);

        // ── Camera ──
        _camera     = new Camera();
        _camera.Pos = player.Center;

        // ── Experience and UI ──
        _xpSpawner = new ExperienceSpawner();
        _xpBar     = new ExperienceBar();
        _levelUp   = new LevelUpScreen();
        _gameOver = new GameOverScreen();

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
            _isOptionsMenu = true;
        };
        _btnMainMenu.OnClick += () =>
        {
            HQ.Audio.PlaySoundEffect(AudioAssets.PauseClose);
            HQ.ChangeScene(new MainMenuScene());
        };

        // ── Enemies ──
        var projectileRegion = MakeSolidRegion(6, 3, Color.Yellow);
        var skeletonAtlas = TextureAtlas.FromFile(Content, "skeleton.xml");
        var patricktlas   = TextureAtlas.FromFile(Content, "patrick.xml");
        var orcAtlas      = TextureAtlas.FromFile(Content, "orc.xml");
        _bossAtlas = TextureAtlas.FromFile(Content, "boss.xml");
        var flyAtlas = TextureAtlas.FromFile(Content, "fly.xml");

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

        // _nav = new Navigation();
        // BuildNavGraph();

        _enemySpawner = new WaveSpawner();
        _enemySpawner.SetNavigation(_nav);

        _enemySpawner = new WaveSpawner(new SpawnZoneConfig
        {
            Shape          = SpawnShape.Circle,
            Radius         = 600f,   
            ViewPadding    = 100f,   
            AllowSpawnInView = false 
        });

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
            var s       = patricktlas.CreateAnimatedSprite("walk");
            s.CenterOrigin();
            var shooter = new Enemies.Shooter.Shooter(s, pos)
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
            return shooter;
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
                    var fly = flyAtlas.CreateAnimatedSprite("walk");

                    fly.CenterOrigin();
                    fly.Scale = new Vector2(2f);

                    var agent = new Agent(fly, aPos);
                    agent.Scale = new Vector2(2f);

                    return agent;
                }
            };
        });

// Wave 1 - just walkers, slow spawn to let player get used to them
_enemySpawner.AddWave(new Wave
{
    Entries = new List<SpawnEntry>
    {
        new() { Type = EnemyType.Walker, Count = 5, DelayBetween = 1.0f },
    },
    WaitForClear   = true,
    DelayAfterWave = 5f
});

// Wave 2 - more walkers + runners, no shooters yet
_enemySpawner.AddWave(new Wave
{
    Entries = new List<SpawnEntry>
    {
        new() { Type = EnemyType.Walker, Count = 6, DelayBetween = 0.8f },
        new() { Type = EnemyType.Runner, Count = 4, DelayBetween = 0.5f },
    },
    WaitForClear   = true,
    DelayAfterWave = 5f
});

// Wave 3 - full mix, introducing shooters
_enemySpawner.AddWave(new Wave
{
    Entries = new List<SpawnEntry>
    {
        new() { Type = EnemyType.Walker,  Count = 8,  DelayBetween = 0.6f },
        new() { Type = EnemyType.Runner,  Count = 5,  DelayBetween = 0.4f },
        new() { Type = EnemyType.Shooter, Count = 2,  DelayBetween = 1.0f },
    },
    WaitForClear   = true,
    DelayAfterWave = 5f
});

// Wave 4 - full mix, more enemies
_enemySpawner.AddWave(new Wave
{
    Entries = new List<SpawnEntry>
    {
        new() { Type = EnemyType.Walker,  Count = 10, DelayBetween = 0.5f },
        new() { Type = EnemyType.Runner,  Count = 8,  DelayBetween = 0.3f },
        new() { Type = EnemyType.Shooter, Count = 3,  DelayBetween = 0.8f },
    },
    WaitForClear   = true,
    DelayAfterWave = 8f
});

// Wave 5 - Boss wave with tanks
_enemySpawner.AddWave(new Wave
{
    Entries        = new List<SpawnEntry>(),
    WaitForClear   = false,
    DelayAfterWave = 0f,
    IsBossWave     = true
});

_enemySpawner.OnBossWave += SpawnBoss;
_enemySpawner.Start();
    HQ.Audio.PlaySong(AudioAssets.GameMusic);
    }

    // ── Update ────────────────────────────────────────────────────────────

    public override void Update(GameTime gameTime)
    {
        var pad = HQ.Input.GamePads[0];

        // Esc переключает меню паузы (только если не открыт экран левелапа)
        if ((HQ.Input.Keyboard.WasKeyJustPressed(Keys.Escape)
            || pad.WasButtonJustPressed(Buttons.Start))
            && !_isPaused)
        {
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

        // Pause menu has priority over game pause
        if (_isPauseMenu)
        {
            // Detect which device is active
            if (pad.WasButtonJustPressed(Buttons.DPadUp)
                || pad.WasButtonJustPressed(Buttons.DPadDown)
                || pad.WasButtonJustPressed(Buttons.A)
                || pad.WasButtonJustPressed(Buttons.B))
                _usingGamepad = true;

            if (HQ.Input.Mouse.WasMoved)
                _usingGamepad = false;

            // ── Gamepad path ──
            if (_usingGamepad)
            {
                if (pad.WasButtonJustPressed(Buttons.DPadUp)
                    || pad.WasButtonJustPressed(Buttons.LeftThumbstickUp))
                    _pauseMenuIndex = Math.Max(0, _pauseMenuIndex - 1);

                if (pad.WasButtonJustPressed(Buttons.DPadDown)
                    || pad.WasButtonJustPressed(Buttons.LeftThumbstickDown))
                    _pauseMenuIndex = Math.Min(1, _pauseMenuIndex + 1);

                if (pad.WasButtonJustPressed(Buttons.A))
                {
                    if (_pauseMenuIndex == 0) _isPauseMenu = false;
                    else HQ.ChangeScene(new MainMenuScene());
                }

                if (pad.WasButtonJustPressed(Buttons.B))
                    _isPauseMenu = false;
            }

            // ForceHover only when gamepad is active
            _btnResume.ForceHover   = _usingGamepad && _pauseMenuIndex == 0;
            _btnMainMenu.ForceHover = _usingGamepad && _pauseMenuIndex == 1;

            // Mouse clicks always go through Button.Update
            _btnResume.Update(gameTime);
            _btnOptions.Update(gameTime);
            _btnMainMenu.Update(gameTime);
            return;
        }

        // UI has priority over game input
        _xpBar.Update(gameTime);
        _levelUp.Update(gameTime, HQ.GraphicsDevice.Viewport);

        if (_isPaused && _currentCards.Length > 0)
        {
            if (HQ.Input.Keyboard.WasKeyJustPressed(Keys.Left)
                || pad.WasButtonJustPressed(Buttons.DPadLeft)
                || pad.WasButtonJustPressed(Buttons.LeftThumbstickLeft))
            {
                _levelUpCardIndex = Math.Max(0, _levelUpCardIndex - 1);
            }

            if (HQ.Input.Keyboard.WasKeyJustPressed(Keys.Right)
                || pad.WasButtonJustPressed(Buttons.DPadRight)
                || pad.WasButtonJustPressed(Buttons.LeftThumbstickRight))
            {
                _levelUpCardIndex = Math.Min(_currentCards.Length - 1, _levelUpCardIndex + 1);
            }
            
            if (pad.WasButtonJustPressed(Buttons.A)
                || HQ.Input.Keyboard.WasKeyJustPressed(Keys.Enter))
            {
                _levelUp.Hide();
                HandleCardChosen(_levelUpCardIndex);
            }
        }
        if (_isPaused) return;

        // ── Player ──
        if (_isGameOver)
        {
            _isDeathTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_isDeathTimer >= 3f && !_gameOver.IsVisible)
                _gameOver.Show(HQ.GraphicsDevice.Viewport, _font);

            _gameOver.Update(gameTime, HQ.GraphicsDevice.Viewport);

            
            _controller.UpdateDeathAnimation(gameTime);
            return;
        }
        if (_isVictory)
        {
            _victoryTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_victoryTimer >= 5f && !_victoryScreen.IsVisible)
                _victoryScreen.Show(HQ.GraphicsDevice.Viewport, _font);

            _victoryScreen.Update(gameTime, HQ.GraphicsDevice.Viewport);

            return;
        }
        Vector2 nearestEnemyDir = GetNearestEnemyDirection();
        Vector2 cursorWorld;
        Vector2 rightStick = pad.RightThumbStick;

        if (rightStick.LengthSquared() > 0.04f) // deadzone
        {
            // Convert stick to world position: player center + stick direction * range
            cursorWorld = _controller.Player.Center 
                        + new Vector2(rightStick.X, -rightStick.Y) * 200f;
        }
        else
        {
            cursorWorld = GetCursorWorld();
        }

    _controller.Update(gameTime, nearestEnemyDir, cursorWorld);
    if (_controller.Player.Health.IsDead && !_isGameOver)
    {
        _isGameOver = true;
        _isDeathTimer = 0f;

        if (!_deathSoundPlayed)
        {
            _deathSoundPlayed = true;

            HQ.Audio.PlaySoundEffect(
                AudioAssets.PlayerDeath,
                0.7f,
                0f,
                0f,
                false);
        }

        return;
    }
        var p   = _controller.Player;
        float r = p.GetBounds().Radius;

        p.Position = new Vector2(
            MathHelper.Clamp(p.Position.X, _mapBounds.Left   + r, _mapBounds.Right  - r),
            MathHelper.Clamp(p.Position.Y, _mapBounds.Top    + r, _mapBounds.Bottom - r));
        
        CheckAbilityHits();

        if (HQ.Input.Keyboard.WasKeyJustPressed(Keys.F5))
            SpawnBoss();

        // ── Camera ──
        _camera.Pos = Vector2.Lerp(_camera.Pos, _controller.Player.Center, CAMERA_LERP);
        var vp      = HQ.GraphicsDevice.Viewport;
        float halfW = vp.Width  * 0.5f;
        float halfH = vp.Height * 0.5f;

        _camera.Pos = new Vector2(
            MathHelper.Clamp(_camera.Pos.X,
                _mapBounds.Left   + halfW, _mapBounds.Right  - halfW),
            MathHelper.Clamp(_camera.Pos.Y,
                _mapBounds.Top    + halfH, _mapBounds.Bottom - halfH));

        // ── Experience ──
        _xpSpawner.Update(gameTime, _camera.Pos, HQ.GraphicsDevice.Viewport);
        int gained = _xpSpawner.CollectOrbs(_controller.Player.Center);
        if (gained > 0)
        {
            _xpBar.TriggerFlash();
            _controller.Player.AddExperience(gained);
        }

        // ── Enemies ──
        var player = _controller.Player;

        _enemySpawner.Zone.Position = _controller.Player.Position;
        _enemySpawner.Update(gameTime, player.Position,
        new Vector2(HQ.GraphicsDevice.Viewport.Width,
                    HQ.GraphicsDevice.Viewport.Height),
        player.Health.IsDead);
        _enemySpawner.UpdateTanks(gameTime, player.Position,
            player.Health.IsDead, _agentConfig, _forceSources);

        // ── Boss ──
        if (_bossSpawned && !_boss.IsDead)
        {
            Circle playerBounds = player.GetBounds();
            _boss.Update(gameTime, player.Position, playerBounds, player.Health.IsDead);

            if (_boss.PendingPlayerDamage > 0)
                DamagePlayer(_boss.PendingPlayerDamage);

            if (_boss.ActiveForceSources.Count > 0)
            {
                _forceSources.AddRange(_boss.ActiveForceSources);

                foreach (var source in _boss.ActiveForceSources)
                {
                    float dist    = Vector2.Distance(player.Position, source.Position);
                    float maxDist = 800f; // vibration falloff range

                    if (dist < maxDist)
                    {
                        // Closer = stronger, 1.0 at center → 0.0 at maxDist
                        float intensity = 1f - (dist / maxDist);

                        // Big explosion (radius 120) vs line explosion (radius 45)
                        float strength = 1f;

                        int ms = 2000;

                        pad.SetVibration(strength, TimeSpan.FromMilliseconds(ms));
                    }
                }
            }
        }

        // ── Collisions: melee ──
        CheckMeleeHits(_enemySpawner.Walkers);
        CheckMeleeHits(_enemySpawner.Runners);
        CheckMeleeHits(_enemySpawner.Tanks);

        // ── Collisions: tank agents → player ──
        float playerRadius = player.GetBounds().Radius;
        foreach (var tank in _enemySpawner.Tanks)
        {
            int damage = tank.ProcessAgentHits(player.Position, playerRadius);
            if (damage > 0)
                DamagePlayer(damage);
        }

        // ── Collisions: shooter projectiles → player ──
        Circle shooterPlayerBounds = player.GetBounds();
        foreach (var shooter in _enemySpawner.Shooters)
        {
            foreach (var j in shooter.Projectiles)
            {
                if (!j.IsDead && j.Bounds.Intersects(shooterPlayerBounds))
                {
                    DamagePlayer(shooter.Damage);
                    j.Hit = true;
                }
            }
        }
    }

    // ── Draw ──────────────────────────────────────────────────────────────

    public override void Draw(GameTime gameTime)
    {
        HQ.GraphicsDevice.Clear(new Color(20, 20, 30));
    
        Matrix cameraMatrix = _camera.get_transformation(HQ.GraphicsDevice);

        // World layer
        HQ.SpriteBatch.Begin(
            sortMode:        SpriteSortMode.Deferred,
            blendState:      BlendState.AlphaBlend,
            samplerState:    SamplerState.PointClamp,
            transformMatrix: cameraMatrix);
        HQ.SpriteBatch.Draw(_mapTexture,
                new Rectangle(0, 0, 2600, 1400),
                Color.White);
        _xpSpawner.Draw(HQ.SpriteBatch, _pixel);
        _controller.Draw(gameTime, HQ.SpriteBatch);
        _enemySpawner.Draw(gameTime, HQ.SpriteBatch);

        if (_bossSpawned)
        {
            foreach (Circle zone in _boss.WarningZones)
                DrawCircle(zone.Location.ToVector2(), zone.Radius, Color.Red * 0.3f);

            if (!_boss.IsDead)
            {
                _boss.Draw(gameTime, HQ.SpriteBatch);
                DrawHealthBar(
                    _boss.Position + new Vector2(-30f, -_boss.Sprite.Height * 0.5f - 12f),
                    60, 6,
                    _boss.Health.CurrentHealth,
                    _boss.Health.MaxHealth,
                    Color.Red);
            }
        }

        if (!_bossSpawned)
            DrawHintSquare();

        HQ.SpriteBatch.End();

        // UI layer
        HQ.SpriteBatch.Begin(
            sortMode:     SpriteSortMode.Deferred,
            blendState:   BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp);

        _xpBar.Draw(HQ.SpriteBatch, _pixel, HQ.GraphicsDevice.Viewport,
                    _controller.Player.CurrentXp,
                    _controller.Player.RequiredXp,
                    _controller.Player.Level);

        DrawPlayerHp();

        _levelUp.Draw(HQ.SpriteBatch, _pixel, _font, HQ.GraphicsDevice.Viewport, _levelUpCardIndex);

        if (_isPauseMenu)
            DrawPauseMenu();
        
        _gameOver.Draw(HQ.SpriteBatch, _pixel, HQ.GraphicsDevice.Viewport);
        _victoryScreen.Draw(HQ.SpriteBatch, _pixel, HQ.GraphicsDevice.Viewport);
        HQ.SpriteBatch.End();
    }

    // ── Pause Menu ────────────────────────────────────────────────────────

    private void DrawPauseMenu()
    {
        var vp = HQ.GraphicsDevice.Viewport;

        // Screen darken
        HQ.SpriteBatch.Draw(_pixel,
            new Rectangle(0, 0, vp.Width, vp.Height),
            new Color(0, 0, 0, 160));

        // Panel background with gothic borders
        int pw = 360, ph = 300;
        int px = (vp.Width  - pw) / 2;
        int py = (vp.Height - ph) / 2 - 20;

        // Inner fill
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px, py, pw, ph), new Color(122, 85, 144));
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px + 1, py + 1, pw - 2, ph - 2), new Color(18, 10, 30, 245));
        
        // Inner border
        const int B = 4;
        Color innerBorder = new Color(90, 53, 112);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px + B, py + B, pw - B * 2, 1), innerBorder);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px + B, py + ph - B, pw - B * 2, 1), innerBorder);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px + B, py + B, 1, ph - B * 2), innerBorder);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px + pw - B, py + B, 1, ph - B * 2), innerBorder);

        // Diamond corners
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

    private void DrawDiamond(int cx, int cy, int size, Color color)
    {
        for (int dy = -size; dy <= size; dy++)
        {
            int dx = size - Math.Abs(dy);
            HQ.SpriteBatch.Draw(_pixel, new Rectangle(cx - dx, cy + dy, dx * 2, 1), color);
        }
        Button[] pauseButtons = { _btnResume, _btnMainMenu };
        for (int i = 0; i < pauseButtons.Length; i++)
        {
            if (i == _pauseMenuIndex)
            {
                var r = pauseButtons[i].Bounds;
                HQ.SpriteBatch.Draw(_pixel,
                    new Rectangle(r.X - 4, r.Y - 4, r.Width + 8, r.Height + 8),
                    Color.White * 0.15f);
            }
        }

        _btnResume.Draw(HQ.SpriteBatch, _pixel, _font);
        _btnMainMenu.Draw(HQ.SpriteBatch, _pixel, _font);
    }

    // ── Ability Cards ─────────────────────────────────────────────────────

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

    // ── Events ────────────────────────────────────────────────────────────

    private void HandleLevelUp()
    {
        _isPaused     = true;
        _levelUpCardIndex = 0;
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

                Circle enemyBounds = enemy.GetBounds();

                foreach (Circle hit in hitCircles)
                {
                    if (hit.Intersects(enemyBounds))
                    {
                        enemy.Health.TakeDamage(ability.Damage);
                        enemy.TriggerHitFlash();
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

            if (_bossSpawned && !_boss.IsDead)
            {
                Circle bossBounds = _boss.GetBounds();
                foreach (Circle hit in hitCircles)
                {
                    if (hit.Intersects(bossBounds))
                    {
                        _boss.Health.TakeDamage(ability.Damage);
                        _boss.TriggerHitFlash();
                        ability.NotifyHit(hit);
                        if (_boss.Health.IsDead)
                        {
                            _boss.ApplyDeath();
                            _xpSpawner.SpawnOrb(_boss.Position, value: 10);

                            _isVictory = true;
                            _victoryTimer = 0f;

                            return;
                        }
                        break;
                    }
                }
            }
        }
    }

    // ── Boss ──────────────────────────────────────────────────────────────

    private void SpawnBoss()
    {
        var bossSprite = _bossAtlas.CreateAnimatedSprite("walk");
        bossSprite.CenterOrigin();
        bossSprite.Scale = new Vector2(2f);

        Vector2 spawnPos = _controller.Player.Position + new Vector2(400f, 0f);
        _boss        = new Boss(bossSprite, spawnPos, hp: 100);
        _bossSpawned = true;
    }

    // ── Collisions ────────────────────────────────────────────────────────

    private void CheckMeleeHits<T>(IReadOnlyList<T> enemies) where T : Enemy
    {
        var player = _controller.Player;

        foreach (var enemy in enemies)
        {
            if (enemy.IsDead) continue;

            if (enemy is Enemies.Walker.Walker w && w.IsAttacking && w.CanAttack)
            {
                if (Vector2.Distance(w.Position, player.Position) <= w.AttackRange
                    && w.TryAttack())
                    DamagePlayer(w.Damage);
            }
            else if (enemy is Enemies.Runner.Runner r && r.IsAttacking && r.CanAttack)
            {
                if (Vector2.Distance(r.Position, player.Position) <= r.AttackRange
                    && r.TryAttack())
                    DamagePlayer(r.Damage);
            }
        }
    }

    // ── Navigation ────────────────────────────────────────────────────────

    // private void BuildNavGraph()
    // {
    //     var a = new NavNode { Id = 1, Position = new Vector2(100, 100) };
    //     var b = new NavNode { Id = 2, Position = new Vector2(400, 100) };
    //     var c = new NavNode { Id = 3, Position = new Vector2(700, 100) };
    //     var d = new NavNode { Id = 4, Position = new Vector2(400, 300) };
    //     var e = new NavNode { Id = 5, Position = new Vector2(100, 500) };
    //     var f = new NavNode { Id = 6, Position = new Vector2(700, 500) };

    //     _nav.AddNode(a); _nav.AddNode(b); _nav.AddNode(c);
    //     _nav.AddNode(d); _nav.AddNode(e); _nav.AddNode(f);

    //     int id = 1;
    //     _nav.AddHighway(Highway.Create(id++, 1, 2, a.Position, b.Position, 90f));
    //     _nav.AddHighway(Highway.Create(id++, 2, 3, b.Position, c.Position, 90f));
    //     _nav.AddHighway(Highway.Create(id++, 2, 4, b.Position, d.Position, 90f));
    //     _nav.AddHighway(Highway.Create(id++, 4, 5, d.Position, e.Position, 90f));
    //     _nav.AddHighway(Highway.Create(id++, 4, 6, d.Position, f.Position, 90f));
    //     _nav.AddHighway(Highway.Create(id++, 1, 5, a.Position, e.Position, 90f));
    //     _nav.AddHighway(Highway.Create(id++, 3, 6, c.Position, f.Position, 90f));
    // }
    private Vector2 GetNearestEnemyDirection()
    {
        var player = _controller.Player;
        float   bestDist = float.MaxValue;
        Vector2 bestDir  = Vector2.Zero;

        // Check all enemies
        var allEnemies = new List<Enemy>();
        allEnemies.AddRange(_enemySpawner.Walkers);
        allEnemies.AddRange(_enemySpawner.Runners);
        allEnemies.AddRange(_enemySpawner.Shooters);
        allEnemies.AddRange(_enemySpawner.Tanks);

        if (_bossSpawned && !_boss.IsDead)
            allEnemies.Add(_boss);

        foreach (var enemy in allEnemies)
        {
            if (enemy.IsDead) continue;
            float dist = Vector2.Distance(player.Position, enemy.Position);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestDir  = Vector2.Normalize(enemy.Position - player.Position);
            }
        }

        return bestDir;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private Vector2 GetCursorWorld()
    {
        Matrix inv = Matrix.Invert(_camera.get_transformation(HQ.GraphicsDevice));
        return Vector2.Transform(HQ.Input.Mouse.Position.ToVector2(), inv);
    }

    private void DrawHealthBar(Vector2 pos, int width, int height,
                                int current, int max, Color color)
    {
        float ratio = (float)current / max;

        HQ.SpriteBatch.Draw(_pixel,
            new Rectangle((int)pos.X, (int)pos.Y, width, height),
            Color.Black * 0.7f);

        HQ.SpriteBatch.Draw(_pixel,
            new Rectangle((int)pos.X, (int)pos.Y, (int)(width * ratio), height),
            color);
    }

    private void DrawCircle(Vector2 center, float radius, Color color, int segments = 48)
    {
        float   step = MathF.PI * 2f / segments;
        Vector2 prev = center + new Vector2(radius, 0);

        for (int i = 1; i <= segments; i++)
        {
            float   angle = step * i;
            Vector2 next  = center + new Vector2(
                MathF.Cos(angle) * radius,
                MathF.Sin(angle) * radius);
            DrawLine(prev, next, color, 2f);
            prev = next;
        }
    }

    private void DrawLine(Vector2 start, Vector2 end, Color color, float thickness)
    {
        Vector2 delta  = end - start;
        float   length = delta.Length();
        if (length < 0.001f) return;

        float angle = MathF.Atan2(delta.Y, delta.X);
        HQ.SpriteBatch.Draw(_pixel, start, null, color, angle,
            Vector2.Zero, new Vector2(length, thickness),
            SpriteEffects.None, 0f);
    }

    private void DrawHintSquare()
    {
        float pulse = (MathF.Sin((float)Environment.TickCount64 / 300f) + 1f) * 0.5f;

        Vector2 topLeft = _camera.Pos + new Vector2(
            -HQ.GraphicsDevice.Viewport.Width  * 0.5f + 10f,
            -HQ.GraphicsDevice.Viewport.Height * 0.5f + 10f);

        HQ.SpriteBatch.Draw(_pixel,
            new Rectangle((int)topLeft.X, (int)topLeft.Y, 12, 12),
            Color.Magenta * (0.4f + 0.6f * pulse));
    }

    private void DrawPlayerHp()
    {
        var    player = _controller.Player;
        string text   = $"{player.Health.CurrentHealth}/{player.Health.MaxHealth}";
        DrawPixelString(text, new Vector2(10, 30), 2, Color.LimeGreen);
    }

    private static readonly Dictionary<char, byte[]> s_glyphs = new()
    {
        ['0'] = new byte[] { 0b111, 0b101, 0b101, 0b101, 0b111 },
        ['1'] = new byte[] { 0b010, 0b110, 0b010, 0b010, 0b111 },
        ['2'] = new byte[] { 0b111, 0b001, 0b111, 0b100, 0b111 },
        ['3'] = new byte[] { 0b111, 0b001, 0b111, 0b001, 0b111 },
        ['4'] = new byte[] { 0b101, 0b101, 0b111, 0b001, 0b001 },
        ['5'] = new byte[] { 0b111, 0b100, 0b111, 0b001, 0b111 },
        ['6'] = new byte[] { 0b111, 0b100, 0b111, 0b101, 0b111 },
        ['7'] = new byte[] { 0b111, 0b001, 0b001, 0b001, 0b001 },
        ['8'] = new byte[] { 0b111, 0b101, 0b111, 0b101, 0b111 },
        ['9'] = new byte[] { 0b111, 0b101, 0b111, 0b001, 0b111 },
        ['/'] = new byte[] { 0b001, 0b001, 0b010, 0b100, 0b100 },
    };

    private void DrawPixelString(string text, Vector2 pos, int scale, Color color)
    {
        float cursorX = pos.X;

        foreach (char c in text)
        {
            if (s_glyphs.TryGetValue(c, out byte[] glyph))
            {
                for (int row = 0; row < 5; row++)
                for (int col = 0; col < 3; col++)
                {
                    if ((glyph[row] & (1 << (2 - col))) != 0)
                        HQ.SpriteBatch.Draw(_pixel,
                            new Rectangle(
                                (int)cursorX + col * scale,
                                (int)pos.Y   + row * scale,
                                scale, scale),
                            color);
                }
                cursorX += 4 * scale;
            }
        }
    }

    private void DamagePlayer(int amount, float strenght = 1f, int ms = 1500)
    {
        _controller.Player.Health.TakeDamage(amount);
        _controller.Player.TriggerHitFlash();
        HQ.Input.GamePads[0].SetVibration(strenght, TimeSpan.FromMicroseconds(ms));
    }

    // ── Texture helpers ───────────────────────────────────────────────────

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
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

namespace LILITH.Core.Scenes;

public class GameScene : Scene
{
    // ── Core ──────────────────────────────────────────────────────────────

    private PlayerController _controller = null!;
    private Camera           _camera     = null!;
    private Texture2D        _pixel      = null!;
    private SpriteFont       _font       = null!;

    private const float CAMERA_LERP = 0.1f;
    private float _isDeathTimer;

    // ── Exp and UI ─────────────────────────────────────────────────────────

    private ExperienceSpawner _xpSpawner = null!;
    private ExperienceBar     _xpBar     = null!;
    private LevelUpScreen     _levelUp   = null!;
    private bool              _isPaused;

    private const int  CARD_COUNT    = 3;
    private IAbility[] _currentCards = Array.Empty<IAbility>();
    private readonly Random _random  = new();
    private GameOverScreen _gameOver = null!;
    private bool           _isGameOver;

    // ── Pause Menu ────────────────────────────────────────────────────────

    private bool          _isPauseMenu = false;
    private Button        _btnResume   = null!;
    private Button        _btnMainMenu = null!;
    private KeyboardState _prevKeys;

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

        _font = Content.Load<SpriteFont>("DefaultFont");

        // ── Player ──
        var atlas  = TextureAtlas.FromFile(Content, "player.xml");
        var idle   = atlas.CreateAnimatedSprite("idle");
        var walk   = atlas.CreateAnimatedSprite("walk");
        var death  = atlas.CreateAnimatedSprite("death");

        var player = new Player(idle, new Vector2(400, 300), hp: 50);
        _controller = new PlayerController(player, _pixel, idle, walk, death);
        _controller.AddAbility(new AutoShootAbility());

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

        _btnResume   = new Button(new Rectangle(cx - 110, cy - 65, 220, 50), "RESUME");
        _btnMainMenu = new Button(new Rectangle(cx - 110, cy + 15,  220, 50), "MAIN MENU");

        _btnResume.OnClick   += () => _isPauseMenu = false;
        _btnMainMenu.OnClick += () => HQ.ChangeScene(new MainMenuScene());

        // ── Enemies ──
        _agentRegion = MakeSolidRegion(8, 8, Color.White);
        var projectileRegion = MakeSolidRegion(6, 3, Color.Yellow);

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

        _enemySpawner = new WaveSpawner();
        _enemySpawner.SetNavigation(_nav);

        _enemySpawner.AddSpawnPoint(new Vector2(100, 100));
        _enemySpawner.AddSpawnPoint(new Vector2(700, 100));
        _enemySpawner.AddSpawnPoint(new Vector2(700, 500));
        _enemySpawner.AddSpawnPoint(new Vector2(100, 500));

        _enemySpawner.RegisterFactory(EnemyType.Walker, pos =>
        {
            var s = MakeAnimSprite(MakeSolidRegion(16, 16, Color.Green));
            return new Enemies.Walker.Walker(s, pos);
        });

        _enemySpawner.RegisterFactory(EnemyType.Runner, pos =>
        {
            var s = MakeAnimSprite(MakeSolidRegion(12, 12, Color.Cyan));
            return new Enemies.Runner.Runner(s, pos);
        });

        _enemySpawner.RegisterFactory(EnemyType.Shooter, pos =>
        {
            var s       = MakeAnimSprite(MakeSolidRegion(16, 16, Color.Orange));
            var shooter = new Enemies.Shooter.Shooter(s, pos)
            {
                ProjectileFactory = (pPos, dir) => new MainEngine.Projectile.Projectile
                {
                    Position  = pPos,
                    Direction = dir,
                    Region    = projectileRegion,
                    Speed     = 200f,
                    LifeTime  = 3f
                }
            };
            return shooter;
        });

        _enemySpawner.RegisterFactory(EnemyType.Tank, pos =>
        {
            var s = MakeAnimSprite(MakeSolidRegion(32, 32, Color.Red));
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

        _enemySpawner.AddWave(new Wave
        {
            Entries = new List<SpawnEntry>
            {
                new() { Type = EnemyType.Walker, Count = 3, DelayBetween = 0.8f },
                new() { Type = EnemyType.Runner, Count = 2, DelayBetween = 0.4f },
            },
            DelayAfterWave = 6f
        });

        _enemySpawner.AddWave(new Wave
        {
            Entries = new List<SpawnEntry>
            {
                new() { Type = EnemyType.Shooter, Count = 2, DelayBetween = 1f   },
                new() { Type = EnemyType.Walker,  Count = 4, DelayBetween = 0.6f },
                new() { Type = EnemyType.Runner,  Count = 3, DelayBetween = 0.3f },
            },
            DelayAfterWave = 8f
        });

        _enemySpawner.AddWave(new Wave
        {
            Entries = new List<SpawnEntry>
            {
                new() { Type = EnemyType.Tank,    Count = 1, DelayBetween = 0f   },
                new() { Type = EnemyType.Shooter, Count = 2, DelayBetween = 1f   },
                new() { Type = EnemyType.Runner,  Count = 5, DelayBetween = 0.3f },
            },
            DelayAfterWave = 10f
        });

        _enemySpawner.Start();
    }

    // ── Update ────────────────────────────────────────────────────────────

    public override void Update(GameTime gameTime)
    {
        // Esc переключает меню паузы (только если не открыт экран левелапа)
        KeyboardState keys = Keyboard.GetState();
        if (keys.IsKeyDown(Keys.Escape) && _prevKeys.IsKeyUp(Keys.Escape) && !_isPaused)
            _isPauseMenu = !_isPauseMenu;
        _prevKeys = keys;

        // Меню паузы перехватывает весь ввод
        if (_isPauseMenu)
        {
            _btnResume.Update(gameTime);
            _btnMainMenu.Update(gameTime);
            return;
        }

        // UI обновляется даже при левелап-паузе
        _xpBar.Update(gameTime);
        _levelUp.Update(gameTime, HQ.GraphicsDevice.Viewport);

        if (HQ.Input.Keyboard.WasKeyJustPressed(Keys.F5))
            SpawnBoss();

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
        Vector2 nearestEnemyDir = GetNearestEnemyDirection();
        Vector2 cursorWorld     = GetCursorWorld();
        _controller.Update(gameTime, nearestEnemyDir, cursorWorld);
        if (!_isGameOver && _controller.Player.Health.IsDead)
        {
            _isGameOver   = true;
            _isDeathTimer = 0f;
        }
        
        CheckAbilityHits();

        // ── Camera ──
        _camera.Pos = Vector2.Lerp(_camera.Pos, _controller.Player.Center, CAMERA_LERP);

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
                player.Health.TakeDamage(_boss.PendingPlayerDamage);

            if (_boss.ActiveForceSources.Count > 0)
                _forceSources.AddRange(_boss.ActiveForceSources);
        }

        // ── Collisions: melee ──
        CheckMeleeHits(_enemySpawner.Walkers);
        CheckMeleeHits(_enemySpawner.Runners);

        // ── Collisions: tank agents → player ──
        float playerRadius = player.GetBounds().Radius;
        foreach (var tank in _enemySpawner.Tanks)
        {
            int damage = tank.ProcessAgentHits(player.Position, playerRadius);
            if (damage > 0)
                player.Health.TakeDamage(damage);
        }

        // ── Collisions: shooter projectiles → player ──
        Circle shooterPlayerBounds = player.GetBounds();
        foreach (var shooter in _enemySpawner.Shooters)
        {
            foreach (var p in shooter.Projectiles)
            {
                if (!p.IsDead && p.Bounds.Intersects(shooterPlayerBounds))
                {
                    player.Health.TakeDamage(shooter.Damage);
                    p.Hit = true;
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

        _levelUp.Draw(HQ.SpriteBatch, _pixel, _font, HQ.GraphicsDevice.Viewport);

        if (_isPauseMenu)
            DrawPauseMenu();
        
        _gameOver.Draw(HQ.SpriteBatch, _pixel, HQ.GraphicsDevice.Viewport);
        HQ.SpriteBatch.End();
    }

    // ── Pause Menu ────────────────────────────────────────────────────────

    private void DrawPauseMenu()
    {
        var vp = HQ.GraphicsDevice.Viewport;

        // Затемнение
        HQ.SpriteBatch.Draw(_pixel,
            new Rectangle(0, 0, vp.Width, vp.Height),
            new Color(0, 0, 0, 160));

        // Панель
        int pw = 280, ph = 200;
        int px = (vp.Width  - pw) / 2;
        int py = (vp.Height - ph) / 2;
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px, py, pw, ph), new Color(20, 20, 40, 240));

        // Рамка
        const int B = 2;
        var border = new Color(160, 160, 220, 200);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px,      py,        pw, B),  border);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px,      py+ph-B,   pw, B),  border);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px,      py,        B,  ph), border);
        HQ.SpriteBatch.Draw(_pixel, new Rectangle(px+pw-B, py,        B,  ph), border);

        // Заголовок PAUSED — сдвинут выше панели
        if (_font != null)
        {
            string  title = "PAUSED";
            Vector2 size  = _font.MeasureString(title);
            Vector2 pos   = new Vector2((vp.Width - size.X) * 0.5f, py - 35);
            HQ.SpriteBatch.DrawString(_font, title, pos + new Vector2(1, 1), Color.Black);
            HQ.SpriteBatch.DrawString(_font, title, pos, Color.White);
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
                        if (enemy.Health.IsDead)
                            enemy.ApplyDeath();
                            _xpSpawner.SpawnOrb(enemy.Position);
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
                        if (_boss.Health.IsDead)
                        {
                            _boss.ApplyDeath();
                            _xpSpawner.SpawnOrb(_boss.Position, value: 10);
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
        var bossRegion = MakeSolidRegion(40, 40, Color.DarkRed);
        var bossSprite = MakeAnimSprite(bossRegion);
        bossSprite.Scale = new Vector2(2f);

        Vector2 spawnPos = _controller.Player.Position + new Vector2(400f, 0f);
        _boss        = new Boss(bossSprite, spawnPos, hp: 20);
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
                    player.Health.TakeDamage(w.Damage);
            }
            else if (enemy is Enemies.Runner.Runner r && r.IsAttacking && r.CanAttack)
            {
                if (Vector2.Distance(r.Position, player.Position) <= r.AttackRange
                    && r.TryAttack())
                    player.Health.TakeDamage(r.Damage);
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
    private Vector2 GetNearestEnemyDirection()
    {
        var player = _controller.Player;
        float   bestDist = float.MaxValue;
        Vector2 bestDir  = Vector2.Zero;

        // Проверяем всех врагов
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
        MouseState mouse = Mouse.GetState();
        Matrix     inv   = Matrix.Invert(_camera.get_transformation(HQ.GraphicsDevice));
        return Vector2.Transform(new Vector2(mouse.X, mouse.Y), inv);
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

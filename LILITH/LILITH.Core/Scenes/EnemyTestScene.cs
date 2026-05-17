using System;
using System.Collections.Generic;
using MainEngine;
using MainEngine.Scenes;
using MainEngine.Camera;
using MainEngine.Entities;
using MainEngine.FlockEnemy;
using MainEngine.Graphics;
using MainEngine.Input;
using MainEngine.Navigation;
using LILITH.Core.Enemies;
using LILITH.Core.Enemies.Boss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LILITH.Core.Scenes
{
    public class EnemyTestScene : Scene
    {
        private Player _player;
        private Camera _camera;

        private Navigation _nav;
        private EnemySpawner _spawner;

        private TextureRegion _agentRegion;
        private AgentConfig _agentConfig;
        private readonly List<ForceSource> _agentForceSources = new();

        // --- Boss ---
        private Boss _boss;
        private bool _bossSpawned;
        private Texture2D _debugPixel;

        public override void Initialize()
        {
            HQ.ExitOnEscape = false;
            _camera = new Camera();
            base.Initialize();
        }

        public override void LoadContent()
        {
            _debugPixel = new Texture2D(HQ.GraphicsDevice, 1, 1);
            _debugPixel.SetData(new[] { Color.White });

            // ── Shared textures ──
            _agentRegion = MakeSolidRegion(8, 8, Color.White);
            var projectileRegion = MakeSolidRegion(6, 3, Color.Yellow);

            // ── Player ──
            var playerRegion = MakeSolidRegion(24, 24, Color.CornflowerBlue);
            var playerSprite = MakeAnimSprite(playerRegion);
            _player = new Player(playerSprite, new Vector2(200, 200), 50);

            // ── Flocking config (shared by all tanks) ──
            _agentConfig = new AgentConfig
            {
                AgentSpeed = 65f,
                RepulsionRadius = 50f,
                AlignmentRadius = 100f,
                AttractionRadius = 200f,
                AttractionAngle = MathHelper.ToRadians(70f),
                RepulsionForce = 10f,
                AlignmentForce = 5f,
                AttractionForce = 2f,
                GravitationForce = 0.5f,
                DebugVisible = false
            };

            // ── Navigation graph ──
            _nav = new Navigation();
            BuildNavGraph();

            // ── Spawner ──
            _spawner = new EnemySpawner();
            _spawner.SetNavigation(_nav);

            _spawner.AddSpawnPoint(new Vector2(100, 100));
            _spawner.AddSpawnPoint(new Vector2(700, 100));
            _spawner.AddSpawnPoint(new Vector2(700, 500));
            _spawner.AddSpawnPoint(new Vector2(100, 500));

            // Register factories
            _spawner.RegisterFactory(EnemyType.Walker, pos =>
            {
                var s = MakeAnimSprite(MakeSolidRegion(16, 16, Color.Green));
                return new Enemies.Walker.Walker(s, pos);
            });

            _spawner.RegisterFactory(EnemyType.Runner, pos =>
            {
                var s = MakeAnimSprite(MakeSolidRegion(12, 12, Color.Cyan));
                return new Enemies.Runner.Runner(s, pos);
            });

            _spawner.RegisterFactory(EnemyType.Shooter, pos =>
            {
                var s = MakeAnimSprite(MakeSolidRegion(16, 16, Color.Orange));
                var shooter = new Enemies.Shooter.Shooter(s, pos)
                {
                    ProjectileFactory = (pPos, dir) => new MainEngine.Projectile.Projectile
                    {
                        Position = pPos,
                        Direction = dir,
                        Region = projectileRegion,
                        Speed = 200f,
                        LifeTime = 3f
                    }
                };
                return shooter;
            });

            _spawner.RegisterFactory(EnemyType.Tank, pos =>
            {
                var s = MakeAnimSprite(MakeSolidRegion(32, 32, Color.Red));
                return new Enemies.Tank.Tank(s, pos)
                {
                    AgentSpawnIntervalSeconds = 2f,
                    MaxAgents = 15,
                    AgentDamage = 1,
                    AgentHitRadius = 10f,
                    AgentFactory = aPos =>
                    {
                        var agent = new Agent(_agentRegion, aPos);
                        agent.Scale = new Vector2(1f);
                        return agent;
                    }
                };
            });

            // ── Define waves ──
            _spawner.AddWave(new Enemies.Wave
            {
                Entries = new List<Enemies.SpawnEntry>
                {
                    new() { Type = EnemyType.Walker, Count = 3, DelayBetween = 0.8f },
                    new() { Type = EnemyType.Runner, Count = 2, DelayBetween = 0.4f },
                },
                DelayAfterWave = 6f
            });

            _spawner.AddWave(new Enemies.Wave
            {
                Entries = new List<Enemies.SpawnEntry>
                {
                    new() { Type = EnemyType.Shooter, Count = 2, DelayBetween = 1f },
                    new() { Type = EnemyType.Walker, Count = 4, DelayBetween = 0.6f },
                    new() { Type = EnemyType.Runner, Count = 3, DelayBetween = 0.3f },
                },
                DelayAfterWave = 8f
            });

            _spawner.AddWave(new Enemies.Wave
            {
                Entries = new List<Enemies.SpawnEntry>
                {
                    new() { Type = EnemyType.Tank, Count = 1, DelayBetween = 0f },
                    new() { Type = EnemyType.Shooter, Count = 2, DelayBetween = 1f },
                    new() { Type = EnemyType.Runner, Count = 5, DelayBetween = 0.3f },
                },
                DelayAfterWave = 10f
            });

            _spawner.Start();
        }

        public override void Update(GameTime gameTime)
        {
            // ── F5 = spawn / respawn boss ──
            if (HQ.Input.Keyboard.WasKeyJustPressed(Keys.F5))
                SpawnBoss();

            _player.Update(gameTime);

            _spawner.Update(gameTime, _player.Position, _player.Health.IsDead);
            _spawner.UpdateTanks(gameTime, _player.Position,
                _player.Health.IsDead, _agentConfig, _agentForceSources);

            // ── Boss ──
            if (_bossSpawned && !_boss.IsDead)
            {
                Circle playerBounds = _player.GetBounds();
                _boss.Update(gameTime, _player.Position, playerBounds, _player.Health.IsDead);

                if (_boss.PendingPlayerDamage > 0)
                    _player.Health.TakeDamage(_boss.PendingPlayerDamage);

                // Shockwaves scatter tank agents
                if (_boss.ActiveForceSources.Count > 0)
                    _agentForceSources.AddRange(_boss.ActiveForceSources);
            }

            // ── Collision: melee enemies ──
            CheckMeleeHits(_spawner.Walkers);
            CheckMeleeHits(_spawner.Runners);

            // ── Collision: agents → player ──
            float playerRadius = _player.GetBounds().Radius;
            foreach (var tank in _spawner.Tanks)
            {
                int damage = tank.ProcessAgentHits(_player.Position, playerRadius);
                if (damage > 0)
                    _player.Health.TakeDamage(damage);
            }

            // ── Collision: shooter projectiles → player ──
            Circle shooterPlayerBounds = _player.GetBounds();
            foreach (var shooter in _spawner.Shooters)
            {
                foreach (var p in shooter.Projectiles)
                {
                    if (!p.IsDead && p.Bounds.Intersects(shooterPlayerBounds))
                    {
                        _player.Health.TakeDamage(shooter.Damage);
                        p.Hit = true;
                    }
                }
            }

            _camera.Pos = _player.Position;
        }

        public override void Draw(GameTime gameTime)
        {
            HQ.GraphicsDevice.Clear(Color.Black);

            HQ.SpriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: _camera.get_transformation(HQ.GraphicsDevice));

            _player.Draw(gameTime, HQ.SpriteBatch);
            _spawner.Draw(gameTime, HQ.SpriteBatch);

            // ── Boss ──
            if (_bossSpawned)
            {
                // Warning zones (telegraphs + flashes)
                foreach (Circle zone in _boss.WarningZones)
                {
                    DrawCircle(
                        zone.Location.ToVector2(),
                        zone.Radius,
                        Color.Red * 0.3f);
                }

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

            // ── HUD: F5 hint ──
            if (!_bossSpawned)
                DrawHintSquare();

            HQ.SpriteBatch.End();

            // ── Screen-space HUD ──
            HQ.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            DrawPlayerHp();
            HQ.SpriteBatch.End();
        }

        // ────────────────────────────────────────
        //  Boss
        // ────────────────────────────────────────

        private void SpawnBoss()
        {
            var bossRegion = MakeSolidRegion(40, 40, Color.DarkRed);
            var bossSprite = MakeAnimSprite(bossRegion);
            bossSprite.Scale = new Vector2(2f);

            Vector2 spawnPos = _player.Position + new Vector2(400f, 0f);
            _boss = new Boss(bossSprite, spawnPos, hp: 20);
            _bossSpawned = true;
        }

        // ────────────────────────────────────────
        //  Collision helpers
        // ────────────────────────────────────────

        private void CheckMeleeHits<T>(IReadOnlyList<T> enemies) where T : Enemy
        {
            foreach (var enemy in enemies)
            {
                if (enemy.IsDead) continue;

                if (enemy is Enemies.Walker.Walker w && w.IsAttacking && w.CanAttack)
                {
                    if (Vector2.Distance(w.Position, _player.Position) <= w.AttackRange
                        && w.TryAttack())
                        _player.Health.TakeDamage(w.Damage);
                }
                else if (enemy is Enemies.Runner.Runner r && r.IsAttacking && r.CanAttack)
                {
                    if (Vector2.Distance(r.Position, _player.Position) <= r.AttackRange
                        && r.TryAttack())
                        _player.Health.TakeDamage(r.Damage);
                }
            }
        }

        // ────────────────────────────────────────
        //  Nav graph
        // ────────────────────────────────────────

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

        // ────────────────────────────────────────
        //  Drawing helpers
        // ────────────────────────────────────────

        private void DrawHealthBar(Vector2 pos, int width, int height, int current, int max, Color color)
        {
            float ratio = (float)current / max;

            HQ.SpriteBatch.Draw(_debugPixel,
                new Rectangle((int)pos.X, (int)pos.Y, width, height),
                Color.Black * 0.7f);

            HQ.SpriteBatch.Draw(_debugPixel,
                new Rectangle((int)pos.X, (int)pos.Y, (int)(width * ratio), height),
                color);
        }

        private void DrawCircle(Vector2 center, float radius, Color color, int segments = 48)
        {
            float step = MathF.PI * 2f / segments;
            Vector2 prev = center + new Vector2(radius, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = step * i;
                Vector2 next = center + new Vector2(
                    MathF.Cos(angle) * radius,
                    MathF.Sin(angle) * radius);
                DrawLine(prev, next, color, 2f);
                prev = next;
            }
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 delta = end - start;
            float length = delta.Length();
            if (length < 0.001f) return;

            float angle = MathF.Atan2(delta.Y, delta.X);
            HQ.SpriteBatch.Draw(_debugPixel, start, null, color, angle,
                Vector2.Zero, new Vector2(length, thickness),
                SpriteEffects.None, 0f);
        }

        private void DrawHintSquare()
        {
            float pulse = (MathF.Sin((float)Environment.TickCount64 / 300f) + 1f) * 0.5f;

            Vector2 topLeft = _camera.Pos + new Vector2(
                -HQ.GraphicsDevice.Viewport.Width * 0.5f + 10f,
                -HQ.GraphicsDevice.Viewport.Height * 0.5f + 10f);

            HQ.SpriteBatch.Draw(_debugPixel,
                new Rectangle((int)topLeft.X, (int)topLeft.Y, 12, 12),
                Color.Magenta * (0.4f + 0.6f * pulse));
        }

        // ────────────────────────────────────────
        //  Player HP (screen-space pixel font)
        // ────────────────────────────────────────

        private void DrawPlayerHp()
        {
            string text = $"{_player.Health.CurrentHealth}/{_player.Health.MaxHealth}";
            DrawPixelString(text, new Vector2(10, 10), 2, Color.LimeGreen);
        }

        // 3×5 bitmap glyphs for 0-9 and /
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
                    {
                        for (int col = 0; col < 3; col++)
                        {
                            bool on = (glyph[row] & (1 << (2 - col))) != 0;
                            if (on)
                            {
                                HQ.SpriteBatch.Draw(_debugPixel,
                                    new Rectangle(
                                        (int)cursorX + col * scale,
                                        (int)pos.Y + row * scale,
                                        scale, scale),
                                    color);
                            }
                        }
                    }
                    cursorX += 4 * scale; // 3 wide + 1 gap
                }
            }
        }

        // ────────────────────────────────────────
        //  Texture helpers
        // ────────────────────────────────────────

        private TextureRegion MakeSolidRegion(int w, int h, Color color)
        {
            var tex = new Texture2D(HQ.GraphicsDevice, w, h);
            var data = new Color[w * h];
            Array.Fill(data, color);
            tex.SetData(data);
            return new TextureRegion(tex, 0, 0, w, h);
        }

        private AnimatedSprite MakeAnimSprite(TextureRegion region)
        {
            var anim = new Animation(
                new List<TextureRegion> { region },
                TimeSpan.FromMilliseconds(100));
            var sprite = new AnimatedSprite(anim);
            sprite.CenterOrigin();
            return sprite;
        }
    }
}
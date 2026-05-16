using System;
using System.Collections.Generic;
using MainEngine;
using MainEngine.Scenes;
using MainEngine.Camera;
using MainEngine.Entities;
using MainEngine.FlockEnemy;
using MainEngine.Graphics;
using MainEngine.Navigation;
using LILITH.Core.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

        public override void Initialize()
        {
            HQ.ExitOnEscape = false;
            _camera = new Camera();
            base.Initialize();
        }

        public override void LoadContent()
        {
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
            _player.Update(gameTime);

            // Spawner ticks non-tank enemies internally
            _spawner.Update(gameTime, _player.Position, _player.Health.IsDead);
            _spawner.UpdateTanks(gameTime, _player.Position,
                _player.Health.IsDead, _agentConfig, _agentForceSources);

            // ── Collision: melee enemies ──
            CheckMeleeHits(_spawner.Walkers);
            CheckMeleeHits(_spawner.Runners);

            // ── Collision: agents → player (self-destruct on hit) ──
            float playerRadius = _player.GetBounds().Radius;
            foreach (var tank in _spawner.Tanks)
            {
                int damage = tank.ProcessAgentHits(_player.Position, playerRadius);
                if (damage > 0)
                    _player.Health.TakeDamage(damage);
            }

            // ── Collision: shooter projectiles → player ──
            Circle playerBounds = _player.GetBounds();
            foreach (var shooter in _spawner.Shooters)
            {
                foreach (var p in shooter.Projectiles)
                {
                    if (!p.IsDead && p.Bounds.Intersects(playerBounds))
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

            HQ.SpriteBatch.End();
        }

        // ────────────────────────────────────────
        //  Helpers
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
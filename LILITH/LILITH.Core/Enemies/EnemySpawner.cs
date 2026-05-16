using System;
using System.Collections.Generic;
using MainEngine.Entities;
using MainEngine.Navigation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LILITH.Core.Enemies
{
    /// <summary>
    /// Defines what to spawn: which factory, how many, and the delay between each.
    /// </summary>
    public class SpawnEntry
    {
        public EnemyType Type { get; set; }
        public int Count { get; set; } = 1;
        public float DelayBetween { get; set; } = 0.5f;
    }

    /// <summary>
    /// A full wave: a list of spawn entries plus time before next wave.
    /// </summary>
    public class Wave
    {
        public List<SpawnEntry> Entries { get; set; } = new();
        public float DelayAfterWave { get; set; } = 5f;
    }

    public enum EnemyType { Walker, Runner, Shooter, Tank }

    /// <summary>
    /// Manages spawning enemies in waves at designated spawn points.
    /// The scene registers factory functions for each enemy type; the spawner
    /// calls them when it's time to create an enemy and assigns navigation.
    /// </summary>
    public class EnemySpawner
    {
        private readonly List<Vector2> _spawnPoints = new();
        private readonly List<Wave> _waves = new();
        private readonly Dictionary<EnemyType, Func<Vector2, Enemy>> _factories = new();
        private readonly Random _rng = new();

        private Navigation _navigation;
        private int _currentWaveIndex;
        private int _currentEntryIndex;
        private int _spawnedInEntry;
        private float _timer;
        private bool _spawningEntry;
        private bool _finished;

        // ── All live enemies, regardless of type ──
        private readonly List<Enemy> _activeEnemies = new();
        public IReadOnlyList<Enemy> ActiveEnemies => _activeEnemies;

        // ── Typed access so the scene can do collision / drawing per type ──
        private readonly List<Tank.Tank> _tanks = new();
        private readonly List<Walker.Walker> _walkers = new();
        private readonly List<Runner.Runner> _runners = new();
        private readonly List<Shooter.Shooter> _shooters = new();

        public IReadOnlyList<Tank.Tank> Tanks => _tanks;
        public IReadOnlyList<Walker.Walker> Walkers => _walkers;
        public IReadOnlyList<Runner.Runner> Runners => _runners;
        public IReadOnlyList<Shooter.Shooter> Shooters => _shooters;

        public int CurrentWave => _currentWaveIndex;
        public bool IsFinished => _finished;

        // ────────────────────────────────────────
        //  Setup
        // ────────────────────────────────────────

        public void SetNavigation(Navigation navigation)
        {
            _navigation = navigation;
        }

        public void AddSpawnPoint(Vector2 position)
        {
            _spawnPoints.Add(position);
        }

        public void AddWave(Wave wave)
        {
            _waves.Add(wave);
        }

        /// <summary>
        /// Register a factory that creates an enemy at a given position.
        /// The spawner will assign a NavigationFollower automatically.
        /// </summary>
        public void RegisterFactory(EnemyType type, Func<Vector2, Enemy> factory)
        {
            _factories[type] = factory;
        }

        /// <summary>
        /// Call after all waves and factories are set up.
        /// </summary>
        public void Start()
        {
            _currentWaveIndex = 0;
            _currentEntryIndex = 0;
            _spawnedInEntry = 0;
            _timer = 0f;
            _spawningEntry = true;
            _finished = _waves.Count == 0;
        }

        // ────────────────────────────────────────
        //  Per-frame
        // ────────────────────────────────────────

        public void Update(GameTime gameTime, Vector2 playerPosition, bool playerIsDead)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Advance spawner state machine
            if (!_finished)
                AdvanceSpawning(dt);

            // Update every live enemy
            UpdateEnemies(gameTime, playerPosition, playerIsDead);

            // Remove dead
            CleanupDead();
        }

        private void AdvanceSpawning(float dt)
        {
            if (_currentWaveIndex >= _waves.Count)
            {
                _finished = true;
                return;
            }

            _timer -= dt;
            if (_timer > 0f)
                return;

            Wave wave = _waves[_currentWaveIndex];

            if (_currentEntryIndex >= wave.Entries.Count)
            {
                // Wave complete — pause then move to next
                _currentWaveIndex++;
                _currentEntryIndex = 0;
                _spawnedInEntry = 0;
                _timer = wave.DelayAfterWave;
                return;
            }

            SpawnEntry entry = wave.Entries[_currentEntryIndex];

            if (_spawnedInEntry < entry.Count)
            {
                SpawnOne(entry.Type);
                _spawnedInEntry++;
                _timer = entry.DelayBetween;
            }

            if (_spawnedInEntry >= entry.Count)
            {
                _currentEntryIndex++;
                _spawnedInEntry = 0;
            }
        }

        private void SpawnOne(EnemyType type)
        {
            if (!_factories.TryGetValue(type, out var factory))
                return;

            if (_spawnPoints.Count == 0)
                return;

            Vector2 pos = _spawnPoints[_rng.Next(_spawnPoints.Count)];
            Enemy enemy = factory(pos);
            if (enemy == null)
                return;

            // Assign navigation
            if (_navigation != null)
            {
                var follower = new NavigationFollower(_navigation);
                AssignNavFollower(enemy, follower);
            }

            _activeEnemies.Add(enemy);

            // Track in typed list
            switch (enemy)
            {
                case Tank.Tank t: _tanks.Add(t); break;
                case Walker.Walker w: _walkers.Add(w); break;
                case Runner.Runner r: _runners.Add(r); break;
                case Shooter.Shooter s: _shooters.Add(s); break;
            }
        }

        private static void AssignNavFollower(Enemy enemy, NavigationFollower follower)
        {
            switch (enemy)
            {
                case Tank.Tank t: t.NavFollower = follower; break;
                case Walker.Walker w: w.NavFollower = follower; break;
                case Runner.Runner r: r.NavFollower = follower; break;
                case Shooter.Shooter s: s.NavFollower = follower; break;
            }
        }

        // ────────────────────────────────────────
        //  Enemy updates (each type uses its own UpdateWithFSM)
        // ────────────────────────────────────────

        private void UpdateEnemies(GameTime gameTime, Vector2 playerPos, bool playerIsDead)
        {
            // Tanks need extra parameters for flocking
            // — the scene passes AgentConfig + shared force list via UpdateTanks()

            foreach (var w in _walkers)
                w.UpdateWithFSM(gameTime, playerPos, playerIsDead);

            foreach (var r in _runners)
                r.UpdateWithFSM(gameTime, playerPos, playerIsDead);

            foreach (var s in _shooters)
                s.UpdateWithFSM(gameTime, playerPos, playerIsDead);

            // Tanks are updated externally by the scene (need AgentConfig).
        }

        /// <summary>
        /// Call from the scene to update all tanks with flocking parameters.
        /// </summary>
        public void UpdateTanks(
            GameTime gameTime,
            Vector2 playerPosition,
            bool playerIsDead,
            MainEngine.FlockEnemy.AgentConfig agentConfig,
            List<MainEngine.FlockEnemy.ForceSource> sharedForces)
        {
            foreach (var t in _tanks)
                t.UpdateWithFSM(gameTime, playerPosition, playerIsDead,
                    agentConfig, sharedForces);
        }

        private void CleanupDead()
        {
            _activeEnemies.RemoveAll(e => e.IsDead);
            _tanks.RemoveAll(t => t.IsDead);
            _walkers.RemoveAll(w => w.IsDead);
            _runners.RemoveAll(r => r.IsDead);
            _shooters.RemoveAll(s => s.IsDead);
        }

        // ────────────────────────────────────────
        //  Drawing
        // ────────────────────────────────────────

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            foreach (var t in _tanks)
                t.DrawWithAgents(gameTime, spriteBatch);

            foreach (var w in _walkers)
                if (!w.IsDead) w.Draw(gameTime, spriteBatch);

            foreach (var r in _runners)
                if (!r.IsDead) r.Draw(gameTime, spriteBatch);

            foreach (var s in _shooters)
                s.DrawWithProjectiles(gameTime, spriteBatch);
        }
    }
}
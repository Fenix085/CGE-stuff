using System;
using System.Collections.Generic;
using MainEngine.Entities;
using MainEngine.FlockEnemy;
using MainEngine.Navigation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LILITH.Core.Enemies.Runner;
using LILITH.Core.Enemies.Shooter;
using LILITH.Core.Enemies.Tank;
using LILITH.Core.Enemies.Walker;

namespace LILITH.Core.Tools;

// ────────────────────────────────────────
//  Shared types
// ────────────────────────────────────────
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
/// Defines spawn weights for weighted random selection.
/// </summary>
public class EnemyWeight
{
    public EnemyType Type { get; set; }
    public float Weight { get; set; }

    public EnemyWeight() { }

    public EnemyWeight(EnemyType type, float weight)
    {
        Type = type;
        Weight = weight;
    }
}


public enum EnemyType { Walker, Runner, Shooter, Tank }

public enum SpawnShape { Circle, Rectangle }

/// <summary>
/// Controls where enemies can appear and how visibility is checked.
/// Mutable at runtime so the zone can shift or resize mid-game.
/// </summary>
public class SpawnZoneConfig
{
    /// <summary>Center of the spawn zone in world space.</summary>
    public Vector2 Position { get; set; }

    public SpawnShape Shape { get; set; } = SpawnShape.Circle;

    /// <summary>Radius when Shape is Circle.</summary>
    public float Radius { get; set; } = 400f;

    /// <summary>Width when Shape is Rectangle.</summary>
    public float Width { get; set; } = 800f;

    /// <summary>Height when Shape is Rectangle.</summary>
    public float Height { get; set; } = 600f;

    /// <summary>
    /// When false (default), points inside the player's view are rejected.
    /// Set true for pickups or anything that should appear on-screen.
    /// </summary>
    public bool AllowSpawnInView { get; set; } = false;

    /// <summary>
    /// Extra padding around the view rect to prevent edge pop-in.
    /// </summary>
    public float ViewPadding { get; set; } = 40f;
}

// ────────────────────────────────────────
//  Base Spawner
// ────────────────────────────────────────

/// <summary>
/// Shared infrastructure for all spawners: factories, positioning,
/// view culling, navigation, typed enemy lists, cleanup, and drawing.
///
/// Subclasses only need to implement their own timing logic and call
/// <see cref="SpawnEnemy"/> when it's time to create something.
/// </summary>
public abstract class Spawner
{
    private readonly SpawnZoneConfig _zone;
    private readonly Dictionary<EnemyType, Func<Vector2, Enemy>> _factories = new();
    private readonly List<Vector2> _spawnPoints = new();
    private Navigation _navigation;

    private readonly List<Enemy> _alive = new();
    private readonly List<Tank> _tanks = new();
    private readonly List<Walker> _walkers = new();
    private readonly List<Runner> _runners = new();
    private readonly List<Shooter> _shooters = new();

    private const int MaxPlacementAttempts = 30;

    // ── Public accessors ──

    public SpawnZoneConfig Zone => _zone;
    public IReadOnlyList<Enemy> Alive => _alive;
    public int AliveCount => _alive.Count;

    public IReadOnlyList<Tank> Tanks => _tanks;
    public IReadOnlyList<Walker> Walkers => _walkers;
    public IReadOnlyList<Runner> Runners => _runners;
    public IReadOnlyList<Shooter> Shooters => _shooters;

    protected Spawner(SpawnZoneConfig zone)
    {
        _zone = zone ?? new SpawnZoneConfig();
    }

    // ────────────────────────────────────────
    //  Setup
    // ────────────────────────────────────────

    public void RegisterFactory(EnemyType type, Func<Vector2, Enemy> factory)
        => _factories[type] = factory;

    public void SetNavigation(Navigation navigation)
        => _navigation = navigation;

    /// <summary>
    /// Add a fixed spawn point. When any exist the spawner picks
    /// from these instead of random zone positions.
    /// </summary>
    public void AddSpawnPoint(Vector2 position)
        => _spawnPoints.Add(position);

    public void ClearSpawnPoints()
        => _spawnPoints.Clear();

    /// <summary>
    /// Clear all living enemies and let the subclass reset its timers.
    /// </summary>
    public void Reset()
    {
        _alive.Clear();
        _tanks.Clear();
        _walkers.Clear();
        _runners.Clear();
        _shooters.Clear();
        OnReset();
    }

    protected virtual void OnReset() { }

    // ────────────────────────────────────────
    //  Per-frame (call from subclass)
    // ────────────────────────────────────────

    /// <summary>
    /// Update all living enemies and remove dead ones.
    /// Subclasses should call this at the start of their Update.
    /// </summary>
    protected void UpdateEnemies(GameTime gameTime, Vector2 playerPosition, bool playerIsDead)
    {
        CleanupDead();

        foreach (var w in _walkers)
            w.UpdateWithFSM(gameTime, playerPosition, playerIsDead);
        foreach (var r in _runners)
            r.UpdateWithFSM(gameTime, playerPosition, playerIsDead);
        foreach (var s in _shooters)
            s.UpdateWithFSM(gameTime, playerPosition, playerIsDead);
    }

    /// <summary>
    /// Tanks need flocking parameters the other types don't.
    /// Call separately after Update.
    /// </summary>
    public void UpdateTanks(GameTime gameTime, Vector2 playerPosition,
                            bool playerIsDead, AgentConfig agentConfig,
                            List<ForceSource> sharedForces)
    {
        foreach (var t in _tanks)
            t.UpdateWithFSM(gameTime, playerPosition, playerIsDead,
                            agentConfig, sharedForces);
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

    // ────────────────────────────────────────
    //  Queries
    // ────────────────────────────────────────

    public List<T> GetAlive<T>() where T : Enemy
    {
        var result = new List<T>();
        foreach (var e in _alive)
        {
            if (!e.IsDead && e is T typed)
                result.Add(typed);
        }
        return result;
    }

    public void ForEachAlive(Action<Enemy> action)
    {
        foreach (var e in _alive)
        {
            if (!e.IsDead)
                action(e);
        }
    }

    // ────────────────────────────────────────
    //  Spawn (called by subclasses)
    // ────────────────────────────────────────

    /// <summary>
    /// Create one enemy of the given type, place it, assign navigation,
    /// and register it in the typed lists.
    /// Returns true if the enemy was successfully spawned.
    /// </summary>
    protected bool SpawnEnemy(EnemyType type, Vector2 playerPos, Vector2 viewSize)
    {
        if (!_factories.TryGetValue(type, out var factory))
            return false;

        if (!TryPickPosition(playerPos, viewSize, out Vector2 pos))
            return false;

        Enemy enemy = factory(pos);
        if (enemy == null)
            return false;

        if (_navigation != null)
        {
            var follower = new NavigationFollower(_navigation);
            AssignNavFollower(enemy, follower);
        }

        _alive.Add(enemy);

        switch (enemy)
        {
            case Tank t:    _tanks.Add(t);    break;
            case Walker w:  _walkers.Add(w);  break;
            case Runner r:  _runners.Add(r);  break;
            case Shooter s: _shooters.Add(s); break;
        }

        return true;
    }

    // ────────────────────────────────────────
    //  Positioning
    // ────────────────────────────────────────

    private bool TryPickPosition(Vector2 playerPos, Vector2 viewSize, out Vector2 result)
    {
        float pad = _zone.ViewPadding;
        float halfW = viewSize.X * 0.5f + pad;
        float halfH = viewSize.Y * 0.5f + pad;

        for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
        {
            Vector2 candidate = _spawnPoints.Count > 0
                ? _spawnPoints[Random.Shared.Next(_spawnPoints.Count)]
                : PickRandomInZone();

            if (!_zone.AllowSpawnInView)
            {
                float dx = MathF.Abs(candidate.X - playerPos.X);
                float dy = MathF.Abs(candidate.Y - playerPos.Y);

                if (dx < halfW && dy < halfH)
                    continue;
            }

            result = candidate;
            return true;
        }

        if (_zone.AllowSpawnInView)
        {
            result = _spawnPoints.Count > 0
                ? _spawnPoints[Random.Shared.Next(_spawnPoints.Count)]
                : PickRandomInZone();
            return true;
        }

        result = Vector2.Zero;
        return false;
    }

    private Vector2 PickRandomInZone()
    {
        return _zone.Shape switch
        {
            SpawnShape.Circle    => PickInCircle(),
            SpawnShape.Rectangle => PickInRectangle(),
            _                    => _zone.Position
        };
    }

    private Vector2 PickInCircle()
    {
        float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
        float r = _zone.Radius * MathF.Sqrt(Random.Shared.NextSingle());
        return _zone.Position + new Vector2(
            MathF.Cos(angle) * r,
            MathF.Sin(angle) * r);
    }

    private Vector2 PickInRectangle()
    {
        float x = _zone.Position.X + (Random.Shared.NextSingle() - 0.5f) * _zone.Width;
        float y = _zone.Position.Y + (Random.Shared.NextSingle() - 0.5f) * _zone.Height;
        return new Vector2(x, y);
    }

    // ────────────────────────────────────────
    //  Navigation
    // ────────────────────────────────────────

    private static void AssignNavFollower(Enemy enemy, NavigationFollower follower)
    {
        switch (enemy)
        {
            case Tank t:    t.NavFollower = follower; break;
            case Walker w:  w.NavFollower = follower; break;
            case Runner r:  r.NavFollower = follower; break;
            case Shooter s: s.NavFollower = follower; break;
        }
    }

    // ────────────────────────────────────────
    //  Cleanup
    // ────────────────────────────────────────

    private void CleanupDead()
    {
        _alive.RemoveAll(e => e.IsDead);
        _tanks.RemoveAll(t => t.IsDead);
        _walkers.RemoveAll(w => w.IsDead);
        _runners.RemoveAll(r => r.IsDead);
        _shooters.RemoveAll(s => s.IsDead);
    }
}
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace LILITH.Core.Tools;

/// <summary>
/// Spawns enemies continuously at a fixed interval, picking types
/// by weighted random selection. All timing values are mutable at
/// runtime so difficulty can ramp mid-game.
///
/// Usage:
///   var spawner = new ContinuousSpawner(zone);
///   spawner.RegisterFactory(EnemyType.Walker, pos => new Walker(s, pos));
///   spawner.AddWeight(EnemyType.Walker, 5f);
///   spawner.AddWeight(EnemyType.Runner, 3f);
///   ...
///   spawner.Update(gameTime, playerPos, viewSize, playerIsDead);
///
///   // Ramp difficulty later:
///   spawner.Interval = 0.8f;
///   spawner.BatchSize = 3;
///   spawner.SetWeights(new EnemyWeight(EnemyType.Runner, 5f),
///                      new EnemyWeight(EnemyType.Shooter, 4f));
/// </summary>
public class ContinuousSpawner : Spawner
{
    private readonly List<EnemyWeight> _weights = new();
    private float _totalWeight;
    private float _timer;

    // ── Tuning (all mutable at runtime) ──

    /// <summary>Seconds between spawn ticks.</summary>
    public float Interval { get; set; } = 2f;

    /// <summary>How many enemies spawn per tick.</summary>
    public int BatchSize { get; set; } = 1;

    /// <summary>Hard cap on total living enemies.</summary>
    public int MaxAlive { get; set; } = 20;

    /// <summary>
    /// When true the timer keeps counting at the cap,
    /// so a burst appears the moment room opens up.
    /// </summary>
    public bool TickWhileFull { get; set; } = false;

    public ContinuousSpawner(SpawnZoneConfig zone) : base(zone)
    {
        _timer = Interval;
    }

    public ContinuousSpawner() : base(new SpawnZoneConfig())
    {
        _timer = Interval;
    }

    // ── Weights ──

    /// <summary>
    /// Add a weighted entry. Weights are relative.
    /// (Walker 5, Runner 3) → ~62% walkers, ~38% runners.
    /// </summary>
    public void AddWeight(EnemyType type, float weight)
    {
        _weights.Add(new EnemyWeight(type, weight));
        RecalcTotal();
    }

    /// <summary>
    /// Replace the entire weight table at once.
    /// </summary>
    public void SetWeights(params EnemyWeight[] weights)
    {
        _weights.Clear();
        _weights.AddRange(weights);
        RecalcTotal();
    }

    public void RemoveWeight(EnemyType type)
    {
        _weights.RemoveAll(w => w.Type == type);
        RecalcTotal();
    }

    protected override void OnReset()
    {
        _timer = Interval;
    }

    // ── Per-frame ──

    public void Update(GameTime gameTime, Vector2 playerPosition,
                       Vector2 viewSize, bool playerIsDead)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        UpdateEnemies(gameTime, playerPosition, playerIsDead);

        if (_weights.Count == 0 || _totalWeight <= 0f)
            return;

        bool atCap = AliveCount >= MaxAlive;
        if (atCap && !TickWhileFull)
            return;

        _timer -= dt;
        if (_timer > 0f)
            return;

        _timer += Interval;

        if (!atCap)
        {
            int toSpawn = Math.Min(BatchSize, MaxAlive - AliveCount);
            for (int i = 0; i < toSpawn; i++)
                SpawnEnemy(PickRandomType(), playerPosition, viewSize);
        }
    }

    // ── Internals ──

    private EnemyType PickRandomType()
    {
        float roll = Random.Shared.NextSingle() * _totalWeight;
        float cumulative = 0f;

        for (int i = 0; i < _weights.Count; i++)
        {
            cumulative += _weights[i].Weight;
            if (roll <= cumulative)
                return _weights[i].Type;
        }

        return _weights[^1].Type;
    }

    private void RecalcTotal()
    {
        _totalWeight = 0f;
        foreach (var w in _weights)
            _totalWeight += w.Weight;
    }
}
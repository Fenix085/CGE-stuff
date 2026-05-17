using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace LILITH.Core.Tools;

/// <summary>
/// Spawns enemies in predefined waves. Each wave is a sequence of
/// entries (type + count + delay), followed by a pause before the next.
/// After the last wave the spawner stops.
///
/// Usage:
///   var spawner = new WaveSpawner(zone);
///   spawner.RegisterFactory(EnemyType.Walker, pos => new Walker(s, pos));
///   spawner.AddWave(new Wave { ... });
///   spawner.Start();
///   ...
///   spawner.Update(gameTime, playerPos, viewSize, playerIsDead);
/// </summary>
public class WaveSpawner : Spawner
{
    private readonly List<Wave> _waves = new();
    private int _waveIndex;
    private int _entryIndex;
    private int _spawnedInEntry;
    private float _timer;
    private bool _finished;

    public int CurrentWave => _waveIndex;
    public bool IsFinished => _finished;
    public int WaveCount => _waves.Count;

    public WaveSpawner(SpawnZoneConfig zone) : base(zone) { }

    public WaveSpawner() : base(new SpawnZoneConfig()) { }

    // ── Setup ──

    public void AddWave(Wave wave)
        => _waves.Add(wave);

    public void ClearWaves()
        => _waves.Clear();

    /// <summary>
    /// Call after all waves and factories are registered.
    /// </summary>
    public void Start()
    {
        _waveIndex = 0;
        _entryIndex = 0;
        _spawnedInEntry = 0;
        _timer = 0f;
        _finished = _waves.Count == 0;
    }

    protected override void OnReset()
    {
        Start();
    }

    // ── Per-frame ──

    public void Update(GameTime gameTime, Vector2 playerPosition,
                       Vector2 viewSize, bool playerIsDead)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        UpdateEnemies(gameTime, playerPosition, playerIsDead);

        if (_finished)
            return;

        _timer -= dt;
        if (_timer > 0f)
            return;

        if (_waveIndex >= _waves.Count)
        {
            _finished = true;
            return;
        }

        Wave wave = _waves[_waveIndex];

        if (_entryIndex >= wave.Entries.Count)
        {
            _waveIndex++;
            _entryIndex = 0;
            _spawnedInEntry = 0;
            _timer = wave.DelayAfterWave;
            return;
        }

        SpawnEntry entry = wave.Entries[_entryIndex];

        if (_spawnedInEntry < entry.Count)
        {
            SpawnEnemy(entry.Type, playerPosition, viewSize);
            _spawnedInEntry++;
            _timer = entry.DelayBetween;
        }

        if (_spawnedInEntry >= entry.Count)
        {
            _entryIndex++;
            _spawnedInEntry = 0;
        }
    }
}
/// <summary>
/// A full wave: a list of spawn entries plus time before next wave.
/// </summary>
public class Wave
{
    public List<SpawnEntry> Entries { get; set; } = new();
    public float DelayAfterWave { get; set; } = 5f;
}
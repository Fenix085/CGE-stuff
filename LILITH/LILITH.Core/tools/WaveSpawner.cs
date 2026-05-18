using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace LILITH.Core.Tools;

public class WaveSpawner : Spawner
{
    private readonly List<Wave> _waves = new();
    private int   _waveIndex;
    private int   _entryIndex;
    private int   _spawnedInEntry;
    private float _timer;
    private bool  _finished;
    private bool  _waitingForClear; // ждём пока все враги умрут

    public int  CurrentWave => _waveIndex + 1;
    public bool IsFinished  => _finished;
    public int  WaveCount   => _waves.Count;

    // Событие — сообщает GameScene что пришла волна с боссом
    public System.Action? OnBossWave;

    public WaveSpawner(SpawnZoneConfig zone) : base(zone) { }
    public WaveSpawner() : base(new SpawnZoneConfig()) { }

    public void AddWave(Wave wave) => _waves.Add(wave);
    public void ClearWaves()      => _waves.Clear();

    public void Start()
    {
        _waveIndex      = 0;
        _entryIndex     = 0;
        _spawnedInEntry = 0;
        _timer          = 3f; // небольшая пауза перед первой волной
        _finished       = _waves.Count == 0;
        _waitingForClear = false;
    }

    protected override void OnReset() => Start();

    public void Update(GameTime gameTime, Vector2 playerPosition,
                       Vector2 viewSize, bool playerIsDead)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        UpdateEnemies(gameTime, playerPosition, playerIsDead);

        if (_finished) return;

        // Ждём пока все враги умрут перед следующей волной
        if (_waitingForClear)
        {
            if (AliveCount > 0) return;
            _waitingForClear = false;
            _timer = _waves[_waveIndex - 1].DelayAfterWave;
            return;
        }

        _timer -= dt;
        if (_timer > 0f) return;

        if (_waveIndex >= _waves.Count)
        {
            _finished = true;
            return;
        }

        Wave wave = _waves[_waveIndex];

        // Все entry текущей волны отправлены — ждём очистки
        if (_entryIndex >= wave.Entries.Count)
        {
            _waveIndex++;
            _entryIndex      = 0;
            _spawnedInEntry  = 0;

            if (wave.WaitForClear)
            {
                _waitingForClear = true;
            }
            else
            {
                _timer = wave.DelayAfterWave;
            }

            // Если это волна с боссом — уведомляем
            if (_waveIndex < _waves.Count && _waves[_waveIndex].IsBossWave)
                OnBossWave?.Invoke();

            return;
        }

        SpawnEntry entry = wave.Entries[_entryIndex];

        // Спавним одного врага и ставим таймер
        if (_spawnedInEntry < entry.Count)
        {
            SpawnEnemy(entry.Type, playerPosition, viewSize);
            _spawnedInEntry++;
            _timer = entry.DelayBetween;

            // Если спавн последнего в entry — переходим к следующему
            if (_spawnedInEntry >= entry.Count)
            {
                _entryIndex++;
                _spawnedInEntry = 0;
            }
        }
    }
}

public class Wave
{
    public List<SpawnEntry> Entries      { get; set; } = new();
    public float            DelayAfterWave { get; set; } = 5f;

    /// <summary>Если true — следующая волна начнётся только после гибели всех врагов.</summary>
    public bool WaitForClear { get; set; } = false;

    /// <summary>Если true — эта волна помечена как волна босса.</summary>
    public bool IsBossWave   { get; set; } = false;
}
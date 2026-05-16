using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace LILITH.Items;

/// <summary>
/// Спавнит кластеры орбов опыта за пределами зоны видимости камеры.
/// </summary>
public class ExperienceSpawner
{
    private readonly List<ExperienceOrb> _orbs = new();
    private readonly Random              _rng  = new();

    private const int   MAX_ORBS         = 120;
    private const int   CLUSTER_SIZE_MIN = 3;
    private const int   CLUSTER_SIZE_MAX = 8;
    private const float SPAWN_INTERVAL   = 2.5f;
    private const float SPAWN_MARGIN     = 80f;
    private const float CLUSTER_SPREAD   = 40f;
    private const int   ORB_VALUE        = 10;

    private float _spawnTimer;

    public void SpawnInitial(Vector2 cameraPos, Viewport viewport)
    {
        for (int i = 0; i < 5; i++)
            SpawnCluster(GetRandomOffscreenPosition(cameraPos, viewport));
    }

    public void Update(GameTime gameTime, Vector2 cameraPos, Viewport viewport)
    {
        foreach (var orb in _orbs)
            orb.Update(gameTime);

        _orbs.RemoveAll(o => o.IsCollected);

        _spawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_spawnTimer >= SPAWN_INTERVAL && _orbs.Count < MAX_ORBS)
        {
            _spawnTimer = 0f;
            SpawnCluster(GetRandomOffscreenPosition(cameraPos, viewport));
        }
    }

    /// <summary>Проверяет подбор орбов и возвращает суммарный опыт.</summary>
    public int CollectOrbs(Vector2 playerCenter)
    {
        int total = 0;
        foreach (var orb in _orbs)
            if (orb.TryCollect(playerCenter))
                total += orb.Value;
        return total;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (var orb in _orbs)
            orb.Draw(spriteBatch, pixel);
    }

    private void SpawnCluster(Vector2 center)
    {
        int count = _rng.Next(CLUSTER_SIZE_MIN, CLUSTER_SIZE_MAX + 1);
        for (int i = 0; i < count; i++)
        {
            var offset = new Vector2(
                (float)(_rng.NextDouble() * 2 - 1) * CLUSTER_SPREAD,
                (float)(_rng.NextDouble() * 2 - 1) * CLUSTER_SPREAD);
            _orbs.Add(new ExperienceOrb(center + offset, ORB_VALUE));
        }
    }

    private Vector2 GetRandomOffscreenPosition(Vector2 cameraPos, Viewport viewport)
    {
        float halfW = viewport.Width  * 0.5f;
        float halfH = viewport.Height * 0.5f;
        int   side  = _rng.Next(4);

        return side switch
        {
            0 => new Vector2(
                    cameraPos.X + (float)(_rng.NextDouble() * 2 - 1) * halfW,
                    cameraPos.Y - halfH - SPAWN_MARGIN - (float)_rng.NextDouble() * 100),
            1 => new Vector2(
                    cameraPos.X + (float)(_rng.NextDouble() * 2 - 1) * halfW,
                    cameraPos.Y + halfH + SPAWN_MARGIN + (float)_rng.NextDouble() * 100),
            2 => new Vector2(
                    cameraPos.X - halfW - SPAWN_MARGIN - (float)_rng.NextDouble() * 100,
                    cameraPos.Y + (float)(_rng.NextDouble() * 2 - 1) * halfH),
            _ => new Vector2(
                    cameraPos.X + halfW + SPAWN_MARGIN + (float)_rng.NextDouble() * 100,
                    cameraPos.Y + (float)(_rng.NextDouble() * 2 - 1) * halfH),
        };
    }
}

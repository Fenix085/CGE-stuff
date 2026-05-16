using System.Collections.Generic;
using MainEngine;
using MainEngine.Entities;
using MainEngine.FlockEnemy;
using MainEngine.Graphics;
using MainEngine.Projectile;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LILITH.Core.Enemies.Boss;

public class Boss : Enemy
{
    private readonly BossFSM _fsm;

    public Boss(AnimatedSprite sprite, Vector2 position, int hp)
        : base(sprite, position, hp)
    {
        DetectionRadius = 400f;
        FollowRadius = 300f;
        _fsm = new BossFSM(this);
    }

    /// <summary>
    /// Assign before the first Update so the wall attack has a texture to use.
    /// </summary>
    public TextureRegion WallProjectileRegion
    {
        set => _fsm.Context.WallProjectileRegion = value;
    }

    /// <summary>
    /// Growing warning circle during the charge phase, or null.
    /// The scene can draw this as a red tinted circle.
    /// </summary>
    public Circle? WarningZone => _fsm.ActiveWarningZone;

    /// <summary>
    /// Damage the boss dealt to the player this frame.
    /// Read after Update, then it resets next frame.
    /// </summary>
    public int PendingPlayerDamage => _fsm.Context.PendingPlayerDamage;

    /// <summary>
    /// Projectiles spawned by the boss this frame.
    /// The scene should move these into its own list after Update.
    /// </summary>
    public List<Projectile> SpawnedProjectiles => _fsm.Context.BossProjectiles;

    /// <summary>
    /// Force sources the boss produced this frame (e.g. explosion shockwave).
    /// </summary>
    public List<ForceSource> ActiveForceSources => _fsm.Context.ActiveForceSources;

    public void Update(GameTime gameTime, Vector2 playerPosition, Circle playerBounds, bool playerIsDead)
    {
        Sprite.Update(gameTime);
        _fsm.Update(playerPosition, playerBounds, playerIsDead, gameTime);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (!IsDead)
            base.Draw(gameTime, spriteBatch);
    }
}
using System.Collections.Generic;
using MainEngine;
using MainEngine.Entities;
using MainEngine.FlockEnemy;
using MainEngine.Graphics;
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
    /// Warning circles active this frame (telegraphs + flashes).
    /// The scene draws these as colored rings/fills.
    /// </summary>
    public IReadOnlyList<Circle> WarningZones => _fsm.Context.WarningZones;

    /// <summary>
    /// Damage the boss dealt to the player this frame.
    /// </summary>
    public int PendingPlayerDamage => _fsm.Context.PendingPlayerDamage;

    /// <summary>
    /// Force sources produced this frame (explosion shockwaves).
    /// </summary>
    public List<ForceSource> ActiveForceSources => _fsm.Context.ActiveForceSources;

    public void Update(GameTime gameTime, Vector2 playerPosition, Circle playerBounds, bool playerIsDead)
    {
        base.Update(gameTime);
        Sprite.Update(gameTime);
        _fsm.Update(playerPosition, playerBounds, playerIsDead, gameTime);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (!IsDead)
            base.Draw(gameTime, spriteBatch);
    }
}
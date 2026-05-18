using System.Collections.Generic;
using LILITH.Abilities;
using MainEngine;
using MainEngine.Entities;
using MainEngine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LILITH.Core;

public class PlayerController
{
    public Player Player { get; }

    private readonly List<IAbility> _abilities = new();
    private readonly Texture2D      _pixel;

    // ── Animations ──────────────────────────────────────────────────────────

    private readonly AnimatedSprite? _idleAnim;
    private readonly AnimatedSprite? _walkAnim;
    private readonly AnimatedSprite? _deathAnim;

    private Vector2 _prevPosition;

    // ── Constructor ──────────────────────────────────────────────────────

    // Playable character with a simple square sprite. For testing and prototyping.
    public PlayerController(Player player, Texture2D pixel)
    {
        Player        = player;
        _pixel        = pixel;
        _prevPosition = player.Position;
    }

    // Playable character with actual animations. For final game.
    public PlayerController(Player player, Texture2D pixel,
                            AnimatedSprite idle, AnimatedSprite walk, AnimatedSprite death)
    {
        Player        = player;
        _pixel        = pixel;
        _idleAnim     = idle;
        _walkAnim     = walk;
        _deathAnim    = death;
        _prevPosition = player.Position;

        if (_deathAnim != null)
        _deathAnim.PlayOnce = true;
    }

    // ── Abilities ─────────────────────────────────────────────────────────

    public void AddAbility(IAbility ability) => _abilities.Add(ability);
    public IReadOnlyList<IAbility> GetAllAbilities() => _abilities;

    public T? GetAbility<T>() where T : class, IAbility
    {
        foreach (var a in _abilities)
            if (a is T found) return found;
        return null;
    }

    // ── Update ────────────────────────────────────────────────────────────

    public void Update(GameTime gameTime, Vector2 moveDirection, Vector2 cursorWorld)
    {
        Player.Update(gameTime);
        UpdateAnimation(gameTime);

        foreach (var ability in _abilities)
        {
            Vector2 dir = ability is AutoShootAbility
                ? moveDirection
                : ability is SlashAbility || ability is TrailAbility
                    ? Player.LastMoveDirection
                    : cursorWorld;

            ability.Update(gameTime, Player.Center, dir);
        }

        _prevPosition = Player.Position;
    }

    private void UpdateAnimation(GameTime gameTime)
    {
        if (_idleAnim == null) return;

        bool isMoving = Vector2.DistanceSquared(Player.Position, _prevPosition) > 0.01f;

        AnimatedSprite target;

        if (Player.Health.IsDead && _deathAnim != null)
            target = _deathAnim;
        else if (isMoving && _walkAnim != null)
            target = _walkAnim;
        else
            target = _idleAnim;

        if (Player.Sprite != target)
            Player.Sprite = target;

        Player.Sprite.Update(gameTime);
    }

    public void UpdateDeathAnimation(GameTime gameTime)
    {
        if (_deathAnim != null && !_deathAnim.IsFinished)
            _deathAnim.Update(gameTime);
    }

    // ── Draw ──────────────────────────────────────────────────────────────

    public void Draw(GameTime gameTime, SpriteBatch sb)
    {
        Player.Draw(gameTime, sb);

        foreach (var ability in _abilities)
            ability.Draw(gameTime, sb, _pixel);
    }
}
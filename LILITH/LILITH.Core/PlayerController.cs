using System.Collections.Generic;
using LILITH.Abilities;
using MainEngine;
using MainEngine.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LILITH.Core;

public class PlayerController
{
    public Player Player { get; }

    private readonly List<IAbility> _abilities = new();
    private readonly Texture2D      _pixel;

    public PlayerController(Player player, Texture2D pixel)
    {
        Player = player;
        _pixel = pixel;
    }

    // ── Способности ───────────────────────────────────────────────────────

    public void AddAbility(IAbility ability) => _abilities.Add(ability);
    public IReadOnlyList<IAbility> GetAllAbilities() => _abilities;
    // Найти существующую способность того же типа — для Upgrade
    public T? GetAbility<T>() where T : class, IAbility
    {
        foreach (var a in _abilities)
            if (a is T found) return found;
        return null;
    }

    // ── Update / Draw ─────────────────────────────────────────────────────

    public void Update(GameTime gameTime, Vector2 moveDirection, Vector2 cursorWorld)
{
    Player.Update(gameTime);

    foreach (var ability in _abilities)
    {
        if (ability is SatelliteAbility || ability is AuraAbility)
            ability.Update(gameTime, Player.Center, cursorWorld);
        else
            ability.Update(gameTime, Player.Center, moveDirection);
    }
}

    public void Draw(GameTime gameTime, SpriteBatch sb)
    {
        Player.Draw(gameTime, sb);

        foreach (var ability in _abilities)
            ability.Draw(gameTime, sb, _pixel);
    }
}
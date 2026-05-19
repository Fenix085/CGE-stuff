using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MainEngine;

namespace LILITH.Abilities;

public interface IAbility
{
    string Name { get; }
    string Description { get; }
    int Damage { get; }

    Texture2D Icon { get; }

    void Update(GameTime gameTime, Vector2 playerCenter, Vector2 aimDirection);
    void Draw(GameTime gameTime, SpriteBatch spriteBatch, Texture2D pixel);

    void Upgrade();
    void NotifyHit(Circle hitCircle);

    IReadOnlyList<Circle> GetHitCircles();
}
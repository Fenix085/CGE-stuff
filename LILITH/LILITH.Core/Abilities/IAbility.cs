using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LILITH.Abilities;

public interface IAbility
{
    string Name        { get; }
    string Description { get; }

    void Update(GameTime gameTime, Vector2 playerCenter, Vector2 cursorWorld);
    void Draw(GameTime gameTime, SpriteBatch spriteBatch, Texture2D pixel);
    void Upgrade();
}
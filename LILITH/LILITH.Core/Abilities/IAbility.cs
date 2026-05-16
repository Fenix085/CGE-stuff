using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LILITH.Abilities;

public interface IAbility
{
    void Update(GameTime gameTime, Vector2 playerCenter, Vector2 cursorWorld);
    void Draw(GameTime gameTime, SpriteBatch spriteBatch, Texture2D pixel);
    void Upgrade(); // вызывается при повторном выборе этой способности
}
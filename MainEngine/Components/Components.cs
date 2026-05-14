using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MainEngine.Components;

public abstract class Components
{
    public abstract void Draw(GameTime gameTime, SpriteBatch spriteBatch);
    public abstract void Update(GameTime gameTime);   
}


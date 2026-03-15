using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MainEngine.Input;

namespace MainEngine;

public class HQ
{
    public SpriteBatch SpriteBatch { get; }
    public InputManager Input { get; }

    public HQ(GraphicsDevice graphicsDevice)
    {
        SpriteBatch = new SpriteBatch(graphicsDevice);
        Input = new InputManager();
    }

    public void Update(GameTime gameTime)
    {
        Input.Update(gameTime);
    }
}

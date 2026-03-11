using Microsoft.Xna.Framework;
using MainEngine.Graphics;

namespace MainEngine.Core;

public class Camera
{
    public Matrix Transform { get; private set; }

    public void Follow(Sprite target)
    {
        var viewport = MainEngine.Core.Graphics.GraphicsDevice.Viewport;

        var offset = Matrix.CreateTranslation(
                viewport.Width / 2,
                viewport.Height / 2,
                0);

        var position = Matrix.CreateTranslation(
            -target.Position.X - (target.Rectangle.Width / 2),
            -target.Position.Y - (target.Rectangle.Height / 2),
            0);
        Transform = position * offset;
    }
}
using Microsoft.Xna.Framework;

namespace MainEngine.Graphics;

public sealed class AtlasSprite : Sprite
{
    public AtlasSprite(TextureRegion region) : base(region)
    {
    }

    public override void ApplyDeath()
    {
    }
}

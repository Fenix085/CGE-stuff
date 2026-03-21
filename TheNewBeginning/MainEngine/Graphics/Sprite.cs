using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace MainEngine.Graphics;

public abstract class Sprite : Components.Components
{
    protected Texture2D _texture;
    public Vector2 Position { get; set; }
    public Rectangle Rectangle
    {
        get{ return new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height); }
    }

    public abstract void ApplyDeath();

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Region.Draw(spriteBatch, Position, Color, Rotation, Origin, Scale, Effects, LayerDepth);
    }
    public Sprite(Texture2D texture)
    {
        _texture = texture;
    }
    public override void Update(GameTime gameTime)
    {
        // No update logic for a basic sprite.
    }

    public TextureRegion Region { get; set; }
    
    public Color Color { get; set; } = Color.White;
    
    public float Rotation { get; set; } = 0.0f;

    public Vector2 Scale { get; set; } = Vector2.One;

    public Vector2 Origin { get; set; } = Vector2.Zero;

    public SpriteEffects Effects { get; set; } = SpriteEffects.None;
    
    public float LayerDepth { get; set; } = 0.0f;
    
    public float Width => Region.Width * Scale.X;
    
    public float Height => Region.Height * Scale.Y;
    
    public Sprite() { }
    
    public Sprite(TextureRegion region)
    {
        Region = region;
    }
    
    public void CenterOrigin()
    {
        Origin = new Vector2(Region.Width, Region.Height) * 0.5f;
    }
}

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace LILITH.Abilities;

public static class AbilityAssets
{
    public static Texture2D SatelliteIcon { get; private set; } = null!;
    public static Texture2D AuraIcon      { get; private set; } = null!;
    public static Texture2D SlashIcon     { get; private set; } = null!;
    public static Texture2D TrailIcon     { get; private set; } = null!;
    public static Texture2D AutoShootIcon { get; private set; } = null!;

    public static void Load(ContentManager content)
    {
        SatelliteIcon = content.Load<Texture2D>("satellite");
        AuraIcon      = content.Load<Texture2D>("aura");
        SlashIcon     = content.Load<Texture2D>("slash");
        TrailIcon     = content.Load<Texture2D>("trail");
        AutoShootIcon = content.Load<Texture2D>("autoshot");
    }
}
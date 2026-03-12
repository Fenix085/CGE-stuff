using System;
using Microsoft.Xna.Framework;

namespace MainEngine.Global;

public class Globals
{
    public static float ElapsedTime = 0f;
    public static float FixedUpdateMultiplier = 0f;
    public static GameTime GameTime;
    public static float NextTickTime = 0;
    public static float FixedUpdateAlpha;
    internal static TimeSpan FixedUpdateRate;
}
using LILITH.Core.Scenes;
using MainEngine;

namespace LILITH.Core;

/// <summary>
/// Точка входа. Наследуется от HQ, стартует с главного меню.
/// </summary>
public class Game1 : HQ
{
    public Game1()
        : base("LILITH", 1280, 720, false)
    {
    }

    protected override void Initialize()
    {
        base.Initialize(); // HQ инициализирует Input, Audio, GraphicsDevice

        HQ.ChangeScene(new MainMenuScene());
    }
}

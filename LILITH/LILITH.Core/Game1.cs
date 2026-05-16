using LILITH.Core.Scenes;
using MainEngine;
using Microsoft.Xna.Framework;

namespace LILITH.Core;

/// <summary>
/// Точка входа. Наследуется от HQ чтобы Input и Audio работали.
/// Только создаёт GameScene и передаёт управление ей через HQ.ChangeScene().
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

        // Запускаем игровую сцену
        HQ.ChangeScene(new GameScene());
    }
}

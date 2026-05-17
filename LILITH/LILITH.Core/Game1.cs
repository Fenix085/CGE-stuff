using LILITH.Core.Scenes;
using MainEngine;

namespace LILITH.Core;


public class Game1 : HQ
{
    public Game1()
        : base("LILITH", 1280, 720, false)
    {
    }

    protected override void Initialize()
    {
        base.Initialize(); 

        HQ.ChangeScene(new MainMenuScene());
    }
}

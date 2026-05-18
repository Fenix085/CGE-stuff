using LILITH.Core.Scenes;
using MainEngine;
using System.Drawing;

namespace LILITH.Core;


public class Game1 : HQ
{
    public Game1()
        : base("Code name: LILITH", 1280, 720, false)
    {
    }

    protected override void Initialize()
    {
        base.Initialize(); 
        
        HQ.ChangeScene(new MainMenuScene());
    }
}

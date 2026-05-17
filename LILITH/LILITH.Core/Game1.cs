using MainEngine;

namespace LILITH.Core;

public class Game1 : HQ
{
    public Game1() : base("Codename: LILITH", 1280, 720, false) {}

    protected override void Initialize()
    {
        base.Initialize();

        ChangeScene(new Scenes.GameScene());
    }
    
}

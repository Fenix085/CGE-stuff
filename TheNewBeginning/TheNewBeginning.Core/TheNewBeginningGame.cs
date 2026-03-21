using MainEngine.Scenes;
using Microsoft.Xna.Framework.Media;
using MainEngine;

namespace TheNewBeginning;

public class TheNewBeginningGame : HQ
{
     protected override void Initialize()
    {
        base.Initialize();

        // Start the game with the title scene.
        ChangeScene(new TitleScene());
    }

    protected override void LoadContent()
    {
        
    }
}

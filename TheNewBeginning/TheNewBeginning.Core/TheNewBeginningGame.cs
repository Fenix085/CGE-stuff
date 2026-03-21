using MainEngine.Scenes;
using Microsoft.Xna.Framework.Media;
using MainEngine;
using TheNewBeginning.Scenes;

namespace TheNewBeginning.Core;

public class TheNewBeginningGame : HQ
{
    public TheNewBeginningGame() : base("The New Beginning", 1280, 720, false) { }

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

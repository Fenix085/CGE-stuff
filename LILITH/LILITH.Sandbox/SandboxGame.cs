using MainEngine;
using MainEngine.Scenes;
using Microsoft.Xna.Framework;

namespace LILITH.Sandbox;

public sealed class SandboxGame : HQ
{
    public SandboxGame() : base("Sandbox", 1280, 720, false){ }

    protected override void Initialize()
    {
        base.Initialize();
        ChangeScene(new SandboxScene());
    }
}

public sealed class SandboxScene : Scene
{
    public override void LoadContent()
    {
        
    }

    public override void Update(GameTime gameTime)
    {
        
    }

    public override void Draw(GameTime gameTime)
    {
        
    }
}
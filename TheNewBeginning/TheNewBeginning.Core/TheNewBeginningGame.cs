using Microsoft.Xna.Framework.Media;
using MainEngine;
using TheNewBeginning.Scenes;
using Gum.Forms;
using Gum.Forms.Controls;
using MonoGameGum;

namespace TheNewBeginning.Core;

public class TheNewBeginningGame : HQ
{
    public TheNewBeginningGame() : base("The New Beginning", 1280, 720, false) { }

    protected override void Initialize()
{
   base.Initialize();

   InitializeGum();

   ChangeScene(new TitleScene());
}
    protected override void LoadContent()
    {

    }
    
    private void InitializeGum()
{
    GumService.Default.Initialize(this, DefaultVisualsVersion.V3);

    GumService.Default.ContentLoader.XnaContentManager = HQ.Content;

    FrameworkElement.KeyboardsForUiControl.Add(GumService.Default.Keyboard);

    FrameworkElement.GamePadsForUiControl.AddRange(GumService.Default.Gamepads);

    FrameworkElement.TabReverseKeyCombos.Add(
       new KeyCombo() { PushedKey = Microsoft.Xna.Framework.Input.Keys.Up });


    FrameworkElement.TabKeyCombos.Add(
       new KeyCombo() { PushedKey = Microsoft.Xna.Framework.Input.Keys.Down });

    GumService.Default.CanvasWidth = GraphicsDevice.PresentationParameters.BackBufferWidth / 4.0f;
    GumService.Default.CanvasHeight = GraphicsDevice.PresentationParameters.BackBufferHeight / 4.0f;
    GumService.Default.Renderer.Camera.Zoom = 4.0f;
}

}
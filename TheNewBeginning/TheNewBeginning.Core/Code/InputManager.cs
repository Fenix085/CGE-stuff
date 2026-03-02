using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TheNewBeginning;

// This class will handle all input-related functionality, such as processing keyboard, mouse, and gamepad inputs.
    // It will also manage input states and provide an interface for other parts of the game to query input status.
public class InputManager
{
    private static MouseState _lastMouseState;
    private static Vector2 _direction;
    public static Vector2 Direction => _direction;
    public static Vector2 MousePosition => Mouse.GetState().Position.ToVector2();
    public static bool MouseClicked {get; private set;}

    public static void Update()
    {
        var keyboardState = Keyboard.GetState();

        _direction = Vector2.Zero;
        if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up))_direction.Y -= 1;
        if (keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down))_direction.Y += 1;
        if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))_direction.X -= 1;
        if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))_direction.X += 1;

        MouseClicked = (Mouse.GetState().LeftButton == ButtonState.Pressed) && (_lastMouseState.LeftButton == ButtonState.Released);
        _lastMouseState = Mouse.GetState();
    }
    
}
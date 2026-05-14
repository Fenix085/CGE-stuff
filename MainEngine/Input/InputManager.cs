using Microsoft.Xna.Framework;

namespace MainEngine.Input;

public class InputManager
{
public KeyboardInfo Keyboard { get; private set; }

public MouseInfo Mouse { get; private set; }

public GamePadInfo[] GamePads { get; private set; }

public InputManager()
{
    Keyboard = new KeyboardInfo();
    Mouse = new MouseInfo();

    GamePads = new GamePadInfo[4];
    for (int i = 0; i < 4; i++)
    {
        GamePads[i] = new GamePadInfo((PlayerIndex)i);
    }
}
public void Update(GameTime gameTime)
{
    Keyboard.Update();
    Mouse.Update();

    for (int i = 0; i < 4; i++)
    {
        GamePads[i].Update(gameTime);
    }
}

}
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheNewBeginning;

public class GameManager
{
    private readonly Player _player;
    public GameManager()
    {
        // Initialize game state, load resources, etc.
        _player = new(Globals.Content.Load<Texture2D>("player"), new Vector2(200, 200));
    }
    public void Update(GameTime gameTime)
    {
        // Update game logic, handle input, etc.
        InputManager.Update();
        _player.Update();
    }
    public void Draw(GameTime gameTime)
    {
        // Draw game elements to the screen.
        _player.Draw();
    }
}
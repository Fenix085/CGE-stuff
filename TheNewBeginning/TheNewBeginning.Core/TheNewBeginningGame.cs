using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MainEngine;
using MainEngine.Graphics;
using MainEngine.Input;

namespace TheNewBeginning.Core;

public class TheNewBeginningGame : MainEngine.Core
{
    // Defines the slime animated sprite.
    private AnimatedSprite _player;

    // Defines the bat animated sprite.
    private AnimatedSprite _enemy;

    // Tracks the position of the player.
    private Vector2 _playerPosition;

    // Speed multiplier when moving.
    private const float MOVEMENT_SPEED = 5.0f;

    public TheNewBeginningGame() : base("The New Beginning", 1280, 720, false)
    {

    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

        base.Initialize();
    }

    protected override void LoadContent()
    {
        // Create the texture atlas from the XML configuration file.
        TextureAtlas atlas = TextureAtlas.FromFile(Content, "images/atlas-definition.xml");

        // Create the player animated sprite from the atlas.
        _player = atlas.CreateAnimatedSprite("player-animation");
        _player.Scale = new Vector2(4.0f, 4.0f);

        // Create the enemy animated sprite from the atlas.
        //_enemy = atlas.CreateAnimatedSprite("enemy-animation");
        //_enemy.Scale = new Vector2(4.0f, 4.0f);
    }

    protected override void Update(GameTime gameTime)
    {
        // Update the player animated sprite.
        _player.Update(gameTime);

        // Update the enemy animated sprite.
        //_enemy.Update(gameTime);

        // Check for keyboard input and handle it.
        CheckKeyboardInput();

        // Check for gamepad input and handle it.
        CheckGamePadInput();

        base.Update(gameTime);
    }

    private void CheckKeyboardInput()
    {
        // If the space key is held down, the movement speed increases by 1.5
        float speed = MOVEMENT_SPEED;
        if (Input.Keyboard.IsKeyDown(Keys.Space))
        {
            speed *= 1.5f;
        }

        // If the W or Up keys are down, move the player up on the screen.
        if (Input.Keyboard.IsKeyDown(Keys.W) || Input.Keyboard.IsKeyDown(Keys.Up))
        {
            _playerPosition.Y -= speed;
        }

        // if the S or Down keys are down, move the player down on the screen.
        if (Input.Keyboard.IsKeyDown(Keys.S) || Input.Keyboard.IsKeyDown(Keys.Down))
        {
            _playerPosition.Y += speed;
        }

        // If the A or Left keys are down, move the player left on the screen.
        if (Input.Keyboard.IsKeyDown(Keys.A) || Input.Keyboard.IsKeyDown(Keys.Left))
        {
            _playerPosition.X -= speed;
        }

        // If the D or Right keys are down, move the player right on the screen.
        if (Input.Keyboard.IsKeyDown(Keys.D) || Input.Keyboard.IsKeyDown(Keys.Right))
        {
            _playerPosition.X += speed;
        }
    }


    private void CheckGamePadInput()
    {
        GamePadInfo gamePadOne = Input.GamePads[(int)PlayerIndex.One];

        // If the A button is held down, the movement speed increases by 1.5
        // and the gamepad vibrates as feedback to the player.
        float speed = MOVEMENT_SPEED;
        if (gamePadOne.IsButtonDown(Buttons.A))
        {
            speed *= 1.5f;
            gamePadOne.SetVibration(1.0f, TimeSpan.FromSeconds(1));
        }
        else
        {
            gamePadOne.StopVibration();
        }

        // Check thumbstick first since it has priority over which gamepad input
        // is movement.  It has priority since the thumbstick values provide a
        // more granular analog value that can be used for movement.
        if (gamePadOne.LeftThumbStick != Vector2.Zero)
        {
            _playerPosition.X += gamePadOne.LeftThumbStick.X * speed;
            _playerPosition.Y -= gamePadOne.LeftThumbStick.Y * speed;
        }
        else
        {
            // If DPadUp is down, move the player up on the screen.
            if (gamePadOne.IsButtonDown(Buttons.DPadUp))
            {
                _playerPosition.Y -= speed;
            }

            // If DPadDown is down, move the player down on the screen.
            if (gamePadOne.IsButtonDown(Buttons.DPadDown))
            {
                _playerPosition.Y += speed;
            }

            // If DPapLeft is down, move the player left on the screen.
            if (gamePadOne.IsButtonDown(Buttons.DPadLeft))
            {
                _playerPosition.X -= speed;
            }

            // If DPadRight is down, move the player right on the screen.
            if (gamePadOne.IsButtonDown(Buttons.DPadRight))
            {
                _playerPosition.X += speed;
            }
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        // Clear the back buffer.
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // Begin the sprite batch to prepare for rendering.
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // Draw the player sprite.
        _player.Draw(SpriteBatch, _playerPosition);

        // Draw the enemy sprite 10px to the right of the player.
        //_enemy.Draw(SpriteBatch, new Vector2(_player.Width + 10, 0));

        // Always end the sprite batch when finished.
        SpriteBatch.End();

        base.Draw(gameTime);
    }
}

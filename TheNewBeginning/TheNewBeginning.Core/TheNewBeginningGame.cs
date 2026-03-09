using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MainEngine;
using MainEngine.Graphics;
using MainEngine.Input;
using MainEngine.FlockEnemy;

namespace TheNewBeginning.Core;

public class TheNewBeginningGame : MainEngine.Core
{
    // Defines the slime animated sprite.
    private AnimatedSprite _player;

    // Defines the bat animated sprite.
    private AnimatedSprite _enemy;

    // Agent flock
    private List<Agent> _agents;
    private Sprite _agentSprite;
    private AgentConfig _agentConfig;

    // Tracks the position of the player.
    private Vector2 _playerPosition;

    private Vector2 _enemyPosition;
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
        _enemy = atlas.CreateAnimatedSprite("enemy-animation");
        _enemy.Scale = new Vector2(4.0f, 4.0f);

        _enemyPosition = new Vector2(_player.Width + 10, 0);

        // Set up the agent sprite using the first Orc frame.
        _agentSprite = atlas.CreateSprite("enemy-1");
        _agentSprite.Scale = new Vector2(3f, 3f);
        _agentSprite.CenterOrigin();

        // Configure flocking behaviour.
        _agentConfig = new AgentConfig
        {
            AgentSpeed = 65f,
            RepulsionRadius = 50f,
            AlignmentRadius = 125f,
            AttractionRadius = 250f,
            AttractionAngle = MathHelper.ToRadians(200f),
            RepulsionForce = 7f,
            AlignmentForce = 3f,
            AttractionForce = 1f,
            GravitationForce = 1f
        };

        // Spawn agents scattered across the screen.
        _agents = new List<Agent>();
        Vector2 center = new Vector2(640, 360);
        for (int i = 0; i < 30; i++)
        {
            Agent agent = new Agent(center);
            agent.Scatter(1280, 720);
            agent.Center = center;
            _agents.Add(agent);
        }
    }

    protected override void Update(GameTime gameTime)
    {
        // Update the player animated sprite.
        _player.Update(gameTime);

        // Update the enemy animated sprite.
        _enemy.Update(gameTime);

        // Process agent flocking logic and update positions.
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Agent.Process(_agents, _agentConfig);
        foreach (var agent in _agents)
            agent.Update(dt, 1280, 720);

        // Check for keyboard input and handle it.
        CheckKeyboardInput();

        // Check for gamepad input and handle it.
        CheckGamePadInput();

        // Creating a bounding circle for the player sprite to use for collision checks.
        Circle playerBounds = new Circle(
            (int)(_playerPosition.X + (_player.Width * 0.1f)),
            (int)(_playerPosition.Y + (_player.Height * 0.1f)),
            (int)(_player.Width * 0.1f)
        );
        
        // Creating a bounding circle for the enemy sprite to use for collision checks.
        Circle enemyBounds = new Circle(
            (int)(_enemyPosition.X + (_enemy.Width * 0.1f)),
            (int)(_enemyPosition.Y + (_enemy.Height * 0.1f)),
            (int)(_enemy.Width * 0.1f)
        );

        if (enemyBounds.Intersects(playerBounds))
        {
            // Divide the width  and height of the screen into equal columns and
            // rows based on the width and height of the bat.
            int totalColumns = GraphicsDevice.PresentationParameters.BackBufferWidth / (int)_player.Width;
            int totalRows = GraphicsDevice.PresentationParameters.BackBufferHeight / (int)_player.Height;

            // Choose a random row and column based on the total number of each
            int column = Random.Shared.Next(0, totalColumns);
            int row = Random.Shared.Next(0, totalRows);

            // Change the bat position by setting the x and y values equal to
            // the column and row multiplied by the width and height.
            _playerPosition = new Vector2(column * _player.Width, row * _player.Height);
        }

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
        _enemy.Draw(SpriteBatch, _enemyPosition);

        // Draw all agents.
        foreach (var agent in _agents)
            agent.Draw(SpriteBatch, _agentSprite);

        // Draw all agents.
        foreach (var agent in _agents)
            agent.Draw(SpriteBatch, _agentSprite);

        // Always end the sprite batch when finished.
        SpriteBatch.End();

        base.Draw(gameTime);
    }
}

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MainEngine;
using MainEngine.Graphics;
using MainEngine.Input;
using MainEngine.FlockEnemy;
using MainEngine.Camera;
using MainEngine.Projectile;

namespace TheNewBeginning.Core;

public class TheNewBeginningGame : HQ
{
    // Defines the slime animated sprite.
    private AnimatedSprite _player;

    private Health _playerHP;

    private Camera _camera;

    // Defines the bat animated sprite.
    private AnimatedSprite _enemy;

    private Health _enemyHP;

    private bool _enemyDead;

    private List<Projectile> _projectiles = new();
    private Sprite _projectileSprite;

    // Agent flock
    private List<Agent> _agents;
    private Sprite _agentSprite;
    private AgentConfig _agentConfig;
    private List<ForceSource> _forceSources;
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
        _camera = new Camera();
        _playerHP = new Health(3);
        _enemyHP = new Health(3);
        _enemyDead = false;

        base.Initialize();
    }

    protected override void LoadContent()
    {
        // Create the texture atlas from the XML configuration file.
        TextureAtlas atlas = TextureAtlas.FromFile(Content, "images/atlas-definition.xml");

        // Create the player animated sprite from the atlas.
        _player = atlas.CreateAnimatedSprite("player-animation");
        _player.Scale = new Vector2(4.0f, 4.0f);
        _player.CenterOrigin();

        // Create the enemy animated sprite from the atlas.
        _enemy = atlas.CreateAnimatedSprite("enemy-animation");
        _enemy.Scale = new Vector2(4.0f, 4.0f);
        _enemy.CenterOrigin();

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
            AlignmentRadius = 75f,
            AttractionRadius = 125f,
            AttractionAngle = MathHelper.ToRadians(70f),
            RepulsionForce = 7f,
            AlignmentForce = 3f,
            AttractionForce = 1f,
            GravitationForce = 0.75f,
            DebugVisible = true

        };
        // Force sources list (rebuilt each frame).
        _forceSources = new List<ForceSource>();
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
        _projectileSprite = atlas.CreateSprite("Arrow");
        _projectileSprite.Scale = new Vector2(2f);
        _projectileSprite.CenterOrigin();
    }

    protected override void Update(GameTime gameTime)
    {
        // Update the player animated sprite.
        _player.Update(gameTime);

        // Update the enemy animated sprite.
        _enemy.Update(gameTime);

        // Build force sources for this frame.
        _forceSources.Clear();
        _forceSources.Add(new ForceSource(_playerPosition, 250f, -100f));
        // Add projectiles, obstacles, lures, etc.:
        // _forceSources.Add(new ForceSource(projectilePos, 60f, -15f));  // repels
        // _forceSources.Add(new ForceSource(lurePos, 120f, 5f));       // attracts
        // Process agent flocking logic and update positions.
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Agent.Process(_agents, _agentConfig, _forceSources);
        foreach (var agent in _agents)
            agent.Update(dt, 1280, 720);

        // Check for mouse input and handle it.
        CheckMouseInput();
        // Check for keyboard input and handle it.
        CheckKeyboardInput();

        // Check for gamepad input and handle it.
        CheckGamePadInput();

        _camera.Pos = _playerPosition;

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
            
            _playerHP.TakeDamage(2);
            if(_playerHP.IsDead)
            {
                Exit();
            }
        }

            foreach (var projectile in _projectiles)
            {
                projectile.Update(dt);
                if(projectile.Bounds.Intersects(enemyBounds))
                {
                    _enemyHP.TakeDamage(1);
                    projectile.Hit = true;
                }
            }
            _projectiles.RemoveAll(b => b.IsDead);

            if (_enemyHP.IsDead && !_enemyDead)
            {
                _enemyDead = true;
            }
        base.Update(gameTime);
    }

    private void CheckMouseInput()
    {
        if (Input.Mouse.WasButtonJustPressed(MouseButton.Left))
        {
            Vector2 mouseScreen = Input.Mouse.Position.ToVector2();

            Vector2 mouseWorld = 
                Vector2.Transform(
                    mouseScreen,
                    Matrix.Invert(_camera.get_transformation(GraphicsDevice))
                );

            Vector2 direction = mouseWorld - _playerPosition;
            direction.Normalize();

            Projectile projectile = new Projectile
            {
                Position = _playerPosition,
                Direction = direction
            };
            _projectiles.Add(projectile);
        }
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
        SpriteBatch.Begin(
        samplerState: SamplerState.PointClamp,
        transformMatrix: _camera.get_transformation(GraphicsDevice)
        );

        // Draw the player sprite.
        _player.Draw(SpriteBatch, _playerPosition);

        // Draw the enemy sprite 10px to the right of the player.
        if (!_enemyDead)
        {
            _enemy.Draw(SpriteBatch, _enemyPosition);
        }
        
        // Draw all agents.
        foreach (var agent in _agents)
            {
                agent.Draw(SpriteBatch, _agentSprite);
                agent.DrawDebug(SpriteBatch, _agentConfig);
            }
        
        Agent.DrawDebugForceSources(SpriteBatch, _forceSources);

        foreach (var projectile in _projectiles)
        {
            projectile.Draw(SpriteBatch, _projectileSprite);
        }

        

        // Always end the sprite batch when finished.
        SpriteBatch.End();

        base.Draw(gameTime);
    }
}

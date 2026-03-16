using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MainEngine.Entities;
using MainEngine;
using MainEngine.Graphics;
using MainEngine.Input;
using MainEngine.FlockEnemy;
using MainEngine.Camera;
using MainEngine.Projectile;

namespace TheNewBeginning.Core;

public class TheNewBeginningGame : HQ
{
    
    private Player _player;
    private Camera _camera;
    private Enemy _enemy;

    private List<Projectile> _projectiles = new();
    private Sprite _projectileSprite;

    // Agent flock
    private List<Agent> _agents;
    private Sprite _agentSprite;
    private AgentConfig _agentConfig;
    private List<ForceSource> _forceSources;

    public TheNewBeginningGame() : base("The New Beginning", 1280, 720, false)
    {

    }
    protected override void Initialize()
    {
        _camera = new Camera();
        base.Initialize();
    }
    protected override void LoadContent()
    {
        // Create the texture atlas from the XML configuration file.
        TextureAtlas atlas = TextureAtlas.FromFile(Content, "images/atlas-definition.xml");

        // Create the player animated sprite from the atlas.
        AnimatedSprite playerSprite = atlas.CreateAnimatedSprite("player-animation");
        playerSprite.Scale = new Vector2(4f);
        playerSprite.CenterOrigin();

        _player = new Player(playerSprite, Vector2.Zero, 3);

        // Create the enemy animated sprite from the atlas.
        AnimatedSprite enemySprite = atlas.CreateAnimatedSprite("enemy-animation");
        enemySprite.Scale = new Vector2(4f);
        enemySprite.CenterOrigin();

        _enemy = new Enemy(enemySprite, Vector2.Zero, 3);

        _enemy.Position = new Vector2(playerSprite.Width + 10, 0);

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
        // Update entities sprites.
        _player.Update(gameTime);
        _enemy.Update(gameTime);

        // Build force sources for this frame.
        _forceSources.Clear();
        _forceSources.Add(new ForceSource(_player.Position, 250f, -100f));
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

        _camera.Pos = _player.Position;

        // Creating a bounding circle for entities sprites to use for collision checks.
        Circle playerBounds = _player.GetBounds();
        
        Circle enemyBounds = _enemy.GetBounds();

        if (enemyBounds.Intersects(_player.GetBounds()))
        {
            
            // Divide the width and height of the screen into equal columns and
            // rows based on the width and height of the player.
            int totalColumns = GraphicsDevice.PresentationParameters.BackBufferWidth / (int)_player.Sprite.Width;
            int totalRows = GraphicsDevice.PresentationParameters.BackBufferHeight / (int)_player.Sprite.Height;

            // Choose a random row and column based on the total number of each
            int column = Random.Shared.Next(0, totalColumns);
            int row = Random.Shared.Next(0, totalRows);

            // Change the player position by setting the x and y values equal to
            // the column and row multiplied by the width and height.
            _player.Position = new Vector2(column * _player.Sprite.Width, row * _player.Sprite.Height);
            
            _player.Health.TakeDamage(2);
            if(_player.Health.IsDead)
            {
                Exit();
            }
        }

            foreach (var projectile in _projectiles)
            {
                projectile.Update(dt);
                if(projectile.Bounds.Intersects(enemyBounds))
                {
                    _enemy.Health.TakeDamage(1);
                    projectile.Hit = true;
                }
            }
            _projectiles.RemoveAll(b => b.IsDead);

            if (_enemy.Health.IsDead && !_enemy.IsDead)
            {
                _enemy.IsDead = true;
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

            Vector2 direction = mouseWorld - _player.Position;
            direction.Normalize();

            Projectile projectile = new Projectile
            {
                Position = _player.Position,
                Direction = direction
            };
            _projectiles.Add(projectile);
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
        _player.Draw(SpriteBatch);

        // Draw the enemy sprite 10px to the right of the player.
        if (!_enemy.IsDead)
        {
            _enemy.Draw(SpriteBatch);
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

        //End the SpriteBatch
        SpriteBatch.End();

        base.Draw(gameTime);
    }
}

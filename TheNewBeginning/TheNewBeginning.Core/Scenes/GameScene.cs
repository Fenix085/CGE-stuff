using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MainEngine;
using MainEngine.Graphics;
using MainEngine.Input;
using MainEngine.Scenes;
using MainEngine.FlockEnemy;
using MainEngine.Camera;
using MainEngine.Projectile;
using MainEngine.Entities;
using System.Collections.Generic;

namespace TheNewBeginning.Scenes;

public class GameScene : Scene
{
    private Player _player;
    private Camera _camera;
    private Enemy _enemy;

    private List<Projectile> _projectiles = new();
    private Sprite _projectileSprite;

    // Agent flock
    private List<Agent> _agents;
    private AgentConfig _agentConfig;
    private List<ForceSource> _forceSources;
    public override void Initialize()
    {
        _camera = new Camera();
        base.Initialize();
    }
    public override void LoadContent()
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
        Sprite agentSpriteTemplate = atlas.CreateSprite("enemy-1");
        agentSpriteTemplate.Scale = new Vector2(3.0f, 3.0f);
        TextureRegion agentRegion = agentSpriteTemplate.Region;

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
            Agent agent = new Agent(agentRegion, center);
            agent.Scale = agentSpriteTemplate.Scale;
            agent.Scatter(1280, 720);
            agent.Center = center;
            _agents.Add(agent);
        }
        _projectileSprite = atlas.CreateSprite("Arrow");
        _projectileSprite.Scale = new Vector2(2f);
        _projectileSprite.CenterOrigin();
    }
    public override void Update(GameTime gameTime)
    {
        if (HQ.Input.Keyboard.IsKeyDown(Keys.Escape))
            HQ.Instance.Exit();
        // Update the player animated sprite.
        _player.Update(gameTime);
        _enemy.Update(gameTime);

        // Build force sources for this frame.
        _forceSources.Clear();
        _forceSources.Add(new ForceSource(_player.Position, 250f, -100f));
        // Add projectiles, obstacles, lures, etc.:
        // _forceSources.Add(new ForceSource(projectilePos, 60f, -15f));  // repels
        // _forceSources.Add(new ForceSource(lurePos, 120f, 5f));       // attracts
        // Process agent flocking logic and update positions.
        Agent.Process(_agents, _agentConfig, _forceSources);
        foreach (var agent in _agents)
            agent.Update(gameTime);

        _camera.Pos = _player.Position;
        
        // Check for mouse input and handle it.
        CheckMouseInput();



        // Creating a bounding circle for entities sprites to use for collision checks.
        Circle playerBounds = _player.GetBounds();
        Circle enemyBounds = _enemy.GetBounds();

        if (enemyBounds.Intersects(playerBounds))
        {
            
            // Divide the width and height of the screen into equal columns and
            // rows based on the width and height of the player.
            int totalColumns = HQ.GraphicsDevice.PresentationParameters.BackBufferWidth / (int)_player.Sprite.Width;
            int totalRows = HQ.GraphicsDevice.PresentationParameters.BackBufferHeight / (int)_player.Sprite.Height;

            // Choose a random row and column based on the total number of each
            int column = Random.Shared.Next(0, totalColumns);
            int row = Random.Shared.Next(0, totalRows);

            // Change the player position by setting the x and y values equal to
            // the column and row multiplied by the width and height.
            _player.Position = new Vector2(column * _player.Sprite.Width, row * _player.Sprite.Height);
            
            _player.Health.TakeDamage(2);
            if(_player.Health.IsDead)
            {
                HQ.Instance.Exit();
            }
        }

            foreach (var projectile in _projectiles)
            {
                projectile.Update(gameTime);
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
        if (HQ.Input.Mouse.WasButtonJustPressed(MouseButton.Left))
        {
            Vector2 mouseScreen = HQ.Input.Mouse.Position.ToVector2();

            Vector2 mouseWorld = 
                Vector2.Transform(
                    mouseScreen,
                    Matrix.Invert(_camera.get_transformation(HQ.GraphicsDevice))
                );

            Vector2 direction = mouseWorld - _player.Position;
            direction.Normalize();

            Projectile projectile = new Projectile
            {
                Position = _player.Position,
                Direction = direction,
                Region = _projectileSprite.Region,
                Scale = _projectileSprite.Scale,
                Origin = _projectileSprite.Origin
            };
            _projectiles.Add(projectile);
        }
    }
    public override void Draw(GameTime gameTime)
    {
        // Clear the back buffer.
        HQ.GraphicsDevice.Clear(Color.CornflowerBlue);

        // Begin the sprite batch to prepare for rendering.
        HQ.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.get_transformation(HQ.GraphicsDevice));

        // Draw the player sprite.
        _player.Draw(gameTime, HQ.SpriteBatch);

        // Draw the enemy sprite 10px to the right of the player.
        if (!_enemy.IsDead)
        {
            _enemy.Draw(gameTime, HQ.SpriteBatch);
        }

        // Draw all agents.
        foreach (var agent in _agents)
        {
            agent.Draw(gameTime, HQ.SpriteBatch);
            agent.DrawDebug(HQ.SpriteBatch, _agentConfig);
        }
        
        Agent.DrawDebugForceSources(HQ.SpriteBatch, _forceSources);

        foreach (var projectile in _projectiles)
        {
            projectile.Draw(gameTime, HQ.SpriteBatch);
        }

        // Always end the sprite batch when finished.
        HQ.SpriteBatch.End();

        base.Draw(gameTime);
    }
}

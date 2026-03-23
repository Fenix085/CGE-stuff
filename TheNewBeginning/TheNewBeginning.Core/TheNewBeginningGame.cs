using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MainEngine.Entities;
using MainEngine;
using MainEngine.Graphics;
using MainEngine.Input;
using MainEngine.FlockEnemy;
using MainEngine.Camera;
using MainEngine.Projectile;

namespace TheNewBeginning.Core;

public class TheNewBeginningGame : Game
{
    private class EnemyFlockGroup
    {
        public Enemy Enemy;
        public List<Agent> Agents = new();
        public AgentConfig Config;
        public List<ForceSource> ForceSources = new();
    }
    
    private Player _player;
    private Camera _camera;
    private GraphicsDeviceManager _graphics;
    private HQ _hq;

    private List<Projectile> _projectiles = new();
    private Sprite _projectileSprite;

    // Agent flock
    private List<EnemyFlockGroup> _enemyFlocks = new();
    private const int EnemyCount = 3;
    private const int AgentsPerEnemy = 20;

    public TheNewBeginningGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        {
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.IsFullScreen = false;
        };
        _graphics.ApplyChanges();
        Content.RootDirectory = "Content";
        Window.Title = "Some Better Name Here";
        IsMouseVisible = true;
    }
    protected override void Initialize()
    {
        _camera = new Camera();
        _hq = new HQ(GraphicsDevice);
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

        _player = new Player(_hq, playerSprite, Vector2.Zero, 3);

        Sprite agentSprite = atlas.CreateSprite("enemy-1");
        agentSprite.Scale = new Vector2(2f, 2f);
        TextureRegion agentRegion = agentSprite.Region;

        _enemyFlocks.Clear();
        for(int i = 0; i < EnemyCount; i++)
        {
            AnimatedSprite enemySprite = atlas.CreateAnimatedSprite("enemy-animation");
            enemySprite.Scale = new Vector2(4f);
            enemySprite.CenterOrigin();

            Vector2 enemyStart = new Vector2(150 + i * 120, 100 + (i % 2) * 180);
            Enemy enemy = new Enemy(enemySprite, enemyStart, 3);

            AgentConfig config = new AgentConfig
            {
                AgentSpeed = 65f,
                RepulsionRadius = 50f,
                AlignmentRadius = 100f,
                AttractionRadius = 200f,
                AttractionAngle = MathHelper.ToRadians(70f),
                RepulsionForce = 10f,
                AlignmentForce = 5f,
                AttractionForce = 2f,
                GravitationForce = 0.07f,
                DebugVisible = true
            };

            var group = new EnemyFlockGroup { Enemy = enemy, Config = config };

            for (int j = 0; j < AgentsPerEnemy; j++)
            {
                Agent agent = new Agent(agentRegion, enemyStart);
                agent.Scale = agentSprite.Scale;
                agent.Scatter(1280, 720);
                agent.Center = enemyStart;
                group.Agents.Add(agent);
            }

            _enemyFlocks.Add(group);
        }

        _projectileSprite = atlas.CreateSprite("Arrow");
        _projectileSprite.Scale = new Vector2(2f);
        _projectileSprite.CenterOrigin();
    }
    protected override void Update(GameTime gameTime)
    {
        _hq.Update(gameTime);

        if (_hq.Input.Keyboard.IsKeyDown(Keys.Escape))
            Exit();
        // Update the player animated sprite.
        _player.Update(gameTime);

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        const float leaderActiveSpeed = 90f;

        foreach ( var group in _enemyFlocks)
        {
            group.Enemy.Update(gameTime);
            if (!group.Enemy.IsDead)
                group.Enemy.MoveToward(_player.Position, dt, leaderActiveSpeed);

            if (group.Enemy.CurrentSpeed <= 0f && !group.Enemy.IsDead)
            {
                group.Config.AgentSpeed = 20f;
            }else if (group.Enemy.IsDead)
            {
                group.Config.AgentSpeed = 0f;
            }else
            {
                group.Config.AgentSpeed = leaderActiveSpeed + 20f;
            }

            group.ForceSources.Clear();
            group.ForceSources.Add(new ForceSource(_player.Position, 100f, -70f));
            if (!group.Enemy.IsDead)
            {
                group.ForceSources.Add(new ForceSource(group.Enemy.Position, 275f, 30f));
                group.ForceSources.Add(new ForceSource(group.Enemy.Position, 100f, -90f));
            }
            foreach (var agent in group.Agents)
                agent.Center = group.Enemy.Position;

            Agent.Process(group.Agents, group.Config, group.ForceSources);
            foreach (var agent in group.Agents)
                agent.Update(gameTime);
        }

        _camera.Pos = _player.Position;
        
        // Check for mouse input and handle it.
        CheckMouseInput();

        foreach (var projectile in _projectiles)
            projectile.Update(gameTime);

        // Creating bounding circles for collision checks.
        Circle playerBounds = _player.GetBounds();

        foreach ( var group in _enemyFlocks)
        {
            if (group.Enemy.IsDead)
                continue;

            Circle enemyBounds = group.Enemy.GetBounds();
            if (enemyBounds.Intersects(playerBounds))
            {
                int totalColumns = GraphicsDevice.PresentationParameters.BackBufferWidth / (int)_player.Sprite.Width;
                int totalRows = GraphicsDevice.PresentationParameters.BackBufferHeight / (int)_player.Sprite.Height;

                int column = Random.Shared.Next(0, totalColumns);
                int row = Random.Shared.Next(0, totalRows);

                _player.Position = new Vector2(column * _player.Sprite.Width, row * _player.Sprite.Height);

                _player.Health.TakeDamage(2);
                if (_player.Health.IsDead)
                {
                    // Exit();
                }
            }

            foreach (var projectile in _projectiles)
            {
                if (!projectile.IsDead && projectile.Bounds.Intersects(enemyBounds))
                {
                    group.Enemy.Health.TakeDamage(1);
                    projectile.Hit = true;
                }
            }    

            if(group.Enemy.Health.IsDead && !group.Enemy.IsDead)
                group.Enemy.ApplyDeath();
        }
    }
    private void CheckMouseInput()
    {
        if (_hq.Input.Mouse.WasButtonJustPressed(MouseButton.Left))
        {
            Vector2 mouseScreen = _hq.Input.Mouse.Position.ToVector2();

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
                Direction = direction,
                Region = _projectileSprite.Region,
                Scale = _projectileSprite.Scale,
                Origin = _projectileSprite.Origin
            };
            _projectiles.Add(projectile);
        }
    }
    protected override void Draw(GameTime gameTime)
    {
        // Clear the back buffer.
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // Begin the sprite batch to prepare for rendering.
        _hq.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.get_transformation(GraphicsDevice));

        // Draw the player sprite.
        _player.Draw(gameTime, _hq.SpriteBatch);

        foreach ( var group in _enemyFlocks)
        {
            if (!group.Enemy.IsDead)
                group.Enemy.Draw(gameTime, _hq.SpriteBatch);

            foreach (var agent in group.Agents)
            {
                agent.Draw(gameTime, _hq.SpriteBatch);
                agent.DrawDebug(_hq.SpriteBatch, group.Config);
            }

            Agent.DrawDebugForceSources(_hq.SpriteBatch, group.ForceSources);
        }

        foreach (var projectile in _projectiles)
        {
            projectile.Draw(gameTime, _hq.SpriteBatch);
        }

        // Always end the sprite batch when finished.
        _hq.SpriteBatch.End();

        base.Draw(gameTime);
    }
}

using System;
using System.Collections.Generic;
using MainEngine;
using MainEngine.Camera;
using MainEngine.Entities;
using MainEngine.FlockEnemy;
using MainEngine.Graphics;
using MainEngine.Input;
using MainEngine.Projectile;
using MainEngine.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheNewBeginning.Core.EnemyFSM;

namespace TheNewBeginning.Scenes;

public class GameScene : Scene
{
    private class EnemyFlockGroup
    {
        public Enemy Enemy;
        public EnemyFSM Brain;
        public List<Agent> Agents = new();
        public AgentConfig Config;
        public List<ForceSource> ForceSources = new();
    }
    private Player _player;
    private Camera _camera;
    private Enemy _enemy;

    private List<Projectile> _projectiles = new();
    private Sprite _projectileSprite;

    // Agent flock
    private List<EnemyFlockGroup> _enemyFlocks = new();
    private const int EnemyCount = 1;
    private const int AgentsPerEnemy = 20;
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
        Sprite agentSprite = atlas.CreateSprite("enemy-1");
        agentSprite.Scale = new Vector2(2f, 2f);
        TextureRegion agentRegion = agentSprite.Region;

        _enemyFlocks.Clear();
        for(int i = 0; i < EnemyCount; i++)
        {
            AnimatedSprite flockEnemySprite = atlas.CreateAnimatedSprite("enemy-animation");
            flockEnemySprite.Scale = new Vector2(4f);
            flockEnemySprite.CenterOrigin();

            Vector2 enemyStart = new Vector2(150 + i * 120, 100 + (i % 2) * 180);
            Enemy enemy = new Enemy(flockEnemySprite, enemyStart, 3);

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
                GravitationForce = 0.5f,
                DebugVisible = true
            };

            var group = new EnemyFlockGroup { Enemy = enemy,
                Brain = new EnemyFSM(enemy),
                Config = config };

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
    public override void Update(GameTime gameTime)
    {
        if (HQ.Input.Keyboard.IsKeyDown(Keys.Escape))
            HQ.Instance.Exit();
        // Update the player animated sprite.
        _player.Update(gameTime);

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        foreach ( var group in _enemyFlocks)
        {
            group.Enemy.Update(gameTime);
            group.Brain.Update(_player.Position, _player.Health.IsDead, gameTime);
            bool following = group.Brain.ShouldFlockAttackPlayer;
            
            if (following)
            {
                group.Agents.ForEach(agent => agent.MoveToward(_player.Position, dt, group.Brain.FlockSpeed));
            }

            group.Config.AgentSpeed = group.Brain.FlockSpeed;


            group.ForceSources.Clear();
            group.ForceSources.Add(new ForceSource(_player.Position, 45f, -10f));
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
                var pp = HQ.GraphicsDevice.PresentationParameters;
                int totalColumns = pp.BackBufferWidth / (int)_player.Sprite.Width;
                int totalRows = pp.BackBufferHeight / (int)_player.Sprite.Height;

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

        }
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

        foreach ( var group in _enemyFlocks)
        {
            if (!group.Enemy.IsDead)
                group.Enemy.Draw(gameTime, HQ.SpriteBatch);

            foreach (var agent in group.Agents)
            {
                agent.Draw(gameTime, HQ.SpriteBatch);
                agent.DrawDebug(HQ.SpriteBatch, group.Config);
            }

            Agent.DrawDebugForceSources(HQ.SpriteBatch, group.ForceSources);
        }
        
        foreach (var projectile in _projectiles)
        {
            projectile.Draw(gameTime, HQ.SpriteBatch);
        }

        // Always end the sprite batch when finished.
        HQ.SpriteBatch.End();

        base.Draw(gameTime);
    }
}

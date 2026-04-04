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
using MainEngine.Navigation;

namespace TheNewBeginning.Scenes;

public class GameScene : Scene
{
    private class EnemyFlockGroup
    {
        public Enemy Enemy;
        public List<Agent> Agents = new();
        public AgentConfig Config;
        public List<ForceSource> ForceSources = new();
        public NavigationFollower Follower;
        public float RepathTimer;
        public bool IsLocalPursuit;
    }
    private Player _player;
    private Camera _camera;
    private Enemy _enemy;

    private List<Projectile> _projectiles = new();
    private Sprite _projectileSprite;

    // Agent flock
    private List<EnemyFlockGroup> _enemyFlocks = new();
    private const int EnemyCount = 2;
    private const int AgentsPerEnemy = 20;
    private const float RepathIntervalSeconds = 0.25f;
    private const float LocalPursuitEnterRadius = 180f;
    private const float LocalPursuitExitRadius = 260f;

    private Navigation _nav = new();
    private Texture2D _debugPixel;
    private bool _drawNavigationDebug = true;

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

        _nav.Clear();

        var node1 = new NavNode { Id = 1, Position = new Vector2(100f, 100f)};
        var node2 = new NavNode { Id = 2, Position = new Vector2(500f, 200f)};
        var node3 = new NavNode { Id = 3, Position = new Vector2(300f, 400f)};

        _nav.AddNode(node1);
        _nav.AddNode(node2);
        _nav.AddNode(node3);

        _nav.AddHighway(
            Highway.Create(
                id: 1,
                fromId: 1,
                toId: 2,
                fromPosition: node1.Position,
                toPosition: node2.Position,
                speedLimit: 90f
            )
        );

        _nav.AddHighway(
            Highway.Create(
                id: 2,
                fromId: 2,
                toId: 1,
                fromPosition: node2.Position,
                toPosition: node1.Position,
                speedLimit: 90f
            )
        );
        _nav.AddHighway(
            Highway.Create(
                id: 3,
                fromId: 2,
                toId: 3,
                fromPosition: node2.Position,
                toPosition: node3.Position,
                speedLimit: 90f
            )
        );
        foreach (var group in _enemyFlocks)
        {
            group.Follower = new NavigationFollower(_nav);
            group.RepathTimer = Random.Shared.NextSingle() * RepathIntervalSeconds;
            group.IsLocalPursuit = false;
        }

        _debugPixel = new Texture2D(HQ.GraphicsDevice, 1, 1);
        _debugPixel.SetData(new[] { Color.White });
    }
    public override void Update(GameTime gameTime)
    {
        if (HQ.Input.Keyboard.IsKeyDown(Keys.Escape))
            HQ.Instance.Exit();

        if (HQ.Input.Keyboard.WasKeyJustPressed(Keys.F3))
            _drawNavigationDebug = !_drawNavigationDebug;

        // Update the player animated sprite.
        _player.Update(gameTime);

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        const float leaderActiveSpeed = 90f;

        foreach ( var group in _enemyFlocks)
        {
            group.Enemy.Update(gameTime);

            float distanceToPlayer = Vector2.Distance(group.Enemy.Position, _player.Position);

            if (!group.Enemy.IsDead)
            {
                if (group.IsLocalPursuit)
                {
                    if (distanceToPlayer > LocalPursuitExitRadius)
                        group.IsLocalPursuit = false;
                }
                else if (distanceToPlayer <= LocalPursuitEnterRadius)
                {
                    group.IsLocalPursuit = true;
                    group.Follower.Clear();
                }

                if (group.IsLocalPursuit)
                {
                    group.Enemy.MoveToward(_player.Position, dt, leaderActiveSpeed);
                }
                else
                {
                    group.RepathTimer -= dt;
                    if (group.RepathTimer <= 0f)
                    {
                        group.RepathTimer = RepathIntervalSeconds;

                        bool foundPath = group.Follower.TryPlanFromWorld(
                            group.Enemy.Position,
                            _player.Position,
                            snapDistance: float.PositiveInfinity
                        );

                        if (!foundPath)
                            group.Follower.Clear();
                    }

                    Vector2 leaderTarget = group.Follower.HasPath
                        ? group.Follower.GetSteeringTarget(group.Enemy.Position)
                        : group.Enemy.Position;

                    group.Enemy.MoveToward(leaderTarget, dt, leaderActiveSpeed);
                }
            }

            distanceToPlayer = Vector2.Distance(group.Enemy.Position, _player.Position);
            bool following = distanceToPlayer <= group.Enemy.FollowRadius;
            
            if (following)
            {
                group.Agents.ForEach(agent => agent.MoveToward(_player.Position, dt, leaderActiveSpeed + 50f));
            }

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

        _projectiles.RemoveAll(projectile => projectile.IsDead);

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

            if(group.Enemy.Health.IsDead && !group.Enemy.IsDead)
                group.Enemy.ApplyDeath();
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

        DrawNavigationDebug();

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

    private void DrawNavigationDebug()
    {
        if (!_drawNavigationDebug || _debugPixel == null)
            return;

        foreach (var lane in _nav.Highways.Values)
        {
            if (!_nav.TryGetNode(lane.FromId, out var fromNode) || !_nav.TryGetNode(lane.ToId, out var toNode))
                continue;

            DrawLine(fromNode.Position, toNode.Position, lane.IsBlocked ? Color.DarkRed : Color.Orange, 2f);
        }

        foreach (var node in _nav.Nodes.Values)
        {
            DrawSquare(node.Position, 8f, Color.Gold);
        }

        foreach (var group in _enemyFlocks)
        {
            if (group.Follower == null)
                continue;

            NavigationPath path = group.Follower.CurrentPath;
            if (path.IsEmpty)
                continue;

            foreach (int highwayId in path.HighwayIds)
            {
                if (!_nav.Highways.TryGetValue(highwayId, out var lane))
                    continue;

                if (!_nav.TryGetNode(lane.FromId, out var fromNode) || !_nav.TryGetNode(lane.ToId, out var toNode))
                    continue;

                DrawLine(fromNode.Position, toNode.Position, Color.LimeGreen, 3f);
            }
        }
    }

    private void DrawSquare(Vector2 center, float size, Color color)
    {
        int half = (int)(size * 0.5f);
        var rect = new Rectangle((int)center.X - half, (int)center.Y - half, (int)size, (int)size);
        HQ.SpriteBatch.Draw(_debugPixel, rect, color);
    }

    private void DrawLine(Vector2 start, Vector2 end, Color color, float thickness)
    {
        Vector2 delta = end - start;
        float length = delta.Length();

        if (length <= 0.001f)
            return;

        float angle = MathF.Atan2(delta.Y, delta.X);
        HQ.SpriteBatch.Draw(_debugPixel, start, null, color, angle, Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0f);
    }
}

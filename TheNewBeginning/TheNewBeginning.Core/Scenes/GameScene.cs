using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using MainEngine;
using MainEngine.Camera;
using MainEngine.Entities;
using MainEngine.FlockEnemy;
using MainEngine.Graphics;
using MainEngine.Input;
using MainEngine.Projectile;
using MainEngine.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using TheNewBeginning.Core.EnemyFSM;
using TheNewBeginning.UI;
using MainEngine.Navigation;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Tiled.Renderers;

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
        public NavigationFollower Follower;
        public float RepathTimer;
        public bool IsLocalPursuit;
    }

    private TiledMap _tiledMap;
    private TiledMapRenderer _tiledRenderer;
    private Player _player;
    private Camera _camera;
    private Enemy _enemy;
    

    private List<Projectile> _projectiles = new();
    private Sprite _projectileSprite;

    // Agent flock
    private List<EnemyFlockGroup> _enemyFlocks = new();


private Panel _pausePanel;
private AnimatedButton _resumeButton;

private SoundEffect _uiSoundEffect;
private TextureAtlas _atlas;

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
        HQ.ExitOnEscape = false;
        _camera = new Camera();
        base.Initialize();
    }
    public override void LoadContent()
    {
        _tiledMap = Content.Load<TiledMap>("map2");
        _tiledRenderer = new TiledMapRenderer(HQ.GraphicsDevice, _tiledMap);
        
        _atlas = TextureAtlas.FromFile(Content, "images/atlas-definition.xml");
        
        InitializeUI();

        AnimatedSprite playerSprite = _atlas.CreateAnimatedSprite("player-animation");
        playerSprite.Scale = new Vector2(4f);
        playerSprite.CenterOrigin();

        _player = new Player(playerSprite, Vector2.Zero, 3);

        AnimatedSprite enemySprite = _atlas.CreateAnimatedSprite("enemy-animation");
        enemySprite.Scale = new Vector2(4f);
        enemySprite.CenterOrigin();

        _enemy = new Enemy(enemySprite, Vector2.Zero, 3);

        _enemy.Position = new Vector2(playerSprite.Width + 10, 0);

        Sprite agentSprite = _atlas.CreateSprite("enemy-1");
        agentSprite.Scale = new Vector2(2f, 2f);
        TextureRegion agentRegion = agentSprite.Region;

        _enemyFlocks.Clear();
        for(int i = 0; i < EnemyCount; i++)
        {
            AnimatedSprite flockEnemySprite = _atlas.CreateAnimatedSprite("enemy-animation");
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
        _projectileSprite = _atlas.CreateSprite("Arrow");
        _projectileSprite.Scale = new Vector2(2f);
        _projectileSprite.CenterOrigin();

        _uiSoundEffect = HQ.Content.Load<SoundEffect>("audio/ui");
    }

private void CreatePausePanel()
{
    _pausePanel = new Panel();
    _pausePanel.Anchor(Anchor.Center);
    _pausePanel.WidthUnits = DimensionUnitType.Absolute;
    _pausePanel.HeightUnits = DimensionUnitType.Absolute;
    _pausePanel.Height = 70;
    _pausePanel.Width = 264;
    _pausePanel.IsVisible = false;
    _pausePanel.AddToRoot();

    // TextureRegion backgroundRegion = _atlas.GetRegion("panel-background");

    // NineSliceRuntime background = new NineSliceRuntime();
    // background.Dock(Dock.Fill);
    // background.Texture = backgroundRegion.Texture;
    // background.TextureAddress = TextureAddress.Custom;
    // background.TextureHeight = backgroundRegion.Height;
    // background.TextureLeft = backgroundRegion.SourceRectangle.Left;
    // background.TextureTop = backgroundRegion.SourceRectangle.Top;
    // background.TextureWidth = backgroundRegion.Width;
    // _pausePanel.AddChild(background);

    TextRuntime textInstance = new TextRuntime();
    textInstance.Text = "PAUSED";
    textInstance.CustomFontFile = @"fonts/04b_30.fnt";
    textInstance.UseCustomFont = true;
    textInstance.FontScale = 0.5f;
    textInstance.X = 10f;
    textInstance.Y = 10f;
    _pausePanel.AddChild(textInstance);

    _resumeButton = new AnimatedButton(_atlas);
    _resumeButton.Text = "RESUME";
    _resumeButton.Anchor(Anchor.BottomLeft);
    _resumeButton.X = 9f;
    _resumeButton.Y = -9f;
    _resumeButton.Click += HandleResumeButtonClicked;
    _pausePanel.AddChild(_resumeButton);

    AnimatedButton quitButton = new AnimatedButton(_atlas);
    quitButton.Text = "QUIT";
    quitButton.Anchor(Anchor.BottomRight);
    quitButton.X = -9f;
    quitButton.Y = -9f;
    quitButton.Click += HandleQuitButtonClicked;

    _pausePanel.AddChild(quitButton);
}

private void HandleResumeButtonClicked(object sender, EventArgs e)
{
    HQ.Audio.PlaySoundEffect(_uiSoundEffect);

    _pausePanel.IsVisible = false;
}

private void HandleQuitButtonClicked(object sender, EventArgs e)
{
    HQ.Audio.PlaySoundEffect(_uiSoundEffect);

    HQ.ChangeScene(new TitleScene());
}

private void InitializeUI()
{
    GumService.Default.Root.Children.Clear();

    CreatePausePanel();
}

private float DistancePointToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;

        if(ab.LengthSquared() == 0)
            return Vector2.Distance(point,a);
        
        float t = Vector2.Dot(point - a, ab) / ab.LengthSquared();
        t = MathHelper.Clamp(t, 0f, 1f);

        Vector2 closest = a + t * ab;
        return Vector2.Distance(point, closest);
    }
public override void Update(GameTime gameTime)
{
    GumService.Default.Update(gameTime);

    // Pause toggle
    if (HQ.Input.Keyboard.WasKeyJustPressed(Keys.Escape))
    {
        _pausePanel.IsVisible = !_pausePanel.IsVisible;

        if (_pausePanel.IsVisible)
            {
                _resumeButton.IsFocused = true;
                return;
            }
    }
    
    if (_pausePanel.IsVisible)
    {    
        return;
    }

    _tiledRenderer.Update(gameTime);
    
    _nav.Clear();

    var node1 = new NavNode { Id = 1, Position = new Vector2(100f, 100f) };
    var node2 = new NavNode { Id = 2, Position = new Vector2(700f, 600f) };
    var node3 = new NavNode { Id = 3, Position = new Vector2(300f, 400f) };

    _nav.AddNode(node1);
    _nav.AddNode(node2);
    _nav.AddNode(node3);

    _nav.AddHighway(Highway.Create(id: 1, fromId: 1, toId: 2,
        fromPosition: node1.Position, toPosition: node2.Position, speedLimit: 90f));
    _nav.AddHighway(Highway.Create(id: 3, fromId: 2, toId: 3,
        fromPosition: node2.Position, toPosition: node3.Position, speedLimit: 90f));

    foreach (var group in _enemyFlocks)
    {
        group.Follower = new NavigationFollower(_nav);
        group.RepathTimer = Random.Shared.NextSingle() * RepathIntervalSeconds;
        group.IsLocalPursuit = false;
    }

    _debugPixel = new Texture2D(HQ.GraphicsDevice, 1, 1);
    _debugPixel.SetData(new[] { Color.White });

    // Hard exit (kept from second Update; note: first Update used pause instead)
    if (HQ.Input.Keyboard.IsKeyDown(Keys.Escape))
        HQ.Instance.Exit();

    if (HQ.Input.Keyboard.WasKeyJustPressed(Keys.F3))
        _drawNavigationDebug = !_drawNavigationDebug;

    _player.Update(gameTime);

    float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

    const float leaderActiveSpeed = 90f;

    foreach (var group in _enemyFlocks)
    {
        group.Enemy.Update(gameTime);
        group.Brain.Update(_player.Position, _player.Health.IsDead, gameTime);

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

    CheckMouseInput();

    foreach (var projectile in _projectiles)
        projectile.Update(gameTime);

    _projectiles.RemoveAll(p => p.IsDead);

    Circle playerBounds = _player.GetBounds();

    foreach (var group in _enemyFlocks)
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
            if (!projectile.IsDead)
            {
                float dist = DistancePointToSegment(
                    group.Enemy.Position,
                    projectile.PreviousPosition,
                    projectile.Position
                );

                float enemyRadius = group.Enemy.GetBounds().Radius;
                float projectileRadius = projectile.Radius;

                if (dist < (enemyRadius + projectileRadius))
                {
                    group.Enemy.Health.TakeDamage(1);
                    projectile.Hit = true;
                }
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

    private void PauseGame()
{
    _pausePanel.IsVisible = true;

    _resumeButton.IsFocused = true;
}

private void CheckKeyboardInput()
{
    KeyboardInfo keyboard = HQ.Input.Keyboard;

    if (HQ.Input.Keyboard.WasKeyJustPressed(Keys.Escape))
    {
        PauseGame();
        return;
    }
}

    public override void Draw(GameTime gameTime)
    {
        HQ.GraphicsDevice.Clear(Color.CornflowerBlue);

        _tiledRenderer.Draw(_camera.get_transformation(HQ.GraphicsDevice));
        
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

        HQ.SpriteBatch.End();

        GumService.Default.Draw();

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

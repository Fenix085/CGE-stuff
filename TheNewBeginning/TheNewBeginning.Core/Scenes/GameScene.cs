using System;
using TheNewBeginning.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using MainEngine;
using MainEngine.Graphics;
using MainEngine.Input;
using MainEngine.Scenes;
using MainEngine.FlockEnemy;
using MainEngine.Camera;
using MainEngine.Projectile;
using MainEngine.Entities;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using MonoGameGum;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;

namespace TheNewBeginning.Scenes;

public class GameScene : Scene
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
    private Enemy _enemy;
    

    private List<Projectile> _projectiles = new();
    private Sprite _projectileSprite;

    // Agent flock
    private List<EnemyFlockGroup> _enemyFlocks = new();
    private const int EnemyCount = 3;
    private const int AgentsPerEnemy = 20;

private Panel _pausePanel;
private AnimatedButton _resumeButton;

private SoundEffect _uiSoundEffect;
private TextureAtlas _atlas;


    public override void Initialize()
    {
        HQ.ExitOnEscape = false;
        _camera = new Camera();
        base.Initialize();
    }
    public override void LoadContent()
    {
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

    public override void Update(GameTime gameTime)
    {
        GumService.Default.Update(gameTime);

        if (HQ.Input.Keyboard.WasKeyJustPressed(Keys.Escape))
{
    _pausePanel.IsVisible = !_pausePanel.IsVisible;

    if (_pausePanel.IsVisible)
        _resumeButton.IsFocused = true;
}

        if (_pausePanel.IsVisible)
        {
            return;
        }
        
        _player.Update(gameTime);

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        const float leaderActiveSpeed = 90f;

        foreach ( var group in _enemyFlocks)
        {
            group.Enemy.Update(gameTime);
            float distanceToPlayer = Vector2.Distance(group.Enemy.Position, _player.Position);
            bool following = distanceToPlayer <= group.Enemy.FollowRadius;
            if (!group.Enemy.IsDead)
                group.Enemy.MoveToward(_player.Position, dt, leaderActiveSpeed);
            
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
    
        CheckMouseInput();

        foreach (var projectile in _projectiles)
            projectile.Update(gameTime);

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

        HQ.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.get_transformation(HQ.GraphicsDevice));

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
}

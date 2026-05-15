using System;
using System.Collections.Generic;
using MainEngine;
using MainEngine.Scenes;
using MainEngine.Camera;
using MainEngine.Entities;
using MainEngine.FlockEnemy;
using MainEngine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LILITH.Core.Scenes
{
    public class MainScene : Scene
    {
        private Player _player;
        private Camera _camera;
        private Enemies.Tank.Tank _tank;
        private Enemies.Tank.TankFSM _tankFSM;
        private TextureRegion _agentRegion;
        private AgentConfig _agentConfig;
        private readonly List<ForceSource> _agentForceSources = new();

        public override void Initialize()
        {
            HQ.ExitOnEscape = false;
            _camera = new Camera();
            base.Initialize();
        }

        public override void LoadContent()
        {
            var agentTexture = new Texture2D(HQ.GraphicsDevice, 8, 8);
            var agentData = new Color[8 * 8];
            Array.Fill(agentData, Color.White);
            agentTexture.SetData(agentData);
            _agentRegion = new TextureRegion(agentTexture, 0, 0, 8, 8);

            var playerTexture = new Texture2D(HQ.GraphicsDevice, 24, 24);
            var playerData = new Color[24 * 24];
            Array.Fill(playerData, Color.CornflowerBlue);
            playerTexture.SetData(playerData);
            var playerRegion = new TextureRegion(playerTexture, 0, 0, 24, 24);
            var playerAnim = new Animation(new List<TextureRegion> { playerRegion }, TimeSpan.FromMilliseconds(100));
            var playerSprite = new AnimatedSprite(playerAnim);
            playerSprite.CenterOrigin();
            _player = new Player(playerSprite, new Vector2(200, 200), 3);

            var tankTexture = new Texture2D(HQ.GraphicsDevice, 32, 32);
            var tankData = new Color[32 * 32];
            Array.Fill(tankData, Color.Red);
            tankTexture.SetData(tankData);
            var tankRegion = new TextureRegion(tankTexture, 0, 0, 32, 32);
            var tankAnim = new Animation(new List<TextureRegion> { tankRegion }, TimeSpan.FromMilliseconds(100));
            var tankSprite = new AnimatedSprite(tankAnim);
            tankSprite.CenterOrigin();

            _tank = new Enemies.Tank.Tank(tankSprite, new Vector2(400, 300))
            {
                AgentSpawnIntervalSeconds = 2f,
                MaxAgents = 15,
                AgentFactory = pos =>
                {
                    var agent = new Agent(_agentRegion, pos);
                    agent.Scale = new Vector2(1f);
                    return agent;
                }
            };

            _tankFSM = new Enemies.Tank.TankFSM(_tank);

            _agentConfig = new AgentConfig
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
                DebugVisible = false
            };
        }

        public override void Update(GameTime gameTime)
        {
            _player.Update(gameTime);

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // FSM decides what the tank and its agents should do
            _tankFSM.Update(_player.Position, _player.Health.IsDead, gameTime);

            // Gate spawning through the FSM
            _tank.CanSpawn = _tankFSM.ShouldSpawnAgents;
            _tank.Update(gameTime);

            // Set flock speed from FSM
            _agentConfig.AgentSpeed = _tankFSM.FlockSpeed;

            // Centres keep agents gravitating around the tank
            _tank.UpdateAgentCenters();

            // Build force sources for this frame
            _agentForceSources.Clear();

            // Player force only when FSM says so (Attack state)
            if (_tankFSM.ApplyPlayerForce)
            {
                _agentForceSources.Add(new ForceSource(
                    _player.Position,
                    _tankFSM.PlayerForceRadius,
                    _tankFSM.PlayerForceStrength));
            }

            // Tank's own attraction/repulsion keeps agents orbiting it
            _tank.AddAgentForceSources(_agentForceSources);

            // If attacking, give agents a base velocity toward the player
            // so flocking has a direction to work with
            if (_tankFSM.ShouldFlockAttackPlayer)
            {
                foreach (var agent in _tank.Agents)
                    agent.MoveToward(_player.Position, dt, _tankFSM.FlockSpeed);
            }

            // Run flocking simulation
            Agent.Process(new List<Agent>(_tank.Agents), _agentConfig, _agentForceSources);

            // Apply velocity → position
            foreach (var agent in _tank.Agents)
                agent.Update(gameTime);

            _camera.Pos = _player.Position;
        }

        public override void Draw(GameTime gameTime)
        {
            HQ.GraphicsDevice.Clear(Color.Black);

            HQ.SpriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: _camera.get_transformation(HQ.GraphicsDevice));

            _player.Draw(gameTime, HQ.SpriteBatch);
            _tank.Draw(gameTime, HQ.SpriteBatch);

            foreach (var agent in _tank.Agents)
                agent.Draw(gameTime, HQ.SpriteBatch);

            HQ.SpriteBatch.End();
        }
    }
}
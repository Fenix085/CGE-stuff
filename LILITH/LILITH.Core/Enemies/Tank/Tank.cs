using System.Collections.Generic;
using MainEngine.Entities;
using MainEngine.FlockEnemy;
using MainEngine.Graphics;
using MainEngine.Navigation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LILITH.Core.Enemies.Tank
{
    public class Tank : Enemy
    {
        public int Damage { get; set; } = 30;

        /// <summary>
        /// Damage each agent deals to the player on contact.
        /// </summary>
        public int AgentDamage { get; set; } = 1;

        /// <summary>
        /// Collision radius for agent-vs-player hit detection.
        /// </summary>
        public float AgentHitRadius { get; set; } = 10f;

        public float AgentSpawnIntervalSeconds { get; set; } = 2f;
        public int MaxAgents { get; set; } = 15;

        private readonly TankFSM _fsm;

        public bool CanSpawn { get; set; }
        public NavigationFollower NavFollower { get; set; }

        public float AgentAttractionRadius { get; set; } = 275f;
        public float AgentAttractionForce { get; set; } = 30f;
        public float AgentRepulsionRadius { get; set; } = 100f;
        public float AgentRepulsionForce { get; set; } = -90f;

        public System.Func<Vector2, Agent> AgentFactory { get; set; }

        private readonly List<Agent> _agents = new();
        public IReadOnlyList<Agent> Agents => _agents;

        private float _agentSpawnTimer;

        public Tank(AnimatedSprite sprite, Vector2 position)
            : base(sprite, position, hp: 50)
        {
            CurrentSpeed = 0f;
            _fsm = new TankFSM(this);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            UpdateSpawning(gameTime);
        }

        private void UpdateSpawning(GameTime gameTime)
        {
            if (IsDead || !CanSpawn)
                return;

            if (MaxAgents <= 0 || _agents.Count >= MaxAgents)
                return;

            if (AgentFactory == null)
                return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _agentSpawnTimer -= dt;
            if (_agentSpawnTimer > 0f)
                return;

            _agentSpawnTimer = AgentSpawnIntervalSeconds > 0f
                ? AgentSpawnIntervalSeconds
                : 0f;

            Agent agent = AgentFactory(Position);
            if (agent == null)
                return;

            _agents.Add(agent);
        }

        public void UpdateWithFSM(
            GameTime gameTime,
            Vector2 playerPosition,
            bool playerIsDead,
            AgentConfig agentConfig,
            List<ForceSource> sharedForceSources)
        {
            _fsm.Update(playerPosition, playerIsDead, gameTime);

            CanSpawn = _fsm.ShouldSpawnAgents;
            Update(gameTime);

            agentConfig.AgentSpeed = _fsm.FlockSpeed;
            UpdateAgentCenters();

            sharedForceSources.Clear();

            if (_fsm.ApplyPlayerForce)
                sharedForceSources.Add(new ForceSource(
                    playerPosition,
                    _fsm.PlayerForceRadius,
                    _fsm.PlayerForceStrength));

            AddAgentForceSources(sharedForceSources);

            if (_fsm.ShouldFlockAttackPlayer)
            {
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                foreach (var agent in _agents)
                    agent.MoveToward(playerPosition, dt, _fsm.FlockSpeed);
            }

            Agent.Process(new List<Agent>(_agents), agentConfig, sharedForceSources);

            foreach (var agent in _agents)
                agent.Update(gameTime);
        }

        /// <summary>
        /// Checks agents against the player position. Agents that hit
        /// are removed and the method returns total damage dealt.
        /// </summary>
        public int ProcessAgentHits(Vector2 playerPosition, float playerRadius)
        {
            int totalDamage = 0;
            float hitDistSq = (AgentHitRadius + playerRadius) * (AgentHitRadius + playerRadius);

            for (int i = _agents.Count - 1; i >= 0; i--)
            {
                float distSq = Vector2.DistanceSquared(_agents[i].Position, playerPosition);
                if (distSq <= hitDistSq)
                {
                    totalDamage += AgentDamage;
                    _agents.RemoveAt(i);
                }
            }

            return totalDamage;
        }

        public void UpdateAgentCenters()
        {
            for (int i = 0; i < _agents.Count; i++)
                _agents[i].Center = Position;
        }

        public void AddAgentForceSources(List<ForceSource> sources)
        {
            if (sources == null || IsDead)
                return;

            if (AgentAttractionRadius > 0f && AgentAttractionForce != 0f)
                sources.Add(new ForceSource(Position, AgentAttractionRadius, AgentAttractionForce));

            if (AgentRepulsionRadius > 0f && AgentRepulsionForce != 0f)
                sources.Add(new ForceSource(Position, AgentRepulsionRadius, AgentRepulsionForce));
        }

        public void DrawWithAgents(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (!IsDead)
                Draw(gameTime, spriteBatch);

            foreach (var agent in _agents)
                agent.Draw(gameTime, spriteBatch);
        }

        public bool RemoveAgent(Agent agent) => _agents.Remove(agent);
        public void ClearAgents() => _agents.Clear();
    }
}
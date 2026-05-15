using System.Collections.Generic;
using MainEngine.Entities;
using MainEngine.FlockEnemy;
using MainEngine.Graphics;
using Microsoft.Xna.Framework;

namespace LILITH.Core.Enemies.Tank
{
    public class Tank : Enemy
    {
        public int Damage { get; set; }

        public float AgentSpawnIntervalSeconds { get; set; } = 2f;
        public int MaxAgents { get; set; } = 15;

        /// <summary>
        /// Set by the FSM each frame to allow or block spawning.
        /// </summary>
        public bool CanSpawn { get; set; }

        public float AgentAttractionRadius { get; set; } = 275f;
        public float AgentAttractionForce { get; set; } = 30f;
        public float AgentRepulsionRadius { get; set; } = 100f;
        public float AgentRepulsionForce { get; set; } = -90f;

        public System.Func<Vector2, Agent> AgentFactory { get; set; }

        private readonly List<Agent> _agents = new();
        public IReadOnlyList<Agent> Agents => _agents;

        private float _agentSpawnTimer;

        public Tank(AnimatedSprite sprite, Vector2 position)
            : base(sprite, position, hp: 200)
        {
            CurrentSpeed = 0f;
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

        public bool RemoveAgent(Agent agent) => _agents.Remove(agent);
        public void ClearAgents() => _agents.Clear();
    }
}
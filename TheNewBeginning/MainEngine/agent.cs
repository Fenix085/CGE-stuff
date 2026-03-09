using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MainEngine;

public class AgentConfig
{
    public float AgentSpeed { get; set; } //determines the minimum speed of an agent; an agent that does not experience any influence will move with this speed.
    public float RepulsionRadius { get; set; }
    public float AlignmentRadius { get; set; }
    public float AttractionRadius { get; set; }//Repulsion, alignment and attraction zone determine the radii of the aforementioned zones. These add up to each other.
    public float AttractionAngle { get; set; } //determines the angle of an agents field of view in degrees.
    public float RepulsionForce { get; set; }
    public float AlignmentForce { get; set; }
    public float AttractionForce { get; set; } //The forces determine the magnitude of influences. For example, if an agent has another agent in its repulsion zone, its velocity will move away from the other agent with a magnitude of Repulsion force. The force is added every time an agent is encountered, so the more agents, the more force is added.
    public float GravitationForce { get; set; } //determines the force that will be added in the direction of the view center. This makes sure all agents move towards the center, which makes larger groups of agents turn as a whole.

}

public class Agent
{
    private const float Length = 16f;
    private const float Width = 14f;
    private const float WrapRadius = 16f;

    public Vector2 Position;
    public Vector2 Center;
    public Vector2 Velocity;

    private readonly List<Vector2> _neighborRepulsion = new();
    private readonly List<Vector2> _neighborAlignment = new();
    private readonly List<Vector2> _neighborAttraction = new();

    private static readonly Random s_random = new();

    public Agent(Vector2 position)
        : this(position, AngleToVector((float)(s_random.NextDouble() * Math.PI * 2))) { }

    public Agent(Vector2 position, Vector2 velocity)
    {
        Position = position;
        Center = position;
        Velocity = velocity;
    }

    public static void Process(List<Agent> agents, AgentConfig config)
    {
        for (int first = 0; first < agents.Count; first++)
        {
            for (int second = first + 1; second < agents.Count; second ++)
                Interact(agents[first], agents[second], config);

            agents[first].React(config);
        }
    }

    public void Scatter(float width, float height)
    {
        Position = new Vector2((float)(s_random.NextDouble() * width), (float)(s_random.NextDouble() * height));
        Velocity = AngleToVector((float)(s_random.NextDouble() * Math.PI * 2));
    }

    private static void Interact(Agent first, Agent second, AgentConfig config)
    {
        Vector2 delta = second.Position - first.Position;
        float squaredDistance = Vector2.Dot(delta, delta);

        if (squaredDistance > config.AttractionRadius * config.AttractionRadius)
            return;

        float distance = (float)Math.Sqrt(squaredDistance);

        if (distance < config.RepulsionRadius)
        {
            float strength = 1f - distance / config.RepulsionRadius;
            Vector2 repulsion = Normalize(delta) * strength;

            first._neighborRepulsion.Add(repulsion);
            second._neighborRepulsion.Add(-repulsion);
        }
        else if (distance < config.AlignmentRadius)
        {
            float strength = (distance - config.RepulsionRadius) / (config.AlignmentRadius - config.RepulsionRadius);

            first._neighborAlignment.Add(Normalize(second.Velocity));
            second._neighborAlignment.Add(Normalize(first.Velocity));
        }
        else
        {
            Vector2 normalizedDelta = Normalize(delta);
            float strength = 1f - (distance - config.AlignmentRadius) / (config.AttractionRadius - config.AlignmentRadius);
            Vector2 attraction = normalizedDelta * strength;

            float dotFirst = Vector2.Dot(Normalize(first.Velocity), normalizedDelta);
            if (MathF.Acos(MathHelper.Clamp(dotFirst, -1f, 1f)) < config.AttractionAngle)
                first._neighborAttraction.Add(attraction);

            float dotSecond = Vector2.Dot(Normalize(second.Velocity), -normalizedDelta);
            if (MathF.Acos(MathHelper.Clamp(dotSecond, -1f, 1f)) < config.AttractionAngle)
                second._neighborAttraction.Add(-attraction);
        }
    }

    private void ApplyRepulsion(AgentConfig config)
    {
        if (_neighborRepulsion.Count == 0)
            return;
        
        Vector2 repulsion = Vector2.Zero;
        foreach (var r in _neighborRepulsion)
            repulsion += r;
        _neighborRepulsion.Clear();

        Velocity += repulsion * config.RepulsionForce;
    }

    private void ApplyAlignment(AgentConfig config)
    {
        if (_neighborAlignment.Count == 0)
            return;

        Vector2 alignment = Vector2.Zero;
        foreach (var a in _neighborAlignment)
            alignment += a;
        _neighborAlignment.Clear();

        Velocity += alignment * config.AlignmentForce;
    }
    private void ApplyAttraction(AgentConfig config)
    {
        if (_neighborAttraction.Count == 0)
            return;

        Vector2 attraction = Vector2.Zero;
        foreach (var a in _neighborAttraction)
            attraction += a;
        _neighborAttraction.Clear();

        Velocity += attraction * config.AttractionForce;
    }

    private void ApplyGravity(AgentConfig config)
    {
        Velocity += -Normalize(Position - Center) * config.GravitationForce;
    }

    private void React(AgentConfig config)
    {
        Velocity = Normalize(Velocity) * config.AgentSpeed;

        ApplyRepulsion(config);
        ApplyAlignment(config);
        ApplyAttraction(config);
        ApplyGravity(config);
    }

    public void Update(SpriteBatch spriteBatch, float timeStep, float width, float height)
    {
        Move(timeStep, width, height);
    }

    private void Move(float timeStep, float width, float height)
    {
        Position += Velocity * timeStep;
    }
    private static Vector2 Normalize(Vector2 v)
    {
        float len = v.Length();
        return len > 0 ? v / len : Vector2.Zero;
    }

    private static Vector2 AngleToVector(float angle)
    {
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
    }
}
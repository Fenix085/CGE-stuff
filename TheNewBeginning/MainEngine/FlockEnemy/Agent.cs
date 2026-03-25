using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MainEngine.Graphics;

namespace MainEngine.FlockEnemy;

/// A positional force that influences agents within its radius.
/// Negative force repels agents away; positive force attracts them toward it.
public struct ForceSource
{
    public Vector2 Position;
    public float Radius;
    public float Force;

    public ForceSource(Vector2 position, float radius, float force)
    {
        Position = position;
        Radius = radius;
        Force = force;
    }
}

public class AgentConfig
{
    public float AgentSpeed { get; set; } //determines the minimum speed of an agent; an agent that does not experience any influence will move with this speed.
    public float RepulsionRadius { get; set; }
    public float AlignmentRadius { get; set; }
    public float AttractionRadius { get; set; }//Repulsion, alignment and attraction zone determine the radii of the aforementioned zones.
    public float AttractionAngle { get; set; } //determines the angle of an agents field of view in degrees.
    public float RepulsionForce { get; set; }
    public float AlignmentForce { get; set; }
    public float AttractionForce { get; set; } //The forces determine the magnitude of influences. For example, if an agent has another agent in its repulsion zone, its velocity will move away from the other agent with a magnitude of Repulsion force. The force is added every time an agent is encountered, so the more agents, the more force is added.
    public float GravitationForce { get; set; } //determines the force that will be added in the direction of the view center. This makes sure all agents move towards the center, which makes larger groups of agents turn as a whole.
    public bool DebugVisible { get; set; } = false; // when true, draws zone radii and velocity vectors
    public bool DebugShowRegions { get; set; } = true; // draw repulsion/alignment/attraction circles
    public bool DebugShowVectors { get; set; } = true; // draw velocity vector
}

public class Agent : Sprite
{
    public Vector2 Center;
    public Vector2 Velocity;

    private readonly List<Vector2> _neighborRepulsion = new();
    private readonly List<Vector2> _neighborAlignment = new();
    private readonly List<Vector2> _neighborAttraction = new();

    private static readonly Random s_random = new();

    public Agent(Vector2 position)
        : this(position, AngleToVector((float)(s_random.NextDouble() * Math.PI * 2))) { }

    public Agent(Vector2 position, Vector2 velocity) : base()
    {
        Position = position;
        Center = position;
        Velocity = velocity;
    }

    public Agent(TextureRegion textureRegion, Vector2 startPosition) : base(textureRegion)
    {
        Position = startPosition;
        Center = startPosition;
        Velocity = Vector2.UnitX;
        CenterOrigin();
    }

    public static void Process(List<Agent> agents, AgentConfig config, List<ForceSource> forceSources = null)
    {
        for (int first = 0; first < agents.Count; first++)
        {
            if (forceSources != null)
            {
                for (int i = 0; i < forceSources.Count; i++)
                    agents[first].ApplyForceSource(forceSources[i]);
            }

            for (int second = first + 1; second < agents.Count; second++)
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
        foreach (var repulsionVector in _neighborRepulsion)
            repulsion += repulsionVector;
        _neighborRepulsion.Clear();

        Velocity -= repulsion * config.RepulsionForce;
    }

    private void ApplyAlignment(AgentConfig config)
    {
        if (_neighborAlignment.Count == 0)
            return;

        Vector2 alignment = Vector2.Zero;
        foreach (var alignmentVector in _neighborAlignment)
            alignment += alignmentVector;
        _neighborAlignment.Clear();

        Velocity += alignment * config.AlignmentForce;
    }

    private void ApplyAttraction(AgentConfig config)
    {
        if (_neighborAttraction.Count == 0)
            return;

        Vector2 attraction = Vector2.Zero;
        foreach (var attractionVector in _neighborAttraction)
            attraction += attractionVector;
        _neighborAttraction.Clear();

        Velocity += attraction * config.AttractionForce;
    }

    private void ApplyGravity(AgentConfig config)
    {
        Velocity += -Normalize(Position - Center) * config.GravitationForce;
    }

    private void ApplyForceSource(ForceSource source)
    {
        if (source.Radius <= 0f)
            return;

        Vector2 delta = Position - source.Position;
        float distance = delta.Length();

        if (distance >= source.Radius || distance < 0.001f)
            return;

        float strength = 1f - distance / source.Radius;
        Velocity -= Normalize(delta) * strength * source.Force;
    }

    private void React(AgentConfig config, List<ForceSource> sources = null)
    {
        Velocity = Normalize(Velocity) * config.AgentSpeed;

        ApplyRepulsion(config);
        ApplyAlignment(config);
        ApplyAttraction(config);
        ApplyGravity(config);

        if (sources != null)
        {
            foreach (var source in sources)
                ApplyForceSource(source);
        }
    }
    
    public float CurrentSpeed { get; private set; }

    public void MoveToward(Vector2 targetPosition, float dt, float activeSpeed)
    {
        CurrentSpeed = activeSpeed;

        Vector2 toTarget = targetPosition - Position;
        float distance = toTarget.Length();

        if (distance <= float.Epsilon)
        {
            Velocity = Vector2.Zero;
            return;
        }

        Vector2 direction = toTarget / distance;
        Velocity = direction * CurrentSpeed;
        Position += Velocity * dt;
    }

    public override void ApplyDeath()
    {
        // Agent death logic here
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Move(dt);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (Region == null)
            return;

        if (Velocity != Vector2.Zero)
            Rotation = MathF.Atan2(Velocity.Y, Velocity.X);

        base.Draw(gameTime, spriteBatch);
    }

    public void DrawDebug(SpriteBatch spriteBatch, AgentConfig config)
    {
        if (!config.DebugVisible)
            return;

        EnsureDebugTexture(spriteBatch.GraphicsDevice);

        if (config.DebugShowRegions)
        {
            DrawCircle(spriteBatch, Position, config.RepulsionRadius, Color.Red * 0.5f);
            DrawCircle(spriteBatch, Position, config.AlignmentRadius, Color.Yellow * 0.5f);
            DrawCircle(spriteBatch, Position, config.AttractionRadius, Color.Green * 0.5f);
        }

        if (config.DebugShowVectors)
        {
            Vector2 direction = Normalize(Velocity);
            DrawLine(spriteBatch, Position, Position + direction * 30f, Color.Cyan);
        }
    }

    public static void DrawDebugForceSources(SpriteBatch spriteBatch, List<ForceSource> sources)
    {
        if (sources == null)
            return;

        EnsureDebugTexture(spriteBatch.GraphicsDevice);

        foreach (var source in sources)
        {
            Color color = source.Force > 0 ? Color.Blue * 0.4f : Color.Red * 0.4f;
            DrawCircle(spriteBatch, source.Position, source.Radius, color);
        }
    }

    private static Texture2D s_debugPixel;

    private static void EnsureDebugTexture(GraphicsDevice graphicsDevice)
    {
        if (s_debugPixel != null)
            return;

        s_debugPixel = new Texture2D(graphicsDevice, 1, 1);
        s_debugPixel.SetData(new[] { Color.White });
    }

    private static void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness = 1f)
    {
        Vector2 delta = end - start;
        float length = delta.Length();
        float angle = MathF.Atan2(delta.Y, delta.X);

        spriteBatch.Draw(s_debugPixel, start, null, color, angle, Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0f);
    }

    private static void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, int segments = 32)
    {
        float step = MathF.PI * 2f / segments;
        Vector2 prev = center + new Vector2(radius, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = step * i;
            Vector2 next = center + new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
            DrawLine(spriteBatch, prev, next, color);
            prev = next;
        }
    }

    private void Move(float timeStep)
    {
        Position += Velocity * timeStep;
    }

    private static Vector2 Normalize(Vector2 vector)
    {
        float len = vector.Length();
        return len > 0 ? vector / len : Vector2.Zero;
    }

    private static Vector2 AngleToVector(float angle)
    {
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
    }
}
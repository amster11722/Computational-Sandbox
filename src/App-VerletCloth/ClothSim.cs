using System.Numerics;
using Core;
using Raylib_cs;

public class ClothSim : Simulation
{
    int screenWidth = 500;
    int screenHeight = 500;
    Vector2 prevMousePos;

    int clothWidth = 800;
    int clothRes = 150;

    float timeSinceReset = 10f;

    Random random = new Random();

    public class Particle
    {
        public Vector2 position;
        public Vector2 prevPosition;
        public bool pinned;
        public Connection? Right;
        public Connection? Down;

        public Particle(float x, float y, bool pin)
        {
            position = new Vector2(x, y);
            prevPosition = new Vector2(x, y);
            pinned = pin;
        }
    }

    public class Connection
    {
        public Particle A;
        public Particle B;
        public float restLength;
        public bool isActive = true;

        public Connection(Particle a, Particle b, float rest)
        {
            A = a;
            B = b;
            restLength = rest;
        }
    }

    public List<Particle> particles = new List<Particle>();
    public List<Connection> connections = new List<Connection>();
    public List<Connection>[] batchedConnections = new List<Connection>[4];

    static void Main()
    {
        // Set up window
        ClothSim clothSim = new ClothSim();
        clothSim.Run(500, 500, "Cloth Simulation");
    }

    protected override void Initialize()
    {
        // Set fullscreen
        int monitor = Raylib.GetCurrentMonitor();
        screenWidth = Raylib.GetMonitorWidth(monitor);
        screenHeight = Raylib.GetMonitorHeight(monitor);

        Raylib.SetWindowSize(screenWidth, screenHeight);
        Raylib.ToggleFullscreen();

        // Initialize particles and connections

        for (int b = 0; b < 4; b++)
        {
            batchedConnections[b] = new List<Connection>();
        }

        for (int i = 0; i < clothRes; i++)
        {
            for (int j = 0; j < clothRes; j++)
            {
                particles.Add(new Particle((float)random.NextDouble() + i * (clothWidth / clothRes) + (screenWidth - clothWidth) / 2f, (float)random.NextDouble() + j * (clothWidth / clothRes) + 50, j == 0));
                if (j > 0)
                {
                    Particle topParticle = particles[particles.Count - 2];
                    Particle bottomParticle = particles[particles.Count - 1];
                    Connection newConn = new Connection(topParticle, bottomParticle, clothWidth / clothRes);
                    if (j % 2 == 0)
                        batchedConnections[0].Add(newConn);
                    else
                        batchedConnections[1].Add(newConn);

                    connections.Add(newConn);
                    topParticle.Down = newConn;
                }
                if (i > 0)
                {
                    Particle leftParticle = particles[particles.Count - 1 - clothRes];
                    Particle rightParticle = particles[particles.Count - 1];
                    Connection newConn = new Connection(leftParticle, rightParticle, clothWidth / clothRes);
                    if (i % 2 == 0)
                        batchedConnections[2].Add(newConn);
                    else
                        batchedConnections[3].Add(newConn);

                    connections.Add(newConn);
                    leftParticle.Right = newConn;
                }
            }
        }

        // Engine warmup to prevent initial chaos
        SettleCloth(150);
    }

    protected override void Update(float deltaTime)
    {

        if (deltaTime > 0.02f) deltaTime = 0.016f; // prevents lag spikes from invoking chaos

        timeSinceReset += deltaTime;

        int gravity = 600;

        // Apply verlet integration
        for (int i = 0; i < particles.Count; i++)
        {
            if (!particles[i].pinned)
            {
                Vector2 prev = new Vector2(particles[i].position.X, particles[i].position.Y);
                particles[i].position.X += (particles[i].position.X - particles[i].prevPosition.X) * 0.999f;
                particles[i].position.Y += (particles[i].position.Y - particles[i].prevPosition.Y + gravity * deltaTime * deltaTime) * 0.999f;
                particles[i].prevPosition = prev;
            }
        }

        // Apply connection constraints in parallel via multi-stage relaxation
        for (int i = 0; i < 15; i++)
        {
            CorrectConstraints();
        }

        // Cloth interaction
        Vector2 mousePos = Raylib.GetMousePosition();
        Vector2 mouseDelta = mousePos - prevMousePos;
        if (Raylib.IsMouseButtonDown(MouseButton.Right))
        {
            float dragRadius = 50f; // Distance around the cursor to influence nodes

            foreach (Particle particle in particles)
            {
                float dist = Vector2.Distance(particle.position, mousePos);
                if (!particle.pinned && dist < dragRadius)
                {
                    // Calculate a falloff factor so particles closer to the cursor 
                    // track the mouse perfectly, while outer particles stretch smoothly
                    float influence = (dragRadius - dist) / dragRadius;

                    // Move the particle along with the mouse movement vector
                    particle.position += mouseDelta * influence;
                }
            }
        }
        if (Raylib.IsMouseButtonDown(MouseButton.Left))
        {
            foreach (Connection connection in connections)
            {
                Vector2 r = connection.B.position - connection.A.position;
                Vector2 s = mousePos - prevMousePos;
                float denom = r.X * s.Y - r.Y * s.X;
                if (denom != 0)
                {
                    float numT = (prevMousePos.X - connection.A.position.X) * s.Y - (prevMousePos.Y - connection.A.position.Y) * s.X;
                    float numU = (prevMousePos.X - connection.A.position.X) * r.Y - (prevMousePos.Y - connection.A.position.Y) * r.X;

                    float t = numT / denom;
                    float u = numU / denom;

                    if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
                    {
                        connection.isActive = false;
                    }
                }
            }
        }
        prevMousePos = mousePos;
        if (Raylib.IsKeyPressed(KeyboardKey.R))
        {
            timeSinceReset = 0f;
            foreach (Connection connection in connections)
            {
                connection.isActive = true;
            }
            SettleCloth(300);
        }
        if (Raylib.IsKeyDown(KeyboardKey.S))
        {
            SettleCloth(100);
        }
    }

    void SettleCloth(int steps)
    {
        float fixedDt = 0.016f;
        int gravity = 600;

        for (int step = 0; step < steps; step++)
        {
            for (int i = 0; i < particles.Count; i++)
            {
                if (!particles[i].pinned)
                {
                    Vector2 currentPos = particles[i].position;

                    float velX = (particles[i].position.X - particles[i].prevPosition.X) * 0.98f;
                    float velY = (particles[i].position.Y - particles[i].prevPosition.Y) * 0.98f;

                    particles[i].position.X += velX;
                    particles[i].position.Y += velY + gravity * fixedDt * fixedDt;
                    particles[i].prevPosition = currentPos;
                }
            }

            for (int c = 0; c < 15; c++)
            {
                CorrectConstraints();
            }
        }
    }

    public void CorrectConstraints()
    {
        // Pulls constraints back to their resting length. Runs parallel in 4 batches for efficiency
        for (int b = 0; b < 4; b++)
        {
            Parallel.ForEach(batchedConnections[b], connection =>
            {
                if (!((connection.A.pinned && connection.B.pinned) || !connection.isActive))
                {
                    Vector2 error = new Vector2(connection.A.position.X - connection.B.position.X, connection.A.position.Y - connection.B.position.Y);
                    float distance = Vector2.Distance(connection.A.position, connection.B.position);
                    if (timeSinceReset > 3f && distance > connection.restLength * 6f)
                    {
                        connection.isActive = false;
                    }
                    else
                    {
                        float difference = connection.restLength - distance;
                        error = Vector2.Normalize(error);
                        error *= difference;
                        if (connection.A.pinned)
                            connection.B.position -= error;
                        else if (connection.B.pinned)
                            connection.A.position += error;
                        else
                        {
                            connection.B.position -= error / 2f;
                            connection.A.position += error / 2f;
                        }
                    }
                }
            });
        }
    }

    protected override void Draw()
    {
        Raylib.ClearBackground(new Color(20, 20, 15, 255));

        // Draw mesh sheet rather than individual particles
        for (int i = 0; i < clothRes - 1; i++)
        {
            for (int j = 0; j < clothRes - 1; j++)
            {
                Particle TL = particles[i * clothRes + j];
                Particle TR = particles[(i + 1) * clothRes + j];
                Particle BL = particles[i * clothRes + j + 1];
                Particle BR = particles[(i + 1) * clothRes + j + 1];

                // Orientation-based diffuse shading

                Vector2 surfaceEdge1 = TR.position - TL.position;
                Vector2 surfaceEdge2 = BL.position - TL.position;

                float faceAngle = MathF.Atan2(surfaceEdge1.Y + surfaceEdge2.Y, surfaceEdge1.X + surfaceEdge2.X);
                Vector2 surfaceNormal = new Vector2(-MathF.Sin(faceAngle), MathF.Cos(faceAngle));

                Vector2 faceCenter = (TL.position + TR.position + BL.position) / 3f;
                Vector2 lightDirection = Vector2.Normalize(Vector2.Zero - faceCenter);

                float diffuse = Vector2.Dot(surfaceNormal, lightDirection);
                diffuse = MathF.Max(0.1f, (diffuse + 1.0f) * 0.5f); // Remap from [-1, 1] to [0.1, 1.0]

                float speed = (TL.position - TL.prevPosition).Length();
                byte r = (byte)Math.Clamp(30 + (diffuse * 120) + (speed * 10), 0, 255);
                byte g = (byte)Math.Clamp(40 + (diffuse * 160) + (speed * 15), 0, 255);
                byte b = (byte)Math.Clamp(70 + (diffuse * 220), 0, 255);

                Color fabricColor = new Color(r, g, b, (byte)255);

                bool topEdgeActive = TL.Right != null && TL.Right.isActive;
                bool leftEdgeActive = TL.Down != null && TL.Down.isActive;
                bool rightEdgeActive = TR.Down != null && TR.Down.isActive;
                bool bottomEdgeActive = BL.Right != null && BL.Right.isActive;

                float restLen = (float)clothWidth / clothRes;


                // Only draw the first triangle if the top and right boundaries are intact
                if (topEdgeActive && rightEdgeActive)
                {
                    Raylib.DrawTriangle(BR.position, TR.position, TL.position, fabricColor);
                    Raylib.DrawTriangle(TL.position, TR.position, BR.position, fabricColor);
                }

                // Only draw the second triangle if the bottom and left boundaries are intact
                if (bottomEdgeActive && leftEdgeActive)
                {
                    Raylib.DrawTriangle(TL.position, BL.position, BR.position, fabricColor);
                    Raylib.DrawTriangle(BR.position, BL.position, TL.position, fabricColor);
                }
            }
        }
    }

    protected override string AddDebugData()
    {
        int activeConnections = 0;
        foreach (var c in connections) if (c.isActive) activeConnections++;

        return $"[SYSTEM]\n" +
               $"FPS             : {Raylib.GetFPS()}\n" +
               $"Thread Model    : C# Parallel.ForEach (4-Stage Relaxation)\n\n" +
               $"[VERLET SHEET METRICS]\n" +
               $"Mesh Resolution : {clothRes}x{clothRes}\n" +
               $"Total Nodes     : {particles.Count:N0} vertices\n" +
               $"Structural Links: {activeConnections:N0} active / {connections.Count:N0} total\n\n" +
               $"[CLOTH CONTROLS]\n" +
               $"L-Click + Drag  : Razor / Slice Structural Connections\n" +
               $"R-Click + Drag  : Drag Cloth\n" +
               $"Key [S]         : Settle Cloth\n" +
               $"Key [R]         : Mend Sheet / Restore Connections";
    }
}
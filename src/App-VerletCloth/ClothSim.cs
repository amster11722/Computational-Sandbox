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

    Random random = new Random();

    public class Particle
    {
        public Vector2 position;
        public Vector2 prevPosition;
        public bool pinned;
        public Connection Right;
        public Connection Down;

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

        // Initiliaze particles and connections

        for (int b = 0; b < 4; b++)
        {
            batchedConnections[b] = new List<Connection>();
        }

        for (int i = 0; i < clothRes; i++)
        {
            for (int j = 0; j < clothRes; j++)
            {
                particles.Add(new Particle((float)random.NextDouble() + i * (clothWidth / clothRes) + 550, (float)random.NextDouble() + j * (clothWidth / clothRes) + 50, j == 0));
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
        float fixedDt = 0.016f;
        int gravity = 600;

        for (int step = 0; step < 150; step++)
        {
            for (int i = 0; i < particles.Count; i++)
            {
                if (!particles[i].pinned)
                {
                    Vector2 currentPos = particles[i].position;

                    // Apply drag damping (0.98f) so the initial drop energy actively drains out
                    float velX = (particles[i].position.X - particles[i].prevPosition.X) * 0.98f;
                    float velY = (particles[i].position.Y - particles[i].prevPosition.Y) * 0.98f;

                    particles[i].position.X += velX;
                    particles[i].position.Y += velY + gravity * fixedDt * fixedDt;
                    particles[i].prevPosition = currentPos;
                }
            }

            for (int c = 0; c < 15; c++)
            {
                correctConstraints();
            }
        }
    }

    protected override void Update(float deltaTime)
    {

        if (deltaTime > 0.02f) deltaTime = 0.016f; // prevents lag spikes from invoking chaos

        int a = 600; // gravity

        // Apply verlet integration
        for (int i = 0; i < particles.Count; i++)
        {
            if (!particles[i].pinned)
            {
                Vector2 prev = new Vector2(particles[i].position.X, particles[i].position.Y);
                particles[i].position.X += particles[i].position.X - particles[i].prevPosition.X;
                particles[i].position.Y += particles[i].position.Y - particles[i].prevPosition.Y + a * deltaTime * deltaTime;
                particles[i].prevPosition = prev;
            }
        }

        // Apply connection contraints in parallel via multi-stage relaxation
        for (int i = 0; i < 15; i++)
        {
            correctConstraints();
        }

        // Cloth cutting
        Vector2 mousePos = Raylib.GetMousePosition();
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
    }

    public void correctConstraints()
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

                bool topEdgeActive = TL.Right != null && TL.Right.isActive;
                bool leftEdgeActive = TL.Down != null && TL.Down.isActive;
                bool rightEdgeActive = TR.Down != null && TR.Down.isActive;
                bool bottomEdgeActive = BL.Right != null && BL.Right.isActive;

                float restLen = (float)clothWidth / clothRes;

                float strainScale = 180f; // Amplifies structural tension
                float speedScale = 40f;   // Amplifies kinetic motion brightness

                // Only draw the first triangle if the top and right boundaries are intact
                if (topEdgeActive && rightEdgeActive)
                {
                    float strainTop = Math.Abs(Vector2.Distance(TL.position, TR.position) - restLen);
                    float strainRight = Math.Abs(Vector2.Distance(TR.position, BR.position) - restLen);

                    float speedTL = (TL.position - TL.prevPosition).Length();
                    float speedTR = (TR.position - TR.prevPosition).Length();
                    float speedBR = (BR.position - BR.prevPosition).Length();
                    float avgSpeed = (speedTL + speedTR + speedBR) / 3f;

                    int r = (int)(strainRight * strainScale + avgSpeed * speedScale * 1.5f);
                    int g = (int)(avgSpeed * speedScale * 2.0f); // Spikes hard green during cuts
                    int b = (int)(130 - (strainTop * strainScale) + avgSpeed * speedScale * 1.2f);

                    Color color = new Color(
                        (byte)Math.Clamp(r, 25, 255),
                        (byte)Math.Clamp(g, 20, 255),
                        (byte)Math.Clamp(b, 45, 255),
                        (byte)255
                    );

                    Raylib.DrawTriangle(BR.position, TR.position, TL.position, color);
                    Raylib.DrawTriangle(TL.position, TR.position, BR.position, color);
                }

                // Only draw the second triangle if the bottom and left boundaries are intact
                if (bottomEdgeActive && leftEdgeActive)
                {
                    float strainLeft = Math.Abs(Vector2.Distance(TL.position, BL.position) - restLen);
                    float strainBottom = Math.Abs(Vector2.Distance(BL.position, BR.position) - restLen);

                    float speedTL = (TL.position - TL.prevPosition).Length();
                    float speedBL = (BL.position - BL.prevPosition).Length();
                    float speedBR = (BR.position - BR.prevPosition).Length();
                    float avgSpeed = (speedTL + speedBL + speedBR) / 3f;

                    int r = (int)(strainLeft * strainScale + avgSpeed * speedScale * 1.5f);
                    int g = (int)(avgSpeed * speedScale * 2.0f);
                    int b = (int)(130 - (strainBottom * strainScale) + avgSpeed * speedScale * 1.2f);

                    Color color = new Color(
                        (byte)Math.Clamp(r, 25, 255),
                        (byte)Math.Clamp(g, 20, 255),
                        (byte)Math.Clamp(b, 45, 255),
                        (byte)255
                    );

                    Raylib.DrawTriangle(TL.position, BL.position, BR.position, color);
                    Raylib.DrawTriangle(BR.position, BL.position, TL.position, color);
                }
            }
        }
    }
}
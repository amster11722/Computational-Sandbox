using System.Numerics;
using Core;
using Raylib_cs;

public class ClothSim : Simulation
{
    int screenWidth = 500;
    int screenHeight = 500;
    Vector2 prevMousePos;

    int clothWidth = 500;
    int clothRes = 40;

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

        for (int i = 0; i < clothRes; i++)
        {
            for (int j = 0; j < clothRes; j++)
            {
                particles.Add(new Particle(i * (clothWidth / clothRes) + 700, j * (clothWidth / clothRes) + 50, j == 0));
                if (j > 0)
                {
                    Particle topParticle = particles[particles.Count - 2];
                    Particle bottomParticle = particles[particles.Count - 1];
                    Connection newConn = new Connection(topParticle, bottomParticle, clothWidth / clothRes);

                    connections.Add(newConn);
                    topParticle.Down = newConn;
                }
                if (i > 0)
                {
                    Particle leftParticle = particles[particles.Count - 1 - clothRes];
                    Particle rightParticle = particles[particles.Count - 1];
                    Connection newConn = new Connection(leftParticle, rightParticle, clothWidth / clothRes);

                    connections.Add(newConn);
                    leftParticle.Right = newConn;
                }
            }
        }
    }

    protected override void Update(float deltaTime)
    {
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

        // Apply connection contraints via multi-stage relaxation
        for (int i = 0; i < 12; i++)
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
        // Pulls constraints back to their resting length
        foreach (Connection connection in connections)
        {
            if ((connection.A.pinned && connection.B.pinned) || !connection.isActive) continue;
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

                // Only draw the first triangle if the top and right boundaries are intact
                if (topEdgeActive && rightEdgeActive)
                {
                    float BRSpeed = (BR.position - BR.prevPosition).Length();
                    float TRSpeed = (TR.position - TR.prevPosition).Length();
                    float TLSpeed = (TL.position - TL.prevPosition).Length();
                    Color color = new Color(BRSpeed, TRSpeed, TLSpeed, 255);
                    Raylib.DrawTriangle(BR.position, TR.position, TL.position, color);
                    Raylib.DrawTriangle(TL.position, TR.position, BR.position, color);
                }

                // Only draw the second triangle if the bottom and left boundaries are intact
                if (bottomEdgeActive && leftEdgeActive)
                {
                    float BRSpeed = (BR.position - BR.prevPosition).Length();
                    float BLSpeed = (BL.position - BL.prevPosition).Length();
                    float TLSpeed = (TL.position - TL.prevPosition).Length();
                    Color color = new Color(BRSpeed, BLSpeed, TLSpeed, 255);
                    Raylib.DrawTriangle(TL.position, BL.position, BR.position, color);
                    Raylib.DrawTriangle(BR.position, BL.position, TL.position, color);
                }
            }
        }

        // foreach (Particle particle in particles)
        // {
        //     Raylib.DrawCircle((int)particle.position.X, (int)particle.position.Y, 10, Color.White);

        // }

        // foreach (Connection connection in connections)
        // {
        //     if (connection.isActive)
        //         Raylib.DrawLine((int)connection.A.position.X, (int)connection.A.position.Y, (int)connection.B.position.X, (int)connection.B.position.Y, Color.White);
        // }
    }
}
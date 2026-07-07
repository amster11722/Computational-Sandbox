using System.Numerics;
using Core;
using Raylib_cs;

public class ClothSim : Simulation
{
    int screenWidth = 500;
    int screenHeight = 500;
    Vector2 prevMousePos;

    public class Particle
    {
        public Vector2 position;
        public Vector2 prevPosition;
        public bool pinned;

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

        for (int i = 0; i < 20; i++)
        {
            for (int j = 0; j < 20; j++)
            {
                particles.Add(new Particle(i * 30 + 700, j * 30 + 50, j == 0));
                if (j > 0)
                    connections.Add(new Connection(particles[particles.Count - 1], particles[particles.Count - 2], 30));
                if (i > 0)
                    connections.Add(new Connection(particles[particles.Count - 1], particles[particles.Count - 21], 30));
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
        for (int i = 0; i < 6; i++)
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

        foreach (Particle particle in particles)
        {
            Raylib.DrawCircle((int)particle.position.X, (int)particle.position.Y, 10, Color.White);
        }

        foreach (Connection connection in connections)
        {
            if(connection.isActive)
                Raylib.DrawLine((int)connection.A.position.X, (int)connection.A.position.Y, (int)connection.B.position.X, (int)connection.B.position.Y, Color.White);
        }
    }
}
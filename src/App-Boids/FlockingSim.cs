using System.Numerics;
using Core;
using Raylib_cs;

public class FlockingSim : Simulation
{

    int screenWidth = 500;
    int screenHeight = 500;
    int gridColumns;
    int gridRows;
    float steerAngle = 0;

    // Variables
    float alignmentWeight = 2;
    float cohesionWeight = 1f;
    float separationWeight = 1.8f;
    float wanderingWeight = 150f;
    float boidPerceptionRadius = 40;
    float maximumBoidSpeed = 250;
    float maximumSteeringForce = 200;

    public enum Preset { Custom, Reef, Ocean, Meteor, Fish, Null }

    Preset currentPreset = Preset.Reef;

    void ApplyPreset(Preset preset)
    {
        if (preset < 0) preset = Preset.Custom;
        if (preset == Preset.Custom || preset == Preset.Null) return;
        currentPreset = preset;
        switch (preset)
        {
            case Preset.Custom:
                break;
            case Preset.Reef:
                alignmentWeight = 2; cohesionWeight = 1; separationWeight = 1.8f; maximumBoidSpeed = 250; maximumSteeringForce = 200;
                break;
            case Preset.Ocean:
                alignmentWeight = 0.3f; cohesionWeight = 1.5f; separationWeight = 1.9f; maximumBoidSpeed = 250; maximumSteeringForce = 200;
                break;
            case Preset.Meteor:
                alignmentWeight = 2.85f; cohesionWeight = 1.4f; separationWeight = 0.6f; maximumBoidSpeed = 380; maximumSteeringForce = 145;
                break;
            case Preset.Fish:
                alignmentWeight = 3.1f; cohesionWeight = 1.7f; separationWeight = 2.8f; maximumBoidSpeed = 65; maximumSteeringForce = 400;
                break;
        }
    }

    public string presetName(Preset preset)
    {
        switch (preset)
        {
            case Preset.Custom:
                return "Custom";
            case Preset.Reef:
                return "Reef";
            case Preset.Ocean:
                return "Ocean";
            case Preset.Meteor:
                return "Meteors";
            case Preset.Fish:
                return "Fish";
        }
        return "N/A";
    }

    // Basic boid class for a single entity
    class Boid
    {
        public Vector2 position;
        public Vector2 velocity;
        public int neighbors;
        public Boid(int x, int y, Vector2 vel)
        {
            position = new Vector2(x, y);
            velocity = vel;
            neighbors = 0;
        }
    }

    // List containing all current boid entities
    List<Boid> boids = new List<Boid>();

    // Hash grid allocation dictionary for boids
    Dictionary<int, List<Boid>> spatialGrid = new Dictionary<int, List<Boid>>();

    Random random = new Random();

    static void Main()
    {
        // Set up window
        FlockingSim flockingSim = new FlockingSim();
        flockingSim.Run(500, 500, "Flocking Simulation");
    }

    protected override void Initialize()
    {
        // Set fullscreen
        int monitor = Raylib.GetCurrentMonitor();
        screenWidth = Raylib.GetMonitorWidth(monitor);
        screenHeight = Raylib.GetMonitorHeight(monitor);
        gridColumns = (int)MathF.Ceiling((float)screenWidth / boidPerceptionRadius);
        gridRows = (int)MathF.Ceiling((float)screenHeight / boidPerceptionRadius);

        Raylib.SetWindowSize(screenWidth, screenHeight);
        Raylib.ToggleFullscreen();

        // Init boids
        for (int i = 0; i < 5000; i++)
        {
            boids.Add(new Boid(random.Next(0, screenWidth), random.Next(0, screenHeight), new Vector2(random.Next(-300, 300), random.Next(-300, 300))));
        }

        // Init spatial grid
        for (int i = 0; i < gridColumns; i++)
        {
            for (int j = 0; j < gridRows; j++)
            {
                int hashKey = i + (j * gridColumns);
                spatialGrid.Add(hashKey, new List<Boid>());
            }
        }
    }

    protected override void Update(float deltaTime)
    {
        // Spatial grid data population
        foreach (List<Boid> cellList in spatialGrid.Values)
        {
            cellList.Clear();
        }
        for (int i = 0; i < boids.Count; i++)
        {
            // Place boids into their proper grid
            int cellX = (int)(boids[i].position.X / boidPerceptionRadius);
            int cellY = (int)(boids[i].position.Y / boidPerceptionRadius);
            cellX = Math.Clamp(cellX, 0, gridColumns - 1);
            cellY = Math.Clamp(cellY, 0, gridRows - 1);
            spatialGrid[cellX + cellY * gridColumns].Add(boids[i]);
        }

        // Boid logic
        for (int i = 0; i < boids.Count; i++)
        {
            Vector2 totalVelocity = Vector2.Zero;
            Vector2 totalPositions = Vector2.Zero;
            Vector2 escapeDirection = Vector2.Zero;
            Boid boid = boids[i];
            boid.neighbors = 0;

            // Find neighbors in perception range via spatial grid and collect values
            for (int xOffset = -1; xOffset <= 1; xOffset++)
            {
                for (int yOffset = -1; yOffset <= 1; yOffset++)
                {
                    // check all neighboring cells
                    int cellX = (int)(boid.position.X / boidPerceptionRadius);
                    int cellY = (int)(boid.position.Y / boidPerceptionRadius);
                    int neighborX = cellX + xOffset;
                    int neighborY = cellY + yOffset;

                    if (neighborX < gridColumns && neighborX >= 0)
                    {
                        if (neighborY < gridRows && neighborY >= 0)
                        {
                            int hash = neighborX + neighborY * gridColumns;
                            foreach (Boid other in spatialGrid[hash])
                            {
                                if (other != boid)
                                {
                                    // Check distance of boids
                                    float dist = Vector2.Distance(other.position, boid.position);
                                    if (dist < boidPerceptionRadius)
                                    {
                                        boid.neighbors++;
                                        totalVelocity += other.velocity;
                                        totalPositions += other.position;
                                        Vector2 diff = boid.position - other.position;
                                        if (dist < boidPerceptionRadius && dist > 0.01f)
                                            diff /= dist * dist; // Push harder inversely proportional to distance
                                        escapeDirection += diff;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Vector2 steering = Vector2.Zero;

            // Calculate wandering
            Vector2 rawJitter = new Vector2((float)(random.NextDouble() - 0.5) * 2, (float)(random.NextDouble() - 0.5) * 2);
            Vector2 wanderForce = Vector2.Zero;

            if (rawJitter.Length() > 0)
            {
                // Normalize it so it represents a pure direction, then apply weight
                if(rawJitter.LengthSquared() > 0)
                    wanderForce = Vector2.Normalize(rawJitter) * wanderingWeight;
            }
            steering += wanderForce;

            // Avoid the mouse cursor
            Vector2 mousePos = Raylib.GetMousePosition();
            if (Vector2.Distance(boid.position, mousePos) < 150f && Raylib.IsMouseButtonDown(MouseButton.Left))
            {
                Vector2 inverseDir = boid.position - mousePos;
                Vector2 escape = Vector2.Zero;
                if(inverseDir.LengthSquared() > 0)
                escape = Vector2.Normalize(inverseDir) * maximumBoidSpeed;

                // Forcefully snap 20% of their velocity toward the escape vector instantly this frame
                boid.velocity = Vector2.Lerp(boid.velocity, escape, 0.2f);
            }

            int neighbors = boid.neighbors;

            if (neighbors > 0)
            {
                // Calculate alignment steering
                totalVelocity /= neighbors;
                if(totalVelocity.LengthSquared() > 0)
                    totalVelocity = Vector2.Normalize(totalVelocity) * maximumBoidSpeed;

                // Calculate cohesion steering
                totalPositions /= neighbors;
                Vector2 directionToCenter = totalPositions - boid.position;
                if(directionToCenter.LengthSquared() > 0)
                    directionToCenter = Vector2.Normalize(directionToCenter) * maximumBoidSpeed;

                // Calculate separation steering
                escapeDirection = Vector2.Normalize(escapeDirection) * maximumBoidSpeed;

                Vector2 desiredSteering = (totalVelocity * alignmentWeight) + (directionToCenter * cohesionWeight) + (escapeDirection * separationWeight);
                if (desiredSteering.LengthSquared() > 0)
                {
                    desiredSteering = Vector2.Normalize(desiredSteering) * maximumBoidSpeed;
                    steering += desiredSteering - boid.velocity;
                }
            }

            // Steer the boid
            if (steering.Length() > maximumSteeringForce)
                steering = Vector2.Normalize(steering) * maximumSteeringForce;
            if (Raylib.IsMouseButtonDown(MouseButton.Right))
            {
                steering += new Vector2(MathF.Cos(steerAngle), MathF.Sin(steerAngle)) * 1000;
            }
            boid.velocity += steering * deltaTime;
            if (boid.velocity.Length() > maximumBoidSpeed)
            {
                boid.velocity = Vector2.Normalize(boid.velocity) * maximumBoidSpeed;
            }

            // Update boid position
            boid.position += boid.velocity * deltaTime;

            // Wrap-around logic
            if (boid.position.X < 0) boid.position.X = screenWidth - 1;
            if (boid.position.X > screenWidth) boid.position.X = 1;
            if (boid.position.Y < 0) boid.position.Y = screenHeight - 1;
            if (boid.position.Y > screenHeight) boid.position.Y = 1;
        }
        // Interactivity
        if (Raylib.IsMouseButtonDown(MouseButton.Right))
        {
            steerAngle += 3.0f * deltaTime; // Smoothly rotates 3 radians per second
        }
        if (Raylib.IsKeyDown(KeyboardKey.B))
        {
            Vector2 mousePos = Raylib.GetMousePosition();
            boids.Add(new Boid(
                (int)mousePos.X,
                (int)mousePos.Y,
                new Vector2(random.Next(-300, 300), random.Next(-300, 300))
            ));
        }
        if (Raylib.IsKeyPressed(KeyboardKey.C))
        {
            boids.Clear();
        }
        if (Raylib.IsKeyDown(KeyboardKey.Down)) alignmentWeight = MathF.Max(0f, alignmentWeight - 0.01f);
        if (Raylib.IsKeyDown(KeyboardKey.Up)) alignmentWeight += 0.01f;

        if (Raylib.IsKeyDown(KeyboardKey.Left)) cohesionWeight = MathF.Max(0f, cohesionWeight - 0.01f);
        if (Raylib.IsKeyDown(KeyboardKey.Right)) cohesionWeight += 0.01f;

        if (Raylib.IsKeyDown(KeyboardKey.S)) separationWeight = MathF.Max(0f, separationWeight - 0.01f);
        if (Raylib.IsKeyDown(KeyboardKey.W)) separationWeight += 0.01f;

        if (Raylib.IsKeyDown(KeyboardKey.One)) maximumBoidSpeed = MathF.Max(0f, maximumBoidSpeed - 1f);
        if (Raylib.IsKeyDown(KeyboardKey.Two)) maximumBoidSpeed += 1f;

        if (Raylib.IsKeyDown(KeyboardKey.Three)) maximumSteeringForce = MathF.Max(0f, maximumSteeringForce - 1f);
        if (Raylib.IsKeyDown(KeyboardKey.Four)) maximumSteeringForce += 1f;

        if (Raylib.IsKeyDown(KeyboardKey.Down) || Raylib.IsKeyDown(KeyboardKey.Up) || Raylib.IsKeyDown(KeyboardKey.Left) || Raylib.IsKeyDown(KeyboardKey.Right) || Raylib.IsKeyDown(KeyboardKey.S) || Raylib.IsKeyDown(KeyboardKey.W) || Raylib.IsKeyDown(KeyboardKey.One) || Raylib.IsKeyDown(KeyboardKey.Two) || Raylib.IsKeyDown(KeyboardKey.Three) || Raylib.IsKeyDown(KeyboardKey.Four)) currentPreset = Preset.Custom;

        if (Raylib.IsKeyPressed(KeyboardKey.Minus))
            ApplyPreset(currentPreset - 1);
        if (Raylib.IsKeyPressed(KeyboardKey.Equal))
            ApplyPreset(currentPreset + 1);
    }

    protected override void Draw()
    {
        Raylib.DrawRectangle(0, 0, screenWidth, screenHeight, new Color(20, 20, 20, 30));
        foreach (Boid boid in boids)
        {
            // Boid triangular shape
            Vector2 vert1 = new Vector2(boid.position.X, boid.position.Y) - boid.position;
            Vector2 vert2 = new Vector2(boid.position.X - 3, boid.position.Y + 10) - boid.position;
            Vector2 vert3 = new Vector2(boid.position.X + 3, boid.position.Y + 10) - boid.position;
            // Rotation logic
            float angle = MathF.Atan2(boid.velocity.Y, boid.velocity.X) + MathF.PI / 2;
            vert1 = new Vector2(vert1.X * MathF.Cos(angle) - vert1.Y * MathF.Sin(angle), vert1.X * MathF.Sin(angle) + vert1.Y * MathF.Cos(angle)) + boid.position;
            vert2 = new Vector2(vert2.X * MathF.Cos(angle) - vert2.Y * MathF.Sin(angle), vert2.X * MathF.Sin(angle) + vert2.Y * MathF.Cos(angle)) + boid.position;
            vert3 = new Vector2(vert3.X * MathF.Cos(angle) - vert3.Y * MathF.Sin(angle), vert3.X * MathF.Sin(angle) + vert3.Y * MathF.Cos(angle)) + boid.position;
            // Draw the boid
            byte r = (byte)(boid.velocity.X / maximumBoidSpeed * 127 + 128);
            byte g = (byte)(boid.velocity.Y / maximumBoidSpeed * 127 + 128);
            byte b = 200;
            byte alpha = (byte)Math.Clamp(50 + (boid.neighbors * 15), 50, 255);
            Raylib.DrawTriangle(vert1, vert2, vert3, new Color(r, g, b, alpha));
        }
    }

    protected override string AddDebugData()
    {
        return $"[SYSTEM]\n" +
           $"FPS             : {Raylib.GetFPS()}\n" +
           $"Screen Space    : {screenWidth}x{screenHeight}\n" +
           $"Spatial Grid    : {gridColumns}x{gridRows} Cells ({boidPerceptionRadius}px tracking)\n\n" +
           $"[FLOCK STATUS]\n" +
           $"Population      : {boids.Count:N0} boids\n\n" +
           $"[VARIABLES & COEFFICIENTS]\n" +
           $"[Up/Down] Alignment : {alignmentWeight}\n" +
           $"[Left/Right] Cohesion  : {cohesionWeight}\n" +
           $"[w/s] Separation: {separationWeight}\n" +
           $"[1/2] Maximum Speed: {maximumBoidSpeed}\n" +
           $"[3/4] Steering Force: {maximumSteeringForce}\n\n" +
           $"[INPUT SUITE]\n" +
           $"[+/-] Change Preset; Current: {presetName(currentPreset)}\n\n" +
           $"L-Click         : Predator Threat (Scatter)\n" +
           $"R-Click         : Rotating Force Field\n" +
           $"Key [B]         : Stream Spawning Brush\n" +
           $"Key [C]         : Clear Flock";
    }
}
using System.Numerics;
using Core;
using Raylib_cs;

public class FlockingSim : Simulation
{
    // Constants
    const int BoidPerceptionRadius = 40;
    const int MaximumBoidSpeed = 250;
    const int MaximumSteeringForce = 200;

    const int screenWidth = 1700;
    const int screenHeight = 900;

    // Spatial grid references
    int totalRows = (int)MathF.Ceiling(screenWidth / BoidPerceptionRadius);
    int totalColumns = (int)MathF.Ceiling(screenHeight / BoidPerceptionRadius);

    // Variables
    float alignmentWeight = 2;
    float cohesionWeight = 1f;
    float separationWeight = 1.8f;
    float wanderingWeight = 150f;

    // Basic boid class for a single entity
    class Boid
    {
        public Vector2 position;
        public Vector2 velocity;

        public Boid(int x, int y, Vector2 vel)
        {
            position = new Vector2(x, y);
            velocity = vel;
        }
    }

    // List containing all current boid entities
    List<Boid> boids;

    // Hash grid allocation dictionary for boids
    Dictionary<int, List<Boid>> spatialGrid = new Dictionary<int, List<Boid>>();

    Random random = new Random();

    static void Main()
    {
        // Set up window
        FlockingSim flockingSim = new FlockingSim();
        flockingSim.Run(screenWidth, screenHeight, "Flocking Simulation");
    }

    protected override void Initialize()
    {
        // Init boids
        boids = new List<Boid>();
        for (int i = 0; i < 5000; i++)
        {
            boids.Add(new Boid(random.Next(0, screenWidth), random.Next(0, screenHeight), new Vector2(random.Next(-300, 300), random.Next(-300, 300))));
        }

        // Init spacial grid
        for (int i = 0; i < totalRows; i++)
        {
            for (int j = 0; j < totalColumns; j++)
            {
                int hashKey = i + (j * totalRows);
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
            int cellX = (int)(boids[i].position.X / BoidPerceptionRadius);
            int cellY = (int)(boids[i].position.Y / BoidPerceptionRadius);
            cellX = Math.Clamp(cellX, 0, totalRows - 1);
            cellY = Math.Clamp(cellY, 0, totalColumns - 1);
            spatialGrid[cellX + cellY * totalRows].Add(boids[i]);
        }

        // Boid logic
        for (int i = 0; i < boids.Count; i++)
        {
            Vector2 totalVelocity = Vector2.Zero;
            Vector2 totalPositions = Vector2.Zero;
            Vector2 escapeDirection = Vector2.Zero;
            int neighbors = 0;
            Boid boid = boids[i];

            // Find neighbors in perception range via spatial grid and collect values
            for (int xOffset = -1; xOffset <= 1; xOffset++)
            {
                for (int yOffset = -1; yOffset <= 1; yOffset++)
                {
                    // check all neighboring cells
                    int cellX = (int)(boid.position.X / BoidPerceptionRadius);
                    int cellY = (int)(boid.position.Y / BoidPerceptionRadius);
                    int neighborX = cellX + xOffset;
                    int neighborY = cellY + yOffset;

                    if (neighborX < totalRows && neighborX > 0)
                    {
                        if (neighborY < totalColumns && neighborY > 0)
                        {
                            int hash = neighborX + neighborY * totalRows;
                            foreach (Boid other in spatialGrid[hash])
                            {
                                if (other != boid)
                                {
                                    // Check distance of boids
                                    float dist = Vector2.Distance(other.position, boid.position);
                                    if (dist < BoidPerceptionRadius)
                                    {
                                        neighbors++;
                                        totalVelocity += other.velocity;
                                        totalPositions += other.position;
                                        Vector2 diff = boid.position - other.position;
                                        if (dist < BoidPerceptionRadius && dist > 0.01f)
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
                wanderForce = Vector2.Normalize(rawJitter) * wanderingWeight;
            }
            steering += wanderForce;

            // Avoid the mouse cursor
            Vector2 mousePos = Raylib.GetMousePosition();
            if (Vector2.Distance(boid.position, mousePos) < 150f && Raylib.IsMouseButtonDown(MouseButton.Left))
            {
                Vector2 escape = Vector2.Normalize(boid.position - mousePos) * MaximumBoidSpeed;

                // Forcefully snap 20% of their velocity toward the escape vector instantly this frame
                boid.velocity = Vector2.Lerp(boid.velocity, escape, 0.2f);
            }

            if (neighbors > 0)
            {
                // Calculate alignment steering
                totalVelocity /= neighbors;
                totalVelocity = Vector2.Normalize(totalVelocity) * MaximumBoidSpeed;

                // Calculate cohesion steering
                totalPositions /= neighbors;
                Vector2 directionToCenter = totalPositions - boid.position;
                directionToCenter = Vector2.Normalize(directionToCenter) * MaximumBoidSpeed;

                // Calculate separation steering
                escapeDirection = Vector2.Normalize(escapeDirection) * MaximumBoidSpeed;

                Vector2 desiredSteering = (totalVelocity * alignmentWeight) + (directionToCenter * cohesionWeight) + (escapeDirection * separationWeight);
                if (desiredSteering.Length() > 0)
                {
                    desiredSteering = Vector2.Normalize(desiredSteering) * MaximumBoidSpeed;
                    steering += desiredSteering - boid.velocity;
                }
            }

            // Steer the boid
            if (steering.Length() > MaximumSteeringForce)
                steering = Vector2.Normalize(steering) * MaximumSteeringForce;
            boid.velocity += steering * deltaTime;
            if (boid.velocity.Length() > MaximumBoidSpeed)
            {
                boid.velocity = Vector2.Normalize(boid.velocity) * MaximumBoidSpeed;
            }

            // Update boid position
            boid.position += boid.velocity * deltaTime;

            // Wrap-around logic
            if (boid.position.X < 0) boid.position.X = screenWidth - 1;
            if (boid.position.X > screenWidth) boid.position.X = 1;
            if (boid.position.Y < 0) boid.position.Y = screenHeight - 1;
            if (boid.position.Y > screenHeight) boid.position.Y = 1;
        }
    }

    protected override void Draw()
    {
        Raylib.ClearBackground(new Color(20, 20, 20, 255));
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
            Raylib.DrawTriangle(vert1, vert2, vert3, Color.White);
        }
        Raylib.DrawFPS(10, 10);
    }
}
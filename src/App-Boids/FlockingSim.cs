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
    Random random = new Random();

    static void Main()
    {
        // Set up window
        FlockingSim flockingSim = new FlockingSim();
        flockingSim.Run(screenWidth, screenHeight, "Flocking Simulation");
    }

    protected override void Initialize()
    {
        boids = new List<Boid>();
        for (int i = 0; i < 500; i++)
        {
            boids.Add(new Boid(random.Next(0, screenWidth), random.Next(0, screenHeight), new Vector2(random.Next(-300, 300), random.Next(-300, 300))));
        }
    }

    protected override void Update(float deltaTime)
    {
        for (int i = 0; i < boids.Count; i++)
        {
            // Boid logic
            Vector2 totalVelocity = Vector2.Zero;
            Vector2 totalPositions = Vector2.Zero;
            Vector2 escapeDirection = Vector2.Zero;
            int neighbors = 0;

            // Find neighbors in perception range and collect values
            for (int j = 0; j < boids.Count; j++)
            {
                if (j != i)
                {
                    // Check distance of boids
                    float dist = Vector2.Distance(boids[j].position, boids[i].position);
                    if (dist < BoidPerceptionRadius)
                    {
                        neighbors++;
                        totalVelocity += boids[j].velocity;
                        totalPositions += boids[j].position;
                        Vector2 diff = boids[i].position - boids[j].position;
                        if (dist < BoidPerceptionRadius && dist > 0.01f)
                            diff /= dist * dist; // Push harder inversely proportional to distance
                        escapeDirection += diff;
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
            if (Vector2.Distance(boids[i].position, mousePos) < 150f && Raylib.IsMouseButtonDown(MouseButton.Left))
            {
                Vector2 escape = Vector2.Normalize(boids[i].position - mousePos) * MaximumBoidSpeed;

                // Forcefully snap 20% of their velocity toward the escape vector instantly this frame
                boids[i].velocity = Vector2.Lerp(boids[i].velocity, escape, 0.2f);
            }

            if (neighbors > 0)
            {
                // Calculate alignment steering
                totalVelocity /= neighbors;
                totalVelocity = Vector2.Normalize(totalVelocity) * MaximumBoidSpeed;

                // Calculate cohesion steering
                totalPositions /= neighbors;
                Vector2 directionToCenter = totalPositions - boids[i].position;
                directionToCenter = Vector2.Normalize(directionToCenter) * MaximumBoidSpeed;

                // Calculate separation steering
                escapeDirection = Vector2.Normalize(escapeDirection) * MaximumBoidSpeed;

                Vector2 desiredSteering = (totalVelocity * alignmentWeight) + (directionToCenter * cohesionWeight) + (escapeDirection * separationWeight);
                if (desiredSteering.Length() > 0)
                {
                    desiredSteering = Vector2.Normalize(desiredSteering) * MaximumBoidSpeed;
                    steering += desiredSteering - boids[i].velocity;
                }
            }

            // Steer the boid
            if (steering.Length() > MaximumSteeringForce)
                steering = Vector2.Normalize(steering) * MaximumSteeringForce;
            boids[i].velocity += steering * deltaTime;
            if (boids[i].velocity.Length() > MaximumBoidSpeed)
            {
                boids[i].velocity = Vector2.Normalize(boids[i].velocity) * MaximumBoidSpeed;
            }

            // Update boid position
            boids[i].position += boids[i].velocity * deltaTime;

            // Wrap-around logic
            if (boids[i].position.X < 0) boids[i].position.X = screenWidth - 1;
            if (boids[i].position.X > screenWidth) boids[i].position.X = 1;
            if (boids[i].position.Y < 0) boids[i].position.Y = screenHeight - 1;
            if (boids[i].position.Y > screenHeight) boids[i].position.Y = 1;
        }
    }

    protected override void Draw()
    {
        Raylib.ClearBackground(new Color(20, 20, 20, 255));
        foreach (Boid boid in boids)
        {
            // Boid triangular shape
            Vector2 vert1 = new Vector2(boid.position.X, boid.position.Y) - boid.position;
            Vector2 vert2 = new Vector2(boid.position.X - 5, boid.position.Y + 15) - boid.position;
            Vector2 vert3 = new Vector2(boid.position.X + 5, boid.position.Y + 15) - boid.position;
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
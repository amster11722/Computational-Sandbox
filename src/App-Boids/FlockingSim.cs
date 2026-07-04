using System.Numerics;
using Core;
using Raylib_cs;

public class FlockingSim : Simulation
{
    // Constants
    const int BoidPerceptionRadius = 50;
    const int MaximumBoidSpeed = 300;
    const int MaximumSteeringForce = 200;

    // Variables
    float alignmentWeight = 1;
    float cohesionWeight = 1;
    float separationWeight = 1;

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

    static void Main()
    {
        // Set up window
        FlockingSim flockingSim = new FlockingSim();
        flockingSim.Run(800, 500, "Flocking Simulation");
    }

    protected override void Initialize()
    {
        boids = new List<Boid>();
        Random rng = new Random();
        for (int i = 0; i < 100; i++)
        {
            boids.Add(new Boid(rng.Next(0, 800), rng.Next(0, 500), new Vector2(rng.Next(-300, 300), rng.Next(-300, 300))));
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
                        diff /= dist * dist; // Push harder inversely proportional to distance
                        escapeDirection += diff;
                    }
                }
            }

            Vector2 steering = Vector2.Zero;

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
                    steering = desiredSteering - boids[i].velocity;
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
            if (boids[i].position.X < 0) boids[i].position.X = 799;
            if (boids[i].position.X > 800) boids[i].position.X = 1;
            if (boids[i].position.Y < 0) boids[i].position.Y = 499;
            if (boids[i].position.Y > 500) boids[i].position.Y = 1;
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
            Raylib.DrawFPS(10, 10);
        }
    }
}
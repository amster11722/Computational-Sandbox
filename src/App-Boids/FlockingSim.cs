using System.Numerics;
using Core;
using Raylib_cs;

public class FlockingSim : Simulation
{

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
            // Update boid position
            boids[i].position += boids[i].velocity * deltaTime;
            // Wrap-around logic
            if(boids[i].position.X < 0) boids[i].position.X = 799;
            if(boids[i].position.X > 800) boids[i].position.X = 1;
            if(boids[i].position.Y < 0) boids[i].position.Y = 499;
            if(boids[i].position.Y > 500) boids[i].position.Y = 1;
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
            float angle = MathF.Atan2(boid.velocity.Y, boid.velocity.X) + MathF.PI/2;
            vert1 = new Vector2(vert1.X * MathF.Cos(angle) - vert1.Y * MathF.Sin(angle), vert1.X * MathF.Sin(angle) + vert1.Y * MathF.Cos(angle)) + boid.position;
            vert2 = new Vector2(vert2.X * MathF.Cos(angle) - vert2.Y * MathF.Sin(angle), vert2.X * MathF.Sin(angle) + vert2.Y * MathF.Cos(angle)) + boid.position;
            vert3 = new Vector2(vert3.X * MathF.Cos(angle) - vert3.Y * MathF.Sin(angle), vert3.X * MathF.Sin(angle) + vert3.Y * MathF.Cos(angle)) + boid.position;
            // Draw the boid
            Raylib.DrawTriangle(vert1, vert2, vert3, Color.White);
        }
    }
}
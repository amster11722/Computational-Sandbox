namespace Core;
using Raylib_cs;

public abstract class Simulation
{

    // Core class for rendering.

    // Function to initialize window of width, height with title
    public void Run(int width, int height, string title)
    {
        // Basic initialization
        Raylib.InitWindow(width, height, title);
        Raylib.SetTargetFPS(60);

        Initialize();

        // Draw loop
        while (!Raylib.WindowShouldClose())
        {
            Update(Raylib.GetFrameTime()); // deltatime
            Raylib.BeginDrawing();
            Draw();
            Raylib.DrawFPS(10, 10);
            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }

    // Basic initialization
    protected virtual void Initialize()
    {
        Console.WriteLine("Initializing...");
    }

    // Runs every frame, framerate independent via deltaTime
    protected virtual void Update(float deltaTime)
    {
    }

    // Overrideable class to render with
    protected abstract void Draw();
}

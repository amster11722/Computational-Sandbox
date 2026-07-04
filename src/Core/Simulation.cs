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
            // Runs data-based updates, passing in deltaTime
            Update(Raylib.GetFrameTime());
            Raylib.BeginDrawing();
            // Runs visual-based updates
            Draw();
            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }

    protected virtual void Initialize() {}
    protected virtual void Update(float deltaTime) {}
    protected abstract void Draw();
}

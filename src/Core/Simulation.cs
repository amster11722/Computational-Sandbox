namespace Core;

using Raylib_cs;

// Core class for rendering
public abstract class Simulation
{

    bool debugMenuOpen = false;

    // Function to initialize window of width, height with title
    public void Run(int width, int height, string title)
    {
        // Basic initialization
        Raylib.InitWindow(width, height, title);
        Raylib.SetTargetFPS(144);

        Initialize();

        // Draw loop
        while (!Raylib.WindowShouldClose())
        {
            // Runs data-based updates, passing in deltaTime
            Update(Raylib.GetFrameTime());
            Raylib.BeginDrawing();
            // Runs visual-based updates
            Draw();
            // Diagnostic data
            if (Raylib.IsKeyPressed(KeyboardKey.Tab))
                debugMenuOpen = !debugMenuOpen;
            if (debugMenuOpen)
            {
                Raylib.DrawFPS(10, 10);
                string data = "";
                data += AddDebugData();
                Raylib.DrawText(data, 10, 35, 20, new Color(0, 158, 47));
            }
            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }

    protected virtual void Initialize() { }
    protected virtual void Update(float deltaTime) { }
    protected abstract void Draw();
    protected abstract string AddDebugData();
}

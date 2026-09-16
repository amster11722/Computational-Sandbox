namespace Core;

using Raylib_cs;

// Shared lifecycle and diagnostics framework for all simulations
public abstract class Simulation
{

    bool debugMenuOpen = true;

    // Function to initialize window of width, height with title
    public void Run(int width, int height, string title)
    {
        // Basic initialization
        Raylib.InitWindow(width, height, title);
        Raylib.SetTargetFPS(144);

        Initialize();

        // Main update/render loop
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
                string data = AddDebugData() + "\n\n[TAB] Toggle diagnostics";
                DrawTextWithOutline(data, 10, 10, 20, new Color(0, 158, 47), Color.Black, 2);
            }
            Raylib.EndDrawing();
        }
        Deinitialize();
        Raylib.CloseWindow();
    }

    void DrawTextWithOutline(string text, int x, int y, int fontSize, Color textColor, Color outlineColor, int strokeWidth)
    {
        Raylib.DrawText(text, x - strokeWidth, y, fontSize, outlineColor);
        Raylib.DrawText(text, x + strokeWidth, y, fontSize, outlineColor);
        Raylib.DrawText(text, x, y - strokeWidth, fontSize, outlineColor);
        Raylib.DrawText(text, x, y + strokeWidth, fontSize, outlineColor);

        Raylib.DrawText(text, x, y, fontSize, textColor);
    }

    protected virtual void Initialize() { }
    protected virtual void Deinitialize() { }
    protected virtual void Update(float deltaTime) { }
    protected abstract void Draw();
    protected abstract string AddDebugData();
}

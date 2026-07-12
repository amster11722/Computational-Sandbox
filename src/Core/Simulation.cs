namespace Core;

using Raylib_cs;

// Core class for rendering
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
                string data = "";
                data += AddDebugData();
                data += "\n\n[TAB] to close or open menu";
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

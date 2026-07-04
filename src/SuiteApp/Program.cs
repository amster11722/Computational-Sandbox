using Core;
using Raylib_cs;

class Program : Core.Simulation
{

    float xPos = 100f;

    static void Main()
    {
        Program myApp = new Program();
        myApp.Run(800, 500, "Test");
    }

    protected override void Initialize()
    {

    }

    protected override void Update(float deltaTime)
    {
        xPos += deltaTime * 50;
    }

    protected override void Draw()
    {
        Raylib.ClearBackground(new Color(20, 20, 20, 255));
        Raylib.DrawCircle((int) xPos, 100, 20, new Color(255, 255, 255, 255));
    }
}
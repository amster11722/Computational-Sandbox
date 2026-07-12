using System.Numerics;
using Core;
using Raylib_cs;

public class SlimeSim : Simulation
{

    float sensorDistance = 20f;
    float turnSpeed = 20f;
    float moveSpeed = 200f;

    int screenWidth = 500;
    int screenHeight = 500;

    Random random = new Random();

    // GLSL helpers
    Shader diffusionShader;
    RenderTexture2D targetA;
    RenderTexture2D targetB;

    // Slime agent which moves along pheromone trails
    public class SlimeAgent
    {
        public Vector2 position;
        public float angle; // Angle in radians

        public SlimeAgent(int x, int y, float dir)
        {
            position = new Vector2(x, y);
            angle = dir;
        }
    }

    List<SlimeAgent> slimeAgents = new List<SlimeAgent>();
    Color[]? pheromones;
    Texture2D trailTexture;

    static void Main()
    {
        // Set up window
        SlimeSim slimeSim = new SlimeSim();
        slimeSim.Run(500, 500, "Flocking Simulation");
    }

    protected override void Initialize()
    {
        // Set fullscreen
        int monitor = Raylib.GetCurrentMonitor();
        screenWidth = Raylib.GetMonitorWidth(monitor);
        screenHeight = Raylib.GetMonitorHeight(monitor);

        Raylib.SetWindowSize(screenWidth, screenHeight);
        Raylib.ToggleFullscreen();

        // Initialize pheromone trail map
        Image blankImage = Raylib.GenImageColor(screenWidth, screenHeight, Color.Black);
        trailTexture = Raylib.LoadTextureFromImage(blankImage);

        pheromones = new Color[screenWidth * screenHeight];
        Array.Fill(pheromones, Color.Black);

        // Allocate framebuffers
        targetA = Raylib.LoadRenderTexture(screenWidth, screenHeight);
        targetB = Raylib.LoadRenderTexture(screenWidth, screenHeight);

        // Load custom GLSL pass for diffusion
        string shaderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "diffusion.frag");
        diffusionShader = Raylib.LoadShader(null, shaderPath);

        // Initialize slime agents
        for (int i = 0; i < 50000; i++)
        {
            slimeAgents.Add(new SlimeAgent(random.Next(0, screenWidth), random.Next(0, screenHeight), (float)(random.NextDouble() * 2 * Math.PI)));
        }
    }

    protected override void Update(float deltaTime)
    {
        // Pull fresh data
        VramToCPU(targetB);
        // Slime logic, runs in parallel
        Parallel.For(0, slimeAgents.Count, i =>
        {
            // Detect at left, center, and right
            Vector2 pos = slimeAgents[i].position;
            float rot = slimeAgents[i].angle;
            Vector2 centerSensor = pos + new Vector2(MathF.Cos(rot), MathF.Sin(rot)) * sensorDistance;
            Vector2 leftSensor = pos + new Vector2(MathF.Cos(rot + MathF.PI / 4), MathF.Sin(rot + MathF.PI / 4)) * sensorDistance;
            Vector2 rightSensor = pos + new Vector2(MathF.Cos(rot - MathF.PI / 4), MathF.Sin(rot - MathF.PI / 4)) * sensorDistance;
            float left = GetPheromoneAt(leftSensor.X, leftSensor.Y);
            float center = GetPheromoneAt(centerSensor.X, centerSensor.Y);
            float right = GetPheromoneAt(rightSensor.X, rightSensor.Y);
            // Rotate if pheromones stronger in one direction
            if (left > right && center < left)
                slimeAgents[i].angle += turnSpeed * deltaTime;
            if (right > left && center < right)
                slimeAgents[i].angle -= turnSpeed * deltaTime;
            // Wander if no pheromones in sight, does not work in parallel
            // if (right == 0 && left == 0 && center == 0)
            //     slimeAgents[i].angle += (float)(random.NextDouble() * 50 - 25) * deltaTime;
            // Move
            slimeAgents[i].position += new Vector2(MathF.Cos(rot), MathF.Sin(rot)) * moveSpeed * deltaTime;
            // Wrap position
            if (slimeAgents[i].position.X < 0) slimeAgents[i].position.X = screenWidth - 1;
            if (slimeAgents[i].position.X > screenWidth) slimeAgents[i].position.X = 1;
            if (slimeAgents[i].position.Y < 0) slimeAgents[i].position.Y = screenHeight - 1;
            if (slimeAgents[i].position.Y > screenHeight) slimeAgents[i].position.Y = 1;
        });

        foreach (SlimeAgent agent in slimeAgents)
        {
            AddPheromoneAt(agent.position.X, agent.position.Y, 255);
        }

        unsafe
        {
            fixed (Color* pixel = pheromones)
            {
                Raylib.UpdateTexture(targetA.Texture, pixel);
            }
        }

        // Apply GLSL shader to diffuse pheromones
        Raylib.BeginTextureMode(targetB);
        Raylib.BeginShaderMode(diffusionShader);
        Raylib.DrawTextureRec(targetA.Texture, new Rectangle(0, 0, screenWidth, -screenHeight), Vector2.Zero, Color.White);
        Raylib.EndShaderMode();
        Raylib.EndTextureMode();
    }

    // Get and set pheromone positions to prevent out of bounds accessing
    float GetPheromoneAt(float x, float y)
    {
        int clampedX = Math.Clamp((int)MathF.Round(x), 0, screenWidth - 1);
        int clampedY = Math.Clamp((int)MathF.Round(y), 0, screenHeight - 1);
        return pheromones[clampedX + clampedY * screenWidth].R;
    }

    void SetPheromoneAt(Color[] list, float x, float y, float value)
    {
        int clampedX = Math.Clamp((int)MathF.Round(x), 0, screenWidth - 1);
        int clampedY = Math.Clamp((int)MathF.Round(y), 0, screenHeight - 1);
        int index = clampedX + clampedY * screenWidth;
        byte val = (byte)Math.Min(255, value);
        list[index].R = val;
        list[index].G = val;
        list[index].B = val;
        list[index].A = 255;
    }

    void AddPheromoneAt(float x, float y, float value)
    {
        SetPheromoneAt(pheromones, x, y, GetPheromoneAt(x, y) + value);
    }

    void VramToCPU(RenderTexture2D source)
    {
        Image gpuImage = Raylib.LoadImageFromTexture(source.Texture);
        unsafe
        {
            Color* pixels = (Color*)gpuImage.Data;

            // Copy bytes to memory array
            fixed (Color* dest = pheromones)
            {
                Buffer.MemoryCopy(pixels, dest, pheromones.Length * sizeof(Color), pheromones.Length * sizeof(Color));
            }
        }
        Raylib.UnloadImage(gpuImage);
    }

    protected override void Draw()
    {
        Raylib.ClearBackground(new Color(20, 20, 20, 255));
        // Blit trail pixels to texture and screen
        unsafe
        {
            fixed (Color* pixel = pheromones)
            {
                Raylib.UpdateTexture(trailTexture, pixel);
            }
        }
        Raylib.DrawTexture(trailTexture, 0, 0, Color.White);
    }

    protected override void Deinitialize()
    {
        Raylib.UnloadTexture(trailTexture);
        Raylib.UnloadShader(diffusionShader);
        Raylib.UnloadRenderTexture(targetA);
        Raylib.UnloadRenderTexture(targetB);
    }

    protected override string AddDebugData()
    {
        return "";
    }
}
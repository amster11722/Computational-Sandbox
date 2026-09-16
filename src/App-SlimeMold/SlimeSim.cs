using System.Numerics;
using Core;
using Raylib_cs;

public class SlimeSim : Simulation
{

    float sensorDistance = 20f;
    float turnSpeed = 20f;
    float moveSpeed = 200f;
    int populationSize = 80000;

    int screenWidth = 500;
    int screenHeight = 500;
    int canvasWidth = 1920;
    int canvasHeight = 1080;

    Random random = new Random();

    // GLSL helpers
    Shader diffusionShader;
    Shader paletteShader;
    RenderTexture2D targetA;
    RenderTexture2D targetB;

    // Slime agent which moves along pheromone trails
    public struct SlimeAgent
    {
        public Vector2 position;
        public float angle; // Angle in radians

        public SlimeAgent(int x, int y, float dir)
        {
            position = new Vector2(x, y);
            angle = dir;
        }
    }

    SlimeAgent[] slimeAgents;
    Color[]? pheromones;

    public enum Preset { Custom, Slime, Snakes, Cells, Viruses, Bacteria, Veins, Maze, Null }

    Preset currentPreset = Preset.Slime;

    void ApplyPreset(Preset preset)
    {
        if (preset < 0) preset = Preset.Custom;
        if (preset == Preset.Custom || preset == Preset.Null) return;
        currentPreset = preset;
        switch (preset)
        {
            case Preset.Custom:
                break;
            case Preset.Slime:
                sensorDistance = 20f; turnSpeed = 20f; moveSpeed = 200f;
                break;
            case Preset.Snakes:
                sensorDistance = 20f; turnSpeed = 20f; moveSpeed = 500f;
                break;
            case Preset.Cells:
                sensorDistance = 20f; turnSpeed = 150f; moveSpeed = 420f;
                break;
            case Preset.Viruses:
                sensorDistance = 3f; turnSpeed = 150f; moveSpeed = 420f;
                break;
            case Preset.Bacteria:
                sensorDistance = 18f; turnSpeed = 200f; moveSpeed = 275f;
                break;
            case Preset.Veins:
                sensorDistance = 20f; turnSpeed = 390f; moveSpeed = 40f;
                break;
            case Preset.Maze:
                sensorDistance = 65f; turnSpeed = 280f; moveSpeed = 1000f;
                break;

        }
    }

    public string presetName(Preset preset)
    {
        switch (preset)
        {
            case Preset.Custom:
                return "Custom";
            case Preset.Slime:
                return "Slime";
            case Preset.Snakes:
                return "Snakes";
            case Preset.Cells:
                return "Cells";
            case Preset.Viruses:
                return "Viruses";
            case Preset.Bacteria:
                return "Bacteria";
            case Preset.Veins:
                return "Veins";
            case Preset.Maze:
                return "Maze";
        }
        return "N/A";
    }

    static void Main()
    {
        // Set up window
        SlimeSim slimeSim = new SlimeSim();
        slimeSim.Run(500, 500, "Slime Mold Simulation");
    }

    protected override void Initialize()
    {
        // Set fullscreen
        int monitor = Raylib.GetCurrentMonitor();
        screenWidth = Raylib.GetMonitorWidth(monitor);
        screenHeight = Raylib.GetMonitorHeight(monitor);

        Raylib.SetWindowSize(screenWidth, screenHeight);
        Raylib.ToggleFullscreen();

        pheromones = new Color[canvasWidth * canvasHeight];
        Array.Fill(pheromones, Color.Black);

        // Allocate framebuffers
        targetA = Raylib.LoadRenderTexture(canvasWidth, canvasHeight);
        targetB = Raylib.LoadRenderTexture(canvasWidth, canvasHeight);

        // Load custom GLSL pass for diffusion and coloring
        string binDir = AppDomain.CurrentDomain.BaseDirectory;
        diffusionShader = Raylib.LoadShader(null, Path.Combine(binDir, "diffusion.frag"));
        paletteShader = Raylib.LoadShader(null, Path.Combine(binDir, "palette.frag"));

        slimeAgents = new SlimeAgent[populationSize];

        // Initialize slime agents
        for (int i = 0; i < populationSize; i++)
        {
            slimeAgents[i] = new SlimeAgent(random.Next(0, canvasWidth), random.Next(0, canvasHeight), (float)(random.NextDouble() * 2 * Math.PI));
        }
    }

    protected override void Update(float deltaTime)
    {
        // Pull fresh data
        VramToCpu(targetB);
        // Slime logic, runs in parallel
        Parallel.For(0, populationSize, i =>
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
            // Move
            slimeAgents[i].position += new Vector2(MathF.Cos(rot), MathF.Sin(rot)) * moveSpeed * deltaTime;
            // Wrap position
            if (slimeAgents[i].position.X < 0) slimeAgents[i].position.X = canvasWidth - 1;
            if (slimeAgents[i].position.X > canvasWidth) slimeAgents[i].position.X = 1;
            if (slimeAgents[i].position.Y < 0) slimeAgents[i].position.Y = canvasHeight - 1;
            if (slimeAgents[i].position.Y > canvasHeight) slimeAgents[i].position.Y = 1;
        });

        // Deposit agent pheromones into the CPU trail buffer

        foreach (SlimeAgent agent in slimeAgents)
        {
            AddPheromoneAt(agent.position.X, agent.position.Y, 255);
        }

        // Mouse interactivity
        if (Raylib.IsMouseButtonDown(MouseButton.Left) || Raylib.IsMouseButtonDown(MouseButton.Right))
        {
            Vector2 screenMouse = Raylib.GetMousePosition();
            int canvasX = (int)(screenMouse.X / screenWidth * canvasWidth);
            int canvasY = (int)((screenHeight - screenMouse.Y) / screenHeight * canvasHeight);
            int brushRadius = 15;
            byte depositValue = Raylib.IsMouseButtonDown(MouseButton.Left) ? (byte)255 : (byte)0;
            for (int dx = -brushRadius; dx <= brushRadius; dx++)
            {
                for (int dy = -brushRadius; dy <= brushRadius; dy++)
                {
                    if (dx * dx + dy * dy <= brushRadius * brushRadius)
                    {
                        SetPheromoneAt(pheromones, canvasX + dx, canvasY + dy, depositValue);
                    }
                }
            }
        }

        // Adjust physics via keyboard
        if (Raylib.IsKeyDown(KeyboardKey.Up)) moveSpeed += 5f;
        if (Raylib.IsKeyDown(KeyboardKey.Down)) moveSpeed = MathF.Max(10f, moveSpeed - 5f);
        if (Raylib.IsKeyDown(KeyboardKey.Right)) turnSpeed += 1f;
        if (Raylib.IsKeyDown(KeyboardKey.Left)) turnSpeed = MathF.Max(0f, turnSpeed - 1f);
        if (Raylib.IsKeyDown(KeyboardKey.W)) sensorDistance += 0.5f;
        if (Raylib.IsKeyDown(KeyboardKey.S)) sensorDistance = MathF.Max(1f, sensorDistance - 0.5f);

        if (Raylib.IsKeyDown(KeyboardKey.Down) || Raylib.IsKeyDown(KeyboardKey.Up) || Raylib.IsKeyDown(KeyboardKey.Left) || Raylib.IsKeyDown(KeyboardKey.Right) || Raylib.IsKeyDown(KeyboardKey.S) || Raylib.IsKeyDown(KeyboardKey.W)) currentPreset = Preset.Custom;

        if (Raylib.IsKeyPressed(KeyboardKey.Minus))
            ApplyPreset(currentPreset - 1);
        if (Raylib.IsKeyPressed(KeyboardKey.Equal))
            ApplyPreset(currentPreset + 1);

        unsafe
        {
            fixed (Color* pixel = pheromones)
            {
                // Upload the updated trail buffer to the GPU
                Raylib.UpdateTexture(targetA.Texture, pixel);
            }
        }

        // Apply GLSL shader to diffuse pheromones
        Raylib.BeginTextureMode(targetB);
        Raylib.BeginShaderMode(diffusionShader);
        Raylib.DrawTextureRec(targetA.Texture, new Rectangle(0, 0, canvasWidth, -canvasHeight), Vector2.Zero, Color.White);
        Raylib.EndShaderMode();
        Raylib.EndTextureMode();

        // Pass time to palette shader
        int timeLocation = Raylib.GetShaderLocation(paletteShader, "u_time");
        float totalTime = (float)Raylib.GetTime();
        Raylib.SetShaderValue(paletteShader, timeLocation, totalTime, ShaderUniformDataType.Float);
    }

    // Get and set pheromone positions to prevent out of bounds accessing
    float GetPheromoneAt(float x, float y)
    {
        int clampedX = Math.Clamp((int)MathF.Round(x), 0, canvasWidth - 1);
        int clampedY = Math.Clamp((int)MathF.Round(y), 0, canvasHeight - 1);
        return pheromones[clampedX + clampedY * canvasWidth].R;
    }

    void SetPheromoneAt(Color[] buffer, float x, float y, float value)
    {
        int clampedX = Math.Clamp((int)MathF.Round(x), 0, canvasWidth - 1);
        int clampedY = Math.Clamp((int)MathF.Round(y), 0, canvasHeight - 1);
        int index = clampedX + clampedY * canvasWidth;
        byte val = (byte)Math.Min(255, value);
        buffer[index].R = val;
        buffer[index].G = val;
        buffer[index].B = val;
        buffer[index].A = 255;
    }

    void AddPheromoneAt(float x, float y, float value)
    {
        SetPheromoneAt(pheromones, x, y, GetPheromoneAt(x, y) + value);
    }

    void VramToCpu(RenderTexture2D source)
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
        Raylib.ClearBackground(Color.Black);
        Rectangle sourceRec = new Rectangle(0, 0, canvasWidth, -canvasHeight);

        Rectangle destRec = new Rectangle(0, 0, screenWidth, screenHeight);
        Raylib.BeginShaderMode(paletteShader);
        Raylib.DrawTexturePro(targetB.Texture, sourceRec, destRec, Vector2.Zero, 0.0f, Color.White);
        Raylib.EndShaderMode();
    }

    protected override void Deinitialize()
    {
        Raylib.UnloadShader(diffusionShader);
        Raylib.UnloadShader(paletteShader);
        Raylib.UnloadRenderTexture(targetA);
        Raylib.UnloadRenderTexture(targetB);
    }

    protected override string AddDebugData()
    {
        return $"[SYSTEM]\n" +
           $"FPS             : {Raylib.GetFPS()}\n" +
           $"Resolution      : {screenWidth}x{screenHeight} (Native)\n" +
           $"Trail Buffer   : {canvasWidth}x{canvasHeight}\n\n" +
           $"[SIMULATION]\n" +
           $"Population Size : {populationSize:N0} agents\n" +
           $"Move Speed (Up/Down)  : {moveSpeed:F0} px/s\n" +
           $"Turn Rate (Left/Right)  : {turnSpeed:F0} rad/s\n" +
           $"Sensor Distance  (W/S)  : {sensorDistance:F1} px\n\n" +
           $"[+/-] Change Preset; Current: {presetName(currentPreset)}\n\n" +
           $"[INTERACTION]\n" +
           $"L-Click : Deposit Pheromones\n" +
           $"R-Click : Vacuum/Erase Trails\n";
    }
}
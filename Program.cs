using System;

class Program
{
    static void Main()
    {
        int width = 80;      // How wide the mountain range is (columns)
        int maxHeight = 20;  // Max height of the mountains (rows)

        int[] heights = GenerateHeights(width, maxHeight);
        DrawMountains(heights, maxHeight);

        // Title / intro printed below the mountains and centered to the mountain width
        Console.WriteLine();
        string intro = "Welcome to this RPG! Press Enter to begin.";
        int padding = Math.Max(0, (width - intro.Length) / 2);
        Console.WriteLine(new string(' ', padding) + intro);

        Console.ReadLine();

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    // Make a 1D "heightmap" using a random walk so it looks like rolling mountains
    static int[] GenerateHeights(int width, int maxHeight)
    {
        var rand = new Random();
        int[] heights = new int[width];

        int currentHeight = maxHeight / 2; // Start somewhere in the middle

        for (int x = 0; x < width; x++)
        {
            // Small random step up/down/flat
            int change = rand.Next(-1, 2); // -1, 0, or +1
            currentHeight += change;

            // Clamp the height so it stays on screen
            if (currentHeight < 1)
                currentHeight = 1;
            if (currentHeight > maxHeight)
                currentHeight = maxHeight;

            heights[x] = currentHeight;
        }

        return heights;
    }

    // Draw the mountains row by row from top to bottom
    static void DrawMountains(int[] heights, int maxHeight)
    {
        for (int y = maxHeight; y >= 1; y--)
        {
            for (int x = 0; x < heights.Length; x++)
            {
                if (heights[x] >= y)
                    Console.Write("^");  // mountain
                else
                    Console.Write(" ");  // sky
            }

            Console.WriteLine();
        }
    }
}
        


using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace DeathDungeon
{
    // ████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████
    // ██                                                                      SECTIONS                                                                                      ██
    // ██ _________________________________________________________________________________________________________________________________________________________________  ██
    // ██ UTILITY = 0      | ConsoleAnsi and ConsoleInit for initiating console window and adding custom colors                                                              ██
    // ██ TERRAIN = 1      | Deals with the Grid and terrainObjets (stationary objects that wont move such as walls etc)                                                     ██
    // ██                                                                                                                                                                    ██
    // ████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████


    // █████████████████████████████████████████████████████████████████████ UTILITY ████████████████████████████████████████████████████████████████ 0 █████████████████████


    // ConsoleAnsi ger tillgång till some serious extra colors n shid! Kolla wiki för https://en.wikipedia.org/wiki/ANSI_escape_code
    public static class ConsoleAnsi
    {
        // Foreground via RGB – typ "text color"
        //\x1b[38;2;R;G;Bm → x1b[ = escape code för ansi | 38/48 = foreground/background | 2 = 24-bit RGB mode | R;G;Bm = RGB värden
        // FG -foreground color går att sätta med HEX värden #00-FF eller med 0-255.
        public static string Fg(byte r, byte g, byte b)
        {
            return "\x1b[38;2;" + r + ";" + g + ";" + b + "m";
        }
        public static string Fg(string hex)
        {
            if (hex.StartsWith("#")) hex = hex.Substring(1);
            if (hex.Length != 6) throw new ArgumentException("Hex must be 6 characters.", nameof(hex));

            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            return Fg(r, g, b);
        }

        // Background om du vill sen (just in case)
        public static string Bg(byte r, byte g, byte b)
        {
            return "\x1b[48;2;" + r + ";" + g + ";" + b + "m";
        }
   
        // Reset – back to default, alltid nice att avsluta med denna
        public const string Reset = "\x1b[0m";
    }
    //Console init för att Initiera fönstret och för att få bättre färger. 
    public static class ConsoleInit
    {
        // checkout : https://www.reddit.com/r/csharp/comments/vuhz6i/fast_console_output_using_a_buffer/ for references. 
        // lite win32 interop – no stress, bara koppla på VT. Annars tolkas ANSIescape som vanlig text.
        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        private const uint ENABLE_PROCESSED_OUTPUT = 0x0001;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);   // ref: https://learn.microsoft.com/en-us/windows/console/getconsolemode

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        public static void InitConsole()
        {
            // Unicode så vi kan rita █ ░ osv
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // VT-mode (Windows-only grej). På andra OS funkar ANSI ändå.
            try
            {
                var handle = GetStdHandle(STD_OUTPUT_HANDLE);
                if (handle != IntPtr.Zero && GetConsoleMode(handle, out uint mode))
                {
                    mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | ENABLE_PROCESSED_OUTPUT;
                    SetConsoleMode(handle, mode);
                }
            }
            catch
            {
                // meh – om detta failar så kör vi ändå
            }
        }
    }

    // █████████████████████████████████████████████████████████████████████ TERRAIN ████████████████████████████████████████████████████████████████ 1 █████████████████████

    // Terrain object skiljar sig från specen. Detta för att kunna använda en GRID för terrängen. så wall kommer extenda Terrain object istället.
    public abstract class TerrainObject
    {
        public string Name { get; }          // namn – mest för debug/klarhet
        public char Symbol { get; }          // unicode char vi ritar i grid
        public string AnsiInView { get; }    // färg när vi ser den nu
        public string AnsiDiscovered { get; } // färg när upptäckt men ej i view

        protected TerrainObject(string name, char symbol, string ansiInView, string ansiDiscovered)
        {
            Name = name;
            Symbol = symbol;
            AnsiInView = ansiInView ?? "\x1b[39m";        // default fg
            AnsiDiscovered = ansiDiscovered ?? "\x1b[90m"; // dim fg
        }

        // enkel logik – discoveredYet = true -> använd discovered-färg
        public string GetAnsi(bool discoveredYet)
        {
            if (discoveredYet) return AnsiDiscovered;
            return AnsiInView;
        }

        // tiny helper – "ansi + symbol + reset", nice när man printar
        public string RenderFragment(bool discoveredYet)
        {
            return GetAnsi(discoveredYet) + Symbol + ConsoleAnsi.Reset;
        }
    }

    //TerrainGrid innehåller hela kartan. Det behövs för att visa special unicode symboler, likt ninja, råtta, snek etc tar upp 2 tecken.
    public class TerrainGrid
    {
        public int Width { get; }
        public int Height { get; }

        // 2D-array – [x,y], vi kör referenser till TerrainObject
        private readonly TerrainObject?[,] _tiles;

        public TerrainGrid(int width, int height)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException("grid size måste vara > 0");
            Width = width;
            Height = height;
            _tiles = new TerrainObject?[width, height];
        }

        // enkel indexer – sweet syntactic sugar: grid[x,y] = ...
        public TerrainObject? this[int x, int y]
        {
            get => _tiles[x, y];
            set => _tiles[x, y] = value;
        }

        // bounds check helper – bra för framtida safe ops
        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        // fill hela grid med samma tile (t.ex. golv senare)
        public void Fill(TerrainObject tile)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    _tiles[x, y] = tile;
        }
    }

    //kanske onödigt, men om vi vill ha fler banor så är det bra, annars kunde detta sköts i main och main kunde slängt runt Gridden
    public class GameWorld
    {
        public TerrainGrid Grid { get; }

        public GameWorld(int width, int height)
        {
            Grid = new TerrainGrid(width, height);
        }
    }

    // MAIN PROGRAM WITH LOOP
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Console.Title = "DeathDungeon";
            ConsoleInit.InitConsole(); // enable VT + UTF8 etc

            // skapa en world bara för att se att allt funkar
            var world = new GameWorld(20, 8);

            // bygg en enkel border av väggar – mest compile/runtest
            var wall = new Wall(); // reuse same instance överallt (ok, den är immutable för våra fields)
            for (int x = 0; x < world.Grid.Width; x++)
            {
                world.Grid[x, 0] = wall;
                world.Grid[x, world.Grid.Height - 1] = wall;
            }
            for (int y = 0; y < world.Grid.Height; y++)
            {
                world.Grid[0, y] = wall;
                world.Grid[world.Grid.Width - 1, y] = wall;
            }

            // printa bara ett litet “Phase 1 clear” – ingen riktig render än
            Console.WriteLine($"{ConsoleAnsi.Fg(0, 200, 160)}Phase 1 ready ✓{ConsoleAnsi.Reset}");
            Console.WriteLine("Grid created (20x8) and walls placed on the border.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
        }
    }
}

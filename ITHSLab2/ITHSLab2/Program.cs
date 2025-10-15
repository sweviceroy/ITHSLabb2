

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.IO;
using System.Linq;


namespace DeathDungeon
{
    // ████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████
    // ██                                                                      SECTIONS                                                                                      ██
    // ██ _________________________________________________________________________________________________________________________________________________________________  ██
    // ██ UTILITY = 0      | ConsoleAnsi and ConsoleInit for initiating console window and adding custom colors                                                              ██
    // ██ TERRAIN = 1      | Deals with the Grid and terrainObjets (stationary objects that wont move such as walls etc)                                                     ██
    // ██ CORE 8  = 2      | This will contain the world, the main loop, and core funtions                                                                                   ██
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

    //GUI to draw
    public static class TopGUI
    {
        private const string top1 = "╔════════════════════ Death Dungeon v.1.0═════════════════════╗";
        private const string top2 = "║═════════════════╦═══════════════╦═══════════════════════════║";
        private const string top3 = "║   R A N G E R   ║   I T E M S   ║   H E A L T H             ║";
        private const string top4 = "║▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔║";
        private const string top5 = "╚═════════════════════════════════════════════════════════════╝";

        // TODO: add player to print his stats
        public static void printTOPGUI()
        {
            Console.WriteLine(top1);
            Console.WriteLine(top2);
            Console.WriteLine(top3);
            Console.WriteLine(top4);
            Console.WriteLine(top5);
        }
    }

    // some usefull symbols for furure ref. 
    public static class Symbols
    {
        // === Player & creatures ===
        public const string Player = "🧍";     // vår hjälte
        public const string Ninja = "🥷";      // sneaky typ
        public const string Snake = "🐍";      // klassisk dungeon snake
        public const string Rat = "🐀";        // litet äckel i hörnen

        // === Weapons ===
        public const string Sword = "🗡";      // standard sword
        public const string Dagger = "🔪";     // knife / dagger
        public const string Bow = "🏹";        // bow (same glyph works fine)
        public const string Axe = "🪓";        // axe
        public const string Mace = "⚒";       // hammer/mace hybrid
        public const string Staff = "🪄";      // magic staff / wand

        // === Armor & gear ===
        public const string Shield = "🛡";     // shield
        public const string Helmet = "🪖";     // combat helmet
        public const string Armor = "🥋";      // martial arts gi = looks like leather armor
        public const string LeatherArmor = "🥋"; // alias, samma symbol
        public const string Cloak = "🧥";      // cloak/coat for mage vibes
        public const string Boots = "🥾";      // boots
        public const string Gloves = "🧤";     // gloves

        // === Items & misc ===
        public const string Potion = "🧪";
        public const string Key = "🗝";
        public const string Gem = "💎";

        // === Environment ===
        public const string Wall = "🧱";
        public const string DoorClosed = "🚪";
        public const string Chest = "💰";
    }

    // █████████████████████████████████████████████████████████████████████ TERRAIN ████████████████████████████████████████████████████████████████ 1 █████████████████████

    // Terrain object skiljar sig från specen. Detta för att kunna använda en GRID för terrängen. så wall kommer extenda Terrain object istället.
    // Bas för alla terräng-typer (vägg, golv, etc.)
    // Håller bara visuals: symbol + två färger (in-view vs discovered).
    // Bas för terräng-typer (vägg, golv, etc.)
    // Håller visuals + en "detected" flag per instans.
    public abstract class TerrainObject
    {
        public string Name { get; }
        public char Symbol { get; }

        public string AnsiInView { get; protected set; }
        public string AnsiOutOfView { get; protected set; }

        public bool Detected { get; private set; } // har vi sett den än?

        // Ny property – anger om symbolen är "wide" (emoji eller 2-kolumn)
        public virtual bool IsWide => false;

        protected TerrainObject(
            string name,
            char symbol,
            string ansiInView,
            string ansiOutOfView)
        {
            Name = name;
            Symbol = symbol;
            AnsiInView = ansiInView ?? "\x1b[39m";
            AnsiOutOfView = ansiOutOfView ?? "\x1b[90m";
        }

        public void MarkDetected() => Detected = true;

        // helper: välj färg beroende på inView/Detected
        public string GetAnsi(bool inView)
        {
            if (inView)
                return AnsiInView;
            if (Detected)
                return AnsiOutOfView;
            return string.Empty;
        }

        // rendera en enda "glyph" med färg
        public string RenderFragment(bool inView)
        {
            string ansi = GetAnsi(inView);
            if (string.IsNullOrEmpty(ansi))
                return string.Empty;

            return ansi + Symbol + "\x1b[0m";
        }
    }

    //wall extendar terrainObject, Alla har en default färg utanför view-range och får en random färg när den är i view. (Dyrt att skapa ett object för varje cell, men 
    // det fungerar för nu, TODO: gör 10 olika walltyper och låt dem illustreras av dessa istället)

    public sealed class Wall : TerrainObject
    {
        private const char DefaultSymbol = '█';
        public static readonly string DiscoveredAnsi = ConsoleAnsi.Fg(90, 90, 95);
        private static readonly Random _rng = new Random();

        public override bool IsWide => false; // väggar är smala, printas 2× i Renderer

        public Wall()
            : base(
                name: "Wall",
                symbol: DefaultSymbol,
                ansiInView: MakeJitteredInView(),
                ansiOutOfView: DiscoveredAnsi)
        { }

        private static string MakeJitteredInView()
        {
            int r = 180, g = 180, b = 185;
            int jR = Clamp01(r + RandJitter(8));
            int jG = Clamp01(g + RandJitter(8));
            int jB = Clamp01(b + RandJitter(8));
            return ConsoleAnsi.Fg((byte)jR, (byte)jG, (byte)jB);
        }

        private static int RandJitter(int range) => _rng.Next(-range, range + 1);

        private static int Clamp01(int v)
        {
            if (v < 0) return 0;
            if (v > 255) return 255;
            return v;
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

    // █████████████████████████████████████████████████████████████████████ CORE AND MAIN LOOP █████████████████████████████████████████████████████ 2 █████████████████████

    // our world
    public class GameWorld
    {
        public TerrainGrid Grid { get; }

        // spawn – enkel storage för senare när vi gör en Player
        public int? PlayerSpawnX { get; set; }
        public int? PlayerSpawnY { get; set; }

        public GameWorld(int width, int height)
        {
            Grid = new TerrainGrid(width, height);
        }
    }
    // LevelLoader for loading the current level
    public static class LevelLoader
    {
        public static GameWorld Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Level file not found", path);

            var lines = File.ReadAllLines(path);
            if (lines.Length == 0)
                throw new InvalidOperationException("Level file is empty");

            // hitta längsta raden manuellt istället för LINQ
            int width = FindLongestLineLength(lines);
            int height = lines.Length;

            var world = new GameWorld(width, height);

            for (int y = 0; y < height; y++)
            {
                var line = lines[y];

                for (int x = 0; x < width; x++)
                {
                    char c = (x < line.Length) ? line[x] : ' ';

                    switch (c)
                    {
                        case '#':
                            world.Grid[x, y] = new Wall();
                            break;

                        case '@':
                            world.PlayerSpawnX = x;
                            world.PlayerSpawnY = y;
                            break;

                        case ' ':
                        default:
                            break;
                    }
                }
            }

            return world;
        }

        // Enkel helper – går igenom alla rader och hittar längsta
        private static int FindLongestLineLength(string[] lines)
        {
            int longest = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                int len = lines[i].Length;
                if (len > longest)
                    longest = len;
            }
            return longest;
        }
    }

    // Renderer är den som skriver ut das Spielvärld
    public static class Renderer
    {
        private const int CELL_W = 2; // varje gridpunkt tar upp två kolumner

        public static void DrawOnce(GameWorld world)
        {
            Console.SetCursorPosition(0, 0);
            TopGUI.printTOPGUI();
            DrawMap(world);
        }

        private static void DrawMap(GameWorld world)
        {
            int headerHeight = 5;
            Console.SetCursorPosition(0, headerHeight);

            for (int y = 0; y < world.Grid.Height; y++)
            {
                var line = new StringBuilder();

                for (int x = 0; x < world.Grid.Width; x++)
                {
                    var tile = world.Grid[x, y];

                    if (tile == null)
                    {
                        // tom ruta = två spaces
                        line.Append(' ', CELL_W);
                    }
                    else
                    {
                        // allt i view just nu (testläge)
                        string frag = tile.RenderFragment(inView: true);

                        if (tile.IsWide)
                        {
                            // emoji / dubbelbred symbol – skriv bara en gång
                            line.Append(frag);
                        }
                        else
                        {
                            // vanlig char – skriv två gånger för att fylla cellbredden
                            line.Append(frag);
                            line.Append(frag);
                        }
                    }
                }

                Console.Write(line.ToString());
                Console.Write(ConsoleAnsi.Reset);
                Console.WriteLine();
            }
        }
    }

    // MAIN PROGRAM WITH LOOP
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Console.Title = "DeathDungeon";
            ConsoleInit.InitConsole(); // UTF8 + VT

            // Viktigt i VS:
            // - Lägg "Assets/Level1.txt" i projektet
            // - Properties: Build Action = Content, Copy to Output Directory = Copy if newer
            var world = LevelLoader.Load("Assets/Level1.txt");

            // rita en gång – header + karta under (allt in-view för test)
            Renderer.DrawOnce(world);

            Console.WriteLine();
            Console.WriteLine($"Spawn (for later): {world.PlayerSpawnX},{world.PlayerSpawnY}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
        }
    }
}

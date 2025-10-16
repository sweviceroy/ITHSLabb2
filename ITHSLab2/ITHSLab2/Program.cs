

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.IO;
using System.Linq;
using System.Collections.Generic;

// TODO: (Phase 5.5) Add threaded input handling so Console.ReadKey() runs on a separate thread to stop the keys from getting stuck!
//       See Microsoft Docs → https://learn.microsoft.com/en-us/dotnet/api/system.threading.thread
//       For background: Wikipedia “Thread (computing)” → https://en.wikipedia.org/wiki/Thread_(computing)

// TODO: (Phase 6 / Nice2Have) Replace simple radius vision with proper FOV.
//       Options:
//         A) Raycast / Bresenham lines from player – simple but slower.
//         B) Recursive Shadowcasting – classic roguelike method, very efficient.
//       See: https://www.roguebasin.com/index.php/Field_of_View for algorithms & examples.


namespace DeathDungeon
{
    // ████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████
    // ██                                                                      SECTIONS                                                                                      ██
    // ██ _________________________________________________________________________________________________________________________________________________________________  ██
    // ██ UTILITY = 0      | ConsoleAnsi and ConsoleInit for initiating console window and adding custom colors                                                              ██
    // ██ TERRAIN = 1      | Deals with the Grid and terrainObjets (stationary objects that wont move such as walls etc)                                                     ██
    // ██ CORE 8  = 2      | This will contain the world, the main loop, and core funtions                                                                                   ██
    // ██ LevelElem 3      | Will contain the player, the enemies and all other dynamic objects that are in the level                                                        ██
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

    // Terrain object skiljer sig från specen. Detta för att kunna använda en GRID för terrängen. så wall kommer extenda Terrain object istället.
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



    // █████████████████████████████████████████████████████████████████████ CORE AND MAIN LOOP █████████████████████████████████████████████████████ 2 █████████████████████

    // GameWorld = spelvärlden, kanske onödigt, men om vi vill ha fler banor så är det bra, annars kunde detta sköts i main och main kunde slängt runt Gridden
    public class GameWorld
    {
        public TerrainGrid Grid { get; }

        public int? PlayerSpawnX { get; set; }
        public int? PlayerSpawnY { get; set; }

        public LevelData Level { get; } = new LevelData();

        // referens till spelaren – sätts i Program när player skapas
        public Player? Player { get; set; }

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

            int width = FindLongestLineLength(lines);
            int height = lines.Length;

            var world = new GameWorld(width, height);

            bool foundSpawn = false;

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
                            foundSpawn = true;
                            break;

                        case 'r':
                        case 'R':
                            world.Level.Add(new Rat(x, y));
                            break;

                        case 's':
                        case 'S':
                            world.Level.Add(new Snake(x, y));
                            break;

                        case ' ':
                        default:
                            break;
                    }
                }
            }

            if (!foundSpawn)
                throw new InvalidOperationException("Level is missing a player spawn '@' – cannot start.");

            return world;
        }

        private static int FindLongestLineLength(string[] lines)
        {
            int longest = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                int len = lines[i].Length;
                if (len > longest) longest = len;
            }
            return longest;
        }
    }

    // Renderer är den som skriver ut das Spielvärld
    // NOTE: The lab spec använder LevelElement.Draw() med  ConsoleColor + single-width chars.
    //       Vi kör istället en central Renderer med ANSI och Unicode (emoji, färg-tint, 2-kolumns layout)
    //       för att få mer kontroll över färger, ljus, och stil – samma logic, bara mer advanced rendering.
    public static class Renderer
    {
        private const int CELL_W = 2;  // varje gridpunkt = 2 kolumner
        private const int HEADER_H = 5;  // höjd på TopGUI (antal rader)
        private const int VISION_R = 8;  // enkel syn-radie (kan bytas till FOV senare)

        public static void Draw(GameWorld world, Player player)
        {
            Console.SetCursorPosition(0, 0);
            Console.Write("\x1b[?25l"); // hide cursor

            TopGUI.printTOPGUI();
            DrawMap(world, player);
        }

        private static void DrawMap(GameWorld world, Player player)
        {
            Console.SetCursorPosition(0, HEADER_H);

            int targetWidth = Console.WindowWidth;
            int mapPixelWidth = world.Grid.Width * CELL_W;

            for (int y = 0; y < world.Grid.Height; y++)
            {
                var line = new StringBuilder(mapPixelWidth + 32);

                for (int x = 0; x < world.Grid.Width; x++)
                {
                    bool inView = InVisionRadius(player.X, player.Y, x, y, VISION_R);
                    var tile = world.Grid[x, y];

                    // === Player inline (ritas överst) ===
                    if (player.X == x && player.Y == y)
                    {
                        string fragP = player.ColorAnsi + player.Symbol + ConsoleAnsi.Reset;
                        if (player.IsWide) line.Append(fragP);
                        else { line.Append(fragP); line.Append(fragP); }
                        tile?.MarkDetected();
                        continue;
                    }

                    // === Enemy / LevelElement inline (ovanför terrain) ===
                    var elem = world.Level.GetAt(x, y);
                    if (elem != null && elem.IsAlive)
                    {
                        // ANSI färg för enemies kan läggas senare; nu default fg
                        string fragE = elem.Symbol + ConsoleAnsi.Reset;
                        if (elem.IsWide) line.Append(fragE);
                        else { line.Append(fragE); line.Append(fragE); }

                        if (inView) tile?.MarkDetected(); // om vägg under, markera sedd
                        continue;
                    }

                    // === Terrain (väggar etc) ===
                    if (tile == null)
                    {
                        line.Append(' ', CELL_W);
                    }
                    else
                    {
                        if (inView) tile.MarkDetected();

                        string frag = tile.RenderFragment(inView);

                        if (string.IsNullOrEmpty(frag))
                            line.Append(' ', CELL_W);
                        else if (tile.IsWide)
                            line.Append(frag);
                        else
                        {
                            line.Append(frag);
                            line.Append(frag);
                        }
                    }
                }

                // pad to full window width to wipe right gutter (no ghost prints)
                int visibleLen = GetVisibleLength(line);
                int pad = Math.Max(0, targetWidth - visibleLen);
                if (pad > 0) line.Append(' ', pad);

                Console.Write(line.ToString());
                Console.Write(ConsoleAnsi.Reset);
                Console.WriteLine();
            }
        }

        private static bool InVisionRadius(int px, int py, int x, int y, int r)
        {
            int dx = x - px;
            int dy = y - py;
            return (dx * dx + dy * dy) <= (r * r);
        }

        // strip ANSI to estimate visible width for padding
        private static int GetVisibleLength(StringBuilder sb)
        {
            int count = 0;
            bool inEsc = false;

            for (int i = 0; i < sb.Length; i++)
            {
                char c = sb[i];

                if (!inEsc)
                {
                    if (c == '\x1b') inEsc = true;
                    else count++;
                }
                else
                {
                    if (c == 'm') inEsc = false;
                }
            }

            return count;
        }
    }

    //GameInput för antingen WASD eller arrows. Notera att spelet är realtojm!
    public static class GameInput
    {
        // returnerar dx,dy och om vi ska avsluta
        public static (int dx, int dy, bool quit) ReadMoveIntent()
        {
            int dx = 0, dy = 0;
            bool quit = false;

            // ingen key? -> no intent
            if (!Console.KeyAvailable)
                return (dx, dy, quit);

            var key = Console.ReadKey(true).Key;

            switch (key)
            {
                // WASD
                case ConsoleKey.W: dy = -1; break;
                case ConsoleKey.S: dy = 1; break;
                case ConsoleKey.A: dx = -1; break;
                case ConsoleKey.D: dx = 1; break;

                // Arrow keys
                case ConsoleKey.UpArrow: dy = -1; break;
                case ConsoleKey.DownArrow: dy = 1; break;
                case ConsoleKey.LeftArrow: dx = -1; break;
                case ConsoleKey.RightArrow: dx = 1; break;

                case ConsoleKey.Escape:
                    quit = true;
                    break;
            }

            return (dx, dy, quit);
        }
    }


    // MAIN PROGRAM WITH LOOP
    internal static class Program
    {
        static void Main()
        {
            Console.Title = "DeathDungeon";
            ConsoleInit.InitConsole();

            var world = LevelLoader.Load("Assets/Level1.txt");

            if (world.PlayerSpawnX is null || world.PlayerSpawnY is null)
                throw new InvalidOperationException("No @ spawn in level (fatal).");

            var player = new Player(world.PlayerSpawnX.Value, world.PlayerSpawnY.Value);
            world.Player = player; // så enemies kan läsa pos

            const int tickMs = 200; // 5 Hz
            bool quit = false;

            while (!quit)
            {
                var (dx, dy, wantQuit) = GameInput.ReadMoveIntent();
                if (wantQuit) { quit = true; break; }

                if (dx != 0 || dy != 0)
                {
                    // bump-combat kommer i nästa steg – nu bara rörelse om ej vägg
                    player.TryMove(dx, dy, world);
                }

                // Uppdatera alla fiender (cooldowns styr när de rör sig)
                foreach (var e in world.Level.Elements)
                {
                    if (e is Enemy enemy)
                        enemy.Update(world, tickMs);
                }

                Renderer.Draw(world, player);
                Thread.Sleep(tickMs);
            }

            Console.Write("\x1b[?25h");
            Console.WriteLine();
            Console.WriteLine("Goodbye from DeathDungeon!");
        }
    }

    // █████████████████████████████████████████████████████████████████████ LevelElements      █████████████████████████████████████████████████████ 3 █████████████████████

    //Our player class! 
    public class Player
    {
        public string Name { get; set; } = "Ranger"; // kan bytas sen
        public int X { get; private set; }
        public int Y { get; private set; }

        // ninja – dubbelbred emoji, ser cool ut ;)
        public string Symbol { get; } = "🥷";
        public bool IsWide => true;

        // lite stats för header (placeholder)
        public int HP { get; set; } = 12;
        public int MaxHP { get; set; } = 12;
        public int STR { get; set; } = 3;

        // färg för spelaren 
        public string ColorAnsi { get; } = ConsoleAnsi.Fg(0, 200, 200);

        public Player(int startX, int startY)
        {
            X = startX;
            Y = startY;
        }

        // försök flytta – very simple: blockeras av väggar och bounds
        public bool TryMove(int dx, int dy, GameWorld world)
        {
            int nx = X + dx;
            int ny = Y + dy;

            if (!world.Grid.InBounds(nx, ny))
                return false;

            // om det är en vägg → nope
            if (world.Grid[nx, ny] is Wall)
                return false;

            X = nx;
            Y = ny;
            return true;
        }
    }

    //LevelElement för allting i dungeon. Fiender, object etc. Notera att spelaren inte är ett LevelElement!
    public abstract class LevelElement
    {
        // pos i grid (tile coords)
        public int X { get; protected set; }
        public int Y { get; protected set; }

        // basic id/info
        public string Name { get; protected set; } = "Element";
        public bool IsAlive { get; protected set; } = true; // enemies/items kan “försvinna”

        // === Spec-mode (enkelt): char + ConsoleColor ===
        public char CharGlyph { get; protected set; } = '?';
        public ConsoleColor Color { get; protected set; } = ConsoleColor.White;

        // === Engine-mode (vår renderer): string + wide-flag (emoji etc) ===
        public string Symbol { get; protected set; } = "@";
        public virtual bool IsWide => false; // default: single-width

        protected LevelElement(int x, int y)
        {
            X = x;
            Y = y;
        }

        // Minimal uppdatering – override i enemies senare
        public virtual void Update(GameWorld world, double dtMs)
        {
            // default: no-op
        }

        // Enkel “spec draw” för ConsoleColor+char
        public virtual void DrawSpecMode(int headerOffset = 0)
        {
            Console.ForegroundColor = Color;
            Console.SetCursorPosition(X, headerOffset + Y);
            Console.Write(CharGlyph);
            Console.ResetColor();
        }
    }

    // LevelData har koll på allting i kartan! 
    public class LevelData
    {
        private readonly List<LevelElement> _elements = new List<LevelElement>();
        public IReadOnlyList<LevelElement> Elements => _elements;

        public void Add(LevelElement e) => _elements.Add(e);
        public void Remove(LevelElement e) => _elements.Remove(e);

        // hitta första element på (x,y) – basic lookup (räcker för små maps) Kanske optimera sen?
        public LevelElement? GetAt(int x, int y)
        {
            for (int i = 0; i < _elements.Count; i++)
            {
                var e = _elements[i];
                if (e.IsAlive && e.X == x && e.Y == y)
                    return e;
            }
            return null;
        }
    }

    // Enemy class_______________________________________________________ ENEMIES ________________________________
    public abstract class Enemy : LevelElement
    {
        // === Combat stats (dice kopplas in i nästa steg) ===
        public int HP { get; protected set; } = 1;
        public int MaxHP { get; protected set; } = 1;

        // placeholder tills Dice-klassen kommer i nästa steg
        // public Dice AttackDice { get; protected set; }
        // public Dice DefenceDice { get; protected set; }

        // === Timing (realtime): agera med pauser mellan ===
        public double CooldownMs { get; private set; } = 0; // när <= 0 → får agera
        public int MinActMs { get; protected set; } = 300;
        public int MaxActMs { get; protected set; } = 600;

        // RNG – shared per typ (simple enough i vår single-thread loop)
        protected static readonly Random Rng = new Random();

        protected Enemy(int x, int y) : base(x, y)
        {
        }

        // Update kallas varje tick – world.Player sätts i Program när spelaren skapas
        public override void Update(GameWorld world, double dtMs)
        {
            if (!IsAlive) return;

            // räkna ner cooldown
            CooldownMs -= dtMs;
            if (CooldownMs > 0) return;

            // hämta player (kan vara null innan Program hunnit koppla)
            var player = world.Player;

            // låt subklassen bestämma rörelse-intent
            DecideAction(world, player, out int dx, out int dy);

            // försök röra dig – bump-combat kopplas in senare
            if (dx != 0 || dy != 0)
            {
                TryStep(world, dx, dy);
            }

            // rulla ny cooldown (liten variation känns “levande”)
            CooldownMs = Rng.Next(MinActMs, MaxActMs + 1);
        }

        // Subklasser bestämmer riktning (dx,dy). Kan också stå still (0,0).
        protected abstract void DecideAction(GameWorld world, Player? player, out int dx, out int dy);

        // Basic rörelse med kollision mot väggar och andra LevelElements (och spelaren)
        protected bool TryStep(GameWorld world, int dx, int dy)
        {
            int nx = X + dx;
            int ny = Y + dy;

            if (!world.Grid.InBounds(nx, ny))
                return false;

            // vägg i grid? nope
            if (world.Grid[nx, ny] is Wall)
                return false;

            // någon annan LevelElement på platsen? block (vi lägger combat senare)
            if (world.Level.GetAt(nx, ny) != null)
                return false;

            // spelaren där? block just nu (combat senare)
            if (world.Player != null && world.Player.X == nx && world.Player.Y == ny)
                return false;

            // ok move
            X = nx;
            Y = ny;
            return true;
        }
    }

    // RAT
    public class Rat : Enemy
    {
        public Rat(int x, int y) : base(x, y)
        {
            Name = "Rat";
            CharGlyph = 'r';
            Color = System.ConsoleColor.DarkYellow;
            Symbol = "r";      // smal glyph (IsWide=false)
            // stats (enligt spec i nästa steg, när Dice finns). Här bara HP:
            MaxHP = HP = 10;

            // tempo – rats är lite snabbare
            MinActMs = 180;
            MaxActMs = 300;
        }

        protected override void DecideAction(GameWorld world, Player? player, out int dx, out int dy)
        {
            // enkel drunk-walk: välj slumpmässig riktning av 4
            int dir = Rng.Next(4);
            dx = 0; dy = 0;
            switch (dir)
            {
                case 0: dy = -1; break; // upp
                case 1: dy = 1; break; // ner
                case 2: dx = -1; break; // vänster
                case 3: dx = 1; break; // höger
            }
        }
    }

    //SNEK
    public class Snake : Enemy
    {
        public Snake(int x, int y) : base(x, y)
        {
            Name = "Snake";
            CharGlyph = 's';
            Color = System.ConsoleColor.Green;
            Symbol = "s";      // smal glyph
            MaxHP = HP = 25;

            // långsammare tempo
            MinActMs = 350;
            MaxActMs = 600;
        }

        protected override void DecideAction(GameWorld world, Player? player, out int dx, out int dy)
        {
            dx = 0; dy = 0;

            if (player == null) return; // ingen spelare kopplad än → stå still

            int px = player.X, py = player.Y;
            int dist2 = (px - X) * (px - X) + (py - Y) * (py - Y);

            // om spelaren är mer än 2 rutor bort → stå still (spec)
            if (dist2 > 2 * 2)
            {
                return; // dx=dy=0
            }

            // annars: rör dig bort från spelaren (1 steg)
            int sx = 0, sy = 0;
            if (X < px) sx = -1; else if (X > px) sx = 1;
            if (Y < py) sy = -1; else if (Y > py) sy = 1;

            // välj axel som ger mest separation – enkel heuristik
            if (System.Math.Abs(px - X) >= System.Math.Abs(py - Y))
            {
                dx = sx; dy = 0;
            }
            else
            {
                dx = 0; dy = sy;
            }

            // fallback: om blockerat i Update() så blir det ingen move (det räcker nu)
        }
    }


}// END NAMESPACE_________________________________


// END NAMESPACE

// ============================================================
// === D E A T H   D U N G E O N   P H A S E   T O D O L I S T ===
// ============================================================
//
// 0️⃣  Bootstrap
//     - Setup project, namespaces, and basic file structure.
//
// 1️⃣  Engine Foundation
//     - ANSI color helpers, ConsoleInit, TerrainObject, Wall, Grid, GameWorld.
//
// 2️⃣  Console GUI
//     - TopGUI header box and layout for stats, map frame planning.
//
// 3️⃣  Renderer v0
//     - Draw header + map, no player yet. Basic console rendering system.
//
// 4️⃣  Level Loading
//     - Read .txt maps with '#' for walls and '@' for player spawn.
//
// 5️⃣  Player & Movement
//     - Add 🥷 player, movement input, vision radius, and fog memory.
//
// 6️⃣  Vision / FOV  (Nice2Have)
//     - Replace radius with true Field Of View (raycast or shadowcasting).
//
// 7️⃣  Combat & Stats
//     - Player HP/STR display, dice rolls, attack logic.
//
// 8️⃣  Enemies & AI
//     - Enemy classes, simple pathfinding, and updates per tick.
//
// 9️⃣  Polish & Nice2Have
//     - Threaded input, sounds, GUI updates, map scrolling, etc.
//
// ============================================================

// More references:
/*
* https://www.youtube.com/watch?v=3GXzS5vzeC4       Console apps, Spectre.Console  en svensk kille
* https://en.wikipedia.org/wiki/ANSI_escape_code    Beautiful custom colors
* https://www.reddit.com/r/csharp/comments/vuhz6i/fast_console_output_using_a_buffer/ & https://www.reddit.com/r/csharp/comments/vts5t3/consolerendersunrise/ - Using screenbuffer to print faster
*/
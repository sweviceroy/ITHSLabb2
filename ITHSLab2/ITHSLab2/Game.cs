// ███████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████
// DragonsDestructiveDeathDungeon 
//  
//  IMPORTS first
//  ________________________________________________________________
//  SECTION INDEX:
//  █ 01 – CORE SYSTEMS       Program, LevelData, Renderer
//  █ 02 – GAME ENTITIES      LevelElement, Wall, Enemy, Rat, Snek, Player
//  █ 03 – UTILITY CLASSES    Dice
//
//  NOTE: Bara MVP nu! Se till att bli godkänd innan real-time projektet kan fortsätta.
// ██████████████████████████████████████████████████████████████████████████████████████████████████████████ 00 ███████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████

using System;                           // basic C# core 
using System.Collections.Generic;       // List  – LevelData, Game, Enemies, Log system
using System.IO;                        // File extraction – LevelData.Load()
using System.Linq;                      // LevelData, Game loops
using System.Numerics;                  // Vector2/3 etc. Could use for Unity like distance calc.
using System.Text;                      // Encoding (Console, Stringfunctionality – Game (Console.OutputEncoding)


namespace DragonsDestructiveDeathDungeon
{

    // █████████████████████████████ CORE SYSTEMS ███████████████████████████████████████████████████████████ 01 █████████████

    // ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄ CLASS GAME ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄

    // ============================================================
    // Game.cs - MAIN GAME LOOP
    // ============================================================
    // ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀ CLASS START ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀
    public static class Game
    {
        //_____________ COMBAT LOG (senaste N rader) _______________________________________
        private static List<string> _log = new List<string>();
        private const int MaxLogLines = 4;
        public static void Log(string message)
        {
            _log.Add(message);
            if (_log.Count > MaxLogLines)
            {
                _log.RemoveAt(0); 
            }
        }
        /// <summary> Renderer läser loggen via denna </summary>
        public static string[] GetLog()
        {
            return _log.ToArray();
        }

        // ________________________ START POINT __________________________________________
        static void Main(string[] args)
        {
            // setup console window
            Console.Title = "Dragons Destructive Death Dungeon";
            Console.OutputEncoding = Encoding.UTF8;
            Console.CursorVisible = false; // göm blinkande cursor

            // Load level Assets/Level1.txt           // put txt in Assets folder
            string path = "Assets/Level1.txt";
            LevelData level = new LevelData(path);

            // spaw player at startPos
            Player player = new Player(level.PlayerStart);

            // lets start the show
            bool gameRunning = true;

            // ________________________ MAIN LOOP __________________________
            while (gameRunning)
            {
                Console.Clear(); // this will make it blink. This is the whole reason why we should have used a screenbuffer =(
                
                // 1. DRAW
                // 2. INPUT
                // 3. Player Phase
                // 4. Enemy Phase

                // MOve to render section [V]
                // ----- Render  -------------------------------------------------
                Renderer.Draw(level, player);
                Renderer.DrawHud(level, player);

                // ----- INPUT -----------------------------------------------------------
                ConsoleKeyInfo key = Console.ReadKey(true);
                int dx = 0, dy = 0;

                switch (key.Key) {
                    case ConsoleKey.W:
                    case ConsoleKey.UpArrow: { dy = -1; break; }
                    case ConsoleKey.S:
                    case ConsoleKey.DownArrow: { dy = 1; break; }
                    case ConsoleKey.A:
                    case ConsoleKey.LeftArrow: { dx = -1; break; }
                    case ConsoleKey.D:
                    case ConsoleKey.RightArrow: { dx = 1; break; }
                    case ConsoleKey.Escape: {
                            gameRunning = false;
                            continue; // lämna loopen till slut-sammanfattning
                    }
                }

                // ====================== PHASE 1: PLAYER ================================
                var target = player.GetTarget(dx, dy);

                var enemyAtNewCell = level.GetEnemyAt(target.tx, target.ty);
                if (enemyAtNewCell != null && enemyAtNewCell.IsAlive)
                {
                    ResolvePlayerAttack(level, player, enemyAtNewCell, target.tx, target.ty);
                }
                else
                {
                    if (!level.IsBlockedByWallOrEnemy(target.tx, target.ty))
                    {
                        player.X = target.tx;
                        player.Y = target.ty;
                    }
                }

                if (!player.IsAlive)
                {
                    ShowSummaryAndExit(level, player, "You died!");
                }

                // ====================== PHASE 2: ENEMIES (STABLE) ======================
                // replaced LINQ ToList() with stable snapshot helper for iteration
                var enemiesThisTurn = GetEnemyTempList(level);

                foreach (var e in enemiesThisTurn)
                {
                    if (!e.IsAlive) { continue; }

                    e.Update(level, player);

                    if (!player.IsAlive)
                    {
                        ShowSummaryAndExit(level, player, $"You were slain by a {e.Name}!");
                    }
                }
            }

            // Quit via ESC → visa sammanfattning
            ShowSummaryAndExit(level, player, "You quit the dungeon.");
        }

        //_____________ PLAYER COMBAT ________________________________________
        private static void ResolvePlayerAttack(LevelData level, Player player, Enemy enemy, int tx, int ty)
        {
            int a1 = player.AttackDice.Throw();
            int d1 = enemy.RollDefence();
            int dmgToEnemy = Math.Max(0, a1 - d1);

            Log($"You attack {enemy.Name}: ATT {a1} vs DEF {d1} → {dmgToEnemy} dmg");

            bool enemyDied = enemy.TakeDamage(dmgToEnemy);

            if (enemyDied)
            {
                Log($"You slay the {enemy.Name}!");
                level.RemoveEnemy(enemy);
                player.X = tx;
                player.Y = ty;
                return;
            }

            int a2 = enemy.RollAttack();
            int d2 = player.DefenceDice.Throw();
            int dmgToPlayer = Math.Max(0, a2 - d2);

            Log($"{enemy.Name} counterattacks: ATT {a2} vs your DEF {d2} → {dmgToPlayer} dmg");

            player.TakeDamage(dmgToPlayer);
        }

        // needed to avoid fatal crashes with null references from dead enemies.
        private static List<Enemy> GetEnemyTempList(LevelData level)
        {
            var _temp = new List<Enemy>();
            foreach (var e in level.GetEnemies())
            {
                _temp.Add(e);
            }
            return _temp;
        }

        //_____________ NICE ENDING / SUMMARY __________________________________
        public static void EndFromEnemy(LevelData level, Player player, string message)
        {
            ShowSummaryAndExit(level, player, message);
        }

        private static void ShowSummaryAndExit(LevelData level, Player player, string message)
        {
            Console.Clear();
            Console.CursorVisible = true;

            Console.WriteLine(message);
            Console.WriteLine();
            Console.WriteLine($"HP: {player.HP}");
            Console.WriteLine($"Enemies defeated: {level.EnemiesDefeated}");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
            Environment.Exit(0);
        }

    } // END CLASS Game ____________________________________________________________ END Game

    // ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄ CLASS LEVELDATA ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄

    // ============================================================
    // LevelData.cs – Håller banans data (SPLIT: Walls + Enemies)
    // Summary: Laddar Level1.txt och separerar statiska väggar från
    //          dynamiska fiender. Behåller API-kompatibilitet med:
    //          - GetFirstAt(x,y)
    //          - IsBlockedByWallOrEnemy(x,y)
    //          - GetEnemies()
    //          - RemoveEnemy(...)
    //          + SeenWalls, Size, PlayerStart, EnemiesDefeated
    // ============================================================
    // ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀ CLASS START ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀
    public sealed class LevelData
    {
        //_____________ FIELDS ______________________________________________________________
        /// <summary> Statiska väggar (ritas varje frame). </summary>
        public List<Wall> Walls = new List<Wall>();

        /// <summary> Dynamiska fiender (uppdateras/ritas per tur). </summary>
        public List<Enemy> Enemies = new List<Enemy>();

        /// <summary> Väggminne: true om väggen på [x,y] har varit synlig minst en gång. </summary>
        public bool[,] SeenWalls = new bool[1, 1];

        /// <summary> Räknare för hur många fiender som dödats. </summary>
        public int EnemiesDefeated { get; private set; } = 0;

        //_____________ PROPERTIES __________________________________________________________
        /// <summary> Tuple för bredd/höjd (width, height). </summary>
        public (int width, int height) Size { get; private set; }

        /// <summary> Spelarens startposition hittad via '@'. </summary>
        public (int x, int y) PlayerStart { get; private set; } = (0, 0);

        //_____________ CONSTRUCTORS ________________________________________________________
        public LevelData() { }

        public LevelData(string filename)
        {
            Load(filename);
        }

        //_____________ LOAD METHOD _________________________________________________________
        /// <summary>
        /// Läser in en textfil (Level1.txt) och bygger listorna.
        /// Tecken:
        ///   '#' = Wall, 'r' = Rat, 's' = Snek, '@' = PlayerStart.
        /// Initierar dessutom seenWalls med rätt storlek.
        /// </summary>
        public void Load(string filename)
        {
            Walls.Clear();
            Enemies.Clear();
            EnemiesDefeated = 0;

            string[] lines = File.ReadAllLines(filename);
            int height = lines.Length;
            int width = 0;

            // Beräkna längsta raden (bredden)
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length > width)
                {
                    width = lines[i].Length;
                }
            }

            Size = (width, height);

            // Initiera seenWalls nu när vi vet kartans storlek
            SeenWalls = new bool[Size.width, Size.height];

            // Loop igenom varje rad och kolumn i filen
            for (int y = 0; y < height; y++)
            {
                string line = lines[y];

                for (int x = 0; x < width; x++)
                {
                    char ch = (x < line.Length) ? line[x] : ' ';

                    switch (ch)
                    {
                        case '#':
                            Walls.Add(new Wall(x, y));
                            break;

                        case 'r':
                            if (!EnemyExistsAt(x, y))
                                Enemies.Add(new Rat(x, y));
                            break;

                        case 's':
                            if (!EnemyExistsAt(x, y))
                                Enemies.Add(new Snek(x, y));
                            break;

                        case '@':
                            PlayerStart = (x, y);
                            break;

                        default:
                            // tom ruta
                            break;
                    }
                }
            }
        }

        //_____________ HELPERS: BOUNDS & QUERIES __________________________________________
        public bool InBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Size.width && y < Size.height;
        }

        public Wall? GetWallAt(int x, int y)
        {
            foreach (var w in Walls)
                if (w.X == x && w.Y == y) return w;
            return null;
        }

        public Enemy? GetEnemyAt(int x, int y)
        {
            foreach (var e in Enemies)
                if (e.IsAlive && e.X == x && e.Y == y) return e;
            return null;
        }

        public LevelElement? GetFirstAt(int x, int y)
        {
            var w = GetWallAt(x, y);
            if (w != null) return w;

            var e = GetEnemyAt(x, y);
            if (e != null) return e;

            return null;
        }

        //_____________ COLLISION / PASSABILITY _____________________________________________
        public bool IsBlockedByWallOrEnemy(int x, int y)
        {
            if (!InBounds(x, y))
                return true;

            if (GetWallAt(x, y) != null)
                return true;

            if (GetEnemyAt(x, y) != null)
                return true;

            return false;
        }

        //_____________ ENEMIES API (ENUM + REMOVE) ________________________________________
        public IEnumerable<Enemy> GetEnemies()
        {
            foreach (var e in Enemies)
                yield return e;
        }

        public void RemoveEnemy(Enemy enemy)
        {
            if (Enemies.Remove(enemy))
            {
                EnemiesDefeated++;
            }
        }

        //_____________ HELPERS: ENEMY EXISTENCE ___________________________________________
        private bool EnemyExistsAt(int x, int y)
        {
            foreach (var e in Enemies)
            {
                if (e.X == x && e.Y == y)
                    return true;
            }
            return false;
        }

    } // END CLASS LevelData _____________________________________________________________ END LevelData


    // ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄ CLASS RENDERER ▄▄▄▄▄▄▄▄▄▄▄▄▄▄

    // ============================================================
    // Renderer.cs – Console-art engine + Combat Log HUD
    // Summary: Ritar kartan (walls + actors) och visar combat-loggen
    //          från Game.GetLog(). Vision = radie 5, inga "." golv.
    // ============================================================

    public static class Renderer
    {
        //_____________ MAIN DRAW __________________________________________________________
        /// <summary>
        /// Ritar hela spelbrädet.
        /// - Vision: euklidisk radie 5 (dist^2 ≤ 25).
        /// - Väggar ritas om de är synliga NU eller har setts FÖRUT (SeenWalls).
        /// - Fiender syns endast när synliga.
        /// - Golv ritas aldrig som "." (bara blank).
        /// </summary>
        public static void Draw(LevelData level, Player player)
        {
            int w = level.Size.width;
            int h = level.Size.height;

            //_____________ VISIBILITY (radie 5) __________________________________________
            bool[,] visible = new bool[w, h];
            int radius = 5;
            int r2 = radius * radius;

            int xmin = Math.Max(0, player.X - radius);
            int xmax = Math.Min(w - 1, player.X + radius);
            int ymin = Math.Max(0, player.Y - radius);
            int ymax = Math.Min(h - 1, player.Y + radius);

            for (int y = ymin; y <= ymax; y++)
            {
                for (int x = xmin; x <= xmax; x++)
                {
                    int dx = x - player.X;
                    int dy = y - player.Y;
                    if (dx * dx + dy * dy <= r2)
                        visible[x, y] = true;
                }
            }

            //_____________ DRAW LOOP ______________________________________________________
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Player ritas alltid överst om exakt denna ruta
                    if (player.X == x && player.Y == y)
                    {
                        Console.Write(player.Glyph);
                        continue;
                    }

                    // Vägg?
                    var wall = level.GetWallAt(x, y);
                    if (wall != null)
                    {
                        // Markera sedd vägg om synlig nu
                        if (visible[x, y])
                            level.SeenWalls[x, y] = true;

                        // Rita vägg om synlig nu ELLER tidigare sedd
                        if (visible[x, y] || level.SeenWalls[x, y])
                            Console.Write('#');
                        else
                            Console.Write(' ');
                        continue;
                    }

                    // Fiende?
                    var enemy = level.GetEnemyAt(x, y);
                    if (enemy != null)
                    {
                        // Fiender syns endast när i range
                        Console.Write(visible[x, y] ? enemy.Glyph : ' ');
                        continue;
                    }

                    // Tomt
                    Console.Write(' ');
                }
                Console.WriteLine();
            }
        }

        //_____________ HUD + COMBAT LOG _________________________________________________
        /// <summary>
        /// Minimal HUD under kartan:
        ///  HP, kills och senaste combat-logg-raderna (från Game).
        /// </summary>
        public static void DrawHud(LevelData level, Player player)
        {
            Console.WriteLine();
            Console.WriteLine($"HP: {player.HP}   |   Kills: {level.EnemiesDefeated}");
            Console.WriteLine("WASD / PILAR = move   |   ESC = quit");
            Console.WriteLine("────────────────────────────────────────────────────────────");

            // Visa combat-logg (nyaste längst ned)
            foreach (var line in Game.GetLog())
            {
                Console.WriteLine(line);
            }
        }
    }

    // █████████████████████████████ GAME ENTITIES ██████████████████████████████████████████████████████████ 02 █████████████

    // ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄ CLASS LEVELELEMENT ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄

    // ============================================================
    // LevelElement.cs – Abstract base class
    // Summary: Basklass för allt på kartan. Håller X/Y + glyph. ritas med Draw().
    // ============================================================
    public abstract class LevelElement
    {
        public int X { get; set; }
        public int Y { get; set; }
        public char Glyph { get; protected set; }

        protected LevelElement(int x, int y, char glyph)
        {
            X = x;
            Y = y;
            Glyph = glyph;
        }

        public abstract void Draw();

        protected void SetPos(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    // ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄ CLASS WALL  ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄

    // ============================================================
    // Wall.cs 
    // Summary: Vägg. Ärver LevelElement och blockerar allt.
    // ============================================================
    public sealed class Wall : LevelElement
    {
        public Wall(int x, int y)
            : base(x, y, '#')
        { }

        public override void Draw()
        {
            // Renderer tar hand om utskriften sen.
        }
    }

    // ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄ CLASS ENEMY ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄


    // ============================================================
    // Enemy.cs – Abstract fiend-bas  (Shared dirs + RNG helpers)
    // Summary: Gemensam logik för alla fiender + combat-hjälpare.
    // Lägger till: delade riktningar och GetRandomDirection().
    // ============================================================

    public abstract class Enemy : LevelElement
    {
        // ________________________ SHARED DIRECTION DATA _________________________________
        /// <summary>
        /// Kardinalriktningar (dx,dy) som alla fiender kan använda.
        /// </summary>
        protected static readonly (int dx, int dy)[] Directions = new (int, int)[]
        {
            (0, -1), // upp
            (0,  1), // ner
            (-1, 0), // vänster
            (1,  0), // höger
        };

        /// <summary>
        /// Delad RNG för fiende-beteenden (separat från Dice.rng).
        /// </summary>
        protected static readonly Random EnemyRng = new Random();

        /// <summary>
        /// Returnerar EN slumpad riktning från <see cref="Directions"/>.
        /// </summary>
        protected static (int dx, int dy) GetRandomDirection()
        {
            return Directions[EnemyRng.Next(Directions.Length)];
        }

        // ________________________ STATS / PROPERTIES ____________________________________
        public string Name { get; protected set; } = string.Empty;
        public int HP { get; protected set; }
        public Dice AttackDice { get; protected set; } = null!;
        public Dice DefenceDice { get; protected set; } = null!;
        public bool IsAlive => HP > 0;

        // ________________________ CTOR _________________________________________________
        protected Enemy(int x, int y, char glyph)
            : base(x, y, glyph)
        { }

        // ________________________ COMBAT HELPERS _______________________________________
        /// <summary> Slår attacktärningarna. </summary>
        public int RollAttack()
        {
            return AttackDice.Throw();
        }

        /// <summary> Slår försvarstärningarna. </summary>
        public int RollDefence()
        {
            return DefenceDice.Throw();
        }

        /// <summary> Tar skada (clamp till 0). Returnerar true om fienden dog. </summary>
        public bool TakeDamage(int damage)
        {
            if (damage <= 0) return false;
            HP = Math.Max(0, HP - damage);
            return HP == 0;
        }

        // ________________________ ABSTRACTS _____________________________________________
        public abstract void Update(LevelData level, Player player);
        public abstract override void Draw();
    }

    // ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄ CLASS RAT ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄

    // ============================================================
    // Rat.cs – The rodent of destruction and doom  (Combat log added)
    // Summary: Enkel AI – går slumpmässigt, attackerar spelaren om bredvid.
    //          Loggar tärningsslag (attack/defence) och skador för båda sidor.
    // ============================================================

    public sealed class Rat : Enemy
    {
        private static readonly System.Random rng = new System.Random();

        // ________________________ CONSTRUCTOR ___________________________________________
        public Rat(int x, int y)
            : base(x, y, 'r')
        {
            Name = "rat";
            HP = 10;
            AttackDice = new Dice(1, 6, 3);
            DefenceDice = new Dice(1, 6, 1);
        }

        // ________________________ UPDATE / AI ___________________________________________
        /// <summary>
        ///  1) Om spelaren står i angränsande ruta så attackera (logga slag och skador).
        ///  2) Annars gå 1 steg i slumpmässig riktning
        /// </summary>
        public override void Update(LevelData level, Player player)
        {
            // --- 1) Attack om spelaren står bredvid ---
            for (int i = 0; i < Directions.Length; i++)
            {
                int tx = X + Directions[i].dx;
                int ty = Y + Directions[i].dy;

                if (tx == player.X && ty == player.Y)
                {
                    // 🐀 Rat attackerar spelaren
                    int a1 = RollAttack();
                    int d1 = player.DefenceDice.Throw();
                    int dmgToPlayer = System.Math.Max(0, a1 - d1);

                    Game.Log($"Rat attacks: ATT {a1} vs your DEF {d1} - {dmgToPlayer} dmg");
                    player.TakeDamage(dmgToPlayer);

                    if (!player.IsAlive)
                    {
                        Game.EndFromEnemy(level, player, "You were slain by a rat! 🐀 \a REEEEEEEEEEE!");
                        return;
                    }

                    // 🔁 Spelaren gör en enkel counterattack
                    int a2 = player.AttackDice.Throw();
                    int d2 = RollDefence();
                    int dmgToRat = System.Math.Max(0, a2 - d2);

                    Game.Log($"You counter the rat: ATT {a2} vs DEF {d2} - {dmgToRat} dmg");
                    bool ratDied = TakeDamage(dmgToRat);

                    if (ratDied)
                    {
                        Game.Log("The rat is slain!");
                        level.RemoveEnemy(this);
                    }

                    return;
                }
            }

            // --- 2) Om inte angränsande: slumpmässig rörelse ---
            for (int tries = 0; tries < 4; tries++)
            {
                var dir = GetRandomDirection();
                int tx = X + dir.dx;
                int ty = Y + dir.dy;

                // Undvik väggar/levande fiender/out-of-bounds
                if (!level.IsBlockedByWallOrEnemy(tx, ty))
                {
                    X = tx;
                    Y = ty;
                    return;
                }
            }
        }

        // ________________________ DRAW _________________________________________________
        /// <summary>
        /// Renderer hanterar utskrift; denna gör inget.
        /// </summary>
        public override void Draw()
        {
            // handled by Renderer
        }

    } // END CLASS Rat __________________________________________________________________ END Rat

    // ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄ CLASS SNEK ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄

    // ============================================================
    // Snek.cs – (Combat log added: Flee AI + enemy-initiated combat)
    // Summary: Om ett steg skulle gå in i spelaren: attackera (counter 1x) och stå kvar.
    //          Annars: om spelaren är nära (dist ≤ 2) → ta passabelt steg som MAXIMERAR
    //          avståndet. Om långt bort (dist > 2) → stå still. Loggar alla tärningsslag.
    // ============================================================

    public sealed class Snek : Enemy
    {
        // ________________________ CONSTRUCTOR ___________________________________________
        public Snek(int x, int y)
            : base(x, y, 's')
        {
            Name = "snek";
            HP = 25;
            AttackDice = new Dice(3, 4, 2);
            DefenceDice = new Dice(1, 8, 5);
        }

        // ________________________ UPDATE / AI ___________________________________________
        /// <summary>
        ///  1) Enemy-initiated combat guard (försök alla 4 grannar): om target är spelaren
        ///     → snek attackerar först (log), spelaren får en counter (log), snek står kvar.
        ///  2) Om spelaren är långt bort (dist^2 > 4, dvs dist > 2) → stå still.
        ///  3) Annars: välj passabel riktning som MAXIMERAR dist^2 till spelaren och gå dit.
        ///     Om alla alternativ blockerade → stå still.
        /// </summary>
        public override void Update(LevelData level, Player player)
        {
            //___________ 1) Enemy-initiated combat (grannar) ____________________________
            for (int i = 0; i < Directions.Length; i++)
            {
                int tx = X + Directions[i].dx;
                int ty = Y + Directions[i].dy;

                if (tx == player.X && ty == player.Y)
                {
                    // 🐍 Snek attackerar först
                    int a1 = RollAttack();
                    int d1 = player.DefenceDice.Throw();
                    int dmgToPlayer = System.Math.Max(0, a1 - d1);

                    Game.Log($"Snek strikes first: ATT {a1} vs your DEF {d1} → {dmgToPlayer} dmg");
                    player.TakeDamage(dmgToPlayer);

                    if (!player.IsAlive)
                    {
                        Game.EndFromEnemy(level, player, "You were bitten by a snek... and died! 🐍");
                        return;
                    }

                    // 🔁 Spelaren får EXAKT en counterattack
                    int a2 = player.AttackDice.Throw();
                    int d2 = RollDefence();
                    int dmgToSnek = System.Math.Max(0, a2 - d2);

                    Game.Log($"You counter the snek: ATT {a2} vs DEF {d2} → {dmgToSnek} dmg");
                    bool snekDied = TakeDamage(dmgToSnek);
                    if (snekDied)
                    {
                        Game.Log("The snek collapses!");
                        level.RemoveEnemy(this);
                    }

                    // Stå kvar; gå inte in i spelarrutan
                    return;
                }
            }

            //___________ 2) Flee-beteende: stå still om spelaren är längre bort än 2 rutor _
            int dx0 = X - player.X;
            int dy0 = Y - player.Y;
            int dist2 = dx0 * dx0 + dy0 * dy0;
            if (dist2 > 4)
            {
                return; // lugn, spelaren är inte nära
            }

            //___________ 3) Backa bort: välj passabel riktning som maximerar dist^2 _______
            int bestTx = X;
            int bestTy = Y;
            int bestDist2 = dist2;
            bool foundBetter = false;

            for (int i = 0; i < Directions.Length; i++)
            {
                int tx = X + Directions[i].dx;
                int ty = Y + Directions[i].dy;

                if (level.IsBlockedByWallOrEnemy(tx, ty))
                    continue; // kan inte gå hit

                int ddx = tx - player.X;
                int ddy = ty - player.Y;
                int cand = ddx * ddx + ddy * ddy;

                if (cand > bestDist2)
                {
                    bestDist2 = cand;
                    bestTx = tx;
                    bestTy = ty;
                    foundBetter = true;
                }
            }

            if (foundBetter)
            {
                X = bestTx;
                Y = bestTy;
            }
            // annars: alla alternativ sämre/blockerade → stå still
        }

        // ________________________ DRAW _________________________________________________
        /// <summary>
        /// Renderer hanterar utskrift; denna gör inget.
        /// </summary>
        public override void Draw()
        {
            // handled by Renderer
        }

    } // END CLASS Snek __________________________________________________________________ END Snek

    // ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄ CLASS PLAYER ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄


    // ============================================================
    // Player.cs – DAS HERO! (Step 6: Movement intent only)
    // Summary: Spelaren. Håller HP + tärningar för attack/defence.
    // Ingen collision, input eller rendering ännu – kommer i steg 7–8.
    // ============================================================

    public sealed class Player : LevelElement
    {
        // ________________________ FIELDS ________________________________________________
        public int HP { get; private set; } = 100;
        public Dice AttackDice { get; }
        public Dice DefenceDice { get; }
        public bool IsAlive => HP > 0;

        // ________________________ CONSTRUCTORS _________________________________________
        /// <summary>
        /// Skapar spelaren utifrån LevelData.PlayerStart. Glyph = '@'.
        /// </summary>
        public Player((int x, int y) start)
            : base(start.x, start.y, '@')
        {
            AttackDice = new Dice(2, 6, 2);   // 2d6+2
            DefenceDice = new Dice(2, 6, 0);  // 2d6+0
        }

        /// <summary>
        /// Alternativ konstruktor – skapa manuellt på X/Y.
        /// </summary>
        public Player(int x, int y)
            : base(x, y, '@')
        {
            AttackDice = new Dice(2, 6, 2);
            DefenceDice = new Dice(2, 6, 0);
        }

        /// <summary>
        /// Legacy-signatur (behåll för kompatibilitet om andra klasser använder den).
        /// </summary>
        public Player(int x, int y, char glyph)
            : base(x, y, glyph)
        {
            AttackDice = new Dice(2, 6, 2);
            DefenceDice = new Dice(2, 6, 0);
        }

        // ________________________ METHODS _______________________________________________
        /// <summary>
        /// Beräknar target-koordinater baserat på (dx, dy) utan att flytta spelaren.
        /// Kollisionsregler hanteras centralt i LevelData/Game-loop (steg 7–8).
        /// </summary>
        public (int tx, int ty) GetTarget(int dx, int dy)
        {
            int tx = X + dx;
            int ty = Y + dy;
            return (tx, ty);
        }

        /// <summary>
        /// Attack-stub (implementeras i steg 10).
        /// </summary>
        public void Attack(Enemy enemy)
        {
            // TODO (Step 10): rulla AttackDice vs enemy.DefenceDice
        }

        /// <summary>
        /// Tar skada och minskar HP (min 0).
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (damage <= 0) return;
            HP = Math.Max(0, HP - damage);
        }

        /// <summary>
        /// Draw() – gör inget ännu; Renderer hanterar visuell del senare.
        /// </summary>
        public override void Draw()
        {
            // TODO: ritas av Renderer i steg 8–9
        }

    } // END CLASS Player ____________________________________________________________ END Player


    // █████████████████████████████ UTILITY CLASSES ███████████████████████████████████████████████████████ 03 █████████████

    // ============================================================
    // Dice.cs – RNGesus take the wheel
    // Summary: Representerar x(Dice)+ y tärningskonfigurationer, 
    // t.ex. "2d6+2". Används för attack/defence rolls.
    // ============================================================

    // ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄ CLASS DICE ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄

    public sealed class Dice
    {
        private static readonly Random rng = new Random();

        public int NumberOfDice { get; }
        public int SidesPerDice { get; }
        public int Modifier { get; }

        public Dice(int numberOfDice, int sidesPerDice, int modifier)
        {
            NumberOfDice = numberOfDice;
            SidesPerDice = sidesPerDice;
            Modifier = modifier;
        }

        public int Throw()
        {
            int sum = 0;
            for (int i = 0; i < NumberOfDice; i++)
                sum += rng.Next(1, SidesPerDice + 1);
            return sum + Modifier;
        }

        public override string ToString()
        {
            string sign = Modifier >= 0 ? "+" : "-";
            int absMod = Math.Abs(Modifier);
            return $"{NumberOfDice}d{SidesPerDice}{sign}{absMod}";
        }
    }

} // END NAMESPACE DragonsDestructiveDeathDungeon _________________________________ END

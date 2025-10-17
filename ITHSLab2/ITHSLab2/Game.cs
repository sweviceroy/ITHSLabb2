


using System;                           // basic C# core 
using System.Collections.Generic;       // List  – LevelData, Game, Enemies, Log system
using System.IO;                        // File extraction – LevelData.Load()
using System.Linq;                      // LevelData, Game loops
using System.Numerics;                  // Vector2/3 etc. Could use for Unity like distance calc.
using System.Text;                      // Encoding (Console, Stringfunctionality – Game (Console.OutputEncoding)


// ███████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████
// DragonsDestructiveDeathDungeon 
//  SECTION INDEX:
//  █ 01 – CORE SYSTEMS       → Program, LevelData, Renderer
//  █ 02 – GAME ENTITIES      → LevelElement, Wall, Enemy, Rat, Snek, Player
//  █ 03 – UTILITY CLASSES    → Dice
//
//  NOTE: Bara MVP nu! Se till att bli godkänd innan real-time projektet kan fortsätta.
// ██████████████████████████████████████████████████████████████████████████████████████████████████████████ 00 ███████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████

namespace DragonsDestructiveDeathDungeon
{

    // █████████████████████████████ CORE SYSTEMS ███████████████████████████████████████████████████████████ 01 █████████████

    // ============================================================
    // Game.cs – Game-loop + combat log
    // Summary: Entry point + strikt turn order + rendering via Renderer
    //          Combat-log: visar tärningsslag (attack/defence) och skador.
    // ============================================================

    // ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀ CLASS START ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀
    public static class Game
    {
        //_____________ COMBAT LOG (senaste N rader) _______________________________________
        private static readonly List<string> _log = new List<string>();
        private const int MaxLogLines = 5;

        /// <summary> Lägg till en rad i combat-loggen (håller bara senaste N raderna). </summary>
        public static void Log(string message)
        {
            _log.Add(message);
            if (_log.Count > MaxLogLines) _log.RemoveAt(0);
        }

        /// <summary> Renderer läser loggen via denna (returnerar en kopia som array, enklare än IReadOnlyList). </summary>
        public static string[] GetLog()
        {
            return _log.ToArray();
        }

        // ________________________ ENTRY POINT __________________________________________
        /// <summary>
        /// Programstart: laddar level och kör spelet tills ESC trycks eller spelaren dör.
        /// </summary>
        static void Main(string[] args)
        {
            Console.Title = "Dragons Destructive Death Dungeon";
            Console.OutputEncoding = Encoding.UTF8;
            Console.CursorVisible = false; // göm blinkande cursor under spel

            // 🔹 Ladda banan
            string path = "Assets/Level1.txt";
            LevelData level = new LevelData(path);

            // 🔹 Skapa spelaren vid startposition
            Player player = new Player(level.PlayerStart);

            bool running = true;

            // ________________________ MAIN LOOP (STRICT ORDER) __________________________
            while (running)
            {
                Console.Clear();

                // ----- Render pre-turn -------------------------------------------------
                Renderer.Draw(level, player);
                Renderer.DrawHud(level, player);

                // ----- INPUT -----------------------------------------------------------
                var input = GetInput();
                if (input.quit)
                {
                    running = false;
                    continue; // lämna loopen till slut-sammanfattning
                }
                int dx = input.dx;
                int dy = input.dy;

                // ====================== PHASE 1: PLAYER ================================
                // Beräkna targetposition men flytta inte ännu
                var newGridLocation = player.moveDir(dx, dy);

                // Finns det en fiende här?
                var checkMoveForEnemy = level.GetEnemyAt(newGridLocation.tx, newGridLocation.ty);
                if (checkMoveForEnemy != null && checkMoveForEnemy.IsAlive)
                {
                    // Spelar-initiated combat (attack + ev. counter)
                    PlayerAttacks(level, player, checkMoveForEnemy, newGridLocation.tx, newGridLocation.ty);
                    // OBS: movement in i rutan sker bara om fienden ripperoni
                }
                else
                {
                    // Annars: vanlig rörelse om rutan inte är blockerad
                    if (!level.isBlocked(newGridLocation.tx, newGridLocation.ty))
                    {
                        player.X = newGridLocation.tx;
                        player.Y = newGridLocation.ty;
                    }
                }

                // Defensiv guard (om framtida effekter dödar spelaren i fas 1)
                if (!player.IsAlive)
                {
                    finishUpEverything(level, player, "YOU DIED ! (Dark souls theme starts playing)");
                }

                // ====================== PHASE 2: ENEMIES strike ======================
                // Snapshot av fiender som ska få agera denna turn – stabil ordning
                var enemiesThisTurn = tempEnemyList(level);

                // FIX THIS FOR FATAL ERROR! 
                foreach (var e in enemiesThisTurn)
                {
                    // Kan ha dött under player-fasen
                    if (!e.IsAlive) { continue; }

                    // Fiendens egen Update (kan attackera spelaren eller flytta)
                    e.Update(level, player);

                    // Om spelaren dog under en fiendes tur – avsluta snyggt
                    if (!player.IsAlive)
                    {
                        finishUpEverything(level, player, $"You were slain by a {e.Name}!");
                    }
                }
                // (Valfritt senare: extra städning/telemetri här)
            }

            // Quit via ESC → visa sammanfattning
            finishUpEverything(level, player, "You quit the dungeon.");
        }

        //_____________ INPUT HANDLER _____________________________________
        /// <summary>
        /// Läser en tangent och returnerar (dx,dy) samt quit-flagga.
        /// ESC (char 27) sätter quit=true.
        /// </summary>
        private static (int dx, int dy, bool quit) GetInput()
        {
            // Half-block divider style kept minimal inside method
            // Definiera de fyra riktningarna som tuples (lokala “konstanter”)
            var dirUp = (dx: 0, dy: -1);
            var dirDown = (dx: 0, dy: 1);
            var dirLeft = (dx: -1, dy: 0);
            var dirRight = (dx: 1, dy: 0);

            char key = Console.ReadKey(true).KeyChar;
            key = char.ToLower(key);

            switch (key)
            {
                case 'w': return (dirUp.dx, dirUp.dy, false);
                case 's': return (dirDown.dx, dirDown.dy, false);
                case 'a': return (dirLeft.dx, dirLeft.dy, false);
                case 'd': return (dirRight.dx, dirRight.dy, false);
                case (char)27: // ESC
                    return (0, 0, true);
                default:
                    return (0, 0, false);
            }
        }

        // ________________________ PLAYER COMBAT ________________________________________
        /// <summary>
        /// Spelarens attack mot fiende på target-rutan.
        /// A slår D → skada=max(0, A-D). Om fienden lever → EXAKT en counter.
        /// Dör fienden → ta bort och flytta in spelaren på rutan.
        /// </summary>
        private static void PlayerAttacks(LevelData level, Player player, Enemy enemy, int tx, int ty)
        {
            //___________ Player → Enemy __________________________________________________
            int a1 = player.AttackDice.Throw();
            int d1 = enemy.RollDefence();
            int dmgToEnemy = Math.Max(0, a1 - d1);

            Log($"Our brave hero attack the {enemy.Name}: attack vs defence roll = {dmgToEnemy} damage");

            bool enemyDied = enemy.TakeDamage(dmgToEnemy);

            if (enemyDied)
            {
                Log($"The {enemy.Name} got rekt! xaxaxaxa rr ))))))");
                level.RemoveEnemy(enemy);
                player.X = tx;
                player.Y = ty;
                return;
            }

            //___________ Enemy counter → Player _________________________________________
            int a2 = enemy.RollAttack();
            int d2 = player.DefenceDice.Throw();
            int dmgToPlayer = a2 - d2;
            Log($"{enemy.Name} strikes back: attack vs defence roll = {dmgToPlayer} damage");
            player.TakeDamage(dmgToPlayer);
        }

        // ________________________ NICE ENDING / SUMMARY __________________________________
        /// <summary>
        /// Enemies kan kalla denna för att avsluta spelet snyggt (hellre än Environment.Exit direkt).
        /// </summary>
        /// 
        public static void EndFromEnemy(LevelData level, Player player, string message)
        {
            finishUpEverything(level, player, message);
        }


        //  skriv ut sammanfattning med HP och antal dödade fiender.

        private static void finishUpEverything(LevelData level, Player player, string message)
        {
            Console.Clear();
            Console.CursorVisible = true; // visa cursor igen innan exit

            Console.WriteLine(message);
            Console.WriteLine();
            Console.WriteLine($"HP: {player.HP}");
            Console.WriteLine($"Enemies wasted: {level.EnemiesDefeated}");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
            Environment.Exit(0);
        }

        private static List<Enemy> tempEnemyList(LevelData level)
        {
            var snapshot = new List<Enemy>();
            foreach (var e in level.getTempEnemies())
            {
                snapshot.Add(e);
            }
            return snapshot;
        }

    } // END CLASS Game ____________________________________________________________ END Game


    // ============================================================
    // LevelData.cs – Håller banans data (SPLIT: Walls + Enemies)
    // Summary: Laddar Level1.txt och separerar  väggar från
    //          fiender. 
    // ============================================================

    public class LevelData
    {
        //_____________ FIELDS ______________________________________________________________
        public List<Wall> Walls = new List<Wall>();
        public List<Enemy> Enemies = new List<Enemy>();

        public bool[,] SeenWalls = new bool[1, 1];
        public int EnemiesDefeated { get; private set; } = 0;

        //_____________ PROPERTIES __________________________________________________________
        /// <summary> Tuple för bredd/höjd (width, height). </summary>
        public (int width, int height) Size { get; private set; }
        public (int x, int y) PlayerStart { get; private set; } = (0, 0);

        //_____________ CONSTRUCTORS ________________________________________________________
        public LevelData()
        {
            /*
             var defaultMap = Path.Combine("Assets", "Level1.txt");
             if (File.Exists(defaultMap))
            {
             GameLoader(defaultMap);
             // Game.Log("[LevelData] Auto-loaded default map.");
             }
            else
            {
             // Game.Log("[LevelData] No default map found. Call Load(...) manually.");
             throw FileDontEx........ whatever
             }
            */
        }
        public LevelData(string filename)
        {
            Load(filename);
        }

        //_____________ LOAD METHOD _________________________________________________________
        /// <summary>
        /// Läser in textfil (Level1.txt) och bygger listorna.
        /// # = Wall, r = Rat, s = Snek, @ = PlayerStart.
        /// Initiera seenWalls med samma storlek.
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
                    char ch = SafeCharAt(line, x);

                    switch (ch)
                    {
                        case '#':
                            {
                                Walls.Add(new Wall(x, y));
                                break;
                            }
                        case 'r':
                            {
                                Enemies.Add(new Rat(x, y));
                                break;
                            }
                        case 's':
                            {
                                Enemies.Add(new Snek(x, y));
                                break;
                            }
                        case '@':
                            {
                                PlayerStart = (x, y);
                                break;
                            }
                        default:
                            {
                                // tom ruta
                                break;
                            }
                    }
                }
            }

        }

        //_______________________________________________________

        public Wall? GetWallAt(int x, int y)
        {
            foreach (var w in Walls)
                if (w.X == x && w.Y == y) return w;
            return null;
        }

        private static char SafeCharAt(string line, int x)
        {
            if (x < 0 || x >= line.Length)
                return ' '; // tom ruta utanför kanten
            return line[x];
        }

        // "public enemy" lol :D
        public Enemy? GetEnemyAt(int x, int y)
        {
            foreach (var e in Enemies)
                if ((e.IsAlive) && (e.X == x) && (e.Y == y)) return e;
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
        public bool isBlocked(int x, int y)
        {
            if (GetWallAt(x, y) != null)
                return true;

            if (GetEnemyAt(x, y) != null)
                return true;

            return false;
        }

        //_____________ tempENEMIES  ________________________________________
        public List<Enemy> getTempEnemies()
        {
            // Return a new copy so the original list can’t be changed from outside.
            return new List<Enemy>(Enemies);
        }

        public void RemoveEnemy(Enemy enemy)
        {
            if (Enemies.Remove(enemy))
            {
                EnemiesDefeated++;
            }
        }

    } // END CLASS LevelData _____________________________________________________________ END LevelData

    // ============================================================
    // Renderer.cs – Console-art engine + Combat Log HUD
    // Summary: Ritar kartan (walls + actors) och visar combat-loggen
    //          från Game.GetLog(). Vision = radie 5
    // ============================================================

    public static class Renderer
    {
        //_____________ MAIN DRAW __________________________________________________________
        /// <summary>
        /// Ritar hela spelbrädet.
        /// - Vision med Pytagoras sats
        /// - Väggar ritas om de är synliga NU eller har setts FÖRUT (SeenWalls).
        /// - Fiender syns endast när de äri vision range.
        /// - Golv ritas inte
        /// </summary>
        public static void Draw(LevelData level, Player player)
        {
            int w = level.Size.width;
            int h = level.Size.height;

            //_____________ VISIBILITY (radie 5) __________________________________________
            bool[,] visible = new bool[w, h];
            int radius = 5;
            int r2 = radius * radius;

            /*
            for (int y = player.Y - radius; y <= player.Y + radius; y++)
            {
                for (int x = player.X - radius; x <= player.X + radius; x++)
                {
                    visible[x, y] = true;
                }                                                                           DELETE THIS!
            }*/
            //for (int y = player.Y - radius; y <= player.Y + radius; y++)
            //{
            //    for (int x = player.X - radius; x <= player.X + radius; x++)             AND HTIS !
            //    {
            //        try
            //        {
            //            int dx = x - player.X;
            //            int dy = y - player.Y;
            //            if (dx * dx + dy * dy <= r2)
            //                visible[x, y] = true;
            //        }
            //        catch (IndexOutOfRangeException)
            //        {
            //            // outside map, skip
            //            continue;
            //        }
            //    }
            //}

            //___________________________________________________________ VISION RANGE

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
                    // Player ritas alltid överst
                    if (player.X == x && player.Y == y)
                    {
                        Console.Write(player.Symbol);
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
                        // Fiender syns endast när inom vision range
                        Console.Write(visible[x, y] ? enemy.Symbol : ' ');
                        continue;
                    }

                    // Tom golvyta blank, 
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

    // ============================================================
    // LevelElement.cs – Abstract base class
    // Summary: Basklass för allt på kartan. Håller X/Y + glyph. ritas med Draw().
    // ============================================================
    public abstract class LevelElement
    {
        public int X { get; set; }
        public int Y { get; set; }
        public char Symbol { get; protected set; }

        protected LevelElement(int x, int y, char _symbol)
        {
            X = x;
            Y = y;
            Symbol = _symbol;
        }

        public abstract void Draw();

        protected void SetPos(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    // ============================================================
    // Wall.cs – 
    // Summary: Väggklass. Ärver LevelElement
    // ============================================================
    public class Wall : LevelElement
    {
        public Wall(int x, int y)
            : base(x, y, '#')
        { }

        public override void Draw()
        {
            // Renderer tar hand om utskriften sen.
        }
    }


    // ============================================================
    // Enemy.cs – Abstract fiend-bas  (Shared dirs + RNG helpers)
    // Summary: Gemensam logik för alla fiender + combat-hjälpare.
    // Lägger till: delade riktningar och GetRandomDirection().
    // ============================================================

    public abstract class Enemy : LevelElement
    {
        // ________________________ SHARED DIRECTION DATA _________________________________
        /// <summary>
        /// (dx,dy) som alla fiender kan använda.
        /// </summary>
        /// 
        private static (int, int) upDir = (0,-1);
        private static (int, int) downDir =(0,1);
        private static (int, int) leftDir =(-1,0);
        private static (int, int) rightDir = (1,0);

        protected static readonly (int dx, int dy)[] Directions = new (int, int)[]{
            upDir,downDir,leftDir,rightDir };

        protected static readonly Random randomDir = new Random();

        protected static (int dx, int dy) GetRandomDirection()
        {
            return Directions[randomDir.Next(4)];
        }

        // ________________________ STATS / PROPERTIES ____________________________________
        public string Name { get; protected set; } = string.Empty;
        public int HP { get; protected set; }
        public Dice AttackDice { get; protected set; } = null!;
        public Dice DefenceDice { get; protected set; } = null!;
        public bool IsAlive
        {
            get { return HP > 0; }
        }

        protected Enemy(int x, int y, char glyph)
            : base(x, y, glyph)
        { }

        // ________________________ COMBAT  _______________________________________
        public int RollAttack()
        {
            return AttackDice.Throw();
        }

        public int RollDefence()
        {
            return DefenceDice.Throw();
        }

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

    // ============================================================
    // Rat.cs – The rodent of destruction and doom  (Combat log added)
    // Summary: Går random directions, attackerar spelaren om nära.
    // ============================================================

    public class Rat : Enemy
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
        ///  1) Om spelaren står i angränsande ruta attackera (logga slag och skador).
        ///  2) Annars försök gå 1 steg i slumpmässig riktning (upp till 4 försök).
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
                    //  attackerar spelaren
                    int a1 = RollAttack();
                    int d1 = player.DefenceDice.Throw();
                    int dmgToPlayer = a1 - d1;
                        if (dmgToPlayer < 0) dmgToPlayer = 0;

                    Game.Log($"Rat attack: Attack {a1}, heroes defence, {d1} = {dmgToPlayer} dmg");
                    player.TakeDamage(dmgToPlayer);

                    if (!player.IsAlive)
                    {
                        Game.EndFromEnemy(level, player, "You were slain by a rat! 🐀 YOU DIED! DarkSouls theme starts playing");
                        return;
                    }

                    // Players turn
                    int a2 = player.AttackDice.Throw();
                    int d2 = RollDefence();
                    int dmgToRat = System.Math.Max(0, a2 - d2);

                    Game.Log($"You counter the rat: ATT {a2} vs DEF {d2} → {dmgToRat} dmg");
                    bool ratDied = TakeDamage(dmgToRat);

                    if (ratDied)
                    {
                        Game.Log("The rat is rekt! rr xaxaxaxa ))))) ");
                        level.RemoveEnemy(this);
                    }

                    return;
                }
            }

            // try move
            for (int i = 0; i < 4; i++)
            {
                var dir = GetRandomDirection();
                int tx = X + dir.dx;
                int ty = Y + dir.dy;

                // Undvik väggar/levande fiender/out-of-bounds
                if (!level.isBlocked(tx, ty))
                {
                    X = tx;
                    Y = ty;
                    return;
                }
            }

            // Cant move, quit loop
        }

        // ________________________ DRAW _________________________________________________

        public override void Draw()
        {
            // handled by Renderer
        }

    } // END CLASS Rat __________________________________________________________________ END Rat


    // ============================================================
    // Snek.cs – (Combat log added: Flee AI + enemy-initiated combat)
    // Summary: Om ett steg skulle gå in i spelaren → attackera (counter 1x) och stå kvar.
    //          Annars: om spelaren är nära (dist ≤ 2) → ta passabelt steg som MAXIMERAR
    //          avståndet. Om långt bort (dist > 2) → stå still. Loggar alla tärningsslag.
    // ============================================================

    public class Snek : Enemy
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
                    int dmgToPlayer = a1 - d1;
                    if (dmgToPlayer < 0) dmgToPlayer = 0;

                    Game.Log($"Snek attack: Attack {a1}, heroes defence {d1} = {dmgToPlayer} dmg");
                    player.TakeDamage(dmgToPlayer);

                    if (!player.IsAlive)
                    {
                        Game.EndFromEnemy(level, player, "snek killed you ! 🐍 YOU DIED! DarkSouls theme starts playing");
                        return;
                    }
                    //player
                    int a2 = player.AttackDice.Throw();
                    int d2 = RollDefence();
                    int dmgToSnek = System.Math.Max(0, a2 - d2);

                    Game.Log($"You counter the snek: ATT {a2} vs DEF {d2} → {dmgToSnek} dmg");
                    bool snekDied = TakeDamage(dmgToSnek);
                    if (snekDied)
                    {
                        Game.Log("The snek ripperoni! GG WP NO RE! "); // https://www.youtube.com/watch?v=MS8OawQegYE
                        level.RemoveEnemy(this);
                    }

                    // Stå kvar´!
                    return;
                }
            }

            //_Fly eller illa fäkta?
            if (!IsPlayerNear(player, 2))
            {
                return; // lugn, spelaren är inte nära
            }

            else
            {
                EscapeSnek(level, player);

            }


        }// end update

        private void EscapeSnek(LevelData level, Player player)
        {
            int bestX = X;
            int bestY = Y;
            int bestDist = 0;

            // Test up
            int upX = X;
            int upY = Y - 1;
            if (!level.isBlocked(upX, upY))
            {
                int dist = Math.Abs(upX - player.X) + Math.Abs(upY - player.Y);
                if (dist > bestDist)
                {
                    bestDist = dist;
                    bestX = upX;
                    bestY = upY;
                }
            }

            //  Test down
            int downX = X;
            int downY = Y + 1;
            if (!level.isBlocked(downX, downY))
            {
                int dist = Math.Abs(downX - player.X) + Math.Abs(downY - player.Y);
                if (dist > bestDist)
                {
                    bestDist = dist;
                    bestX = downX;
                    bestY = downY;
                }
            }

            // Test left
            int leftX = X - 1;
            int leftY = Y;
            if (!level.isBlocked(leftX, leftY))
            {
                int dist = Math.Abs(leftX - player.X) + Math.Abs(leftY - player.Y);
                if (dist > bestDist)
                {
                    bestDist = dist;
                    bestX = leftX;
                    bestY = leftY;
                }
            }

            // Test right
            int rightX = X + 1;
            int rightY = Y;
            if (!level.isBlocked(rightX, rightY))
            {
                int dist = Math.Abs(rightX - player.X) + Math.Abs(rightY - player.Y);
                if (dist > bestDist)
                {
                    bestDist = dist;
                    bestX = rightX;
                    bestY = rightY;
                }
            }

            // Move if found good place
            if (bestX != X || bestY != Y)
            {
                X = bestX;
                Y = bestY;
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
        private bool IsPlayerNear(Player player, int range)
        {
            int dx = Math.Abs(X - player.X);
            int dy = Math.Abs(Y - player.Y);
            return dx <= range && dy <= range;
        }

    } // END CLASS Snek __________________________________________________________________ END Snek


    // ============================================================
    // Player.cs – DAS HERO! (Step 6: Movement intent only)
    // Summary: Spelaren. Håller HP + tärningar för attack/defence.
    // Ingen collision, input eller rendering ännu – kommer i steg 7–8.
    // ============================================================

    public class Player : LevelElement
    {
        // ________________________ FIELDS ________________________________________________
        public int HP { get; private set; } = 100;
        public Dice AttackDice { get; }
        public Dice DefenceDice { get; }
        public bool IsAlive => HP > 0;

        // ________________________ CONSTRUCTORS _________________________________________
        public Player((int x, int y) start)
            : base(start.x, start.y, '@')
        {
            AttackDice = new Dice(2, 6, 2);   // 2d6+2
            DefenceDice = new Dice(2, 6, 0);  // 2d6+0
        }

        public Player(int x, int y)
            : base(x, y, '@')
        {
            AttackDice = new Dice(2, 6, 2);
            DefenceDice = new Dice(2, 6, 0);
        }

        public Player(int x, int y, char glyph)
            : base(x, y, glyph)
        {
            AttackDice = new Dice(2, 6, 2);
            DefenceDice = new Dice(2, 6, 0);
        }

  
        public (int tx, int ty) moveDir(int dx, int dy)
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
    // Dice.cs – 
    // Summary: x-antal * (Dice)+ y modifier, 
    // "2d6+2". Används för attack/defence rolls.
    // ============================================================
    public class Dice
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

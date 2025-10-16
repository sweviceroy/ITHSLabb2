//
// ██████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████
// DragonsDestructiveDeathDungeon 
//
//
//  SECTION INDEX:
//  █ 01 – CORE SYSTEMS       → Program, LevelData, Renderer
//  █ 02 – GAME ENTITIES      → LevelElement, Wall, Enemy, Rat, Snek, Player
//  █ 03 – UTILITY CLASSES    → Dice
//
//  NOTE: Bara MVP nu! Se till att bli godkänd innan real-time projektet kan fortsätta.
// ██████████████████████████████████████████████████████████████████████████████████████████████████████████ 00 █████████████

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace DragonsDestructiveDeathDungeon
{

    // █████████████████████████████ CORE SYSTEMS ███████████████████████████████████████████████████████████ 01 █████████████

    // ============================================================
    // Game.cs – Grundläggande game-loop (Step 10: Player combat)
    // Summary: Entry point + huvudloop. Rörelse + strid när spelaren
    // försöker gå in i en fienderuta (en enkel attack + counter).
    // ============================================================

    public static class Game
    {
        // ________________________ ENTRY POINT __________________________________________
        /// <summary>
        /// Programstart: laddar level och kör spelet tills ESC trycks.
        /// </summary>
        static void Main(string[] args)
        {
            Console.Title = "Dragons Destructive Death Dungeon";
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // 🔹 Ladda banan
            string path = "Assets/Level1.txt";
            LevelData level = new LevelData(path);

            // 🔹 Skapa spelaren vid startposition
            Player player = new Player(level.PlayerStart);

            bool running = true;

            // ________________________ MAIN LOOP ________________________________________
            while (running)
            {
                Console.Clear();

                // Rita kartan
                Draw(level, player);

                // Instruktioner
                Console.WriteLine();
                Console.WriteLine("WASD / PILAR = move   |   ESC = quit");

                // Vänta på tangenttryck
                ConsoleKeyInfo key = Console.ReadKey(true);

                int dx = 0;
                int dy = 0;

                // ________________________ INPUT ________________________________________
                switch (key.Key)
                {
                    case ConsoleKey.W:
                    case ConsoleKey.UpArrow:
                        {
                            dy = -1; break;
                        }
                    case ConsoleKey.S:
                    case ConsoleKey.DownArrow:
                        {
                            dy = 1; break;
                        }
                    case ConsoleKey.A:
                    case ConsoleKey.LeftArrow:
                        {
                            dx = -1; break;
                        }
                    case ConsoleKey.D:
                    case ConsoleKey.RightArrow:
                        {
                            dx = 1; break;
                        }
                    case ConsoleKey.Escape:
                        {
                            running = false;
                            continue; // hoppa ut ur loopen direkt
                        }
                }

                // ________________________ PLAYER MOVEMENT & COMBAT _____________________
                // Beräkna targetposition men flytta inte ännu
                var target = player.GetTarget(dx, dy);

                // Finns det en levande fiende på target-rutan?
                var targetElem = level.GetFirstAt(target.tx, target.ty);
                if (targetElem is Enemy enemy && enemy.IsAlive)
                {
                    ResolvePlayerAttack(level, player, enemy, target.tx, target.ty);
                }
                else
                {
                    // Annars: vanlig rörelse om rutan inte är blockerad
                    if (!level.IsBlockedByWallOrEnemy(target.tx, target.ty))
                    {
                        // (Ja, vi borde egentligen ha en grid såhär långt, men vi håller det enkelt!)
                        player.X = target.tx;
                        player.Y = target.ty;
                    }
                }

                // ________________________ ENEMY UPDATE (AI) _____________________________
                foreach (var e in level.GetEnemies())
                {
                    e.Update(level, player);
                }

                // (Senare: vision, logg, dödshantering m.m.)
            }

            Console.Clear();
            Console.WriteLine("Game over. Thanks for playing!");
            Console.ReadKey(true);
        }

        // ________________________ COMBAT RESOLUTION ____________________________________
        /// <summary>
        /// Spelarens attack mot fiende på target-rutan. En enkel växling:
        ///  A slår D → skada=max(0, A-D).
        ///  Om fienden lever → EN (1) counterattack.
        ///  Dör fienden → ta bort och flytta in spelaren på rutan.
        /// </summary>
        private static void ResolvePlayerAttack(LevelData level, Player player, Enemy enemy, int tx, int ty)
        {
            // --- Spelaren attackerar ---
            int a1 = player.AttackDice.Throw();
            int d1 = enemy.RollDefence();
            int dmgToEnemy = Math.Max(0, a1 - d1);
            bool enemyDied = enemy.TakeDamage(dmgToEnemy);

            if (enemyDied)
            {
                level.RemoveEnemy(enemy);
                player.X = tx;
                player.Y = ty;
                return;
            }

            // --- Counterattack (EXAKT en gång) ---
            int a2 = enemy.RollAttack();
            int d2 = player.DefenceDice.Throw();
            int dmgToPlayer = Math.Max(0, a2 - d2);
            player.TakeDamage(dmgToPlayer);

            // Om spelaren får 0 HP i framtiden: här kan vi lägga Game Over-logik.
            // Nu: spelaren stannar om fienden överlevde.
        }

        // ________________________ RENDERING ____________________________________________
        /// <summary>
        /// Enkel rituppgift: skriver ut hela kartan + spelaren.
        /// </summary>
        private static void Draw(LevelData level, Player player)
        {
            for (int y = 0; y < level.Size.height; y++)
            {
                for (int x = 0; x < level.Size.width; x++)
                {
                    var element = level.GetFirstAt(x, y);

                    if (player.X == x && player.Y == y)
                    {
                        Console.Write(player.Glyph);
                    }
                    else if (element != null)
                    {
                        Console.Write(element.Glyph);
                    }
                    else
                    {
                        Console.Write(' ');
                    }
                }
                Console.WriteLine();
            }
        }

    } // END CLASS Game ____________________________________________________________ END Game


    // ============================================================
    // LevelData.cs – Håller banans data  (Step 7: Collision helpers)
    // Summary: Laddar Level1.txt, skapar Walls/Rats/Sneks och sparar PlayerStart.
    // Exponerar read-only Elements + helpers för bounds, lookup och blockering.
    //
    // Movement rule (player):
    //  - Vägg (#) och out-of-bounds är alltid blockerande.
    //  - fiender blockerar rörelse, men du får "targeta" rutan för att initiera bonkning.
    //  - Om fienden ripperoni under attack så kan man gå
    // ============================================================

    public sealed class LevelData
    {
        // ________________________ FIELDS ________________________________________________
        // En enkel lista med alla element i leveln (väggar + fiender)
        public List<LevelElement> Elements = new List<LevelElement>();

        // ________________________ PROPERTIES ____________________________________________
        // Tuple för bredd/höjd
        public (int width, int height) Size { get; private set; }

        // Spelarens startposition hittad via '@'
        public (int x, int y) PlayerStart { get; private set; } = (0, 0);

        // ________________________ CONSTRUCTORS _________________________________________
        public LevelData()
        {
            // vi laddar via Load(filename)
        }

        public LevelData(string filename)
        {
            Load(filename);
        }

        // ________________________ METHODS _______________________________________________

        /// <summary>
        /// Läser in en textfil (Level1.txt) och bygger elementlistan.
        /// Tecken:
        ///   '#' = Wall, 'r' = Rat, 's' = Snek, '@' = PlayerStart.
        /// </summary>
        public void Load(string filename)
        {
            Elements.Clear();

            string[] lines = File.ReadAllLines(filename);
            int height = lines.Length;
            int width = 0;

            // Räknar ut längsta raden (bredden)
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length > width)
                {
                    width = lines[i].Length;
                }
            }

            Size = (width, height);

            // Loopar igenom varje rad/kolumn i filen
            for (int y = 0; y < height; y++)
            {
                string line = lines[y];

                for (int x = 0; x < width; x++)
                {
                    char ch = (x < line.Length) ? line[x] : ' ';

                    switch (ch)
                    {
                        case '#': Elements.Add(new Wall(x, y)); break;
                        case 'r': Elements.Add(new Rat(x, y)); break;
                        case 's': Elements.Add(new Snek(x, y)); break;
                        case '@': PlayerStart = (x, y); break;
                        default: break; // tom ruta
                    }
                }
            }
        }

        // ________________________ HELPERS ______________________________________________

        /// <summary>
        /// Kollar om koordinaten (x,y) ligger innanför kartans storlek.
        /// Returnerar true om giltig ruta, annars false.
        /// </summary>
        public bool InBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Size.width && y < Size.height;
        }

        /// <summary>
        /// Returnerar första elementet på (x,y) eller null om det inte finns något där.
        /// </summary>
        public LevelElement? GetFirstAt(int x, int y)
        {
            foreach (var e in Elements)
            {
                if (e.X == x && e.Y == y)
                {
                    return e;
                }
            }
            return null;
        }

        /// <summary>
        /// Returnerar true om rutan blockeras av vägg eller levande fiende.
        /// False om rutan är tom (eller död fiende).
        /// </summary>
        public bool IsBlockedByWallOrEnemy(int x, int y)
        {
            if (!InBounds(x, y))
            {
                return true;
            }

            foreach (var e in Elements)
            {
                if (e is Wall && e.X == x && e.Y == y)
                {
                    return true;
                }

                if (e is Enemy enemy && enemy.IsAlive && enemy.X == x && enemy.Y == y)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returnerar en lista över alla fiender i leveln (både levande och döda).
        /// Praktiskt i game-loopen för att uppdatera AI.
        /// </summary>
        public IEnumerable<Enemy> GetEnemies()
        {
            foreach (var e in Elements)
            {
                if (e is Enemy enemy)
                {
                    yield return enemy;
                }
            }
        }

        /// <summary>
        /// Tar bort en specifik fiende från listan, t.ex. när den dör i strid.
        /// </summary>
        public void RemoveEnemy(Enemy enemy)
        {
            Elements.Remove(enemy);
        }

    } // END CLASS LevelData _____________________________________________________________ END LevelData

    // ============================================================
    // Renderer.cs – Console-art engine
    // Summary: Ansvarar för att rita kartan. MVP: enkel text.
    // Senare: vision radius + fog-of-war. 
    // ============================================================
    public static class Renderer
    {
        // TODOs:
        // [ ] Draw(LevelData, Player) skeleton
        // [ ] (Senare) Fog-of-war + seenWalls-array
        // [ ] (Senare) färg + memes kanske 🤷‍♂️
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

    // ============================================================
    // Wall.cs – Bonk!
    // Summary: Väggklass. Ärver LevelElement och blockerar allt.
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


    // ============================================================
    // Enemy.cs – Abstract fiend-bas  (Step 10: Combat helpers)
    // Summary: Gemensam logik för alla fiender + enkla combat-hjälpare.
    // ============================================================

    public abstract class Enemy : LevelElement
    {
        public string Name { get; protected set; } = string.Empty;
        public int HP { get; protected set; }
        public Dice AttackDice { get; protected set; } = null!;
        public Dice DefenceDice { get; protected set; } = null!;
        public bool IsAlive => HP > 0;

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

        public abstract void Update(LevelData level, Player player);
        public abstract override void Draw();
    }


    // ============================================================
    // Rat.cs – The rodent of destruction and doom  (Step 9: AI)
    // Summary: Slumpar en riktning och försöker gå 1 steg. Försöker upp till 4 håll.
    // ============================================================
    public sealed class Rat : Enemy
    {
        private static readonly Random rng = new Random();

        public Rat(int x, int y)
            : base(x, y, 'r')
        {
            Name = "rat";
            HP = 10;
            AttackDice = new Dice(1, 6, 3);
            DefenceDice = new Dice(1, 6, 1);
        }

        // ________________________ METHODS _______________________________________________
        /// <summary>
        /// Step 9 – Enkel AI:
        ///  1) Slumpar ordningen på fyra kardinalriktningar.
        ///  2) Testar varje riktning: om rutan inte är blockerad → flytta dit och avsluta.
        ///  3) Om alla fyra är blockerade → stå still.
        /// </summary>
        public override void Update(LevelData level, Player player)
        {
            // Kandidatriktningar (dx,dy)
            (int dx, int dy)[] dirs = new (int, int)[]
            {
                (0, -1), // upp
                (0,  1), // ner
                (-1, 0), // vänster
                (1,  0), // höger
            };

            // Fisher–Yates shuffle för att randomisera ordningen varje tur
            for (int i = dirs.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var tmp = dirs[i];
                dirs[i] = dirs[j];
                dirs[j] = tmp;
            }

            // Försök gå i första passabla riktningen
            for (int i = 0; i < dirs.Length; i++)
            {
                int tx = X + dirs[i].dx;
                int ty = Y + dirs[i].dy;

                // Out-of-bounds räknas som blockerad via helpern
                if (!level.IsBlockedByWallOrEnemy(tx, ty))
                {
                    X = tx;
                    Y = ty;
                    return; // gjort för denna turn
                }
            }

            // Alla håll blockerade → gör inget denna turn
        }

        public override void Draw()
        {
            // Renderer ritar senare.
        }
    }

    // ============================================================
    // Snek.cs – (Step 9: AI – flee logic)
    // Summary: Snek where are you going? SNEK STAHP! (>2). Om nära (≤2) så backa       //https://preview.redd.it/eg1zq0wvk9r51.jpg?width=640&crop=smart&auto=webp&s=43e25c1641086cce6f74e35b43c2fe03a85f9241
    // en ruta i den riktning som MAXIMERAR avståndet till spelaren.
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

        // ________________________ METHODS _______________________________________________
        /// <summary>
        /// Step 9 – Enkel "flee" AI:
        ///  - Räkna avstånd^2 till spelaren. Om > 4 (dvs avstånd > 2) → gör inget.
        ///  - Annars: testa de fyra kardinalriktningarna och välj den passabla ruta
        ///    som ger STÖRST avstånd^2 till spelaren. Om alla blockerade → stå still.
        /// </summary>
        public override void Update(LevelData level, Player player)
        {
            // Avstånd^2 (vi skippar Math.Sqrt för enkelhet och prestanda)
            int dx0 = X - player.X;
            int dy0 = Y - player.Y;
            int dist2 = dx0 * dx0 + dy0 * dy0;

            // Om spelaren är längre bort än 2 rutor → stå still
            if (dist2 > 4)
            {
                return;
            }

            // Kandidatriktningar (dx,dy)
            (int dx, int dy)[] dirs = new (int, int)[]
            {
                (0, -1), // upp
                (0,  1), // ner
                (-1, 0), // vänster
                (1,  0), // höger
            };

            int bestTx = X;
            int bestTy = Y;
            int bestDist2 = dist2; // vi vill hitta något STÖRRE än nuvarande
            bool foundBetter = false;

            for (int i = 0; i < dirs.Length; i++)
            {
                int tx = X + dirs[i].dx;
                int ty = Y + dirs[i].dy;

                // Måste vara passabelt (ingen vägg/levande fiende och inom bounds)
                if (level.IsBlockedByWallOrEnemy(tx, ty))
                {
                    continue;
                }

                int ddx = tx - player.X;
                int ddy = ty - player.Y;
                int candidateDist2 = ddx * ddx + ddy * ddy;

                // Välj den riktning som maximerar avståndet
                if (candidateDist2 > bestDist2)
                {
                    bestDist2 = candidateDist2;
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

        public override void Draw()
        {
            // Renderer tar hand om utskriften senare.
        }

    } // END CLASS Snek __________________________________________________________________ END Snek


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

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

namespace DragonsDestructiveDeathDungeon
{

    // █████████████████████████████ CORE SYSTEMS ███████████████████████████████████████████████████████████ 01 █████████████

    // ============================================================================
    // Program.cs – Entry point
    // ============================================================================
    internal class Program
    {
        // TODOs:
        // [V] Print header och liten helptext
        // [V] Vänta på knapp och avsluta clean
        // [ ] (Senare) starta Game-loop
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("Test av tärning: ");

            var testDice = new Dice(2, 6, 2);
            for (int i = 0; i < 5; i++)
            {
                Console.WriteLine($"Roll {i + 1}: {testDice.Throw()}  ({testDice})");
            }

            Console.ReadKey(true);
        }
    }

    // ============================================================
    // LevelData.cs – Håller banans data
    // Summary: Lagrar alla LevelElements, map-storlek, och PlayerStart.
    // Laddar filen och hanterar collisions.
    // ============================================================
    public sealed class LevelData
    {
        // TODOs:
        // [ ] Private List<LevelElement> elements + read-only getter
        // [ ] Load(filename) parser (steg 5)
        // [ ] Helpers: InBounds(), IsBlocked(), GetAt(), Enemies-lista
    }

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
        public int X { get; protected set; }
        public int Y { get; protected set; }
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
    // Enemy.cs – Abstract fiend-bas
    // Summary: Gemensam logik för alla fiender. 
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

        public abstract void Update(LevelData level, Player player);
        public abstract override void Draw();
    }

    // ============================================================
    // Rat.cs – The rodent of destruction and doom
    // Summary: He nibble, he squeak, he’ll ruin your week. 
    // ============================================================
    public sealed class Rat : Enemy
    {
        public Rat(int x, int y)
            : base(x, y, 'r')
        {
            Name = "rat";
            HP = 10;
            AttackDice = new Dice(1, 6, 3);
            DefenceDice = new Dice(1, 6, 1);
        }

        public override void Update(LevelData level, Player player)
        {
            // TODO: Slumpa en riktning och försök gå 1 steg (steg 9)
        }

        public override void Draw()
        {
            // Renderer ritar senare.
        }
    }

    // ============================================================
    // Snek.cs –
    // Summary: Snek where are you going?! SNEK STAHP! https://preview.redd.it/eg1zq0wvk9r51.jpg?width=640&crop=smart&auto=webp&s=43e25c1641086cce6f74e35b43c2fe03a85f9241
    // Beter sig fegt: står still >2 tiles, annars backar bort.
    // HP 25, Atk 3d4+2, Def 1d8+5.
    // ============================================================

    /// <summary>
    /// Snek – försiktig fiende som backar från spelaren när han är nära.
    /// AI implementeras senare (steg 9).
    /// </summary>
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
        /// Update() – placeholder för AI (kommer i steg 9):
        /// Om dist > 2: stå still. Annars: försök ta ett steg bort från spelaren.
        /// </summary>
        public override void Update(LevelData level, Player player)
        {
            // TODO: Implementera flee-logic i steg 9
        }

        /// <summary>
        /// Draw() – placeholder, Renderer ritar senare.
        /// </summary>
        public override void Draw()
        {
            // TODO: Renderer tar hand om utskriften
        }

    } // END CLASS Snek __________________________________________________________________ END Snek


    // ============================================================
    // Player.cs – Main character
    // Summary: Hjälten. 1 steg/turn WASD och PILAR för movement
    // ============================================================
    public sealed class Player : LevelElement
    {
        public Player(int x, int y, char glyph) : base(x, y, glyph)
        {
        }

        // TODOs:
        // [ ] HP 100, AttackDice 2d6+2, DefenceDice 2d6+0
        // [ ] ProposeMove(dx,dy) logik (steg 6)
        // [ ] Attack() + TakeDamage() stubbar
        public override void Draw()
        {
            throw new NotImplementedException();
        }
    }


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

// ============================================================
// DragonsDestructiveDeathDungeon 
// ============================================================

using System;

namespace DragonsDestructiveDeathDungeon
{
    // ============================================================================
    // BARA MVP NU! Se till att bli godkänd innan real time projektet kan fortsätta
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
    // LevelElement.cs – Abstract base class
    // Summary: Basklass för allt på kartan. Håller X/Y + glyph. ritas med Draw().
    // ============================================================
    /// <summary>
    /// Basklass för alla level-objekt (väggar, spelare, fiender).
    /// Innehåller gemensam data: position (X,Y) och tecknet (Glyph) som ritas.
    /// </summary>
    public abstract class LevelElement
    {
        // ________________________ PROPERTIES ____________________________________________
        /// <summary> X-position i rutnätet (kolumn). </summary>
        public int X { get; protected set; }

        /// <summary> Y-position i rutnätet (rad). </summary>
        public int Y { get; protected set; }

        /// <summary> Tecknet som representerar elementet i konsolen. </summary>
        public char Glyph { get; protected set; }

        // ________________________ CONSTRUCTOR ___________________________________________

        // vi kan använda vadsomhelst som glyph, men kör P för player, # för wall, S för snake och R för rat
        protected LevelElement(int x, int y, char glyph)
        {
            X = x;
            Y = y;
            Glyph = glyph;
        }

        // ________________________ METHODS _______________________________________________
        /// <summary>
        /// Ritar elementet. Själva utskriften sker centralt i Renderer senare,
        /// men kontraktet finns här så alla element kan "be om" att ritas.
        /// </summary>
        public abstract void Draw();

        /// <summary>
        /// Flytta objektet till en ny koordinat. Minimal helper (no physics).
        /// </summary>
        protected void SetPos(int x, int y)
        {
            X = x;
            Y = y;
        }

    } // END CLASS LevelElement _________________________________________________________ END LevelElement

    // ============================================================
    // Wall.cs – Bonk!
    // Summary: Väggklass. Ärver LevelElement och blockerar allt.
    // Hårdkodad glyph '#' och används för att bygga kartans väggar.
    // ============================================================

    /// <summary>
    /// Representerar en vägg i dungeonen.
    /// Väggar är statiska och har alltid glyph '#'.
    /// Ingen AI, ingen rörelse, bara BONK.
    /// </summary>
    public sealed class Wall : LevelElement
    {
        // ________________________ CONSTRUCTOR ___________________________________________
        public Wall(int x, int y)
            : base(x, y, '#')
        {

        }

        // ________________________ METHODS _______________________________________________
        /// <summary>
        /// Draw() – placeholder, den riktiga renderingen sker via Renderer senare.
        /// </summary>
        public override void Draw()
        {
            // Ingenting här än – Renderer tar hand om utskriften sen.
        }

    } // END CLASS Wall _________________________________________________________________ END Wall




    // ============================================================
    // Enemy.cs – Abstract fiend-bas
    // Summary: Gemensam logik för alla fiender. 
    // Har namn, HP och Dice för attack/defence. Update() kör varje turn för hur de skall röra sig
    // ============================================================

    /// <summary>
    /// Abstrakt fiendeklass som Rat och Snake ärver från.
    /// Innehåller gemensamma stats och kontrakt för uppdatering varje turn.
    /// </summary>
    public abstract class Enemy : LevelElement
    {
        // ________________________ PROPERTIES ____________________________________________
        /// <summary> Visningsnamn, t.ex. "rat" eller "snake". </summary>
        public string Name { get; protected set; } = string.Empty;

        /// <summary> Hälsopoäng. När HP ≤ 0: dead.exe </summary>
        public int HP { get; protected set; }

        /// <summary> Tärningar för attack (t.ex. 1d6+3). </summary>
        public Dice AttackDice { get; protected set; } = null!;

        /// <summary> Tärningar för defence (t.ex. 1d6+1). </summary>
        public Dice DefenceDice { get; protected set; } = null!;

        /// <summary> True om fienden lever (HP &gt; 0). </summary>
        public bool IsAlive => HP > 0;

        // ________________________ CONSTRUCTOR ___________________________________________
        protected Enemy(int x, int y, char glyph)
            : base(x, y, glyph)
        {
            // Subklasser sätter Name/HP/Dice.
        }

        // ________________________ METHODS _______________________________________________
        /// <summary>
        /// AI-uppdatering för fienden. Körs en gång per turn.
        /// Subklasser bestämmer rörelsebeteende.
        /// </summary>
        public abstract void Update(LevelData level, Player player);

        /// <summary>
        /// Ritkontrakt – låt subklasser bestämma hur de ritas (glyph finns redan).
        /// Renderer kommer senare att hantera konsol-io centralt.
        /// </summary>
        public abstract override void Draw();

        // (Senare) Helpers som TryStep(), TakeDamage(int dmg) etc. läggs här.

    } // END CLASS Enemy ________________________________________________________________ END Enemy



    // ============================================================
    // Rat.cs – The rodent of destruction and doom
    // Summary: He nibble, he squeak, he´ll ruin your week. 
    // HP 10, Atk 1d6+3, Def 1d6+1.
    // ============================================================

    /// <summary>
    /// Rat – en liten fiende med slumpmässiga rörelser.
    /// Kommer senare att röra sig 1 steg i random riktning varje turn.
    /// </summary>
    public sealed class Rat : Enemy
    {
        // ________________________ CONSTRUCTOR ___________________________________________
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
        /// Update() – placeholder för AI (kommer i steg 9).
        /// Just nu gör råttan nada.
        /// </summary>
        public override void Update(LevelData level, Player player)
        {
            // TODO: Slumpa en riktning och försök gå 1 steg (steg 9)
        }

        /// <summary>
        /// Draw() – placeholder, Renderer ritar senare.
        /// </summary>
        public override void Draw()
        {
            // Nada här tills Renderer implementeras.
        }

    } // END CLASS Rat _________________________________________________________________ END Rat

    // ============================================================
    // Snake.cs – SNEK
    // Summary: Snek where are you going?! SNEK STAHP! https://i.redd.it/eg1zq0wvk9r51.jpg
    // Backar bort. HP 25, Atk 3d4+2, Def 1d8+5.
    // ============================================================
    public sealed class Snake : Enemy
    {
        // TODOs:
        // [ ] Init stats i konstruktorn
        // [ ] Implementera Update(): flee if ≤2 (steg 9)
        // [ ] (Senare) Tie-break logik för rörelse
    }

    // ============================================================
    // Player.cs – Main character
    // Summary: Hjälten. 1 steg/turn WASD och PILAR för movement
    // Krocka med väggar och fiender = combat!
    // ============================================================
    public sealed class Player : LevelElement
    {
        // TODOs:
        // [ ] HP 100, AttackDice 2d6+2, DefenceDice 2d6+0
        // [ ] ProposeMove(dx,dy) logik (steg 6)
        // [ ] Attack() + TakeDamage() stubbar
        // [ ] (Senare) XP och loot? lol nope, MVP only.
    }

    // ============================================================
    // Dice.cs – 
    // Summary: Representerar x(Dice)+ y tärningskonfigurationer, 
    // t.ex. "2d6+2". Används för attack/defence rolls.
    // ============================================================

    /// <summary>
    /// Dice hanterar tärningskast som "2d6+2".
    /// Använd Throw() för att rulla, och ToString() för att visa config.
    /// </summary>
    public sealed class Dice
    {
        // ________________________ FIELDS ________________________________________________
        private static readonly Random rng = new Random(); // shared random generator

        // ________________________ PROPERTIES ____________________________________________
        public int NumberOfDice { get; }
        public int SidesPerDice { get; }
        public int Modifier { get; }

        // ________________________ CONSTRUCTOR ___________________________________________
        public Dice(int numberOfDice, int sidesPerDice, int modifier)
        {
            NumberOfDice = numberOfDice;
            SidesPerDice = sidesPerDice;
            Modifier = modifier;
        }

        // ________________________ METHODS _______________________________________________
        /// <summary>
        /// Rullar tärningarna och returnerar totalpoängen.
        /// </summary>
        public int Throw()
        {
            int sum = 0;

            for (int i = 0; i < NumberOfDice; i++)
            {
                // rollar 1 till SidesPerDice (inklusive)
                sum += rng.Next(1, SidesPerDice + 1);
            }

            sum += Modifier;
            return sum;
        }

        /// <summary>
        /// Returnerar en sträng som beskriver tärningen, t.ex. "2d6+2".
        /// </summary>
        public override string ToString()
        {
            string sign = Modifier >= 0 ? "+" : "-";
            int absMod = Math.Abs(Modifier);
            return $"{NumberOfDice}d{SidesPerDice}{sign}{absMod}";
        }

    } // END CLASS Dice ________________________________________________________________ END Dice

    // ============================================================
    // LevelData.cs – Håller banans data
    // Summary: Lagrar alla LevelElements, map-storlek, och PlayerStart.
    // Laddar filen och hanterar collisions.
    // ============================================================
    public sealed class LevelData
    {
        // TODOs:
        // [ ] Private List<LevelElement> elements + read-only getterd
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
}

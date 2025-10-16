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
    public abstract class LevelElement
    {
        // TODOs:
        // [ ] Props: X, Y, Glyph
        // [ ] Abstrakt Draw() metod (Renderer gör jobbet senare)
        // [ ] (Kanske) helper SetPos()
    }

    // ============================================================
    // Wall.cs 
    // Summary: Vägg. Ärver LevelElement. Hårdkodad '#' för väggar
    // ============================================================
    public sealed class Wall : LevelElement
    {
        // TODOs:
        // [ ] Konstruktor sätter glyph till '#'
        // [ ] Draw() placeholder (Renderer hanterar rendering später)
    }
    // ============================================================
    // Enemy.cs – Abstract fiend-bas
    // Summary: Gemensam logik för alla fiender. 
    // Har namn, HP och Dice för attack/defence. Update() kör varje turn för hur de skall röra sig
    // ============================================================
    public abstract class Enemy : LevelElement
    {
        // TODOs:
        // [ ] Props: Name, HP, AttackDice, DefenceDice
        // [ ] Abstrakt Update(LevelData, Player)
        // [ ] (Senare) IsAlive helper, TryStep() helper
    }

    // ============================================================
    // Rat.cs – The chaotic rodent
    // Summary: Gör Kaos med er! Ett
    // HP 10, Atk 1d6+3, Def 1d6+1.
    // ============================================================
    public sealed class Rat : Enemy
    {
        // TODOs:
        // [ ] Init stats i konstruktorn
        // [ ] Implementera Update(): slumpa riktning, gå ett steg (steg 9)
        // [ ] (Senare) Squeak-ljud meme 🐀
    }

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

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
        // [ ] Print header och liten helptext
        // [ ] Vänta på knapp och avsluta clean
        // [ ] (Senare) starta Game-loop
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "Dragons Destructive Death Dungeon – Lab 2 MVP";
            Console.WriteLine("=== Dragons Destructive Death Dungeon – Lab 2 (MVP) ===");
            Console.WriteLine("Projekt scaffold klart. Nästa steg: Dice!");
            Console.WriteLine("Tryck valfri tangent för att avsluta…");
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
    // Summary: Representerar NdS±M tärningskonfigurationer, 
    // t.ex. "2d6+2". Används för attack/defence rolls.
    // ============================================================
    public sealed class Dice
    {
        // TODOs:
        // [ ] Fields: numberOfDice, sidesPerDice, modifier
        // [ ] Implement Throw() (steg 2)
        // [ ] ToString() returnerar t.ex. "2d6+2"
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
}

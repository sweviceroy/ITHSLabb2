using System;
using System.Collections.Generic;
using System.IO;
// TODO: 
// Add all the classes.                                                   [V]
// Add cool intro ascii art picture                                       [V]
// Setup the console window and enable emojis                             [V]
// Print the intro ascii art pic the first thing in the main Method       [ ]
// Add Summary to all classes what they are supposed to do.               [ ]
// Create the Level Element class                                         [ ]
// Create the LevelData                                                   [ ]
// Load the level                                                         [ ]
// Print the walls!                                                       [ ]

// ███████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████ NAMESPACE - START █████████████ 1 ████
namespace Labb2DungeonCrawler
{
    // TODO: DESCRIPTION OF HOW ALL CLASSES FIT TOGETHER !


    // ███████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████ CORE CLASSES      █████████████ 2 ████
    // ███████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████ CORE CLASSES      █████████████ 2 ████
    public class Dice
    {
    }

    public abstract class LevelElement
    {
    }

    public abstract class Enemy : LevelElement
    {
    }

    public class Wall : LevelElement
    {
    }

    public class LevelData
    {
    }

    public class GameState
    {
    }
    // ███████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████ Player and        █████████████ 3 ████
    // ███████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████ Enemies           █████████████ 3 ████

    public class Player : LevelElement
    {
    }

    public class Rat : Enemy
    {
    }

    public class Snake : Enemy
    {
    }
    // ███████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████ UTILITY and       █████████████ 4 ████
    // ███████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████ COMBAT            █████████████ 4 ████

    public static class Renderer
    {
    }
    public static class Symbols
    {
        public const string Crown = "👑";
        public const string Rat = "🐀";
        public const string Ninja = "🥷";
        public const string Web = "🕸";
        public const string Scroll = "📜";
        public const string Key = "🗝";
        public const string Sword = "🗡";
        public const string Potion = "🧪";
        public const string Door = "🚪";
        public const string Bed = "🛏";
        public const string Basket = "🧺";
        public const string Urn = "⚱";
        public const string Statue = "🗿";
        public const string Snake = "🐍";
    }
    public static class Combat
    {
    }
    // ███████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████ MAIN CLASS        █████████████ 5 ████
    // ███████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████                   █████████████ 5 ████

    public static class Labb2DungeonCrawler
    {

        public static void Main(string[] args)
        {

            InitiateConsole();


        }

        // THis is needed to setup the console and enable more colors
        // checkout : https://en.wikipedia.org/wiki/ANSI_escape_code for reference
        public static void InitiateConsole()
        {
            // === Basic window setup ===
            Console.Title = "Lab 2 - Dangerous Dark Dungeon Crawler";
            Console.CursorVisible = false;

            Console.SetWindowSize(120, 40);   // width x height
            Console.BufferWidth = 120;
            Console.BufferHeight = 40;

            // === Colors ===
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Clear();

            // === Enable UTF-8 output for emojis ===
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // === Custom ANSI color test (example) ===
            // You can use these later to draw colored walls, ground, etc.
            Console.WriteLine("\x1b[38;2;100;100;100mThis is a dark gray wall test.\x1b[0m");
            Console.WriteLine("\x1b[38;2;50;180;50mThis is green ground test.\x1b[0m");
            Console.WriteLine("\x1b[38;2;160;0;0mThis is a dark red border test.\x1b[0m");
        }

    }
}

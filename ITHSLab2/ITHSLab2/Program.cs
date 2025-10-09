// blocks: █

using System;
using System.Collections.Generic;
using System.IO;
// TODO: 
// Add all the classes.                                                   [V]
// Add cool intro ascii art picture                                       [V]
// Setup the console window and enable emojis                             [V]
// Print the intro ascii art pic the first thing in the main Method       [X]
// Add Summary to all classes what they are supposed to do.               [-]
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

    /// <summary>
    /// Base class for all visible elements in the dungeon level.
    /// Contains position (X,Y), a visual symbol (emoji or ASCII), and ANSI color information.
    /// Also includes a flag for emoji usage and a Draw() method for rendering.
    /// </summary>
    public abstract class LevelElement
    {
        public (int X, int Y) Position { get; set; }      // Tuple for grid coordinates
        public string Symbol { get; set; }                // Emoji or ASCII character
        public bool IsEmoji { get; set; }                 // True = Emoji, False = ASCII/ANSI char
        public string? AnsiColorCode { get; set; }        // ANSI color code for ASCII

        public virtual void Draw()
        {
            Console.SetCursorPosition(Position.X, Position.Y);

            if (IsEmoji)
            {
                Console.Write(Symbol);
            }
            else
            {
                if (!string.IsNullOrEmpty(AnsiColorCode))
                    Console.Write(AnsiColorCode + Symbol + "\x1b[0m");
                else
                    Console.Write(Symbol);
            }
        }
    }// end Level Element


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

    // Some more symbols to use for epicness! 
    public static class Symbols
    {
        public const string Crown = "👑";
        public const string Rat = "🐀";
        public const string Ninja = "🥷";
        public const string SpiderWeb = "🕸";   // was Web
        public const string Scroll = "📜";
        public const string Key = "🗝";
        public const string Sword = "🗡";
        public const string HealthP = "🧪";     // was Potion
        public const string Door = "🚪";
        public const string Bed = "🛏";
        public const string Korg = "🧺";        // was Basket
        public const string Urn = "⚱";
        public const string Face = "🗿";        // was Statue
        public const string Snake = "🐍";
    }


    /// <summary>
    /// Class used to get ANSI color codes and has predefined colors.
    /// Allows converting HEX RGB values (#RRGGBB) to ANSI 24-bit escape codes for extra colours. 
    /// 
    /// Usage example:
    /// <code>
    /// string wallColor = AnsiColors.FromHex(AnsiColors.DarkGray); // using oredefined color DarkGrey 
    /// Console.WriteLine(wallColor + "▓▓▓▓▓▓▓" + "\x1b[0m"); // prints dark gray blocks
    /// 
    /// string redText = AnsiColors.FromHex(AnsiColors.BrightRed);
    /// Console.WriteLine(redText + "Danger ahead!" + "\x1b[0m"); // prints bright red text
    /// </code>
    /// </summary>
    public static class AnsiColors
    {
        // === Predefined color hex values ===
        // === Fördefinierade färger (Predefined color hex values) ===
        public const string LjusRod = "#FF0000";         // Bright Red
        public const string LjusGrön = "#00FF00";        // Bright Green
        public const string LjusBlå = "#0000FF";         // Bright Blue
        public const string MörkGrå = "#404040";         // Dark Gray
        public const string LjusGrå = "#A0A0A0";         // Light Gray
        public const string Guld = "#FFD700";            // Gold
        public const string Turkos = "#00FFFF";          // Cyan
        public const string Purpur = "#FF00FF";          // Magenta

        // === Extra färger ===
        public const string KörsbärsRöd = "#DC143C";     // Crimson
        public const string Orange = "#FFA500";          // Orange
        public const string MörkOrange = "#FF8C00";      // Dark Orange
        public const string Gul = "#FFFF00";              // Yellow
        public const string Oliv = "#808000";             // Olive
        public const string Lime = "#32CD32";             // Lime
        public const string SjöGrön = "#2E8B57";          // Sea Green
        public const string Teal = "#008080";             // Teal
        public const string HimmelBlå = "#87CEEB";        // Sky Blue
        public const string KungBlå = "#4169E1";          // Royal Blue
        public const string Indigo = "#4B0082";           // Indigo
        public const string Violett = "#EE82EE";          // Violet
        public const string Brun = "#A52A2A";             // Brown
        public const string SandBrun = "#F4A460";         // Sandy Brown
        public const string LjusBrun = "#D2B48C";         // Tan
        public const string Silver = "#C0C0C0";           // Silver
        public const string Vit = "#FFFFFF";              // White
        public const string Svart = "#000000";            // Black
        public const string SkogsGrön = "#228B22";        // Forest Green
        public const string TegelRöd = "#B22222";         // Firebrick

        /// <summary>
        /// Converts a HEX color (#RRGGBB) to an ANSI 24-bit escape code for foreground color.
        /// Example: "#FF0000" → "\x1b[38;2;255;0;0m"
        /// </summary>
        public static string FromHex(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex) || !hex.StartsWith("#") || hex.Length != 7)
                throw new ArgumentException("Invalid hex color format. Use #RRGGBB.", nameof(hex));

            int r = Convert.ToInt32(hex.Substring(1, 2), 16);
            int g = Convert.ToInt32(hex.Substring(3, 2), 16);
            int b = Convert.ToInt32(hex.Substring(5, 2), 16);

            return $"\x1b[38;2;{r};{g};{b}m";
        }
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
            printSymbolsAndColoredWalls();


        }
        // test to print 

        public static void printSymbolsAndColoredWalls()
        {
            Console.Clear();
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // === Print all symbols ===
            Console.WriteLine("=== SYMBOLER ===");
            var symbols = new Dictionary<string, string>
    {
        { nameof(Symbols.Crown), Symbols.Crown },
        { nameof(Symbols.Rat), Symbols.Rat },
        { nameof(Symbols.Ninja), Symbols.Ninja },
        { nameof(Symbols.SpiderWeb), Symbols.SpiderWeb },
        { nameof(Symbols.Scroll), Symbols.Scroll },
        { nameof(Symbols.Key), Symbols.Key },
        { nameof(Symbols.Sword), Symbols.Sword },
        { nameof(Symbols.HealthP), Symbols.HealthP },
        { nameof(Symbols.Door), Symbols.Door },
        { nameof(Symbols.Bed), Symbols.Bed },
        { nameof(Symbols.Korg), Symbols.Korg },
        { nameof(Symbols.Urn), Symbols.Urn },
        { nameof(Symbols.Face), Symbols.Face },
        { nameof(Symbols.Snake), Symbols.Snake }
    };

            foreach (var s in symbols)
                Console.WriteLine($"{s.Key.PadRight(10)} {s.Value}");

            Console.WriteLine();
            Console.WriteLine("=== FÄRGPALETT ===");

            // === Print all predefined colors as a single continuous bar ===
            var colors = new[]
            {
        AnsiColors.LjusRod, AnsiColors.LjusGrön, AnsiColors.LjusBlå,
        AnsiColors.MörkGrå, AnsiColors.LjusGrå, AnsiColors.Guld,
        AnsiColors.Turkos, AnsiColors.Purpur, AnsiColors.KörsbärsRöd,
        AnsiColors.Orange, AnsiColors.MörkOrange, AnsiColors.Gul,
        AnsiColors.Oliv, AnsiColors.Lime, AnsiColors.SjöGrön,
        AnsiColors.Teal, AnsiColors.HimmelBlå, AnsiColors.KungBlå,
        AnsiColors.Indigo, AnsiColors.Violett, AnsiColors.Brun,
        AnsiColors.SandBrun, AnsiColors.LjusBrun, AnsiColors.Silver,
        AnsiColors.Vit, AnsiColors.Svart, AnsiColors.SkogsGrön,
        AnsiColors.TegelRöd
    };

            foreach (var hex in colors)
            {
                string ansi = AnsiColors.FromHex(hex);
                Console.Write(ansi + "███" + "\x1b[0m");
            }

            Console.WriteLine("\n\nTryck på valfri tangent för att fortsätta...");
            Console.ReadKey(true);
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

/*
  Lands of Fellmore — Text RPG (C# .NET 8)
  ----------------------------------------------------------------------------
  Movement input parsing by Tianac.
  All other systems (world, inventory, events, title screen, and text) by Ben.
*/

using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    // ==============================
    // ROOM CLASS — by Ben
    // ==============================
    sealed class Room
    {
        public int Id { get; }
        public string Description { get; }
        public Dictionary<string, int> Exits { get; }
        public Func<GameState, bool> OnEnter { get; set; }

        public Room(int id, string desc)
        {
            Id = id;
            Description = desc;
            Exits = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            OnEnter = _ => true;
        }
    }

    // ==============================
    // GAME STATE — by Ben
    // ==============================
    sealed class GameState
    {
        public int CurrentRoomId = 1;
        public bool Running = true;
        public bool Alive = true;
        public bool EndedPeacefully = false;

        public bool GateUnlocked = false;
        public bool KeyInAlley = true;
        public bool AmbushCleared = false;

        public List<string> Inventory { get; } = new List<string>();

        public void Reset()
        {
            CurrentRoomId = 1;
            Running = true;
            Alive = true;
            EndedPeacefully = false;
            GateUnlocked = false;
            KeyInAlley = true;
            AmbushCleared = false;
            Inventory.Clear();
            Inventory.Add("sack of gold");
        }

        public bool HasItem(string name) =>
            Inventory.Any(i => i.Equals(name, StringComparison.OrdinalIgnoreCase));

        public bool RemoveItem(string name)
        {
            var i = Inventory.FindIndex(s => s.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (i >= 0) { Inventory.RemoveAt(i); return true; }
            return false;
        }
    }

    static readonly Dictionary<int, Room> World = BuildWorld();

    // ==============================
    // TITLE SCREEN — by Ben
    // ==============================
    enum TitleChoice { None, Start, Help, Quit }

    static TitleChoice ShowTitle()
    {
        Console.Clear();
        Console.WriteLine("           /\\                     /\\                     /\\                     /\\                     /\\");
        Console.WriteLine("          /  \\        /\\         /  \\        /\\         /  \\        /\\         /  \\        /\\         /  \\");
        Console.WriteLine("     /\\  /    \\  /\\  /  \\  /\\  /    \\  /\\  /  \\  /\\  /    \\  /\\  /  \\  /\\  /    \\  /\\  /  \\  /\\  /    \\  /\\ ");
        Console.WriteLine("    /  \\/      \\/  \\/    \\/  \\/      \\/  \\/    \\/  \\/      \\/  \\/    \\/  \\/      \\/  \\/    \\/  \\/      \\/  \\/");
        Console.WriteLine("===============================================================================================================");
        Console.WriteLine("                              LANDS OF FELLMORE");
        Console.WriteLine("===============================================================================================================");
        Console.WriteLine("                          a game made by Tnac09 & Dothmar");
        Console.WriteLine("---------------------------------------------------------------------------------------------------------------");
        Console.WriteLine();
        Console.WriteLine("    [S] START GAME");
        Console.WriteLine("    [H] HELP");
        Console.WriteLine("    [Q] QUIT");
        Console.WriteLine();
        Console.WriteLine("---------------------------------------------------------------------------------------------------------------");
        Console.Write("> ");

        var c = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();
        if (c == "S") return TitleChoice.Start;
        if (c == "H") return TitleChoice.Help;
        if (c == "Q") return TitleChoice.Quit;
        return TitleChoice.None;
    }

    // ==============================
    // HELP SCREEN — by Ben
    // ==============================
    static void ShowHelpScreen()
    {
        Console.Clear();
        Console.WriteLine("================================== HELP ==================================");
        Console.WriteLine("  MOVEMENT:  north   south   east   west");
        Console.WriteLine("  INVENTORY: inventory   (or)   inv");
        Console.WriteLine("  PICKUP:    pick up <item>");
        Console.WriteLine("  USE:       use <item>");
        Console.WriteLine("  BUY:       buy <item>");
        Console.WriteLine("=========================================================================== ");
        Console.Write("Press ENTER to return...");
        Console.ReadLine();
    }

    // ==============================
    // MAIN LOOP — movement by Tianac
    // ==============================
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        while (true)
        {
            var sel = ShowTitle();
            if (sel == TitleChoice.Quit) return;
            if (sel == TitleChoice.Help) { ShowHelpScreen(); continue; }
            if (sel == TitleChoice.Start) break;
        }

        var state = new GameState();
        state.Reset();
        PrintRoom(state);

        while (state.Running)
        {
            Console.Write("> ");
            var input = (Console.ReadLine() ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(input)) continue;

            var lower = input.ToLowerInvariant();

            // Movement system by Tianac
            if (lower == "north" || lower == "south" || lower == "east" || lower == "west")
            {
                AttemptMove(state, lower);
                continue;
            }
            else if (lower == "quit") break;

            // Commands by Ben
            if (lower == "look") { PrintRoom(state); continue; }
            if (lower == "inventory" || lower == "inv") { ShowInventory(state); continue; }
            if (lower.StartsWith("pick up ")) { HandlePickup(state, input.Substring(8).Trim()); continue; }
            if (lower.StartsWith("use ")) { HandleUseItem(state, input.Substring(4).Trim()); continue; }
            if (lower.StartsWith("buy ")) { HandleBuyItem(state, input.Substring(4).Trim()); continue; }

            // Short command help reminder
            Console.WriteLine("Unknown command.");
            Console.WriteLine("Try: north/south/east/west, inventory, pick up <item>, use <item>, buy <item>, look, quit.");
        }
    }

    // ==============================
    // WORLD DATA — by Ben
    // ==============================
    static Dictionary<int, Room> BuildWorld()
    {
        var r = new Dictionary<int, Room>();

        r[1] = new Room(1,
@"You are in The Rusty Mug, a smoky tavern filled with noise and the smell of ale. The barkeep eyes you suspiciously as drunks argue nearby.
Exits: NORTH — to the village square. SOUTH — to your room.");

        r[2] = new Room(2,
@"A small, dimly lit room with a straw bed and a candle burning low. Your pack rests on a chair.
Exits: NORTH — back to the tavern.");

        r[3] = new Room(3,
@"You stand in the busy heart of the village. Merchants shout prices, and horses clatter across cobblestones.
Exits: NORTH — to the bridge. EAST — to a narrow alley. SOUTH — to the tavern. WEST — to the blacksmith’s forge.");

        r[4] = new Room(4,
@"You stand before a roaring forge. The blacksmith hammers steel with practiced rhythm.
Exits: EAST — to the square. WEST — to the forest trail. NORTH — to the storage shed.
A sword rests on a rack. Perhaps he’ll part with it for a price.");

        r[5] = new Room(5,
@"The alley is damp and silent. Trash piles against the walls, and rats scurry in the shadows.
Exits: WEST — to the square.
A rusted iron key lies half-buried in mud.");

        // Troll bridge — death
        r[6] = new Room(6,
@"You walk north from the square and reach an ancient bridge. A massive troll stirs beneath it.
YOU ARE DEAD.");
        r[6].OnEnter = s => { s.Alive = false; return false; };

        r[7] = new Room(7,
@"The path twists through tall trees. Ahead, a great iron gate bars the way.
Exits: EAST — to the blacksmith. WEST — to the gate (locked). SOUTH — back toward the village.");

        // Mimic — death
        r[8] = new Room(8,
@"You find a ruined tower and a single chest. It trembles...
The lid snaps shut with teeth. YOU ARE DEAD.");
        r[8].OnEnter = s => { s.Alive = false; return false; };

        r[9] = new Room(9,
@"The towering gate looms before you.
Exits: EAST — to the forest trail. WEST — to the clearing.");

        r[10] = new Room(10,
@"You stand in a moonlit clearing. Exits: EAST — to the gate. NORTH — along a winding trail.");

        // Goblin ambush — Ben
        r[11] = new Room(11,
@"The path narrows as it climbs toward the summit.");
        r[11].OnEnter = s =>
        {
            if (s.AmbushCleared) return true;

            Console.WriteLine();
            Console.WriteLine("Brush parts—A GOBLIN LURCHES FROM THE DARK!");
            if (s.HasItem("shortsword"))
            {
                Console.WriteLine("Your hand moves on instinct; you draw the shortsword.");
                Console.WriteLine("Steel bites. The goblin hisses, staggers, and falls.");
                Console.WriteLine("Its crude knife skitters across the stones.");
                Console.WriteLine("The night is still again. The way north stands open.");
                s.AmbushCleared = true;
                return true;
            }
            Console.WriteLine("You reach for a weapon—there is none.");
            Console.WriteLine("The goblin crashes into you; a jagged blade flashes.");
            Console.WriteLine("Cold earth. Dimming stars. Silence.");
            Console.WriteLine("YOU ARE DEAD.");
            s.Alive = false;
            return false;
        };

        r[12] = new Room(12,
@"The rocky path climbs steeply. Exits: NORTH — to the hill summit. SOUTH — back to the narrow trail.");

        r[13] = new Room(13,
@"You climb to the top of the hill. The stars burn bright above.
This is the end of your adventure.");
        r[13].OnEnter = s => { s.EndedPeacefully = true; return false; };

        r[16] = new Room(16,
@"A rickety shed filled with dust and cobwebs.
Exits: SOUTH — back to the forge. EAST — through a collapsed wall toward old ruins.");

        // Links
        r[1].Exits["north"] = 3; r[1].Exits["south"] = 2;
        r[2].Exits["north"] = 1;
        r[3].Exits["north"] = 6; r[3].Exits["east"] = 5; r[3].Exits["south"] = 1; r[3].Exits["west"] = 4;
        r[4].Exits["east"] = 3; r[4].Exits["west"] = 7; r[4].Exits["north"] = 16;
        r[5].Exits["west"] = 3;
        r[7].Exits["east"] = 4; r[7].Exits["west"] = 9; r[7].Exits["south"] = 3;
        r[9].Exits["east"] = 7; r[9].Exits["west"] = 10;
        r[10].Exits["east"] = 9; r[10].Exits["north"] = 11;
        r[11].Exits["south"] = 10; r[11].Exits["north"] = 12;
        r[12].Exits["south"] = 11; r[12].Exits["north"] = 13;
        r[16].Exits["south"] = 4; r[16].Exits["east"] = 8;

        return r;
    }

    // ==============================
    // DISPLAY & MOVEMENT
    // ==============================
    static void PrintRoom(GameState state)
    {
        var room = World[state.CurrentRoomId];
        Console.WriteLine();
        Console.WriteLine(room.Description);
        if (room.Id == 11 && state.AmbushCleared)
            Console.WriteLine("A fallen goblin lies in the brush. The way north stands open.");
    }

    static void AttemptMove(GameState state, string dir)
    {
        var cur = World[state.CurrentRoomId];
        if (cur.Id == 7 && dir == "west" && !state.GateUnlocked)
        {
            Console.WriteLine("The great iron gate is locked.");
            return;
        }

        if (!cur.Exits.TryGetValue(dir, out var nextId))
        {
            Console.WriteLine("That's not a direction you can go.");
            return;
        }

        state.CurrentRoomId = nextId;
        var next = World[state.CurrentRoomId];

        if (!next.OnEnter(state))
        {
            Console.WriteLine();
            Console.WriteLine(next.Description);
            if (!state.Alive || state.EndedPeacefully)
                PromptRestart(state);
            return;
        }

        PrintRoom(state);
    }

    // ==============================
    // COMMANDS
    // ==============================
    static void ShowInventory(GameState state)
    {
        if (state.Inventory.Count == 0) { Console.WriteLine("Inventory is empty."); return; }
        Console.WriteLine("Inventory:");
        foreach (var it in state.Inventory) Console.WriteLine("- " + it);
    }

    static void HandlePickup(GameState state, string itemRaw)
    {
        if (string.IsNullOrWhiteSpace(itemRaw)) { Console.WriteLine("Pick up what?"); return; }
        var item = itemRaw.ToLowerInvariant();
        if (state.CurrentRoomId == 5 && state.KeyInAlley &&
            (item.Contains("key") || item == "rusted iron key" || item == "iron key"))
        {
            state.KeyInAlley = false;
            if (!state.HasItem("rusted iron key")) state.Inventory.Add("rusted iron key");
            Console.WriteLine("You picked up the rusted iron key.");
        }
        else Console.WriteLine("There's nothing like that to pick up here.");
    }

    static void HandleUseItem(GameState state, string item)
    {
        if (string.IsNullOrWhiteSpace(item)) { Console.WriteLine("Use what?"); return; }
        if (item.Contains("key")) HandleUseKey(state);
        else Console.WriteLine("You can't use that right now.");
    }

    static void HandleUseKey(GameState state)
    {
        if (!state.HasItem("rusted iron key")) { Console.WriteLine("You don't have a key."); return; }
        if (state.CurrentRoomId != 7) { Console.WriteLine("There's nothing here that the key fits."); return; }
        if (state.GateUnlocked) { Console.WriteLine("The gate is already unlocked."); return; }
        state.GateUnlocked = true;
        Console.WriteLine("Metal clinks; the mechanism yields. The gate is now unlocked to the west.");
    }

    static void HandleBuyItem(GameState state, string item)
    {
        if (string.IsNullOrWhiteSpace(item)) { Console.WriteLine("Buy what?"); return; }
        if (item.Contains("sword")) HandleBuySword(state);
        else Console.WriteLine("The blacksmith doesn't sell that.");
    }

    static void HandleBuySword(GameState state)
    {
        if (state.CurrentRoomId != 4) { Console.WriteLine("There's no smith here to sell you a sword."); return; }
        if (state.HasItem("shortsword")) { Console.WriteLine("You already have a shortsword."); return; }
        if (!state.HasItem("sack of gold")) { Console.WriteLine("You don't have anything to pay with."); return; }
        state.RemoveItem("sack of gold");
        state.Inventory.Add("shortsword");
        Console.WriteLine("You purchase a shortsword, handing over your sack of gold.");
    }

    // ==============================
    // RESTART LOGIC
    // ==============================
    static void PromptRestart(GameState state)
    {
        Console.WriteLine();
        Console.Write("Restart? (y/n): ");
        var ans = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(ans) || ans == "n" || ans == "no")
        {
            Console.WriteLine("Type 'quit' to exit, or keep exploring from the start.");
            state.Reset();
            PrintRoom(state);
            return;
        }

        if (ans == "y" || ans == "yes")
        {
            state.Reset();
            PrintRoom(state);
        }
        else
        {
            Console.WriteLine("Type 'quit' to exit, or keep exploring from the start.");
            state.Reset();
            PrintRoom(state);
        }
    }
}

# Project ARDO

Project ARDO is a text-prompt dark fantasy RPG framework built in C# with Windows Forms, object-oriented gameplay systems, and SQLite-backed persistence. The game focuses on clean architecture, modular code, and a command-driven terminal-style experience rather than a heavy graphics engine.

## Phase 3 Release

This repository currently reflects the Phase 3 release of the project, including:

- Text-driven RPG gameplay with town, shop, combat, and defeat flows
- `Warrior` and `Mage` hero classes
- Enemy encounters including goblins, fire imps, wraith heralds, and hollow captains
- A command-driven town shop with `buy`, `sell`, `sell all`, `equip`, `unequip`, and `exit`
- Weapons and armor that can be equipped, add to attack and defense, and stay equipped across saves
- Target selection in group fights (`target 2`, `attack 2`), critical hits and misses
- Enemies that telegraph heavy blows a turn ahead, plus a trick for each enemy type: goblins steal gold,
  skeletons get back up once, fire imps set you burning, the Wraith Herald drains mana or life, and bosses enrage
- Loot with rarities from Common to Legendary that improve with depth; Rare and better gear carries an effect
  (burning, vampiric, keen, warded), and guardians always drop gear
- Events between fights (a shrine, a wounded traveler, a campfire, a locked chest) and a side passage at the end
  of each floor that trades one more fight for double gold and a rare find
- A shop that restocks with better gear as you go deeper, and a Mana Draught for mages
- Colored story text with health bars, revealed a line at a time
- Real-time inventory and gold updates
- SQLite save/load support through a repository-based data layer
- Item selection and combat prompts with explicit cancel behavior

## Since Phase 3

- Inventory and Gear screens, opened with `i` / `g` or the buttons in the bottom-right corner
- Six gear slots (weapon, helmet, chest, shield, boots, ring) shown on an ASCII figure; mages hold a staff
- The shop sells staffs to mages and blades to warriors, at the same price and power
- A character creation screen with the two classes side by side; hero names are capped at 16 characters
- Click a menu option, list row, enemy or class card to fill in its command, then press Enter
- Attack, Special, Flee and Item buttons during a fight, plus Esc, command history (Up / Down) and PageUp / PageDown
- A hero who reaches the epilogue is marked on the load list as fallen to the God-King. They can still be loaded and pick floor 1, 2 or 3 from the dungeon, but the throne is sealed to them

## Screen Recording

Project walkthrough video:

- https://youtu.be/9qPY1hUw6mw

## How to Run

### Option 1: Run from Visual Studio

1. Open the project in Visual Studio or another C# IDE.
2. Restore NuGet packages if needed.
3. Build and run the `ARDO` project.
4. The game launches at the title screen.

### Option 2: Run the Published Build

1. Download `ARDO.exe` from the [Releases](https://github.com/WattsOfCode/Project_ARDO/releases) page.
2. Double-click it. The build is self-contained, so no .NET runtime installation is needed.

To make the build yourself, run `dotnet publish -p:PublishProfile=WinSelfContained`. It is written to
`publish/win-x64`, which is not committed to git.

Saves are kept in `%LocalAppData%\ARDO\ardo.db`, so the game runs from any folder without administrator
rights. A save file next to an older build of the game is copied there on first launch.

## Application Start Flow

The application starts in `Program.cs`, which initializes the SQLite database and launches `StartMenuForm`.

From the title screen, the player can:

- `1` Create New Character Profile
- `2` Load Existing Character Save Slot
- `3` Exit Application

When loading a save, choosing `0` returns the player to the main menu.

## Core Features

- Text-driven RPG gameplay with menu-based navigation
- Windows Forms-based game shell for narrative, input, stats, and character display
- Polymorphic combat system built around the `ICombatant` interface
- Abstract hero and enemy class hierarchies
- Playable hero classes: `Warrior` and `Mage`
- Multiple enemy types with boss encounters
- Inventory and equipment management
- Weapon, armor, and consumable item systems
- Quest-driven story progression
- SQLite save/load support with repository-based data access

## Extended Features

- ASCII art support for heroes and enemies
- Separate panels for narrative, character art, and stats
- Factory classes for enemy and item creation
- Command-driven town shop with `buy`, `sell`, `sell all`, `equip`, `unequip`, and `exit`
- Better weapons and armor are equipped automatically when bought or looted
- In-shop gold updates in real time after buying or selling
- Item-use flow that supports `0`, `back`, or `cancel` to back out
- Combat command prompts that switch context based on the current game state
- Defeat prompt that lets the player rise again in Oakhaven for 50 gold, or let the hero go
- Letting the hero go (`2`) deletes that save profile and returns the player to the main menu
- Database initialization and schema migration support
- Self-contained Windows publish output for standalone launch

## Project Summary

Project ARDO demonstrates the design and implementation of a production-style C# game framework built around object-oriented principles, UI state management, and SQLite persistence. The project was developed to show how gameplay logic, user interface flow, and database operations can be separated into focused classes that remain easy to extend and maintain.

The strongest technical outcomes were the combat architecture, the repository-based persistence layer, and the command-driven game flow across town, shop, combat, and defeat states. The project also helped reinforce practical skills in class design, composition, polymorphism, and stateful application development.

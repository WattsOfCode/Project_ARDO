# Project ARDO

Project ARDO is a text-prompt dark fantasy RPG framework built in C# with Windows Forms, object-oriented gameplay systems, and SQLite-backed persistence. The game focuses on clean architecture, modular code, and a command-driven terminal-style experience rather than a heavy graphics engine.

## Phase 3 Release

This repository currently reflects the Phase 3 release of the project, including:

- Text-driven RPG gameplay with town, shop, combat, and defeat flows
- `Warrior` and `Mage` hero classes
- Enemy encounters including goblins, fire imps, wraith heralds, and hollow captains
- A command-driven town shop with `buy`, `sell`, `sell all`, and `exit`
- Real-time inventory and gold updates
- SQLite save/load support through a repository-based data layer
- Item selection and combat prompts with explicit cancel behavior

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

1. Open the `publish/win-x64` folder.
2. Launch `ARDO.exe`.
3. The published build is self-contained, so no separate .NET runtime installation should be required.

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
- Command-driven town shop with `buy`, `sell`, `sell all`, and `exit`
- In-shop gold updates in real time after buying or selling
- Item-use flow that supports `0`, `back`, or `cancel` to back out
- Combat command prompts that switch context based on the current game state
- Defeat prompt that allows the player to continue or delete the active character profile
- Choosing `no` at the defeat prompt deletes the current save profile and returns the player to the main menu
- Database initialization and schema migration support
- Self-contained Windows publish output for standalone launch

## Project Summary

Project ARDO demonstrates the design and implementation of a production-style C# game framework built around object-oriented principles, UI state management, and SQLite persistence. The project was developed to show how gameplay logic, user interface flow, and database operations can be separated into focused classes that remain easy to extend and maintain.

The strongest technical outcomes were the combat architecture, the repository-based persistence layer, and the command-driven game flow across town, shop, combat, and defeat states. The project also helped reinforce practical skills in class design, composition, polymorphism, and stateful application development.

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

### Heroes and controls

- Two new classes: the `Archer` (more crits, Aimed Shot, a Volley that hits every foe, and a Pinning Shot that
  breaks a heavy blow) and the `Rogue` (dodges and flees more often, Backstab, Poison Blade, and Smoke Step)
- A character creation screen with the four classes side by side; hero names are capped at 16 characters.
  Hard mode is chosen there too (type `hard`): a hard-mode hero who falls is erased at once, and carries a
  small skull badge under their portrait
- Every new hero sets out with a Wooden Staff (+3 ATK) that breaks after 10 won fights
- Every level-up offers three perks to choose from, drawn from shared perks and the hero's own class
  (Cleave, Pyromancer, Barbed Arrows, Shadow Dance and more). Type `perks` to see the ones you hold;
  heroes from older saves get one to choose for every level they had already earned
- Click a menu option, list row, enemy or class card to fill in its command, then press Enter
- Attack, Special, Flee and Item buttons during a fight, plus Esc, command history (Up / Down) and PageUp / PageDown
- Quick guide: type `help` or `?` anywhere for the controls

### Gear and the shop

- Inventory and Gear screens, opened with `i` / `g` or the buttons in the bottom-right corner
- Six gear slots (weapon, helmet, chest, shield, boots, ring) shown on an ASCII figure, holding each class's own weapon
- The shop sells each class its own weapon (blade, staff, bow or daggers) at the same price and power
- Clicking an item in the shop or pack (or `info 3` in the shop) shows its name, its description and what it
  does, compared with what the hero wears in that slot now
- Full pack: a better find replaces the least valuable item carried; a worse one is left behind

### The dungeon

- Between fights the passage forks left and right, each way with a hint of what lies down it. Most lead to the
  next fight; some to an event, or to a door. Behind a door is a treasure chest (sometimes trapped, sometimes
  guarded) or an empty room to catch your breath. A rogue can pick a chest's lock and skip the trap
- Empty rooms on floors 1 to 3 and the deeper roads often hold a journal page that hints at Alhuezo, the
  keys and the door to his throne
- Every floor ends at its guardian, each with a fight of its own: Grisk, the Goblin King calls in lackeys who
  scatter when he falls; the Cinder Matron is healed by fire and looses a Firestorm every third turn; the Raven Witch
  drinks from every wound, turns blows aside with her parasol, and calls down her ravens when badly hurt.
  The first kill of each leaves an Epic trophy, and a hero can turn back before the fight, leaving the floor uncleared
- Run summary: every trip into the dungeon ends with a tally of fights won, foes slain, XP and gold earned,
  items found and levels gained, plus any first guardian kill or key of the wheel. It shows on the way back to town,
  on death (before the choice to rise again) and after an ending
- Defeat screen: a fallen hero chooses between two cards, Rise Again (50 gold) or Start a New Hero, which erases
  the save after a second confirmation and opens the new-hero screen. The run summary shows underneath

### The throne and the wheel

- A hero who reaches the epilogue is marked on the load list as fallen to Alhuezo. They can still be loaded and
  pick any open floor from the dungeon, but the throne is sealed to them
- After the epilogue, three keys of the wheel are hidden on floors 1 to 3. With all three, the sealed throne opens
  for a rematch with Alhuezo that can be won, and the wheel turns forward. A rogue can instead pick the door's
  servant lock and fight their own shadow for a second ending. The load list shows which ending each hero reached
- The Wheel Turns Again: after either ending, option 7 in town turns the wheel once more. The hero keeps their level,
  gear, gold and perks; the keys scatter and the guardians rise (with their Epic trophies) for another way to the
  throne. Each turn keeps every twist before it and adds one: Bloodied Stone (enemies +25% health), Sharper Teeth
  (+20% damage), The Crowded Dark (fights often bring one more foe), Wrathful Guardians (guardians and throne foes
  +25% again) and Cold Hearths (the inn costs 30 gold). Past Turn 5 the wheel grinds on, +10% health and damage a
  turn. Alhuezo and the shadow already grow with the hero, so they take 40% of each twist. Every turn pays 15% more
  XP and gold, and the load list and stats panel show the hero's turn
- Guardians' second phase: from Turn 2 of the wheel, each floor guardian changes how it fights below half health.
  Grisk puts on a gilded ward that soaks blows and grows with every coin he taxes; the Cinder Matron breaks the
  floor open, and the magma burns the hero (through armor and dodges) a little hotter every round; the Raven Witch
  goes to mist behind three ravens, and no blow reaches her while they live and feed her. The approach warns of it
- Deeper roads: each turn of the wheel opens two travel floors on the road to one boss, listed before the floor they
  lead to and picked by label (1a, 1b). They have no guardian or key, just longer runs of fights, forks and events at
  the deepest tier, with veteran enemies. Turn 1 opens The Robbers' Road and The Banner Halls on the way to Grisk,
  with the Goblin Cutpurse (escapes with your gold if not killed in time), the Goblin Brute and the Goblin Shaman
  (heals the most hurt goblin). Turns 2 to 4 are planned for the Matron, the Witch and the throne

### Town

- Talking in town: option 6 opens the tavern. Maren is always there; Eldrin arrives by quest 2 (or once
  the hero has helped her as the wounded scout), Sera by quest 3. Each has lines that change with progress
- Saves from earlier versions open with the new classes available; nothing in them is lost

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

## Project Layout

The code follows a model-view-controller split:

- **Model**: `Core/` (heroes, enemies, items and their contracts) and the game rules in `Game/` (`Combat`,
  `Enemies`, `Events`, `Inventory`, `Players`, `Progression`, and `Shop`, where the `Merchant` keeps the stock
  and does the trading)
- **View**: `UI/` (the windows in `Forms`, the panels and screens in `Controls`, text helpers in `Display`)
- **Controller**: `GameStateManager` runs the game flow and `CombatEngine` runs a fight. Both talk to the window
  only through `IGameView`, and both are split by area into partial files (`GameStateManager.Town.cs`,
  `.Dungeon.cs`, `.Events.cs` and so on; `CombatEngine.PlayerActions.cs`, `.Display.cs`, `.Resolution.cs`)
- **Data**: `Data/` saves and loads through repositories (`IPlayerRepository`, SQLite underneath)

## Core Features

- Text-driven RPG gameplay with menu-based navigation
- Windows Forms-based game shell for narrative, input, stats, and character display
- Polymorphic combat system built around the `ICombatant` interface
- Abstract hero and enemy class hierarchies
- Playable hero classes: `Warrior`, `Mage`, `Archer` and `Rogue`
- Multiple enemy types with boss encounters and a named guardian on every floor
- Inventory and equipment management
- Weapon, armor, and consumable item systems
- Quest-driven story progression
- Level-up perks that shape each hero's build
- Repeatable harder runs after an ending (The Wheel Turns Again)
- A summary of each dungeon run
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
- Defeat screen that lets the player rise again in Oakhaven for 50 gold, or let the hero go
- Letting the hero go deletes that save profile and opens the new-hero screen
- Database initialization and schema migration support
- Self-contained Windows publish output for standalone launch

## Project Summary

Project ARDO demonstrates the design and implementation of a production-style C# game framework built around object-oriented principles, UI state management, and SQLite persistence. The project was developed to show how gameplay logic, user interface flow, and database operations can be separated into focused classes that remain easy to extend and maintain.

The strongest technical outcomes were the combat architecture, the repository-based persistence layer, and the command-driven game flow across town, shop, combat, and defeat states. The project also helped reinforce practical skills in class design, composition, polymorphism, and stateful application development.

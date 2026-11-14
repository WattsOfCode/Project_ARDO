# ARDO ASCII art standard

Every picture in this folder follows the same rules, so the game looks like one piece.

## The rules

- **Bright is dense.** The game draws light green on black, so `@` is the brightest cell and a space is black.
  Art made here uses the ramp ` .:-=+*#%@` (dark to bright); supplied art may bring its own (like ` .:;+xX$&`).
- **Supplied art is used as given.** Art the designer provides (like `Enemies/grisk.txt`, `Enemies/cindermatron.txt`,
  `Enemies/ravenwitch.txt`) goes in exactly as sent, background and all, unless they ask for it to be cut out.
- **Class portraits are busts.** `Heroes/` pictures show the hero from the waist up, cut out from their background
  (the supplied characters kept exactly, everything around the figure blank), so the four cards on the creation
  screen stand about the same height. All four were made this way from supplied art.
- **Any size.** Pictures are centered and fitted to the panel. Small ones are drawn as text at the largest font
  that fits; detailed ones (like 250 by 205) are drawn at a normal size and the picture is scaled down, so they keep
  their true shape. Up to 300 columns and 260 rows.
- **Drawn from a real picture.** A portrait starts from a drawing or painting turned into ASCII, not from
  shapes drawn in code.
- **Art made here** (not supplied) has a blank background: everything around the figure is spaces, with no
  dotted or dashed backdrop, frame or stray specks, and the figure fills the frame.
- **Readable at a glance.** Face, hands and the one object that says who they are (bow, parasol, crown)
  must stand out from the body. Raise the contrast inside the figure if the subject is mostly one tone.
- **Gear pictures** (`Gear/`) are small line drawings and keep their own shapes; the rules above are for portraits.

File names are the enemy's or class's `ArtKey`: `Heroes/<class>.txt`, `Enemies/<ArtKey>.txt`.

**Placeholder.** An enemy whose `Enemies/<ArtKey>.txt` doesn't exist yet shows `Enemies/placeholder.txt`, a plain
question mark. To give it real art, add the file under its ArtKey; nothing else changes. Waiting on art now:
`goblinbrute` (Goblin Brute) and `goblinshaman` (Goblin Shaman). The Goblin Cutpurse reuses `goblin`.

## The tool

`tools/asciiart.py` needs only Python.

```
python tools/asciiart.py check Content/ASCII          # lists any art that breaks the rules
python tools/asciiart.py clean Content/ASCII/Enemies/goblin.txt
```

`clean` blanks a uniform backdrop (the fill that reaches the edges, plus the specks floating in it) and trims the margins.
A busy background, like swirls or scenery behind the figure, needs the figure traced by hand instead.

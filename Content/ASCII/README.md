# ARDO ASCII art standard

Every picture in this folder follows the same rules, so the game looks like one piece.

## The rules

- **Bright is dense.** The game draws light green on black, so `@` is the brightest cell and a space is black.
  Use the ramp ` .:-=+*#%@` (dark to bright) and nothing else in portraits.
- **No background.** Everything around the figure is spaces. No dotted or dashed backdrop, no frame, no stray specks.
- **The figure fills the frame.** Trim empty rows and columns; the panel scales the font to fit.
- **Portrait size.** At most 100 columns wide; about 55 to 70 rows tall (90 at most).
- **Drawn from a real picture.** A portrait starts from a drawing or painting turned into ASCII
  (like `Enemies/ravenwitch.txt`), not from shapes drawn in code.
- **Readable at a glance.** Face, hands and the one object that says who they are (bow, parasol, crown)
  must stand out from the body. Raise the contrast inside the figure if the subject is mostly one tone.
- **Gear pictures** (`Gear/`) are small line drawings and keep their own shapes; the rules above are for portraits.

File names are the enemy's or class's `ArtKey`: `Heroes/<class>.txt`, `Enemies/<ArtKey>.txt`.

## The tool

`tools/asciiart.py` needs only Python.

```
python tools/asciiart.py check Content/ASCII          # lists any art that breaks the rules
python tools/asciiart.py clean Content/ASCII/Enemies/goblin.txt
```

`clean` blanks a uniform backdrop (the fill that reaches the edges, plus the specks floating in it) and trims the margins.
A busy background, like swirls or scenery behind the figure, needs the figure traced by hand instead.

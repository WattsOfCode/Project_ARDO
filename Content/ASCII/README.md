# ARDO ASCII art standard

Every picture in this folder follows the same rules, so the game looks like one piece.

## The rules

- **Bright is dense.** The game draws light green on black, so `@` is the brightest cell and a space is black.
  Use the ramp ` .:-=+*#%@` (dark to bright) and nothing else in portraits.
- **Supplied art is used as given.** Art the designer provides (like `Enemies/grisk.txt` and
  `Enemies/cindermatron.txt`) goes in exactly as sent, background and all, unless they ask for it to be cut out
  (as `Enemies/ravenwitch.txt` was).
- **Any size.** The portrait panel shrinks the font until the whole picture fits and centers it, so a detailed
  250 by 205 picture shows in full. Up to 300 columns and 260 rows.
- **Drawn from a real picture.** A portrait starts from a drawing or painting turned into ASCII, not from
  shapes drawn in code.
- **Art made here** (not supplied) has a blank background: everything around the figure is spaces, with no
  dotted or dashed backdrop, frame or stray specks, and the figure fills the frame.
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

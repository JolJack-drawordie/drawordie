================================================================
 PIXEL CARD UI
================================================================

A retro pixel-art card UI pack for Unity. Five color themes built from
modular layers (Base / Symbol / Hinge / Monster with cutout Window
variants), two card backs per theme, and a generous set of shared
decorations (frames, ribbons, badges, icons). A pixel font ready for
TextMeshPro is included.


----------------------------------------------------------------
 FEATURES
----------------------------------------------------------------

- 5 card themes - Basic, Red, Blue, Green, Purple
- Layered card art - each card is composed of 4 main front parts
  (Base / Symbol / Hinge / Monster), each with a Window cutout variant,
  so you can recolor, swap symbols, or mix-and-match parts without
  editing PNGs
- 2 card backs per theme - SunMoon, MagicStar
- 23 shared decoration sprites - rarity badges (Common / Epic /
  Legendary), ornamental frames (4 corners + slot), attack/defense
  icons, 5 ribbon colors, 4 card backgrounds, 2 background panels,
  FX overlays
- 6 ready-to-use prefabs in Prefabs/
- Demo scene showing all six cards laid out in a Canvas
- Pixel-perfect import settings - Point filter, mipmaps off,
  no compression on the default platform
- TextMeshPro SDF asset bundled - crisp pixel text out of the box
- Source PSD files included - 4 master files (Front, Back, Complete, Monster) for deep customization


----------------------------------------------------------------
 REQUIREMENTS
----------------------------------------------------------------

- Unity 2021.3 LTS or newer
- Compatible with Built-in, URP, and HDRP (UI-only)


----------------------------------------------------------------
 QUICK START
----------------------------------------------------------------

1. Open PixelCardUI/Demo/DEMO.unity to see all six cards in action.

2. To use in your own scene:
   1) Create a Canvas: GameObject > UI > Canvas
   2) Set the Canvas Scaler to "Scale With Screen Size" with a
      reference resolution that's a clean multiple of the card size
      (cards are 156 x 236 px).
   3) Drag any prefab from PixelCardUI/Prefabs/ as a child of
      the Canvas.

3. Customize:
   - Recolor instantly - tint the Base or Hinge Image.color field
   - New card types - swap the Symbol layer sprite
   - Custom artwork - replace the Window sprite to mask your own image
   - New rarity - duplicate one of the Badge sprites and tint as needed


----------------------------------------------------------------
 FOLDER STRUCTURE
----------------------------------------------------------------

PixelCardUI/
  Demo/                        Demo scene (DEMO.unity)
  Font/                        NeoDunggeunmoPro pixel font, TMP SDF asset, OFL.txt
  Prefabs/                     6 ready-to-use card prefabs (01_Basic ~ 05_Purple + 06_Monster)
  PSD/                         Source Photoshop files (Front/Back/Complete/Monster masters, .psb format)
  Previews/                    Marketing preview images
  Sprites/
    01_Basic/                  Beige card layers + 2 card backs
    02_Red/                    Red card layers + 2 card backs
    03_Blue/                   Blue card layers + 2 card backs
    04_Green/                  Green card layers + 2 card backs
    05_Purple/                 Purple card layers + 2 card backs
    06_Common_Decorations/     Shared frames, badges, ribbons, icons, BGs, FX


----------------------------------------------------------------
 PIXEL-PERFECT TIPS
----------------------------------------------------------------

- Keep the Canvas reference resolution as a clean multiple of the
  source sprite size. The card art is 156 x 236, so 1248 x 1888 (x8)
  is a safe target for desktop.

- If pixel edges look soft, enable Pixel Perfect on the Canvas
  (Screen Space - Camera) or use the 2D Pixel Perfect package.

- If you ship to Standalone and notice subtle pixel smearing,
  override the Standalone texture import setting to None
  (compression off). The Default platform setting in this pack
  is already None.

- The bundled font SDF is generated at Padding 5 / Sampling Point
  Size 16. Re-bake at a higher resolution if you intend to display
  the font at very large sizes.


----------------------------------------------------------------
 FONT LICENSE
----------------------------------------------------------------

The bundled font NeoDunggeunmoPro-Regular is
(c) Eunbin Jeong (Dalgona.) and is distributed under the
SIL Open Font License, Version 1.1. The full license text is in
Font/OFL.txt.

In short, you may freely use, modify and redistribute the font as
part of this package or your own products, but you may not sell the
font by itself as a standalone product.


----------------------------------------------------------------
 ASSET LICENSE
----------------------------------------------------------------

All sprites, prefabs, scenes, and TextMeshPro assets in this package
are (c) IndigoLay and are licensed under the Unity Asset Store EULA.
You may use them in commercial and non-commercial projects, but you
may not redistribute them as standalone assets, sprite sheets, or
art packs.


----------------------------------------------------------------
 SUPPORT
----------------------------------------------------------------

For bug reports or feature requests, please contact:
support@indigolay.com

When reporting an issue, please include:
- Unity version
- Render pipeline (Built-in / URP / HDRP)
- Steps to reproduce


----------------------------------------------------------------
 UPDATES
----------------------------------------------------------------

Version 1.1
- Monster card layer - new 4th front part (Base / Symbol / Hinge /
  Monster), available across all 5 color themes
- 05_Monster.prefab - new ready-to-use prefab demonstrating the
  Monster layer
- UI_Card_Monster.psb - source PSD master file for the Monster layer

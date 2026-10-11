"""Composes the loading page: night sky + moon + hill (painted here), the kin layer rendered in Unity, and the title.
Writes loading_art.png (title only: the game adds its own 'Loading world...' line) and splash.png (with 'Loading...')."""
import math, random, sys
from PIL import Image, ImageDraw, ImageFilter, ImageFont

import os
P = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # the project folder (this file lives in Tools/)
TITLE = 'KINVELIS'   # the game's name as shown on the loading pictures (Core/GameInfo.cs has it for the menus)
W, H = 2400, 1080   # 20:9, like most phones; everything that matters sits in the middle 1920 so 16:9 screens only lose empty sky
KW = 1920            # width of the kin layer as rendered
kin = Image.open(P + r'\Previews\Loading\kin_layer.png').convert('RGBA')
assert kin.size == (KW, H), kin.size
# pull the outermost kin in from the edges a little: scale the layer about its centre
S = 0.97
k2 = kin.resize((int(KW * S), int(H * S)), Image.LANCZOS)
layer = Image.new('RGBA', (W, H), (0, 0, 0, 0))
ox, oy = (W - k2.width) // 2, int(H * 0.06)
layer.paste(k2, (ox, oy), k2)

def lerp(a, b, t): return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))

bg = Image.new('RGB', (W, H))
px = bg.load()
top, bot = (34, 44, 120), (12, 14, 48)
for y in range(H):
    c = lerp(top, bot, y / (H - 1))
    for x in range(W): px[x, y] = c
d = ImageDraw.Draw(bg, 'RGBA')
# glow, moon, stars
mx, my, mr = W // 2, int(H * 0.50), int(H * 0.33)
glow = Image.new('RGBA', (W, H), (0, 0, 0, 0))
gd = ImageDraw.Draw(glow)
gd.ellipse([mx - mr * 2.1, my - mr * 2.1, mx + mr * 2.1, my + mr * 2.1], fill=(120, 140, 255, 70))
glow = glow.filter(ImageFilter.GaussianBlur(120))
bg.paste(glow, (0, 0), glow)
d = ImageDraw.Draw(bg, 'RGBA')
rnd = random.Random(11)
for _ in range(190):
    x, y, r = rnd.uniform(0, W), rnd.uniform(0, H * 0.74), rnd.choice([1.2, 1.6, 2.0, 2.8])
    d.ellipse([x - r, y - r, x + r, y + r], fill=(255, 255, 255, rnd.randint(130, 255)))
moon = Image.new('RGBA', (W, H), (0, 0, 0, 0))
md = ImageDraw.Draw(moon)
for i in range(40, 0, -1):
    t = i / 40.0
    md.ellipse([mx - mr * t, my - mr * t, mx + mr * t, my + mr * t], fill=lerp((255, 244, 190), (255, 212, 128), t) + (255,))
bg.paste(moon, (0, 0), moon)
# the hill they stand on
hill = Image.new('RGBA', (W, H), (0, 0, 0, 0))
hd = ImageDraw.Draw(hill)
cx, cy, rx, ry = W / 2, H * 1.20, KW * 1.35, H * 0.42
hd.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], fill=(30, 96, 78, 255))
hd.ellipse([cx - rx, cy - ry + 14, cx + rx, cy + ry + 14], fill=(22, 72, 62, 255))
bg.paste(hill, (0, 0), hill)
# soft shadows where their feet are (viewport fractions from the Unity render, scaled like the layer)
feet = {'hexpurr': (0.789, 0.757), 'lilute': (0.943, 0.799), 'prestofox': (0.708, 0.781), 'knuckub': (0.292, 0.781), 'bulwarden': (0.057, 0.799), 'kagekit': (0.500, 0.813)}
sh = Image.new('RGBA', (W, H), (0, 0, 0, 0))
sd = ImageDraw.Draw(sh)
for name, (fx, fy) in feet.items():
    x, y = ox + fx * KW * S, oy + fy * H * S
    w = 105 if name != 'hexpurr' else 70
    sd.ellipse([x - w, y - 20, x + w, y + 20], fill=(4, 18, 20, 150 if name != 'hexpurr' else 80))
sh = sh.filter(ImageFilter.GaussianBlur(9))
bg.paste(sh, (0, 0), sh)
art = bg.convert('RGBA')
art.alpha_composite(layer)

def text(img, s, size, y, fill, stroke, font_file=r'C:\Windows\Fonts\impact.ttf', spacing=0):
    f = ImageFont.truetype(font_file, size)
    dr = ImageDraw.Draw(img)
    # letter-spaced, centred
    widths = [dr.textlength(ch, font=f) for ch in s]
    total = sum(widths) + spacing * (len(s) - 1)
    x = (W - total) / 2
    for ch, wch in zip(s, widths):
        dr.text((x, y), ch, font=f, fill=fill, stroke_width=stroke, stroke_fill=(14, 16, 50, 255))
        x += wch + spacing

title = art.copy()
text(title, TITLE, 180, int(H * 0.07), (255, 236, 160, 255), 10, spacing=14)
title.convert('RGB').save(P + r'\Previews\Loading\loading_art.png')
splash = title.copy()
text(splash, 'Loading...', 56, int(H * 0.86), (235, 240, 255, 255), 5, font_file=r'C:\Windows\Fonts\bahnschrift.ttf', spacing=2)
splash.convert('RGB').save(P + r'\Previews\Loading\splash.png')
print('ok')

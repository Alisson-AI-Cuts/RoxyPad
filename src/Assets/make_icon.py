from PIL import Image, ImageDraw, ImageFont, ImageFilter
from pathlib import Path

OUT = Path(__file__).parent
SIZE = 512

def cubic(a, b, c, d, steps=100):
    points = []
    for i in range(steps + 1):
        t = i / steps
        u = 1 - t
        points.append((round(u**3*a[0] + 3*u*u*t*b[0] + 3*u*t*t*c[0] + t**3*d[0]), round(u**3*a[1] + 3*u*u*t*b[1] + 3*u*t*t*c[1] + t**3*d[1])))
    return points

def gradient(size, stops, horizontal=False):
    w, h = size
    img = Image.new("RGBA", size)
    px = img.load()
    axis = w if horizontal else h
    for y in range(h):
        for x in range(w):
            p = (x if horizontal else y) / max(1, axis - 1)
            color = stops[-1][1]
            for i in range(len(stops)-1):
                at0, c0 = stops[i]
                at1, c1 = stops[i+1]
                if at0 <= p <= at1:
                    t = (p-at0)/(at1-at0)
                    color = tuple(round(c0[k]*(1-t)+c1[k]*t) for k in range(4))
                    break
            px[x,y] = color
    return img

base = gradient((SIZE,SIZE), [(0,(27,17,44,255)),(.52,(20,20,41,255)),(1,(13,29,48,255))], True)
glow = Image.new("RGBA", base.size, (0,0,0,0))
gd = ImageDraw.Draw(glow)
gd.ellipse((260,-120,650,300), fill=(45,132,223,118))
gd.ellipse((130,210,550,640), fill=(112,45,211,106))
base = Image.alpha_composite(base, glow.filter(ImageFilter.GaussianBlur(95)))

ribbons = [
    (cubic((488,-30),(505,80),(298,152),(341,272)) + cubic((341,272),(382,371),(411,414),(329,550)), 108,
     [(0,(37,84,194,0)),(.42,(106,52,220,190)),(1,(47,139,219,185))]),
    (cubic((533,5),(468,103),(356,165),(384,275)) + cubic((384,275),(411,350),(374,414),(363,535)), 44,
     [(0,(55,169,234,20)),(.48,(116,64,222,205)),(1,(77,126,229,180))]),
]
for points, width, stops in ribbons:
    mask = Image.new("L", base.size, 0)
    ImageDraw.Draw(mask).line(points, fill=255, width=width, joint="curve")
    halo = mask.filter(ImageFilter.GaussianBlur(width*.62))
    color = Image.new("RGBA", base.size, (126,75,244,0))
    color.putalpha(halo.point(lambda x: round(x*.23)))
    base = Image.alpha_composite(base, color)
    band = gradient(base.size, stops)
    band.putalpha(mask.point(lambda x: round(x*.50)))
    base = Image.alpha_composite(base, band)

edge = cubic((424,-10),(431,130),(330,199),(352,294)) + cubic((352,294),(373,373),(395,411),(343,541))
edge_layer = Image.new("RGBA",base.size,(0,0,0,0))
ImageDraw.Draw(edge_layer).line(edge, fill=(202,160,255,130), width=3, joint="curve")
base = Image.alpha_composite(base, edge_layer.filter(ImageFilter.GaussianBlur(6)))
ImageDraw.Draw(base).line(edge, fill=(214,180,255,220), width=2, joint="curve")

font = ImageFont.truetype(str(OUT / "Poppins-Bold.ttf"), 352)
letter = "R"
bbox = ImageDraw.Draw(Image.new("L",(1,1))).textbbox((0,0),letter,font=font)
tw, th = bbox[2]-bbox[0], bbox[3]-bbox[1]
tx = (SIZE-tw)//2 - bbox[0] - 9
ty = (SIZE-th)//2 - bbox[1] - 5
shadow = Image.new("RGBA", base.size, (0,0,0,0))
ImageDraw.Draw(shadow).text((tx+5,ty+9),letter,font=font,fill=(0,0,0,155),stroke_width=2,stroke_fill=(0,0,0,90))
base = Image.alpha_composite(base,shadow.filter(ImageFilter.GaussianBlur(6)))
glyph_mask = Image.new("L", base.size, 0)
ImageDraw.Draw(glyph_mask).text((tx,ty),letter,font=font,fill=255)
glyph = gradient(base.size,[(0,(250,237,255,255)),(1,(185,207,255,255))])
glyph.putalpha(glyph_mask)
base = Image.alpha_composite(base,glyph)

shape = Image.new("L",base.size,0)
ImageDraw.Draw(shape).rounded_rectangle((4,4,508,508),radius=120,fill=255)
base.putalpha(shape)
border = Image.new("RGBA",base.size,(0,0,0,0))
ImageDraw.Draw(border).rounded_rectangle((5,5,507,507),radius=119,outline=(207,174,255,115),width=3)
base=Image.alpha_composite(base,border)
base.save(OUT / "RoxyPad.png")
base.save(OUT / "RoxyPad.ico", format="ICO", sizes=[(256,256),(128,128),(64,64),(48,48),(32,32),(16,16)])

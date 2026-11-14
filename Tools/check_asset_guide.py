"""Check PDF contents and make page contact sheets for visual review."""
import json
from pathlib import Path
import re
import zipfile
from PIL import Image, ImageDraw
from pypdf import PdfReader

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'output/pdf'
TMP=ROOT/'tmp/pdfs'
pdf=OUT/'Tiny-Giant-Problems-3D-Model-Guide.pdf'
rows=json.loads((OUT/'asset-catalogue.json').read_text())
reader=PdfReader(pdf)
assert len(reader.pages)==36
for a in rows:
    page=reader.pages[a['id']+2]
    text=page.extract_text()
    assert a['title'] in text, a['id']
    assert 'AVOID:' in text and 'COPY-READY MODEL PROMPT' in text, a['id']
    assert len(page.images)==1, (a['id'],len(page.images))
    prompt=(OUT/'prompts'/f"{a['slug']}.txt").read_text(encoding='utf-8')
    assert prompt==a['prompt']
    assert re.sub(r'\s+','',prompt) in re.sub(r'\s+','',text), f"Prompt missing text: {a['id']}"
    with Image.open(OUT/a['image']) as im: im.verify()
print('PASS: 36 pages; all 32 prompts complete and paired with one valid reference image.')
pages=sorted(TMP.glob('page-*.png'))
assert len(pages)==36,len(pages)
for start in range(0,len(pages),9):
    sheet=Image.new('RGB',(1200,1740),'#d9ded6')
    draw=ImageDraw.Draw(sheet)
    for j,path in enumerate(pages[start:start+9]):
        with Image.open(path) as im:
            im.thumbnail((380,540))
            x=10+(j%3)*400+(380-im.width)//2; y=25+(j//3)*580
            sheet.paste(im,(x,y))
            draw.text((x,y-17),f'Page {start+j+1}',fill='black')
    sheet.save(TMP/f'contact-{start//9+1}.jpg',quality=90)
archive=OUT/'Tiny-Giant-Problems-Reference-Pack.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as z:
    for folder in ('reference-images','prompts'):
        for path in sorted((OUT/folder).iterdir()): z.write(path,path.relative_to(OUT))
    for name in ('asset-catalogue.json','image-provenance.json'):
        z.write(OUT/name,name)
print(f'PDF {pdf.stat().st_size/1e6:.1f} MB; companion pack {archive.stat().st_size/1e6:.1f} MB')

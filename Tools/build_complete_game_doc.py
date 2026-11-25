"""Compile the current project overview and preserved design records into a PDF."""
from pathlib import Path
import re
from html import escape
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, PageBreak, Image, Table, TableStyle, Preformatted
from reportlab.platypus.tableofcontents import TableOfContents
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from PIL import Image as PILImage

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'output/pdf'
OUT.mkdir(parents=True, exist_ok=True)
for name, filename in [('Body','arial.ttf'), ('BodyBold','arialbd.ttf'), ('BodyItalic','ariali.ttf')]:
    pdfmetrics.registerFont(TTFont(name, 'C:/Windows/Fonts/'+filename))
pdfmetrics.registerFontFamily('Body', normal='Body', bold='BodyBold', italic='BodyItalic', boldItalic='BodyBold')
styles = getSampleStyleSheet()
styles.add(ParagraphStyle('Text', fontName='Body', fontSize=9, leading=13, spaceAfter=7, textColor=colors.HexColor('#263a3b')))
styles.add(ParagraphStyle('Title2', fontName='BodyBold',fontSize=32, leading=37, textColor=colors.HexColor('#194e47'),spaceAfter=18))
styles.add(ParagraphStyle('Section',fontName='BodyBold',fontSize=17,leading=21,spaceBefore=17,spaceAfter=10,keepWithNext=True,textColor=colors.HexColor('#194e47')))
styles.add(ParagraphStyle('Sub',fontName='BodyBold',fontSize=12,leading=16,spaceBefore=12,spaceAfter=7,keepWithNext=True,textColor=colors.HexColor('#194e47')))
styles.add(ParagraphStyle('Cell',fontName='Body',fontSize=7.5,leading=10,spaceAfter=0,wordWrap='CJK'))
styles.add(ParagraphStyle('Caption2',fontName='BodyItalic',fontSize=8,leading=11,spaceAfter=12,textColor=colors.HexColor('#566a6b')))
styles.add(ParagraphStyle('Mono2',fontName='Courier',fontSize=6.4,leading=8,spaceAfter=8))

def inline(s):
    s=s.replace('—',' - ').replace('–','-').replace('‑','-').replace('→',' -> ').replace('↔',' <-> ').replace('≥','>=').replace('≤','<=').replace('−','-')
    s=escape(s)
    s=re.sub(r'`([^`]+)`',r'<font color="#35695d">\1</font>',s)
    s=re.sub(r'\*\*([^*]+)\*\*',r'<b>\1</b>',s)
    s=re.sub(r'\*([^*]+)\*',r'<i>\1</i>',s)
    s=re.sub(r'\[([^\]]+)\]\(([^)]+)\)',r'\1 (\2)',s)
    return s

story=[]
def para(s, style='Text'): return Paragraph(inline(s),styles[style])
def heading(s, level=0):
    p=para(s,'Section' if level==0 else 'Sub')
    p.toc_level=level
    story.append(p)

def markdown(text, toc=False):
    lines=text.splitlines(); i=0
    while i<len(lines):
        line=lines[i].strip()
        if not line or line=='---': i+=1; continue
        if line.startswith('```'):
            code=[]; i+=1
            while i<len(lines) and not lines[i].startswith('```'):
                code.append(lines[i].replace('→','->').replace('—','-'));i+=1
            import textwrap
            wrapped='\n'.join('\n'.join(textwrap.wrap(x, width=105, replace_whitespace=False, drop_whitespace=False)) if len(x)>105 else x for x in code)
            story.append(Preformatted(wrapped,styles['Mono2']));i+=1;continue
        if line.startswith('|'):
            rows=[]
            while i<len(lines) and lines[i].strip().startswith('|'):
                cells=lines[i].strip().strip('|').split('|')
                if not all(re.fullmatch(r'[\s:–-]+',c) for c in cells): rows.append([para(c.strip(),'Cell') for c in cells])
                i+=1
            n=max(map(len,rows)); rows=[r+[para('','Cell')]*(n-len(r)) for r in rows]
            widths=[468/n]*n
            t=Table(rows,colWidths=widths,repeatRows=1,hAlign='LEFT')
            t.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,0),colors.HexColor('#dceae4')),('ROWBACKGROUNDS',(0,1),(-1,-1),[colors.HexColor('#f4f7f5'),colors.white]),('VALIGN',(0,0),(-1,-1),'TOP'),('LEFTPADDING',(0,0),(-1,-1),6),('RIGHTPADDING',(0,0),(-1,-1),6),('TOPPADDING',(0,0),(-1,-1),6),('BOTTOMPADDING',(0,0),(-1,-1),6),('LINEBELOW',(0,0),(-1,0),.6,colors.HexColor('#a4bdb2'))]))
            story.extend([t,Spacer(1,10)]);continue
        if line.startswith('#'):
            count=len(line)-len(line.lstrip('#')); title=line.lstrip('#').strip()
            if toc and count<=2: heading(title,1)
            else: story.append(para(title,'Section' if count<=2 else 'Sub'))
            i+=1;continue
        chunks=[line.lstrip('> ')]
        i+=1
        if not re.match(r'^([-*]|\d+\.)\s',line):
            while i<len(lines) and lines[i].strip() and not re.match(r'^(#|\||```|---|[-*] |\d+\. )',lines[i].strip()):
                chunks.append(lines[i].strip().lstrip('> '));i+=1
        story.append(para(' '.join(chunks)))

def figure(filename, caption, maxheight=270):
    p=ROOT/'Screenshots'/filename
    if not p.exists(): return
    w,h=PILImage.open(p).size; scale=min(468/w,maxheight/h)
    story.extend([Image(str(p),w*scale,h*scale),Spacer(1,6),para(caption,'Caption2')])

story.extend([Spacer(1,35),para('TINY GIANT\nPROBLEMS'.replace('\n',' '),'Title2'),para('Complete game design & development record','Sub'),para('Vision, playable systems, art direction, technical implementation and remaining production work.'),Spacer(1,15)])
figure('Mossback-Rig-Fixed-Eyes-Left.png','Mossback: current supplied model, fitted harness and eye-corrected skeleton preview.',300)
story.extend([Spacer(1,14),para('Project snapshot | November 16, 2026','Sub'),para('Includes the current status overview, the complete original design proposal and preserved development records. Plans and implemented features are explicitly distinguished.'),PageBreak()])
heading('Contents')
toc=TableOfContents();toc.levelStyles=[ParagraphStyle('TOC0',fontName='BodyBold',fontSize=10,leading=14,spaceBefore=9),ParagraphStyle('TOC1',fontName='Body',fontSize=8,leading=10,leftIndent=12,spaceBefore=1)]
story.extend([toc,PageBreak()])
heading('Part I | Current design and development overview')
overview=(ROOT/'Docs/Game-Design-Current-Overview.md').read_text(encoding='utf-8')
markdown(overview,toc=True)
story.append(PageBreak());heading('Visual record of the playable world')
figure('Polished-Harness.png','Fitted harness: wooden decking, access ladders, rails and supporting rigging.',280)
figure('Redecorated-Camp.png','Camp and forest dressing. Props do not imply completed contract-board or upgrade systems.',280)
story.append(PageBreak());heading('Visual record of the creatures')
figure('Mossback-Rig-Fixed-Eyes-Right.png','Mossback: right-facing look with eyes fixed relative to the skull.',280)
figure('Bramblemite-Rig-A.png','Bramblemite: current six-legged pest model with an 18-joint skeleton.',280)

original=ROOT/'Tiny Giant Problems/Tiny_Giant_Problems_GDD_v0.1.md'
story.append(PageBreak());heading('Part II | Complete original design proposal')
story.append(para('Historical proposal dated November 10, 2026. Preserve its ideas and alternatives; use Part I for current implementation status. External references below are retained from the original document and have not been freshly reverified.'))
markdown(original.read_text(encoding='utf-8-sig'),toc=True)

files=['Mossback-Technical-Specification.md','Development-Log.md','3D-Asset-Checklist.md','GiantTiny-Import-Report.md','Map-Redecoration.md','Replacement-Models-2026-11-13.md','Harness-Polish-And-Idle-Motion.md','Mossback-Collision.md','Mossback-Skeleton.md','Mossback-Animator-Behaviors.md','World-Collision-And-Bramblemite.md']
story.append(PageBreak());heading('Part III | Detailed implementation records')
story.append(para('These source records describe successive development stages. Later replacement, rigging, animation and collision records supersede earlier graybox and unrigged-model descriptions. File paths refer to the repository; they are included for developer handoff.'))
combined=overview+'\n\n# Part II | Original design proposal (historical)\n\n'+original.read_text(encoding='utf-8-sig')+'\n\n# Part III | Detailed implementation records\n'
for index, name in enumerate(files):
    source=ROOT/'Docs'/name
    body=source.read_text(encoding='utf-8-sig')
    if index: story.append(PageBreak())
    heading(name.replace('.md','').replace('-',' '),1)
    story.append(para('Source: Docs/'+name+' | Historical implementation record; see Part I for reconciled status.','Caption2'))
    markdown(body)
    combined+='\n\n## Source: Docs/'+name+'\n\n'+body
(ROOT/'Docs/Tiny-Giant-Problems-Complete-Game-Document.md').write_text(combined,encoding='utf-8')

class Document(SimpleDocTemplate):
    def afterFlowable(self,flowable):
        if hasattr(flowable,'toc_level'):
            title=flowable.getPlainText()
            if title=='Contents': return
            key='h'+str(self.seq.nextf('heading'))
            self.canv.bookmarkPage(key)
            self.notify('TOCEntry',(flowable.toc_level,title,self.page,key))

def footer(canvas,doc):
    canvas.saveState()
    canvas.setStrokeColor(colors.HexColor('#c5d6cf'));canvas.line(54,42,522,42)
    canvas.setFont('Body',7);canvas.setFillColor(colors.HexColor('#597069'))
    canvas.drawString(54,29,'TINY GIANT PROBLEMS  |  Design & development record  |  16 Nov 2026')
    canvas.drawRightString(522,29,str(doc.page));canvas.restoreState()

pdf=OUT/'Tiny-Giant-Problems-Complete-Game-Document.pdf'
doc=Document(str(pdf),pagesize=(576,756),rightMargin=54,leftMargin=54,topMargin=46,bottomMargin=56,title='Tiny Giant Problems - Complete Game Design and Development Record',author='Tiny Giant Problems project')
doc.multiBuild(story,onFirstPage=footer,onLaterPages=footer)
print(pdf)
import pymupdf
d=pymupdf.open(pdf)
print('Pages:',len(d))
preview=ROOT/'tmp/pdfs/game-doc';preview.mkdir(parents=True,exist_ok=True)
for i,page in enumerate(d):
    pix=page.get_pixmap(matrix=pymupdf.Matrix(.45,.45),alpha=False)
    pix.save(str(preview/f'{i+1:03}.png'))
thumbs=[]
for start in range(0,len(d),24):
    group=list(range(start,min(start+24,len(d))))
    sheet=PILImage.new('RGB',(6*280,4*370),'#cdd6d2')
    from PIL import ImageDraw
    draw=ImageDraw.Draw(sheet)
    for j,i in enumerate(group):
        im=PILImage.open(preview/f'{i+1:03}.png');sheet.paste(im,((j%6)*280,(j//6)*370));draw.text(((j%6)*280+5,(j//6)*370+343),str(i+1),fill='black')
    sheet.save(preview/f'contact-{start//24+1}.png')
print('Preview:',preview)

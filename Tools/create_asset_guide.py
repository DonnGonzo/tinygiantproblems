"""Author the project's model prompt catalogue and illustrated PDF."""
import json
from pathlib import Path
import sys
from io import BytesIO
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'output/pdf'

# title, phase, approximate scale, form/material brief, separate parts, special avoid
ROWS = [
('Guild technician','CURRENT','1.65 m tall',
 'Friendly adult miniature creature-care worker with a rounded silhouette, compact torso, large readable hands and sturdy boots. Coral padded jacket, dark forest trousers, cream gloves, ochre safety helmet and a teal utility backpack with brass clasps. Practical clothing, simple friendly face, clear elbow and knee articulation. Neutral symmetrical A-pose with empty hands, fingers slightly separated and feet shoulder-width apart.',
 'Body, helmet and backpack as separate meshes; gloves and boots may stay on the body. Leave clearance for shoulder and hip movement. Rig-ready humanoid topology.',
 'weapons, military armor, childlike body proportions, fused fingers, extra fingers or limbs, tools in hands, giant backpack, dynamic pose'),
('Mossback harness rig','CURRENT','Fit to the existing 3x Mossback mesh',
 'Gentle padded creature harness with broad dark brown leather bands, ochre stitching, rounded brass buckles, teal padded saddle contact pieces and large closed attachment rings. Distribute loads across broad cushions. Use adjustable bands and clear joints. Leave the neck, legs, face and dorsal spikes unobstructed. Treat this as modular equipment fitted around the supplied creature, not a replacement creature.',
 'Separate strap sections, buckle frame, buckle pin, ring and padded saddle support. Do not bake a creature into the mesh. Final fitting must use the actual imported model.',
 'creature geometry, tight bands over joints, spikes or fur embedded in straps, sharp metal against skin, sealed rigid body cage, weapons'),
('Decking and rope ladder kit','CURRENT','Deck module 3 x 3 m; ladder width 0.8 m',
 'Modular guild work platform with thick warm brown timber planks, rounded corners, brass corner plates and dark leather lashings. Include a straight deck section, a matching edge section and one hanging rope ladder with thick tan side ropes and evenly spaced sturdy wooden rungs. Clear climbing gaps and unobstructed walking surface. Storybook handmade construction with modest wear.',
 'Deck modules and ladder as separate objects. Ladder top hooks separate; repeatable rung and rope segments. Keep mounting edges straight and snapping dimensions consistent.',
 'creature body, floating unattached decorative pieces, splinter hazards, irregular rung spacing, blocked walkway, baked terrain'),
('Bramblemite','CURRENT','0.6 m long',
 'Round mischievous fantasy pest resembling a soft burr crossed with a beetle. Coral-red segmented oval shell, dark brown underside, exactly six short stubby legs, two short rounded antennae and two expressive dark eyes. Sparse blunt bramble nubs across the shell. Friendly troublemaker silhouette, no realistic disease imagery. Neutral crawling stance with all six feet visible and separated.',
 'Body, six legs and antennae clearly distinguishable for rigging. Keep the underside simple and free of overlapping parts.',
 'realistic ticks, blood, gore, sharp fangs, extra legs, eight spider legs, wings, needles, terrifying eyes'),
('Pest vacuum','CURRENT','0.75 m long',
 'Handheld creature-care vacuum with a wide rounded bell nozzle, short ribbed hose coupling, compact teal housing, ochre upper handle, brown rubber grip and detachable cream collection canister. A small smoked viewing window shows the canister is hollow without requiring visible pests. Large simple brass latch and a clearly readable intake direction. Friendly workshop appliance silhouette.',
 'Nozzle, housing, handle and canister separate. Hose port open and round; optional flexible hose added later. Provide grip clearance for a gloved hand.',
 'gun barrel, trigger guard resembling a firearm, bullets, trapped animals, built-in hand, excessively thin hose, liquid effects'),
('Grooming brush','CURRENT','0.55 m long',
 'Oversized creature grooming brush with a gently curved oval teal head, ochre back plate, comfortable brown handle and widely spaced short cream rubber bristles with rounded tips. Broad readable silhouette, soft tool suitable for untangling mossy fur. A brass hanging ring at the handle end. Bristles arranged in a tidy sparse grid.',
 'Handle, head and bristle rows separate where supported. Rounded bristles must have real thickness and clear spacing.',
 'razor edges, needles, dense individual hairs, metal spikes, comb teeth fused together, giant creature attached'),
('Soothing sprayer','CURRENT','0.65 m long',
 'Handheld soothing-care sprayer with a compact mint-teal bottle tank below a cream rounded pump housing, broad brass mist nozzle, ochre squeeze lever and a dark brown ergonomic grip. Include a large cap and simple recessed level window. A calm leaf-shaped embossed symbol may identify the tank without lettering. Soft practical curves and matte finish.',
 'Tank, cap, lever and nozzle separate; leave a hose connector if needed. Empty neutral state with no emitted particles.',
 'flamethrower silhouette, needles, weapon sights, spray clouds, liquid streams, text labels, glass too thin to model'),
('Rope launcher','CURRENT','0.65 m long',
 'Compact rescue rope launcher built around an exposed chunky brass spool in a teal protective frame, ochre carry handle and broad rounded guide opening. Brown padded grip, simple latch, a rounded docking cradle for a separate closed rescue hook. Read as rescue equipment and a hand winch rather than a firearm. No rope is deployed.',
 'Spool, housing, crank or lever and rescue hook separate. Rope exit socket centered and easy to locate; the game draws deployed rope.',
 'firearm receiver, ammunition, sharp spear or harpoon, long baked rope, thin tangled cables, glowing weapon effects'),
('Anchor and connector kit','CURRENT','Anchor post 0.8 m tall; carabiner 0.18 m',
 'Reusable guild safety attachment set: a short brass anchor post with broad padded teal base and large closed top eye, a thick rounded brass carabiner with visibly separate spring gate, and one closed attachment ring. Clear functional openings large enough to read in gameplay. Cream grip accents and restrained wear.',
 'Anchor, carabiner body, carabiner gate and ring separate. Position gate pivot at hinge. Keep the ring opening empty and clean.',
 'sharp ground spike aimed into an animal, fused gate, filled holes, long rope, microscopic screws, extra decorative hooks'),
('Tangled fur clump','CURRENT','1.2 m wide',
 'Removable tangled patch of Mossback-colored fur and moss, made of chunky layered sage and olive locks curled into a soft central knot. Warm brown roots, gently rounded tips and a broad shallow underside suitable for placement on a creature. Large readable strands, roughly oval silhouette, modest irregularity. It must look brushable and harmless.',
 'One main clump with a simple flat-ish attachment underside. Optionally separate three large curled locks for a grooming disappearance animation.',
 'skin wounds, blood, individual hair simulation, razor blades, full creature, rocks, hard crystal spikes'),
('Burr cluster','CURRENT','0.45 m across',
 'A small removable cluster of three warm ochre seed burrs with large blunt hook-shaped nubs, brown stems and a few muted green leaves. Clearly botanical debris with soft stylized tips. One larger central burr and two smaller companions, readable spaces between hooked nubs.',
 'Three seed heads and stems separable for pickup variations. All hooks should be thick enough for a clean mesh.',
 'needle spines, realistic parasites, blood, barbed wire, thousands of tiny thorns, merged leaf planes'),
('Caught branch','CURRENT','1.5 m long',
 'A gently forked fallen branch caught during a woodland migration. Warm brown bark with broad hand-painted grooves, two blunt broken ends, one short side branch and three simple sage leaves. Slight curve, lightweight and easily grabbable. Thick readable forms and restrained surface detail.',
 'Main branch as one connected mesh; leaf cluster separate if possible. Keep a clear central gripping area.',
 'sharp spear tip, entire tree, excessive twigs, tiny bark flakes, ropes, embedded creature fur, scenery'),
('Supply crate','CURRENT','0.9 x 0.7 x 0.65 m',
 'Small guild supply crate of thick warm timber planks with rounded brass corner protectors, dark green straps, a hinged lid and two recessed side handles. Ochre latch at the front. Sturdy stable rectangular silhouette, softly worn corners and a clean empty interior if the lid is opened. No contents required.',
 'Crate body, lid and front latch separate, lid hinge at rear. Carry handles may be integrated with visible openings.',
 'unreadable labels, nailed-shut lid, elaborate treasure chest jewelry, scattered inventory, oversized gaps, sharp nails'),
('Treatment bottle','CURRENT','0.25 m tall',
 'Squat reusable soothing-gel bottle with an opaque mint-teal body, wide cream screw cap, rounded shoulders and a recessed leaf emblem. Small brown protective sleeve around the base and one brass cap tether loop. Broad stable bottom, easy to identify among supplies. Matte ceramic-like coating rather than transparent medical glass.',
 'Bottle and cap separate with simple clean mating surfaces. Keep emblem shallow and geometric.',
 'syringes, pills, real medical brand labels, written dosage, thin glass, pouring liquid, extra bottles'),
('Spare canister','CURRENT','0.35 m tall',
 'Detachable guild vacuum collection canister: cream cylindrical capsule with rounded ends, a teal protective rim, chunky brass locking collar, dark smoked inset inspection window and a short sealed connector on top. Compact carrying handle on its side. Match the pest vacuum family and leave it empty.',
 'Canister body, locking collar and top cap separate. Connector must match the vacuum socket during final assembly.',
 'gas weapon cylinder, trapped pests, glass fragility, unreadable measuring text, liquid, excessive valves'),
('Tool case','CURRENT','0.7 x 0.45 x 0.25 m',
 'Compact teal guild tool case with rounded rectangular body, ochre corner pads, two chunky brass latches, a brown carry handle and a hinged lid. Slightly handmade hard-shell look with broad flat surfaces and subtle wear. Show the closed case, with a visible lid seam and functional hinge.',
 'Base, lid, handle and latches separate. Hinge axis follows the long rear edge. Interior may be a simple empty compartment.',
 'tools fused into case, military ammunition markings, logos, excessive compartments, fragile corners, open exploded assembly'),
('Extraction beacon','CURRENT','1.2 m tall',
 'Portable guild signal lantern on a short sturdy tripod. Rounded brass cage, cream translucent-looking lamp chamber, teal base housing and an ochre top carry handle. Three broad brown feet and one large side switch. Clear tall silhouette visible across a creature back. The lamp material can glow later; model the physical lamp only.',
 'Lantern housing, lens, switch and three legs separate. Use closed simple lens geometry and leave space for a light source.',
 'giant light beams, bloom haze, magical runes, text, skinny unstable legs, cables trailing across ground'),
('Highland tree kit','ENVIRONMENT','4-12 m tall',
 'Three related stylized highland trees: a rounded broadleaf, a tall slender broadleaf and a small wind-bent tree. Warm brown tapered trunks, clearly joined thick branches and clustered sage/olive canopy masses. Large leaf clumps with a softly sculpted silhouette, flat roots suitable for terrain placement. Match a peaceful storybook meadow.',
 'Each tree separate; trunk and canopy separate for variants and wind. Do not connect the three trees or include terrain.',
 'photoreal leaf cards, thousands of individual leaves, giant roots, fruit, animals, terrain base, a whole forest'),
('Rock and stone arch kit','ENVIRONMENT','Boulders 0.8-3 m; arch opening 5 m',
 'A coordinated highland stone set: three rounded weathered boulders and one broad natural stone arch with a clear walk-through opening. Desaturated warm gray rock, broad planar facets softened at edges, sparse sage moss patches in creases. Stable flat lower contacts and readable silhouettes. Arch should look structurally substantial.',
 'Each boulder separate. Arch may be split into left support, right support and top span with concealed overlap joints.',
 'razor crystals, floating stones, narrow fragile arch crown, baked grass field, inscriptions, caves, buildings'),
('Meadow plant kit','ENVIRONMENT','0.15-0.8 m tall',
 'Five coordinated small plant assets: a grass tuft with broad curved blades, a low moss cushion, a simple fern with thick readable fronds, a rounded leafy shrub, and a cluster of cream flowers with ochre centers. Sage, olive and muted teal greens. Chunky stylized foliage with visible gaps and calm natural curves.',
 'Five independent clumps, each centered at its ground contact. Leaf and petal groups may be simplified solid meshes; no shared soil base.',
 'microscopic foliage, photoreal vegetation scans, thin tangled strands, mushrooms, vases, pots, terrain tiles, floating roots'),
('Boarding dock','ENVIRONMENT','6 x 4 m deck, 2 m high',
 'Small welcoming guild boarding dock built from thick timber planks and posts, with a broad short stairway, simple wooden side rails, brass corner brackets, rope handline and a blank hanging signboard. One open boarding edge faces the creature. Warm brown wood, ochre fittings and a restrained teal trim. Clear pedestrian route and rounded edges.',
 'Deck, stairs, rail sections, posts and signboard separate for snapping. Keep the boarding edge unobstructed and the support feet on one ground plane.',
 'creature, water, terrain, people, lettering, buildings, swinging rope bridges, missing stairs, rails across the boarding opening'),
('Portable scanner','LATER','0.3 m tall',
 'Palm-sized guild field scanner with a rounded teal body, cream recessed screen, thick ochre protective frame, two large brass buttons and a brown wrist strap loop. A small rounded sensor dome on top. Screen is blank dark green for a future UI overlay. Friendly rugged field instrument, easy to hold in a gloved hand.',
 'Body, screen surface, buttons and strap loop separate. Keep sensor unobstructed and rear grip shallow.',
 'readable text, holograms, phone brands, futuristic guns, dozens of tiny buttons, hands, thin dangling cables'),
('Multi-grabber tongs','LATER','0.8 m long',
 'Two-handed humane pest-grabbing tongs with two broad curved teal padded jaws, a chunky brass central pivot, brown handles and an ochre locking slider. Jaws open slightly with a clear empty gap. Rounded contact pads designed to grip burrs or pests gently. Simple mechanically plausible linkage.',
 'Left arm, right arm, pivot pin, pads and slider separate. Both arms rotate around the same hinge axis.',
 'sharp pincers, cutting blades, serrated jaws, trapped animal, extra arms, impossible linkage, closed fused jaws'),
('Rescue glider','LATER','3 m wingspan when deployed',
 'Compact wearable rescue glider with two symmetrical ochre canvas wings stretched over thick curved wooden spars, teal reinforced fabric patches, brass hinges and a compact brown backpack mounting plate. Gentle shallow V shape, clearly readable center mount, rounded wing tips. Show fully deployed neutral configuration without a pilot.',
 'Left wing, right wing, spars and central mount separate. Hinges aligned for folding; canopy has thickness and simple seams.',
 'person, aircraft cockpit, engine, feathers, shredded fabric, loose suspension line tangles, asymmetric wings, motion blur'),
('Rescue winch','LATER','0.8 m wide',
 'Portable guild rescue winch with a wide brass cable drum in a stable teal frame, large ochre hand crank, brown padded mounting base and two closed attachment eyes. Rounded mechanical shapes, readable reel axis and comfortable crank grip. Show an orderly short coil on the drum, no deployed rope.',
 'Drum, frame, crank and attachment eyes separate. Crank and drum pivots share the correct rotation axis.',
 'engine exhaust, weapon elements, long loose ropes, dangling victim, dense gears, thin fragile mounting feet'),
('Snack launcher','LATER','0.65 m long',
 'Friendly creature treat dispenser with a broad shallow cream serving cup at the front, round teal hopper behind it, ochre hand lever and brown padded grip. Brass retaining band and a hinged refill lid. Rounded kitchen-appliance-like silhouette with no barrel. Show empty neutral equipment.',
 'Hopper, lid, serving cup and lever separate. Cup should be open and thick-walled, refill opening accessible.',
 'firearm shape, bullets, explosive mechanisms, launched food, giant candy decorations, hands, text'),
('Survey drone','LATER','0.6 m across',
 'Small whimsical guild survey drone with a rounded teal central pod, cream front camera lens, brass protective rim and exactly four short arms ending in enclosed ochre rotor guards. Brown carry grip on top and two short landing skids. Friendly appliance personality from its single lens, no face painted on it.',
 'Pod, four rotor guards, four rotors, camera gimbal and skids separate. All four rotors have clear spinning clearance.',
 'weapons, military styling, extra rotors, exposed sharp blades, flying trails, faces, text screens, long antenna wires'),
('Baby creature retrieval target','LATER','0.8 m tall',
 'A gentle young fantasy valley creature inspired by the current Mossback family: rounded moss-green body, cream muzzle, two large soft ears, exactly four short sturdy legs, two kind dark eyes, a small curved tail and sparse soft leaf-like tufts down the back. Oversized head and compact juvenile proportions, standing neutrally with visible separated feet. Affectionate and curious.',
 'Rig-ready body with clear limb, ear, jaw and tail roots. Keep eyes separate where supported. This is a future creature concept, not a change to the adult model.',
 'extra legs, horns, hard spikes, teeth, snarling, baby clothing, wings, adult giant proportions, sad injury cues'),
('Guild workshop furniture kit','LATER','Workbench 1.8 x 0.7 x 0.9 m',
 'Coordinated workshop furniture set: a sturdy timber workbench with teal drawer fronts and brass pulls, a simple matching stool, and a freestanding open supply shelf with three broad shelves. Rounded edges, thick legs, warm wood grain and restrained ochre trim. Empty surfaces for placing separate equipment later.',
 'Bench, stool and shelf separate; drawers separate with clear slide direction. Flat feet, no shared floor base.',
 'tools or clutter fused to furniture, entire room, walls, people, excessive tiny drawers, text labels, delicate chair legs'),
('Technician cosmetic kit','LATER','Fit the 1.65 m technician',
 'Three removable technician accessories: a soft ochre brimmed field hat, a short coral neck scarf with two broad rounded tails, and a compact teal belt pouch with a brass clasp. Match the guild technician material language. Show the three pieces separately with enough space to identify their mounting openings.',
 'Hat, scarf and pouch independent. Hollow head opening and belt loop visible; fit them to the actual technician mesh after import.',
 'character body or mannequin, long trailing cloth, giant hat, weapons, logos, tiny jewelry, fused accessories'),
('Shellstrider','LATER','Colossal second creature; size to be playtested',
 'Gentle colossal tortoise-like mountain wanderer with exactly four thick legs, a long soft neck, rounded beaked muzzle, warm amber eyes and a short tail. Broad high shell formed from large layered warm-gray stone-like plates, deep readable seams and restrained moss growth. Shell provides wide terraces and shallow channels for tiny caretakers. Friendly ancient expression, neutral standing pose.',
 'Body and shell separate; major shell plates, head, legs and tail identifiable for rigging. Keep playable shell surfaces continuous and broad.',
 'hostile spikes, castles, trees or people baked into shell, extra legs, enormous fangs, floating plates, narrow sharp ridges, blood'),
('Skywhale','STRETCH','Colossal flying creature; size to be playtested',
 'Peaceful airborne whale-like fantasy giant with a broad gently domed back, rounded cream belly, muted blue-teal upper body, two broad side fins and a wide horizontal tail fluke. Soft amber eyes, rounded muzzle and subtle flowing ridges along its flanks. Wide stable-looking back for miniature care expeditions. Serene neutral hovering pose with fins extended, full body visible.',
 'Body, fins and tail roots cleanly separated for deformation. Keep a broad continuous dorsal surface. No equipment or riders attached.',
 'extra fins, feathers, bird wings, shark teeth, jets, mechanical engines, clouds, ocean, people, buildings, bioluminescent clutter'),
]

STYLE = ('Use a cohesive hand-painted storybook 3D game style: rounded readable forms, exaggerated functional details, '
         'matte tactile materials, warm timber and leather, muted sage/forest greens, teal equipment, ochre accents, '
         'cream trim and restrained brass. Keep shapes readable at third-person camera distance.')
COMMON_AVOID = ('Avoid photorealism, glossy plastic, microscopic surface noise, unnecessary greebles, text, logos, '
                'watermarks, scenery, display pedestals, cropped parts, floating fragments, intersecting meshes, '
                'unintentionally fused moving parts and baked lighting or shadows in the base-color texture.')

def catalogue():
    rows = []
    for i, (title, phase, scale, form, parts, avoid) in enumerate(ROWS, 1):
        slug = f'{i:02d}-' + title.lower().replace(' ','-')
        style = STYLE
        if i in (4,10,11,12,18,19,20,28,31,32):
            style = ('Use a cohesive hand-painted storybook 3D game style with rounded readable forms, '
                     'matte natural materials, broad color blocking and restrained surface detail. '
                     'Use only the natural colors and materials specified below. Do not add equipment or accessories.')
        if i in (31,32):
            avoid += ', harnesses, saddles, bags, straps, cloth, buckles, lanterns, jewelry, platforms, water, waterfalls and other equipment'
        if i == 21:
            form = form.replace('rope handline and a blank hanging signboard','a blank signboard')
            avoid += ', crates, loose bags, lanterns, coiled ropes, barrels, props or clutter placed on the deck'
        prompt = (f'Create {title.lower()} for Tiny Giant Problems, a cooperative fantasy creature-care game. '
                  f'Use the attached reference image as the visual target for silhouette, color blocking and materials. '
                  f'{style}\n\nFORM AND DETAILS: {form}\n\n'
                  f'SCALE AND ASSEMBLY: Suggested scale: {scale}. {parts} '
                  'Use meters, Y-up and a clear forward direction. Put ground props at their base pivot and handheld tools '
                  'at a useful grip pivot. Supply a textured GLB or FBX with UVs and base-color, roughness and normal maps '
                  'if supported. Preserve clean geometry and practical joint clearance; geometry and rigging must be checked after import.\n\n'
                  'PRESENTATION: Complete isolated asset, neutral pose or resting configuration, all parts visible. '
                  'For a modular kit, export each listed component as an independent named mesh. If the generator only supports '
                  'one object, generate one listed component at a time while retaining the same design language.\n\n'
                  f'AVOID: {avoid}. {COMMON_AVOID}')
        image_prompt = (f'Use case: stylized-concept. Create a clean 3D asset reference image of {title.lower()} for '
                        f'Tiny Giant Problems. {form} {STYLE} '
                        'Single three-quarter orthographic-like view, soft even studio lighting on a plain light warm-gray '
                        'background. Entire asset fully visible with generous margins. If a kit is specified, show only its '
                        'listed components neatly spaced apart, no shared base. No dimensions, no labels, no text, no watermark. '
                        f'Avoid {avoid}. Do not include people unless this is the technician character. No scenery or pedestal.')
        rows.append(dict(id=i,title=title,phase=phase,scale=scale,form=form,parts=parts,avoid=avoid,
                         slug=slug,prompt=prompt,image_prompt=image_prompt,image=f'reference-images/{slug}.png'))
    return rows

def write_catalogue():
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT/'prompts').mkdir(exist_ok=True)
    rows = catalogue()
    (OUT/'asset-catalogue.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
    for a in rows:
        (OUT/'prompts'/f"{a['slug']}.txt").write_text(a['prompt'],encoding='utf-8')
    print(f'Wrote {len(rows)} complete model prompts.')

def build_pdf():
    from reportlab.pdfgen import canvas
    from reportlab.lib.colors import HexColor
    from reportlab.lib.pagesizes import A4
    from reportlab.platypus import Paragraph
    from reportlab.lib.styles import ParagraphStyle
    from reportlab.pdfbase import pdfmetrics
    from reportlab.pdfbase.ttfonts import TTFont
    from PIL import Image
    rows = catalogue()
    for a in rows:
        assert (OUT/a['image']).exists(), a['image']
    pdfmetrics.registerFont(TTFont('Guide', 'C:/Windows/Fonts/calibri.ttf'))
    pdfmetrics.registerFont(TTFont('GuideBold', 'C:/Windows/Fonts/calibrib.ttf'))
    pdfmetrics.registerFontFamily('Guide', normal='Guide',bold='GuideBold')
    W,H=A4
    ink=HexColor('#233C36'); teal=HexColor('#356F69'); gold=HexColor('#BD8B3F')
    paper=HexColor('#FAF8F2'); muted=HexColor('#61716B')
    path=OUT/'Tiny-Giant-Problems-3D-Model-Guide.pdf'
    c=canvas.Canvas(str(path),pagesize=A4,pageCompression=1)
    c.setTitle('Tiny Giant Problems | Illustrated 3D Model Guide')
    c.setAuthor('Small Works Guild - project asset planning')
    c.setSubject('32 model briefs, integrated avoid instructions and generated reference images')
    def para(text,x,y,width,size=11,leading=None,color=ink):
        p=Paragraph(text,ParagraphStyle('p',fontName='Guide',fontSize=size,leading=leading or size*1.3,textColor=color))
        _,h=p.wrap(width,H)
        p.drawOn(c,x,y-h)
        return y-h
    def top(kicker,title):
        c.setFillColor(paper);c.rect(0,0,W,H,fill=1,stroke=0)
        c.setFillColor(teal);c.setFont('GuideBold',9);c.drawString(40,H-36,kicker)
        para(escape(title),40,H-53,W-80,24,27)
    def footer():
        c.setStrokeColor(HexColor('#D9DED6'));c.line(40,35,W-40,35)
        c.setFont('Guide',8);c.setFillColor(muted)
        c.drawString(40,22,'TINY GIANT PROBLEMS  /  MODEL REFERENCE GUIDE')
        c.drawRightString(W-40,22,str(c.getPageNumber()))
        c.showPage()
    def picture(path,x,y,w,h):
        with Image.open(path) as im:
            iw,ih=im.size
            # Compress only the PDF embedding; retain full-resolution source PNGs in the pack.
            encoded=BytesIO()
            im.convert('RGB').save(encoded,format='JPEG',quality=92,optimize=True)
            encoded.seek(0)
        scale=min(w/iw,h/ih); dw,dh=iw*scale,ih*scale
        from reportlab.lib.utils import ImageReader
        c.drawImage(ImageReader(encoded),x+(w-dw)/2,y+(h-dh)/2,dw,dh)
    top('SMALL WORKS GUILD / ASSET ART DIRECTION','Tiny Giant Problems')
    para('Illustrated 3D model guide',40,H-90,W-80,21,color=teal)
    para('32 detailed model briefs with matching reference images',40,H-125,W-80,12)
    picture(OUT/rows[0]['image'],40,280,250,380)
    picture(OUT/rows[4]['image'],305,450,250,190)
    picture(OUT/rows[3]['image'],305,265,250,190)
    para('<b>Ready to copy into your model generator.</b><br/>Each asset includes its visual target, suggested scale, assembly requirements and a complete prompt with the avoid instructions already included.',40,240,W-80,13,18)
    para('Current playable assets + environment kit + later expansion concepts<br/>Revision 1 / November 11, 2026',40,130,W-80,10,color=muted)
    footer()
    top('READ FIRST','How to use this guide')
    y=H-112
    sections=[
      ('1. Attach the matching image','Upload the original PNG named on the asset page. Paste the entire prompt, including its AVOID paragraph. A reference image improves consistency but does not guarantee correct geometry, scale, UVs or rigging.'),
      ('2. Generate one usable object at a time','A kit image shows the design family. For a one-object generator, select a single listed part and state its name first in the prompt. Exclude every other component from that generation. A second view of the same object, such as the open crate, is not a second required object.'),
      ('3. Keep the scale meaningful','Dimensions are proposed authoring targets, not measured output guarantees. The technician remains 1.65 m tall. Mossback already exists and is now 3x its initial imported size; its current scene mesh bounds are approximately 54 m wide, 54 m high and 180 m long. Fit harness geometry to the actual mesh and gameplay route, not to these bounds alone.'),
      ('4. Export and check','Use textured GLB or FBX. Inspect the model from all sides, verify openings and underside geometry, separate moving pieces, set pivots, test materials and add appropriate simplified collision. Generated images show intended appearance, not tested engineering drawings.'),
      ('5. Rigging is a separate task','The technician, Bramblemite and future creatures need skeletons, weights and animation. Mossback still needs a rig and proper walk, ear, sneeze and shake animations. A neutral pose request does not create a working rig by itself.'),
      ('Scope and provenance','CURRENT means artwork for the existing playable loop or its immediate polish. ENVIRONMENT dresses the level. LATER and STRETCH are planned additions, not features already implemented. Briefs expand the project GDD and asset checklist with proposed visual details. All reference illustrations were made with the built-in image-generation tool for this guide.'),
    ]
    for title,body in sections:
        y=para('<b>'+title+'</b>',40,y,W-80,12,15)-5
        y=para(body,40,y,W-80,10.5,14)-18
    assert y>45,y
    footer()
    top('PRODUCTION ORDER','Asset index')
    y=H-113
    for phase,label in [('CURRENT','Current playable'),('ENVIRONMENT','Environment'),('LATER','Later expansion'),('STRETCH','Stretch goal')]:
        y=para('<b>'+label+'</b>',40,y,W-80,11.5,14)-6
        for a in [a for a in rows if a['phase']==phase]:
            c.setFillColor(ink);c.setFont('Guide',10)
            c.drawString(44,y-10,f"{a['id']:02d}  {a['title']}")
            c.drawRightString(W-44,y-10,str(a['id']+3))
            c.linkRect('',f"asset-{a['id']}",(40,y-14,W-40,y+2),relative=0,thickness=0)
            y-=16
        y-=10
    assert y>42,y
    footer()
    heights=[]
    for a in rows:
        c.bookmarkPage(f"asset-{a['id']}")
        c.addOutlineEntry(f"{a['id']:02d} {a['title']}",f"asset-{a['id']}",level=0)
        top(f"MODEL {a['id']:02d} / {a['phase']}",a['title'])
        # Fixed image slot, preserving aspect ratio; the original PNG is packaged separately.
        picture(OUT/a['image'],40,H-350,W-80,238)
        y=para('REFERENCE PNG: '+escape(a['slug']+'.png'),40,H-359,W-80,8.2,color=muted)-12
        y=para('<b>COPY-READY MODEL PROMPT</b>',40,y,W-80,9.5,color=teal)-8
        for block in a['prompt'].split('\n\n'):
            txt=escape(block)
            for label in ['FORM AND DETAILS:', 'SCALE AND ASSEMBLY:', 'PRESENTATION:', 'AVOID:']:
                txt=txt.replace(label,'<b>'+label+'</b>')
            y=para(txt,40,y,W-80,10.25,13.2)-7
        heights.append((a['id'],round(y,1)))
        assert y>46, f"Asset {a['id']} overflows: {y}"
        footer()
    top('HANDOFF','Before an asset enters the game')
    y=H-112
    for title,body in [
      ('Silhouette first','Compare the result with its reference from the gameplay camera distance. Keep handles, limbs, rungs, jaws and rings readable. Fix holes, duplicated pieces, fused components or missing backside surfaces before integration.'),
      ('Movement and fit','Test tool grips against the technician hands. Test harness contact against Mossback at its current scene scale. Keep straps clear of the neck, ears, legs and dorsal details. Locate hinges and rotation pivots before animation.'),
      ('Performance','Use sensible material counts, check texture resolution at actual viewing distance, create LODs where useful, and use simple collision proxies. A polygon target is a production decision, not something the reference image can enforce.'),
      ('What does not need a separate generated model','Draw rope dynamically. Make hot spots with decals or materials, and mist, dust, glow and sneeze effects with VFX. Hills can remain terrain or procedural meshes. Water and clouds chiefly need shader/VFX work. Reuse the tool cases, canisters and scanner as lost-equipment targets.'),
      ('Files in the companion pack','reference-images/ contains the 32 full-resolution PNGs. prompts/ contains matching copy-ready TXT prompts. asset-catalogue.json records every brief and the image-generation prompts. Kit previews are visual assemblies; their components still need separate mesh exports.'),
      ('Project references','Design basis: Tiny_Giant_Problems_GDD_v0.1.md and Docs/3D-Asset-Checklist.md in the project. Proposed sizes and exact colors in this guide are new art-direction choices. Future creature concepts do not replace the imported Mossback.'),
    ]:
        y=para('<b>'+title+'</b>',40,y,W-80,12)-5
        y=para(body,40,y,W-80,11,15)-21
    assert y>46,y
    footer();c.save()
    (OUT/'layout-check.json').write_text(json.dumps(heights,indent=2))
    print(f'Created {path} ({len(rows)+4} pages). Lowest content edge: {min(h[1] for h in heights)} pt.')

if __name__ == '__main__':
    write_catalogue()
    if '--pdf' in sys.argv:
        build_pdf()

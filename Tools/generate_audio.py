"""Generate the small prototype audio set. Reads ELEVENLABS_API_KEY from the environment.
Never stores credentials. Existing files are skipped to avoid duplicate charges.
"""
from pathlib import Path
from elevenlabs import ElevenLabs
import json

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/_TinyGiantProblems/Audio'
client = ElevenLabs()
items = [
    ('Music/MossbackMeadow.mp3', 'music', 'Instrumental whimsical storybook adventure underscore, warm felt piano, pizzicato strings, wooden flute and soft marimba, gentle walking pace 88 BPM, curious tiny caretakers exploring a gentle enormous moss creature, pastoral wonder, playful and cozy, restrained dynamics, no vocals, no dramatic percussion', 45),
    ('SFX/Sneeze.mp3', 'sfx', 'One huge friendly fantasy animal sneeze: a rising breathy nasal inhale for three seconds then a comical deep fluffy achoo with a gust of air, warm non scary cartoon creature, clean isolated sound, no music or speech', 5),
    ('SFX/Shake.mp3', 'sfx', 'A giant furry friendly creature shakes its thick leafy fur, low warning rumble followed by rustling leaves and rattling wooden harness, comical and soft, no music or speech', 5),
    ('SFX/CareComplete.mp3', 'sfx', 'Short warm magical success chime, three ascending wooden marimba notes with delicate sparkles, cozy fantasy game completed care task, clean isolated, no speech', 2),
    ('SFX/Spray.mp3', 'sfx', 'Gentle handheld soothing mist sprayer, soft airy spray and tiny watery bubbles, clean isolated game tool loop, no music or voices', 2),
]
manifest = []
for rel, kind, prompt, seconds in items:
    target = OUT / rel
    target.parent.mkdir(parents=True, exist_ok=True)
    if target.exists():
        print(f'Skipping existing {rel}', flush=True)
        continue
    try:
        if kind == 'music':
            stream = client.music.compose(prompt=prompt, music_length_ms=seconds * 1000, model_id='music_v2', force_instrumental=True)
        else:
            stream = client.text_to_sound_effects.convert(text=prompt, duration_seconds=seconds, model_id='eleven_text_to_sound_v2', prompt_influence=0.5)
        data = b''.join(stream)
        target.write_bytes(data)
        manifest.append({'file': rel, 'provider': 'ElevenLabs', 'prompt': prompt, 'bytes': len(data)})
        print(f'Generated {rel}: {len(data)} bytes', flush=True)
    except Exception as exc:
        # Exception type only: provider errors can include request details.
        print(f'Generation failed for {rel}: {type(exc).__name__}', flush=True)
        break
(ROOT / 'Docs/audio-provenance.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')

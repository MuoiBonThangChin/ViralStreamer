"""Validate the full asset contract; exit nonzero on any failure."""
from pathlib import Path
import wave, math
import numpy as np
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[2]
ART=ROOT/'Assets/Game/Resources/Art'; AUDIO=ROOT/'Assets/Game/Resources/Audio'
large='gear_webcam gear_mic gear_headset gear_keyboard gear_lights gear_chair gear_pc gear_greenscreen decor_plant decor_poster decor_lamp decor_shelf decor_neon decor_trophy skill_tap skill_engage skill_crit skill_hype skill_offline'.split()
small='icon_coin icon_gem icon_heart icon_viewer icon_follower icon_gift icon_boost icon_star icon_lock icon_check tab_gear tab_skills tab_room tab_outfit'.split()
expected={'bg_room':(1080,1920),**{'streamer_'+k:(768,1024) for k in ['casual','gamer','idol','cyber','royal']},**{k:(256,256) for k in large},**{k:(128,128) for k in small},**{k:(96,96) for k in ['ui_panel','ui_button','ui_pill']},'ui_glow':(256,256),'ui_circle':(128,128),'ui_sparkle':(128,128),'ui_live':(160,64)}
durations=dict(zip('sfx_tap sfx_crit sfx_coin sfx_buy sfx_error sfx_gift sfx_hype sfx_rankup sfx_tab bgm_loop'.split(),[.08,.25,.25,.3,.18,.6,.9,1.4,.06,20]))
errors=[];rows=[]
def check(ok,message):
    if not ok:errors.append(message)
for name,size in expected.items():
    p=ART/(name+'.png')
    if not p.exists():errors.append('Missing '+str(p));continue
    with Image.open(p) as im:
        check(im.format=='PNG' and im.mode=='RGBA' and im.size==size,name+' format/size')
        amin,amax=im.getchannel('A').getextrema();check(amax==255,name+' lacks opaque pixels')
        check(amin==255 if name=='bg_room' else amin==0,name+' alpha contract')
        if name in large+small:
            box=im.getchannel('A').point(lambda a:255 if a>16 else 0).getbbox()
            check(box is not None and box[0]>=size[0]*.065 and box[1]>=size[1]*.065 and box[2]<=size[0]*.935 and box[3]<=size[1]*.935,name+' padding')
        if name.startswith('streamer_'):check(im.getchannel('A').crop((0,size[1]-1,size[0],size[1])).getbbox() is not None,name+' waist not touching bottom')
        if name in ['ui_panel','ui_button','ui_pill','ui_circle']:
            check(im.getpixel((size[0]//2,size[1]//2))==(255,255,255,255),name+' white opaque centre')
            check(im.getpixel((0,0))[3]==0,name+' transparent corner')
        if name=='ui_button':check(im.getpixel((48,92))==(218,218,218,255),name+' grey bottom band')
        if name=='ui_glow':
            check(im.getpixel((127,127))==(255,255,255,255),name+' centre')
            check(im.getpixel((0,127))[3]==0,name+' edge')
        rows.append(f'{p.name:27} {im.width:4} x {im.height:<4} RGBA {p.stat().st_size:9} bytes')
for name,duration in durations.items():
    p=AUDIO/(name+'.wav')
    if not p.exists():errors.append('Missing '+str(p));continue
    with wave.open(str(p),'rb') as f:
        sr=f.getframerate();length=f.getnframes()/sr
        check((sr,f.getsampwidth(),f.getnchannels(),f.getcomptype())==(44100,2,1,'NONE'),name+' encoding')
        check(abs(length-duration)<1/sr,name+' duration')
        a=np.frombuffer(f.readframes(f.getnframes()),dtype='<i2').astype(float)/32767
        peak=20*math.log10(max(np.abs(a).max(),1e-12));check(abs(peak-(-14 if name=='bgm_loop' else -3))<.05,name+' peak')
        if name=='bgm_loop':check(abs(a[0]-a[-1])<2/32767,name+' seam')
        else:check(abs(a[0])+abs(a[-1])==0,name+' endpoint fade')
        rows.append(f'{p.name:27} {length:6.3f}s 44100 Hz / 16-bit / mono peak {peak:6.2f} dBFS')
text='\n'.join(rows)+f'\n\n{len(expected)} PNGs + {len(durations)} WAVs; {len(errors)} failures.\n'+'\n'.join(errors)
print(text);(ROOT/'tools/assetgen/verification.txt').write_text(text,encoding='utf-8')
# A labelled contact sheet for visual review, kept with helper outputs.
thumb=192;sheet=Image.new('RGB',(6*thumb,math.ceil(len(expected)/6)*(thumb+30)),'#302646');d=ImageDraw.Draw(sheet)
for i,name in enumerate(expected):
    p=ART/(name+'.png')
    if not p.exists():continue
    im=Image.open(p);im.thumbnail((180,180));x=(i%6)*thumb;y=(i//6)*(thumb+30)
    sheet.paste(im,(x+(thumb-im.width)//2,y),im);d.text((x+4,y+184),name,fill='white')
sheet.save(ROOT/'tools/assetgen/contact-sheet.jpg')
raise SystemExit(bool(errors))

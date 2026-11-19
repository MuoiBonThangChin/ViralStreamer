"""Finish generated illustrations, synthesize UI and original audio. Run from repo root."""
from pathlib import Path
import json, math, wave
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageOps, ImageFilter

ROOT=Path(__file__).resolve().parents[2]
ART=ROOT/'Assets/Game/Resources/Art'; AUDIO=ROOT/'Assets/Game/Resources/Audio'
ART.mkdir(parents=True,exist_ok=True); AUDIO.mkdir(parents=True,exist_ok=True)
S=4
def ui(name,size,draw):
    im=Image.new('RGBA',(size[0]*S,size[1]*S)); d=ImageDraw.Draw(im); draw(d)
    im.resize(size,Image.Resampling.LANCZOS).save(ART/(name+'.png'))
def rr(d,box,r,fill): d.rounded_rectangle(tuple(int(x*S) for x in box),radius=r*S,fill=fill)
ui('ui_panel',(96,96),lambda d:rr(d,(0,0,95.75,95.75),28,'white'))
def button(d):
    rr(d,(0,0,95.75,95.75),28,'#DADADA'); rr(d,(0,0,95.75,87.75),28,'white')
ui('ui_button',(96,96),button)
ui('ui_pill',(96,96),lambda d:rr(d,(0,0,95.75,95.75),48,'white'))
ui('ui_circle',(128,128),lambda d:d.ellipse((0,0,511,511),fill='white'))
y,x=np.mgrid[:256,:256]; alpha=np.clip((127.5-np.sqrt((x-127.5)**2+(y-127.5)**2))/(127.5-math.sqrt(.5)),0,1)
im=np.full((256,256,4),255,dtype=np.uint8); im[:,:,3]=np.round(alpha*255); Image.fromarray(im).save(ART/'ui_glow.png')
def sparkle(d):
    d.polygon([(256,12),(302,210),(500,256),(302,302),(256,500),(210,302),(12,256),(210,210)],fill='white')
ui('ui_sparkle',(128,128),sparkle)
im=Image.open(ART/'ui_sparkle.png'); im.filter(ImageFilter.GaussianBlur(.55)).save(ART/'ui_sparkle.png')
def live(d):
    rr(d,(0,0,159.75,63.75),32,'#FF3B5C');d.ellipse((16*S,24*S,32*S,40*S),fill='white')
    font=ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf',32*S)
    d.text((43*S,13*S),'LIVE',font=font,fill='white',stroke_width=0)
ui('ui_live',(160,64),live)

manifest=json.loads((ROOT/'tools/assetgen/sources.json').read_text(encoding='utf-8-sig'))
for name,item in manifest.items():
    im=Image.open(item['path']).convert('RGBA'); size=tuple(item['size'])
    if name=='bg_room':
        im=ImageOps.fit(im,size,method=Image.Resampling.LANCZOS); im.putalpha(255)
    else:
        # Preserve generated alpha, trim transparent margins, and enforce game framing.
        box=im.getchannel('A').getbbox()
        if box: im=im.crop(box)
        char=name.startswith('streamer_'); bounds=(round(size[0]*.94),size[1]) if char else (round(size[0]*.84),round(size[1]*.84))
        im.thumbnail(bounds,Image.Resampling.LANCZOS)
        canvas=Image.new('RGBA',size); canvas.alpha_composite(im,((size[0]-im.width)//2,size[1]-im.height if char else (size[1]-im.height)//2)); im=canvas
    im.save(ART/(name+'.png'))

SR=44100
rng=np.random.default_rng(1208)
def tone(freq,dur,kind='bell'):
    t=np.arange(round(dur*SR))/SR
    if kind=='pad': return (np.sin(2*np.pi*freq*t)+.22*np.sin(4*np.pi*freq*t))*np.sin(np.pi*np.minimum(t/.2,1)/2)*np.minimum((dur-t)/.25,1)
    return (np.sin(2*np.pi*freq*t)+.28*np.sin(2*np.pi*freq*2.003*t)+.12*np.sin(2*np.pi*freq*3*t))*np.exp(-t*9/max(dur,.1))
def add(a,b,start):
    k=round(start*SR); n=min(len(b),len(a)-k)
    if n>0:a[k:k+n]+=b[:n]
def write(name,a,db=-3,loop=False):
    if not loop:
        n=min(220,len(a)//4); a[:n]*=np.linspace(0,1,n);a[-n:]*=np.linspace(1,0,n)
    a=a/max(np.max(np.abs(a)),1e-9)*10**(db/20)
    with wave.open(str(AUDIO/(name+'.wav')),'wb') as f:f.setparams((1,2,SR,len(a),'NONE','not compressed'));f.writeframes(np.round(a*32767).astype('<i2').tobytes())
durations={'sfx_tap':.08,'sfx_crit':.25,'sfx_coin':.25,'sfx_buy':.30,'sfx_error':.18,'sfx_gift':.6,'sfx_hype':.9,'sfx_rankup':1.4,'sfx_tab':.06}
for name,dur in durations.items():
    a=np.zeros(round(dur*SR));t=np.arange(len(a))/SR
    if name in ('sfx_tap','sfx_crit','sfx_tab'):
        a=np.sin(2*np.pi*(900*t-350*t*t/dur))*np.exp(-t*7/dur)
        if name=='sfx_crit':add(a,.5*tone(2637,.18),.06);add(a,.3*tone(3520,.13),.12)
    elif name=='sfx_error':
        for start in [0,.09]:add(a,tone(100,.09),start)
    elif name=='sfx_coin':
        add(a,tone(987.7666,.13),0);add(a,tone(1318.5102,.15),.1)
    elif name=='sfx_buy':
        for i,f in enumerate([660,988,1319]):add(a,tone(f,.18),i*.06)
    elif name=='sfx_gift':
        for i,f in enumerate([523.25,659.25,783.99,1046.5,1318.5]):add(a,tone(f,.28),i*.075)
    elif name=='sfx_hype':
        noise=rng.normal(0,1,len(a));noise=np.convolve(noise,np.ones(12)/12,'same');a+=noise*np.sin(np.pi*np.minimum(t/.65,1))*.5
        for f in [523.25,659.25,783.99,1046.5]:add(a,tone(f,.4),.5)
    else:
        for i,f in enumerate([523.25,659.25,783.99,1046.5]):add(a,tone(f,.4),i*.12)
        for f in [523.25,659.25,783.99,1046.5]:add(a,.6*tone(f,.85,'pad'),.55)
    write(name,a)
# Eight bars at 96 BPM: exactly 20 seconds. Circular placement includes tails across seam.
dur=20.; n=round(dur*SR);a=np.zeros(n)
def wrapadd(signal,start):
    ids=(np.arange(len(signal))+round(start*SR))%n;np.add.at(a,ids,signal)
chords=[[130.8128,164.8138,195.9977,246.9417],[110,130.8128,164.8138,195.9977],[87.3071,110,130.8128,164.8138],[97.9989,123.4708,146.8324,195.9977]]
for bar in range(8):
    chord=chords[bar%4];start=bar*2.5
    for f in chord:wrapadd(.16*tone(f,3.,'pad'),start)
    for beat in range(4):
        tt=np.arange(round(.25*SR))/SR
        kick=np.sin(2*np.pi*(48*tt+4*(1-np.exp(-tt*35))))*np.exp(-tt*22)
        if beat in [0,2]:wrapadd(.3*kick,start+beat*.625)
        if beat in [1,3]:wrapadd(.09*rng.normal(size=len(tt))*np.exp(-tt*32),start+beat*.625)
        wrapadd(.1*tone(chord[beat]*4,.5),start+beat*.625+.3125)
# Match endpoints smoothly without a musical fade; period is exactly eight bars.
a-=np.mean(a);a[-128:]+=np.linspace(0,a[0]-a[-1],128)
write('bgm_loop',a,-14,True)
print(f'Built {len(manifest)+7} RGBA PNGs and 10 mono PCM WAVs.')

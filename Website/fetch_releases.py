from pathlib import Path
import urllib.request,hashlib,json,os
root=Path(__file__).resolve().parent
releases=json.loads((root/'releases.json').read_text())
(root/'downloads').mkdir(exist_ok=True)
for platform,release in releases.items():
    temporary=root/'downloads'/(release['file']+'.part')
    digest=hashlib.sha256()
    with urllib.request.urlopen('https://lunarrelay1.pythonanywhere.com/download/'+platform) as response, temporary.open('wb') as output:
        while chunk:=response.read(1024*1024):digest.update(chunk);output.write(chunk)
    if digest.hexdigest()!=release['sha256']:raise RuntimeError('Release checksum mismatch')
    os.replace(temporary,root/'downloads'/release['file'])
    print('Verified:',release['file'])

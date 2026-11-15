"""Run after replacing a release binary; standard library only."""
from pathlib import Path
import hashlib,json,os
ROOT=Path(__file__).resolve().parent
metadata={}
for platform,name in [('android','LunarRelay-Android.apk'),('windows','LunarRelay-Windows.zip')]:
    path=ROOT/'downloads'/name
    digest=hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda:stream.read(1024*1024),b''):digest.update(block)
    metadata[platform]={'file':name,'bytes':path.stat().st_size,'sha256':digest.hexdigest()}
target=ROOT/'releases.json'
temporary=target.with_suffix('.json.tmp')
temporary.write_text(json.dumps(metadata,indent=2)+'\n',encoding='utf-8')
os.replace(temporary,target)
print('Release metadata updated. Reload your PythonAnywhere web app.')

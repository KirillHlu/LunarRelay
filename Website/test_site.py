import unittest,hashlib,json,re
from pathlib import Path
from app import app,RELEASES,ROOT

class SiteTests(unittest.TestCase):
 def setUp(self): self.client=app.test_client()
 def test_home_and_assets(self):
  r=self.client.get('/');self.assertEqual(r.status_code,200)
  html=r.get_data(as_text=True);self.assertIn('Lunar Relay',html);self.assertIn('lang="en"',html);self.assertIsNone(re.search('[\u0400-\u04ff]',html))
  assets=set(re.findall(r'(?:src|href)="(/static/[^\"]+)"',html))
  self.assertGreater(len(assets),5)
  for asset in assets:
   with self.subTest(asset=asset):
    with self.client.get(asset) as asset_response:self.assertEqual(asset_response.status_code,200)
 def test_downloads_exact_hashes(self):
  for platform,release in RELEASES.items():
   with self.subTest(platform=platform):
    r=self.client.get('/download/'+platform,buffered=False)
    self.assertEqual(r.status_code,200);self.assertIn('attachment',r.headers['Content-Disposition'])
    digest=hashlib.sha256();size=0
    for chunk in r.response:digest.update(chunk);size+=len(chunk)
    r.close();self.assertEqual(digest.hexdigest(),release['sha256']);self.assertEqual(size,release['bytes'])
 def test_head_range_and_cache(self):
  for platform,release in RELEASES.items():
   path='/download/'+platform
   r=self.client.head(path);self.assertEqual(int(r.headers['Content-Length']),release['bytes']);self.assertEqual(r.data,b'');r.close()
   r=self.client.get(path,headers={'Range':'bytes=0-31'})
   self.assertEqual(r.status_code,206);self.assertEqual(len(r.data),32)
   self.assertEqual(r.data,(ROOT/'downloads'/release['file']).read_bytes()[:32]);r.close()
   with self.client.get(path,headers={'If-None-Match':'"'+release['sha256']+'"'}) as cached:self.assertEqual(cached.status_code,304)
 def test_invalid_paths_and_security(self):
  for path in ['/download/nope','/download/../../app.py','/missing']:
   self.assertEqual(self.client.get(path).status_code,404)
  self.assertEqual(self.client.get('/').headers['X-Content-Type-Options'],'nosniff')
 def test_checksums(self):
  r=self.client.get('/checksums.txt');self.assertEqual(r.status_code,200)
  for v in RELEASES.values():self.assertIn(v['sha256'],r.get_data(as_text=True))

if __name__=='__main__':unittest.main(verbosity=2)

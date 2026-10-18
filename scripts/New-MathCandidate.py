"""Offline candidate authoring. Does not run or inspect holdout data."""
import argparse,json
from decimal import Decimal,ROUND_HALF_EVEN
from pathlib import Path
parser=argparse.ArgumentParser()
parser.add_argument('--source',required=True);parser.add_argument('--version',required=True);parser.add_argument('--out',required=True)
parser.add_argument('--pay-scale',default='1');parser.add_argument('--base-weights');parser.add_argument('--bonus-weights');parser.add_argument('--orb-weights')
a=parser.parse_args();g=json.loads(Path(a.source).read_text(encoding='utf-8'));g['mathVersion']=a.version
for bands in g['paytable'].values():
 for band in bands:band['payTicks']=int((Decimal(band['payTicks'])*Decimal(a.pay_scale)).quantize(Decimal(1),rounding=ROUND_HALF_EVEN))
for key,text in [('baseGenerationProfile',a.base_weights),('bonusGenerationProfile',a.bonus_weights)]:
 if text:
  weights=[int(x) for x in text.split(',')]
  if len(weights)!=len(g[key]['initialWeights']):parser.error('symbol weight count mismatch')
  for field in ['initialWeights','refillWeights']:
   for item,weight in zip(g[key][field],weights):item['weight']=weight
if a.orb_weights:
 weights=[int(x) for x in a.orb_weights.split(',')]
 for profile in ['baseGenerationProfile','bonusGenerationProfile']:
  if len(weights)!=len(g[profile]['orbValues']):parser.error('orb weight count mismatch')
  for item,weight in zip(g[profile]['orbValues'],weights):item['weight']=weight
out=Path(a.out)
if out.exists():parser.error('candidate files are immutable; choose a new output path')
out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(g,indent=2)+'\n',encoding='utf-8')
print('Wrote candidate '+str(out))

import argparse
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('stdout',type=Path);p.add_argument('--mode',choices=['all','reflection','minimal','oracle'],default='all');a=p.parse_args()
reflection=['reflection-shape','reflection-byreflike','reflection-null-target']
public=['public-parse','public-try-false','public-exact','public-exact-false']
if a.mode=='all':
    labels=[f'{route}-{i}' for route in ['direct','virtual','calli','delegate'] for i in range(6 if route=='delegate' else 7)]
    labels += [f'{route}-producer-{i}' for route in ['raw','virtual'] for i in range(6)]
    labels += reflection+public
elif a.mode=='reflection':labels=reflection
elif a.mode=='oracle':labels=public
else:labels=['minimal-no-access']
expected=['dtfi-tokenize-'+label+':True' for label in labels]
if a.mode=='all':expected+=['DTFI Tokenize boundary end']
actual=a.stdout.read_bytes().replace(b'\r\n',b'\n').decode().splitlines()
assert actual==expected,(a.mode,actual,expected)
print('dtfi-tokenize-witnesses:'+str(len(labels)))

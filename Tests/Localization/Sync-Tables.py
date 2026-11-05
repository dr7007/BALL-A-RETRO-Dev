"""Rebuild Unity tables from the reviewed CSV files without changing existing key IDs."""
from pathlib import Path
import csv,json,re
ROOT=Path(__file__).resolve().parents[2]

def sync(check=False):
    for name,csvname in [('UITable','ui.csv'),('DialogueTable','dialogues.csv')]:
        rows=list(csv.DictReader((ROOT/'Assets/Resources'/csvname).open(encoding='utf-8-sig',newline='')))
        ids=[r['Id'] for r in rows];keys=[r['Key'] for r in rows]
        assert len(ids)==len(set(ids)) and len(keys)==len(set(keys)),name+' duplicate IDs/keys'
        base=ROOT/'Assets/Resources/LocalizationData'
        p=base/(name+' Shared Data.asset');raw=p.read_text(encoding='utf-8-sig')
        entries=''.join(f"  - m_Id: {r['Id']}\n    m_Key: {r['Key']}\n    m_Metadata:\n      m_Items: []\n" for r in rows)
        updated=re.sub(r'(?<=  m_Entries:\n).*?(?=^  m_Metadata:|^  m_KeyGenerator:|^  references:)',lambda m:entries,raw,flags=re.S|re.M)
        if check:assert raw==updated,str(p)+' is out of sync'
        else:p.write_bytes(updated.encode('utf-8'))
        for code,column in [('en','English(en)'),('ko','Korean(ko)'),('ja','Japanese(ja)')]:
            p=base/(name+'_'+code+'.asset');raw=p.read_text(encoding='utf-8-sig')
            for r in rows:assert r[column].strip(),r['Key']+' missing '+code
            entries=''.join(f"  - m_Id: {r['Id']}\n    m_Localized: {json.dumps(r[column],ensure_ascii=False)}\n    m_Metadata:\n      m_Items: []\n" for r in rows)
            updated=re.sub(r'(?<=  m_TableData:\n).*?(?=^  references:)',lambda m:entries,raw,flags=re.S|re.M)
            if check:assert raw==updated,str(p)+' is out of sync'
            else:p.write_bytes(updated.encode('utf-8'))
        print(f'{name}: {len(rows)} keys, ko/en/ja complete')
if __name__=='__main__':
    import sys
    sync('--check' in sys.argv)

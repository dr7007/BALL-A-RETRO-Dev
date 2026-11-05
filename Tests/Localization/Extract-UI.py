"""Copy release UI into an isolated project, preserving text, layout and localization wiring.
Unrelated gameplay/audio scripts and their listeners are omitted; source assets are not modified.
"""
from pathlib import Path
import re,sys,shutil
source,out=map(Path,sys.argv[1:]);meta={}
for folder in ['Assets/Public','Assets/MemberFolder/PSH','Assets/MemberFolder/CJS']:
 for p in (source/folder).rglob('*.meta'):
  m=re.search(r'^guid: (\w+)',p.read_text(encoding='utf-8-sig'),re.M)
  if m:meta[m[1]]=p.with_suffix('')
keep_names={'PSH_Script_LocalizedText.cs','PSH_Script_LanguageManager.cs','PSH_Script_ResolutionSettings.cs','PSH_Script_ToggleUIController.cs','PSH_Script_BaseButtonHover.cs','CJS_Script_ChoiceCard.cs'}
keep={g for g,p in meta.items() if p.name in keep_names}
keep.add('56eb0353ae6e5124bb35b17aff880f16')
for p in (source/'Library/PackageCache').glob('com.unity.ugui@*/**/*.cs.meta'):
 m=re.search(r'^guid: (\w+)',p.read_text(encoding='utf-8-sig'),re.M)
 if m:keep.add(m[1])
files=['Assets/Public/Main_Game/UI_Game/Prefabs/Panel_Setting.prefab','Assets/Public/UI_Public/Prefabs/Button_Close.prefab','Assets/Public/Main_Game/UI_Game/Prefabs/Image_Warning.prefab','Assets/Public/Main_Game/UI_Game/Prefabs/Button_Choice.prefab','Assets/Public/Main_Lobby/UI_Lobby/Prefabs/Canvas_Lobby.prefab','Assets/Public/Main_Lobby/UI_Lobby/Prefabs/Panel_InputEmail.prefab','Assets/Public/Main_Game/UI_Game/Prefabs/Panel_GameEnd.prefab','Assets/Public/UI_Public/Prefabs/Panel_Rank.prefab']
for name in files:
 p=source/name;raw=p.read_text(encoding='utf-8-sig');blocks=re.split(r'(?=^--- !u!)',raw,flags=re.M);removed=set()
 for b in blocks:
  if b.startswith('--- !u!95'):removed.add(b.splitlines()[0].split('&')[1]);continue
  if not b.startswith('--- !u!114') or 'stripped' in b.splitlines()[0]:continue
  m=re.search(r'm_Script:.*guid: (\w+)',b)
  if m and m[1] not in keep:removed.add(b.splitlines()[0].split('&')[1])
 parts=[]
 for b in blocks:
  head=b.splitlines()[0] if b else ''
  if head.startswith('--- !u!') and head.split('&')[1] in removed:continue
  if b.startswith('--- !u!1 '):
   b=re.sub(r'  - component: \{fileID: (\d+)\}\n',lambda m:'' if m[1] in removed else m[0],b)
  # Button events reach gameplay actors omitted by the fixture. Value/locale bindings remain intact.
  if '  m_OnClick:' in b:b=b.split('  m_OnClick:')[0]+'  m_OnClick:\n    m_PersistentCalls:\n      m_Calls: []\n'
  parts.append(b)
 dest=out/name;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(''.join(parts).encode('utf8'));shutil.copyfile(str(p)+'.meta',str(dest)+'.meta')
 for g in set(re.findall(r'guid: (\w+)',raw)):
  dep=meta.get(g)
  if dep and dep.suffix.lower() in ['.png','.jpg','.jpeg'] and not (out/'Assets'/dep.name).exists():
   dest=out/dep.relative_to(source);dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(dep,dest);shutil.copyfile(str(dep)+'.meta',str(dest)+'.meta')
print('Copied',len(files),'release UI prefabs with native localization bindings.')

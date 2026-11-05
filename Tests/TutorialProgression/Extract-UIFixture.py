"""Extract tutorial UI verbatim into a disposable Unity project (stdlib only)."""
from pathlib import Path
import re
import sys

source, output = map(Path, sys.argv[1:])
text = source.read_text(encoding="utf-8-sig")
docs = {int(m[2]): (int(m[1]), m[0]) for m in re.finditer(
    r"^--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=^--- !u!|\Z)", text, re.M | re.S)}
roots = [962259515, 2061069303]
ids = {2109000000, 2109000001, 2109000002, 2109000003}

def descend(transform):
    block = docs[transform][1]
    go = int(re.search(r"m_GameObject: \{fileID: (\d+)\}", block)[1])
    ids.add(go)
    ids.update(map(int, re.findall(r"component: \{fileID: (\d+)\}", docs[go][1])))
    children = block.split("  m_Children:")[1].split("  m_Father:")[0]
    for child in re.findall(r"fileID: (\d+)", children):
        descend(int(child))

for root in roots:
    descend(root)
parts = ["%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n"]
for ident in sorted(ids):
    kind, block = docs[ident]
    if ident in roots:
        block = re.sub(r"m_Father: \{fileID: \d+\}", "m_Father: {fileID: 0}", block)
    if kind == 1:
        block = block.replace("m_IsActive: 0", "m_IsActive: 1")
    if "  m_OnClick:" in block:
        block = block.split("  m_OnClick:")[0] + "  m_OnClick:\n    m_PersistentCalls:\n      m_Calls: []\n"
    parts.append(block)
parts.append("--- !u!1660057539 &9223372036854775807\nSceneRoots:\n  m_ObjectHideFlags: 0\n  m_Roots:\n")
parts.extend(f"  - {{fileID: {i}}}\n" for i in roots + [2109000001])
output.write_text("".join(parts), encoding="utf-8")

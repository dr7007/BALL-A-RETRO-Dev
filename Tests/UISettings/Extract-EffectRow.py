"""Copy the real settings row into an isolated prefab, without localization/animation dependencies."""
from pathlib import Path
import re
import sys

source, output = map(Path, sys.argv[1:])
text = source.read_text(encoding="utf-8-sig")
docs = {int(m[2]): (int(m[1]), m[0]) for m in re.finditer(
    r"^--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=^--- !u!|\Z)", text, re.M | re.S)}
root = 8138185010306440079
ids = set()
excluded = set()

def descend(transform):
    block = docs[transform][1]
    go = int(re.search(r"m_GameObject: \{fileID: (\d+)\}", block)[1])
    ids.add(go)
    for component in map(int, re.findall(r"component: \{fileID: (\d+)\}", docs[go][1])):
        kind, component_text = docs[component]
        if kind == 95 or "guid: 56eb0353ae6e5124bb35b17aff880f16" in component_text:
            excluded.add(component)
        else:
            ids.add(component)
    children = block.split("  m_Children:")[1].split("  m_Father:")[0]
    for child in re.findall(r"fileID: (\d+)", children):
        descend(int(child))

descend(root)
parts = ["%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n"]
for ident in sorted(ids):
    kind, block = docs[ident]
    if ident == root:
        block = re.sub(r"m_Father: \{fileID: \d+\}", "m_Father: {fileID: 0}", block)
    if kind == 1:
        for component in excluded:
            block = block.replace(f"  - component: {{fileID: {component}}}\n", "")
    parts.append(block)
output.write_bytes("".join(parts).encode("utf-8"))

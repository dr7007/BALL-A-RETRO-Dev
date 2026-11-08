#!/usr/bin/env python3
"""Prove that the batching manifest changes flags only; never writes Assets.

Checks current raw SHA-256 values, reverses each declared flag/instance override
in memory, then compares the restored prefab with both its recorded original
hash and the Git baseline. CRLF checkout conversion is reported explicitly.
"""

import argparse
from collections import defaultdict
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys


BLOCK = re.compile(
    r"^--- !u!(\d+) &(\d+)[^\n]*\n.*?(?=^--- !u!|\Z)", re.MULTILINE | re.DOTALL
)


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def git_bytes(repo, *args):
    result = subprocess.run(
        ["git", *args], cwd=repo, stdout=subprocess.PIPE, stderr=subprocess.PIPE
    )
    require(result.returncode == 0, result.stderr.decode("utf-8", errors="replace"))
    return result.stdout


def asset_path(repo, relative):
    path = (repo / relative).resolve()
    require(path.is_relative_to(repo / "Assets"), f"Not an Assets path: {relative}")
    require(path.suffix == ".prefab", f"Not a prefab: {relative}")
    return path


def reverse_flags(current, entries, prefab_guid):
    direct = {}
    nested = defaultdict(list)
    for entry in entries:
        require(entry["prefabGuid"] == prefab_guid, "Manifest prefab GUID mismatch")
        before, after = entry["staticFlagsBefore"], entry["staticFlagsAfter"]
        require(before & 4 == 0 and after == before | 4, "Change is not BatchingStatic only")
        if "prefabInstanceFileId" in entry:
            nested[int(entry["prefabInstanceFileId"])].append(entry)
        else:
            key = int(entry["gameObjectFileId"])
            require(key not in direct, f"Duplicate GameObject: {key}")
            direct[key] = entry

    reversed_direct, reversed_nested = set(), set()
    nested_count = 0

    def restore(match):
        nonlocal nested_count
        kind, file_id = int(match.group(1)), int(match.group(2))
        text = match.group(0)
        if file_id in direct:
            require(kind == 1, f"Expected GameObject block: {file_id}")
            require(file_id not in reversed_direct, f"Duplicate block: {file_id}")
            entry = direct[file_id]
            pattern = rf"^  m_StaticEditorFlags: {entry['staticFlagsAfter']}(?=\r?$)"
            text, count = re.subn(
                pattern,
                f"  m_StaticEditorFlags: {entry['staticFlagsBefore']}",
                text,
                flags=re.MULTILINE,
            )
            require(count == 1, f"Expected exactly one changed flag: {file_id}")
            reversed_direct.add(file_id)
        if file_id in nested:
            require(kind == 1001, f"Expected PrefabInstance block: {file_id}")
            require(file_id not in reversed_nested, f"Duplicate instance: {file_id}")
            newline = "\r\n" if "\r\n" in text else "\n"
            seen_targets = set()
            for entry in nested[file_id]:
                target = entry["gameObjectFileId"]
                source_guid = entry["sourcePrefabGuid"]
                target_key = (source_guid, target)
                require(target_key not in seen_targets, "Duplicate nested target")
                seen_targets.add(target_key)
                source = re.search(
                    r"^  m_SourcePrefab: \{fileID: \d+, guid: ([a-f0-9]+), type: 3\}",
                    text,
                    re.MULTILINE,
                )
                require(source and source.group(1) == source_guid, "Nested source GUID mismatch")
                override = newline.join(
                    [
                        "    - target: {fileID: " + target + ", guid: " + source_guid + ", type: 3}",
                        "      propertyPath: m_StaticEditorFlags",
                        f"      value: {entry['staticFlagsAfter']}",
                        "      objectReference: {fileID: 0}",
                        "",
                    ]
                )
                require(text.count(override) == 1, f"Expected one exact override: {target_key}")
                text = text.replace(override, "", 1)
                nested_count += 1
            reversed_nested.add(file_id)
        return text

    restored = BLOCK.sub(restore, current.decode("utf-8")).encode("utf-8")
    require(reversed_direct == set(direct), "Not every direct flag was reversed")
    require(reversed_nested == set(nested), "Not every nested instance was reversed")
    require(len(reversed_direct) + nested_count == len(entries), "Reversed count mismatch")
    return restored, len(reversed_direct), nested_count


def verify(repo, manifest_path, report):
    raw_manifest = manifest_path.read_bytes()
    manifest = json.loads(raw_manifest)
    require(manifest["schemaVersion"] == 2, "Expected manifest schema version 2")
    entries = manifest["entries"]
    require(len(entries) == manifest["count"], "Manifest entry count mismatch")
    require(len({entry["path"] for entry in entries}) == len(entries), "Duplicate runtime path")
    baseline = manifest["baselineCommit"]
    resolved_baseline = git_bytes(repo, "rev-parse", "--verify", baseline + "^{commit}").decode().strip()
    report.update(
        manifestPath=str(manifest_path),
        manifestSha256=sha256(raw_manifest),
        baselineCommit=baseline,
        resolvedBaselineCommit=resolved_baseline,
        count=len(entries),
    )
    by_prefab = defaultdict(list)
    for entry in entries:
        by_prefab[entry["prefab"]].append(entry)
    prefabs = manifest["prefabs"]
    require(len({p["path"] for p in prefabs}) == len(prefabs), "Duplicate prefab record")
    require(set(by_prefab) == {p["path"] for p in prefabs}, "Prefab records and entries differ")

    for prefab in prefabs:
        relative = prefab["path"]
        current = asset_path(repo, relative).read_bytes()
        require(sha256(current) == prefab["afterSha256"], f"Current SHA mismatch: {relative}")
        restored, direct_count, nested_count = reverse_flags(current, by_prefab[relative], prefab["guid"])
        require(sha256(restored) == prefab["beforeSha256"], f"Restored original SHA mismatch: {relative}")
        baseline_bytes = git_bytes(repo, "show", resolved_baseline + ":" + relative)
        baseline_equivalent = restored
        newline_normalization = "none"
        if restored != baseline_bytes:
            baseline_equivalent = restored.replace(b"\r\n", b"\n")
            newline_normalization = "CRLF to LF only"
        require(baseline_equivalent == baseline_bytes, f"Content differs from Git baseline: {relative}")
        report["prefabs"].append(
            {
                "path": relative,
                "beforeSha256": prefab["beforeSha256"],
                "afterSha256": sha256(current),
                "restoredSha256": sha256(restored),
                "baselineSha256": sha256(baseline_bytes),
                "restoredGitSha256": sha256(baseline_equivalent),
                "newlineNormalization": newline_normalization,
                "directFlagCount": direct_count,
                "nestedOverrideCount": nested_count,
                "flagsOnly": True,
                "baselineMatches": True,
            }
        )
    report["success"] = True


def main():
    repo = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, default=Path(__file__).with_name("StaticBoardBatchingManifest.json"))
    parser.add_argument("--output", required=True, type=Path, help="Evidence JSON path outside Assets")
    args = parser.parse_args()
    output = args.output.resolve()
    require(not output.is_relative_to(repo / "Assets"), "Evidence must not be written inside Assets")
    report = {
        "schemaVersion": 1,
        "success": False,
        "checkedAtUtc": datetime.now(timezone.utc).isoformat(),
        "repository": str(repo),
        "verifierSha256": sha256(Path(__file__).read_bytes()),
        "proof": "Only declared BatchingStatic flags/instance overrides differ; restored prefab contents exactly match the Git baseline, allowing recorded CRLF checkout conversion only.",
        "prefabs": [],
        "errors": [],
    }
    try:
        verify(repo, args.manifest.resolve(), report)
    except (OSError, ValueError, KeyError, TypeError) as error:
        report["errors"].append(str(error))
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"success": report["success"], "count": report.get("count"), "output": str(output), "errors": report["errors"]}))
    return 0 if report["success"] else 1


if __name__ == "__main__":
    sys.exit(main())

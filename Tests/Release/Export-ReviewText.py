"""Export the shipped localization sources as a readable review attachment."""
import argparse
import csv
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument("--output", type=Path, required=True)
args = parser.parse_args()
if args.output.exists():
    raise SystemExit("Use a new output file; existing attachments are not overwritten.")
root = Path(__file__).resolve().parents[2]
sections = ["BALL-A-RETRO 심사용 대사·UI 원문", "출처: Assets/Resources/*.csv", ""]
count = 0
for name in ("dialogues.csv", "ui.csv"):
    sections.append(f"===== {name} =====")
    with (root / "Assets" / "Resources" / name).open(encoding="utf-8-sig", newline="") as source:
        reader = csv.DictReader(source)
        language_columns = [col for col in reader.fieldnames if col not in ("Key", "Id")]
        for row in reader:
            sections.append(f"[{row['Key']}]")
            for column in language_columns:
                if not row[column].strip():
                    raise SystemExit(f"Missing translation: {name} {row['Key']} {column}")
                sections.append(f"{column}: {row[column]}")
            sections.append("")
            count += 1
args.output.parent.mkdir(parents=True, exist_ok=True)
args.output.write_text("\n".join(sections), encoding="utf-8-sig")
print(f"Exported {count} entries to {args.output}")

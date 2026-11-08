"""Inspect and trim real review recordings without changing game speed or content.

Dependencies are installed outside the repository; pass --tools-dir explicitly.
Technical checks never certify that a clip covers a particular round or ending.
"""
import argparse
import hashlib
import json
import re
import shutil
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path


def digest(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def write_json(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding="utf-8")


def inspect_video(source, evidence, minimum, av, ffmpeg):
    from PIL import Image, ImageDraw, ImageOps

    if evidence.exists():
        raise ValueError("Use a new evidence directory; existing evidence is preserved.")
    evidence.mkdir(parents=True)
    with av.open(str(source)) as container:
        videos = list(container.streams.video)
        if not videos:
            raise ValueError("No video stream found.")
        stream = videos[0]
        duration = (float(stream.duration * stream.time_base) if stream.duration is not None
                    else float(container.duration or 0) / av.time_base)
        if duration <= 0:
            raise ValueError("No usable video duration.")
        report = {
            "source": str(source), "sourceSha256": digest(source),
            "bytes": source.stat().st_size, "observedAt": datetime.now(timezone.utc).isoformat(),
            "durationSeconds": duration, "minimumSeconds": minimum,
            "width": stream.width, "height": stream.height,
            "fps": float(stream.average_rate) if stream.average_rate else None,
            "videoCodec": stream.codec_context.name,
            "audioStreams": [{"codec": audio.codec_context.name,
                              "sampleRate": audio.codec_context.sample_rate}
                             for audio in container.streams.audio],
            "contentReviewed": False, "submissionReady": False,
            "contentNote": "Human review must confirm gameplay stage, complete ending, readable dialogue and audible game sound.",
        }

    decode = subprocess.run([ffmpeg, "-hide_banner", "-nostdin", "-v", "error", "-xerror",
                             "-i", str(source), "-map", "0:v:0", "-map", "0:a?",
                             "-f", "null", "-"], capture_output=True, text=True, errors="replace")
    (evidence / "decode.log").write_text(decode.stderr, encoding="utf-8")
    report["fullDecodeSucceeded"] = decode.returncode == 0

    if report["audioStreams"]:
        volume = subprocess.run([ffmpeg, "-hide_banner", "-nostdin", "-i", str(source),
                                 "-map", "0:a:0", "-vn", "-af", "volumedetect",
                                 "-f", "null", "-"], capture_output=True, text=True, errors="replace")
        (evidence / "audio-analysis.log").write_text(volume.stderr, encoding="utf-8")
        matches = dict(re.findall(r"(mean_volume|max_volume):\s+(-?inf|-?[\d.]+) dB", volume.stderr))
        report["audioLevelDb"] = matches
        report["audioAnalysisSucceeded"] = volume.returncode == 0 and "max_volume" in matches
    else:
        report["audioLevelDb"] = {}
        report["audioAnalysisSucceeded"] = False

    width, height, label = 480, 270, 28
    sheet = Image.new("RGB", (width * 3, (height + label) * 4), "#151515")
    draw = ImageDraw.Draw(sheet)
    samples = []
    # Include opening and closing moments; sample the interior without duplicating frames.
    for index in range(12):
        wanted = min(max(0.0, duration - 0.15), duration * index / 11)
        with av.open(str(source)) as container:
            stream = container.streams.video[0]
            origin = stream.start_time or 0
            container.seek(int(wanted / float(stream.time_base)) + origin, stream=stream, backward=True)
            selected = None
            actual = None
            for frame in container.decode(stream):
                if frame.pts is None:
                    continue
                actual = float((frame.pts - origin) * stream.time_base)
                selected = frame
                if actual >= wanted:
                    break
            if selected is None:
                raise ValueError(f"Cannot decode preview at {wanted:.2f} seconds")
            picture = ImageOps.contain(selected.to_image().convert("RGB"), (width, height))
            x, y = (index % 3) * width, (index // 3) * (height + label)
            sheet.paste(picture, (x + (width - picture.width) // 2, y))
            draw.text((x + 8, y + height + 6), f"{actual:08.2f}s", fill="white")
            samples.append({"requestedSeconds": wanted, "decodedSeconds": actual})
    sheet.save(evidence / "contact-sheet.jpg", quality=92)
    report["previewSamples"] = samples
    report["checks"] = {
        "minimumDuration": duration + 0.02 >= minimum,
        "atLeast720p": min(report["width"], report["height"]) >= 720,
        "audioPresent": bool(report["audioStreams"]),
        "audiblePeak": report["audioAnalysisSucceeded"] and
                       float(report["audioLevelDb"].get("max_volume", "-inf")) > -60,
        "fullDecode": report["fullDecodeSucceeded"],
    }
    report["technicalChecksPassed"] = all(report["checks"].values())
    write_json(evidence / "inspection.json", report)
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--tools-dir", required=True, type=Path)
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--evidence", required=True, type=Path)
    parser.add_argument("--minimum-seconds", type=float, default=0)
    parser.add_argument("--output", type=Path, help="Optional new MP4 for an actual source interval.")
    parser.add_argument("--start-seconds", type=float, default=0)
    parser.add_argument("--duration-seconds", type=float)
    args = parser.parse_args()
    sys.path.insert(0, str(args.tools_dir.resolve(strict=True)))
    import av
    import imageio_ffmpeg

    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    source = args.input.resolve(strict=True)
    target = source
    if args.minimum_seconds < 0 or args.start_seconds < 0:
        parser.error("Durations and positions must be nonnegative.")
    if args.evidence.exists():
        parser.error("Use a new evidence directory.")
    if not args.output and (args.start_seconds or args.duration_seconds is not None):
        parser.error("Trimming requires --output.")
    if args.output:
        target = args.output.resolve()
        if target.exists() or target == source or target.suffix.lower() != ".mp4":
            parser.error("Output must be a new .mp4 file, separate from the source.")
        if args.duration_seconds is not None and args.duration_seconds <= 0:
            parser.error("Clip duration must be positive.")
        target.parent.mkdir(parents=True, exist_ok=True)
        if args.start_seconds == 0 and args.duration_seconds is None and source.suffix.lower() == ".mp4":
            shutil.copy2(source, target)
        else:
            command = [ffmpeg, "-hide_banner", "-nostdin", "-n", "-ss", str(args.start_seconds), "-i", str(source)]
            if args.duration_seconds is not None:
                command += ["-t", str(args.duration_seconds)]
            command += ["-map", "0:v:0", "-map", "0:a:0?", "-c:v", "libx264", "-preset", "medium",
                        "-crf", "19", "-pix_fmt", "yuv420p", "-c:a", "aac", "-b:a", "192k",
                        "-movflags", "+faststart", str(target)]
            subprocess.run(command, check=True)
    report = inspect_video(target, args.evidence.resolve(), args.minimum_seconds, av, ffmpeg)
    if args.output:
        write_json(args.evidence / "source-interval.json", {
            "original": str(source), "originalSha256": digest(source), "output": str(target),
            "startSeconds": args.start_seconds, "durationSecondsRequested": args.duration_seconds,
            "speed": 1.0, "loopedOrPadded": False,
        })
    print(json.dumps({"technicalChecksPassed": report["technicalChecksPassed"],
                      "durationSeconds": report["durationSeconds"], "contentReviewed": False,
                      "evidence": str(args.evidence)}, ensure_ascii=False))
    return 0 if report["technicalChecksPassed"] else 2


if __name__ == "__main__":
    raise SystemExit(main())

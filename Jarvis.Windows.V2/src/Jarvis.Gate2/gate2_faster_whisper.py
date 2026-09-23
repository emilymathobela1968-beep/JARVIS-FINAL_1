import json, sys
from faster_whisper import WhisperModel

if len(sys.argv) != 2:
    raise SystemExit("usage: gate2_faster_whisper.py <wave-path>")
model = WhisperModel("small.en", device="cpu", compute_type="int8")
segments, _ = model.transcribe(sys.argv[1], language="en", temperature=0.0)
print(json.dumps({"transcript": "".join(segment.text for segment in segments).strip()}))

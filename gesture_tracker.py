"""
Hand landmark tracker for KeepSessionAlive gesture control.
Requires: pip install mediapipe opencv-python

Writes one JSON line per frame to stdout:
  {"lm": [[x,y], ...21 pairs...]}   - hand detected
  {"lm": null}                       - no hand
"""
import os
import sys
import json
import time
import urllib.request
import cv2
import mediapipe as mp
from mediapipe.tasks.python import BaseOptions
from mediapipe.tasks.python import vision

MODEL_URL = ("https://storage.googleapis.com/mediapipe-models/hand_landmarker/"
             "hand_landmarker/float16/latest/hand_landmarker.task")
MODEL_PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)), "hand_landmarker.task")


def log(msg):
    print(msg, file=sys.stderr, flush=True)


def ensure_model():
    if os.path.exists(MODEL_PATH):
        return
    log("INFO: downloading hand model (one-time)...")
    urllib.request.urlretrieve(MODEL_URL, MODEL_PATH)
    log("INFO: model downloaded")


def main():
    ensure_model()

    cap = cv2.VideoCapture(0)
    if not cap.isOpened():
        log("ERROR: could not open camera index 0")
        sys.exit(1)

    cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)
    cap.set(cv2.CAP_PROP_FPS, 30)
    log("INFO: camera opened")

    options = vision.HandLandmarkerOptions(
        base_options=BaseOptions(model_asset_path=MODEL_PATH),
        running_mode=vision.RunningMode.VIDEO,
        num_hands=1,
        min_hand_detection_confidence=0.7,
        min_tracking_confidence=0.5,
    )

    start = time.monotonic()
    with vision.HandLandmarker.create_from_options(options) as landmarker:
        log("INFO: tracking started")
        while True:
            ret, frame = cap.read()
            if not ret:
                log("ERROR: camera stopped returning frames")
                break

            image = mp.Image(image_format=mp.ImageFormat.SRGB,
                             data=cv2.cvtColor(frame, cv2.COLOR_BGR2RGB))
            timestamp_ms = int((time.monotonic() - start) * 1000)
            result = landmarker.detect_for_video(image, timestamp_ms)

            if result.hand_landmarks:
                lm = result.hand_landmarks[0]
                data = {"lm": [[round(p.x, 4), round(p.y, 4)] for p in lm]}
            else:
                data = {"lm": None}

            sys.stdout.write(json.dumps(data) + "\n")
            sys.stdout.flush()

    cap.release()


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        pass

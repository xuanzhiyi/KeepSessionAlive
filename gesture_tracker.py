"""
Hand gesture tracker for KeepSessionAlive gesture control.
Requires: pip install mediapipe opencv-python

Writes one JSON line per frame to stdout:
  {"g": "Closed_Fist", "s": 0.93, "lm": [[x,y], ...21 pairs...]}   - hand detected
  {"lm": null}                                                     - no hand

"g" is MediaPipe's trained gesture label (None, Closed_Fist, Open_Palm, Pointing_Up,
Thumb_Down, Thumb_Up, Victory, ILoveYou) and "s" its confidence.
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

MODEL_URL = ("https://storage.googleapis.com/mediapipe-models/gesture_recognizer/"
             "gesture_recognizer/float16/latest/gesture_recognizer.task")
MODEL_PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)), "gesture_recognizer.task")

# Detect a few hands so a bystander's hand can't take over tracking; we act on the largest
# (closest to the camera). Hands smaller than this are ignored entirely.
MAX_HANDS = 3
# Wrist -> middle knuckle length as a fraction of the frame; raise if far hands still interfere
MIN_HAND_SIZE = 0.08


def log(msg):
    print(msg, file=sys.stderr, flush=True)


def ensure_model():
    if os.path.exists(MODEL_PATH):
        return
    log("INFO: downloading gesture model (one-time)...")
    urllib.request.urlretrieve(MODEL_URL, MODEL_PATH)
    log("INFO: model downloaded")


def hand_size(landmarks):
    wrist, knuckle = landmarks[0], landmarks[9]
    return ((wrist.x - knuckle.x) ** 2 + (wrist.y - knuckle.y) ** 2) ** 0.5


def pick_main_hand(result):
    """Index of the largest hand, or None if no hand is big enough to be the user's."""
    best, best_size = None, MIN_HAND_SIZE
    for i, lm in enumerate(result.hand_landmarks):
        size = hand_size(lm)
        if size >= best_size:
            best, best_size = i, size
    return best


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

    options = vision.GestureRecognizerOptions(
        base_options=BaseOptions(model_asset_path=MODEL_PATH),
        running_mode=vision.RunningMode.VIDEO,
        num_hands=MAX_HANDS,
        min_hand_detection_confidence=0.7,
        min_tracking_confidence=0.5,
    )

    start = time.monotonic()
    with vision.GestureRecognizer.create_from_options(options) as recognizer:
        log("INFO: tracking started")
        while True:
            ret, frame = cap.read()
            if not ret:
                log("ERROR: camera stopped returning frames")
                break

            image = mp.Image(image_format=mp.ImageFormat.SRGB,
                             data=cv2.cvtColor(frame, cv2.COLOR_BGR2RGB))
            timestamp_ms = int((time.monotonic() - start) * 1000)
            result = recognizer.recognize_for_video(image, timestamp_ms)

            main_hand = pick_main_hand(result)
            if main_hand is not None:
                lm = result.hand_landmarks[main_hand]
                gestures = result.gestures[main_hand] if main_hand < len(result.gestures) else None
                top = gestures[0] if gestures else None
                data = {
                    "g": top.category_name if top else "None",
                    "s": round(top.score, 3) if top else 0.0,
                    "lm": [[round(p.x, 4), round(p.y, 4)] for p in lm],
                }
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

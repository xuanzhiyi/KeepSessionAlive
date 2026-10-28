"""
Hand landmark tracker for KeepSessionAlive gesture control.
Requires: pip install mediapipe opencv-python

Writes one JSON line per frame to stdout:
  {"lm": [[x,y], ...21 pairs...]}   - hand detected
  {"lm": null}                       - no hand
"""
import sys
import json
import cv2
import mediapipe as mp

def main():
    cap = cv2.VideoCapture(0)
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)
    cap.set(cv2.CAP_PROP_FPS, 30)

    mp_hands = mp.solutions.hands
    with mp_hands.Hands(
        static_image_mode=False,
        max_num_hands=1,
        min_detection_confidence=0.7,
        min_tracking_confidence=0.5,
    ) as hands:
        while True:
            ret, frame = cap.read()
            if not ret:
                break

            results = hands.process(cv2.cvtColor(frame, cv2.COLOR_BGR2RGB))

            if results.multi_hand_landmarks:
                lm = results.multi_hand_landmarks[0].landmark
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

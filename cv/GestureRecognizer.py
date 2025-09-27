import time
import cv2
import mediapipe as mp
import numpy as np
from collections import deque
from dataclasses import dataclass
from typing import List, Tuple

@dataclass
class GestureState:
    current_gesture: str = "neutral"
    gesture_start_time: float = 0
    is_transitioning: bool = False
    confidence: float = 0.0

@dataclass
class CalibrationData:
    rest_fist_openness: float = 0.5
    velocity_threshold: float = 0.015
    initialized: bool = False

class GestureRecognizer:
    def __init__(self):
        self.mp_hands = mp.solutions.hands
        self.mp_drawing = mp.solutions.drawing_utils
        self.hands = self.mp_hands.Hands(
            static_image_mode=False,
            max_num_hands=1,
            min_detection_confidence=0.7,
            min_tracking_confidence=0.5
        )
        
        self.state = GestureState()
        self.calibration = CalibrationData()

        self.position_history = deque(maxlen=10)
        self.velocity_history = deque(maxlen=8)
        self.gesture_history = deque(maxlen=8)
        
        self.GESTURE_HOLD_TIME = 0.4
        self.NEUTRAL_RETURN_TIME = 1.0

        self.colors = {
            'neutral': (128, 128, 128),
            'transitioning': (0, 165, 255),
            'open': (0, 255, 0),
            'closed': (0, 0, 255)
        }

    def calculate_fist(self, landmarks) -> bool:
        finger_tips = [8, 12, 16, 20]
        finger_bases = [5, 9, 13, 17]
        closed_count = 0
        for tip, base in zip(finger_tips, finger_bases):
            tip_coord = np.array([landmarks[tip].x, landmarks[tip].y])
            base_coord = np.array([landmarks[base].x, landmarks[base].y])
            dist = np.linalg.norm(tip_coord - base_coord)
            if dist < 0.07:  # slightly relaxed threshold for better fist detection
                closed_count += 1
        return closed_count >= 3

    def calculate_hand_openness(self, landmarks) -> float:
        palm_center = np.array([landmarks[9].x, landmarks[9].y])
        fingertips = [8, 12, 16, 20]
        distances = [np.linalg.norm(np.array([landmarks[i].x, landmarks[i].y]) - palm_center) for i in fingertips]
        avg_distance = np.mean(distances)
        openness = np.clip((avg_distance - 0.08) / 0.17, 0, 1)
        return openness

    def calculate_hand_velocity(self, current_position: np.ndarray) -> float:
        if len(self.position_history) < 2:
            return 0.0
        prev_position = self.position_history[-1]
        velocity = np.linalg.norm(current_position - prev_position)
        return velocity

    def classify_raw_gesture(self, landmarks, openness: float) -> str:
        if self.calculate_fist(landmarks):
            return "closed"
        if self.calibration.initialized:
            rest_pos = self.calibration.rest_fist_openness
            if openness > rest_pos + 0.2:
                return "open"
            elif abs(openness - rest_pos) < 0.15:
                return "neutral"
        else:
            if openness > 0.65:
                return "open"
            elif openness < 0.25:
                return "closed"
        return "neutral"

    def update_state_machine(self, raw_gesture: str) -> str:
        current_time = time.time()
        if raw_gesture != self.state.current_gesture:
            if not self.state.is_transitioning:
                self.state.is_transitioning = True
                self.state.gesture_start_time = current_time
                self.state.candidate_state = raw_gesture
            else:
                if raw_gesture == getattr(self.state, 'candidate_state', None) and current_time - self.state.gesture_start_time >= self.GESTURE_HOLD_TIME:
                    self.state.current_gesture = raw_gesture
                    self.state.is_transitioning = False
        else:
            self.state.is_transitioning = False
        return self.state.current_gesture

    def process_hand(self, landmarks, image_shape: Tuple[int, int]) -> str:
        openness = self.calculate_hand_openness(landmarks)
        hand_center = np.mean([[lm.x, lm.y] for lm in landmarks], axis=0)
        velocity = self.calculate_hand_velocity(hand_center)
        self.position_history.append(hand_center)
        self.velocity_history.append(velocity)

        raw_gesture = self.classify_raw_gesture(landmarks, openness)
        self.gesture_history.append(raw_gesture)

        gesture = self.update_state_machine(raw_gesture)

        if gesture != self.state.current_gesture:
            if gesture == "closed":
                print("YES - Fist Closed")
            elif gesture == "open":
                print("NO - Hand Open")
            elif gesture == "neutral":
                print("Neutral")

        return gesture

    def draw_feedback(self, image: np.ndarray, gesture: str, landmarks=None, openness=None, velocity=None):
        h, w = image.shape[:2]
        color = self.colors.get(gesture, (128, 128, 128))
        if self.state.is_transitioning:
            color = self.colors['transitioning']
            progress = (time.time() - self.state.gesture_start_time) / self.GESTURE_HOLD_TIME
            progress = min(progress, 1.0)
            bar_width = int(200 * progress)
            cv2.rectangle(image, (w//2 - 100, 20), (w//2 - 100 + bar_width, 40), color, -1)
            cv2.rectangle(image, (w//2 - 100, 20), (w//2 + 100, 40), color, 2)
            cv2.putText(image, "Confirming...", (w//2 - 60, 35), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 1)

        status_text = f"Gesture: {gesture.upper()}"
        cv2.putText(image, status_text, (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 1, color, 2)

        if landmarks:
            self.mp_drawing.draw_landmarks(image, landmarks, self.mp_hands.HAND_CONNECTIONS)
        
        return image

    def run(self):
        cap = cv2.VideoCapture(0, cv2.CAP_DSHOW)
        cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
        cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)

        print("Starting gesture recognition...")
        print("Press 'q' to quit, 'c' to calibrate")

        calibration_active = False
        calibration_samples = []
        calibration_start_time = 0

        while cap.isOpened():
            ret, frame = cap.read()
            if not ret:
                break

            frame = cv2.flip(frame, 1)
            rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            results = self.hands.process(rgb_frame)

            if results.multi_hand_landmarks:
                hand_landmarks = results.multi_hand_landmarks[0]
                openness = self.calculate_hand_openness(hand_landmarks.landmark)

                if calibration_active:
                    calibration_samples.append(openness)
                    elapsed = time.time() - calibration_start_time
                    if len(calibration_samples) >= 60:
                        self.calibration.rest_fist_openness = np.median(calibration_samples)
                        self.calibration.initialized = True
                        calibration_active = False
                        calibration_samples = []
                        print(f"Calibration complete. Rest openness = {self.calibration.rest_fist_openness:.2f}")
                else:
                    gesture = self.process_hand(hand_landmarks.landmark, frame.shape)
                    frame = self.draw_feedback(frame, gesture, hand_landmarks, openness)
            else:
                frame = self.draw_feedback(frame, "neutral", None)

            cv2.imshow('Gesture Recognition', frame)

            key = cv2.waitKey(1) & 0xFF
            if key == ord('q'):
                break
            elif key == ord('c') and not calibration_active:
                calibration_active = True
                calibration_samples = []
                calibration_start_time = time.time()
                print("Calibrating... hold hand steady")

        cap.release()
        cv2.destroyAllWindows()
        self.hands.close()

if __name__ == "__main__":
    recognizer = GestureRecognizer()
    recognizer.run()

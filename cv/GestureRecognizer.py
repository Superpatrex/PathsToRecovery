import time
import cv2
import mediapipe as mp
import numpy as np
from collections import deque
from dataclasses import dataclass
from typing import List, Tuple
import math

@dataclass
class GestureState:
    current_gesture: str = "neutral"
    gesture_start_time: float = 0
    is_transitioning: bool = False
    confidence: float = 0.0
    hand_x: float = -1.0
    hand_y: float = -1.0

@dataclass
class CalibrationData:
    neutral_curl_score: float = 0.0
    open_curl_score: float = 0.0
    closed_curl_score: float = 0.0
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
        self.curl_history = deque(maxlen=10)
        
        self.GESTURE_HOLD_TIME = 0.3
        
        self.colors = {
            'neutral': (128, 128, 128),
            'transitioning': (0, 165, 255),
            'open': (0, 255, 0),
            'closed': (0, 0, 255)
        }

        # MediaPipe hand landmark indices
        self.finger_joints = {
            'index': [5, 6, 7, 8],
            'middle': [9, 10, 11, 12],
            'ring': [13, 14, 15, 16],
            'pinky': [17, 18, 19, 20]
        }

    def calculate_finger_curl_distance(self, landmarks, finger_name: str) -> float:
        """Calculate finger curl based on distance from palm base to fingertip"""
        joints = self.finger_joints[finger_name]
        
        # Get palm base (landmark 0 is wrist, use it as reference)
        palm_base = np.array([landmarks[0].x, landmarks[0].y])
        
        # Get fingertip
        tip = np.array([landmarks[joints[3]].x, landmarks[joints[3]].y])
        
        # Calculate distance from palm base to fingertip
        distance = np.linalg.norm(tip - palm_base)
        
        return distance

    def calculate_thumb_curl_distance(self, landmarks) -> float:
        """Calculate thumb distance from palm base"""
        # Palm base
        palm_base = np.array([landmarks[0].x, landmarks[0].y])
        
        # Thumb tip
        thumb_tip = np.array([landmarks[4].x, landmarks[4].y])
        
        # Calculate distance
        distance = np.linalg.norm(thumb_tip - palm_base)
        
        return distance

    def calculate_overall_curl_score(self, landmarks) -> float:
        """Calculate overall hand curl score based on distances from palm base"""
        distances = []
        
        # Calculate distance for each finger from palm base to tip
        for finger_name in self.finger_joints.keys():
            distance = self.calculate_finger_curl_distance(landmarks, finger_name)
            distances.append(distance)
        
        # Add thumb distance
        thumb_distance = self.calculate_thumb_curl_distance(landmarks)
        distances.append(thumb_distance)
        
        # Calculate average distance
        avg_distance = np.mean(distances)
        
        # Convert distance to curl score (smaller distance = more curl)
        # Adjusted range to better capture open hand positions
        min_distance = 0.06  # Very curled fist
        max_distance = 0.55  # Fully extended/spread hand
        
        # Clamp the distance to expected range
        clamped_distance = np.clip(avg_distance, min_distance, max_distance)
        
        # Convert to curl score (invert so smaller distance = higher curl)
        curl_score = 1.0 - ((clamped_distance - min_distance) / (max_distance - min_distance))
        
        return np.clip(curl_score, 0, 1)

    def calculate_relative_gesture_score(self, current_curl: float) -> dict:
        """Calculate relative scores for each gesture based on calibration"""
        if not self.calibration.initialized:
            return {'open': 0, 'neutral': 1, 'closed': 0}
        
        # Calculate distances to each calibrated gesture
        dist_to_open = abs(current_curl - self.calibration.open_curl_score)
        dist_to_neutral = abs(current_curl - self.calibration.neutral_curl_score)
        dist_to_closed = abs(current_curl - self.calibration.closed_curl_score)
        
        # Convert distances to scores (closer = higher score)
        max_dist = max(dist_to_open, dist_to_neutral, dist_to_closed) + 0.001  # Avoid division by zero
        
        scores = {
            'open': 1 - (dist_to_open / max_dist),
            'neutral': 1 - (dist_to_neutral / max_dist),
            'closed': 1 - (dist_to_closed / max_dist)
        }
        
        return scores

    def classify_gesture_from_scores(self, scores: dict, current_curl: float) -> str:
        """Classify gesture based on relative scores with confidence thresholds"""
        # Find the gesture with highest score
        best_gesture = max(scores, key=scores.get)
        best_score = scores[best_gesture]
        
        # Calculate confidence (how much better is the best vs second best)
        sorted_scores = sorted(scores.values(), reverse=True)
        if len(sorted_scores) > 1:
            confidence = sorted_scores[0] - sorted_scores[1]
        else:
            confidence = best_score
        
        # Require minimum confidence for non-neutral gestures
        if best_gesture != 'neutral' and confidence < 0.2:
            return 'neutral'  # Low confidence, default to neutral
        
        return best_gesture

    def calculate_hand_velocity(self, current_position: np.ndarray) -> float:
        if len(self.position_history) < 2:
            return 0.0
        prev_position = self.position_history[-1]
        velocity = np.linalg.norm(current_position - prev_position)
        return velocity

    def classify_raw_gesture(self, landmarks) -> str:
        """Classify gesture using relative scoring"""
        curl_score = self.calculate_overall_curl_score(landmarks)
        self.curl_history.append(curl_score)
        
        # Get relative scores for each gesture
        scores = self.calculate_relative_gesture_score(curl_score)
        
        # Classify based on scores
        gesture = self.classify_gesture_from_scores(scores, curl_score)
        
        return gesture

    def update_state_machine(self, raw_gesture: str) -> str:
        """State machine for gesture confirmation"""
        current_time = time.time()
        
        if raw_gesture != self.state.current_gesture:
            if not self.state.is_transitioning:
                # Start transition
                self.state.is_transitioning = True
                self.state.gesture_start_time = current_time
                self.state.candidate_state = raw_gesture
                return self.state.current_gesture
            else:
                # Check if we're transitioning to the same candidate
                if raw_gesture == getattr(self.state, 'candidate_state', None):
                    # Check if hold time is met
                    if current_time - self.state.gesture_start_time >= self.GESTURE_HOLD_TIME:
                        # Confirm gesture change
                        self.state.current_gesture = raw_gesture
                        self.state.is_transitioning = False
                        
                        return raw_gesture
                else:
                    # Different candidate, restart transition
                    self.state.candidate_state = raw_gesture
                    self.state.gesture_start_time = current_time
                    return self.state.current_gesture
        else:
            # Same as current gesture, stop transitioning
            self.state.is_transitioning = False
        
        return self.state.current_gesture

    def process_hand(self, landmarks, image_shape: Tuple[int, int]) -> str:
        """Process hand landmarks and return detected gesture"""
        # Calculate hand center for velocity tracking
        hand_center = np.mean([[lm.x, lm.y] for lm in landmarks], axis=0)
        velocity = self.calculate_hand_velocity(hand_center)
        
        self.position_history.append(hand_center)
        self.velocity_history.append(velocity)

        # Store hand position in state (normalized coordinates 0-1)
        self.state.hand_x = float(hand_center[0])
        self.state.hand_y = float(hand_center[1])

        # Get raw gesture classification
        raw_gesture = self.classify_raw_gesture(landmarks)
        self.gesture_history.append(raw_gesture)

        # Apply state machine for confirmation
        confirmed_gesture = self.update_state_machine(raw_gesture)

        return confirmed_gesture

    def draw_feedback(self, image: np.ndarray, gesture: str, landmarks=None):
        """Draw visual feedback on the image"""
        h, w = image.shape[:2]
        color = self.colors.get(gesture, (128, 128, 128))
        
        if self.state.is_transitioning:
            color = self.colors['transitioning']
            progress = (time.time() - self.state.gesture_start_time) / self.GESTURE_HOLD_TIME
            progress = min(progress, 1.0)
            bar_width = int(200 * progress)
            cv2.rectangle(image, (w - 250, 20), (w - 250 + bar_width, 40), color, -1)
            cv2.rectangle(image, (w - 250, 20), (w - 50, 40), color, 2)
            cv2.putText(image, "Confirming...", (w - 200, 35), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 1)

        # Status text
        status_text = f"Gesture: {gesture.upper()}"
        cv2.putText(image, status_text, (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 1, color, 2)

        # Calibration status
        if not self.calibration.initialized:
            calib_text = "Press 'c' to calibrate"
            cv2.putText(image, calib_text, (10, h-30), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 255), 2)

        # Draw hand landmarks
        if landmarks:
            self.mp_drawing.draw_landmarks(image, landmarks, self.mp_hands.HAND_CONNECTIONS)

        return image

    def run_calibration(self):
        """Run three-step calibration process"""
        cap = cv2.VideoCapture(0, cv2.CAP_ANY)
        cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
        cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)

        gestures_to_calibrate = ['neutral', 'open', 'closed']
        current_gesture_idx = 0
        samples = []
        sample_time = 0
        collecting = False

        while cap.isOpened() and current_gesture_idx < len(gestures_to_calibrate):
            ret, frame = cap.read()
            if not ret:
                break

            frame = cv2.flip(frame, 1)
            rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            results = self.hands.process(rgb_frame)

            current_gesture = gestures_to_calibrate[current_gesture_idx]
            
            # Instructions
            instruction_text = f"Position {current_gesture_idx + 1}/3: Make {current_gesture.upper()} hand"
            cv2.putText(frame, instruction_text, (50, 50), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 255), 2)
            cv2.putText(frame, "Press SPACE when ready", (50, 100), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255, 255, 255), 2)

            if results.multi_hand_landmarks:
                hand_landmarks = results.multi_hand_landmarks[0]
                curl_score = self.calculate_overall_curl_score(hand_landmarks.landmark)
                
                if collecting:
                    samples.append(curl_score)
                    elapsed = time.time() - sample_time
                    countdown = max(0, 2.0 - elapsed)
                    
                    if countdown > 0:
                        cv2.putText(frame, f"Collecting... {countdown:.1f}s", 
                                  (50, 150), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
                        
                        # Progress bar
                        progress = min(1.0, (2.0 - countdown) / 2.0)
                        bar_width = int(300 * progress)
                        cv2.rectangle(frame, (50, 180), (50 + bar_width, 200), (0, 255, 0), -1)
                        cv2.rectangle(frame, (50, 180), (350, 200), (0, 255, 0), 2)
                    
                    if len(samples) >= 60:  # 2 seconds worth
                        avg_curl = np.mean(samples)
                        
                        if current_gesture == 'neutral':
                            self.calibration.neutral_curl_score = avg_curl
                        elif current_gesture == 'open':
                            self.calibration.open_curl_score = avg_curl
                        elif current_gesture == 'closed':
                            self.calibration.closed_curl_score = avg_curl
                        
                        current_gesture_idx += 1
                        samples = []
                        collecting = False
                
                # Show current curl score during calibration
                cv2.putText(frame, f"Curl: {curl_score:.3f}", (50, 250), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 1)
                
                # Draw landmarks
                self.mp_drawing.draw_landmarks(frame, hand_landmarks, self.mp_hands.HAND_CONNECTIONS)

            cv2.imshow('Calibration', frame)

            key = cv2.waitKey(1) & 0xFF
            if key == ord('q'):
                cap.release()
                cv2.destroyAllWindows()
                return False
            elif key == ord(' ') and not collecting and results.multi_hand_landmarks:
                collecting = True
                samples = []
                sample_time = time.time()

        cap.release()
        cv2.destroyAllWindows()
        
        if current_gesture_idx >= len(gestures_to_calibrate):
            self.calibration.initialized = True
            print(f"Calibration complete!")
            print(f"   Neutral: {self.calibration.neutral_curl_score:.3f}")
            print(f"   Open: {self.calibration.open_curl_score:.3f}")
            print(f"   Closed: {self.calibration.closed_curl_score:.3f}")
            return True
        
        return False

    def run(self):
        """Main loop for gesture recognition"""
        cap = cv2.VideoCapture(0, cv2.CAP_ANY)
        cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
        cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)

        while cap.isOpened():
            ret, frame = cap.read()
            if not ret:
                break

            frame = cv2.flip(frame, 1)
            rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            results = self.hands.process(rgb_frame)

            if results.multi_hand_landmarks:
                hand_landmarks = results.multi_hand_landmarks[0]
                
                if self.calibration.initialized:
                    gesture = self.process_hand(hand_landmarks.landmark, frame.shape)
                    frame = self.draw_feedback(frame, gesture, hand_landmarks)
                else:
                    frame = self.draw_feedback(frame, "neutral", hand_landmarks)
            else:
                self.state.hand_x, self.state.hand_y = -1, -1
                frame = self.draw_feedback(frame, "neutral", None)

            cv2.imshow('Gesture Recognition', frame)

            key = cv2.waitKey(1) & 0xFF
            if key == ord('q'):
                break
            elif key == ord('c'):
                cap.release()
                cv2.destroyAllWindows()
                success = self.run_calibration()
                if success:
                    cap = cv2.VideoCapture(0, cv2.CAP_ANY)
                    cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
                    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)
                else:
                    break

        cap.release()
        cv2.destroyAllWindows()
        self.hands.close()

if __name__ == "__main__":
    recognizer = GestureRecognizer()
    recognizer.run()
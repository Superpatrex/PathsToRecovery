import time
import cv2
import mediapipe as mp
import numpy as np
from collections import deque
from dataclasses import dataclass
from typing import List, Tuple
import math
from enum import Enum

class GestureMode(Enum):
    FIST_CURL = "fist_curl"
    WRIST_ROTATION = "wrist_rotation"

@dataclass
class GestureState:
    current_gesture: str = "neutral"
    gesture_start_time: float = 0
    is_transitioning: bool = False
    confidence: float = 0.0
    hand_x: float = 0.0
    hand_y: float = 0.0

@dataclass
class CalibrationData:
    # Fist curl calibration
    neutral_curl_score: float = 0.0
    open_curl_score: float = 0.0
    closed_curl_score: float = 0.0
    
    # Wrist rotation calibration
    neutral_palm_angle: float = 0.0
    palm_up_angle: float = 0.0
    palm_down_angle: float = 0.0
    
    fist_initialized: bool = False
    rotation_initialized: bool = False

class GestureRecognizer:
    def __init__(self, mode: GestureMode = GestureMode.FIST_CURL):
        self.mp_hands = mp.solutions.hands
        self.mp_drawing = mp.solutions.drawing_utils
        self.hands = self.mp_hands.Hands(
            static_image_mode=False,
            max_num_hands=1,
            min_detection_confidence=0.7,
            min_tracking_confidence=0.5
        )

        self.mode = mode
        self.state = GestureState()
        self.calibration = CalibrationData()

        self.position_history = deque(maxlen=10)
        self.velocity_history = deque(maxlen=8)
        self.gesture_history = deque(maxlen=8)
        self.curl_history = deque(maxlen=10)
        self.angle_history = deque(maxlen=10)  # New for rotation
        
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

    def calculate_palm_orientation_angle(self, landmarks) -> float:
        """
        Calculate palm orientation based on the normal vector to the palm plane.
        Returns angle in degrees where:
        - 0° = palm facing camera (neutral)
        - Positive = palm up
        - Negative = palm down
        """
        # Define three points on the palm to create a plane
        # Using wrist (0), base of middle finger (9), and base of pinky (17)
        wrist = np.array([landmarks[0].x, landmarks[0].y, landmarks[0].z])
        middle_base = np.array([landmarks[9].x, landmarks[9].y, landmarks[9].z])
        pinky_base = np.array([landmarks[17].x, landmarks[17].y, landmarks[17].z])
        
        # Create two vectors on the palm plane
        vec1 = middle_base - wrist
        vec2 = pinky_base - wrist
        
        # Calculate normal vector using cross product
        normal = np.cross(vec1, vec2)
        
        # Normalize the normal vector
        normal_magnitude = np.linalg.norm(normal)
        if normal_magnitude == 0:
            return 0.0
        
        normal_normalized = normal / normal_magnitude
        
        # Calculate angle between normal and camera direction (z-axis)
        # Camera direction is [0, 0, -1] (negative z points toward camera)
        camera_direction = np.array([0, 0, -1])
        
        # Calculate dot product to get cosine of angle
        dot_product = np.dot(normal_normalized, camera_direction)
        
        # Clamp to prevent numerical errors
        dot_product = np.clip(dot_product, -1.0, 1.0)
        
        # Calculate angle in radians, then convert to degrees
        angle_rad = np.arccos(dot_product)
        angle_deg = np.degrees(angle_rad)
        
        # Adjust sign based on hand orientation
        # If the normal's z-component is positive, palm is facing up
        if normal_normalized[2] > 0:
            angle_deg = -angle_deg  # Palm down (negative)
        else:
            angle_deg = angle_deg   # Palm up (positive)
            
        return angle_deg

    def calculate_relative_rotation_score(self, current_angle: float) -> dict:
        """Calculate relative scores for rotation gestures based on calibration"""
        if not self.calibration.rotation_initialized:
            return {'open': 0, 'neutral': 1, 'closed': 0}
        
        # Calculate distances to each calibrated angle
        dist_to_up = abs(current_angle - self.calibration.palm_up_angle)
        dist_to_neutral = abs(current_angle - self.calibration.neutral_palm_angle)
        dist_to_down = abs(current_angle - self.calibration.palm_down_angle)
        
        # Convert distances to scores (closer = higher score)
        max_dist = max(dist_to_up, dist_to_neutral, dist_to_down) + 0.001
        
        scores = {
            'open': 1 - (dist_to_up / max_dist),
            'neutral': 1 - (dist_to_neutral / max_dist),
            'closed': 1 - (dist_to_down / max_dist)
        }
        
        return scores

    def classify_rotation_gesture(self, landmarks) -> Tuple[str, float, dict]:
        """Classify gesture based on palm rotation"""
        angle = self.calculate_palm_orientation_angle(landmarks)
        self.angle_history.append(angle)
        
        # Get relative scores for each gesture
        scores = self.calculate_relative_rotation_score(angle)
        
        # Find the gesture with highest score
        best_gesture = max(scores, key=scores.get)
        best_score = scores[best_gesture]
        
        # Calculate confidence
        sorted_scores = sorted(scores.values(), reverse=True)
        if len(sorted_scores) > 1:
            confidence = sorted_scores[0] - sorted_scores[1]
        else:
            confidence = best_score
        
        # Require minimum confidence for non-neutral gestures
        if best_gesture != 'neutral' and confidence < 0.2:
            return 'neutral', confidence, scores
        
        return best_gesture, confidence, scores

    # Keep existing fist curl methods
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
        min_distance = 0.06  # Very curled fist
        max_distance = 0.55  # Fully extended/spread hand
        
        # Clamp the distance to expected range
        clamped_distance = np.clip(avg_distance, min_distance, max_distance)
        
        # Convert to curl score (invert so smaller distance = higher curl)
        curl_score = 1.0 - ((clamped_distance - min_distance) / (max_distance - min_distance))
        
        return np.clip(curl_score, 0, 1)

    def calculate_relative_curl_score(self, current_curl: float) -> dict:
        """Calculate relative scores for each curl gesture based on calibration"""
        if not self.calibration.fist_initialized:
            return {'open': 0, 'neutral': 1, 'closed': 0}
        
        # Calculate distances to each calibrated gesture
        dist_to_open = abs(current_curl - self.calibration.open_curl_score)
        dist_to_neutral = abs(current_curl - self.calibration.neutral_curl_score)
        dist_to_closed = abs(current_curl - self.calibration.closed_curl_score)
        
        # Convert distances to scores (closer = higher score)
        max_dist = max(dist_to_open, dist_to_neutral, dist_to_closed) + 0.001
        
        scores = {
            'open': 1 - (dist_to_open / max_dist),
            'neutral': 1 - (dist_to_neutral / max_dist),
            'closed': 1 - (dist_to_closed / max_dist)
        }
        
        return scores

    def classify_curl_gesture(self, landmarks) -> Tuple[str, float, dict]:
        """Classify gesture using relative scoring for fist curl"""
        curl_score = self.calculate_overall_curl_score(landmarks)
        self.curl_history.append(curl_score)
        
        # Get relative scores for each gesture
        scores = self.calculate_relative_curl_score(curl_score)
        
        # Find the gesture with highest score
        best_gesture = max(scores, key=scores.get)
        best_score = scores[best_gesture]
        
        # Calculate confidence
        sorted_scores = sorted(scores.values(), reverse=True)
        if len(sorted_scores) > 1:
            confidence = sorted_scores[0] - sorted_scores[1]
        else:
            confidence = best_score
        
        # Require minimum confidence for non-neutral gestures
        if best_gesture != 'neutral' and confidence < 0.2:
            return 'neutral', confidence, scores
        
        return best_gesture, confidence, scores

    def classify_raw_gesture(self, landmarks) -> Tuple[str, float, dict]:
        """Classify gesture based on current mode"""
        if self.mode == GestureMode.FIST_CURL:
            return self.classify_curl_gesture(landmarks)
        else:  # WRIST_ROTATION
            return self.classify_rotation_gesture(landmarks)

    def update_state_machine(self, raw_gesture: str, confidence: float, scores: dict) -> str:
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

                        print(f"Gesture: {raw_gesture.upper()}")
                        print(f"  Confidence: {confidence:.3f}")
                        if self.mode == GestureMode.FIST_CURL:
                            print(f"  Scores - Open: {scores['open']:.3f}, Neutral: {scores['neutral']:.3f}, Closed: {scores['closed']:.3f}")
                        else:
                            print(f"  Scores - Open: {scores['open']:.3f}, Neutral: {scores['neutral']:.3f}, Closed: {scores['closed']:.3f}")
                        
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

    def calculate_hand_velocity(self, current_position: np.ndarray) -> float:
        if len(self.position_history) < 2:
            return 0.0
        prev_position = self.position_history[-1]
        velocity = np.linalg.norm(current_position - prev_position)
        return velocity

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
        raw_gesture, confidence, scores = self.classify_raw_gesture(landmarks)
        self.gesture_history.append(raw_gesture)

        # Apply state machine for confirmation
        confirmed_gesture = self.update_state_machine(raw_gesture, confidence, scores)

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
            cv2.rectangle(image, (w - 250, 60), (w - 250 + bar_width, 80), color, -1)
            cv2.rectangle(image, (w - 250, 60), (w - 50, 80), color, 2)
            cv2.putText(image, "Confirming...", (w - 200, 75), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 1)

        # Status text
        mode_text = "Fist" if self.mode == GestureMode.FIST_CURL else "Rotation"
        status_text = f"Mode: {mode_text} | Gesture: {gesture.upper()}"
        cv2.putText(image, status_text, (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 0.8, color, 2)

        # Calibration status
        is_calibrated = (self.calibration.fist_initialized if self.mode == GestureMode.FIST_CURL 
                        else self.calibration.rotation_initialized)
        
        if not is_calibrated:
            calib_text = "Press 'c' to calibrate"
            cv2.putText(image, calib_text, (10, h-30), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 255), 2)

        # Mode switching instructions
        cv2.putText(image, "Press 'm' to switch mode", (10, h-60), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 1)

        # Draw hand landmarks
        if landmarks:
            self.mp_drawing.draw_landmarks(image, landmarks, self.mp_hands.HAND_CONNECTIONS)

        return image

    def run_calibration(self):
        """Run calibration process based on current mode"""
        cap = cv2.VideoCapture(0, cv2.CAP_ANY)
        cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
        cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)

        if self.mode == GestureMode.FIST_CURL:
            gestures_to_calibrate = ['neutral', 'open', 'closed']
        else:  # WRIST_ROTATION
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
            
            # Instructions based on mode
            if self.mode == GestureMode.FIST_CURL:
                instruction_text = f"Position {current_gesture_idx + 1}/3: Make {current_gesture.upper()} hand"
            else:
                if current_gesture == 'neutral':
                    instruction_text = f"Position {current_gesture_idx + 1}/3: Hand flat facing camera"
                elif current_gesture == 'open':
                    instruction_text = f"Position {current_gesture_idx + 1}/3: Palm UP (parallel to camera)"
                else:  # closed
                    instruction_text = f"Position {current_gesture_idx + 1}/3: Palm DOWN (away from camera)"
            
            cv2.putText(frame, instruction_text, (20, 50), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 255), 2)
            cv2.putText(frame, "Press SPACE when ready", (50, 100), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255, 255, 255), 2)

            if results.multi_hand_landmarks:
                hand_landmarks = results.multi_hand_landmarks[0]
                
                if self.mode == GestureMode.FIST_CURL:
                    measurement = self.calculate_overall_curl_score(hand_landmarks.landmark)
                    measurement_name = "Curl"
                else:
                    measurement = self.calculate_palm_orientation_angle(hand_landmarks.landmark)
                    measurement_name = "Angle"
                
                if collecting:
                    samples.append(measurement)
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
                        avg_measurement = np.mean(samples)
                        
                        if self.mode == GestureMode.FIST_CURL:
                            if current_gesture == 'neutral':
                                self.calibration.neutral_curl_score = avg_measurement
                            elif current_gesture == 'open':
                                self.calibration.open_curl_score = avg_measurement
                            elif current_gesture == 'closed':
                                self.calibration.closed_curl_score = avg_measurement
                        else:  # WRIST_ROTATION
                            if current_gesture == 'neutral':
                                self.calibration.neutral_palm_angle = avg_measurement
                            elif current_gesture == 'open':
                                self.calibration.palm_up_angle = avg_measurement
                            elif current_gesture == 'closed':
                                self.calibration.palm_down_angle = avg_measurement
                        
                        current_gesture_idx += 1
                        samples = []
                        collecting = False
                
                # Show current measurement
                cv2.putText(frame, f"{measurement_name}: {measurement:.3f}", (50, 250), 
                          cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 1)
                
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
            if self.mode == GestureMode.FIST_CURL:
                self.calibration.fist_initialized = True
            else:
                self.calibration.rotation_initialized = True
            return True
        
        return False

    def switch_mode(self):
        """Switch between fist curl and wrist rotation modes"""
        if self.mode == GestureMode.FIST_CURL:
            self.mode = GestureMode.WRIST_ROTATION
            print("Switched to WRIST ROTATION mode")
        else:
            self.mode = GestureMode.FIST_CURL
            print("Switched to FIST CURL mode")
        
        # Reset state when switching modes
        self.state.current_gesture = "neutral"
        self.state.is_transitioning = False

    def run(self):
        """Main loop for gesture recognition"""
        cap = cv2.VideoCapture(0, cv2.CAP_ANY)
        cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
        cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)

        print("Gesture Recognition Controls:")
        print("- Press 'q' to quit")
        print("- Press 'c' to calibrate current mode")
        print("- Press 'm' to switch between fist curl and wrist rotation modes")

        while cap.isOpened():
            ret, frame = cap.read()
            if not ret:
                break

            frame = cv2.flip(frame, 1)
            rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            results = self.hands.process(rgb_frame)

            if results.multi_hand_landmarks:
                hand_landmarks = results.multi_hand_landmarks[0]
                
                # Check if current mode is calibrated
                is_calibrated = (self.calibration.fist_initialized if self.mode == GestureMode.FIST_CURL 
                               else self.calibration.rotation_initialized)
                
                if is_calibrated:
                    gesture = self.process_hand(hand_landmarks.landmark, frame.shape)
                    frame = self.draw_feedback(frame, gesture, hand_landmarks)
                else:
                    frame = self.draw_feedback(frame, "neutral", hand_landmarks)
            else:
                self.state.hand_x = -1.0
                self.state.hand_y = -1.0
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
            elif key == ord('m'):
                self.switch_mode()

        cap.release()
        cv2.destroyAllWindows()
        self.hands.close()

if __name__ == "__main__":
    # Start with fist curl mode by default
    recognizer = GestureRecognizer(mode=GestureMode.FIST_CURL)
    recognizer.run()
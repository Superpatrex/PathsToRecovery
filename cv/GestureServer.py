import socket
import threading
import json
import struct
import time
from GestureRecognizer import GestureRecognizer

class GestureServer:
    def __init__(self, host='127.0.0.1', port=8081):
        self.host = host
        self.port = port
        self.running = False
        self.gesture_recognizer = GestureRecognizer()
        self.last_gesture = "neutral"  # Track gesture changes

    def get_gesture_data(self):
        """Get current gesture data"""
        current_gesture = self.gesture_recognizer.state.current_gesture
        confidence = self.gesture_recognizer.state.confidence

        # Print when gesture changes
        if current_gesture != self.last_gesture:
            print(f"[GESTURE] Changed: {self.last_gesture} -> {current_gesture} (confidence: {confidence:.2f})")
            self.last_gesture = current_gesture

        data = {
            "gesture": current_gesture,
            "confidence": confidence,
            "is_transitioning": self.gesture_recognizer.state.is_transitioning,
            "hand_x": self.gesture_recognizer.state.hand_x,
            "hand_y": self.gesture_recognizer.state.hand_y,
            "timestamp": time.time()
        }
        return data

    def handle_client(self, client_socket, client_address):
        """Handle client connection"""
        print(f"[SERVER] Client connected: {client_address}")
        message_count = 0

        try:
            while self.running:
                data = self.get_gesture_data()
                json_data = json.dumps(data)
                data_bytes = json_data.encode('utf-8')

                # Send length first (4 bytes, big-endian)
                length = len(data_bytes)
                length_bytes = struct.pack('>I', length)

                client_socket.send(length_bytes)
                client_socket.send(data_bytes)

                message_count += 1

                # Debug every 10 messages (1 second)
                if message_count % 10 == 0:
                    print(f"[SERVER] Sent to {client_address}: {data}")

                # Debug first few messages to verify data flow
                if message_count <= 3:
                    print(f"[SERVER] Message {message_count} to {client_address}: {json_data}")

                time.sleep(0.1)  # 10 FPS

        except Exception as e:
            print(f"[SERVER] Client {client_address} disconnected: {e}")
        finally:
            client_socket.close()

    def start_server(self):
        """Start the TCP server"""
        try:
            server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            server_socket.bind((self.host, self.port))
            server_socket.listen(5)
        except OSError as e:
            print(f"[SERVER] Error: {e}")
            print(f"[SERVER] Port {self.port} is already in use. Try a different port or kill existing process.")
            return

        print(f"Gesture TCP Server started on {self.host}:{self.port}")
        self.running = True

        while self.running:
            try:
                client_socket, client_address = server_socket.accept()
                client_thread = threading.Thread(
                    target=self.handle_client,
                    args=(client_socket, client_address),
                    daemon=True
                )
                client_thread.start()
            except:
                break

        server_socket.close()

    def run(self):
        """Run server and gesture recognition"""
        print("Starting TCP server in background thread...")

        # Start TCP server in separate thread
        server_thread = threading.Thread(target=self.start_server, daemon=True)
        server_thread.start()

        print("TCP server started, now starting gesture recognition in main thread...")
        print("Camera window will appear - press 'q' in the window to quit")

        # Run gesture recognition in main thread (OpenCV needs this)
        try:
            self.gesture_recognizer.run()
        except KeyboardInterrupt:
            print("\nShutting down...")
        finally:
            self.running = False

if __name__ == "__main__":
    server = GestureServer()
    print("Starting Gesture TCP Server...")
    print("Press Ctrl+C to stop")
    server.run()
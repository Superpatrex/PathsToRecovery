using UnityEngine;
using System;
using System.Collections;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.IO;

public class TCPClient : MonoBehaviour
{
    [Header("Connection Settings")]
    public string TCP_SERVER_HOST = "127.0.0.1";
    public int TCP_SERVER_PORT = 8081;
    public float reconnectDelay = 5.0f;

    [Header("Gesture Data")]
    public GestureState currentGestureState;
    public string currentGesture = "neutral";
    public float gestureConfidence = 0.0f;
    public bool isGestureTransitioning = false;
    public float handX = 0.0f;
    public float handY = 0.0f;

    [Header("//Debug")]
    public bool enableDebugLogging = false;

    private TcpClient client;
    private NetworkStream stream;
    private Thread clientThread;

    private volatile bool isConnected = false;
    private volatile bool shouldReconnect = true;
    private string lastGesture = "";

    private object dataLock = new object();
    private string receivedData = "";

    // Events for gesture changes
    public System.Action<string> OnGestureChanged;
    public System.Action OnGestureTransitionStart;
    public System.Action OnGestureTransitionEnd;

    void Start()
    {
        StartConnection();
    }

    void StartConnection()
    {
        shouldReconnect = true;
        clientThread = new Thread(new ThreadStart(ConnectToServer));
        clientThread.IsBackground = true;
        clientThread.Start();
    }

    void Update()
    {
        string dataToProcess = "";

        lock (dataLock)
        {
            dataToProcess = receivedData;
            receivedData = "";
        }

        if (!string.IsNullOrEmpty(dataToProcess))
        {
            if (enableDebugLogging && Time.frameCount % 30 == 0)
            {
                ////Debug.Log($"[TCP] Raw JSON received: {dataToProcess}");
            }
            ProcessGestureData(dataToProcess);
        }

    }

    void ProcessGestureData(string jsonData)
    {
        try
        {
            if (enableDebugLogging && Time.frameCount % 60 == 0) // //Debug every 60 frames
            {
                ////Debug.Log($"[TCP] Processing JSON: {jsonData}");
            }

            currentGestureState = JsonUtility.FromJson<GestureState>(jsonData);

            if (currentGestureState == null)
            {
                if (enableDebugLogging)
                    //Debug.LogWarning("[TCP] Failed to parse gesture data - received null");

                currentGesture = "neutral";
                gestureConfidence = 0.0f;
                isGestureTransitioning = false;
                handX = 0.0f;
                handY = 0.0f;
                return;
            }

            if (enableDebugLogging && Time.frameCount % 60 == 0)
            {
                //Debug.Log($"[TCP] Parsed state - Gesture: {currentGestureState.gesture}, Confidence: {currentGestureState.confidence}, Hand: ({currentGestureState.hand_x:F3}, {currentGestureState.hand_y:F3})");
            }

            string newGesture = currentGestureState.gesture ?? "neutral";
            float newConfidence = currentGestureState.confidence;
            bool newTransitioning = currentGestureState.is_transitioning;
            float newHandX = currentGestureState.hand_x;
            float newHandY = currentGestureState.hand_y;

            // Check for gesture changes
            if (newGesture != lastGesture)
            {
                if (enableDebugLogging)
                    //Debug.Log($"Gesture changed: {lastGesture} -> {newGesture} (confidence: {newConfidence:F2}) at ({newHandX:F2}, {newHandY:F2})");

                OnGestureChanged?.Invoke(newGesture);
                lastGesture = newGesture;
            }

            // Check for transition changes
            if (newTransitioning != isGestureTransitioning)
            {
                if (newTransitioning)
                    OnGestureTransitionStart?.Invoke();
                else
                    OnGestureTransitionEnd?.Invoke();
            }

            currentGesture = newGesture;
            gestureConfidence = newConfidence;
            isGestureTransitioning = newTransitioning;
            handX = newHandX;
            handY = newHandY;

            // Log gesture data for debugging
            if (enableDebugLogging && Time.frameCount % 30 == 0) // Log every 30 frames
            {
                //Debug.Log($"[TCP] Gesture: {currentGesture}, Confidence: {gestureConfidence:F2}, Position: ({handX:F2}, {handY:F2}), Transitioning: {isGestureTransitioning}");
            }
        }
        catch (Exception e)
        {
            //Debug.LogError("Error processing gesture data: " + e.Message);
        }
    }

    // Public methods for other scripts to use
    public bool IsConnected()
    {
        return isConnected;
    }

    public bool IsGestureClosed()
    {
        return currentGesture == "closed";
    }

    public bool IsGestureOpen()
    {
        return currentGesture == "open";
    }

    public bool IsGestureNeutral()
    {
        return currentGesture == "neutral";
    }

    public string GetCurrentGesture()
    {
        return currentGesture;
    }

    public string GetLastGesture()
    {
        return lastGesture;
    }

    public float GetGestureConfidence()
    {
        return gestureConfidence;
    }

    public bool IsTransitioning()
    {
        return isGestureTransitioning;
    }

    public float GetHandX()
    {
        return handX;
    }

    public float GetHandY()
    {
        return handY;
    }

    public Vector2 GetHandPosition()
    {
        return new Vector2(handX, handY);
    }

    public Vector2 GetHandPositionScreenSpace()
    {
        return new Vector2(handX * Screen.width, handY * Screen.height);
    }

    public Vector3 GetHandPositionWorldSpace(Camera camera, float depth = 10f)
    {
        Vector3 screenPos = new Vector3(handX * Screen.width, handY * Screen.height, depth);
        return camera.ScreenToWorldPoint(screenPos);
    }

    public bool HasValidHandPosition()
    {
        return handX >= 0f && handX <= 1f && handY >= 0f && handY <= 1f;
    }

    void ConnectToServer()
    {
        while (shouldReconnect)
        {
            try
            {
                if (enableDebugLogging)
                    //Debug.Log($"Attempting to connect to gesture server at {TCP_SERVER_HOST}:{TCP_SERVER_PORT}");

                client = new TcpClient(TCP_SERVER_HOST, TCP_SERVER_PORT);
                stream = client.GetStream();
                isConnected = true;

                //Debug.Log($"[TCP] Connected to gesture server at {TCP_SERVER_HOST}:{TCP_SERVER_PORT}");

                while (isConnected && shouldReconnect)
                {
                    byte[] lengthBuffer = new byte[4];
                    int bytesRead = stream.Read(lengthBuffer, 0, lengthBuffer.Length);

                    if (bytesRead == 0)
                    {
                        //Debug.Log("Connection closed by the server");
                        break;
                    }

                    // Convert the length buffer to an integer (big-endian from Python server)
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(lengthBuffer);
                    }

                    int dataLength = BitConverter.ToInt32(lengthBuffer, 0);

                    if (dataLength <= 0 || dataLength > 1024 * 1024) // Sanity check
                    {
                        //Debug.LogError($"Invalid data length received: {dataLength}");
                        break;
                    }

                    byte[] data = ReceiveAll(dataLength);

                    if (data == null)
                    {
                        //Debug.Log("Connection closed by the server");
                        break;
                    }

                    string dataString = Encoding.UTF8.GetString(data);

                    lock (dataLock)
                    {
                        receivedData = dataString;
                    }
                }
            }
            catch (IOException ioEx)
            {
                //Debug.LogError("IO Exception: " + ioEx.Message);
            }
            catch (SocketException sockEx)
            {
                //Debug.LogError("Socket Exception: " + sockEx.Message);
            }
            catch (Exception e)
            {
                //Debug.LogError("Exception: " + e.Message);
            }
            finally
            {
                isConnected = false;
                CleanupConnection();
            }

            if (shouldReconnect)
            {
                //Debug.Log($"Reconnecting in {reconnectDelay} seconds...");
                Thread.Sleep((int)(reconnectDelay * 1000));
            }
        }
    }

    void CleanupConnection()
    {
        try
        {
            if (stream != null) stream.Close();
            if (client != null) client.Close();
        }
        catch (Exception e)
        {
            //Debug.LogError("Error during connection cleanup: " + e.Message);
        }
    }

    byte[] ReceiveAll(int length)
    {
        byte[] data = new byte[length];
        int totalReceived = 0;

        while (totalReceived < length)
        {
            int received = stream.Read(data, totalReceived, length - totalReceived);

            if (received == 0)
            {
                return null;
            }

            totalReceived += received;
        }

        return data;
    }

    void OnApplicationQuit()
    {
        shouldReconnect = false;
        isConnected = false;

        if (client != null)
        {
            client.Close();
        }

        if (clientThread != null && clientThread.IsAlive)
        {
            clientThread.Join(1000); // Wait up to 1 second for thread to finish
        }
    }

    void OnDestroy()
    {
        OnApplicationQuit();
    }
}

[Serializable]
public class GestureState
{
    public string gesture;
    public float confidence;
    public bool is_transitioning;
    public float hand_x;
    public float hand_y;
    public float timestamp;

    public override string ToString()
    {
        return $"Gesture: {gesture}, Confidence: {confidence:F2}, Position: ({hand_x:F2}, {hand_y:F2}), Transitioning: {is_transitioning}, Time: {timestamp}";
    }
}
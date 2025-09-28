using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ControlMouse : MonoBehaviour
{
    public Image image;
    public TCPClient tcpClient;
    void Start()
    {
        
    }

    void Update()
    {
        var handPos = tcpClient.GetHandPosition();
        float x = handPos.x * 700f - 350f;
        float y = -1f * (handPos.y * 400f - 200f);
        this.image.rectTransform.anchoredPosition = new Vector2(x, y);
        //Debug.Log($"Hand Position: ({handPos.x}, {handPos.y}) -> Image Position: ({x}, {y})");
    }
}

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
        Debug.Log(handPos.ToString());
        float x = handPos.x * 3000f - 1500f;
        float y = -1f * (handPos.y * 1640f - 820f);
        this.image.rectTransform.anchoredPosition = new Vector2(x, y);
        //Debug.Log($"Hand Position: ({handPos.x}, {handPos.y}) -> Image Position: ({x}, {y})");
    }
}

// シリアル通信で受信したメッセージをコンソールに表示するスクリプト ※ここでは"Hello World!"
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelloWorld : MonoBehaviour
{
    public SerialHandler serialHandler;

    void Start()
    {
        serialHandler.OnDataReceived += OnDataReceived;
    }

    void OnDataReceived(string message)
    {
        try
        {
            Debug.Log(message);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e.Message);
        }
    }
}
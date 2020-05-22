using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class receive_data_load : MonoBehaviour
{
    public SerialHandler_load serialHandler;
    public float load_val;

    void Start()
    {
        //信号を受信したときに、そのメッセージの処理を行う
        serialHandler.OnDataReceived += OnDataReceived;
    }

    void Update()
    {
        //Rキーで値取得のためのコマンド送信
        if (Input.GetKey(KeyCode.R))
        {
            serialHandler.Write("RCLM\r\n");  
        }
    }

    //受信処理
    void OnDataReceived(string message)
    {
        try
        {
            //load_val = float.Parse(message);
            //Debug.Log("load: " + load_val);
            string[] data = message.Split(new string[] { "," }, StringSplitOptions.None);
            string[] data2 = data[1].Split(new string[] { "  " }, StringSplitOptions.None);
            load_val = float.Parse(data2[0]);
            Debug.Log("load: " + load_val);
            //Debug.Log(message);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e.Message);
        }
    }
}
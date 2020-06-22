using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class receive_data : MonoBehaviour
{
    public SerialHandler serialHandler;
    public GameObject FaceController;
    public float yaw_val;
    public float pitch_val;
    private bool init = false;       //初期化フラグ

    void Start()
    {
        //信号を受信したときに、そのメッセージの処理を行う
        serialHandler.OnDataReceived += OnDataReceived;
    }

    void Update()
    {
        //姿勢初期化フラグ
        init = FaceController.GetComponent<FaceController_gyro_D_rotation>().init_flag;

        //Rキーで姿勢を初期化する
        if (Input.GetKey(KeyCode.R))
        {
            serialHandler.Write("0");   //文字列を送信
        }
        //Pキーでpitch姿勢のみ初期化する
        if (Input.GetKey(KeyCode.P) || init == true)
        {
            serialHandler.Write("1");   //文字列を送信
        }
    }

    //受信処理
    void OnDataReceived(string message)
    {
        try
        {
            //"\t"分割でpitchとyaw情報取得
            string[] data = message.Split(new string[] { "\t" }, StringSplitOptions.None);
            //Debug.Log("pitch: " + data[0] + " yaw: " + data[1]);
            yaw_val = -float.Parse(data[1]);        //ここで±決める
            pitch_val = float.Parse(data[0]);
            //Debug.Log(message);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e.Message);
        }
    }
}
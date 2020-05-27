using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class receive_data_airpress : MonoBehaviour
{
    public SerialHandler_airpress serialHandler;
    public float f_val;     //左右方向のフォース
    public float f_val_ud;     //上下方向のフォース
    //
    private bool cal_flag_1 = false;  //キャリブレーションフラグ1：初期化
    private bool cal_flag_2 = false;  //キャリブレーションフラグ2：キャリブレーション終了
    private float P0, P1;            //圧力生データ
    private float P0_initial = 1.0f; //圧力初期値
    private float P1_initial = 1.0f;
    private float D0, D1;            //圧力変化量
    private float D_right_max = 0.0f;     //最大圧力変化量
    private float D_left_max = 0.0f;
    private float D_up_max = 0.0f;
    private float D_down_min = 0.0f;


    void Start()
    {
        //信号を受信したときに、そのメッセージの処理を行う
        serialHandler.OnDataReceived += OnDataReceived;
    }

    void Update()
    {
        //キャリブレーション//////////////////////////////////////////////////////////////
        if (Input.GetKey(KeyCode.R))
        {
            P0_initial = P0;    //頭を中心部に置いた時の圧力
            P1_initial = P1;
            D_right_max = 0.0f;
            D_left_max = 0.0f;
            D_up_max = 0.0f;
            D_down_min = 0.0f;
            f_val = 0.0f;
            f_val_ud = 0.0f;
            cal_flag_1 = true;
            cal_flag_2 = false;
        }
        if (Input.GetKey(KeyCode.S))
        {
            cal_flag_1 = false;
            cal_flag_2 = true;
        }

        if (cal_flag_1 == true)
        {
            //圧力の変化量
            D0 = (P0 - P0_initial) / P0_initial;
            D1 = (P1 - P1_initial) / P1_initial;
            //Debug.Log("D0: " + D0 + " D1: " + D1);
            //左右最大ストローク
            if (D_right_max < (D1 - D0))
            {
                D_right_max = D1 - D0;
            }
            if (D_left_max < (D0 - D1))
            {
                D_left_max = D0 - D1;
            }
            //上下最大ストローク
            if (D0 * D1 > 0.0f)         //左右のエアバッグ共に圧力+もしくは-
            {
                if (D_up_max < (D0 + D1))
                {
                    D_up_max = D0 + D1;
                }
                if (D_down_min > (D0 + D1))
                {
                    D_down_min = D0 + D1;
                }
            }
            Debug.Log("D_right_max: " + D_right_max + " D_left_max: " + D_left_max + "D_up_max: " + D_up_max + " D_down_min: " + D_down_min);
            //Debug.Log("D0 " + D0 + "D1 " + D1);
        }

        //値の計算///////////////////////////////////////////////////////////////////////////
        if (cal_flag_2 == true)
        {
            //圧力の変化量
            D0 = (P0 - P0_initial) / P0_initial;
            D1 = (P1 - P1_initial) / P1_initial;
            //圧力の左右変化量
            if (D1 > D0)
            {
                f_val = (D1 - D0) / D_right_max * 100;   //%表示
            }
            else
            {
                f_val = -(D0 - D1) / D_left_max * 100;
            }
            //圧力の上下変化量
            if (D0 > 0.0f && D1 > 0.0f)     //左右のエアバッグ共に圧力がかかる  if (D0 > 0.0f && D1 > 0.0f && Math.Abs(D1 - D0) < D_up_max * 0.5f)
            {
                f_val_ud = (D0 + D1) / D_up_max * 100;
            }
            else if (D0 < 0.0f && D1 < 0.0f)   //左右のエアバッグ共に圧力が下がる
            {
                f_val_ud = -(D0 + D1) / D_down_min * 100;
            }
            else
            {
                //f_val_ud = 0.0f * 100;            //どちらかのエアバッグに圧力がかかる（回転中）
            }
            Debug.Log("f_val " + f_val + "f_val_ud " + f_val_ud);
            //Debug.Log("D0 " + D0 + "D1 " + D1);
        }
    }

    //受信処理
    void OnDataReceived(string message)
    {
        try
        {
            //Debug.Log(message);
            //"\t"分割でpitchとyaw情報取得
            string[] data = message.Split(new string[] { "\t" }, StringSplitOptions.None);
            P0 = float.Parse(data[0]);      //圧力生データ
            P1 = float.Parse(data[1]);
            //Debug.Log("P0: " + P0 + " P1: " + P1);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e.Message);
        }
    }
}
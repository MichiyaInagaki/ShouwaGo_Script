using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.IO;
using UnityEngine.UI;


public class FaceController_gyro_airpress : MonoBehaviour
{

    public GameObject Data_Receiver;
    public GameObject VReye;
    public Camera mainCamera;
    //各種パラメータ
    public float t_force = 50.0f;          //回転閾値
    public float t_force_ud = 50.0f;       //歩行閾値
    public float t_force_ud_2 = 30.0f;     //歩行閾値（長押し解除）
    public int stride = 100;               //離散移動の歩幅
    public float rotation_speed = 0.5f;            //旋回スピード
    //
    private Vector3 lastMousePosition;
    private Vector3 newAngle = new Vector3(0, 0, 0);
    private float force;                    //空気圧の値（左右）
    private float force_ud;                    //空気圧の値（上下）
    private float force_to_angle;           //空気圧の値を角度に変換した値
    private float yaw_angle = 0.0f;
    private float pitch_angle = 0.0f;
    //書き出し用変数
    private int log_key = 0;
    private float log_yaw = 0.0f;
    private float log_pitch = 0.0f;
    private float log_force = 0.0f;
    private string filePath = @"C:\Users\inaga\OneDrive\デスクトップ\exe\csv\test_F3.csv";
    private string filePath2 = @"C:\Users\inaga\OneDrive\デスクトップ\exe\csv\test_F4.csv";
    //ローパスフィルタ
    private float filter_gain = 0.75f;      //default: 0.75
    private float pre_yaw = 0.0f;
    private float pre_pitch = 0.0f;
    private float pre_force = 0.0f;
    private float mod_force = 0.0f;
    //平均値フィルタ
    private const int AVE_NUM = 10;               //平均するデータの個数
    private float ave_force = 0;
    private float[] list_force = new float[AVE_NUM];
    //回転用変数
    private float initial_angle = 0.0f;             //初期角度
    private float initial_angle_pitch = 0.0f;       //初期角度pitch
    private float rotation_angle = 0.0f;            //旋回角度
    private float rotation_angle_pitch = 0.0f;      //旋回角度
    //private float move_angle = 20.0f;               //追従角度（局所回転を行う角度）
    //private float move_angle_pitch = 10.0f;         //追従角度（局所回転を行う角度）
    //private float temp_yaw_angle = 0.0f;            //一時格納する角度
    //private float max_pitch = 20;                   //キーボード操作pitch角最大
    //private float min_pitch = -20;                  //キーボード操作pitch角最小
    //ゲイン
    private float rotation_gain = 1.5f;     //局所回転ゲイン
    private float pitch_gain = 1.0f;        //pitchゲイン
    //歩行
    private bool first_step_flag = true;    //短押し用フラグ
    private float span = 1.0f;              //長押しの時間間隔
    private float currentTime = 0f;
    private bool rotate_flag = false;       //回転中かどうかのフラグ
    //public float moveSpeed = 0.01f;
    //public float moveAngleX = 20.0f; 
    //
    private bool f1_flag = false;
    private bool f2_flag = false;
    private bool f3_flag = false;
    private bool f4_flag = false;
    private bool f5_flag = false;
    private bool f6_flag = false;
    private bool f7_flag = false;


    void Start()
    {
        // ファイル書き出し
        // ヘッダー出力
        string[] s1 = { "key", "yaw", "pitch", "force"};
        string s2 = string.Join(",", s1);   //s1を一つの文字列として
        File.AppendAllText(filePath, s2 + "\n");          //書き込み
        File.AppendAllText(filePath2, s2 + "\n");          //書き込み

        //角度の初期化
        newAngle.y = initial_angle;
    }

    void Update()
    {
        //シリアル通信から値を取得
        log_yaw = Data_Receiver.GetComponent<receive_data_gyro_airpress>().yaw_val;
        log_pitch = Data_Receiver.GetComponent<receive_data_gyro_airpress>().pitch_val;
        force = Data_Receiver.GetComponent<receive_data_gyro_airpress>().f_val;
        force_ud = Data_Receiver.GetComponent<receive_data_gyro_airpress>().f_val_ud;
        //
        log_force = force;

        //動作切り替え
        if (Input.GetKey(KeyCode.F1))
        {
            FlagDown();
            f1_flag = true;
        }
        if (Input.GetKey(KeyCode.F2))
        {
            FlagDown();
            f2_flag = true;
        }
        if (Input.GetKey(KeyCode.F3))
        {
            FlagDown();
            f3_flag = true;
        }
        if (Input.GetKey(KeyCode.F4))
        {
            FlagDown();
            f4_flag = true;
        }
        if (Input.GetKey(KeyCode.F5))
        {
            FlagDown();
            f5_flag = true;
        }
        if (Input.GetKey(KeyCode.F6))
        {
            FlagDown();
            f6_flag = true;
        }
        if (Input.GetKey(KeyCode.F7))
        {
            FlagDown();
            f7_flag = true;
        }


        //各種処理
        if (f1_flag == true)
        {
            //1. 空気圧センサの動作確認（閾値） /////////////////////////////////////////////////////////////////////////
            yaw_angle = 0;
            pitch_angle = 0;
            //回転制御部
            if (Math.Abs(force) > t_force)
            {
                rotate_flag = true;
                if (force > 0)
                {
                    rotation_angle -= rotation_speed;
                }
                else
                {
                    rotation_angle += rotation_speed;
                }
            }
            else
            {
                rotate_flag = false;
            }
            newAngle.y = yaw_angle - initial_angle + rotation_angle;     //首回転角＋初期調整角度＋旋回角度
            newAngle.z = 0;                                              //首回転角roll初期化
            newAngle.x = pitch_angle + initial_angle_pitch;              //首回転角pitch
            VReye.gameObject.transform.localEulerAngles = newAngle;
            //歩行制御部（長押し・短押し対応版）
            if (force_ud > t_force_ud && rotate_flag == false)           //押し込み圧力が閾値超える＋回転していない
            {
                currentTime += Time.deltaTime;  //長押しの時間カウント
                if (first_step_flag == true)    //最初に押した瞬間は一歩進む（短押し用）
                {
                    moveForward_D();
                    first_step_flag = false;
                }
                else
                {
                    if (currentTime > span)     //長押しで一定時間ごとに前進
                    {
                        moveForward_D();
                        currentTime = 0f;
                    }
                }
            }
            if (force_ud < t_force_ud_2)  //ボタン離したらフラグ戻す（短押し用）
            {
                first_step_flag = true;
                currentTime = 0f;
            }
            ////歩行動作（短押しのみ対応版）
            //if (Input.GetKeyDown(KeyCode.Space))
            //{
            //    moveForward_D();
            //}
            //END 1.///////////////////////////////////////////////////////////////////////////////////////
        }

        if (f2_flag == true)
        {
            //2. ジャイロセンサの動作確認 （yaw+pitch，デバイス左右）///////////////////////////////////////////
            yaw_angle = log_yaw;
            pitch_angle = log_pitch;
            //ローパスフィルタ
            yaw_angle = pre_yaw * filter_gain + yaw_angle * (1 - filter_gain);
            pre_yaw = yaw_angle;
            pitch_angle = pre_pitch * filter_gain + pitch_angle * (1 - filter_gain);
            pre_pitch = pitch_angle;
            //
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                rotation_angle -= rotation_speed;
            }

            if (Input.GetKey(KeyCode.RightArrow))
            {
                rotation_angle += rotation_speed;
            }
            newAngle.y = yaw_angle - initial_angle + rotation_angle;     //首回転角＋初期調整角度＋旋回角度
            newAngle.z = 0;                                              //首回転角roll初期化
            newAngle.x = pitch_angle + initial_angle_pitch;              //首回転角pitch
            VReye.gameObject.transform.localEulerAngles = newAngle;
            //歩行動作（長押し・短押し対応版）
            if (Input.GetKey(KeyCode.Space))
            {
                currentTime += Time.deltaTime;  //長押しの時間カウント
                if (first_step_flag == true)    //最初に押した瞬間は一歩進む（短押し用）
                {
                    moveForward_D();
                    first_step_flag = false;
                }
                else
                {
                    if (currentTime > span)     //長押しで一定時間ごとに前進
                    {
                        moveForward_D();
                        currentTime = 0f;
                    }
                }
            }
            if (Input.GetKeyUp(KeyCode.Space))  //ボタン離したらフラグ戻す（短押し用）
            {
                first_step_flag = true;
                currentTime = 0f;
            }
            //END 2.//////////////////////////////////////////////////////////////////////////////////////////
        }

        if (f3_flag == true)
        {
            //3. 記録用（局所回転なし，デバイス左右） /////////////////////////////////
            log_key = 0;    //キー押さない：0
            //歩行動作（長押し・短押し対応版）
            if (Input.GetKey(KeyCode.Space))
            {
                log_key = 1;    //スペースキー：1
                currentTime += Time.deltaTime;  //長押しの時間カウント
                if (first_step_flag == true)    //最初に押した瞬間は一歩進む（短押し用）
                {
                    moveForward_D();
                    first_step_flag = false;
                }
                else
                {
                    if (currentTime > span)     //長押しで一定時間ごとに前進
                    {
                        moveForward_D();
                        currentTime = 0f;
                    }
                }
            }
            if (Input.GetKeyUp(KeyCode.Space))  //ボタン離したらフラグ戻す（短押し用）
            {
                first_step_flag = true;
                currentTime = 0f;
            }
            //
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                log_key = 2;    //左キー：2
                rotation_angle -= rotation_speed;
            }

            if (Input.GetKey(KeyCode.RightArrow))
            {
                log_key = 3;    //右キー：3
                rotation_angle += rotation_speed;
            }
            newAngle.x = 0;      //首回転角pitch初期化
            newAngle.z = 0;      //首回転角roll初期化
            newAngle.y = -initial_angle + rotation_angle;     //初期調整角度＋旋回角度
            VReye.gameObject.transform.localEulerAngles = newAngle;
            //Debug.Log("log_key: " + log_key);
            
            // ファイル書き出し
            string[] str = { log_key.ToString(), log_yaw.ToString(), log_pitch.ToString(), log_force.ToString() };
            string str2 = string.Join(",", str);
            //書き込み
            File.AppendAllText(filePath, str2 + "\n");

            //END 3./////////////////////////////////////////////////////////////////////////////////////////
        }

        if (f4_flag == true)
        {
            //4. 記録用（局所回転あり，デバイス左右）///////////////////////////////////////////
            log_key = 0;    //キー押さない：0
            yaw_angle = log_yaw;
            pitch_angle = log_pitch;
            //ローパスフィルタ
            yaw_angle = pre_yaw * filter_gain + yaw_angle * (1 - filter_gain);
            pre_yaw = yaw_angle;
            pitch_angle = pre_pitch * filter_gain + pitch_angle * (1 - filter_gain);
            pre_pitch = pitch_angle;
            //歩行動作（長押し・短押し対応版）
            if (Input.GetKey(KeyCode.Space))
            {
                log_key = 1;    //スペースキー：1
                currentTime += Time.deltaTime;  //長押しの時間カウント
                if (first_step_flag == true)    //最初に押した瞬間は一歩進む（短押し用）
                {
                    moveForward_D();
                    first_step_flag = false;
                }
                else
                {
                    if (currentTime > span)     //長押しで一定時間ごとに前進
                    {
                        moveForward_D();
                        currentTime = 0f;
                    }
                }
            }
            if (Input.GetKeyUp(KeyCode.Space))  //ボタン離したらフラグ戻す（短押し用）
            {
                first_step_flag = true;
                currentTime = 0f;
            }
            //
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                log_key = 2;    //左キー：2
                rotation_angle -= rotation_speed;
            }

            if (Input.GetKey(KeyCode.RightArrow))
            {
                log_key = 3;    //右キー：3
                rotation_angle += rotation_speed;
            }
            newAngle.y = yaw_angle - initial_angle + rotation_angle;     //首回転角＋初期調整角度＋旋回角度
            newAngle.z = 0;                                              //首回転角roll初期化
            newAngle.x = pitch_angle + initial_angle_pitch;              //首回転角pitch
            VReye.gameObject.transform.localEulerAngles = newAngle;

            // ファイル書き出し
            string[] str = { log_key.ToString(), log_yaw.ToString(), log_pitch.ToString(), log_force.ToString() };
            string str2 = string.Join(",", str);
            //書き込み
            File.AppendAllText(filePath2, str2 + "\n");

            //END 4./////////////////////////////////////////////////////////////////////////////////////////
        }

        if (f5_flag == true)
        {
            //5. 連続変速////////////////////

            //END 5./////////////////////////////////////////////////////////////////////////////////////
        }

        if (f6_flag == true)
        {
            //6. /////////////////////////////

            //END 3.////////////////////////////////////////////////////////////////////////////
        }


        if (f7_flag == true)
        {
            //7. //////////////////////////

            //END 3.////////////////////////////////////////////////////////////////////////////
        }
    }

    void FlagDown()
    {
        f1_flag = f2_flag = f3_flag = f4_flag = f5_flag = f6_flag = f7_flag = false;
    }

    //離散移動の関数/////////////////////////////////////////////////////////////
    private void moveForward_D()
    {
        VReye.transform.position = VReye.transform.position + new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z) * Time.deltaTime * stride;
    }

    private void moveBackward_D()
    {
        VReye.transform.position = VReye.transform.position + new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z) * Time.deltaTime * stride * (-1);
    }

}

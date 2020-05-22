using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class FaceController_airpress : MonoBehaviour
{

    public GameObject Data_Receiver;
    public GameObject VReye;
    public Camera mainCamera;
    //各種パラメータ
    public float t_force = 50.0f;          //回転閾値
    public float t_force_ud = 50.0f;       //歩行閾値
    public float t_force_ud_2 = 25.0f;     //歩行閾値（長押し解除）
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
    //ローパスフィルタ
    private float filter_gain = 0.98f;      //default: 0.75
    private float pre_yaw = 0.0f;
    private float pre_pitch = 0.0f;
    private float pre_force = 0.0f;
    private float mod_force = 0.0f;
    //平均値フィルタ
    private const int AVE_NUM = 30;               //平均するデータの個数
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
        //角度の初期化
        newAngle.y = initial_angle;
    }

    void Update()
    {
        //シリアル通信から空気圧の値を取得
        force = Data_Receiver.GetComponent<receive_data_airpress>().f_val;
        force_ud = Data_Receiver.GetComponent<receive_data_airpress>().f_val_ud;

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
            //1. 閾値でYaw一定速度旋回 /////////////////////////////////////////////////////////////////////////
            //回転制御部
            if (Math.Abs(force) > t_force)
            {
                if (force > 0)
                {
                    rotation_angle -= rotation_speed;
                }
                else
                {
                    rotation_angle += rotation_speed;
                }
            }
            newAngle.y = yaw_angle - initial_angle + rotation_angle;     //首回転角＋初期調整角度＋旋回角度
            newAngle.z = 0;                                              //首回転角roll初期化
            newAngle.x = pitch_angle + initial_angle_pitch;              //首回転角pitch
            VReye.gameObject.transform.localEulerAngles = newAngle;
            //歩行制御部（長押し・短押し対応版）
            if (force_ud > t_force_ud)
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
            //2. 局所回転＋閾値で一定速度旋回 ///////////////////////////////////////////
            //局所回転角の取得
            //ここの「圧力→角度」変換の関数を考える
            if (force > 0)
            {
                force_to_angle = 0.007f * force * force;     //y=ax^2, a=0.007
            }
            else
            {
                force_to_angle = -0.007f * force * force;     //y=ax^2, a=0.007
            }

            //平均値フィルタ
            for (int i = AVE_NUM - 1; i > 0; i--)
            {
                list_force[i] = list_force[i - 1];
            }
            list_force[0] = force_to_angle;
            for (int i = 0; i < AVE_NUM; i++)
            {
                ave_force += list_force[i];
            }
            ave_force = (float)(ave_force / AVE_NUM);
            //ローパスフィルタ
            mod_force = pre_force * filter_gain + ave_force * (1 - filter_gain);
            pre_force = mod_force;
            //ここで角度代入
            yaw_angle = -mod_force;
            //Debug.Log(yaw_angle);
            //頭部制御部
            if (Math.Abs(force) > 80)
            {
                if (force > 0)
                {
                    rotation_angle -= rotation_speed;
                }
                else
                {
                    rotation_angle += rotation_speed;
                }
            }
            newAngle.y = yaw_angle - initial_angle + rotation_angle;     //首回転角＋初期調整角度＋旋回角度
            newAngle.z = 0;                                              //首回転角roll初期化
            newAngle.x = pitch_angle + initial_angle_pitch;              //首回転角pitch
            VReye.gameObject.transform.localEulerAngles = newAngle;
            //歩行制御部（長押し・短押し対応版）//顎引く版
            if (force_ud < -t_force_ud)
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
            if (force_ud > -t_force_ud)  //ボタン離したらフラグ戻す（短押し用）
            {
                first_step_flag = true;
                currentTime = 0f;
            }
            //END 2.//////////////////////////////////////////////////////////////////////////////////////////
        }

        if (f3_flag == true)
        {
            //3. y=ax+b /////////////////////////////////
            //局所回転角の取得
            //ここの「圧力→角度」変換の関数を考える
            if (Math.Abs(force) < 15)
            {
                force_to_angle = 0.0f;
            }
            else
            {
                force_to_angle = 0.69f * force - 3.46f;       //y=ax+b
            }

            //平均値フィルタ
            for (int i = AVE_NUM - 1; i > 0; i--)
            {
                list_force[i] = list_force[i - 1];
            }
            list_force[0] = force_to_angle;
            for (int i = 0; i < AVE_NUM; i++)
            {
                ave_force += list_force[i];
            }
            ave_force = (float)(ave_force / AVE_NUM);
            //ローパスフィルタ
            mod_force = pre_force * filter_gain + ave_force * (1 - filter_gain);
            pre_force = mod_force;
            //ここで角度代入
            yaw_angle = -mod_force;
            //Debug.Log(yaw_angle);
            //頭部制御部
            if (Math.Abs(force) > 80)
            {
                if (force > 0)
                {
                    rotation_angle -= rotation_speed;
                }
                else
                {
                    rotation_angle += rotation_speed;
                }
            }
            newAngle.y = yaw_angle - initial_angle + rotation_angle;     //首回転角＋初期調整角度＋旋回角度
            newAngle.z = 0;                                              //首回転角roll初期化
            newAngle.x = pitch_angle + initial_angle_pitch;              //首回転角pitch
            VReye.gameObject.transform.localEulerAngles = newAngle;
            //歩行制御部（長押し・短押し対応版）
            if (force_ud > t_force_ud)
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
            if (force_ud < t_force_ud)  //ボタン離したらフラグ戻す（短押し用）
            {
                first_step_flag = true;
                currentTime = 0f;
            }
            //END 3./////////////////////////////////////////////////////////////////////////////////////////
        }

        if (f4_flag == true)
        {
            //4.///////////////////////////////////////////

            //END 4./////////////////////////////////////////////////////////////////////////////////////////
        }

        if (f5_flag == true)
        {
            //5. ////////////////////

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

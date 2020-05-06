using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class FaceController_SVM : MonoBehaviour
{

    public GameObject UDP;
    public GameObject VReye;
    public Camera mainCamera;
    //歩行
    public float moveSpeed = 0.01f;
    public float moveAngleX = 20.0f;
    public int stride = 50;
    private bool first_step_flag = true;    //短押し用フラグ
    public float span = 1.0f;               //長押しの時間間隔
    private float currentTime = 0f;
    //
    private Vector3 lastMousePosition;
    private Vector3 newAngle = new Vector3(0, 0, 0);
    private float yaw_angle;
    private float pitch_angle;
    private float roll_angle;
    private int svm_label;
    //ローパスフィルタ
    private float filter_gain = 0.75f;      //default: 0.75
    private float pre_yaw = 0.0f;
    private float pre_pitch = 0.0f;
    private float pre_roll = 0.0f;
    //
    private float initial_angle = 0.0f;             //初期角度
    private float initial_angle_pitch = 0.0f;       //初期角度pitch
    private float move_angle = 20.0f;               //追従角度（局所回転を行う角度）
    private float move_angle_pitch = 10.0f;         //追従角度（局所回転を行う角度）
    private float rotation_speed = 1.0f;            //旋回スピード
    private float rotation_angle = 0.0f;            //旋回角度
    private float rotation_angle_pitch = 0.0f;      //旋回角度
    private float temp_yaw_angle = 0.0f;            //一時格納する角度
    private float max_pitch = 20;                   //キーボード操作pitch角最大
    private float min_pitch = -20;                  //キーボード操作pitch角最小
    //ゲイン
    private float rotation_gain = 1.5f;     //局所回転ゲイン
    private float pitch_gain = 1.0f;        //pitchゲイン
    //
    private float temp_angle = 0.0f;
    private float return_angle = 10.0f;
    private float return_face_angle = 0.0f;
    private bool return_face = false;
    private bool return_face_m = false;
    private bool return_face_p = false;
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
        //シリアル通信から頭部角度取得
        yaw_angle = UDP.GetComponent<UDP>().yaw_val;
        pitch_angle = UDP.GetComponent<UDP>().pitch_val;
        svm_label = UDP.GetComponent<UDP>().SVM_label;
        //Debug.Log(yaw_angle);

        //ローパスフィルタ
        yaw_angle = pre_yaw * filter_gain + yaw_angle * (1 - filter_gain);
        pre_yaw = yaw_angle;
        pitch_angle = pre_pitch * filter_gain + pitch_angle * (1 - filter_gain);
        pre_pitch = pitch_angle;
        roll_angle = pre_roll * filter_gain + roll_angle * (1 - filter_gain);
        pre_roll = roll_angle;


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
            //1. SVM予測使用　ラベル0：局所回転Yaw＋Pitch　/　ラベル1：大域回転  /////////////////////////////
            //回転制御部　ラベル1のとき頭部方向に応じて大域回転量++
            if (svm_label == 1)         
            {
                if (yaw_angle < 0)
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
            //END 1.///////////////////////////////////////////////////////////////////////////////////////
        }

        if (f2_flag == true)
        {
            //2. 局所回転：なし / 大域回転：頭部制御（Yaw＋Pitch）///////////////////////////////////////////
            //頭部制御部Yaw
            if (Math.Abs(yaw_angle) > Math.Abs(move_angle))
            {
                if (yaw_angle < 0)
                {
                    rotation_angle -= rotation_speed;
                }
                else
                {
                    rotation_angle += rotation_speed;
                }
            }
            //頭部制御部Pitch（角度制限あり）
            if (Math.Abs(pitch_angle) > Math.Abs(move_angle_pitch))
            {
                if (pitch_angle < 0)
                {
                    if (rotation_angle_pitch > min_pitch)
                    {
                        rotation_angle_pitch -= rotation_speed;
                    }
                }
                else
                {
                    if (rotation_angle_pitch < max_pitch)
                    {
                        rotation_angle_pitch += rotation_speed;
                    }
                }
            }
            newAngle.x = rotation_angle_pitch;                //pitch
            newAngle.z = 0;      //首回転角roll初期化
            newAngle.y = -initial_angle + rotation_angle;     //初期調整角度＋旋回角度
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
            //3. 局所回転：あり（Yawのみ） / 大域回転：デバイス（Yaw＋Pitch）/////////////////////////////////
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                rotation_angle -= rotation_speed;
            }

            if (Input.GetKey(KeyCode.RightArrow))
            {
                rotation_angle += rotation_speed;
            }

            if (Input.GetKey(KeyCode.UpArrow))
            {
                if (rotation_angle_pitch > min_pitch)
                {
                    rotation_angle_pitch -= rotation_speed;
                }
            }

            if (Input.GetKey(KeyCode.DownArrow))
            {
                if (rotation_angle_pitch < max_pitch)
                {
                    rotation_angle_pitch += rotation_speed;
                }
            }
            newAngle.x = rotation_angle_pitch;                           //キーボードでpitch角操作
            //newAngle.x = 0;      //首回転角pitch初期化
            newAngle.z = 0;      //首回転角roll初期化
            newAngle.y = yaw_angle - initial_angle + rotation_angle;     //首回転角＋初期調整角度＋旋回角度
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
            //END 3./////////////////////////////////////////////////////////////////////////////////////////
        }

        if (f4_flag == true)
        {
            //4. 局所回転：なし / 大域回転：デバイス（Yaw＋Pitch）///////////////////////////////////////////
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                rotation_angle -= rotation_speed;
            }

            if (Input.GetKey(KeyCode.RightArrow))
            {
                rotation_angle += rotation_speed;
            }

            if (Input.GetKey(KeyCode.UpArrow))
            {
                if (rotation_angle_pitch > min_pitch)
                {
                    rotation_angle_pitch -= rotation_speed;
                }
            }

            if (Input.GetKey(KeyCode.DownArrow))
            {
                if (rotation_angle_pitch < max_pitch)
                {
                    rotation_angle_pitch += rotation_speed;
                }
            }
            newAngle.x = rotation_angle_pitch;                           //キーボードでpitch角操作
            //newAngle.x = 0;      //首回転角pitch初期化
            newAngle.z = 0;      //首回転角roll初期化
            newAngle.y = -initial_angle + rotation_angle;     //初期調整角度＋旋回角度
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
            //END 4./////////////////////////////////////////////////////////////////////////////////////////
        }

        if (f5_flag == true)
        {
            //5. 局所回転：あり（Yaw＋Pitch，ゲイン付き） / 大域回転：デバイス（Yawのみ）////////////////////
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                rotation_angle -= rotation_speed;
            }

            if (Input.GetKey(KeyCode.RightArrow))
            {
                rotation_angle += rotation_speed;
            }
            newAngle.y = yaw_angle * rotation_gain - initial_angle + rotation_angle;    //（首回転角×ゲイン）＋初期調整角度＋旋回角度
            newAngle.z = 0;                                                             //首回転角roll初期化
            newAngle.x = pitch_angle * rotation_gain + initial_angle_pitch;             //首回転角pitch×ゲイン　＋初期調整角度
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
            //END 5./////////////////////////////////////////////////////////////////////////////////////
        }

        if (f6_flag == true)
        {
            //6. 局所回転：あり（Yaw＋Pitch） / 大域回転：デバイス（Yawのみ）/////////////////////////////
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
            //END 3.////////////////////////////////////////////////////////////////////////////
        }


        if (f7_flag == true)
        {
            //7. 局所回転：あり（Yaw＋Pitch） / 大域回転：頭部＋デバイス（Yawのみ）//////////////////////////
            if (Math.Abs(yaw_angle) > Math.Abs(move_angle) && Input.GetKey(KeyCode.Space))  //頭部回転時にスペースキー押すと旋回
            {
                currentTime = 0f;
                if (yaw_angle < 0)
                {
                    rotation_angle -= rotation_speed;
                }
                else
                {
                    rotation_angle += rotation_speed;
                }
            }
            else if (Math.Abs(yaw_angle) <= Math.Abs(move_angle) && Input.GetKey(KeyCode.Space))
            {
                //歩行動作（長押し・短押し対応版）
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
            newAngle.y = yaw_angle - initial_angle + rotation_angle;     //首回転角＋初期調整角度＋旋回角度
            newAngle.z = 0;                                              //首回転角roll初期化
            newAngle.x = pitch_angle + initial_angle_pitch;              //首回転角pitch
            VReye.gameObject.transform.localEulerAngles = newAngle;
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

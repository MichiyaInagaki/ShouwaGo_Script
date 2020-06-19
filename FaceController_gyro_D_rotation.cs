using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class FaceController_gyro_D_rotation : MonoBehaviour
{

    public GameObject Serial_Data;
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
    private float rotation_speed = 0.5f;            //旋回スピード default:0.5f
    private float rotation_speed2 = 2.0f;           //旋回スピード@なめらか回転
    private float rotation_angle = 0.0f;            //旋回角度
    private float rotation_angle_pitch = 0.0f;      //旋回角度
    private float temp_yaw_angle = 0.0f;            //一時格納する角度
    private float temp_pitch_angle = 0.0f;          //一時格納する角度
    private float max_pitch = 20.0f;                //キーボード操作pitch角最大
    private float min_pitch = -20.0f;               //キーボード操作pitch角最小
    //
    private float D_rotation = 15.0f;               //離散回転の刻み
    private bool D_rotation_flag_L = false;         //離散等速回転用
    private bool D_rotation_flag_R = false;         //離散等速回転用
    private bool end_rotation_flag = true;
    private Vector3 rotAngle;                       //回転後の角度
    private const int ROT_MAX = 360;                //最大回転角
    private float delta_yaw = 0.0f;                 //回転前後の位置ズレ補正
    private float delta_pitch = 0.0f;
    private bool headlock_flag = false;
    //
    private bool fixed_L_flag = false;              //固定フレームレートで回転させるためのフラグ
    private bool fixed_R_flag = false;
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
    private bool f8_flag = false;
    private bool f9_flag = false;
    private bool f10_flag = false;
    private bool f11_flag = false;
    private bool f12_flag = false;

    void Start()
    {
        //角度の初期化
        newAngle.y = initial_angle;
    }

    void Update()
    {
        //シリアル通信から頭部角度取得
        yaw_angle = Serial_Data.GetComponent<receive_data>().yaw_val;
        pitch_angle = Serial_Data.GetComponent<receive_data>().pitch_val;
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
        if (Input.GetKey(KeyCode.F8))
        {
            FlagDown();
            f8_flag = true;
        }
        if (Input.GetKey(KeyCode.F9))
        {
            FlagDown();
            f9_flag = true;
        }
        if (Input.GetKey(KeyCode.F10))
        {
            FlagDown();
            f10_flag = true;
        }
        if (Input.GetKey(KeyCode.F11))
        {
            FlagDown();
            f11_flag = true;
        }
        if (Input.GetKey(KeyCode.F12))
        {
            FlagDown();
            f12_flag = true;
        }


        //各種処理
        if (f1_flag == true)
        {
            //1-1. 局所回転：あり（Yaw＋Pitch） / 大域回転：デバイス（Yawのみ）////////////////////////////
            ////回転時もトラッキング有効
            //if (Input.GetKey(KeyCode.LeftArrow))
            //{
            //    fixed_L_flag = true;
            //    //rotation_angle -= rotation_speed;
            //}
            //if (Input.GetKeyUp(KeyCode.LeftArrow))
            //{
            //    fixed_L_flag = false;
            //}
            //if (Input.GetKey(KeyCode.RightArrow))
            //{
            //    fixed_R_flag = true;
            //    //rotation_angle += rotation_speed;
            //}
            //if (Input.GetKeyUp(KeyCode.RightArrow))
            //{
            //    fixed_R_flag = false;
            //}
            //newAngle.y = yaw_angle - initial_angle + rotation_angle;     //首回転角＋初期調整角度＋旋回角度
            //newAngle.z = 0;                                              //首回転角roll初期化
            //newAngle.x = pitch_angle + initial_angle_pitch;              //首回転角pitch
            //VReye.gameObject.transform.localEulerAngles = newAngle;


            //回転時はトラッキング無効-----------------------------
            //キーを押した瞬間の角度を取得
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                temp_yaw_angle = yaw_angle;
                temp_pitch_angle = pitch_angle;
            }
            //FixedUpdeteでの回転処理開始
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                fixed_L_flag = true;
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                fixed_R_flag = true;
            }
            //FixedUpdeteでの回転処理終了
            if (Input.GetKeyUp(KeyCode.LeftArrow))
            {
                delta_yaw += yaw_angle - temp_yaw_angle;
                delta_pitch += pitch_angle - temp_pitch_angle;
                fixed_L_flag = false;
            }
            if (Input.GetKeyUp(KeyCode.RightArrow))
            {
                delta_yaw += yaw_angle - temp_yaw_angle;
                delta_pitch += pitch_angle - temp_pitch_angle;
                fixed_R_flag = false;
            }
            //回転していないときはトラッキングあり
            if (fixed_L_flag == false && fixed_R_flag == false)
            {
                newAngle.y = yaw_angle - initial_angle + rotation_angle - delta_yaw;        //首回転角＋初期調整角度＋旋回角度
                newAngle.z = 0;                                                             //首回転角roll初期化
                newAngle.x = pitch_angle + initial_angle_pitch - delta_pitch;               //首回転角pitch
                VReye.gameObject.transform.localEulerAngles = newAngle;
            }
            else
            {
                //回転時はトラッキング無効
                newAngle.y = temp_yaw_angle - initial_angle + rotation_angle - delta_yaw;     //首回転角＋初期調整角度＋旋回角度
                newAngle.z = 0;                                                               //首回転角roll初期化
                newAngle.x = temp_pitch_angle + initial_angle_pitch - delta_pitch;            //首回転角pitch
                VReye.gameObject.transform.localEulerAngles = newAngle;
            }
            //

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
            //END 1-1.///////////////////////////////////////////////////////////////////////////////////////
        }

        if (f2_flag == true)
        {
            //2. 局所回転：なし / 大域回転：デバイス離散回転///////////////////////////////////////////
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                rotation_angle -= D_rotation;
            }

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                rotation_angle += D_rotation;
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
            //END 2.//////////////////////////////////////////////////////////////////////////////////////////
        }

        if (f3_flag == true)
        {
            //3. 局所回転：あり（Yaw + Pitch） / 大域回転：デバイス離散回転/////////////////////////////////
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                rotation_angle -= D_rotation;
            }

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                rotation_angle += D_rotation;
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
            //END 3./////////////////////////////////////////////////////////////////////////////////////////
        }

        if (f4_flag == true)
        {
            //4. 局所回転：なし / 大域回転：デバイス離散回転_等速 ///////////////////////////////////////////
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                temp_angle = rotation_angle;
                D_rotation_flag_R = false;
                D_rotation_flag_L = true;
                fixed_R_flag = false;
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                temp_angle = rotation_angle;
                D_rotation_flag_L = false;
                D_rotation_flag_R = true;
                fixed_L_flag = false;
            }
            //閾値D_rotationまで連続回転
            if (D_rotation_flag_L == true)
            {
                fixed_L_flag = true;
                //rotation_angle -= rotation_speed;
                if (Math.Abs(temp_angle - rotation_angle) > D_rotation)
                {
                    D_rotation_flag_L = false;
                    fixed_L_flag = false;
                }
            }
            if (D_rotation_flag_R == true)
            {
                fixed_R_flag = true;
                //rotation_angle += rotation_speed;
                if (Math.Abs(temp_angle - rotation_angle) > D_rotation)
                {
                    D_rotation_flag_R = false;
                    fixed_R_flag = false;
                }
            }
            //
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
            //5. 局所回転：あり（Yaw + Pitch） / 大域回転：デバイス離散回転_等速 ////////////////////
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                temp_angle = rotation_angle;
                temp_yaw_angle = yaw_angle;
                temp_pitch_angle = pitch_angle;
                D_rotation_flag_R = false;
                D_rotation_flag_L = true;
                fixed_R_flag = false;
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                temp_angle = rotation_angle;
                temp_yaw_angle = yaw_angle;
                temp_pitch_angle = pitch_angle;
                D_rotation_flag_L = false;
                D_rotation_flag_R = true;
                fixed_L_flag = false;
            }
            //閾値：D_rotationまで連続回転
            if (D_rotation_flag_L == true)
            {
                fixed_L_flag = true;
                //rotation_angle -= rotation_speed;
                if (Math.Abs(temp_angle - rotation_angle) > D_rotation)
                {
                    D_rotation_flag_L = false;
                    fixed_L_flag = false;
                    delta_yaw += yaw_angle - temp_yaw_angle;         //回転前後の位置ズレ補正用
                    delta_pitch += pitch_angle - temp_pitch_angle;
                }
            }
            if (D_rotation_flag_R == true)
            {
                fixed_R_flag = true;
                //rotation_angle += rotation_speed;
                if (Math.Abs(temp_angle - rotation_angle) > D_rotation)
                {
                    D_rotation_flag_R = false;
                    fixed_R_flag = false;
                    delta_yaw += yaw_angle - temp_yaw_angle;         //回転前後の位置ズレ補正用
                    delta_pitch += pitch_angle - temp_pitch_angle;
                }
            }
            //
            //旋回していないときのみトラッキング有効
            if (D_rotation_flag_L == false && D_rotation_flag_R == false)
            {
                newAngle.y = yaw_angle - initial_angle + rotation_angle - delta_yaw;     //首回転角＋初期調整角度＋旋回角度＋回転前後の位置ズレ補正
                newAngle.z = 0;                                                          //首回転角roll初期化
                newAngle.x = pitch_angle + initial_angle_pitch - delta_pitch;            //首回転角pitch
                VReye.gameObject.transform.localEulerAngles = newAngle;
            }
            else
            {
                newAngle.y = temp_yaw_angle - initial_angle + rotation_angle - delta_yaw;       //首回転角＋初期調整角度＋旋回角度
                newAngle.z = 0;                                                                 //首回転角roll初期化
                newAngle.x = temp_pitch_angle + initial_angle_pitch - delta_pitch;              //首回転角pitch
                VReye.gameObject.transform.localEulerAngles = newAngle;
            }



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
            //6. 局所回転：なし / 大域回転：デバイス離散回転_加速 /////////////////////////////
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                rotAngle = new Vector3(VReye.gameObject.transform.localEulerAngles.x, VReye.gameObject.transform.localEulerAngles.y - D_rotation, VReye.gameObject.transform.localEulerAngles.z);   //回転後の角度
                rotAngle = new Vector3(Mathf.Repeat(rotAngle.x, ROT_MAX), Mathf.Repeat(rotAngle.y, ROT_MAX), Mathf.Repeat(rotAngle.z, ROT_MAX));    //回転角度を0°～360°間でループさせる
                if (end_rotation_flag == true)     //回転が完全に完了しているときのみtempに角度を入れる
                {
                    temp_angle = VReye.gameObject.transform.localEulerAngles.y;
                }
                D_rotation_flag_R = false;
                D_rotation_flag_L = true;
                end_rotation_flag = false;
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                rotAngle = new Vector3(VReye.gameObject.transform.localEulerAngles.x, VReye.gameObject.transform.localEulerAngles.y + D_rotation, VReye.gameObject.transform.localEulerAngles.z);   //回転後の角度
                rotAngle = new Vector3(Mathf.Repeat(rotAngle.x, ROT_MAX), Mathf.Repeat(rotAngle.y, ROT_MAX), Mathf.Repeat(rotAngle.z, ROT_MAX));    //回転角度を0°～360°間でループさせる
                if (end_rotation_flag == true)     //回転が完全に完了しているときのみtempに角度を入れる
                {
                    temp_angle = VReye.gameObject.transform.localEulerAngles.y;
                }
                D_rotation_flag_L = false;
                D_rotation_flag_R = true;
                end_rotation_flag = false;
            }
            //なめらかな回転
            if (D_rotation_flag_L == true)
            {
                VReye.gameObject.transform.rotation = Quaternion.Slerp(VReye.gameObject.transform.rotation, Quaternion.Euler(rotAngle), rotation_speed2 * Time.deltaTime);
                //終了検知
                //Debug.Log("VR anlge" + VReye.gameObject.transform.localEulerAngles);
                //Debug.Log("rot anlge" + rotAngle);
                //Debug.Log("mod VR anlge" + Mathf.Round(VReye.gameObject.transform.eulerAngles.y));
                //Debug.Log("mod rot anlge" + Mathf.Round(rotAngle.y));
                //if (Mathf.DeltaAngle(Mathf.Round(VReye.gameObject.transform.eulerAngles.y), Mathf.Round(rotAngle.y)) == 0)
                if (Math.Abs(Mathf.Round(VReye.gameObject.transform.eulerAngles.y) - Mathf.Round(rotAngle.y)) == 1)     //ほぼ収束（差が1度以下）すれば終了
                {
                    rotation_angle -= Math.Abs(mod_deg(VReye.gameObject.transform.eulerAngles.y - temp_angle));
                    D_rotation_flag_L = false;
                    end_rotation_flag = true;
                }
            }
            if (D_rotation_flag_R == true)
            {
                VReye.gameObject.transform.rotation = Quaternion.Slerp(VReye.gameObject.transform.rotation, Quaternion.Euler(rotAngle), rotation_speed2 * Time.deltaTime);
                //終了検知
                //Debug.Log("VR anlge" + VReye.gameObject.transform.localEulerAngles);
                //Debug.Log("rot anlge" + rotAngle);
                //Debug.Log("mod VR anlge" + Mathf.Round(VReye.gameObject.transform.eulerAngles.y));
                //Debug.Log("mod rot anlge" + Mathf.Round(rotAngle.y));
                //if (Mathf.DeltaAngle(Mathf.Round(VReye.gameObject.transform.eulerAngles.y), Mathf.Round(rotAngle.y)) == 0)
                if (Math.Abs(Mathf.Round(VReye.gameObject.transform.eulerAngles.y) - Mathf.Round(rotAngle.y)) == 1)
                {
                    rotation_angle += Math.Abs(mod_deg(VReye.gameObject.transform.eulerAngles.y - temp_angle));
                    D_rotation_flag_R = false;
                    end_rotation_flag = true;
                }
            }
            //旋回していないときのみ動作
            if (D_rotation_flag_L == false && D_rotation_flag_R == false)
            {
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
                //
                newAngle.x = rotation_angle_pitch;                //キーボードでpitch角操作
                newAngle.z = 0;      //首回転角roll初期化
                newAngle.y = -initial_angle + rotation_angle;     //初期調整角度＋旋回角度
                VReye.gameObject.transform.localEulerAngles = newAngle;
            }

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
            //END 6.////////////////////////////////////////////////////////////////////////////
        }

        if (f7_flag == true)
        {
            //7. 局所回転：あり（Yaw + Pitch） / 大域回転：デバイス離散回転_加速 //////////////////////////
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                rotAngle = new Vector3(VReye.gameObject.transform.localEulerAngles.x, VReye.gameObject.transform.localEulerAngles.y - D_rotation, VReye.gameObject.transform.localEulerAngles.z);   //回転後の角度
                rotAngle = new Vector3(Mathf.Repeat(rotAngle.x, ROT_MAX), Mathf.Repeat(rotAngle.y, ROT_MAX), Mathf.Repeat(rotAngle.z, ROT_MAX));    //回転角度を0°～360°間でループさせる
                if (D_rotation_flag_L == false)     //回転が完全に完了しているときのみtempに角度を入れる
                {
                    temp_angle = VReye.gameObject.transform.localEulerAngles.y;
                    temp_yaw_angle = yaw_angle;
                    temp_pitch_angle = pitch_angle;
                }
                D_rotation_flag_R = false;
                D_rotation_flag_L = true;
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                rotAngle = new Vector3(VReye.gameObject.transform.localEulerAngles.x, VReye.gameObject.transform.localEulerAngles.y + D_rotation, VReye.gameObject.transform.localEulerAngles.z);   //回転後の角度
                rotAngle = new Vector3(Mathf.Repeat(rotAngle.x, ROT_MAX), Mathf.Repeat(rotAngle.y, ROT_MAX), Mathf.Repeat(rotAngle.z, ROT_MAX));    //回転角度を0°～360°間でループさせる
                if (D_rotation_flag_R == false)     //回転が完全に完了しているときのみtempに角度を入れる
                {
                    temp_angle = VReye.gameObject.transform.localEulerAngles.y;
                    temp_yaw_angle = yaw_angle;
                    temp_pitch_angle = pitch_angle;
                }
                D_rotation_flag_L = false;
                D_rotation_flag_R = true;
            }
            //なめらかな回転
            if (D_rotation_flag_L == true)
            {
                VReye.gameObject.transform.rotation = Quaternion.Slerp(VReye.gameObject.transform.rotation, Quaternion.Euler(rotAngle), rotation_speed2 * Time.deltaTime);
                if (Math.Abs(Mathf.Round(VReye.gameObject.transform.eulerAngles.y) - Mathf.Round(rotAngle.y)) == 1)     //ほぼ収束（差が1度以下）すれば終了
                {
                    rotation_angle -= Math.Abs(mod_deg(VReye.gameObject.transform.eulerAngles.y - temp_angle));
                    D_rotation_flag_L = false;
                    delta_yaw += (yaw_angle - temp_yaw_angle);         //回転前後の位置ズレ補正用
                    delta_pitch += (pitch_angle - temp_pitch_angle);
                }
            }
            if (D_rotation_flag_R == true)
            {
                VReye.gameObject.transform.rotation = Quaternion.Slerp(VReye.gameObject.transform.rotation, Quaternion.Euler(rotAngle), rotation_speed2 * Time.deltaTime);
                if (Math.Abs(Mathf.Round(VReye.gameObject.transform.eulerAngles.y) - Mathf.Round(rotAngle.y)) == 1)
                {
                    rotation_angle += Math.Abs(mod_deg(VReye.gameObject.transform.eulerAngles.y - temp_angle));
                    D_rotation_flag_R = false;
                    delta_yaw += (yaw_angle - temp_yaw_angle);         //回転前後の位置ズレ補正用
                    delta_pitch += (pitch_angle - temp_pitch_angle);
                }
            }
            //旋回していないときのみ動作
            if (D_rotation_flag_L == false && D_rotation_flag_R == false)
            {
                newAngle.y = yaw_angle - initial_angle + rotation_angle - delta_yaw;        //首回転角＋初期調整角度＋旋回角度＋回転前後の位置ズレ補正
                newAngle.z = 0;                                                             //首回転角roll初期化
                newAngle.x = pitch_angle + initial_angle_pitch - delta_pitch;               //首回転角pitch
                VReye.gameObject.transform.localEulerAngles = newAngle;
            }


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
            //END 7.////////////////////////////////////////////////////////////////////////////
        }

        if (f8_flag == true)
        {
            //8. 局所回転：なし / ヘッドロック ////////////////////////////////////////////
            //連続ヘッドロック//////////////////////////////
            if (Input.GetKeyDown(KeyCode.Z))
            {
                headlock_flag = true;
                temp_yaw_angle = yaw_angle;
            }
            if (Input.GetKey(KeyCode.Z))
            {
                yaw_angle = - (yaw_angle - temp_yaw_angle);
            }
            if (Input.GetKeyUp(KeyCode.Z))
            {
                rotation_angle -= yaw_angle - temp_yaw_angle;
                headlock_flag = false;
            }
            //離散ヘッドロック//////////////////////////////////
            if (Input.GetKeyDown(KeyCode.X))
            {
                temp_yaw_angle = yaw_angle;
            }
            if (Input.GetKeyUp(KeyCode.X))
            {
                rotation_angle -= (yaw_angle - temp_yaw_angle);
            }
            ////////////////////////////////////////////////////

            //キーボードでpitch方向
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
            //ヘッドロックしてない＝無回転
            if (headlock_flag == false)
            {
                newAngle.x = rotation_angle_pitch;                           //キーボードでpitch角操作
                newAngle.z = 0;      //首回転角roll初期化
                newAngle.y = -initial_angle + rotation_angle;     //初期調整角度＋旋回角度
                VReye.gameObject.transform.localEulerAngles = newAngle;
            }
            else
            {
                //ヘッドロック中＝逆旋回
                newAngle.x = rotation_angle_pitch;                           //キーボードでpitch角操作
                newAngle.z = 0;      //首回転角roll初期化
                newAngle.y = -initial_angle + rotation_angle + yaw_angle;     //初期調整角度＋旋回角度
                VReye.gameObject.transform.localEulerAngles = newAngle;
            }

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
            //END 8. //////////////////////////////////////////////////////////////////////
        }

        if (f9_flag == true)
        {
            //9. 局所回転：あり（Yaw＋Pitch） / ヘッドロック ////////////////////////////

            //連続ヘッドロック//////////////////////////////
            if (Input.GetKeyDown(KeyCode.Z))
            {
                rotation_angle += yaw_angle * 2;
            }
            if (Input.GetKey(KeyCode.Z))
            {
                yaw_angle = -yaw_angle;
            }
            if (Input.GetKeyUp(KeyCode.Z))
            {
                rotation_angle -= yaw_angle * 2;
            }
            //離散ヘッドロック//////////////////////////////////
            if (Input.GetKeyDown(KeyCode.X))
            {
                temp_yaw_angle = yaw_angle;
                temp_pitch_angle = pitch_angle;
                headlock_flag = true;
            }
            if (Input.GetKeyUp(KeyCode.X))
            {
                //delta_yaw += yaw_angle - temp_yaw_angle;
                //delta_pitch += pitch_angle - temp_pitch_angle;
                rotation_angle -= (yaw_angle - temp_yaw_angle) * 2;
                headlock_flag = false;
            }
            ///////////////////////////////////////////////////

            //ヘッドロックしてないとき  
            if (headlock_flag == false)
            {
                newAngle.y = yaw_angle - initial_angle + rotation_angle;        //首回転角＋初期調整角度＋旋回角度
                newAngle.z = 0;                                                             //首回転角roll初期化
                newAngle.x = pitch_angle + initial_angle_pitch;               //首回転角pitch
                VReye.gameObject.transform.localEulerAngles = newAngle;
            }
            else
            {
                //ヘッドロックしてるとき固定
                newAngle.y = temp_yaw_angle - initial_angle + rotation_angle;     //首回転角＋初期調整角度＋旋回角度
                newAngle.z = 0;                                                               //首回転角roll初期化
                newAngle.x = temp_pitch_angle + initial_angle_pitch;            //首回転角pitch
                VReye.gameObject.transform.localEulerAngles = newAngle;
            }
            //

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
            //END 9.///////////////////////////////////////////////////////////////////////////////////////
        }

        if (f10_flag == true)
        {
            //10. 局所回転：なし / 大域回転：デバイス（Yaw + Pitch）////////////////////////////
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
            //END 10. //////////////////////////////////////////////////////////////////////
        }

        if (f11_flag == true)
        {
            //11. 局所回転：なし / ムーブ ////////////////////////////////////////////
            //連続ヘッドムーブ//////////////////////////////
            if (Input.GetKeyDown(KeyCode.Z))
            {
                headlock_flag = true;
                temp_yaw_angle = yaw_angle;
            }
            if (Input.GetKey(KeyCode.Z))
            {
                yaw_angle = (yaw_angle - temp_yaw_angle);
            }
            if (Input.GetKeyUp(KeyCode.Z))
            {
                rotation_angle += yaw_angle - temp_yaw_angle;
                headlock_flag = false;
            }
            //離散ヘッドロック//////////////////////////////////
            if (Input.GetKeyDown(KeyCode.X))
            {
                temp_yaw_angle = yaw_angle;
            }
            if (Input.GetKeyUp(KeyCode.X))
            {
                rotation_angle += (yaw_angle - temp_yaw_angle);
            }
            ////////////////////////////////////////////////////

            //キーボードでpitch方向
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
            //ヘッドムーブしてない＝無回転
            if (headlock_flag == false)
            {
                newAngle.x = rotation_angle_pitch;                           //キーボードでpitch角操作
                newAngle.z = 0;      //首回転角roll初期化
                newAngle.y = -initial_angle + rotation_angle;     //初期調整角度＋旋回角度
                VReye.gameObject.transform.localEulerAngles = newAngle;
            }
            else
            {
                //ヘッドムーブ中＝順旋回
                newAngle.x = rotation_angle_pitch;                           //キーボードでpitch角操作
                newAngle.z = 0;      //首回転角roll初期化
                newAngle.y = -initial_angle + rotation_angle + yaw_angle;     //初期調整角度＋旋回角度
                VReye.gameObject.transform.localEulerAngles = newAngle;
            }

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
            //END 11. //////////////////////////////////////////////////////////////////////
        }

    }

        //フレームレート依存の動作をここで行う
        void FixedUpdate()
    {
        if (fixed_L_flag == true)
        {
            rotation_angle -= rotation_speed;
        }

        if (fixed_R_flag == true)
        {
            rotation_angle += rotation_speed;
        }
    }

    void FlagDown()
    {
        f1_flag = f2_flag = f3_flag = f4_flag = f5_flag = f6_flag = f7_flag = f8_flag = f9_flag = f10_flag = f11_flag = f12_flag = false;
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

    //角度を180度以下で表現するための変換
    float mod_deg(float deg)
    {
        float mod_deg;
        if (deg >= 180)
        {
            mod_deg = deg - 360f;
        }
        else if (deg <= -180)
        {
            mod_deg = deg + 360;
        }
        else
        {
            mod_deg = deg;
        }
        return mod_deg;
    }
}

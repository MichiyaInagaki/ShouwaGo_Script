using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Oculus_Controller : MonoBehaviour
{
    //GameObject
    public GameObject VReye;
    public GameObject mainCamera;
    //
    public float moveAngleX = 20.0f;
    public float moveSpeed = 1.5f;
    public int stride = 15;
    private bool tap_flag = false;
    float yOffset;
    //マウスで視点回転用
    public Vector2 mouse_rotationSpeed = new Vector2(0.1f, 0.1f);
    public bool reverse;
    private Vector2 lastMousePosition;
    private Vector2 mouse_newAngle = new Vector2(0, 0);

    // Use this for initialization
    void Start()
    {
        yOffset = mainCamera.transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        //Oculusコントローラによる操作/////////////////////////////
        //トリガー引くと前進
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
        {
            moveForward_D();
        }
        //戻るボタンで後進
        if (OVRInput.GetDown(OVRInput.Button.Back))
        {
            moveBackward_D();
        }
        //END Oculusコントローラによる操作//////////////////////////

        //マウスで視点回転//////////////////////////////////////////
        if (Input.GetMouseButtonDown(0))
        {
            // カメラの角度を変数"newAngle"に格納
            mouse_newAngle = VReye.transform.localEulerAngles;
            // マウス座標を変数"lastMousePosition"に格納
            lastMousePosition = Input.mousePosition;
        }
        // 左ドラッグしている間
        else if (Input.GetMouseButton(0))
        {
            //カメラ回転方向の判定フラグが"true"の場合
            if (!reverse)
            {
                // Y軸の回転：マウスドラッグ方向に視点回転
                // マウスの水平移動値に変数"rotationSpeed"を掛ける
                //（クリック時の座標とマウス座標の現在値の差分値）
                mouse_newAngle.y -= (lastMousePosition.x - Input.mousePosition.x) * mouse_rotationSpeed.y;
                // X軸の回転：マウスドラッグ方向に視点回転
                // マウスの垂直移動値に変数"rotationSpeed"を掛ける
                //（クリック時の座標とマウス座標の現在値の差分値）
                //mouse_newAngle.x -= (Input.mousePosition.y - lastMousePosition.y) * mouse_rotationSpeed.x;
                // "newAngle"の角度をカメラ角度に格納
                VReye.transform.localEulerAngles = mouse_newAngle;
                //mainCamera.transform.localEulerAngles = mouse_newAngle;
                // マウス座標を変数"lastMousePosition"に格納
                lastMousePosition = Input.mousePosition;
            }
            // カメラ回転方向の判定フラグが"reverse"の場合
            else if (reverse)
            {
                // Y軸の回転：マウスドラッグと逆方向に視点回転
                mouse_newAngle.y -= (Input.mousePosition.x - lastMousePosition.x) * mouse_rotationSpeed.y;
                // X軸の回転：マウスドラッグと逆方向に視点回転
                //mouse_newAngle.x -= (lastMousePosition.y - Input.mousePosition.y) * mouse_rotationSpeed.x;
                // "newAngle"の角度をカメラ角度に格納
                VReye.transform.localEulerAngles = mouse_newAngle;
                //mainCamera.transform.localEulerAngles = mouse_newAngle;
                // マウス座標を変数"lastMousePosition"に格納
                lastMousePosition = Input.mousePosition;
            }
        }
        //END マウスで視点回転//////////////////////////////////////
    }

    //連続移動の関数/////////////////////////////////////////////////////////////
    //private void moveForward_C()
    //{
    //    VReye.transform.position = VReye.transform.position + new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z) * Time.deltaTime * moveSpeed;
    //}

    //private void moveBackward_C()
    //{
    //    VReye.transform.position = VReye.transform.position + new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z) * Time.deltaTime * moveSpeed * (-1);
    //}

    //離散移動の関数/////////////////////////////////////////////////////////////
    private void moveForward_D()
    {
        VReye.transform.position = VReye.transform.position + new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z) * Time.deltaTime * stride;
    }

    private void moveBackward_D()
    {
        VReye.transform.position = VReye.transform.position + new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z) * Time.deltaTime * stride * (-1);
    }

    // マウスドラッグ方向と視点回転方向を反転する処理
    public void DirectionChange()
    {
        // 判定フラグ変数"reverse"が"false"であれば
        if (!reverse)
        {
            // 判定フラグ変数"reverse"に"true"を代入
            reverse = true;
        }
        // でなければ（判定フラグ変数"reverse"が"true"であれば）
        else
        {
            // 判定フラグ変数"reverse"に"false"を代入
            reverse = false;
        }
    }
}

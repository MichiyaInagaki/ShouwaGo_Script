using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tobii.Gaming; //ライブラリの追加

public class GazeTargetTest : MonoBehaviour
{
    //注視情報
    private GazeAware gazeAware;

    void Start()
    {
        //注視情報の取得
        gazeAware = GetComponent<GazeAware>();
    }

    void Update()
    {
        //オブジェクトを注視していたらTrue
        bool flg = gazeAware.HasGazeFocus;
        Debug.Log(flg);
    }
}
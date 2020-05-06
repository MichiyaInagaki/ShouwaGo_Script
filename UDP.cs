using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class UDP : MonoBehaviour
{
    public int SVM_label;
    public float yaw_val;
    public float pitch_val;
    //
    static UdpClient udp;
    IPEndPoint remoteEP = null;
    int i = 0;
    // Use this for initialization
    void Start()
    {
        int LOCA_LPORT = 50007;

        udp = new UdpClient(LOCA_LPORT);
        udp.Client.ReceiveTimeout = 2000;
    }

    // Update is called once per frame
    void Update()
    {
        IPEndPoint remoteEP = null;
        byte[] data = udp.Receive(ref remoteEP);        //送られてきたデータをbyte型で取得
        string text = Encoding.UTF8.GetString(data);    //string型に変換
        //Debug.Log(text);
        string[] dest = text.Split(',');                //ラベルとYaw角，Pitch角に分割
        SVM_label = int.Parse(dest[0]);
        yaw_val = -float.Parse(dest[1]);
        pitch_val = float.Parse(dest[2]);
        Debug.Log("label: " + SVM_label + "  yaw: " + yaw_val + "  pitch: " + pitch_val);
    }
}
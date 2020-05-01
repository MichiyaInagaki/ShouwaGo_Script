using UnityEngine;
using Tobii.Gaming;

public class headmovetest : MonoBehaviour
{
    void Update()
    {
        HeadPose headPose = TobiiAPI.GetHeadPose();
        Debug.Log("hoge");
        if (headPose.IsRecent())
        {
            Debug.Log("HeadPose Position (X,Y,Z): " + headPose.Position.x + ", " + headPose.Position.y + ", " + headPose.Position.z);
            Debug.Log("HeadPose Rotation (X,Y,Z): " + headPose.Rotation.eulerAngles.x + ", " + headPose.Rotation.eulerAngles.y + ", " + headPose.Rotation.eulerAngles.z);
        }
    }
}
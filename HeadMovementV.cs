using UnityEngine;
using Tobii.Gaming;

namespace TobiiEyeTracking
{
    public class HeadMovementV : MonoBehaviour
    {
        public float yaw_val;
        public float pitch_val;
        public float roll_val;
        public float Responsiveness = 10f;
        private Vector3 head_rotation;

        void Update()
        {
            var headPose = TobiiAPI.GetHeadPose();
            if (headPose.IsRecent())
            {
                Debug.Log("hoge");
                head_rotation = headPose.Rotation.eulerAngles;
                //yaw角の正規化（原点から±角度にする）
                if (head_rotation.y > 180)
                {
                    yaw_val = head_rotation.y - 360;
                }
                else
                {
                    yaw_val = head_rotation.y;
                }
                //pitch角の正規化（原点から±角度にする）
                if (head_rotation.x > 180)
                {
                    pitch_val = head_rotation.x - 360;
                }
                else
                {
                    pitch_val = head_rotation.x;
                }
                //roll角の正規化（原点から±角度にする）
                if (head_rotation.z > 180)
                {
                    roll_val = head_rotation.z - 360;
                }
                else
                {
                    roll_val = head_rotation.z;
                }
                //Debug.Log(head_rotation);
            }
        }
    }
}
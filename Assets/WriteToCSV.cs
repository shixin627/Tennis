using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WriteToCVSFile
{
    class Program
    {
        public static void Main(string[] args)
        {
            AddHeadingToFile(args[0]);
        }

        public static void AddHeadingToFile(string fileName)
        {
            try
            {
                string str_quat_upperArm = "upperarm_Qw,upperarm_Qx,upperarm_Qy,upperarm_Qz";
                string str_quat_foreArm = "forearm_Qw,forearm_Qx,forearm_Qy,forearm_Qz";
                string str_acc_foreArm = "forearm_AccX,forearm_AccY,forearm_AccZ";
                string str_angV_foreArm = "forearm_AngVX,forearm_AngVY,forearm_AngVZ";
                string str_euler_upperArm = "upperArmVectorX,upperArmVectorY,upperArmVectorZ";
                string str_angleDiff_upperArm = "upperArm_angleDiff";

                //string str_ForeArm_Angle = "ForeArm_Ang_X,ForeArm_Ang_Y,ForeArm_Ang_Z";
                //string str_UpperArm_Angle = "UpperArm_Ang_X,UpperArm_Ang_Y,UpperArm_Ang_Z";

                string str = "timestamp" + "," + str_quat_upperArm + "," + str_quat_foreArm + "," + str_acc_foreArm + "," + str_angV_foreArm + "," + str_euler_upperArm + "," + str_angleDiff_upperArm;
                //string str = "timestamp" + "," + str_acc_foreArm + "," + str_angV_foreArm;
                Debug.Log(str);

                string filePath = GetPath(fileName);

                StreamWriter outStream = System.IO.File.CreateText(filePath);
                outStream.WriteLine(str);
                outStream.Close();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("This prgram did an oopsie :", ex);
            }

        }

        public static void AddMARGRecordToFile(string timestamp, Quaternion raw_quat_upperArm, Quaternion raw_quat_foreArm, Vector3 raw_acc_foreArm, Vector3 raw_angV_foreArm, Vector3 upperArmVector, float angle_diff, string fileName)
        {
            try
            {
                string str_quat_upperArm = raw_quat_upperArm.w.ToString() + "," + raw_quat_upperArm.x.ToString() + "," + raw_quat_upperArm.y.ToString() + "," + raw_quat_upperArm.z.ToString();
                string str_quat_foreArm = raw_quat_foreArm.w.ToString() + "," + raw_quat_foreArm.x.ToString() + "," + raw_quat_foreArm.y.ToString() + "," + raw_quat_foreArm.z.ToString();
                string str_acc_foreArm = raw_acc_foreArm.x.ToString() + "," + raw_acc_foreArm.y.ToString() + "," + raw_acc_foreArm.z.ToString();
                string str_angV_foreArm = raw_angV_foreArm.x.ToString() + "," + raw_angV_foreArm.y.ToString() + "," + raw_angV_foreArm.z.ToString();
                string str_upperArmVector = upperArmVector.x.ToString() + "," + upperArmVector.y.ToString() + "," + upperArmVector.z.ToString();

                string str = timestamp + "," + str_quat_upperArm + "," + str_quat_foreArm + "," + str_acc_foreArm + "," + str_angV_foreArm + "," + str_upperArmVector + "," + angle_diff.ToString() ;
                //string str = timestamp  + "," + str_acc_foreArm + "," + str_angV_foreArm;
                //Debug.Log(str);

                string filePath = GetPath(fileName);
                StreamWriter outStream = System.IO.File.AppendText(filePath);
                outStream.WriteLine(str);
                outStream.Close();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("This prgram did an oopsie :", ex);
            }
        }

        public static void AddMARGRecordToFile(string timestamp, Vector3 raw_acc_foreArm, Vector3 raw_angV_foreArm, string fileName)
        {
            try
            {
                string str_acc_foreArm = raw_acc_foreArm.x.ToString() + "," + raw_acc_foreArm.y.ToString() + "," + raw_acc_foreArm.z.ToString();
                string str_angV_foreArm = raw_angV_foreArm.x.ToString() + "," + raw_angV_foreArm.y.ToString() + "," + raw_angV_foreArm.z.ToString();
                string str = timestamp  + "," + str_acc_foreArm + "," + str_angV_foreArm;

                string filePath = GetPath(fileName);
                StreamWriter outStream = System.IO.File.AppendText(filePath);
                outStream.WriteLine(str);
                outStream.Close();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("This prgram did an oopsie :", ex);
            }
        }

        private static string GetPath(string fileName)
        {
#if UNITY_EDITOR
            return Application.dataPath + "/CSV/" + $"{fileName}.csv";
#elif UNITY_ANDROID
        return Application.persistentDataPath+"/"+ $"{fileName}.csv";
#elif UNITY_IPHONE
        return Application.persistentDataPath+"/"+ $"{fileName}.csv";
#else
        return Application.dataPath +"/"+ $"{fileName}.csv";
#endif
        }
    }
}
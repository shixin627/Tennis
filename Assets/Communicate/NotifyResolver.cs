using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum NotifyKey
{
    KeyIncomingCall = 0x01,
    KeyIncomingCallAccept = 0x02,
    KeyIncomingCallRefuse = 0x03,
    KeyIncomingMessage = 0x04,
    KeyIncomingCallReject = 0x05,
    KeyBatteryChargeStatus = 0x06,
    KeyIncomingCallId = 0x07,
    KeyBbproMacAddressReturn = 0x10,
    KeyBbproStateReturn = 0x11,
    KeyAncsIncomingCallReturn = 0x12,
    KeyBbproConnInfoReturn = 0x13,
    KeyVoiceRecognitionResult = 0x14,
    KeyGSensorSample = 0x15,
    KeyWristCoordinate = 0x16,
    KeyGestureDetect = 0x17,
    KeyMovementSensitivity = 0x18,
    KeyGetTask = 0x19,
    KeyCreateTask = 0x1A,
    KeyToggleTask = 0x1B,
    KeyTaskSyncStart = 0x1C,
    KeyTaskSyncEnd = 0x1D,
    KeyUpdateTask = 0x1E,
    KeyGetNote = 0x1F,
    KeyRemoteInput = 0x20,
    KeyCreateNote = 0x21,
    KeyNoteSyncStart = 0x22,
    KeyNoteSyncEnd = 0x23,
    KeyUpdateNote = 0x24,
    KeyWatchFontSyncStart = 0x30,
    KeyWatchFontSyncEnd = 0x31,
    KeyUpdateWatchFont = 0x32,
    KeyWatchImageSyncStart = 0x33,
    KeyWatchImageSyncEnd = 0x34,
    KeyUpdateWatchImage = 0x35,
    KeyWatchIconSyncStart = 0x36,
    KeyWatchIconSyncEnd = 0x37,
    KeyUpdateWatchIcon = 0x38,
    KeyWatchfaceSyncStart = 0x39,
    KeyWatchfaceSyncEnd = 0x3A,
    KeyUpdateWatchface = 0x3B,
    KeyHeartRateSensorSample = 0x40,
    KeyHeartRateSensorResult = 0x41,
    KeyWatchSysRequest = 0x42,
    KeyWatchSysReturn = 0x43,
    KeyReturnChatIntent = 0x44,
    KeyChatResult = 0x45,
    KeyMediaTitle = 0x46,
    KeyQuaternionData = 0x47,
    KeyHrData = 0x48,
    KeyAudioData = 0x49,
    KeyAudioFile = 0x4A,
}

public class NotifyResolver
{
    private const double int16ToG = 8192.0;
    private const double gravity = 9.8;

    public void Resolve(L2Header l2Header, List<int> firstValue)
    {
        if (l2Header.ValueLength != firstValue.Count)
        {
            return;
        }

        ResolveNotifyCommand(l2Header.FirstKey, firstValue);
    }
    private void ResolveNotifyCommand(int key, List<int> pValue)
    {
        NotifyKey notifyKey = (NotifyKey)key;
        string debugMessage;

        switch (notifyKey)
        {
            case NotifyKey.KeyQuaternionData:
                {
                    // convert byte array to 4 little endiant float
                    float qw = BitConverter.ToSingle(new byte[] { (byte)pValue[0], (byte)pValue[1], (byte)pValue[2], (byte)pValue[3], }, 0);
                    float qx = BitConverter.ToSingle(new byte[] { (byte)pValue[4], (byte)pValue[5], (byte)pValue[6], (byte)pValue[7], }, 0);
                    float qy = BitConverter.ToSingle(new byte[] { (byte)pValue[8], (byte)pValue[9], (byte)pValue[10], (byte)pValue[11], }, 0);
                    float qz = BitConverter.ToSingle(new byte[] { (byte)pValue[12], (byte)pValue[13], (byte)pValue[14], (byte)pValue[15], }, 0);
                    Quaternion quaternion = new Quaternion(qx, qy, qz, qw);
                    // Debug.Log("Quaternion: " + quaternion);
                    if (quaternion == null)
                    {
                        Debug.LogError("Quaternion parameter is null");
                        return;
                    }
                    
                    GameObject playerBasePrefab = GameObject.Find("Player base prefab(Clone)");
                    if (playerBasePrefab == null)
                    {
                        Debug.LogError("Player base prefab(Clone) not found");
                        return;
                    }

                    ArmControl armControl = playerBasePrefab.GetComponent<ArmControl>();
                    if (armControl == null)
                    {
                        Debug.LogError("ArmControl component not found on Player base prefab");
                        return;
                    }

                    armControl.Set_Player_Forearm(quaternion);
                }

                break;
            // ... other cases
            default:
                break;
        }
    }

    // ... other methods
}
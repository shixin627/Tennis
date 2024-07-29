using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;

public enum ControlKey
{
    KeyTakePhoto = 0x01,
    KeyFindPhone = 0x02,
    KeyFindWatch = 0x03,
    KeyPhoneMediaControl = 0x04,
    KeyPhoneMediaStatus = 0x05,
    KeyPhoneVolumn = 0x06,
    KeyReturnVolumn = 0x07,
    KeyPhoneCameraStatus = 0x11,
    KeyVoice2TextStatus = 0x12,
    KeyReturnVoice2TextIntent = 0x13,
    KeyVoiceRecordStatus = 0x14,
    KeyReturnVoiceRecordIntent = 0x15,
    KeyGestureModeStatus = 0x16,
    KeyTpCoordinate = 0x20,
    KeyTpGesture = 0x21,
    KeyAudioRecord = 0x22,
    KeyAudioPlay = 0x23,
    KeyUnitTestUnicode = 0xE0,
    KeyUnitTestPageview = 0xE1,
}


public class ControlResolver
{
    public void Resolve(L2Header l2Header, List<int> firstValue)
    {
        if (l2Header.ValueLength != firstValue.Count)
        {
            return;
        }

        ResolveControlCommand(l2Header.FirstKey, firstValue);
    }
    private void ResolveControlCommand(int key, List<int> pValue)
    {
        ControlKey controlKey = (ControlKey)key;
        string debugMessage;

        switch (controlKey)
        {
            case ControlKey.KeyTpCoordinate:
                {
                    int direction = (pValue[0] == 0) ? 1 : -1;
                    GameObject.Find("MainApp").GetComponent<ArmControl>().HandleCoordinateY(direction);
                }
                break;
            case ControlKey.KeyTpGesture:
                {
                    GameObject.Find("MainApp").GetComponent<ArmControl>().HandleGesture(pValue[0]);
                }
                break;
            // ... other cases
            default:
                break;
        }
    }

    // ... other methods
}
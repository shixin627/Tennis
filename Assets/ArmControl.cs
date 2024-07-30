using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.Security.Cryptography;


public class ArmControl : MonoBehaviour
{
    /// <summary>
    /// define app mode (quaternion of upper sensor will be collected in two sensors mode)
    /// </summary>
    private readonly bool TWO_SENSOES_MODE = true;
    private readonly bool ANGULAR_V = true;
    private readonly int Sample_Count = 4;
    // TWO_SENSOES_MODE + ANGULAR_V:56 + Gravity => 68
    // TWO_SENSOES_MODE:44
    // ONE_SENSOE_MODE + ANGULAR_V:40
    // ONE_SENSOE_MODE:28

    private readonly int BUFFER_SIZE_ONE_SAMPLE = 68;
    private bool calibrated = false;

    private readonly string DeviceName = "SkaiWatch"; // SkaiWatch
    private readonly string ServiceUUID = "0000";
    private readonly string CharacteristicIMU = "0200";
    private readonly int sample_rate = 250;
    CommunicateParse parser = new CommunicateParse();

    enum States
    {
        None,
        Scan,
        Connect,
        RequestMTU,
        Subscribe,
        Unsubscribe,
        Disconnect,
        Communication,
    }
    private bool _workingFoundDevice = true;
    private bool _connected = false;
    private float _timeout = 0f;
    private States _state = States.None;
    private bool _foundMARGUUID = false;
    private string _deviceAddress;
    [SerializeField] private Button ConnectButton;
    [SerializeField] private Text stateText;
    [SerializeField] private TMP_Text RecordButtonText;
    [SerializeField] private TMP_Text ConnectButtonText;
    [SerializeField] private Transform Bot1_Forearm;
    [SerializeField] private Transform Bot1_Upperarm;
    [SerializeField] private Transform Bot2_Forearm;
    [SerializeField] private Transform Bot2_Upperarm;
    [SerializeField] private TMP_Dropdown genderDropdown;
    [SerializeField] private TMP_Dropdown heightDropdown;
    [SerializeField] private Image WatchFace;
    [SerializeField] private TMP_Text CoordinateY;
    [SerializeField] private TMP_Text CoordinateZ;
    [SerializeField] private TMP_Text GestureLabel;
    static Vector3 CalculateGravity(Quaternion Q)
    {
        // Calculate gravity in the sensor coordinate frame
        Vector3 gravity = new Vector3
        {
            x = 2.0f * (Q.x * Q.z - Q.w * Q.y),
            y = 2.0f * (Q.y * Q.z + Q.w * Q.x),
            z = 2.0f * (Q.w * Q.w - 0.5f + Q.z * Q.z)
        };
        return gravity;
    }

    static Quaternion last_forearm_quat = new Quaternion(0, 0, 0, 1);
    public void Set_Player_Forearm(Quaternion q)
    {
        Quat_UnityFrame_Forearm = new Quaternion(q.x, q.z, q.y, q.w);
        forearm_rotation = Quat_UnityFrame_Forearm.Add(Quat_Diff) * Inverse_Quat_Forearm;
        Bot1_Forearm.rotation = forearm_rotation;
        // calculate the angle difference on axis y and z of forearm

        Quaternion deltaRotation = Quaternion.Inverse(last_forearm_quat) * forearm_rotation;
        Vector3 deltaEuler = deltaRotation.eulerAngles;
        float delta_yaw = deltaEuler.y;
        float delta_pitch = deltaEuler.z;
        delta_yaw = delta_yaw > 180 ? delta_yaw - 360 : delta_yaw;
        delta_pitch = delta_pitch > 180 ? delta_pitch - 360 : delta_pitch;
        AccumulateAngleYaw(delta_yaw);
        AccumulateAnglePitch(delta_pitch);
        last_forearm_quat = forearm_rotation;

        Quaternion localRotation = Bot1_Forearm.localRotation;
        Message = $"Local Rotation:(X={localRotation.x:F2}, Y={localRotation.y:F2}, Z={localRotation.z:F2})";
        SetStateText(Message);

        if (!calibrated)
        {
            Vector3 gravityVector = CalculateGravity(Quat_UnityFrame_Forearm);
            bool ready = IsReadyForCalibration(gravityVector);
            if (!ready)
            {
                Message = "Uncalibrated...gravity=\n" + gravityVector.ToString();
            }
            else
            {
                ResetRotation();
                calibrated = true;
                Message = "It's Ready!!!";
            }
            SetStateText(Message);
        }
    }

    static bool isPress = false;
    static int coordinateZ = 0;
    static int coordinateY = 0;
    const int MAX_COORDINATE = 10;
    float accumulated_angle_yaw;
    float accumulated_angle_pitch;

    private void HandleCoordinateZ(int z)
    {
        if (isPress)
        {
            // 1: increase, -1: decrease
            if (z == 1)
            {
                if (coordinateZ < MAX_COORDINATE)
                {
                    coordinateZ++;
                }
            }
            else if (z == -1)
            {
                if (coordinateZ > 0)
                {
                    coordinateZ--;
                }
            }
            CoordinateZ.text = coordinateZ.ToString();
        }
    }

    public void HandleCoordinateY(int y)
    {
        if (isPress)
        {
            if (y == 1)
            {
                if (coordinateY < MAX_COORDINATE)
                {
                    coordinateY++;
                }
            }
            else if (y == -1)
            {
                if (coordinateY > 0)
                {
                    coordinateY--;
                }
            }
            CoordinateY.text = ArmControl.coordinateY.ToString();
        }
    }

    private void AccumulateAngleYaw(float delta_yaw)
    {
        accumulated_angle_yaw += delta_yaw;
        if (accumulated_angle_yaw >= 8.0f)
        {
            accumulated_angle_yaw = 0.0f;
            HandleCoordinateZ(1);
        }
        else if (accumulated_angle_yaw <= -8.0f)
        {
            accumulated_angle_yaw = 0.0f;
            HandleCoordinateZ(-1);
        }
    }
    private void AccumulateAnglePitch(float delta_pitch)
    {
        accumulated_angle_pitch += delta_pitch;
        if (accumulated_angle_pitch >= 8.0f)
        {
            accumulated_angle_pitch = 0.0f;
            HandleCoordinateY(1);
        }
        else if (accumulated_angle_pitch <= -8.0f)
        {
            accumulated_angle_pitch = 0.0f;
            HandleCoordinateY(-1);
        }
    }
    static int gesture_label = 0;
    public void HandleGesture(int gesture)
    {
        // 0 is release, 1 is press
        if (gesture == 0)
        {
            isPress = false;
            WatchFace.color = new Color32(0x38, 0x38, 0x38, 0xFF);
            accumulated_angle_pitch = 0.0f;
            accumulated_angle_yaw = 0.0f;
            gesture_label = 0;
            GestureLabel.text = "Release";
        }
        else if (gesture == 1)
        {
            isPress = true;
            WatchFace.color = Color.white;
            gesture_label = 1;
            GestureLabel.text = "Press";
        }
    }


    private Vector3 Acc_Forearm = new();
    private Vector3 AngV_Forearm = new();
    private Vector3 ForeArm_Angle = new();
    private Vector3 UpperArm_Angle1 = new();
    private Vector3 UpperArm_Angle2 = new();
    private Vector3 UpperArm_Euler = new();
    public Vector3 UE = new();
    private float Angle_Diff;

    private Quaternion Quat_GlobalFrame_Upperarm = new();
    private Quaternion Quat_GlobalFrame_Forearm = new();

    private Quaternion Quat_UnityFrame_Forearm = new();
    private Quaternion Quat_UnityFrame_Upperarm = new();

    private Quaternion Inverse_Quat_Forearm = new();
    private Quaternion Inverse_Quat_Upperarm = new();

    private Quaternion Inverse_Predict_Quat_Upperarm = new();

    private Quaternion Quat_North = Quaternion.Euler(0, 180, 180);
    private Quaternion Quat_Diff;

    private Quaternion upperarm_rotation1 = Quaternion.identity;
    private Quaternion upperarm_rotation2 = Quaternion.identity;
    public Vector3 upperarm_pred_vector = new();
    private Quaternion UpperArm_Diff;

    private Quaternion forearm_rotation;

    public Quaternion modleUpperArm;

    public String gender = "M";
    public String height = "150";
    public String FingerType;
    public String Message;

    string fileName = "";

    bool isRecording = false;

    public void selectGender(TMP_Dropdown change)
    {
        if (change.value == 0)
        {
            gender = "M";
        }
        else
        {
            gender = "F";
        }

    }
    public void selectHeight(TMP_Dropdown change)
    {
        height = (150 + change.value).ToString();
    }

    void Reset()
    {
        _workingFoundDevice =
            false; // used to guard against trying to connect to a second device while still connecting to the first
        _connected = false;
        _timeout = 0f;
        _state = States.None;
        _deviceAddress = null;
    }

    void SetState(States newState, float timeout)
    {
        _state = newState;
        _timeout = timeout;
    }

    public void SetStateText(string text)
    {
        if (stateText == null) return;
        if (text == "") return;
        stateText.text = text;
    }

    public void StartProcess()
    {
        SetStateText("Initializing...");
        Reset();
        BluetoothLEHardwareInterface.Initialize(true, false, () =>
        {
            SetState(States.Scan, 0.1f);
            SetStateText("Initialized");
        }, (error) => { BluetoothLEHardwareInterface.Log("Error: " + error); });
    }

    // Use this for initialization
    void Start()
    {
        StartProcess();

        Inverse_Quat_Forearm = Quaternion.identity;
        Inverse_Quat_Upperarm = Quaternion.identity;

        // genderDropdown.onValueChanged.AddListener(delegate
        // {
        //     selectGender(genderDropdown);
        // });

        // heightDropdown.onValueChanged.AddListener(delegate
        // {
        //     selectHeight(heightDropdown);
        // });
    }

    private double calculate_total_linear_acceleration(double x, double y, double z)
    {
        return Math.Pow(Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Pow(z, 2), 0.5);
    }
    private bool is_linear_acceleration_higher_than_boundary(double acc, double boundary)
    {
        return (acc > boundary);
    }
    private bool IsReadyForCalibration(Vector3 gravity)
    {
        double squaredGravity = gravity.x * gravity.x + gravity.y * gravity.y + gravity.z * gravity.z;
        float totalMagnetic = (float)Math.Sqrt(squaredGravity);
        float verticalMagnetic = Mathf.Abs(gravity.y);
        float sine = verticalMagnetic / totalMagnetic;
        return (sine < 0.001f);
    }
    //--------------------------------------------------------------------
    // Update is called once per frame
    void Update()
    {
        // if (_connected)
        // {
        //     ConnectButton.interactable = false;
        //     ConnectButtonText.text = "connected";
        // }
        // else
        // {
        //     ConnectButton.interactable = true;
        //     ConnectButtonText.text = "Connect";
        // }
        if (_timeout > 0f)
        {
            _timeout -= Time.deltaTime;
            if (_timeout <= 0f)
            {
                _timeout = 0f;

                switch (_state)
                {
                    case States.None:
                        SetStateText("None");
                        break;

                    case States.Scan:
                        Message = "Scanning Watch...";
                        SetStateText(Message);
                        BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(null, (address, name) =>
                        {
                            if (name.Contains(DeviceName))
                            {
                                _workingFoundDevice = true;

                                BluetoothLEHardwareInterface.StopScan();

                                _deviceAddress = address;
                                SetState(States.Connect, 0.5f);

                                _workingFoundDevice = false;
                            }
                        }, null, false, false);
                        break;

                    case States.Connect:
                        Message = "Connecting to Watch";
                        SetStateText(Message);
                        _foundMARGUUID = false;
                        BluetoothLEHardwareInterface.ConnectToPeripheral(_deviceAddress, null, null,
                            (address, serviceUUID, characteristicUUID) =>
                            {
                                if (IsEqual(serviceUUID, ServiceUUID))
                                {
                                    Message = "Found ServiceUUID";
                                    SetStateText(Message);
                                    _foundMARGUUID = _foundMARGUUID || IsEqual(characteristicUUID, CharacteristicIMU);
                                    if (_foundMARGUUID)
                                    {
                                        _connected = true;
                                        SetState(States.RequestMTU, 2f);
                                        Message = "Found CharacteristicIMU";
                                        SetStateText(Message);
                                    }
                                }

                            }, (disconnectedAddress) =>
                            {
                                BluetoothLEHardwareInterface.Log("Device disconnected: " + disconnectedAddress);
                                SetStateText("Disconnected");
                            });
                        break;

                    case States.RequestMTU:
                        //SetStateText("Requesting MTU");
                        int requiredMTU = 512;
                        BluetoothLEHardwareInterface.RequestMtu(_deviceAddress, requiredMTU, (address, newMTU) =>
                        {
                            //(Sample_Count * BUFFER_SIZE_ONE_SAMPLE + 3)
                            SetStateText("MTU set to " + newMTU.ToString());
                            SetState(States.Subscribe, 0.1f);
                        });
                        break;

                    case States.Subscribe:
                        Message = "Subscribing to Watch address" + _deviceAddress;
                        SetStateText(Message);

                        BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(_deviceAddress,
                            FullUUID(ServiceUUID),
                            FullUUID(CharacteristicIMU), null,
                            (address, characteristicUUID, bytes) =>
                            {
                                // convert bytes to List<int>
                                List<int> data = bytes.Select(b => (int)b).ToList();
                                parser.ResolveL2Frame(data);
                                long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                                // print data length
                                //string str = $"Data Length: {data.Count}";
                                //SetStateText(str);
                                //SetStateText("Listening data stream from wristband");
                                /*
                                string str = "";

                                if (bytes.Length == Sample_Count * BUFFER_SIZE_ONE_SAMPLE)
                                {
                                    for (int i = 0; i < Sample_Count; i++)
                                    {
                                        Message = "";
                                        int index = BUFFER_SIZE_ONE_SAMPLE * i;
                                        
                                        float qw_foreArm = BitConverter.ToSingle(bytes, index + 0);
                                        float qx_foreArm = BitConverter.ToSingle(bytes, index + 4);
                                        float qy_foreArm = BitConverter.ToSingle(bytes, index + 8);
                                        float qz_foreArm = BitConverter.ToSingle(bytes, index + 12);

                                        float accx_foreArm = BitConverter.ToSingle(bytes, index + 16);
                                        float accy_foreArm = BitConverter.ToSingle(bytes, index + 20);
                                        float accz_foreArm = BitConverter.ToSingle(bytes, index + 24);
                                        
                                        if (ANGULAR_V)
                                        {
                                            float angVx_foreArm = BitConverter.ToSingle(bytes, index + 28);
                                            float angVy_foreArm = BitConverter.ToSingle(bytes, index + 32);
                                            float angVz_foreArm = BitConverter.ToSingle(bytes, index + 36);
                                            AngV_Forearm = new Vector3(angVx_foreArm, angVy_foreArm, angVz_foreArm);
                                        }

                                        if (TWO_SENSOES_MODE)
                                        {
                                            if (ANGULAR_V)
                                            {
                                                float qw_upperArm = BitConverter.ToSingle(bytes, index + 40);
                                                float qx_upperArm = BitConverter.ToSingle(bytes, index + 44);
                                                float qy_upperArm = BitConverter.ToSingle(bytes, index + 48);
                                                float qz_upperArm = BitConverter.ToSingle(bytes, index + 52);
                                                Quat_GlobalFrame_Upperarm = new Quaternion(qx_upperArm, qy_upperArm, qz_upperArm, qw_upperArm);
                                                float gravityX = BitConverter.ToSingle(bytes, index + 56);
                                                float gravityY = BitConverter.ToSingle(bytes, index + 60);
                                                float gravityZ = BitConverter.ToSingle(bytes, index + 64);
                                                Vector3 gravityVector = new Vector3(gravityX, gravityY, gravityZ);
                                                bool ready = IsReadyForCalibration(gravityVector);
                                                if (!calibrated)
                                                {
                                                    if (!ready)
                                                    {
                                                        Message += "Uncalibrated";
                                                    }
                                                    else
                                                    {
                                                        ResetRotation();
                                                        calibrated = true;
                                                        Message += "It's Ready";
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                float qw_upperArm = BitConverter.ToSingle(bytes, index + 28);
                                                float qx_upperArm = BitConverter.ToSingle(bytes, index + 32);
                                                float qy_upperArm = BitConverter.ToSingle(bytes, index + 36);
                                                float qz_upperArm = BitConverter.ToSingle(bytes, index + 40);
                                                Quat_GlobalFrame_Upperarm = new Quaternion(qx_upperArm, qy_upperArm, qz_upperArm, qw_upperArm);
                                            }
                                        }
                                        else
                                        {
                                            float gravityX = BitConverter.ToSingle(bytes, index + 40);
                                            float gravityY = BitConverter.ToSingle(bytes, index + 44);
                                            float gravityZ = BitConverter.ToSingle(bytes, index + 48);
                                            Vector3 gravityVector = new Vector3(gravityX, gravityY, gravityZ);
                                            bool ready = IsReadyForCalibration(gravityVector);
                                            if (!calibrated)
                                            {
                                                if (!ready)
                                                {
                                                    Message += "Uncalibrated";
                                                }
                                                else
                                                {
                                                    ResetRotation();
                                                    calibrated = true;
                                                    Message += "It's Ready";
                                                }
                                            }
                                        }

                                        Quat_GlobalFrame_Forearm = new Quaternion(qx_foreArm, qy_foreArm, qz_foreArm, qw_foreArm);
                                        Acc_Forearm = new Vector3(accx_foreArm, accy_foreArm, accz_foreArm);
                                        
                                        HandleSampleData(Quat_GlobalFrame_Upperarm, Quat_GlobalFrame_Forearm);

                                        //str = $"Sample: {i} | UpperArm => qx: " + (qx_upperArm).ToString("0.00000") +
                                        //", qy: " + (qy_upperArm).ToString("0.00000") + ", qz: " + (qz_upperArm).ToString("0.00000") +
                                        //", qw: " + (qw_upperArm).ToString("0.00000") + "\nForeArm => qx: " + (qx_foreArm).ToString("0.00000") +
                                        //", qy: " + (qy_foreArm).ToString("0.00000") + ", qz: " + (qz_foreArm).ToString("0.00000") +
                                        //", qw: " + (qw_foreArm).ToString("0.00000") + "\n";
                                        if (TWO_SENSOES_MODE)
                                        {
                                            upperarm_rotation1 = Quat_UnityFrame_Upperarm.Add(Quat_Diff) * Inverse_Quat_Upperarm;
                                            //UpperArm_Euler = upperarm_rotation1.eulerAngles;
                                        }

                                        forearm_rotation = Quat_UnityFrame_Forearm.Add(Quat_Diff) * Inverse_Quat_Forearm;

                                        Bot1_Upperarm.rotation = upperarm_rotation1;
                                        Bot1_Forearm.rotation = forearm_rotation;
                                         
                                         Vector3 upperArmVector = Bot1_Upperarm.transform.right;
                                        //str = $"Sample: {i} |" +
                                        //$"\n  UpperArm : " + upperarm_rotation.ToString()+
                                        //"\n  ForeArm : " + forearm_rotation.ToString() +"\n";

                                        string timestamp = (milliseconds + 20 * i).ToString();

                                        if (isRecording)
                                        {
                                            WriteToCVSFile.Program.AddMARGRecordToFile(timestamp, upperarm_rotation1, forearm_rotation, Acc_Forearm, AngV_Forearm, upperArmVector, Angle_Diff, fileName);
                                            
                                            //GameObject.Find("MainApp").GetComponent<FingerGuesture>().Invoke(Acc_Forearm, AngV_Forearm);
                                            
                                        }
                                        //double acc_total = calculate_total_linear_acceleration(accx_foreArm, accy_foreArm, accz_foreArm);
                                        //Debug.Log(str);
                                        SetStateText(Message);
                                    }


                            
                                    //UpperArm_Diff = Bot1_Upperarm.rotation.Diff(Bot2_Upperarm.rotation);
                                    //Vector3 eulerAngles = UpperArm_Diff.eulerAngles;
                                    //str += ("eulerAngles:" + eulerAngles.ToString());
                                }
                                else
                                {
                                    str = $"The length of bytes {bytes.Length} is not expected!";
                                    //Debug.Log(str);
                                }
                                */

                                ////////Set State Text////////

                            });

                        _state = States.None;
                        break;

                    case States.Unsubscribe:
                        BluetoothLEHardwareInterface.UnSubscribeCharacteristic(_deviceAddress, FullUUID(ServiceUUID),
                            FullUUID(CharacteristicIMU),
                            null);
                        SetState(States.Disconnect, 4f);
                        break;

                    case States.Disconnect:
                        if (_connected)
                        {
                            BluetoothLEHardwareInterface.DisconnectPeripheral(_deviceAddress, (address) =>
                            {
                                BluetoothLEHardwareInterface.DeInitialize(() =>
                                {
                                    _connected = false;
                                    _state = States.None;
                                });
                            });
                        }
                        else
                        {
                            BluetoothLEHardwareInterface.DeInitialize(() => { _state = States.None; });
                        }

                        break;
                }
            }
        }
    }
    // BLE Methods
    string FullUUID(string uuid)
    {
        return "00000000-0000-" + uuid + "-454c-42494c464953";
    }

    bool IsEqual(string uuid1, string uuid2)
    {
        if (uuid1.Length == 4)
            uuid1 = FullUUID(uuid1);
        if (uuid2.Length == 4)
            uuid2 = FullUUID(uuid2);

        return (uuid1.ToUpper().Equals(uuid2.ToUpper()));
    }
    // Motion Trackiing Methods
    public void ResetRotation()
    {
        Quat_Diff = Quat_North.Diff(Quat_UnityFrame_Forearm);
        //Inverse_Quat_Forearm = Quaternion.Inverse(Quat_North);
        //Inverse_Quat_Upperarm = Quaternion.Inverse(Quat_North);
        Inverse_Quat_Forearm = Quaternion.Inverse(Quat_UnityFrame_Forearm.Add(Quat_Diff));
        Inverse_Quat_Upperarm = Quaternion.Inverse(Quat_UnityFrame_Upperarm.Add(Quat_Diff));
        //Inverse_Predict_Quat_Upperarm = Quaternion.Inverse(modleUpperArm.Add(Quat_Diff));
    }

    public void ResetCalibration()
    {
        calibrated = false;
    }

    //Record
    public void ToggleRecordingDataset()
    {
        //csv
        if (!isRecording)
        {
            if (RecordButtonText == null) return;
            RecordButtonText.text = "Stop Record";
            //fileName = "Record_" + DateTime.Now.ToString("yyyy_MM_dd_HH-mm-ss");
            fileName = gender + height + "_" + DateTime.Now.ToString("yyyy_MM_dd_HH-mm-ss");
            string[] args = { fileName };
            WriteToCVSFile.Program.Main(args);
            isRecording = true;
        }
        else
        {
            if (RecordButtonText == null) return;
            RecordButtonText.text = "Start Record";
            isRecording = false;

        }
    }

    private void HandleSampleData(Quaternion raw_quat_upperArm, Quaternion raw_quat_foreArm)
    {
        Quat_UnityFrame_Upperarm = new Quaternion(-raw_quat_upperArm.x, -raw_quat_upperArm.z, -raw_quat_upperArm.y, raw_quat_upperArm.w);
        Quat_UnityFrame_Forearm = new Quaternion(-raw_quat_foreArm.x, -raw_quat_foreArm.z, -raw_quat_foreArm.y, raw_quat_foreArm.w);
    }

}
public static class QuaternionExtensions
{
    public static Quaternion Diff(this Quaternion to, Quaternion from)
    {
        return to * Quaternion.Inverse(from);
    }
    public static Quaternion Add(this Quaternion start, Quaternion diff)
    {
        return diff * start;
    }
}
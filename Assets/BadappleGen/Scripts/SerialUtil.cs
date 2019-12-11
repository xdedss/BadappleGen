using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

public class SerialUtil : MonoBehaviour
{
    public static SerialUtil instance;

    public string portName;
    public SerialPort port;
    public TextMesh dbgText;

    [Space]
    public Vector2 posToSend;
    public int scale = 40;

    public List<LaserNode> dataToSend;
    //bool waiting = false;

    private void Init()
    {
        port = new SerialPort("COM8", 115200);
        port.RtsEnable = true;
        port.Open();
        Debug.Log(port);
    }
    int seg = 255;
    int maxSegCount = 4;
    int segWaitTime = 8;
    
    void WriteDataSync()
    {
        if (dataToSend == null)
        {
            port.Write(new byte[] { 0, 0, 0, 0, 0, 255 }, 0, 6);
            for (int ti = 0; ti < segWaitTime; ti++)
            {
                if (port.BytesToRead != 0)
                {
                    byte response = (byte)port.ReadByte();
                    if (response == 0xff)
                    {
                        break;
                    }
                }
                Thread.Sleep(1);
            }
            Debug.LogWarning("Empty frame");
            return;
        }
        int count = 0;
        List<byte> bytes = new List<byte>();
        foreach (var node in dataToSend)
        {
            byte[] bts = node.ToBytes(scale);
            foreach (var bt in bts)
            {
                count++;
                bytes.Add(bt);
            }
            if (count >= seg * maxSegCount)
            {
                Debug.LogWarning("break");
                break;
            }
        }
        byte[] btw = bytes.ToArray();
        int btlen;
        for (int offset = 0; offset < btw.Length; offset += seg)
        {
            btlen = Mathf.Min(btw.Length - offset, seg);
            port.Write(btw, offset, btlen);
            if (offset + seg < btw.Length)
            {
                for (int ti = 0; ti < segWaitTime; ti++)
                {
                    if (port.BytesToRead != 0)
                    {
                        byte response = (byte)port.ReadByte();
                        if (response == 0xfe)
                        {
                            Debug.LogWarning("seg trans successful");
                            break;
                        }
                    }
                    //Debug.Log("sleep" + ti);
                    Thread.Sleep(1);
                    if(ti == segWaitTime - 1)
                    {
                        Debug.LogError("seg trans failed");
                    }
                }
            }
        }
        port.Write(new byte[] { 255 }, 0, 1);
        Debug.LogWarning("SyncWrote" + btw.Length + "Bytes");
        for (int ti = 0; ti < segWaitTime; ti++)
        {
            if (port.BytesToRead != 0)
            {
                byte response = (byte)port.ReadByte();
                if (response == 0xff)
                {
                    Debug.LogWarning("Sync response 255 OK");
                    break;
                }
            }
            Thread.Sleep(1);
            if (ti == segWaitTime - 1)
            {
                Debug.LogError("Sync failed");
            }
        }
        count_sent++;
        dbgText.text = count_sent.ToString();
        //waiting = true;
    }

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        Init();
    }

    int count_sent = 0;
    void Update()
    {
        //Debug.Log("Bytes:" + port.BytesToRead);
        if (port.BytesToRead != 0)
        {
            //count_rec++;
            byte response = (byte)port.ReadByte();
            //if (response == 255)
            //{
            //    waiting = false;
            //}
            Debug.LogWarning("response:" + response);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            WriteDataSync();
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            SendSingle();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            port.Write(new byte[] { 255 }, 0, 1);
        }
        WriteDataSync();
    }

    private void SendSingle()
    {
        byte[] bts = new LaserNode(posToSend, 1, 1).ToBytes(scale);
        port.Write(bts, 0, bts.Length);
    }
    private void SendTest()
    {
        byte[] testData = new byte[] { 0x01, 0x00, 0x00, 0x00, 0xfe, 0x00, 0x00, 0x01, 0x00, 0xfe };
        port.Write(testData, 0, testData.Length);
        port.Write(new byte[] { 255 }, 0, 1);
        //waiting = true;
        Debug.LogWarning("sending");
    }
}

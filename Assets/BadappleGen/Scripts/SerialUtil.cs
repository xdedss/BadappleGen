using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

public class SerialUtil : MonoBehaviour
{
    public static SerialUtil instance;

    public string portName;
    private SerialPort port;
    private bool portConnected = false;
    public TextMesh dbgText;

    [Space]
    public Vector2 posToSend;
    public int scale = 40;
    public bool autoSend;

    public List<LaserNode> dataToSend;
    //bool waiting = false;

    public void Init(string portName)
    {
        port = new SerialPort(portName, 115200);
        port.RtsEnable = true;
    }

    public bool TryConnect()
    {
        try
        {
            port.Open();
        }
        catch (IOException)
        {
            portConnected = false;
            Debug.LogWarning("Connection failed @ " + port.PortName);
            return false;
        }
        portConnected = true;
        return true;
    }


    int seg = 255;
    int maxSegCount = 4;
    int segWaitTime = 8;

    void SendDataSync()
    {
        byte response;
        if (dataToSend == null) //空帧
        {
            SendNode(new LaserNode(Vector2.zero, Color.black, 1));
            SendEnd();
            if (ReceiveByte(out response, segWaitTime) && response == 0xff)
            {
                Debug.Log("Empty frame 0xff OK");
            }
            else
            {
                Debug.LogWarning("Empty frame failed");
            }
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
                Debug.LogWarning("break: too many nodes");
                break;
            }
        }
        byte[] btw = bytes.ToArray();
        int btlen;
        Debug.Log("Bytes to write: " + btw.Length);
        //分段发送
        for (int offset = 0; offset < btw.Length; offset += seg)
        {
            btlen = Mathf.Min(btw.Length - offset, seg);
            port.Write(btw, offset, btlen);
            if (offset + seg < btw.Length)
            {
                if (ReceiveByte(out response, segWaitTime) && response == 0xfe)
                {
                    Debug.Log("seg response 0xfe OK");
                }
                else
                {
                    Debug.LogWarning("seg failed");
                }
            }
        }
        SendEnd();
        if (ReceiveByte(out response, segWaitTime) && response == 0xff)
        {
            Debug.Log("response 0xff OK");
        }
        else
        {
            Debug.LogWarning("Sync failed");
        }
        count_sent++;
        DebugText(count_sent.ToString());
    }

    public bool ReceiveByte(out byte b, int millisecTimeout)
    {
        for (int ti = 0; ti < millisecTimeout; ti++)
        {
            if (port.BytesToRead != 0)
            {
                b = (byte)port.ReadByte();
                return true;
            }
            Thread.Sleep(1);
        }
        if (port.BytesToRead != 0)
        {
            b = (byte)port.ReadByte();
            return true;
        }
        b = 0;
        return false;
    }

    private void DebugText(string msg)
    {
        if (dbgText)
        {
            dbgText.text = msg;
        }
    }

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        Init(portName);
        TryConnect();
    }

    int count_sent = 0;
    void Update()
    {
        if (portConnected)
        {
            if (port.BytesToRead != 0)
            {
                byte response = (byte)port.ReadByte();
                Debug.LogWarning("response:" + response);
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                SendDataSync();
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                SendNode(new LaserNode(posToSend, 1, 1));
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                SendEnd();
            }
            if (autoSend)
            {
                SendDataSync();
            }
        }
    }

    public void SendNode(LaserNode node)
    {
        byte[] bts = node.ToBytes(scale);
        SendBytes(bts);
        Debug.LogWarning("sending node");
    }
    public void SendTest()
    {
        byte[] testData = new byte[] { 0x01, 0x00, 0x00, 0x00, 0xfe, 0x00, 0x00, 0x01, 0x00, 0xfe };
        SendBytes(testData);
        SendEnd();
        Debug.LogWarning("sending test");
    }
    public void SendEnd()
    {
        SendBytes(new byte[] { 255 });
    }
    public void SendBytes(byte[] bytes)
    {
        port.Write(bytes, 0, bytes.Length);
    }
    public void SendBytes(byte[] bytes, int offset, int count)
    {
        port.Write(bytes, offset, count);
    }
}

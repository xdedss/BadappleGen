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

    public List<LaserNode> dataToSend;
    bool waiting = false;

    private void Init()
    {
        port = new SerialPort("COM8", 115200);
        port.RtsEnable = true;
        //port.WriteBufferSize = 16384;
        //port.ReadBufferSize = 16384;
        port.Open();
        Debug.Log(port);
        //port.DataReceived += DataReceived;
        //Log("initialized");

        //port.Write("1");
    }

    void WriteData()
    {
        int count = 0;
        List<byte> bytes = new List<byte>();
        foreach (var node in dataToSend)
        {
            count++;
            byte[] bts = Node2Bytes(node);
            foreach(var bt in bts)
            {
                bytes.Add(bt);
            }
            if (count >= 1000) break;
        }
        byte[] btw = bytes.ToArray();
        port.Write(btw, 0, btw.Length);

        var s = "";
        for(int i = 0; i < 100; i++)
        {
            s += btw[i] + " ";
        }
        Debug.LogWarning(s);
        Debug.LogWarning("wrote" + btw.Length);
    }

    int seg = 255;
    int maxSegCount = 4;
    int segWaitTime = 8;
    void WriteDataAsync()
    {
        StartCoroutine(WriteDataAsyncEnum());
    }

    IEnumerator WriteDataAsyncEnum()
    {
        int count = 0;
        List<byte> bytes = new List<byte>();
        foreach (var node in dataToSend)
        {
            byte[] bts = Node2Bytes(node);
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
        for (int offset = 0; offset < btw.Length; offset += seg)
        {
            yield return new WaitForSeconds(0);
            port.Write(btw, offset, Mathf.Min(btw.Length - offset, seg));
        }
        port.Write(new byte[] { 255 }, 0, 1);
        Debug.LogWarning("AsyncWrote" + btw.Length);
        count_sent++;
        dbgText.text = count_sent.ToString();
        waiting = true;
        yield return 0;
        //port.Write(btw, 0, btw.Length);
    }

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
            byte[] bts = Node2Bytes(node);
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

    float scale = 40;
    //0-65024, 0->32512
    byte[] Node2Bytes(LaserNode node)
    {
        byte[] res = new byte[5];
        int xi = Mathf.RoundToInt(node.pos.x * scale) + 32512;
        if (xi > 65024) xi = 65024;
        if (xi < 0) xi = 0;
        int yi = Mathf.RoundToInt(-node.pos.y * scale) + 32512;
        if (yi > 65024) yi = 65024;
        if (yi < 0) yi = 0;
        int intensityi = Mathf.RoundToInt(node.intensity * 254);
        res[0] = (byte)(xi / 255);
        res[1] = (byte)(xi % 255);
        res[2] = (byte)(yi / 255);
        res[3] = (byte)(yi % 255);
        res[4] = (byte)intensityi;
        return res;
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
        Debug.Log("Bytes:" + port.BytesToRead);
        if (port.BytesToRead != 0)
        {
            //count_rec++;
            byte response = (byte)port.ReadByte();
            if (response == 255)
            {
                waiting = false;
            }
            Debug.LogWarning("response:" + response);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            WriteDataSync();
            //SendNext();
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
        //TrySendNext();
    }

    private void TrySendNext()
    {
        if (!waiting && dataToSend != null)
        {
            WriteDataAsync();
        }
    }

    private void SendNext()
    {
        WriteData();
        port.Write(new byte[] { 255 }, 0, 1);
        count_sent++;
        dbgText.text = count_sent.ToString();
        waiting = true;
        Debug.LogWarning("sending");
    }
    private void SendSingle()
    {
        byte[] bts = Node2Bytes(new LaserNode(posToSend, 1, 1));
        port.Write(bts, 0, bts.Length);
    }
    private void SendTest()
    {
        byte[] testData = new byte[] { 0x01, 0x00, 0x00, 0x00, 0xfe, 0x00, 0x00, 0x01, 0x00, 0xfe };
        port.Write(testData, 0, testData.Length);
        port.Write(new byte[] { 255 }, 0, 1);
        waiting = true;
        Debug.LogWarning("sending");
    }
}

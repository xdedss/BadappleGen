using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using System.IO;

public class Test : MonoBehaviour
{
    public bool video = false;
    public Texture2D textureToTest;
    public RenderTexture rt;
    public VideoPlayer vp;
    public int chainIndex = 0;
    public Material mat;
    public Transform pic;

    Texture2D tex2d;

    void Start()
    {
        vp.sendFrameReadyEvents = true;
        vp.frameReady += Vp_frameReady;
    }

    private void Vp_frameReady(VideoPlayer source, long frameIdx)
    {
        //Debug.Log(frameIdx);
        RT2T2(rt, tex2d);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            RunTest();
        }
        if(video && tex2d)
        {
            EdgeDetect.image = tex2d;
            EdgeDetect.Process();

            //var path = LaserPath.ConvertEdge(EdgeDetect.chains);
            //path = LaserPath.OptimizePath(path, 16);
        }
        LaserPath.color = Color.HSVToRGB(Mathf.Clamp((EdgeDetect.brightness - 0.5f) * 2 + 1 / 3f, 1 / 6f, 1 / 2f), 1, 1);
        var path = LaserPath.ConvertEdge(new List<EdgeChain>(EdgeDetect.chains));
        DrawLaserPath(path);
        SerialUtil.instance.dataToSend = path;
    }

    void OnGUI()
    {
        //GUI.Label(new Rect(0, 0, 100, 30), EdgeDetect.chains.Count.ToString());
    }

    void RunTest()
    {
        if (!video)
        {
            mat.SetTexture("_MainTex", textureToTest);
            pic.localScale = new Vector3(textureToTest.width, textureToTest.height, 1);
            EdgeDetect.image = textureToTest;
            EdgeDetect.Process();
            //var path = LaserPath.ConvertEdge(EdgeDetect.chains);
            //path = LaserPath.OptimizePath(path, 16);

            //var cfile = File.CreateText(@"D:\LZR\MyFiles\temp\coors.txt");

            //cfile.Write("float coorX[] = {");
            //foreach (var node in path)
            //{
            //    cfile.Write(string.Format("{0:F3}f, ", node.pos.x));
            //}
            //cfile.Write("};");
            //cfile.WriteLine("");
            //cfile.Write("float coorY[] = {");
            //foreach (var node in path)
            //{
            //    cfile.Write(string.Format("{0:F3}f, ", node.pos.y));
            //}
            //cfile.Write("};");
            //cfile.WriteLine("");
            //cfile.Write("float coorI[] = {");
            //foreach (var node in path)
            //{
            //    cfile.Write(string.Format("{0:F3}f, ", node.intensity));
            //}
            //cfile.Write("};");
            ////cfile.WriteLine("");

            //cfile.Flush(); cfile.Close();
        }
        else
        {
            mat.SetTexture("_MainTex", rt);
            pic.localScale = new Vector3(rt.width, rt.height, 1);
            tex2d = new Texture2D(rt.width, rt.height);
            vp.Play();
        }
        //Debug.Log(EdgeDetect.chains.Count);

    }

    void DrawChain (EdgeChain chain)
    {
        if (chain.next)
        {
            Debug.DrawLine(chain.pos.V3(0), chain.next.pos.V3(0), Color.green);
            DrawChain(chain.next);
        }
    }

    void DrawLaserPath(List<LaserNode> path)
    {
        LaserNode last = null;
        //bool flip = true;
        foreach(var node in path)
        {
            if(last != null)
            {
                //flip ^= true;
                //float intensity = (last.intensity + node.intensity) / 4 + 0.5f;
                //Debug.DrawLine(last.pos.V3(0), node.pos.V3(0), new Color(flip?0:1, 1, 0, intensity));
                var col = last.color;
                col.a = Mathf.Clamp01(col.r + col.g + col.b) * 0.7f + 0.3f;
                Debug.DrawLine(last.pos.V3(0), node.pos.V3(0), col);
            }
            last = node;
        }
    }

    void RT2T2(RenderTexture renderTex, Texture2D target)
    {
        RenderTexture.active = renderTex;
        target.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        target.Apply();
        RenderTexture.active = null;
    }
}

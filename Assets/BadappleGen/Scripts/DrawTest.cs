using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 屏幕上画线，给SerialUtil
/// </summary>
public class DrawTest : MonoBehaviour
{

    public float lineMag;

    public Color col;
    public bool refreshColor = false;

    List<LaserNode> currentData = new List<LaserNode>();
    Vector2 lastPoint = Vector2.zero;
    
    void Start()
    {
        currentData.Add(new LaserNode(new Vector2(0, 0), col, 1));
    }
    
    void Update()
    {
        Vector2 mouse = Input.mousePosition;
        currentData[currentData.Count - 1].pos = mouse;
        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            var mag = (lastPoint - mouse).magnitude;
            if(mag > lineMag)
            {
                NewNode(new LaserNode(mouse, col, 1));
            }
        }
        if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            NewNode(new LaserNode(mouse, Color.black, 1));
            NewNode(new LaserNode(mouse, Color.black, 1));
            NewNode(new LaserNode(mouse, col, 1));
        }
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            //NewNode(new LaserNode(mouse, Color.black, 1));
            currentData[currentData.Count - 1] = new LaserNode(mouse, Color.black, 1);
            NewNode(new LaserNode(mouse, col, 1));
        }
        SerialUtil.instance.dataToSend = currentData;

        Test.DrawLaserPath(currentData);
        if (Input.GetKeyDown(KeyCode.C))
        {
            refreshColor ^= true;
        }
        if (refreshColor)
        {
            foreach(var node in currentData)
            {
                node.color = col;
            }
        }
        if (Input.GetKeyDown(KeyCode.Comma))
        {
            currentData.RemoveAt(currentData.Count - 1);
            DrawPoly(mouse, Mathf.PI / 4, 4, 120);
            NewNode(new LaserNode(mouse, col, 1));
        }
        if (Input.GetKeyDown(KeyCode.Period))
        {
            currentData.RemoveAt(currentData.Count - 1);
            DrawPoly(mouse, 0, 3, 120);
            NewNode(new LaserNode(mouse, col, 1));
        }
        if (Input.GetKeyDown(KeyCode.Slash))
        {
            currentData.RemoveAt(currentData.Count - 1);
            DrawPoly(mouse, 0, 36, 120);//36边 接近圆
            NewNode(new LaserNode(mouse, col, 1));
        }
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            currentData.RemoveAt(currentData.Count - 1);
            currentData.Clear();
            currentData.Add(new LaserNode(new Vector2(0, 0), col, 1));
        }
    }

    /// <summary>
    /// 正多边形
    /// </summary>
    /// <param name="center">中心点</param>
    /// <param name="startRad">起始弧度</param>
    /// <param name="n">边数</param>
    /// <param name="r">外接圆半径</param>
    void DrawPoly(Vector2 center, float startRad, int n, float r)
    {
        for(float i = 0; i < n; i++)
        {
            var angle1 = Mathf.PI * 2 / n * i + startRad;
            var angle2 = Mathf.PI * 2 / n * (i + 1) + startRad;
            DrawLine(center + new Vector2(r * Mathf.Cos(angle1), r * Mathf.Sin(angle1)), center + new Vector2(r * Mathf.Cos(angle2), r * Mathf.Sin(angle2)));
        }

    }

    void DrawLine(Vector2 p1, Vector2 p2)
    {
        var len = (p1 - p2).magnitude;
        int segmentCount = Mathf.CeilToInt(len / lineMag);
        for(float i = 0; i <= segmentCount; i++)
        {
            NewNode(new LaserNode(Vector2.Lerp(p1, p2, i / (float)segmentCount), col, 1));
        }
    }

    void NewNode(LaserNode node)
    {
        currentData.Add(node);
        lastPoint = node.pos;
    }
}

﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 用EdgeDetect处理完得到的路径链在这里调用ConvertEdge进行处理，返回一个单一的完整路径点（LaserNode）列表
/// </summary>
public static class LaserPath
{
    public static Color color = Color.cyan;

    //贪心 把多个路径连成一个
    public static List<LaserNode> ConvertEdge(List<EdgeChain> chains)
    {
        if (chains == null || chains.Count == 0) return new List<LaserNode>();
        var laserNodes = new List<LaserNode>();
        var currentPos = chains[0].pos;
        while(chains.Count > 0)
        {
            var chain = NearestChain(chains, currentPos);
            chains.Remove(chain);
            laserNodes.Add(new LaserNode(chain.pos, 0, 0));
            laserNodes.Add(new LaserNode(chain.pos, color, 0));
            while (chain.next)
            {
                chain = chain.next;
                laserNodes.Add(new LaserNode(chain.pos, color, 1));
            }
            laserNodes.Add(new LaserNode(chain.pos, 0, 0));
            currentPos = chain.pos;
        }
        //Debug.Log("NodeCount:" + laserNodes.Count);
        return laserNodes;
    }

    static EdgeChain NearestChain(List<EdgeChain> chains, Vector2 pos)
    {
        EdgeChain nearest = null;
        float nearestD2 = float.PositiveInfinity;
        foreach(var chain in chains)
        {
            var D2 = (chain.pos - pos).sqrMagnitude;
            if(D2 < nearestD2)
            {
                nearestD2 = D2;
                nearest = chain;
            }
        }
        return nearest;
    }
    
}

/// <summary>
/// 单个光点，包含位置和颜色信息
/// </summary>
public class LaserNode
{
    public Vector2 pos;
    //public float intensity;
    public Color color;
    /// <summary>
    /// 暂时没用
    /// </summary>
    public float deltaTime;

    public LaserNode(Vector2 pos, float intensity, float deltaTime)
    {
        this.pos = pos;
        this.color = new Color(intensity, intensity, intensity);
        this.deltaTime = deltaTime;
    }
    public LaserNode(Vector2 pos, Color color, float deltaTime)
    {
        this.pos = pos;
        this.color = color;
        this.deltaTime = deltaTime;
    }

    public byte[] ToBytes(float scale)
    {
        byte[] res = new byte[5];
        int xi = Mathf.RoundToInt(pos.x * scale) + 32512;
        if (xi > 65024) xi = 65024;
        if (xi < 0) xi = 0;
        int yi = Mathf.RoundToInt(-pos.y * scale) + 32512;
        if (yi > 65024) yi = 65024;
        if (yi < 0) yi = 0;
        int ri = Mathf.RoundToInt(color.r * 3);
        int gi = Mathf.RoundToInt(color.g * 3);
        int bi = Mathf.RoundToInt(color.b * 3);
        int intensityi = (ri << 6) + (gi << 4) + (bi << 2);
        res[0] = (byte)(xi / 255);
        res[1] = (byte)(xi % 255);
        res[2] = (byte)(yi / 255);
        res[3] = (byte)(yi % 255);
        res[4] = (byte)intensityi;
        return res;
    }
}

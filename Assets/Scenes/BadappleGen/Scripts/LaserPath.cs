using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LaserPath
{
    //贪心 把多个路径连成一个
    public static List<LaserNode> ConvertEdge(List<EdgeChain> chains)
    {
        if (chains.Count == 0) return null;
        var laserNodes = new List<LaserNode>();
        var currentPos = chains[0].pos;
        while(chains.Count > 0)
        {
            var chain = NearestChain(chains, currentPos);
            chains.Remove(chain);
            laserNodes.Add(new LaserNode(chain.pos, 0, 0));
            laserNodes.Add(new LaserNode(chain.pos, 1, 0));
            while (chain.next)
            {
                chain = chain.next;
                laserNodes.Add(new LaserNode(chain.pos, 1, 1));
            }
            laserNodes.Add(new LaserNode(chain.pos, 0, 0));
            currentPos = chain.pos;
        }
        Debug.Log("NodeCount:" + laserNodes.Count);
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

//单个光点
public class LaserNode
{
    public Vector2 pos;
    public float intensity;
    public float deltaTime;

    public LaserNode(Vector2 pos, float intensity, float deltaTime)
    {
        this.pos = pos;
        this.intensity = intensity;
        this.deltaTime = deltaTime;
    }
}

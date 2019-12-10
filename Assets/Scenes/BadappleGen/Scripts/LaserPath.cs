using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LaserPath
{

    public static List<LaserNode> OptimizePath(List<LaserNode> path, float maxError)
    {
        var pathArr = path.ToArray();
        List<LaserNode> res = new List<LaserNode>();
        int i0 = 0;
        int i1 = 1;
        float totalError = 0;
        float error;
        float newError;
        res.Add(pathArr[0]);
        while (true)
        {
            if(i1 == pathArr.Length - 1)
            {
                break;
            }
            if(CanOptimize(pathArr[i0], pathArr[i1], pathArr[i1 + 1], out error))
            {
                newError = error + totalError;
                if (Mathf.Abs(newError) < maxError)
                {
                    pathArr[i1] = null;
                    i1++;
                    totalError = newError;
                    continue;
                }
            }
            res.Add(pathArr[i1]);
            i0 = i1;
            i1++;
            totalError = 0;
        }
        res.Add(pathArr[i1]);
        Debug.Log(res.Count);
        return res;
    }

    static bool CanOptimize(LaserNode n0, LaserNode n1, LaserNode n2, out float error)
    {
        if(n1.deltaTime > 0 && n2.deltaTime > 0)
        {
            error = V2Cross((n0.pos - n1.pos), (n2.pos - n1.pos));
            return true;
        }
        else
        {
            error = 0;
            return false;
        }
    }

    static float V2Cross(Vector2 v1, Vector2 v2)
    {
        return v1.x * v2.y - v1.y * v2.x;
    }

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


    static float centerRad(float rad, float center)
    {
        while(rad > center + Mathf.PI)
        {
            rad -= Mathf.PI * 2;
        }
        while(rad <= center - Mathf.PI){
            rad += Mathf.PI * 2;
        }
        return rad;
    }
}

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

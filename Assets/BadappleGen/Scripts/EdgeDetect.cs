using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EdgeDetect
{

    public static List<EdgeChain> chains = new List<EdgeChain>();
    public static Texture2D image;

    static List<TurningPoint> turningPoints = new List<TurningPoint>();
    static TurningPoint[,] turningPointsTable;
    static bool[,] searchedMask;

    static int stepPri = 4;
    static int stepSec = 1;
    static float rayLen = 16;
    static float angleStep = Mathf.PI / 6;

    static bool errorFrameSaved = false;

    public static void Process()
    {
        if (!image) return;
        chains.Clear();

        FindTurningPoints();
        searchedMask = new bool[image.width, image.height];

        while(turningPoints.Count > 0)
        {
            var point = turningPoints[0];
            //turningPoints.RemoveAt(0);
            //Debug.Log("start at " + turningPoints[0].pos);
            var chain = SearchChain(new EdgeChain(point));
            if (chain)
            {
                chains.Add(chain);
            }
        }

    }

    static EdgeChain SearchChain(EdgeChain chainStart, int maxSearch = 800)
    {
        int counter = 0;
        RemoveTurningPoints(chainStart.pos, rayLen * 1.5f);
        //MarkSearchedMask(chainStart.pos, rayLen * 0.5f);
        EdgeChain currentChainStart = chainStart;
        Vector2 prevPos;
        float L2 = rayLen * rayLen;
        while(SearchPrevious(currentChainStart, out prevPos))
        {
            counter++;
            if (counter > maxSearch)
            {
                Debug.LogError("search exited");
                //if (!errorFrameSaved)
                //{
                //    byte[] bytes = image.EncodeToPNG();
                //    var fs = System.IO.File.OpenWrite(@"D:\LZR\MyFiles\temp\errorImage.png");
                //    fs.Write(bytes, 0, bytes.Length);
                //    fs.Flush();
                //    fs.Close();
                //}
                break;
            }
            //成环
            if (currentChainStart.next && currentChainStart.next.next && (currentChainStart.pos - chainStart.pos).sqrMagnitude < L2)
            {
                return currentChainStart;
            }
            //合并
            EdgeChain chainToMerge = null;
            foreach (var chain in chains)
            {
                if ((chain.Last.pos - currentChainStart.pos).sqrMagnitude <= L2)
                {
                    chainToMerge = chain;
                    break;
                }
            }
            if (chainToMerge)
            {
                var chainEnd = chainToMerge.Last;
                chainEnd.next = currentChainStart;
                currentChainStart.previous = chainEnd;
                return null;
            }
            //此处标记为已搜索
            RemoveTurningPoints(prevPos, rayLen * 1.5f);
            //MarkSearchedMask((currentChainStart.pos + prevPos) / 2, rayLen * 0.5f);
            MarkSearchedMask(prevPos, rayLen * 0.3f);
            //新节点
            var newNode = new EdgeChain(prevPos);
            newNode.next = currentChainStart;
            currentChainStart.previous = newNode;
            currentChainStart = newNode;
        }
        return currentChainStart;
    }
    
    static bool SearchPrevious(EdgeChain chainStart, out Vector2 res)
    {
        float prevAngle = 0;
        if (chainStart.next)
        {
            var d = chainStart.pos - chainStart.next.pos;
            prevAngle = Mathf.Atan2(d.y, d.x);
        }
        Vector2 prevPos = chainStart.pos + new Vector2(Mathf.Cos(prevAngle), Mathf.Sin(prevAngle)) * rayLen;
        int prevSample = BilinearSample(prevPos);
        int steps = Mathf.FloorToInt(Mathf.PI / angleStep);
        int firstSample = prevSample;
        int dir = firstSample > 0 ? -1 : 1;
        for (float i = 0; i <= steps; i++)
        {
            float curAngle = prevAngle + angleStep * dir;
            Vector2 curPos = chainStart.pos + new Vector2(Mathf.Cos(curAngle), Mathf.Sin(curAngle)) * rayLen;
            int curSample = BilinearSample(curPos);
            if (curSample != prevSample && curSample != firstSample && curSample != -1 && prevSample != -1 && !searchedMask[Mathf.FloorToInt(curPos.x), Mathf.FloorToInt(curPos.y)])
            {
                res = BinSearchBetween(curPos, prevPos, curSample, prevSample, 1);
                return true;
            }
            prevPos = curPos;
            prevSample = curSample;
            prevAngle = curAngle;
        }
        res = Vector2.zero;
        return false;
    }

    static Vector2 BinSearchBetween(Vector2 v1, Vector2 v2, int v1s, int v2s, int count)
    {
        var center = (v1 + v2) / 2;
        if (count == 0) return center;
        int centerSample = BilinearSample(center);
        if(centerSample == v1s)
        {
            return BinSearchBetween(center, v2, v1s, v2s, count - 1);
        }
        else
        {
            return BinSearchBetween(v1, center, v1s, v2s, count - 1);
        }
    }

    static void MarkSearchedMask(Vector2 pos, float radius)
    {
        int startX = Mathf.RoundToInt(pos.x - radius);
        int endX = Mathf.RoundToInt(pos.x + radius);
        int startY = Mathf.RoundToInt(pos.y - radius);
        int endY = Mathf.RoundToInt(pos.y + radius);
        float r2 = radius * radius;
        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                if (InBound(x, y) && (pos - new Vector2(x, y)).sqrMagnitude <= r2)
                {
                    searchedMask[x, y] = true;
                }
            }
        }
    }

    static void RemoveTurningPoints(Vector2 pos, float radius)
    {
        int startX = Mathf.RoundToInt(pos.x - radius);
        int endX = Mathf.RoundToInt(pos.x + radius);
        int startY = Mathf.RoundToInt(pos.y - radius);
        int endY = Mathf.RoundToInt(pos.y + radius);
        float r2 = radius * radius;
        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                if(InBound(x, y) && (pos - new Vector2(x, y)).sqrMagnitude <= r2)
                {
                    RemoveTurningPoint(x, y);
                }
            }
        }
    }

    static void RemoveTurningPoint(int x, int y)
    {
        var turningPoint = turningPointsTable[x, y];
        if (turningPoint != null)
        {
            turningPoints.Remove(turningPoint);
            turningPointsTable[x, y] = null;
        }
    }

    static void AddTurningPoint(int x, int y)
    {
        if (turningPointsTable[x, y] == null)
        {
            var turningPoint = new TurningPoint(new Vector2(x, y));
            turningPoints.Add(turningPoint);
            turningPointsTable[x, y] = turningPoint;
        }
    }

    static void FindTurningPoints()
    {
        turningPoints.Clear();
        turningPointsTable = new TurningPoint[image.width, image.height];
        //从画面四个边缘找
        int prevPoint = -1;
        for (int x = 0; x < image.width; x += stepSec)
        {
            int curPoint = SafeSample(x, 0);
            if (prevPoint != curPoint && prevPoint != -1)
            {
                AddTurningPoint(x, 0);
            }
            prevPoint = curPoint;
        }
        prevPoint = -1;
        for (int x = 0; x < image.width; x += stepSec)
        {
            int curPoint = SafeSample(x, image.height - 1);
            if (prevPoint != curPoint && prevPoint != -1)
            {
                AddTurningPoint(x, image.height - 1);
            }
            prevPoint = curPoint;
        }
        prevPoint = -1;
        for (int y = 0; y < image.height; y += stepSec)
        {
            int curPoint = SafeSample(0, y);
            if (prevPoint != curPoint && prevPoint != -1)
            {
                AddTurningPoint(0, y);
            }
            prevPoint = curPoint;
        }
        prevPoint = -1;
        for (int y = 0; y < image.height; y += stepSec)
        {
            int curPoint = SafeSample(image.width - 1, y);
            if (prevPoint != curPoint && prevPoint != -1)
            {
                AddTurningPoint(image.width - 1, y);
            }
            prevPoint = curPoint;
        }
        //画面内部，竖向
        for (int x = stepPri; x < image.width; x += stepPri)
        {
            prevPoint = -1;
            for (int y = 0; y < image.height; y += stepSec)
            {
                int curPoint = SafeSample(x, y);
                if(prevPoint != curPoint && prevPoint != -1)
                {
                    AddTurningPoint(x, y);
                }
                prevPoint = curPoint;
            }
        }
        //画面内部，横向
        for (int y = stepPri; y < image.height; y += stepPri)
        {
            prevPoint = -1;
            for (int x = 0; x < image.width; x += stepSec)
            {
                int curPoint = SafeSample(x, y);
                if (prevPoint != curPoint && prevPoint != -1)
                {
                    AddTurningPoint(x, y);
                }
                prevPoint = curPoint;
            }
        }
    }

    static bool InBound(int x, int y)
    {
        return !(x < 0 || x >= image.width || y < 0 || y >= image.height);
    }
    static bool InBound(Vector2 pos)
    {
        return !(pos.x < 0 || pos.x >= image.width || pos.y < 0 || pos.y >= image.height);
    }

    static int BilinearSample(Vector2 pos)
    {
        if (!InBound(pos))
        {
            return -1;
        }
        var col = image.GetPixelBilinear(pos.x / image.width, pos.y / image.height).g;
        return col > 0.5 ? 1 : 0;
    }
    static int SafeSampleDiscrete(Vector2 pos)
    {
        int xi = Mathf.RoundToInt(pos.x);
        int yi = Mathf.RoundToInt(pos.y);
        return SafeSample(xi, yi);
    }
    static int SafeSample(int x, int y)
    {
        if (!InBound(x, y))
        {
            return -1;
        }
        return image.GetPixel(x, y).g > 0.5 ? 1 : 0;
    }

}

public class TurningPoint
{
    public Vector2 pos;
    public TurningPoint(Vector2 pos)
    {
        this.pos = pos;
    }
}

//双向索引链式结构 可以拼接 不可以中间插入
public class EdgeChain
{
    public EdgeChain next = null;
    public EdgeChain previous = null;
    public Vector2 pos;

    public EdgeChain Last
    {
        get
        {
            if (last)
            {
                if (last.next)
                {
                    last = last.next.Last;
                }
            }
            else if (next)
            {
                last = next.Last;
            }
            else
            {
                last = this;
            }
            return last;
        }
    }
    EdgeChain last = null;
    public EdgeChain First
    {
        get
        {
            if (first)
            {
                if (first.previous)
                {
                    first = first.previous.First;
                }
            }
            else if (previous)
            {
                first = previous.First;
            }
            else
            {
                first = this;
            }
            return first;
        }
    }
    EdgeChain first = null;
    public int IndexToLast
    {
        get
        {
            if (!next)
            {
                return 0;
            }
            return next.IndexToLast + 1;
        }
    }
    public int IndexToFirst
    {
        get
        {
            if (!previous)
            {
                return 0;
            }
            return previous.IndexToFirst + 1;
        }
    }

    public EdgeChain(Vector2 pos)
    {
        this.pos = pos;
    }

    public EdgeChain(TurningPoint turningPoint)
    {
        this.pos = turningPoint.pos;
    }

    public static implicit operator bool(EdgeChain e)
    {
        return e != null;
    }
}
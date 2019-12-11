using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CodeConverter : MonoBehaviour
{
    public static CodeConverter instance;

    public TextAsset template;
    public int scale = 40;

    string GenerateArray(List<LaserNode> nodes)
    {
        string s = "{ ";
        foreach(var node in nodes)
        {
            var bts = node.ToBytes(scale);
            foreach(var bt in bts)
            {
                s += bt.ToString("x2") + ", ";
            }
        }
        s += "0 }";
        return s;
    }

    string GenerateCode(List<LaserNode> nodes)
    {
        var arrstr = GenerateArray(nodes);
        return string.Format(template.text, nodes.Count * 5, arrstr);
    }

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {

    }
    
    void Update()
    {
        
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GlobalRuntimeControl : MonoBehaviour
{
    public static GlobalRuntimeControl instance;

    public Test videoTest;
    public DrawTest drawTest;
    public Text tipText;
    bool drawMode = true;
    public InputField comNum;
    public Text debugText;
    public RawImage colInd;

    public Slider sR;
    public Slider sG;
    public Slider sB;

    public static void SetDebugText(string txt)
    {
        instance.debugText.text = txt;
    }

    public void ConnectClicked()
    {
        SerialUtil.instance.Init(comNum.text);
        SerialUtil.instance.TryConnect();
    }

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        SwitchMode(true);
    }
    
    void Update()
    {
        var col = new Color(sR.value, sG.value, sB.value);
        drawTest.col = col;
        colInd.color = col;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SwitchMode(!drawMode);
        }
    }

    void SwitchMode(bool drawMode)
    {
        this.drawMode = drawMode;
        if (drawMode)
        {
            drawTest.enabled = true;
            videoTest.enabled = false;
            tipText.text = @"
鼠标绘图模式：
键盘操作
逗号键画正方形
句号键画正三角形
斜杠键画圆
Delete清空
C键切换实时更改颜色

空格键切换模式
";
        }
        else
        {
            drawTest.enabled = false;
            videoTest.enabled = true;
            tipText.text = @"
播放视频模式：
键盘操作：
T键开始播放

空格键切换模式
";
        }
    }
}

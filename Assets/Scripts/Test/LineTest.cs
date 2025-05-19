using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserConnector : MonoBehaviour
{
    [Header("连接目标")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("线条属性")]
    public float width = 0.1f;
    public Color glowColor = Color.cyan;
    public Material lineMaterial; // 可选自定义材质

    private LineRenderer line;

    void Start()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;

        line.startWidth = width;
        line.endWidth = width;

        if (lineMaterial != null)
        {
            line.material = lineMaterial;
        }
        else
        {
            // fallback shader: Unity 内置 Particles/Additive 适合发光
            line.material = new Material(Shader.Find("Particles/Additive"));
        }

        line.startColor = glowColor;
        line.endColor = glowColor;
    }

    void Update()
    {
        if (startPoint != null && endPoint != null)
        {
            line.SetPosition(0, startPoint.position);
            line.SetPosition(1, endPoint.position);
        }
    }
}
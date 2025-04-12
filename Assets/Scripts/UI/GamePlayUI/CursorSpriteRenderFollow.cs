using SparFlame.GamePlaySystem.CustomInput;
using Unity.Entities;
using UnityEngine;

public class SpriteCursor : MonoBehaviour
{
    [Header("跟随设置")]
    public Camera targetCamera;   // 鼠标坐标转换的相机
    public Vector3 worldOffset;   // 世界坐标偏移（可选）

    [Header("可选动画设置")]
    public bool faceCamera = false; // 是否让鼠标面向摄像机（3D 时用）

    private EntityManager _em;
    private EntityQuery _query;
    void Start()
    {
        // 隐藏系统鼠标
        Cursor.visible = false;

        if (targetCamera == null)
            targetCamera = Camera.main;
        _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        _query = _em.CreateEntityQuery(typeof(InputMouseData));
    }

    void Update()
    {
        var data = _query.GetSingleton<InputMouseData>();
        
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(targetCamera.transform.position.z); // 设置距离（仅在透视相机下需要）

        // 将屏幕坐标转为世界坐标
        // Vector3 worldPos = targetCamera.ScreenToWorldPoint(mousePos) + worldOffset;
        
        var worldPos = data.HitPosition;
        // 设置 sprite cursor 位置
        transform.position = worldPos;

        // 可选：3D 时朝向摄像机
        if (faceCamera)
            transform.rotation = Quaternion.LookRotation(targetCamera.transform.forward);
    }

    private void OnDisable()
    {
        Cursor.visible = true; // 关闭脚本时恢复系统光标
    }
}
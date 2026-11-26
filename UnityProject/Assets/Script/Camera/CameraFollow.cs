using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("跟踪设置 / Follow Settings")]
    public Transform target;          // 玩家的 Transform
    public float smoothTime = 0.25f;  // 平滑时间，越小跟踪越快 / Lower is faster
    public Vector3 offset = new Vector3(0, 0, -10); // 摄像机偏移量

    private Vector3 currentVelocity = Vector3.zero;

    void Start()
    {
        // 如果没有手动赋值，尝试寻找带 Player 标签的物体
        // If not assigned, try to find object with "Player" tag
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    // 摄像机必须在 LateUpdate 中执行，确保玩家先移动完
    // Camera follow must happen in LateUpdate to ensure player moved first
    void LateUpdate()
    {
        if (target != null)
        {
            // 计算目标位置 / Calculate target position
            Vector3 targetPosition = target.position + offset;

            // 使用 SmoothDamp 平滑移动 / Smooth move using SmoothDamp
            transform.position = Vector3.SmoothDamp(
                transform.position, 
                targetPosition, 
                ref currentVelocity, 
                smoothTime
            );
        }
    }
}
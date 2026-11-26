using UnityEngine;

public class SimpleMovement : MonoBehaviour
{
    // 在 Inspector 面板可以实时调节的速度值
    public float moveSpeed = 5f;

    void Update()
    {
        // 1. 获取输入：读取键盘的上下左右或 WASD
        // GetAxis 会返回 -1 到 1 之间的数值
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // 2. 计算移动方向
        Vector3 direction = new Vector3(horizontalInput, verticalInput, 0);

        // 3. 执行位移：坐标 = 当前坐标 + 方向 * 速度 * 时间修正
        // transform.Translate 是最直接的坐标变更方法
        transform.Translate(direction * moveSpeed * Time.deltaTime);
    }
}

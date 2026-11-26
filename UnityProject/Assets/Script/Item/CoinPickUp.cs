using UnityEngine;

public class CoinPickUp : MonoBehaviour
{
    public ItemID itemId;
    public AudioID pickUpAudioId = AudioID.CoinPickUp; // 在 Inspector 选择 101

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 检测到玩家碰撞箱后拾取
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            // 1. 发送物品拾取广播 / Item pick up broadcast
            GameEvents.TriggerItemPickedUp(itemId);

            // 2. 发送音频播放广播 / Audio play broadcast
            GameEvents.TriggerPlaySound(pickUpAudioId);

            // 3. 销毁 / Destroy
            Destroy(gameObject);
        }
    }
}
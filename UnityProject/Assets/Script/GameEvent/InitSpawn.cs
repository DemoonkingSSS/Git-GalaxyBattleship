using UnityEngine;
using System.Collections.Generic;

public class InitSpawn : MonoBehaviour
{
    [System.Serializable]
    public class SpawnGroup
    {
        public string groupName = "New Group";
        public GameObject prefab;
        public int count = 5;
    }

    [Header("生成配置 / Spawn Settings")]
    public List<SpawnGroup> spawnList = new List<SpawnGroup>();
    
    [Header("范围设置 / Range Settings")]
    public Vector2 spawnAreaMin = new Vector2(-10,-10);
    public Vector2 spawnAreaMax = new Vector2(10,10);

    [Header("分布强度 / Distribution Jitter")]
    [Range(0f, 1f)]
    public float jitterAmount = 0.8f; // 随机偏移强度：0为完美网格，1为最大乱序

    void Start()
    {
        ExecuteInitialSpawn();
    }

    public void ExecuteInitialSpawn()
    {
        // 1. 计算总共需要生成的数量
        // 1. Calculate the total quantity needed
        int totalToSpawn = 0;
        foreach (var group in spawnList) totalToSpawn += group.count;
        if (totalToSpawn <= 0) return;

        // 2. 根据总数和范围，计算网格的行数和列数
        // 2. Calculate grid rows and columns based on total count and area
        float width = spawnAreaMax.x - spawnAreaMin.x;
        float height = spawnAreaMax.y - spawnAreaMin.y;
        float aspectRatio = width / height;

        // 计算近似的列数：列 * (列 / 宽高比) ≈ 总数
        // Calculate approximate columns: cols * (cols / aspect) ≈ total
        int columns = Mathf.CeilToInt(Mathf.Sqrt(totalToSpawn * aspectRatio));
        int rows = Mathf.CeilToInt((float)totalToSpawn / columns);

        float cellWidth = width / columns;
        float cellHeight = height / rows;

        // 3. 生成所有可能的网格点位并打乱顺序
        // 3. Generate all possible grid points and shuffle them
        List<Vector2> gridPositions = new List<Vector2>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                // 计算格子中心点 / Calculate cell center
                float centerX = spawnAreaMin.x + (c * cellWidth) + (cellWidth / 2f);
                float centerY = spawnAreaMin.y + (r * cellHeight) + (cellHeight / 2f);

                // 添加随机抖动偏移，让分布更自然
                // Add random jitter offset for a more natural distribution
                float offsetX = Random.Range(-cellWidth / 2f, cellWidth / 2f) * jitterAmount;
                float offsetY = Random.Range(-cellHeight / 2f, cellHeight / 2f) * jitterAmount;

                gridPositions.Add(new Vector2(centerX + offsetX, centerY + offsetY));
            }
        }

        // 随机打乱点位顺序，防止前面的预制体总是挤在左下角
        // Shuffle positions so earlier prefabs aren't always in the bottom-left
        ShuffleList(gridPositions);

        // 4. 按顺序生成物体
        // 4. Instantiate objects in sequence
        int posIndex = 0;
        foreach (var group in spawnList)
        {
            if (group.prefab == null) continue;

            for (int i = 0; i < group.count; i++)
            {
                if (posIndex >= gridPositions.Count) break;

                Vector2 pos = gridPositions[posIndex];
                GameObject spawnedObj = Instantiate(group.prefab, pos, Quaternion.identity, this.transform);
                // --- 动态添加追踪组件 ---
                // 传入 groupName 确保不同组被分别统计
                var tracker = spawnedObj.AddComponent<SpawnedTarget>();
                tracker.Initialize(group.groupName);
                posIndex++;
            
            }
        }
        
        Debug.Log($"[InitSpawn] 已按网格随机散布完成。网格大小: {columns}x{rows}");
    }
    
    // 经典的洗牌算法 | Classic Fisher-Yates Shuffle
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3((spawnAreaMin.x + spawnAreaMax.x) / 2, (spawnAreaMin.y + spawnAreaMax.y) / 2, 0);
        Vector3 size = new Vector3(spawnAreaMax.x - spawnAreaMin.x, spawnAreaMax.y - spawnAreaMin.y, 1);
        Gizmos.DrawWireCube(center, size);
    }
}
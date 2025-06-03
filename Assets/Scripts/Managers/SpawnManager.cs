using UnityEngine;

public class SpawnManager : GenericObjectPool<Block>
{
  public static SpawnManager Instance;

  [Header("Prediction Settings")]
  [SerializeField] private Block PredictedBlock;
  [SerializeField] private int PredictedIndex = 0;

  bool hasSpawnedFirstblock = false;

  protected override void Awake()
  {
    Instance = this;
    if (!PredictedBlock)
    {
      Debug.LogError("PredictedBlock is not assigned in the inspector.");
      return;
    }
    base.Awake();
  }

  private void Start()
  {
    SpawnNextBlock();
  }

  internal void Reset()
  {
    ReturnAllItemsToPool();
    hasSpawnedFirstblock = false;
    SpawnNextBlock();
  }

  internal void SpawnNextBlock()
  {
    // Debug.Log("Spawning Next Block");
    if (!hasSpawnedFirstblock)
    {
      hasSpawnedFirstblock = true;
      PredictedIndex = Random.Range(0, 4);
      PredictedBlock.SetValue(PredictedIndex);
      Spawn(Random.Range(0, 4));
    }
    else
    {
      Spawn(PredictedIndex);
      PredictedIndex = Random.Range(0, 4);
      PredictedBlock.SetValue(PredictedIndex);
    }
  }

  void Spawn(int SpawnedBlockIndex)
  {
    if (GridManager.Instance.IsGameOver())
    {
      BoardManager.Instance.EndGame();
      return;
    }

    Block block = GetFromPool();
    block.Init(SpawnedBlockIndex);

    if (!InputManager.Instance.enabled)
      InputManager.Instance.enabled = true;
    InputManager.Instance.SetActiveBlock(block.transform);
    GridManager.Instance.BlockList.Add(block);
  }
}

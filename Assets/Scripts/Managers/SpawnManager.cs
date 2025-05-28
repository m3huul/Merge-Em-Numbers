using UnityEngine;

public class SpawnManager : MonoBehaviour
{
  public static SpawnManager Instance;
  [SerializeField] private Block PredictedBlock;
  [SerializeField] private GameObject BlockPrefab;
  [SerializeField] private int PredictedIndex = 0;
  [SerializeField] private Transform NewBlocksParent;
  [SerializeField] internal Block CurrentBlock;
  bool initBlock = false;
  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
    }
  }

  private void Start()
  {
    SpawnNextBlock();
  }

  internal void Reset()
  {
    initBlock = false;
    SpawnNextBlock();
  }

  internal void SpawnNextBlock()
  {
    // Debug.Log("Spawning Next Block");
    if (!initBlock)
    {
      initBlock = true;
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
    if (GridManager.Instance.CheckGameEnd())
    {
      BoardManager.Instance.KillGame();
      return;
    }

    GameObject Block = Instantiate(BlockPrefab, GridManager.Instance.BlockGrid[2].Column[0].boardPosition.position, Quaternion.identity, NewBlocksParent);
    Block blockScript = Block.GetComponent<Block>();
    blockScript.Init(SpawnedBlockIndex);
    CurrentBlock = blockScript;


    if (!InputManager.Instance.enabled)
      InputManager.Instance.enabled = true;
    InputManager.Instance.setBlock(Block.transform);
    GridManager.Instance.BlockList.Add(blockScript);
  }
}

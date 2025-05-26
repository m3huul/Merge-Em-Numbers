using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
  public static SpawnManager Instance;
  [SerializeField] private Block PredictedBlock;
  [SerializeField] private GameObject BlockPrefab;
  [SerializeField] private int PredictiedIndexValue = 0;
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

  internal void SpawnNextBlock()
  {
    if (!initBlock)
    {
      initBlock = true;
      PredictiedIndexValue = Random.Range(0, 4);
      PredictedBlock.SetValue(PredictiedIndexValue);
      Spawn(Random.Range(0, 4));
    }
    else
    {
      Spawn(PredictiedIndexValue);
      PredictiedIndexValue = Random.Range(0, 4);
      PredictedBlock.SetValue(PredictiedIndexValue);
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


    if(!InputManager.Instance.enabled)
      InputManager.Instance.enabled = true;
    InputManager.Instance.setBlock(Block.transform);
    GridManager.Instance.BlockList.Add(blockScript);
  }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
  public static SpawnManager Instance;
  [SerializeField] private Block PredictedBlock;
  [SerializeField] private GameObject BlockPrefab;
  int NextPredictedBlockIndex = -1;
  private void Awake() {
    if (Instance == null) {
      Instance = this;
    }
  }

  internal void SpawnNextBlock()
  {
    Block currBlock = null;
    if (NextPredictedBlockIndex == -1)
    {
      NextPredictedBlockIndex = Random.Range(0, 2);
      PredictedBlock.SetPredictedValue(Random.Range(0, 2));
      currBlock = Instantiate(BlockPrefab, BoardManager.Instance.m_boardBlocks[InputManager.Instance.m_columnIndex].Column[0].boardPosition.position, Quaternion.identity).GetComponent<Block>();
      currBlock.Init();
    }
    else
    {

    }
  }
}

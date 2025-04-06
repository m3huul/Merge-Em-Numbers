using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Linq;

public class BoardManager : MonoBehaviour
{
  public static BoardManager Instance;
  [SerializeField] internal List<BoardBlocks> m_boardBlocks = new();
  [SerializeField] internal List<int> BlockNumbers = new();
  [SerializeField] internal List<Color> BlockColors = new();
  [SerializeField] internal Gradient colorGradient;
  [SerializeField] private List<BlockView> BlockViews = new();
  [SerializeField] private Transform m_newBlocksParent;
  [SerializeField] private GameObject m_blockPrefab;
  [SerializeField] private float basePullDownSpeed = 15f;
  [SerializeField] internal float fastPullDownSpeed = 0.2f;
  [SerializeField] private int predictiedValue = 0;
  [SerializeField] private BlockView predictedBlock;
  private bool initBlock = false;
  private BlockView _currMovingBlock;
  private Tween _currBlockTween;
  private int activeMergeCheck = 0;

  void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
    }
  }

  void Start()
  {
    for (int i = 0; i < 4; i++)
    {
      int newValue = GenerateNextNumber();
      BlockColors.Add(GetColorForValue(newValue));
      BlockNumbers.Add(newValue);
    }
    SetupBlock();
  }

  int GenerateNextNumber()
  {
    int lastNumber = BlockNumbers[BlockNumbers.Count - 1];
    return lastNumber * 2;
  }

  internal Color GetColorForValue(int value)
  {
    int index = BlockNumbers.IndexOf(value);
    if (index != -1)
    {
      return BlockColors[index];
    }
    else
    {
      return GenerateNewColor();
    }
  }

  Color GenerateNewColor()
  {
    float hue = BlockNumbers.Count * 0.1f % 1;
    return Color.HSVToRGB(hue, 0.8f, 1.0f);
  }

  internal void CheckAndExpandNumbers(int newValue)
  {
    if (!BlockNumbers.Contains(newValue))
    {
      BlockColors.Add(GetColorForValue(newValue));
      BlockNumbers.Add(newValue);
    }
  }

  void SetupBlock()
  {
    if (!initBlock)
    {
      initBlock = true;
      predictiedValue = 0;
      // predictiedValue = UnityEngine.Random.Range(0, 4);
      // predictiedValue = UnityEngine.Random.Range(0, BlockNumbers.Count);
    }

    int randomIndex = predictiedValue; // Use the predicted value
    predictiedValue = 0; // Generate next prediction
    // predictiedValue = UnityEngine.Random.Range(0, 4); // Generate next prediction
    // predictiedValue = UnityEngine.Random.Range(0, BlockNumbers.Count); // Generate next prediction

    predictedBlock.SetPredictedValue(predictiedValue);

    GameObject FirstBlock = Instantiate(m_blockPrefab, m_boardBlocks[2].Column[0].boardPosition.position, Quaternion.identity, m_newBlocksParent);
    _currMovingBlock = FirstBlock.GetComponent<BlockView>();
    _currMovingBlock.initBlockView(randomIndex);

    StartCoroutine(PullTheBlock(basePullDownSpeed));

    InputManager.Instance.setBlock(_currMovingBlock);
    BlockViews.Add(_currMovingBlock);
  }


  internal IEnumerator PullTheBlock(float duration)
  {
    KeyValuePair<Block, int> BlockData = GetMovableBlock();
    if (BlockData.Key == null)
    {
      Debug.Log("Game Over - No available space!");
      yield break;
    }
    _currBlockTween?.Kill();
    _currBlockTween = _currMovingBlock.transform.DOLocalMoveY(BlockData.Key.boardPosition.localPosition.y, duration)
    .OnComplete(() =>
    {
      activeMergeCheck++;
      _currMovingBlock.transform.localPosition = BlockData.Key.boardPosition.localPosition;
      StartCoroutine(OnBlockLanded(_currMovingBlock, BlockData.Value));
      StartCoroutine(WaitForGameLoop());
    });
  }


  private void PullTheBlock(float duration, BlockView blockView, KeyValuePair<Block, int> BlockData)
  {
    activeMergeCheck++;
    blockView.transform.DOLocalMoveY(BlockData.Key.boardPosition.localPosition.y, duration)
    .OnComplete(() =>
    {
      blockView.transform.localPosition = BlockData.Key.boardPosition.localPosition;
      StartCoroutine(OnBlockLanded(blockView, BlockData.Value));
    });
  }


  private IEnumerator WaitForGameLoop()
  {
    while (activeMergeCheck != 0)
    {
      yield return null;
    }
    SetupBlock();
  }

  internal IEnumerator OnBlockLanded(BlockView landedBlock, int rowIndex, int comboCount = 0)
  {
    int colIndex = InputManager.Instance.m_columnIndex;
    _currMovingBlock.OnLand(rowIndex, colIndex);
    m_boardBlocks[colIndex].Column[rowIndex].value = landedBlock.blockData.value;

    yield return new WaitForSeconds(0.2f);

    // 🔹 Downward Merge
    while (rowIndex < m_boardBlocks[colIndex].Column.Count - 1 &&
        m_boardBlocks[colIndex].Column[rowIndex + 1].value == landedBlock.blockData.value)
    {
      BlockView belowBlock = GetBlockViewAt(colIndex, rowIndex + 1);
      if (belowBlock != null)
      {
        int newValue = landedBlock.blockData.value * 2;
        CheckAndExpandNumbers(newValue);

        yield return belowBlock.MergeBlock(landedBlock);
        BlockViews.Remove(landedBlock);

        m_boardBlocks[colIndex].Column[rowIndex + 1].value = newValue;
        m_boardBlocks[colIndex].Column[rowIndex].value = 0;
        rowIndex++;
        landedBlock = BlockViews.FirstOrDefault(v => v.row == rowIndex && v.column == colIndex);
      }
    }

    yield return new WaitForSeconds(0.2f);
    BlockView leftBlock = GetBlockViewAt(colIndex - 1, rowIndex);
    BlockView rightBlock = GetBlockViewAt(colIndex + 1, rowIndex);
    bool leftBool = false;
    bool rightBool = false;
    if (leftBlock != null && leftBlock.blockData.value == landedBlock.blockData.value)
    {
      Debug.Log("called left bool");
      leftBool = true;
    }
    if (rightBlock != null && rightBlock.blockData.value == landedBlock.blockData.value)
    {
      Debug.Log("called right bool");
      rightBool = true;
    }
    int newVal = 0;
    bool merged = false;
    if (leftBool && rightBool)
    {
      merged = true;
      newVal = landedBlock.blockData.value * 4;
      CheckAndExpandNumbers(newVal);
      CheckAndExpandNumbers(landedBlock.blockData.value * 2);
      yield return landedBlock.MergeBlock(leftBlock, rightBlock);
      BlockViews.Remove(leftBlock);
      BlockViews.Remove(rightBlock);
      m_boardBlocks[leftBlock.column].Column[leftBlock.row].value = 0;
      m_boardBlocks[rightBlock.column].Column[rightBlock.row].value = 0;
    }
    else if (leftBool)
    {
      merged = true;
      newVal = landedBlock.blockData.value * 2;
      CheckAndExpandNumbers(newVal);
      yield return landedBlock.MergeBlock(leftBlock, true);
      BlockViews.Remove(leftBlock);
      m_boardBlocks[leftBlock.column].Column[leftBlock.row].value = 0;
    }
    else if (rightBool)
    {
      merged = true;
      newVal = landedBlock.blockData.value * 2;
      CheckAndExpandNumbers(newVal);
      yield return landedBlock.MergeBlock(rightBlock, true);
      BlockViews.Remove(rightBlock);
      m_boardBlocks[rightBlock.column].Column[rightBlock.row].value = 0;
    }

    yield return new WaitForSeconds(0.2f);
    if (merged)
    {
      m_boardBlocks[colIndex].Column[rowIndex].value = newVal;

      foreach (BlockView view in BlockViews)
      {
        if (view.row < m_boardBlocks[view.column].Column.Count - 1)
        {
          int lowestEmptyRow = -1;
          for (int i = 0; i < m_boardBlocks[view.column].Column.Count; i++)
          {
            if (m_boardBlocks[view.column].Column[i].value == 0)
            {
              lowestEmptyRow = i;
            }
          }

          if (lowestEmptyRow != -1 && lowestEmptyRow > view.row)
          {
            PullTheBlock(fastPullDownSpeed, view,
                new KeyValuePair<Block, int>(m_boardBlocks[view.column].Column[lowestEmptyRow], lowestEmptyRow));
          }
        }
      }
    }
    if (activeMergeCheck > 0)
    {
      activeMergeCheck--;
    }
  }


  private BlockView GetBlockViewAt(int col, int row)
  {
    if (col < 0 || col >= m_boardBlocks.Count) return null;

    foreach (BlockView view in BlockViews)
    {
      if (view.column == col && view.row == row)
      {
        return view;
      }
    }
    return null;
  }

  KeyValuePair<Block, int> GetMovableBlock()
  {
    for (int i = m_boardBlocks[InputManager.Instance.m_columnIndex].Column.Count - 1; i >= 0; i--)
    {
      int val = m_boardBlocks[InputManager.Instance.m_columnIndex].Column[i].value;
      if (val == 0)
      {
        return new KeyValuePair<Block, int>(m_boardBlocks[InputManager.Instance.m_columnIndex].Column[i], i);
      }
    }
    return new KeyValuePair<Block, int>(null, 0);
  }

  KeyValuePair<Block, int> GetMovableBlock(int col, int row)
  {
    Block block = null;
    int Row = 0;
    for (int i = row + 1; i < m_boardBlocks[col].Column.Count - 1; i++)
    {
      if (m_boardBlocks[col].Column[i].value == 0)
      {
        block = m_boardBlocks[col].Column[i];
        Row = i;
      }
    }
    if (block != null)
    {
      return new KeyValuePair<Block, int>(block, Row);
    }
    else
    {
      return new KeyValuePair<Block, int>(null, 0);
    }
  }
}

[Serializable]
public class BoardBlocks
{
  public List<Block> Column = new();
}

[Serializable]
public class Block
{
  public int value;
  public Transform boardPosition;

  public Block()
  {
    value = 0;
    boardPosition = null;
  }
}

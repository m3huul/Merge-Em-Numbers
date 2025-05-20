using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Linq;
using System.Data;

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
  private List<Coroutine> LandingBlockCoroutines = new();
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
      // predictiedValue = 0;
      predictiedValue = UnityEngine.Random.Range(0, 4);
      // predictiedValue = UnityEngine.Random.Range(0, BlockNumbers.Count);
    }

    int randomIndex = predictiedValue; // Use the predicted value
    predictiedValue = 0; // Generate next prediction
    predictiedValue = UnityEngine.Random.Range(0, 4); // Generate next prediction
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
      _currMovingBlock.transform.localPosition = BlockData.Key.boardPosition.localPosition;
      Coroutine routine = StartCoroutine(OnBlockLanded(_currMovingBlock, BlockData.Value, InputManager.Instance.m_columnIndex));
      StartCoroutine(WaitForGame(routine));
    });
  }


  private void PullTheBlock(float duration, BlockView blockView, Block blockToMove)
  {
    blockView.transform.DOLocalMoveY(blockToMove.boardPosition.localPosition.y, duration)
    .OnComplete(() =>
    {
      blockView.transform.localPosition = blockToMove.boardPosition.localPosition;
    });
  }


  private IEnumerator WaitForGame(Coroutine routine)
  {
    yield return routine;
    SetupBlock();
  }

  internal IEnumerator OnBlockLanded(BlockView landedBlock, int rowIndex, int colIndex = -1)
  {
    _currMovingBlock.OnLand(rowIndex, colIndex);
    m_boardBlocks[colIndex].Column[rowIndex].value = landedBlock.blockData.value;
    yield return new WaitForSeconds(0.4f);

    int newVal = 0;
    HashSet<int> affectedColumns = new HashSet<int>();
    bool merged = false;
    // 🔹 Downward Merge
    if (rowIndex < m_boardBlocks[colIndex].Column.Count - 1 &&
        m_boardBlocks[colIndex].Column[rowIndex + 1].value == landedBlock.blockData.value)
    {
      Debug.Log("RowIndex in downward merge : " + rowIndex);
      merged = true;
      BlockView belowBlock = GetBlockViewAt(colIndex, rowIndex + 1);
      if (belowBlock != null)
      {
        newVal = landedBlock.blockData.value * 2;
        CheckAndExpandNumbers(newVal);
        yield return belowBlock.MergeBlock(landedBlock);
        BlockViews.Remove(landedBlock);
        m_boardBlocks[colIndex].Column[rowIndex + 1].value = newVal;
        m_boardBlocks[colIndex].Column[rowIndex].value = 0;
        rowIndex++;
        landedBlock = BlockViews.FirstOrDefault(v => v.row == rowIndex && v.column == colIndex);
        affectedColumns.Add(colIndex);
      }
    }

    if (merged)
      yield return new WaitForSeconds(0.4f);

    BlockView leftBlock = GetBlockViewAt(colIndex - 1, rowIndex);
    BlockView rightBlock = GetBlockViewAt(colIndex + 1, rowIndex);
    bool leftBool = false;
    bool rightBool = false;
    if (leftBlock != null && leftBlock.blockData.value == landedBlock.blockData.value)
    {
      Debug.Log("called left bool, left val: " + leftBlock.blockData.value + " and curr block val: " + landedBlock.blockData.value);
      leftBool = true;
    }
    if (rightBlock != null && rightBlock.blockData.value == landedBlock.blockData.value)
    {
      Debug.Log("called right bool, right val: " + rightBlock.blockData.value + " and curr block val: " + landedBlock.blockData.value);
      rightBool = true;
    }
    if (leftBool && rightBool)
    {
      merged = true;
      newVal = landedBlock.blockData.value * 4;
      CheckAndExpandNumbers(newVal);
      CheckAndExpandNumbers(landedBlock.blockData.value * 2);
      affectedColumns.Add(leftBlock.column);
      affectedColumns.Add(rightBlock.column);
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
      affectedColumns.Add(leftBlock.column);
      CheckAndExpandNumbers(newVal);
      yield return landedBlock.MergeBlock(leftBlock, true);
      BlockViews.Remove(leftBlock);
      m_boardBlocks[leftBlock.column].Column[leftBlock.row].value = 0;
    }
    else if (rightBool)
    {
      merged = true;
      newVal = landedBlock.blockData.value * 2;
      affectedColumns.Add(rightBlock.column);
      CheckAndExpandNumbers(newVal);
      yield return landedBlock.MergeBlock(rightBlock, true);
      BlockViews.Remove(rightBlock);
      m_boardBlocks[rightBlock.column].Column[rightBlock.row].value = 0;
    }

    if (merged)
    {
      yield return new WaitForSeconds(0.4f);
      m_boardBlocks[colIndex].Column[rowIndex].value = newVal;

      List<Dictionary<int, int>> blocksAbove = new List<Dictionary<int, int>>();
      foreach (int col in affectedColumns)
      {
        Dictionary<int, int> BlocksAbove = GetBlocksAboveEmptyBlock(col); // row , col
        blocksAbove.Add(BlocksAbove);
        foreach (KeyValuePair<int, int> block in BlocksAbove)
        {
          BlockView view = GetBlockViewAt(block.Value, block.Key);
          if (view != null)
          {
            PullTheBlock(fastPullDownSpeed, view, m_boardBlocks[view.column].Column[view.row - 1]);
            yield return new WaitForSeconds(0.1f);
          }
        }
      }

      foreach (Dictionary<int, int> columnMap in blocksAbove)
      {
        foreach (KeyValuePair<int, int> pos in columnMap)
        {
          BlockView view = GetBlockViewAt(pos.Value, pos.Key);
          if (view != null)
          {
            if (IsMergeAvailable(pos.Key, pos.Value))
            {
              Debug.Log("Merge Available");
              yield return new WaitForSeconds(0.1f);
              yield return OnBlockLanded(view, pos.Key, pos.Value);
              yield break;
            }
          }
        }
      }
    }
  }

  bool IsMergeAvailable(int rowIndex, int colIndex)
  {
    BlockView blockView = GetBlockViewAt(colIndex, rowIndex);
    if (!blockView) {
      return false;
    }

    bool mergeAvailable = false;
    BlockView bottomBlock = GetBlockViewAt(colIndex, rowIndex+1);
    if(bottomBlock && bottomBlock.blockData.value == blockView.blockData.value){
      Debug.Log("Found Bottom Merge");
      mergeAvailable = true;
    }

    BlockView leftBlock = GetBlockViewAt(colIndex - 1, rowIndex);
    BlockView rightBlock = GetBlockViewAt(colIndex + 1, rowIndex);
    if (leftBlock != null && leftBlock.blockData.value == blockView.blockData.value)
    {
      Debug.Log("Found Left Merge");
      mergeAvailable = true;
    }
    if (rightBlock != null && rightBlock.blockData.value == blockView.blockData.value)
    {
      Debug.Log("Found Right Merge");
      mergeAvailable = true;
    }

    return mergeAvailable;
  }


  private BlockView GetBlockViewAt(int col, int row)
  {
    if (col < 0 || col >= m_boardBlocks.Count) return null;
    if(row < 0 || row >= m_boardBlocks[col].Column.Count) return null;

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

  Dictionary<int, int> GetBlocksAboveEmptyBlock(int col)
  {
    int rowIndex = -1;
    for (int i = m_boardBlocks[col].Column.Count - 1; i > 0; i--)
    {
      if (m_boardBlocks[col].Column[i - 1] != null || m_boardBlocks[col].Column[i - 1].value != 0)
      {
        continue;
      }
      else
      {
        if (m_boardBlocks[col].Column[i].value != 0)
        {
          rowIndex = i;
          break;
        }
      }
    }

    Dictionary<int, int> BlocksAbove = new Dictionary<int, int>();
    // foreach (BlockView view in BlockViews)
    // {
    //   if (view.column == col && view.row >= rowIndex)
    //   {
    //     BlocksAbove.Add(view.row, view.column);
    //   }
    // }
    for(int i=rowIndex;i>=0;i--){
      if(m_boardBlocks[col].Column[i].value != 0 && BlockViews.FirstOrDefault(v => v.row == i && v.column == col) != null){
        BlocksAbove.Add(i, col);
      }
    }
    
    return BlocksAbove;
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

using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Collections;
using UnityEngine.UI;

public class BoardManager : MonoBehaviour
{
  public static BoardManager Instance;
  [SerializeField] internal List<int> BlockNumbers = new();
  [SerializeField] internal List<Color> BlockColors = new();
  [SerializeField] internal float basePullDownSpeed = 3f;
  [SerializeField] internal float fastPullDownSpeed = 0.05f;
  [SerializeField] private float fallDuration = 0.05f;
  [SerializeField] private MergeData MergeData;
  internal int FocusedColumnIndex = 2;
  private Tween CurrBlockTween;
  [SerializeField] private Button RestartButton;
  void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
    }

    RestartButton.onClick.AddListener(() =>
    {
      RestartGame();
    });

    Time.timeScale = 1f;
    Application.targetFrameRate = 60;
  }

  void Start()
  {
    ExpandNumAndColorList();
  }

  internal void KillGame()
  {
    Debug.Log("No Block To Move To. Game End");
    InputManager.Instance.enabled = false;
    StopAllCoroutines();
    if (CurrBlockTween != null)
    {
      CurrBlockTween.Kill();
      CurrBlockTween = null;
    }
    DOTween.KillAll();

    RestartButton.gameObject.SetActive(true);
  }

  void RestartGame()
  {
    Debug.Log("Restarting Game");
    RestartButton.gameObject.SetActive(false);
    GridManager.Instance.Reset();
    SpawnManager.Instance.Reset();
  }

  int GenerateNextNumber()
  {
    int lastNumber = BlockNumbers[BlockNumbers.Count - 1];
    return lastNumber * 2;
  }

  void ExpandNumAndColorList()
  {
    for (int i = 0; i < 4; i++)
    {
      int newValue = GenerateNextNumber();
      BlockColors.Add(GetColorForValue(newValue));
      BlockNumbers.Add(newValue);
    }
  }

  internal Color GetColorForValue(int value)
  {
    int index = BlockNumbers.IndexOf(value);
    if (index != -1 && index < BlockColors.Count)
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
    float hue = BlockNumbers.Count * 0.15f % 1;
    return Color.HSVToRGB(hue, 0.8f, 1.0f);
  }

  internal int CheckAndExpandNumbers(int Value)
  {
    ExpandNumAndColorList();

    for (int i = 0; i < GridManager.Instance.BlockGrid.Count; i++)
    {
      for (int j = 0; j < GridManager.Instance.BlockGrid[i].Column.Count; j++)
      {
        int blockValue = GridManager.Instance.BlockGrid[i].Column[j].value;
        if (blockValue == 0)
          continue;
        if (!BlockNumbers.Contains(blockValue))
        {
          ExpandNumAndColorList();
          return BlockNumbers.IndexOf(Value);
        }
      }
    }

    return BlockNumbers.IndexOf(Value);
  }

  internal IEnumerator PullTheBlock(Block BlockToPull, BlockData MoveToBlock, float duration)
  {
    if (MoveToBlock == null)
    {
      Debug.Log("Game Over - No available space!");
      yield break;
    }
    // Debug.Log("Pulling block to: " + MoveToBlock.gridPosition.ToString());
    CurrBlockTween?.Kill();
    CurrBlockTween = BlockToPull.transform.DOMoveY(MoveToBlock.boardPosition.position.y, duration)
    .OnComplete(() =>
    {
      InputManager.Instance.BlockTransform = null;
      InputManager.Instance.enabled = false;
      // Debug.Log("Block Dropped at : " + MoveToBlock.gridPosition.ToString());
      BlockToPull.transform.position = MoveToBlock.boardPosition.position;
      GridManager.Instance.SetBlockDataOnTheGrid(BlockToPull, MoveToBlock.gridPosition);
      if (GridManager.Instance.CheckBlockForMerge(MoveToBlock.gridPosition, out MergeData))
      {
        StartCascade();
      }
      else
      {
        SpawnManager.Instance.SpawnNextBlock();
      }
    })
    .SetEase(Ease.Linear);
  }

  internal IEnumerator PullTheBlockDown(Block BlockToPull, BlockData BlockMoveTo, float duration)
  {
    yield return BlockToPull.transform.DOLocalMoveY(BlockMoveTo.boardPosition.localPosition.y, duration).SetEase(Ease.Linear).WaitForCompletion();
    GridManager.Instance.SetBlockDataOnTheGrid(BlockToPull, BlockMoveTo.gridPosition);
  }

  public void StartCascade()
  {
    StartCoroutine(CascadeRoutine());
  }

  private IEnumerator CascadeRoutine()
  {
    while (MergeData.Direction != MergeDirection.None)
    {
      yield return new WaitForSecondsRealtime(fallDuration);
      yield return Merge();
      yield return GridManager.Instance.MoveAllBlocksDown();
      bool cont = GridManager.Instance.CheckBlocksForMerge(out MergeData);
      // if (cont)
      // {
      //   Debug.Log("Cascade Continue");
      // }
    }

    SpawnManager.Instance.SpawnNextBlock();
  }

  IEnumerator Merge()
  {
    Block targetBlock = GridManager.Instance.BlockList.Find(b => b.GridPos == MergeData.TargetBlock.gridPosition);
    Block leftBlock = GridManager.Instance.BlockList.Find(b => b.GridPos == MergeData.LeftBlock?.gridPosition);
    Block rightBlock = GridManager.Instance.BlockList.Find(b => b.GridPos == MergeData.RightBlock?.gridPosition);
    Block bottomBlock = GridManager.Instance.BlockList.Find(b => b.GridPos == MergeData.BottomBlock?.gridPosition);
    switch (MergeData.Direction)
    {
      case MergeDirection.Left:
        yield return targetBlock.Merge(leftBlock);
        break;

      case MergeDirection.Right:
        yield return targetBlock.Merge(rightBlock);
        break;

      case MergeDirection.Bottom:
        yield return targetBlock.Merge(bottomBlock);
        break;

      case MergeDirection.LeftRight:
        yield return targetBlock.Merge(leftBlock, rightBlock);
        break;

      case MergeDirection.LeftBottom:
        yield return targetBlock.Merge(leftBlock, bottomBlock);
        break;

      case MergeDirection.RightBottom:
        yield return targetBlock.Merge(rightBlock, bottomBlock);
        break;

      case MergeDirection.LeftRightBottom:
        yield return targetBlock.Merge(leftBlock, rightBlock, bottomBlock);
        break;
    }
  }
}


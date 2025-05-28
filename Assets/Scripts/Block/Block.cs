using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

public class Block : MonoBehaviour
{
  [SerializeField] internal int Value;
  [SerializeField] private float startPosiYOffset = 100f;
  internal Vector2Int GridPos;
  private Image BlockImage;
  private TMP_Text BlockNumberText;

  private void Awake()
  {
    BlockImage = GetComponent<Image>();
    BlockNumberText = transform.GetChild(0).GetComponent<TMP_Text>();
  }

  internal void Init(int index)
  {
    Vector3 initPosi = GridManager.Instance.BlockGrid[InputManager.Instance.IColumnIndex].Column[0].boardPosition.position;
    Vector3 startPosi = initPosi + new Vector3(0, startPosiYOffset, 0);

    transform.position = startPosi;
    GridPos = new(InputManager.Instance.IColumnIndex, GridManager.Instance.BlockGrid[0].Column.Count-1);

    SetValue(index);
  }

  internal void SetValue(int index)
  {
    BlockImage.color = BoardManager.Instance.BlockColors[index];
    Value = BoardManager.Instance.BlockNumbers[index];
    BlockNumberText.text = BoardManager.Instance.BlockNumbers[index].ToString();
  }

  internal void SetGridPosition(Vector2Int pos)
  {
    GridPos = pos;
  }

  internal IEnumerator Merge(Block targetBlock)
  {
    targetBlock.BlockImage.DOFade(0, 0.1f).SetEase(Ease.Linear);
    yield return targetBlock.transform.DOLocalMove(transform.localPosition, 0.1f).SetEase(Ease.Linear).WaitForCompletion();

    GridManager.Instance.RemoveBlock(targetBlock.GridPos);

    Value *= 2;
    GridManager.Instance.SetBlockDataOnTheGrid(this, GridPos);

    int ValueIndex = BoardManager.Instance.BlockNumbers.IndexOf(Value);
    if (ValueIndex == -1)
    {
      ValueIndex = BoardManager.Instance.CheckAndExpandNumbers(Value);
    }
    SetValue(ValueIndex);
  }

  internal IEnumerator Merge(Block leftBlock, Block rightBlock)
  {
    leftBlock.BlockImage.DOFade(0, 0.1f).SetEase(Ease.Linear);
    rightBlock.BlockImage.DOFade(0, 0.1f).SetEase(Ease.Linear);
    leftBlock.transform.DOLocalMoveX(transform.localPosition.x, 0.1f);
    yield return rightBlock.transform.DOLocalMoveX(transform.localPosition.x, 0.1f).SetEase(Ease.Linear).WaitForCompletion();

    GridManager.Instance.RemoveBlock(leftBlock.GridPos);
    GridManager.Instance.RemoveBlock(rightBlock.GridPos);

    Value *= 4;
    GridManager.Instance.SetBlockDataOnTheGrid(this, GridPos);

    int ValueIndex = BoardManager.Instance.BlockNumbers.IndexOf(Value);
    if (ValueIndex == -1)
    {
      ValueIndex = BoardManager.Instance.CheckAndExpandNumbers(Value);
    }
    SetValue(ValueIndex);
  }

  internal IEnumerator Merge(Block leftBlock, Block rightBlock, Block bottomBlock)
  {
    leftBlock.BlockImage.DOFade(0, 0.1f).SetEase(Ease.Linear);
    rightBlock.BlockImage.DOFade(0, 0.1f).SetEase(Ease.Linear);
    bottomBlock.BlockImage.DOFade(0, 0.1f).SetEase(Ease.Linear);
    
    leftBlock.transform.DOLocalMoveX(transform.localPosition.x, 0.1f);
    rightBlock.transform.DOLocalMoveX(transform.localPosition.x, 0.1f).SetEase(Ease.Linear).WaitForCompletion();
    yield return bottomBlock.transform.DOLocalMoveY(transform.localPosition.y, 0.1f).SetEase(Ease.Linear).WaitForCompletion();
    
    GridManager.Instance.RemoveBlock(leftBlock.GridPos);
    GridManager.Instance.RemoveBlock(rightBlock.GridPos);
    GridManager.Instance.RemoveBlock(bottomBlock.GridPos);
    
    Value *= 4;
    GridManager.Instance.SetBlockDataOnTheGrid(this, GridPos);

    int ValueIndex = BoardManager.Instance.BlockNumbers.IndexOf(Value);
    if (ValueIndex == -1)
    {
      ValueIndex = BoardManager.Instance.CheckAndExpandNumbers(Value);
    }
    SetValue(ValueIndex);
  }
}
 

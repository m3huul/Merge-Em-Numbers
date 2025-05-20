using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using DG.Tweening;

[Serializable]
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
    GridPos = new();

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
  
  internal IEnumerator MergeBlock(Block Block, bool horizontal = false)
  {
    if (!horizontal)
    {
      BlockImage.DOFade(0, 0.1f);
      yield return Block.transform.DOLocalMoveY(transform.localPosition.y, 0.1f).WaitForCompletion();
      GridManager.Instance.RemoveBlock(Block.GridPos);
      Destroy(Block.gameObject);
      Value *= 2;
      GridManager.Instance.SetBlockData(this, GridPos, Value);
      SetValue(BoardManager.Instance.BlockNumbers.IndexOf(Value));
    }
    else
    {
      Block.BlockImage.DOFade(0, 0.1f);
      yield return Block.transform.DOLocalMoveX(transform.localPosition.x, 0.1f).WaitForCompletion();
      GridManager.Instance.RemoveBlock(Block.GridPos);
      Destroy(Block.gameObject);
      Value *= 2;
      GridManager.Instance.SetBlockData(this, GridPos, Value);
      SetValue(BoardManager.Instance.BlockNumbers.IndexOf(Value));
    }
  }

  internal IEnumerator MergeBlock(Block Block1, Block Block2)
  {
    BlockImage.DOFade(0, 0.1f);
    BlockImage.DOFade(0, 0.1f);
    Block1.transform.DOLocalMoveX(transform.localPosition.x, 0.1f);
    yield return Block2.transform.DOLocalMoveX(transform.localPosition.x, 0.1f).WaitForCompletion();
    Destroy(Block1.gameObject);
    Destroy(Block2.gameObject);
    GridManager.Instance.RemoveBlock(Block1.GridPos);
    GridManager.Instance.RemoveBlock(Block2.GridPos);
    Value *= 4;
    SetValue(BoardManager.Instance.BlockNumbers.IndexOf(Value));
    GridManager.Instance.SetBlockData(this, GridPos, Value);
  }
}
 

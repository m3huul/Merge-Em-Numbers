using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public class BlockView : MonoBehaviour
{
    [SerializeField] internal Block blockData = new();
    [SerializeField] internal int row;
    [SerializeField] internal int column;
    private TMP_Text m_numText;
    internal Image m_image;
    internal bool Landed = false;
    void Awake()
    {
        m_numText = transform.GetChild(0).GetComponent<TMP_Text>();
        m_image = GetComponent<Image>();
    }

    internal void initBlockView(int index)
    {
        Vector3 initPosi = BoardManager.Instance.m_boardBlocks[InputManager.Instance.m_columnIndex].Column[0].boardPosition.position;
        Vector3 StartPosi = initPosi + new Vector3(0, 200, 0);

        transform.position = StartPosi;
        blockData.boardPosition = transform;

        //Value Calc here
        m_image.color = BoardManager.Instance.BlockColors[index];
        blockData.value = BoardManager.Instance.BlockNumbers[index];
        UpdateBlockText();
    }

    internal void SetPredictedValue(int index)
    {
        m_image.color = BoardManager.Instance.BlockColors[index];
        m_numText.text = BoardManager.Instance.BlockNumbers[index].ToString();
    }

    internal void OnLand(int Row, int Col)
    {
        Landed = true;
        row = Row;
        column = Col;
    }

    internal IEnumerator MergeBlock(BlockView Block, bool horizontal = false)
    {
        if (!horizontal)
        {
            Block.m_image.DOFade(0, 0.1f);
            yield return Block.transform.DOLocalMoveY(transform.localPosition.y, 0.1f).WaitForCompletion();
            Destroy(Block.gameObject);
            blockData.value *= 2;
            m_image.color = BoardManager.Instance.GetColorForValue(blockData.value);
            UpdateBlockText();
        }
        else
        {
            Block.m_image.DOFade(0, 0.1f);
            yield return Block.transform.DOLocalMoveX(transform.localPosition.x, 0.1f).WaitForCompletion();
            Destroy(Block.gameObject);
            blockData.value *= 2;
            m_image.color = BoardManager.Instance.GetColorForValue(blockData.value);
            UpdateBlockText();
        }
    }

    internal IEnumerator MergeBlock(BlockView Block1, BlockView Block2)
    {
        Block1.m_image.DOFade(0, 0.1f);
        Block2.m_image.DOFade(0, 0.1f);
        Block1.transform.DOLocalMoveX(transform.localPosition.x, 0.1f);
        yield return Block2.transform.DOLocalMoveX(transform.localPosition.x, 0.1f).WaitForCompletion();
        Destroy(Block1.gameObject);
        Destroy(Block2.gameObject);
        blockData.value *= 4;
        m_image.color = BoardManager.Instance.GetColorForValue(blockData.value);
        UpdateBlockText();
    }

    void UpdateBlockText()
    {
        m_numText.text = blockData.value.ToString();
    }

    internal bool TryMerge(BlockView otherBlock)
    {
        if (blockData.value == otherBlock.blockData.value) 
        {
          return true;
        }
        return false;
    }
}

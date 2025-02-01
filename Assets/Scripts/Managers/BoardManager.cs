using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Collections;


public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance;
    // [SerializeField] private KeyValuePair<int, float> PossibleValues=new(); //Make my own datatype
    [SerializeField] internal List<BoardBlocks> m_boardBlocks = new();
    [SerializeField] internal List<int> BlockNumbers = new();
    [SerializeField] internal List<Color> BlockColors = new();
    [SerializeField] internal Gradient colorGradient;
    [SerializeField] private List<BlockView> BlockViews = new();
    [SerializeField] private Transform m_newBlocksParent;
    [SerializeField] private GameObject m_blockPrefab;
    [SerializeField] private float basePullDownSpeed = 15f;
    [SerializeField] internal float fastPullDownSpeed = 0.2f;
    private BlockView _currMovingBlock;
    private Tween _currBlockTween;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Start()
    {
        SetupBlock();
    }

    void SetupBlock()
    {
        GameObject FirstBlock = Instantiate(m_blockPrefab, m_boardBlocks[2].Column[0].boardPosition.position, m_boardBlocks[2].Column[0].boardPosition.rotation, m_newBlocksParent);
        _currMovingBlock = FirstBlock.GetComponent<BlockView>();                        //Test block instantiation
        _currMovingBlock.initBlockView();

        PullTheBlock(basePullDownSpeed);

        InputManager.Instance.setBlock(_currMovingBlock);
        BlockViews.Add(_currMovingBlock);
    }

    internal void PullTheBlock(float duration)
    {
        KeyValuePair<Block, int> BlockData = GetMovableBlock();
        if (BlockData.Key == null)
        {
            Debug.LogError("Couldnt find a block to go to");
            return;
        }
        _currBlockTween?.Kill();
        _currBlockTween = _currMovingBlock.transform.DOLocalMoveY(BlockData.Key.boardPosition.localPosition.y, duration)
        .OnComplete(() =>
        {
            _currMovingBlock.transform.localPosition = BlockData.Key.boardPosition.localPosition;
            StartCoroutine(OnBlockLanded(BlockData.Key, BlockData.Value));
        });
    }

    IEnumerator OnBlockLanded(Block LandedBlock, int rowIndex)
    {
        _currMovingBlock.OnLand(rowIndex, InputManager.Instance.m_columnIndex);
        LandedBlock.value = _currMovingBlock.blockData.value;
        yield return new WaitForSeconds(0.2f);
        if (m_boardBlocks[0].Column.Count - 1 != rowIndex)
        {
            if (m_boardBlocks[InputManager.Instance.m_columnIndex].Column[rowIndex + 1].value == LandedBlock.value)
            {
                foreach (BlockView block in BlockViews)
                {
                    if (block.column == InputManager.Instance.m_columnIndex && block.row == rowIndex + 1)
                    {
                        yield return block.MergeBlock(_currMovingBlock);
                        BlockViews.Remove(_currMovingBlock);
                        m_boardBlocks[InputManager.Instance.m_columnIndex].Column[rowIndex + 1].value *= 2;
                        LandedBlock.value = 0;
                        break;
                    }
                }
            }
        }
        else
        {

        }

        SetupBlock();
    }

    internal void AddNewBlockValue(int newValue)
    {
        BlockNumbers.Add(newValue);

        // Assign a new color dynamically
        float t = (float)BlockNumbers.Count / (BlockNumbers.Count + 5); // Keep it scaled smoothly
        Color newColor = colorGradient.Evaluate(t);
        BlockColors.Add(newColor);
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
using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Collections;


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
        return lastNumber * 2;  // Next value is double the last one
    }

    internal Color GetColorForValue(int value)
    {
        int index = BlockNumbers.IndexOf(value);
        if (index != -1)
        {
            return BlockColors[index];  // If color already exists
        }
        else
        {
            // float t = Mathf.Log(value) / Mathf.Log(2048);  // Normalize in log scale
            // return colorGradient.Evaluate(t);
            return GenerateNewColor();
        }
    }

    Color GenerateNewColor()
    {
        float hue = BlockNumbers.Count * 0.1f % 1;  // Gradually change hue
        return Color.HSVToRGB(hue, 0.8f, 1.0f);  // Vibrant colors
    }

    internal void CheckAndExpandNumbers(int newValue)
    {
        if (!BlockNumbers.Contains(newValue))
        {
            BlockColors.Add(GetColorForValue(newValue));  // Auto-assign color
            BlockNumbers.Add(newValue);
        }
    }

    void SetupBlock()
    {
        int randomIndex;
        if (predictiedValue == 0)
        {
            randomIndex = UnityEngine.Random.Range(0, BoardManager.Instance.BlockNumbers.Count);
            predictiedValue = UnityEngine.Random.Range(0, BoardManager.Instance.BlockNumbers.Count);
        }
        else
        {
            randomIndex = predictiedValue;
            predictiedValue = UnityEngine.Random.Range(0, BoardManager.Instance.BlockNumbers.Count);
        }
        predictedBlock.SetPredictedValue(predictiedValue);

        GameObject FirstBlock = Instantiate(m_blockPrefab, m_boardBlocks[2].Column[0].boardPosition.position, m_boardBlocks[2].Column[0].boardPosition.rotation, m_newBlocksParent);
        _currMovingBlock = FirstBlock.GetComponent<BlockView>();                        //Test block instantiation
        _currMovingBlock.initBlockView(randomIndex);

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
                        int newValue = m_boardBlocks[InputManager.Instance.m_columnIndex].Column[rowIndex + 1].value *= 2;
                        CheckAndExpandNumbers(newValue);
                        yield return block.MergeBlock(_currMovingBlock);
                        BlockViews.Remove(_currMovingBlock);
                        m_boardBlocks[InputManager.Instance.m_columnIndex].Column[rowIndex + 1].value = newValue;
                        LandedBlock.value = 0;
                        break;
                    }
                }
            }
            else if (m_boardBlocks[InputManager.Instance.m_columnIndex + 1].Column[rowIndex]?.value == LandedBlock.value
                && m_boardBlocks[InputManager.Instance.m_columnIndex - 1].Column[rowIndex]?.value == LandedBlock.value)
            {
                BlockView view1 = null;
                BlockView view2 = null;
                foreach (BlockView view in BlockViews)
                {
                    if (view.column == InputManager.Instance.m_columnIndex + 1 && view.row == rowIndex)
                    {
                        view1 = view;
                    }
                    if (view.column == InputManager.Instance.m_columnIndex - 1 && view.row == rowIndex)
                    {
                        view2 = view;
                    }
                }
                if (view1 == null || view2 == null)
                {
                    Debug.LogError("Couldnt find block views");
                    yield break;
                }
                int newValue = _currMovingBlock.blockData.value *= 4;
                CheckAndExpandNumbers(newValue);
                yield return _currMovingBlock.MergeBlock(view1, view2);
                BlockViews.Remove(view1);
                BlockViews.Remove(view2);
                m_boardBlocks[InputManager.Instance.m_columnIndex].Column[rowIndex].value = newValue;
            }
        }
        else
        {

        }
        yield return new WaitForSeconds(0.2f);
        SetupBlock();
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
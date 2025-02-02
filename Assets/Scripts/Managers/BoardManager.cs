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
        int randomIndex;
        if (predictiedValue == 0)
        {
            randomIndex = UnityEngine.Random.Range(0, BlockNumbers.Count);
            predictiedValue = UnityEngine.Random.Range(0, BlockNumbers.Count);
        }
        else
        {
            randomIndex = predictiedValue;
            predictiedValue = UnityEngine.Random.Range(0, BlockNumbers.Count);
        }
        predictedBlock.SetPredictedValue(predictiedValue);

        GameObject FirstBlock = Instantiate(m_blockPrefab, m_boardBlocks[2].Column[0].boardPosition.position, Quaternion.identity, m_newBlocksParent);
        _currMovingBlock = FirstBlock.GetComponent<BlockView>();
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
            Debug.Log("Game Over - No available space!");
            return;
        }

        _currBlockTween?.Kill();
        _currBlockTween = _currMovingBlock.transform.DOLocalMoveY(BlockData.Key.boardPosition.localPosition.y, duration)
        .OnComplete(() =>
        {
            _currMovingBlock.transform.localPosition = BlockData.Key.boardPosition.localPosition;
            StartCoroutine(OnBlockLanded(_currMovingBlock, BlockData.Value));
        });
    }

    internal IEnumerator OnBlockLanded(BlockView landedBlock, int rowIndex, int comboCount = 0)
    {
        int colIndex = InputManager.Instance.m_columnIndex;
        _currMovingBlock.OnLand(rowIndex, colIndex);
        m_boardBlocks[colIndex].Column[rowIndex].value = landedBlock.blockData.value;

        yield return new WaitForSeconds(0.2f);

        bool merged = false;

        // 🔹 Downward Merge
        if (rowIndex < m_boardBlocks[colIndex].Column.Count - 1 &&
            m_boardBlocks[colIndex].Column[rowIndex + 1].value == landedBlock.blockData.value)
        {
            BlockView belowBlock = GetBlockViewAt(colIndex, rowIndex + 1);
            if (belowBlock != null)
            {
                int newValue = landedBlock.blockData.value * 2;
                CheckAndExpandNumbers(newValue);

                yield return belowBlock.MergeBlock(_currMovingBlock);
                BlockViews.Remove(_currMovingBlock);

                m_boardBlocks[colIndex].Column[rowIndex + 1].value = newValue;
                m_boardBlocks[colIndex].Column[rowIndex].value = 0;

                comboCount++;
                merged = true;
                yield return StartCoroutine(OnBlockLanded(belowBlock, rowIndex + 1, comboCount));
            }
        }

        // 🔹 Left and Right Merge
        BlockView leftBlock = GetBlockViewAt(colIndex - 1, rowIndex);
        BlockView rightBlock = GetBlockViewAt(colIndex + 1, rowIndex);
        bool mergedLR = false;
        if (leftBlock != null && leftBlock.blockData.value == landedBlock.blockData.value)
        {
            int newValue = landedBlock.blockData.value * 2;
            CheckAndExpandNumbers(newValue);

            // Merge with the left block
            yield return _currMovingBlock.MergeBlock(leftBlock);
            BlockViews.Remove(leftBlock);
            m_boardBlocks[leftBlock.column].Column[leftBlock.row].value = 0;

            m_boardBlocks[colIndex].Column[rowIndex].value = newValue;
            comboCount++;
            mergedLR = true;
        }

        if (rightBlock != null && rightBlock.blockData.value == landedBlock.blockData.value)
        {
            int newValue = landedBlock.blockData.value * 2;
            CheckAndExpandNumbers(newValue);

            // Merge with the right block
            yield return _currMovingBlock.MergeBlock(rightBlock);
            BlockViews.Remove(rightBlock);
            m_boardBlocks[rightBlock.column].Column[rightBlock.row].value = 0;

            m_boardBlocks[colIndex].Column[rowIndex].value = newValue;
            comboCount++;
            mergedLR = true;
        }

        if (mergedLR)
        {
            yield return StartCoroutine(OnBlockLanded(_currMovingBlock, rowIndex, comboCount));
        }
        else if (!merged)
        {
            // No merge, move to the next setup block
            SetupBlock();
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

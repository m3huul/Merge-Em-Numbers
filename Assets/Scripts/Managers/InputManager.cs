using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public static InputManager Instance;
    [SerializeField] internal int m_columnIndex = 2;
    [SerializeField] private BlockView m_blockTransform;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    internal void setBlock(BlockView block)
    {
        m_blockTransform = block;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnUserInput(eventData);
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (m_blockTransform)
        {
            m_blockTransform = null;
            BoardManager.Instance.PullTheBlock(BoardManager.Instance.fastPullDownSpeed);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        OnUserInput(eventData);
    }

    private void OnUserInput(PointerEventData eventData)
    {
        if (m_blockTransform != null)
        {
            Transform closestColumnPosition = BoardManager.Instance.m_boardBlocks[FindClosestColumnIndex(eventData.position.x)].Column[0].boardPosition;

            // Move the block to the closest column's X position if found
            if (closestColumnPosition != null)
            {
                Vector3 targetPosition = m_blockTransform.transform.position;
                targetPosition.x = closestColumnPosition.position.x;
                m_blockTransform.transform.position = targetPosition;
            }
        }
    }

    private int FindClosestColumnIndex(float xPosition)
    {
        float closestDistance = float.MaxValue;
        int closestIndex = 0;

        for (int i = 0; i < BoardManager.Instance.m_boardBlocks.Count; i++)
        {
            float columnX = BoardManager.Instance.m_boardBlocks[i].Column[0].boardPosition.position.x;
            float distance = Mathf.Abs(xPosition - columnX);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }
        m_columnIndex = closestIndex;
        return closestIndex;
    }
}

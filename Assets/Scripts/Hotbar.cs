using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Hotbar : MonoBehaviour
{
    private static Hotbar instance;

    public static Hotbar Instance
    {
        get
        {
            if (!instance)
            {
                instance = FindObjectOfType<Hotbar>();
            }
            return instance;
        }
    }

    [SerializeField]
    private List<BlockType> slots;

    [SerializeField]
    private float indicatorStep = 80;

    [SerializeField]
    private float indicatorMinX = -360;

    [SerializeField]
    private float indicatorMinY = 0;

    private int selectionIdx;

    [SerializeField]
    private RawImage selectionIndicator;

    private void Start()
    {
        selectionIdx = 0;
        UpdateSelectionIndicator();
    }

    private void Update()
    {
        var scroll =Input.GetAxis("Mouse ScrollWheel");
        if (scroll == 0) return;
        if (scroll > 0)
        {
            selectionIdx++;
        }
        else
        {
            selectionIdx--;
        }
        if (selectionIdx >= slots.Count)
        {
            selectionIdx = 0;
        }
        else if (selectionIdx < 0)
        {
            selectionIdx = slots.Count - 1;
        }
        UpdateSelectionIndicator();
    }


    public BlockType GetSelected()
    {
        return slots[selectionIdx];
    }

    private void OnValidate()
    {
        if (!selectionIndicator) return;

        selectionIdx = 0;
        UpdateSelectionIndicator();
    }

    private void UpdateSelectionIndicator()
    {
        var selectionPos = selectionIndicator.transform.localPosition;
        var x = selectionIdx % BlockDictionary.TILESET_DIMENSIONS;
        var y = selectionIdx / BlockDictionary.TILESET_DIMENSIONS;
        selectionPos.x = indicatorMinX + x * indicatorStep;
        selectionPos.y = indicatorMinY + y * indicatorStep;
        selectionIndicator.transform.localPosition = selectionPos;
    }
}

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New UnlockNode", menuName = "Unlock/UnlockNodeData")]

public class UnlockNodeData : ScriptableObject
{
    public string nodeId;

    public string displayName;

    public ItemData requiredItem;

    public int cost;

    public Sprite unlockedIcon;

    public List<UnlockNodeData> prerequisites;

}
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerToolInventory : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save list of owned tools, levels and durability.")]
    [SerializeField] List<PlayerToolState> tools = new List<PlayerToolState>();

    public IReadOnlyList<PlayerToolState> Tools => tools;
    public event Action<PlayerToolState> OnToolChanged;

    public bool HasTool(ToolDefinition tool, int requiredLevel = 1, int requiredDurability = 1) {
        var state = GetTool(tool);
        return state != null
            && state.level >= Mathf.Max(1, requiredLevel)
            && state.durability >= Mathf.Max(0, requiredDurability);
    }

    public PlayerToolState GetTool(ToolDefinition tool) {
        if(tool == null) {
            return null;
        }

        return tools.FirstOrDefault(t => t.toolId == tool.Id);
    }

    public void AddOrRepairTool(ToolDefinition tool, int level = 1, int durability = -1) {
        if(tool == null) {
            return;
        }

        var state = GetTool(tool);
        if(state == null) {
            state = new PlayerToolState() {
                toolId = tool.Id,
                level = Mathf.Clamp(level, 1, tool.MaxLevel),
                durability = durability < 0 ? tool.MaxDurability : Mathf.Clamp(durability, 0, tool.MaxDurability)
            };
            tools.Add(state);
        } else {
            state.level = Mathf.Clamp(Mathf.Max(state.level, level), 1, tool.MaxLevel);
            state.durability = durability < 0 ? tool.MaxDurability : Mathf.Clamp(state.durability + durability, 0, tool.MaxDurability);
        }

        OnToolChanged?.Invoke(state);
    }

    public bool ConsumeDurability(ToolDefinition tool, int amount) {
        if(tool == null || amount <= 0) {
            return true;
        }

        var state = GetTool(tool);
        if(state == null || state.durability < amount) {
            return false;
        }

        state.durability = Mathf.Max(0, state.durability - amount);
        OnToolChanged?.Invoke(state);
        return true;
    }

    public object CaptureState() {
        return new PlayerToolInventorySaveData() {
            tools = tools.Select(t => new PlayerToolStateSaveData() {
                toolId = t.toolId,
                level = t.level,
                durability = t.durability
            }).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerToolInventorySaveData;
        if(saveData == null) {
            return;
        }

        tools = saveData.tools?.Select(t => new PlayerToolState() {
            toolId = t.toolId,
            level = Mathf.Max(1, t.level),
            durability = Mathf.Max(0, t.durability)
        }).ToList() ?? new List<PlayerToolState>();
    }
}

[Serializable]
public class PlayerToolState {
    [Tooltip("Saved tool definition id.")]
    public string toolId;
    [Tooltip("Current tool upgrade level.")]
    [Min(1)]
    public int level = 1;
    [Tooltip("Current remaining durability.")]
    [Min(0)]
    public int durability;
}

[Serializable]
public class PlayerToolInventorySaveData {
    public List<PlayerToolStateSaveData> tools;
}

[Serializable]
public class PlayerToolStateSaveData {
    public string toolId;
    public int level;
    public int durability;
}

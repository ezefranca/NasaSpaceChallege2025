using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class MaterialDef {
    public string id, name, @class;
    public float density_kg_m3;
    public float melt_C, cp_kJ_kgK, latent_kJ_kg;
    public float baseline_strength_MPa;
    public string[] notes;
    public string[] suitableProcesses;
    public string[] sourceWasteIds;
}

[Serializable] public class WasteKey {
    public string material;
    public float massPct;
}

[Serializable] public class WasteItemDef {
    public string id, name, category;
    public int difficultyFactor;
    public WasteKey[] keyMaterials;
}

[Serializable] public class ProcessDef {
    public string id, name;
}

[Serializable] public class Requirements {
    public float minStrength_MPa;
    public float tempLow_C;
    public float tempHigh_C;
}

[Serializable] public class TaskDef {
    public string id, title;
    public Requirements requirements;
    public string[] preferredProcesses;
    public string[] candidateMaterials;
}

[Serializable] public class ManufacturingDB {
    public MaterialDef[] materials;
    public WasteItemDef[] wasteItems;
    public ProcessDef[] processes;
    public TaskDef[] manufacturingTasks;

    Dictionary<string, MaterialDef> _matIdx;
    Dictionary<string, ProcessDef>  _procIdx;
    Dictionary<string, TaskDef>     _taskIdx;

    public void BuildIndexes() {
        _matIdx  = materials?.ToDictionary(m => m.id) ?? new();
        _procIdx = processes?.ToDictionary(p => p.id) ?? new();
        _taskIdx = manufacturingTasks?.ToDictionary(t => t.id) ?? new();
    }

    public MaterialDef Mat(string id) => (_matIdx != null && _matIdx.TryGetValue(id, out var m)) ? m : null;
    public ProcessDef  Proc(string id) => (_procIdx != null && _procIdx.TryGetValue(id, out var p)) ? p : null;
    public TaskDef     Task(string id) => (_taskIdx != null && _taskIdx.TryGetValue(id, out var t)) ? t : null;
}
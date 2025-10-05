using UnityEngine;

public class ManufacturingConsole : MonoBehaviour
{
    public ManufacturingDB DB { get; private set; }

    void Awake()
    {
        try
        {
            DB = JsonUtility.FromJson<ManufacturingDB>(EmbeddedJson());
            if (DB == null) throw new System.Exception("JsonUtility returned null.");
            DB.BuildIndexes();
            Debug.Log($"ManufacturingConsole: Embedded DB loaded with {DB.materials.Length} materials.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"ManufacturingConsole: Failed to load embedded DB. {ex.Message}");
        }
    }

    // Evaluator (keeps your simulation working)
    public MfgModels.EvalResult Evaluate(
        string taskId, string materialId, string processId,
        float partMass_kg, bool crossLayer, bool reinforcedCF, int recycleCycles)
    {
        if (DB == null)
            return new MfgModels.EvalResult { explanation = "DB not loaded." };

        var task = DB.Task(taskId);
        var mat  = DB.Mat(materialId);
        if (task == null || mat == null)
            return new MfgModels.EvalResult { explanation = $"Bad IDs: task={taskId}, mat={materialId}" };

        return MfgModels.Evaluate(task, mat, processId, partMass_kg,
                                  crossLayer, recycleCycles,
                                  fdminfill: 0.25f, targetPrint_C_override: -1f,
                                  reinforcedCF: reinforcedCF,
                                  foamForming: (processId=="Thermal_Bonding" || processId=="Hot_Forming"));
    }

    // ---- EMBEDDED JSON ----
    string EmbeddedJson() => @"
{
  ""materials"": [
    { ""id"": ""PE"", ""name"": ""Polyethylene"", ""class"": ""polymer"", ""density_kg_m3"": 950, ""melt_C"": 130, ""cp_kJ_kgK"": 1.9, ""latent_kJ_kg"": 230, ""baseline_strength_MPa"": 20,
      ""notes"": [""Low melt; easy to re-extrude; good for compounding with regolith or chopped CF""],
      ""suitableProcesses"": [""FDM_FFF"", ""Reinforced_Polymer_Printing"", ""BinderJet_Regolith""],
      ""sourceWasteIds"": [""air_pillow_film"", ""bubble_wrap"", ""reclosable_bags"", ""drink_pouch"", ""food_overwrap"", ""anti_static_bags"", ""plastazote_ld45_fr""] },

    { ""id"": ""NYLON"", ""name"": ""Nylon (Polyamide)"", ""class"": ""polymer"", ""density_kg_m3"": 1150, ""melt_C"": 220, ""cp_kJ_kgK"": 1.6, ""latent_kJ_kg"": 200, ""baseline_strength_MPa"": 50,
      ""notes"": [""High strength and abrasion resistance; good across Martian temperature swings""],
      ""suitableProcesses"": [""FDM_FFF"", ""SLS"", ""Reinforced_Polymer_Printing""],
      ""sourceWasteIds"": [""rehydratable_pouch"", ""ctb_nomex_blend"", ""clothing_mix""] },

    { ""id"": ""AL"", ""name"": ""Aluminum"", ""class"": ""metal"", ""density_kg_m3"": 2700, ""melt_C"": 660, ""cp_kJ_kgK"": 0.9, ""latent_kJ_kg"": 400, ""baseline_strength_MPa"": 150,
      ""notes"": [""High stiffness-to-weight; remelting/SLM are energy intensive""],
      ""suitableProcesses"": [""SLM"", ""CNC_Remelt_Form""],
      ""sourceWasteIds"": [""aluminum_struts"", ""food_overwrap"", ""drink_pouch""] },

    { ""id"": ""CFRP"", ""name"": ""Carbon Fiber Composite"", ""class"": ""composite"", ""density_kg_m3"": 1200, ""melt_C"": 0, ""cp_kJ_kgK"": 1.4, ""latent_kJ_kg"": 0, ""baseline_strength_MPa"": 120,
      ""notes"": [""Recovered fibers reinforcing recycled polymers; excellent stiffness/mass""],
      ""suitableProcesses"": [""Reinforced_Polymer_Printing"", ""Layup_Cure""],
      ""sourceWasteIds"": [""polymer_matrix_composites""] },

    { ""id"": ""PVDF_FOAM"", ""name"": ""PVDF Foam (Zotek F30)"", ""class"": ""foam"", ""density_kg_m3"": 30, ""melt_C"": 170, ""cp_kJ_kgK"": 1.25, ""latent_kJ_kg"": 90, ""baseline_strength_MPa"": 1,
      ""notes"": [""Excellent insulation; FR variants; for pads/liners; thermal bonding""],
      ""suitableProcesses"": [""Thermal_Bonding"", ""Hot_Forming""],
      ""sourceWasteIds"": [""zotek_f30""] },

    { ""id"": ""EVA_FOAM"", ""name"": ""EVA Foam (Plastazote LD45 FR)"", ""class"": ""foam"", ""density_kg_m3"": 45, ""melt_C"": 100, ""cp_kJ_kgK"": 2.0, ""latent_kJ_kg"": 140, ""baseline_strength_MPa"": 1,
      ""notes"": [""Light padding; thermal breaks; easy to form""],
      ""suitableProcesses"": [""Thermal_Bonding"", ""Hot_Forming""],
      ""sourceWasteIds"": [""plastazote_ld45_fr""] }
  ],
  ""processes"": [
    { ""id"": ""FDM_FFF"", ""name"": ""Fused Deposition Modeling"" },
    { ""id"": ""SLS"", ""name"": ""Selective Laser Sintering"" },
    { ""id"": ""BinderJet_Regolith"", ""name"": ""Binder Jetting (Regolith Mix)"" },
    { ""id"": ""Reinforced_Polymer_Printing"", ""name"": ""Recycled Polymer + CF Reinforcement"" },
    { ""id"": ""CNC_Remelt_Form"", ""name"": ""Melt & Form / CNC"" },
    { ""id"": ""Thermal_Bonding"", ""name"": ""Thermal Bonding (Foams)"" },
    { ""id"": ""Hot_Forming"", ""name"": ""Hot Forming (Foams)"" }
  ],
  ""manufacturingTasks"": [
    { ""id"": ""repair_rover_gear"", ""title"": ""Replace Rover Gear"", ""requirements"": { ""minStrength_MPa"": 60, ""tempLow_C"": -80, ""tempHigh_C"": 40 },
      ""preferredProcesses"": [""SLS"", ""Reinforced_Polymer_Printing""], ""candidateMaterials"": [""NYLON"", ""CFRP""] },
    { ""id"": ""hab_bracket"", ""title"": ""Habitat Panel Bracket"", ""requirements"": { ""minStrength_MPa"": 30, ""tempLow_C"": -60, ""tempHigh_C"": 30 },
      ""preferredProcesses"": [""FDM_FFF"", ""Reinforced_Polymer_Printing""], ""candidateMaterials"": [""PE"", ""CFRP""] },
    { ""id"": ""beam_reinforcement"", ""title"": ""Reinforce Structural Beam"", ""requirements"": { ""minStrength_MPa"": 120, ""tempLow_C"": -80, ""tempHigh_C"": 40 },
      ""preferredProcesses"": [""SLM"", ""CNC_Remelt_Form""], ""candidateMaterials"": [""AL""] },
    { ""id"": ""insulation_panel"", ""title"": ""Interior Insulation Panel"", ""requirements"": { ""minStrength_MPa"": 2, ""tempLow_C"": -40, ""tempHigh_C"": 30 },
      ""preferredProcesses"": [""Thermal_Bonding"", ""Hot_Forming""], ""candidateMaterials"": [""PVDF_FOAM"", ""EVA_FOAM""] }
  ]
}";
}
using UnityEngine;

public static class MfgModels
{
    // ---------- GLOBAL KNOBS (tune to lab/refs) ----------
    public static float ambient_C = 20f;            // workshop ambient
    public static float heater_efficiency = 0.55f;  // fraction of heat reaching the charge
    public static float mech_efficiency   = 0.80f;  // motion/extruder efficiency
    public static float printer_idle_W    = 40f;    // standby power draw

    // FDM head power model (for time -> energy)
    public static float fdm_head_W       = 80f;     // additional average power while printing

    // Volumetric throughput (FDM); mm^3/s (set per your printer)
    public static float vfr_mm3_s_walk   = 10f;     // conservative rate for tough polymers
    public static float vfr_mm3_s_run    = 18f;     // optimistic tuned head

    // Polymer anisotropy + porosity (effective strength multipliers)
    public static float raster_parallel_factor   = 0.9f;
    public static float raster_cross_factor      = 0.6f;
    public static float porosity_strength_factor = 0.85f;  // 15% strength loss typical in FDM

    // Recycle aging (molecular weight drop): each cycle reduces baseline
    public static float recycle_strength_decay = 0.90f;    // 10% loss per recycle (tune)

    // CF reinforcement: simple uplift (rule-of-thumb)
    public static float choppedCF_mass_frac      = 0.15f;  // 15 wt% CF in recycled polymer
    public static float choppedCF_strength_gain  = 1.6f;   // 60% gain (tune per test coupons)
    public static float choppedCF_stiffness_gain = 1.8f;

    // SLM / remelt placeholders (set per empirical power data)
    public static float slm_specific_energy_MJ_per_kg = 12f; // very rough; tune with real machine data
    public static float remelt_specific_energy_MJ_per_kg = 2.5f;

    // ---------- UNIT HELPERS ----------
    static float C_to_K(float C) => C + 273.15f;
    static float kJ_to_J(float kJ) => kJ * 1000f;

    // ---------- ENERGY FOR HEATING/MELTING A MASS ----------
    // E [J] = m * ( cp * dT + latent ) / efficiency
    public static float EnergyToMeltJ(float mass_kg, float cp_kJ_kgK, float melt_C, float target_C, float ambient_C, float latent_kJ_kg, float efficiency)
    {
        float dT = Mathf.Max(0f, target_C - ambient_C);
        float sensible_J = mass_kg * kJ_to_J(cp_kJ_kgK) * dT;
        float latent_J   = mass_kg * kJ_to_J(Mathf.Max(latent_kJ_kg, 0f));
        return (sensible_J + latent_J) / Mathf.Max(efficiency, 1e-3f);
    }

    // ---------- PRINT TIME (FDM) ----------
    // t = Volume / volumetric_flow
    public static float FDM_PrintTime_s(float part_mass_kg, float density_kg_m3, float vfr_mm3_s, float infill = 0.25f)
    {
        float vol_m3 = part_mass_kg / Mathf.Max(density_kg_m3, 1e-6f);
        float vol_mm3 = vol_m3 * 1e9f; // m^3 -> mm^3
        float eff_mm3 = vol_mm3 * Mathf.Clamp01(infill + 0.20f); // shell + infill (20% shells default)
        float t_s = eff_mm3 / Mathf.Max(vfr_mm3_s, 1e-6f);
        return t_s;
    }

    // ---------- TOTAL ENERGY (FDM) ----------
    // Heater + motion + idle during print
    public static float FDM_TotalEnergy_J(MaterialDef mat, float part_mass_kg, float print_target_C, float vfr_mm3_s, float infill = 0.25f)
    {
        float E_heat = EnergyToMeltJ(part_mass_kg, mat.cp_kJ_kgK, mat.melt_C, print_target_C, ambient_C, mat.latent_kJ_kg, heater_efficiency);
        float t_s    = FDM_PrintTime_s(part_mass_kg, mat.density_kg_m3, vfr_mm3_s, infill);
        float E_motion = (printer_idle_W + fdm_head_W) * t_s / mech_efficiency; // J
        return E_heat + E_motion;
    }

    // ---------- STRENGTH ESTIMATES ----------
    // Base printed strength along raster (MPa)
    public static float PolymerPrintedStrength_MPa(MaterialDef mat, bool crossLayer, int recycleCycles = 0, float porosityFactor = 0.85f)
    {
        float dirFactor = crossLayer ? raster_cross_factor : raster_parallel_factor;
        float aging = Mathf.Pow(recycle_strength_decay, Mathf.Max(0, recycleCycles));
        return mat.baseline_strength_MPa * dirFactor * porosityFactor * aging;
    }

    // Recycled polymer + chopped CF (very simple uplift model)
    public static float ReinforcedPolymerStrength_MPa(MaterialDef polymer, bool crossLayer, int recycleCycles = 0)
    {
        float baseMPa = PolymerPrintedStrength_MPa(polymer, crossLayer, recycleCycles, porosity_strength_factor);
        return baseMPa * choppedCF_strength_gain;
    }

    // Foams for insulation: use strength floor; pass via minStrength_MPa
    public static float FoamEffectiveStrength_MPa(MaterialDef foam)
    {
        return Mathf.Max(foam.baseline_strength_MPa, 0.8f); // floor
    }

    // ---------- SLM / REMELT ENERGY ----------
    public static float SLM_Energy_J(float mass_kg) => slm_specific_energy_MJ_per_kg * 1e6f * mass_kg;
    public static float RemeltEnergy_J(float mass_kg) => remelt_specific_energy_MJ_per_kg * 1e6f * mass_kg;

    // ---------- SCORING ----------
    public struct EvalResult {
        public bool meetsStrength;
        public bool meetsTempLow;
        public bool meetsTempHigh;
        public float estStrength_MPa;
        public float energy_Wh;
        public float time_s;
        public string explanation;
    }

    // Evaluate one (material, process, partMass_kg) candidate against a task
    public static EvalResult Evaluate(TaskDef task, MaterialDef mat, string processId, float partMass_kg,
                                      bool crossLayer = false, int recycleCycles = 0,
                                      float fdminfill = 0.25f, float targetPrint_C_override = -1f,
                                      bool reinforcedCF = false, bool foamForming = false)
    {
        float targetPrint_C = targetPrint_C_override > 0 ? targetPrint_C_override : Mathf.Max(mat.melt_C + 20f, mat.melt_C); // melt + margin

        float strength_MPa = 0f;
        float time_s = 0f;
        float energy_J = 0f;

        switch (processId)
        {
            case "FDM_FFF":
            {
                float vfr = reinforcedCF ? vfr_mm3_s_walk : vfr_mm3_s_walk; // same by default; tune if CF reduces flow
                time_s = FDM_PrintTime_s(partMass_kg, mat.density_kg_m3, vfr, fdminfill);
                energy_J = FDM_TotalEnergy_J(mat, partMass_kg, targetPrint_C, vfr, fdminfill);
                strength_MPa = reinforcedCF ? ReinforcedPolymerStrength_MPa(mat, crossLayer, recycleCycles)
                                             : PolymerPrintedStrength_MPa(mat, crossLayer, recycleCycles, porosity_strength_factor);
                break;
            }
            case "Reinforced_Polymer_Printing":
            {
                float vfr = vfr_mm3_s_walk;
                time_s = FDM_PrintTime_s(partMass_kg, mat.density_kg_m3, vfr, fdminfill);
                energy_J = FDM_TotalEnergy_J(mat, partMass_kg, targetPrint_C, vfr, fdminfill);
                strength_MPa = ReinforcedPolymerStrength_MPa(mat, crossLayer, recycleCycles);
                break;
            }
            case "Thermal_Bonding":
            case "Hot_Forming":
            {
                // Assume forming energy ~ sensible heat only; short cycle
                time_s = Mathf.Max(120f, partMass_kg * 30f); // 2 mins min or 30 s per kg
                energy_J = EnergyToMeltJ(partMass_kg, mat.cp_kJ_kgK, mat.melt_C, mat.melt_C + 10f, ambient_C, 0f, heater_efficiency);
                strength_MPa = FoamEffectiveStrength_MPa(mat);
                break;
            }
            case "SLM":
            {
                // Placeholder—tune with real machine curves
                time_s = Mathf.Max(1800f, partMass_kg * 1200f); // at least 30 min; ~20 min/kg
                energy_J = SLM_Energy_J(partMass_kg);
                strength_MPa = Mathf.Max(mat.baseline_strength_MPa, 120f); // typical heat-treated Al parts are strong
                break;
            }
            case "CNC_Remelt_Form":
            {
                time_s = Mathf.Max(1200f, partMass_kg * 900f); // casting + machining
                energy_J = RemeltEnergy_J(partMass_kg);
                strength_MPa = Mathf.Max(mat.baseline_strength_MPa, 100f);
                break;
            }
            default:
                return new EvalResult { explanation = $"Unknown process '{processId}'." };
        }

        float energy_Wh = energy_J / 3600f;

        bool meetsStrength = strength_MPa >= task.requirements.minStrength_MPa;
        // Temperature checks: polymers must have Tg/Tm margins; here we approximate with melt point margin
        bool meetsTempLow  = (task.requirements.tempLow_C >= -100f); // placeholder: low temp dominated by embrittlement; tune per mat
        bool meetsTempHigh = (task.requirements.tempHigh_C <= (mat.melt_C - 20f)) || (mat.@class == "metal") || (processId == "SLM");

        string msg = $"Process {processId}: σ≈{strength_MPa:F1} MPa, E≈{energy_Wh:F1} Wh, t≈{time_s/60f:F1} min";

        return new EvalResult {
            meetsStrength = meetsStrength,
            meetsTempLow = meetsTempLow,
            meetsTempHigh = meetsTempHigh,
            estStrength_MPa = strength_MPa,
            energy_Wh = energy_Wh,
            time_s = time_s,
            explanation = msg
        };
    }
}
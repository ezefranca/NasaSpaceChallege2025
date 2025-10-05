using UnityEngine;

[RequireComponent(typeof(ManufacturingConsole))]
public class ManufacturingTerminal : MonoBehaviour
{
    ManufacturingConsole console;
    ManufacturingDB db; // cache once discovered (optional)
    MfgModels.EvalResult lastEval;
    int taskIdx = 0, matIdx = 0, procIdx = 0;
    float partMass = 0.15f;
    bool crossLayer = false, reinforcedCF = false;
    int recycleCycles = 0;

    [Header("Terminal Look & Feel")] public Font terminalFont;
    public int fontSize = 16;
    [Tooltip("Extra size boost applied specifically to the evaluation result line for readability.")]
    public int evalEmphasis = 6;
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.90f);
    public Color primaryColor = new Color(0.0f, 0.95f, 0.5f); // matrix green
    public Color accentColor = new Color(0.3f, 0.9f, 1f); // cyan accent
    public Color warningColor = new Color(1f, 0.8f, 0.2f);
    public bool scanlineEffect = true;

    GUIStyle terminalStyle;
    Texture2D bgTex;
    float lastStyleBuild = -1f;

    [Header("Part Object Mass Override")] public bool usePartObject = false;
    public Transform partObject; // assign a Cube or any mesh root
    [Tooltip("Multiplier applied after volume*density (use to represent infill, e.g. 0.30 for 30% effective)." )]
    public float infillFactor = 0.25f;
    [Tooltip("Fallback volume (m^3) if no renderer found; unit Cube (1,1,1) is 1 m^3.")] public float fallbackVolume_m3 = 1f;

    void Awake()
    {
        console = GetComponent<ManufacturingConsole>();
        // Don't fetch DB here; other component's Awake order is unspecified.
    }

    void Start()
    {
        db = console?.DB;
        if (db == null)
        {
            // Not necessarily an error yet; console may finish later this frame.
            Debug.LogWarning("ManufacturingTerminal: DB not ready at Start; will retry.");
        }
    }

    void Update()
    {
        if (console == null) return;

        // Late bind if DB became available after our Start.
        if (db == null && console.DB != null)
        {
            db = console.DB;
            Debug.Log("ManufacturingTerminal: Linked DB after late load.");
        }

        if (db == null) return; // still not ready

        if (Input.GetKeyDown(KeyCode.T)) taskIdx = (taskIdx + 1) % db.manufacturingTasks.Length;
        if (Input.GetKeyDown(KeyCode.M)) matIdx  = (matIdx + 1) % db.materials.Length;
        if (Input.GetKeyDown(KeyCode.P)) procIdx = (procIdx + 1) % db.processes.Length;
        if (Input.GetKeyDown(KeyCode.C)) crossLayer = !crossLayer;
        if (Input.GetKeyDown(KeyCode.R)) reinforcedCF = !reinforcedCF;
        if (Input.GetKeyDown(KeyCode.UpArrow)) partMass += 0.05f;
        if (Input.GetKeyDown(KeyCode.DownArrow)) partMass = Mathf.Max(0.05f, partMass - 0.05f);
        if (Input.GetKeyDown(KeyCode.LeftArrow)) recycleCycles = Mathf.Max(0, recycleCycles - 1);
        if (Input.GetKeyDown(KeyCode.RightArrow)) recycleCycles++;

        if (Input.GetKeyDown(KeyCode.F))
        {
            var task = db.manufacturingTasks[taskIdx];
            var mat  = db.materials[matIdx];
            var proc = db.processes[procIdx];
            lastEval = console.Evaluate(task.id, mat.id, proc.id, partMass, crossLayer, reinforcedCF, recycleCycles);
        }

        // Dynamic mass from part object (updates before evaluation key use)
        if (usePartObject && partObject != null)
        {
            var mat = db.materials[matIdx];
            float volume = ComputeObjectVolume(partObject, fallbackVolume_m3); // m^3
            float rawMass = volume * Mathf.Max(mat.density_kg_m3, 0.0001f); // kg
            partMass = rawMass * Mathf.Clamp01(infillFactor <= 0 ? 1f : infillFactor); // apply infill scaling
        }
    }

    void OnGUI()
    {
        if (console == null)
        {
            GUI.Label(new Rect(20,20,700,30), "ManufacturingTerminal: Missing ManufacturingConsole component.");
            return;
        }
        if (db == null)
        {
            GUI.Label(new Rect(20,20,1000,60),
                "Manufacturing DB not loaded.\n" +
                "Put 'manufacturing_db.json' in Assets/Resources/ OR assign it to 'dbOverride' on ManufacturingConsole.");
            return;
        }

        EnsureStyles();

        var rect = new Rect(12, 12, Screen.width - 24, Mathf.Min(Screen.height - 24, 360));
        if (bgTex != null)
            GUI.DrawTexture(rect, bgTex, ScaleMode.StretchToFill);

        GUILayout.BeginArea(rect);
        GUILayout.BeginVertical();

        // Simulated scanline overlay (simple alpha stripes)
        if (scanlineEffect)
        {
            var lineH = 2;
            var scanColor = new Color(1f, 1f, 1f, 0.015f);
            var scanTex = Texture2D.whiteTexture;
            var fullW = rect.width;
            for (int y = 0; y < rect.height; y += lineH * 2)
            {
                GUI.color = scanColor;
                GUI.DrawTexture(new Rect(rect.x, rect.y + y, fullW, lineH), scanTex);
            }
            GUI.color = Color.white;
        }

        var blink = (Time.time % 1f) < 0.5f ? "_" : " ";
        var task = db.manufacturingTasks[taskIdx];
        var mat  = db.materials[matIdx];
        var proc = db.processes[procIdx];

        string header = "=== MARS OPS // ADDITIVE MANUFACTURING TERMINAL ===";
        string massLine;
        if (usePartObject)
        {
            massLine = $"Mass(auto) : {partMass:F3} kg (obj='{partObject?.name}' infill={infillFactor:P0})";
        }
        else
        {
            massLine = $"Mass [↑↓]  : {partMass:F2} kg    Recycles [←→]: {recycleCycles}";
        }

        string body =
            $"[T] Task       : <color=#{ColorToHex(accentColor)}>{task.title}</color> ({task.id})\n" +
            $"[M] Material   : <color=#{ColorToHex(accentColor)}>{mat.name}</color> ({mat.id})\n" +
            $"[P] Process    : <color=#{ColorToHex(accentColor)}>{proc.name}</color> ({proc.id})\n" +
            massLine + "\n" +
            $"CrossLayer [C] : {crossLayer}    ReinforcedCF [R]: {reinforcedCF}\n\n" +
            "Press [F] Evaluate | [T/M/P] Cycle | [C] CrossLayer | [R] Reinforced" + (usePartObject ? " | (auto mass)" : " | [Arrows] Mass/Recycles") + "\n\n";

    string eval = lastEval.explanation ?? "(no evaluation yet)";
    bool hasEval = lastEval.explanation != null;
    bool passAll = lastEval.meetsStrength && lastEval.meetsTempLow && lastEval.meetsTempHigh && hasEval;
    Color evalColor = hasEval ? (passAll ? accentColor : warningColor) : primaryColor;

    // Temporarily push bigger font for the evaluation line
    int origSize = terminalStyle.fontSize;
    var bigStyle = new GUIStyle(terminalStyle) { fontSize = fontSize + evalEmphasis, fontStyle = FontStyle.Bold };
    bigStyle.normal.textColor = evalColor;

    string status = $"Strength OK: {lastEval.meetsStrength} | TempRange OK: {(lastEval.meetsTempLow && lastEval.meetsTempHigh)}\n" +
            $"σ≈{lastEval.estStrength_MPa:F1} MPa | E≈{lastEval.energy_Wh:F1} Wh | t≈{lastEval.time_s/60f:F1} min";

        GUILayout.Label($"<color=#{ColorToHex(primaryColor)}>{header}</color>", terminalStyle);
        GUILayout.Space(4);
        GUILayout.Label($"<color=#{ColorToHex(primaryColor)}>{body}</color>", terminalStyle);
    GUILayout.Label($"<color=#{ColorToHex(evalColor)}>{eval}</color>", bigStyle);
        GUILayout.Space(2);
        GUILayout.Label($"<color=#{ColorToHex(primaryColor)}>{status}</color> {blink}", terminalStyle);

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    void EnsureStyles()
    {
        if (terminalStyle != null && Time.time - lastStyleBuild < 2f) return; // rebuild occasionally if values tweak in inspector

        terminalStyle = new GUIStyle(GUI.skin.label)
        {
            richText = true,
            fontSize = fontSize,
            font = terminalFont,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.UpperLeft,
            wordWrap = true
        };
        terminalStyle.normal.textColor = primaryColor;

        if (bgTex == null || bgTex.width != 1)
        {
            bgTex = new Texture2D(1,1,TextureFormat.RGBA32,false) { wrapMode = TextureWrapMode.Clamp };            
        }
        bgTex.SetPixel(0,0, backgroundColor);
        bgTex.Apply();
        lastStyleBuild = Time.time;
    }

    static string ColorToHex(Color c)
    {
        Color32 c32 = c;
        return c32.r.ToString("X2") + c32.g.ToString("X2") + c32.b.ToString("X2");
    }

    float ComputeObjectVolume(Transform t, float fallback)
    {
        // Priority: Renderer bounds (approx), else scaled unit cube assumption, else fallback.
        var rend = t.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            var size = rend.bounds.size; // world-space meters
            return Mathf.Max(0f, size.x * size.y * size.z);
        }
        // Assume original mesh/cube was 1m^3 at scale (1,1,1)
        var s = t.lossyScale;
        float vol = s.x * s.y * s.z;
        if (vol <= 0f) vol = fallback;
        return vol;
    }
}
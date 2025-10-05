using UnityEngine;

// Handles selecting manufacturable parts via center screen raycast and evaluating them.
[DefaultExecutionOrder(50)]
public class PartInteractionController : MonoBehaviour
{
    [Header("Setup")] public Camera playerCamera; // assign; if null uses Camera.main
    public float interactDistance = 6f;
    public LayerMask interactMask = ~0;

    [Header("Keys")] public KeyCode evaluateKey = KeyCode.F;
    public KeyCode cycleMaterialKey = KeyCode.Alpha1;
    public KeyCode cycleProcessKey = KeyCode.Alpha2;

    [Header("UI Toggle")] public bool showHUD = true;

    [Header("HUD Styles")] public int hudFontSize = 14; // base font size for general info
    public int evalFontSize = 22; // emphasized evaluation result font size
    public Color evalOkColor = new Color(0.2f, 1f, 0.3f);
    public Color evalFailColor = new Color(1f, 0.4f, 0.2f);
    public Color evalPendingColor = new Color(1f, 0.95f, 0.3f);
    public bool evalOutline = true;
    public Color evalOutlineColor = new Color(0f,0f,0f,0.9f);
    [Tooltip("Toggle compact HUD (hides some lines)")] public KeyCode toggleCompactKey = KeyCode.BackQuote;
    public bool compactHUD = false;

    [Header("Auto Hide & Fade")] public bool autoHide = true;
    [Tooltip("Seconds of inactivity before HUD fades out (interaction = key press or selection change)")] public float hideDelay = 6f;
    [Tooltip("Seconds for fade in/out")] public float fadeDuration = 0.5f;
    [Tooltip("Keep HUD visible while a part is targeted even if inactive.")] public bool keepVisibleOnTarget = true;

    [Header("Status Icons & Colors")] public bool showStatusIcons = true;
    public string passIcon = "✔"; public string failIcon = "✖"; public string pendingIcon = "…";
    public Color passColor = new Color(0.3f,1f,0.4f);
    public Color failColor = new Color(1f,0.35f,0.25f);
    public Color pendingColor = new Color(1f,0.9f,0.4f);

    [Header("Highlight")] public Color highlightColor = new Color(0f,1f,0.5f,0.4f);

    ManufacturablePart current;    // currently looked-at part
    ManufacturablePart last;       // last part (for unhighlight)

    ManufacturingConsole console;  // DB provider
    MfgModels.EvalResult lastEval; // last evaluation result

    int matIdx = 0, procIdx = 0;

    // Cache arrays for current target
    string[] activeMats;
    string[] activeProcs;

    Material originalMat; // simplistic highlight (if object has a single renderer + material)
    Renderer targetRenderer;

    void Awake()
    {
        if (!playerCamera) playerCamera = Camera.main;
        console = FindObjectOfType<ManufacturingConsole>();
    }

    GUIStyle boxStyle, baseStyle, evalStyle;
    int _lastHudFontSize, _lastEvalFontSize;

    void EnsureStyles()
    {
        if (baseStyle != null && _lastHudFontSize == hudFontSize && _lastEvalFontSize == evalFontSize) return;

        _lastHudFontSize = hudFontSize;
        _lastEvalFontSize = evalFontSize;

        baseStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = hudFontSize,
            alignment = TextAnchor.UpperLeft,
            wordWrap = true
        };
        baseStyle.normal.textColor = Color.white;

        evalStyle = new GUIStyle(baseStyle)
        {
            fontSize = evalFontSize,
            fontStyle = FontStyle.Bold
        };
        evalStyle.normal.textColor = evalPendingColor;

        boxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { textColor = Color.white },
            fontSize = hudFontSize,
            alignment = TextAnchor.UpperLeft
        };
    }

    void Update()
    {
        RaycastSelect();
        if (!console || console.DB == null || current == null) return;

        var db = console.DB;
        var task = db.Task(current.taskId) ?? db.manufacturingTasks[0];

        // Determine candidate arrays
        activeMats = (current.candidateMaterials != null && current.candidateMaterials.Length > 0)
            ? current.candidateMaterials : task.candidateMaterials;
        activeProcs = (current.candidateProcesses != null && current.candidateProcesses.Length > 0)
            ? current.candidateProcesses : task.preferredProcesses;

        if (activeMats == null || activeMats.Length == 0 || activeProcs == null || activeProcs.Length == 0)
            return;

        if (Input.GetKeyDown(cycleMaterialKey))
            matIdx = (matIdx + 1) % activeMats.Length;
        if (Input.GetKeyDown(cycleProcessKey))
            procIdx = (procIdx + 1) % activeProcs.Length;

        if (Input.GetKeyDown(evaluateKey))
        {
            var mat = db.Mat(activeMats[matIdx]);
            var proc = activeProcs[procIdx];
            if (mat == null)
            {
                lastEval = new MfgModels.EvalResult { explanation = $"Material '{activeMats[matIdx]}' not found." };
            }
            else
            {
                float massKg = current.GetMassKg(db, mat);
                lastEval = console.Evaluate(task.id, mat.id, proc, massKg, crossLayer:false, reinforcedCF:false, recycleCycles:0);
            }
            TouchInteraction();
        }

        if (Input.GetKeyDown(toggleCompactKey)) compactHUD = !compactHUD;

        // Fade logic
        UpdateFade();
    }

    void RaycastSelect()
    {
        if (!playerCamera) return;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f,0.5f,0f));
        current = null;
        if (Physics.Raycast(ray, out var hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
        {
            current = hit.collider.GetComponentInParent<ManufacturablePart>();
        }
        if (current != last)
        {
            ClearHighlight();
            if (current != null)
                ApplyHighlight(current);
            last = current;
        }
    }

    void ApplyHighlight(ManufacturablePart part)
    {
        targetRenderer = part.GetComponentInChildren<Renderer>();
        if (!targetRenderer) return;
        originalMat = targetRenderer.sharedMaterial;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetColor("_BaseColor", highlightColor);
        targetRenderer.material = mat;
        TouchInteraction();
    }

    void ClearHighlight()
    {
        if (targetRenderer && originalMat)
        {
            targetRenderer.material = originalMat;
        }
        targetRenderer = null;
        originalMat = null;
    }

    void OnDisable() => ClearHighlight();

    float lastInteractionTime = -999f; // epoch
    float hudAlpha = 1f; // current fade alpha

    void TouchInteraction()
    {
        lastInteractionTime = Time.time;
    }

    void UpdateFade()
    {
        if (!autoHide) { hudAlpha = 1f; return; }
        bool activeTarget = keepVisibleOnTarget && current != null;
        float since = Time.time - lastInteractionTime;
        float targetA = (since < hideDelay || activeTarget) ? 1f : 0f;
        if (Mathf.Approximately(fadeDuration, 0f)) { hudAlpha = targetA; return; }
        float speed = Time.deltaTime / Mathf.Max(0.0001f, fadeDuration);
        hudAlpha = Mathf.MoveTowards(hudAlpha, targetA, speed);
    }

    void OnGUI()
    {
        if (!showHUD) return;
        if (!console || console.DB == null)
        {
            GUI.Label(new Rect(20,20,400,40), "Manufacturing DB not loaded.");
            return;
        }

        EnsureStyles();

        var db = console.DB;
        GUI.Label(new Rect(Screen.width/2 - 4, Screen.height/2 - 4, 8, 8), "+"); // simple crosshair

        if (current == null)
        {
            //GUI.Label(new Rect(20, Screen.height - 80, 480, 70), "Look at a manufacturable part (add ManufacturablePart.cs).");
            return;
        }

        var task = db.Task(current.taskId) ?? (db.manufacturingTasks.Length > 0 ? db.manufacturingTasks[0] : null);
        if (task == null)
        {
            GUI.Label(new Rect(20, Screen.height - 80, 480, 70), $"Task '{current.taskId}' not found.");
            return;
        }

        string matId = (activeMats != null && activeMats.Length > 0) ? activeMats[Mathf.Clamp(matIdx,0,activeMats.Length-1)] : "(none)";
        string procId = (activeProcs != null && activeProcs.Length > 0) ? activeProcs[Mathf.Clamp(procIdx,0,activeProcs.Length-1)] : "(none)";

        string evalLine = string.IsNullOrEmpty(lastEval.explanation) ? "Press [F] to evaluate" : lastEval.explanation;

        // Determine evaluation color (pass if meets all criteria)
        bool hasResult = !string.IsNullOrEmpty(lastEval.explanation);
        bool pass = lastEval.meetsStrength && lastEval.meetsTempLow && lastEval.meetsTempHigh && hasResult;
        if (hasResult)
            evalStyle.normal.textColor = pass ? evalOkColor : evalFailColor;
        else
            evalStyle.normal.textColor = evalPendingColor;

        // Dynamic height estimate based on fonts
        float panelW = 600f;
        // If compact hide keys + status or reduce spacing
        float panelH = compactHUD ? 150f : 200f;
        float panelY = Screen.height - panelH - 20f;
    // Respect fade alpha (crosshair stays full alpha for aiming)
    var prevColor = GUI.color;
    GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * hudAlpha);
    GUI.Box(new Rect(20, panelY, panelW, panelH), GUIContent.none, boxStyle);

        float y = panelY + 10f;
        GUI.Label(new Rect(30, y, panelW - 40f, 40f), $"TARGET: {current.name}", baseStyle); y += hudFontSize + 8f;
        GUI.Label(new Rect(30, y, panelW - 40f, 40f), $"Task: {task.id}    Mat[1]: {matId}    Proc[2]: {procId}", baseStyle); y += hudFontSize + 6f;
        if (!compactHUD)
        {
            GUI.Label(new Rect(30, y, panelW - 40f, 50f), "Keys: [F] Evaluate   [1] Material   [2] Process   [`] Compact", baseStyle); y += hudFontSize + 10f;
        }

        var evalRect = new Rect(30, y, panelW - 40f, evalFontSize + 28f);
        DrawOutlinedLabel(evalRect, evalLine, evalStyle, evalOutline && hasResult ? evalOutlineColor : new Color(0,0,0,0));
        y += evalFontSize + 14f;
        if (!compactHUD)
        {
            string strengthTok = showStatusIcons ? (lastEval.meetsStrength ? passIcon : (hasResult ? failIcon : pendingIcon)) : (lastEval.meetsStrength ? "OK" : "FAIL");
            string tempTok = showStatusIcons ? ((lastEval.meetsTempLow && lastEval.meetsTempHigh) ? passIcon : (hasResult ? failIcon : pendingIcon)) : ((lastEval.meetsTempLow && lastEval.meetsTempHigh) ? "OK" : "FAIL");
            Color strengthColor = hasResult ? (lastEval.meetsStrength ? passColor : failColor) : pendingColor;
            Color tempColor = hasResult ? ((lastEval.meetsTempLow && lastEval.meetsTempHigh) ? passColor : failColor) : pendingColor;
            var prev = GUI.color;
            string statusTime = hasResult ? $" t={(lastEval.time_s/60f):F1}min" : "";
            // Strength
            GUI.color = new Color(prev.r, prev.g, prev.b, prev.a * hudAlpha);
            DrawInlineColored(new Rect(30, y, panelW - 40f, 25f),
                $"Strength: ", strengthTok, strengthColor, baseStyle);
            y += hudFontSize + 6f;
            DrawInlineColored(new Rect(30, y, panelW - 40f, 25f),
                $"TempRange: ", tempTok + statusTime, tempColor, baseStyle);
            GUI.color = prev;
        }
        GUI.color = prevColor; // restore global GUI color
    }

    void DrawOutlinedLabel(Rect r, string text, GUIStyle style, Color outlineColor, int thickness = 1)
    {
        if (outlineColor.a > 0.01f)
        {
            var prev = GUI.color;
            GUI.color = outlineColor;
            for (int dx = -thickness; dx <= thickness; dx++)
            for (int dy = -thickness; dy <= thickness; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var rr = new Rect(r.x + dx, r.y + dy, r.width, r.height);
                GUI.Label(rr, text, style);
            }
            GUI.color = prev;
        }
        GUI.Label(r, text, style);
    }

    void DrawInlineColored(Rect r, string prefix, string token, Color tokenColor, GUIStyle style)
    {
        // Simple inline: draw prefix then colored token using rich text fallback if style supports
        if (!style.richText)
        {
            // manual two labels
            float mid = GUI.skin.label.CalcSize(new GUIContent(prefix)).x;
            GUI.Label(new Rect(r.x, r.y, mid + 4f, r.height), prefix, style);
            var prev = GUI.color;
            GUI.color = tokenColor;
            GUI.Label(new Rect(r.x + mid, r.y, r.width - mid, r.height), token, style);
            GUI.color = prev;
        }
        else
        {
            string hex = ColorUtility.ToHtmlStringRGB(tokenColor);
            GUI.Label(r, prefix + $"<color=#{hex}>{token}</color>", style);
        }
    }
}

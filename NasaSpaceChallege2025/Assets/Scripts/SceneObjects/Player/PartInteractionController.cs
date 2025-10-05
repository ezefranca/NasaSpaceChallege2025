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
        }
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

    void OnGUI()
    {
        if (!showHUD) return;
        if (!console || console.DB == null)
        {
            GUI.Label(new Rect(20,20,400,40), "Manufacturing DB not loaded.");
            return;
        }

        var db = console.DB;
        GUI.Label(new Rect(Screen.width/2 - 4, Screen.height/2 - 4, 8, 8), "+"); // simple crosshair

        if (current == null)
        {
            GUI.Label(new Rect(20, Screen.height - 80, 480, 70), "Look at a manufacturable part (add ManufacturablePart.cs).");
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

        string evalLine = string.IsNullOrEmpty(lastEval.explanation) ? "(F to evaluate)" : lastEval.explanation;

        GUI.Box(new Rect(20, Screen.height - 170, 520, 150), "");
        GUI.Label(new Rect(30, Screen.height - 160, 500, 140),
            $"TARGET: {current.name}\n" +
            $"Task: {task.id}  Mat[1]: {matId}  Proc[2]: {procId}\n" +
            $"Keys: [F] Evaluate  [1] Cycle Material  [2] Cycle Process\n" +
            $"Result: {evalLine}\n" +
            $"StrengthOK={lastEval.meetsStrength}  TempOK={(lastEval.meetsTempLow && lastEval.meetsTempHigh)}  t={(lastEval.time_s/60f):F1}min");
    }
}

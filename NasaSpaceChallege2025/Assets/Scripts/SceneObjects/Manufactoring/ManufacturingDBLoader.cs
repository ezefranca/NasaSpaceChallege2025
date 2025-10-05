using UnityEngine;

public static class ManufacturingDBLoader
{
    public static ManufacturingDB LoadFromResources(string path = "manufacturing_db")
    {
        TextAsset ta = Resources.Load<TextAsset>(path);

        if (!ta && !path.Contains("/"))
        {
            // scan all TextAssets under Resources, match by name
            var all = Resources.LoadAll<TextAsset>("");
            foreach (var t in all)
                if (t.name.Equals(path, System.StringComparison.OrdinalIgnoreCase)) { ta = t; break; }
        }

        if (!ta)
        {
            Debug.LogError($"ManufacturingDBLoader: could not find Resources/{path}.json. " +
                           $"Ensure the file is under Assets/Resources/ with the name 'manufacturing_db.json', " +
                           $"or pass a subpath like LoadFromResources(\"Data/manufacturing_db\").");
            return null;
        }

        try
        {
            var db = JsonUtility.FromJson<ManufacturingDB>(ta.text);
            if (db == null) throw new System.Exception("JsonUtility returned null (malformed JSON?)");
            db.BuildIndexes();
            return db;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"ManufacturingDBLoader: JSON parse failed for '{ta.name}'. Error: {ex.Message}");
            return null;
        }
    }
}
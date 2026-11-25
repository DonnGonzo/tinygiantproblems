using UnityEngine;

namespace TinyGiantProblems
{
    // Ordered list of every mission (Resources/Missions/MissionCatalog).
    [CreateAssetMenu(menuName = "Tiny Giant Problems/Mission Catalog")]
    public sealed class MissionCatalog : ScriptableObject
    {
        public MissionDef[] missions = new MissionDef[0];
        static MissionCatalog cached;
        public static MissionCatalog Instance { get { if (!cached) cached = Resources.Load<MissionCatalog>("Missions/MissionCatalog"); return cached; } }
        public MissionDef Get(string id) { foreach (var m in missions) if (m && m.id == id) return m; return null; }
    }
}

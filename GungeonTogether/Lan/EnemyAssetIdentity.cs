namespace GungeonTogether.Lan
{
    // Empty means no enemy dependency; otherwise only a native database GUID.
    internal static class EnemyAssetIdentity
    {
        internal static bool Valid(string guid)
        {
            if (guid == null) return false;
            if (guid.Length == 0) return true;
            if (guid.Length != 32) return false;
            foreach (char c in guid) if (!(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'f')) return false;
            return true;
        }
    }
}

using System;
using System.IO;

namespace GungeonTogether.Lan
{
    public static class CharacterCatalog
    {
        public const int GuestCount = 5;
        private static readonly string[] Prefabs = { "PlayerCoopCultist", "PlayerRogue", "PlayerMarine", "PlayerConvict", "PlayerGuide", "PlayerRobot", "PlayerBullet", "PlayerGunslinger", "PlayerEevee" };
        private static readonly string[] Identities = { "CoopCultist", "Pilot", "Soldier", "Convict", "Guide", "Robot", "Bullet", "Gunslinger", "Eevee" };
        private static readonly string[] Names = { "Cultist", "Pilot", "Marine", "Convict", "Hunter", "Robot", "Bullet", "Gunslinger", "Paradox" };
        public static bool Valid(int id) { return id >= 0 && id < Prefabs.Length; }
        public static bool ValidGuest(int id) { return id >= 0 && id < GuestCount; }
        public static string Prefab(int id) { if (!Valid(id)) throw new IOException("Unknown character."); return Prefabs[id]; }
        public static string Name(int id) { return Valid(id) ? Names[id] : "?"; }
        public static int FromIdentity(string identity)
        {
            for (int i = 0; i < Identities.Length; i++) if (Identities[i] == identity) return i;
            throw new IOException("Unsupported host character: " + identity);
        }
    }

    public sealed class LobbyChoice
    {
        public int Character;
        public byte[] Encode() { return Wire.Write(DungeonMessage.Character, delegate(BinaryWriter w) { w.Write(Character); }); }
        public static LobbyChoice Decode(byte[] bytes)
        {
            return Wire.Read(bytes, DungeonMessage.Character, delegate(BinaryReader r) {
                int id = r.ReadInt32();
                if (!CharacterCatalog.ValidGuest(id)) throw new IOException("Unsupported guest character.");
                return new LobbyChoice { Character = id };
            });
        }
    }
}

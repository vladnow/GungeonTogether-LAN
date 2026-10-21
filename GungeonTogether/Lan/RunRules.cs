using System;
using System.IO;
namespace GungeonTogether.Lan
{
    public sealed class RunRules
    {
        public bool SeparateMoney, SeparateKeys, PersonalLoot;
        public bool AnyWallet { get { return SeparateMoney || SeparateKeys; } }
        public byte Flags { get { return (byte)((SeparateMoney ? 1 : 0) | (SeparateKeys ? 2 : 0) | (PersonalLoot ? 4 : 0)); } }
        public RunRules Copy() { return new RunRules { SeparateMoney = SeparateMoney, SeparateKeys = SeparateKeys, PersonalLoot = PersonalLoot }; }
        public byte[] Encode() { return Wire.Write(DungeonMessage.Rules, delegate(BinaryWriter w) { w.Write(Flags); }); }
        public static RunRules Decode(byte[] bytes) { return Wire.Read(bytes, DungeonMessage.Rules, delegate(BinaryReader r) {
            byte flags = r.ReadByte(); if ((flags & ~7) != 0) throw new IOException("Invalid host run rules.");
            return new RunRules { SeparateMoney = (flags & 1) != 0, SeparateKeys = (flags & 2) != 0, PersonalLoot = (flags & 4) != 0 };
        }); }
        // Alternate odd boss-reward remainders; never create or discard money.
        public static int[] SplitBossMoney(int amount, bool guestGetsRemainder) {
            if (amount < 0) throw new ArgumentOutOfRangeException("amount");
            int half = amount / 2; return new[] { half + (!guestGetsRemainder ? amount % 2 : 0), half + (guestGetsRemainder ? amount % 2 : 0) };
        }
    }
}

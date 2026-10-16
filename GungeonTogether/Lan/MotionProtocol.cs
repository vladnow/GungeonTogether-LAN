using System;
using System.IO;
namespace GungeonTogether.Lan
{
    public sealed class PlayerMotion
    {
        public int Epoch, RoomX, RoomY;
        public uint Sequence, InputSequence;
        public double SentAt;
        public float X, Y, Speed, FootX, FootY, HalfWidth = .25f, HalfHeight = .25f;
        public float CameraX, CameraY, CameraZ, CameraSize = 7;
        public bool AllowPrediction, CameraFollowsPlayer;
        public byte[] Encode()
        {
            Validate();
            return Wire.Write(DungeonMessage.Motion, delegate(BinaryWriter w) {
                w.Write(Epoch); w.Write(Sequence); w.Write(SentAt); w.Write(InputSequence);
                w.Write(X); w.Write(Y); w.Write(Speed); w.Write(RoomX); w.Write(RoomY);
                w.Write(FootX); w.Write(FootY); w.Write(HalfWidth); w.Write(HalfHeight);
                w.Write(CameraX); w.Write(CameraY); w.Write(CameraZ); w.Write(CameraSize); w.Write(AllowPrediction); w.Write(CameraFollowsPlayer);
            });
        }
        public static PlayerMotion Decode(byte[] bytes)
        {
            return Wire.Read(bytes, DungeonMessage.Motion, delegate(BinaryReader r) {
                PlayerMotion value = new PlayerMotion {
                    Epoch = r.ReadInt32(), Sequence = r.ReadUInt32(), SentAt = r.ReadDouble(), InputSequence = r.ReadUInt32(),
                    X = r.ReadSingle(), Y = r.ReadSingle(), Speed = r.ReadSingle(), RoomX = r.ReadInt32(), RoomY = r.ReadInt32(),
                    FootX = r.ReadSingle(), FootY = r.ReadSingle(), HalfWidth = r.ReadSingle(), HalfHeight = r.ReadSingle(),
                    CameraX = r.ReadSingle(), CameraY = r.ReadSingle(), CameraZ = r.ReadSingle(), CameraSize = r.ReadSingle(), AllowPrediction = r.ReadBoolean(), CameraFollowsPlayer = r.ReadBoolean()
                }; value.Validate(); return value;
            });
        }
        private void Validate()
        {
            if (Epoch < 1 || Math.Abs((long)RoomX) > 100000 || Math.Abs((long)RoomY) > 100000) throw new IOException("Invalid motion epoch/room.");
            Wire.Time(SentAt); Wire.Range(X, -100000, 100000); Wire.Range(Y, -100000, 100000); Wire.Range(Speed, 0, 100);
            Wire.Range(FootX, -10, 10); Wire.Range(FootY, -10, 10); Wire.Range(HalfWidth, .01f, 3); Wire.Range(HalfHeight, .01f, 3);
            Wire.Range(CameraX, -100000, 100000); Wire.Range(CameraY, -100000, 100000); Wire.Range(CameraZ, -100000, 100000); Wire.Range(CameraSize, .01f, 10000);
        }
    }
}

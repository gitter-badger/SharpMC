﻿namespace SharpMCRewrite
{
    public class PlayerLook : IPacket
    {
        public int PacketID
        {
            get
            {
                return 0x05;
            }
        }

        public bool IsPlayePacket
        {
            get
            {
                return true;
            }
        }

        public void Read(ClientWrapper state, MSGBuffer buffer, object[] Arguments)
        {
            float Yaw = buffer.ReadFloat ();
            float Pitch = buffer.ReadFloat ();
            bool OnGround = buffer.ReadBool ();
            state.Player.Yaw = Yaw;
            state.Player.Pitch = Pitch;
            state.Player.OnGround = OnGround;
            Globals.Level.BroadcastPacket (new EntityLook (), new object[] { state.Player.UniqueServerID, (byte)Yaw, (byte)Pitch, OnGround });
        }

        public void Write(ClientWrapper state, MSGBuffer buffer, object[] Arguments)
        {

        }
    }
}


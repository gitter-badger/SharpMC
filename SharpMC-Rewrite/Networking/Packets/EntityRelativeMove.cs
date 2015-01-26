﻿namespace SharpMCRewrite
{
    public class EntityRelativeMove : IPacket
    {
        public int PacketID
        {
            get
            {
                return 0x15;
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

        }

        /// <summary>
        /// Arg 0: Player
        /// Arg 1: X Movement
        /// Arg 2: Y Movement
        /// Arg 3: Z Movement
        /// Arg 4: onGround
        /// </summary>
        /// <param name="state">State.</param>
        /// <param name="buffer">Buffer.</param>
        /// <param name="Arguments">Arguments.</param>
        public void Write(ClientWrapper state, MSGBuffer buffer, object[] Arguments)
        {
            Player target = (Player)Arguments [0];
            if (state.Player != target)
            {  
                buffer.WriteVarInt (0x15);
                buffer.WriteVarInt (target.UniqueServerID);
                buffer.WriteByte ((byte)((double)Arguments [1] * 32));
                buffer.WriteByte ((byte)((double)Arguments [2] * 32));
                buffer.WriteByte ((byte)((double)Arguments [3] * 32));
                buffer.WriteBool ((bool)Arguments [4]);
                buffer.FlushData ();
            }
        }
    }
}


﻿using ProtoBuf;
using System.Collections.Generic;

namespace Protocol
{
    [ProtoContract]
    public class WorldState : Packet
    {
        [ProtoMember(1)]
        public List<PlayerStateUpdate_S2C> PlayerStates { get; set; }

        [ProtoMember(2)]
        public int CurrentServerTime { get; set; }
    }
}

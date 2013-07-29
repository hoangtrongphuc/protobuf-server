﻿using Data.Accounts;
using Data.NPCs;
using NLog;
using Protocol;
using Server.Utility;
using Server.Zones;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Server
{
    public partial class PlayerPeer : NetPeer
    {
        private static Logger s_log = LogManager.GetCurrentClassLogger();

        private ObjectRouter m_unauthenticatedHandler = new ObjectRouter();
        private ObjectRouter m_authenticatedHandler = new ObjectRouter();

        private int m_lastActivity = Environment.TickCount;
        private const int PING_TIMEOUT = 5000;

        private PlayerState m_playerState = new PlayerState();
        private ThreadSafeWrapper<PlayerState> m_playerStateAccessor;

        public bool IsAuthenticated
        {
            get;
            private set;
        }

        public PlayerPeer(Socket socket, IAccountRepository accountRepository, INPCRepository npcRepository, Dictionary<int, Zone> zones) : base(socket)
        {
            m_playerStateAccessor = new ThreadSafeWrapper<PlayerState>(m_playerState, Fiber);
            m_accountRepository = accountRepository;
            m_zones = zones;
            m_npcRepository = npcRepository;

            InitialiseRoutes();
        }

        private void InitialiseRoutes()
        {
            m_unauthenticatedHandler.SetRoute<AuthenticationAttempt_C2S>(Handle_AuthenticationAttempt);
            m_unauthenticatedHandler.SetRoute<TimeSync_C2S>(Handle_TimeSync);

            m_authenticatedHandler.SetRoute<TimeSync_C2S>(Handle_TimeSync);
            m_authenticatedHandler.SetRoute<PlayerStateUpdate_C2S>(Handle_PlayerStateUpdate);
        }

        public void Update()
        {
            Fiber.Enqueue(InternalUpdate);
        }

        private void InternalUpdate()
        {
            LatestStateUpdate = new PlayerStateUpdate_S2C()
            {
                PlayerID = ID,
                CurrentHP = 0,
                MaxHP = 0,
                Rot = m_playerState.Rotation,
                TargetID = m_playerState.TargetID,
                Time = m_playerState.TimeOnClient,
                VelX = m_playerState.Velocity.X,
                VelY = m_playerState.Velocity.Y,
                X = m_playerState.Position.X,
                Y = m_playerState.Position.Y
            };

            if (IsAuthenticated && m_playerState.CurrentZone != null)
            {
                BuildAndSendWorldStateUpdate();
            }

            if (Environment.TickCount - m_lastActivity > PING_TIMEOUT)
            {
                Send(new Ping());
                m_lastActivity = Environment.TickCount;
            }
        }

        protected override void DispatchPacket(Packet packet)
        {
            bool handled = IsAuthenticated ?
                m_authenticatedHandler.Route(packet) :
                m_unauthenticatedHandler.Route(packet);

            if (!handled)
            {
                s_log.Warn("Failed to handle packet of type {0}. Authenticated: {1}", packet.GetType(), IsAuthenticated);
                Disconnect();
            }

            m_lastActivity = Environment.TickCount;
        }

        public override void Dispose()
        {
            m_playerState.CurrentZone.RemoveFromZone(this);

            base.Dispose();
        }

        private void Handle_TimeSync(TimeSync_C2S sync)
        {
            s_log.Trace("Time sync request from ID {0}", ID);
            Respond(sync, new TimeSync_S2C() { Time = Environment.TickCount });
        }
    }
}

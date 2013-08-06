﻿using Data.Accounts;
using Data.NPCs;
using Data.Players;
using Data.Stats;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
using NLog;
using Protocol;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        private static Logger s_log = LogManager.GetCurrentClassLogger();

        private static void Main(string[] args)
        {
            ServerConfiguration config = (ServerConfiguration)ConfigurationManager.GetSection("server");

            s_log.Info("Creating repositories...");
            UnityContainer repositoryResolver = new UnityContainer();
            repositoryResolver.LoadConfiguration();

            IAccountRepository accountRepository = repositoryResolver.Resolve<IAccountRepository>();
            INPCRepository npcRepository = repositoryResolver.Resolve<INPCRepository>();
            IPlayerRepository playerRepository = repositoryResolver.Resolve<IPlayerRepository>();
            IStatsRepository statsRepository = new NullStatsRepository();
            try
            {
                statsRepository = repositoryResolver.Resolve<IStatsRepository>();
            }
            catch
            {
                s_log.Warn("Failed to create stats repository. Stats will be disabled.");
            }

            s_log.Info("Precaching NPCs...");
            npcRepository.GetNPCs();

            s_log.Info("Precaching NPC Spawns...");
            npcRepository.GetNPCSpawns();

            s_log.Info("Precaching NPC Behaviours...");
            npcRepository.GetNPCBehaviours();

            s_log.Info("Precaching NPC Behaviour Vars...");
            npcRepository.GetNPCBehaviourVars();

            s_log.Info("Creating world...");
            World world = new World(accountRepository, npcRepository, playerRepository, statsRepository);

            s_log.Info("Initialising serializer...");
            ProtocolUtility.InitialiseSerializer();

            TcpListener listener = new TcpListener(IPAddress.Any, config.Port);
            listener.Start();
            s_log.Info("Listening for connections on " + listener.LocalEndpoint.ToString());

            while (true)
            {
                Socket socket = listener.AcceptSocket();
                world.AcceptSocket(socket);
            }
        }
    }
}

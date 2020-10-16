using StarbotLib;
using StarbotLib.Logging;
using StarbotLib.Pathfinding;
using StarbotLib.World;
using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;

namespace StarbotServerTest
{
    class StarbotServerTest
    {
        static void Main(string[] args)
        {
            CLogger.Info("waiting...");
            Console.ReadLine();

            var serverAddress = "ipc://starbot/StarbotServer";
            IpcClientChannel clientChannel = new IpcClientChannel();
            ChannelServices.RegisterChannel(clientChannel, false);
            RemotingConfiguration.RegisterActivatedClientType(typeof(StarbotServerCore), serverAddress);
            var server = new StarbotServerCore();
            if (server == null)
            {
                throw new Exception("Unable to connect to Starbot Server. Shutting down Starbot.");
            }
            RemotingConfiguration.RegisterActivatedClientType(typeof(Map), serverAddress);
            RemotingConfiguration.RegisterActivatedClientType(typeof(Location), serverAddress);
            RemotingConfiguration.RegisterActivatedClientType(typeof(WorldObject), serverAddress);
            RemotingConfiguration.RegisterActivatedClientType(typeof(Path), serverAddress);
            RemotingConfiguration.RegisterActivatedClientType(typeof(Route), serverAddress);

            CLogger.Info("connected...");

            Console.ReadLine();

            var map = new Map("test");
            map.maxX = 3;

            var loc = map.AddLocation(1, 1);

            CLogger.Info("map things...");

            Console.ReadLine();
        }
    }
}

using StarbotLib;
using StarbotLib.Logging;
using StarbotLib.Pathfinding;
using StarbotLib.World;
using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;

namespace StarbotServer
{
    class StarbotServer
    {
        static void Main(string[] args)
        {
            CLogger.Info("Starbot Server Started...");
            var serverProvider = new BinaryServerFormatterSinkProvider();
            serverProvider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
            var properties = new System.Collections.Hashtable();
            properties["portName"] = "starbot";
            IpcServerChannel serverChannel = new IpcServerChannel(properties, serverProvider);
            ChannelServices.RegisterChannel(serverChannel, false);

            RemotingConfiguration.RegisterWellKnownServiceType(typeof(StarbotServerCore), "StarbotServer", WellKnownObjectMode.Singleton);

            //RemotingConfiguration.ApplicationName = "StarbotServer";
            //RemotingConfiguration.RegisterActivatedServiceType(typeof(StarbotServerCore));
            //RemotingConfiguration.RegisterActivatedServiceType(typeof(Map));
            //RemotingConfiguration.RegisterActivatedServiceType(typeof(Location));
            //RemotingConfiguration.RegisterActivatedServiceType(typeof(WorldObject));
            //RemotingConfiguration.RegisterActivatedServiceType(typeof(Path));
            //RemotingConfiguration.RegisterActivatedServiceType(typeof(Route));
            Console.WriteLine("Listening on {0}", serverChannel.GetChannelUri());
            Console.WriteLine("Press <Enter> to exit.");
            Console.ReadLine();
        }
    }
}

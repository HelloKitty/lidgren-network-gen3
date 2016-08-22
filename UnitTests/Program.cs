using System;
using System.Reflection;
using Lidgren.Network;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace UnitTests
{
	class Program
	{
		static void Main(string[] args)
		{
			NetPeerConfiguration peerConfig = new NetPeerConfiguration("unittests");
			NetPeerConfiguration serverConfig = new NetPeerConfiguration("unittests") { Port = 5070, AcceptIncomingConnections = true };
			peerConfig.EnableUPnP = true;
			NetClient peer1 = new NetClient(peerConfig);
			NetClient peer2 = new NetClient(peerConfig);

			NetServer server = new NetServer(serverConfig);
			peer1.Start(); // needed for initialization
			peer2.Start(); // needed for initialization
			server.Start();

			peer1.Connect(new IPEndPoint(IPAddress.Loopback, 5070), peer1.CreateMessage());
			Thread.Sleep(1000);
			peer2.Connect(new IPEndPoint(IPAddress.Loopback, 5070), peer2.CreateMessage());
			Thread.Sleep(1000);

			server.ReadMessages(new List<NetIncomingMessage>());

			Console.WriteLine($"Unique identifier is {peer1.UniqueIdentifier}");

			ReadWriteTests.Run(peer1);

			NetQueueTests.Run();

			MiscTests.Run(peer1);

			BitVectorTests.Run();

			EncryptionTests.Run(peer1);

			var om = peer1.CreateMessage();
			peer1.SendUnconnectedMessage(om, new IPEndPoint(IPAddress.Loopback, 14242));
			try
			{
				peer1.SendUnconnectedMessage(om, new IPEndPoint(IPAddress.Loopback, 14242));
			}
			catch (NetException nex)
			{
				if (nex.Message != "This message has already been sent! Use NetPeer.SendMessage() to send to multiple recipients efficiently")
					throw;
			}

			peer1.Shutdown("bye");

			// read all message
			NetIncomingMessage inc = peer1.WaitMessage(5000);
			while (inc != null)
			{
				switch (inc.MessageType)
				{
					case NetIncomingMessageType.DebugMessage:
					case NetIncomingMessageType.VerboseDebugMessage:
					case NetIncomingMessageType.WarningMessage:
					case NetIncomingMessageType.ErrorMessage:
						Console.WriteLine("Peer message: " + inc.ReadString());
						break;
					case NetIncomingMessageType.Error:
						throw new Exception("Received error message!");
				}

				inc = peer1.ReadMessage();
			}
			Console.WriteLine($"Unique identifier for connection is {peer1.ServerConnection?.RemoteUniqueIdentifier}");
			Console.WriteLine($"Unique identifier for connection is {peer2.ServerConnection?.RemoteUniqueIdentifier}");

			foreach (NetConnection c in server.Connections)
				Console.WriteLine($"Unique identifier in Server connection is {c.RemoteUniqueIdentifier}");
			Console.WriteLine("Done");
			Console.ReadKey();
		}

		/// <summary>
		/// Helper method
		/// </summary>
		public static NetIncomingMessage CreateIncomingMessage(byte[] fromData, int bitLength)
		{
			NetIncomingMessage inc = (NetIncomingMessage)Activator.CreateInstance(typeof(NetIncomingMessage), true);
			typeof(NetIncomingMessage).GetField("m_data", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(inc, fromData);
			typeof(NetIncomingMessage).GetField("m_bitLength", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(inc, bitLength);
			return inc;
		}
	}
}

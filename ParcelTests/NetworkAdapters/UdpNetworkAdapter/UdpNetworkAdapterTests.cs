﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parcel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Parcel.Tests
{
    [TestClass()]
    public class UdpNetworkAdapterTests
    {
        [TestMethod()]
        public void OneClientOneServerTest()
        {
            Task.Run(async () =>
            {
                FieldInfo _selfField = typeof(UdpNetworkAdapter).GetField("_self", BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo _channelsField = typeof(UdpNetworkAdapter).GetField("_channels", BindingFlags.NonPublic | BindingFlags.Instance);
                PropertyInfo remoteProp = typeof(UdpNetworkAdapter).GetNestedType("PeerChannel", BindingFlags.NonPublic).GetProperty("Remote", BindingFlags.Public | BindingFlags.Instance);

                //Create Peers
                Peer clientPeer = new PeerBuilder()
                .SetAddress("localhost")
                .SetPort(9898)
                .AddProperty("Key", "Value")
                .AddProperty("Hello", "World");

                Peer serverPeer = new PeerBuilder()
                .SetAddress("localhost")
                .SetPort(9899)
                .AddProperty("I am", "The server");

                //Create two UdpNetworkAdapters with different ports
                ParcelSettings clientSettings = new ParcelSettingsBuilder().SetNetworkAdapter<UdpNetworkAdapter>()
                    .SetPeer(clientPeer)
                    .SetConnectionTimeout(2000)
                    .SetDisconnectionTimeout(10000);

                ParcelSettings serverSettings = new ParcelSettingsBuilder().SetNetworkAdapter<UdpNetworkAdapter>()
                    .SetPeer(serverPeer)
                    .SetConnectionTimeout(2000)
                    .SetDisconnectionTimeout(10000);



                ConnectionToken clientConnectionToken = new ConnectionToken("localhost", 9898);
                ConnectionToken serverConnectionToken = new ConnectionToken("localhost", 9899);

                UdpNetworkAdapter client = new UdpNetworkAdapter();
                UdpNetworkAdapter server = new UdpNetworkAdapter();

                client.Start(false, clientSettings);
                server.Start(true, serverSettings);

                Peer newServerPeer = await client.ConnectTo(serverConnectionToken);

                IDictionary clientChannels = (IDictionary)_channelsField.GetValue(client);
                IDictionary serverChannels = (IDictionary)_channelsField.GetValue(server);

                Peer clientRemote = remoteProp.GetValue(clientChannels[serverConnectionToken]) as Peer;
                Peer serverRemote = remoteProp.GetValue(serverChannels[clientConnectionToken]) as Peer;

                Peer clientSelf = (Peer)_selfField.GetValue(client);
                Peer serverSelf = (Peer)_selfField.GetValue(server);

                Assert.IsTrue(newServerPeer != null);
                Assert.AreEqual(serverPeer, newServerPeer);
                Assert.AreEqual(serverPeer, clientRemote);
                Assert.AreEqual(clientPeer, serverRemote);
                Assert.AreEqual(clientPeer, clientSelf);
                Assert.AreEqual(serverPeer, serverSelf);

                //TODO: Proper Disconnect

                client.Dispose();
                server.Dispose();

            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void MultipleClientOneServerTest()
        {
            const int NUM_OF_CLIENTS = 8;

            //Ensure multiple clients connecting to a single server works. 
            Task.Run(async () =>
            {
                FieldInfo _selfField = typeof(UdpNetworkAdapter).GetField("_self", BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo _channelsField = typeof(UdpNetworkAdapter).GetField("_channels", BindingFlags.NonPublic | BindingFlags.Instance);
                PropertyInfo remoteProp = typeof(UdpNetworkAdapter).GetNestedType("PeerChannel", BindingFlags.NonPublic).GetProperty("Remote", BindingFlags.Public | BindingFlags.Instance);

                Peer serverPeer = new PeerBuilder()
                .SetAddress("localhost")
                .SetPort(9901)
                .AddProperty("I am", "The server");

                ParcelSettings serverSettings = new ParcelSettingsBuilder().SetNetworkAdapter<UdpNetworkAdapter>()
                    .SetPeer(serverPeer)
                    .SetConnectionTimeout(2000)
                    .SetDisconnectionTimeout(10000);



                ConnectionToken serverConnectionToken = new ConnectionToken("localhost", 9901);
                UdpNetworkAdapter server = new UdpNetworkAdapter();
                server.Start(true, serverSettings);

                Peer[] clientPeers = new Peer[NUM_OF_CLIENTS];
                UdpNetworkAdapter[] clientAdapters = new UdpNetworkAdapter[NUM_OF_CLIENTS];

                for (int i = 0; i < NUM_OF_CLIENTS; i++)
                {
                    clientPeers[i] = new PeerBuilder()
                        .SetAddress("localhost")
                        .SetPort(9800 + i)
                        .AddProperty("Key", "Value")
                        .AddProperty("Hello", "World");

                    Console.WriteLine($"Starting Client {i} on Port {9800 + i}");
                    ParcelSettings clientSettings = new ParcelSettingsBuilder().SetNetworkAdapter<UdpNetworkAdapter>()
                    .SetPeer(clientPeers[i])
                    .SetConnectionTimeout(2000)
                    .SetDisconnectionTimeout(10000);

                    clientAdapters[i] = new UdpNetworkAdapter();
                    clientAdapters[i].Start(false, clientSettings);
                    Peer newServerPeer = await clientAdapters[i].ConnectTo(serverConnectionToken);

                    Assert.IsTrue(newServerPeer != null);
                    Assert.AreEqual(serverPeer, newServerPeer);
                }

                IDictionary serverChannels = (IDictionary)_channelsField.GetValue(server);
                Peer serverSelf = (Peer)_selfField.GetValue(server);

                Assert.AreEqual(serverPeer, serverSelf);

                for (int i = 0; i < NUM_OF_CLIENTS; i++)
                {
                    Peer serverRemote = remoteProp.GetValue(serverChannels[new ConnectionToken(clientPeers[i].Address, clientPeers[i].Port)]) as Peer;
                    Assert.AreEqual(clientPeers[i], serverRemote);

                    IDictionary clientChannels = (IDictionary)_channelsField.GetValue(clientAdapters[i]);
                    Peer clientSelf = (Peer)_selfField.GetValue(clientAdapters[i]);

                    Peer clientRemote = remoteProp.GetValue(clientChannels[serverConnectionToken]) as Peer;

                    Assert.AreEqual(serverPeer, clientRemote);
                    Assert.AreEqual(clientPeers[i], clientSelf);

                    clientAdapters[i].Dispose();
                }
                server.Dispose();

            }).GetAwaiter().GetResult();


        }
    }
}
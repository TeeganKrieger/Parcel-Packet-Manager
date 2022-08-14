using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parcel.DataStructures;
using Parcel.Debug;
using Parcel.Networking;
using Parcel.Packets;
using Parcel.Serialization;
using Parcel.Serialization.Tests;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Parcel.Tests
{
    [TestClass]
    public class EndToEndTests
    {
        [TestMethod]
        public void EndToEnd()
        {
            Task.Run(async () =>
            {
                Setup(8, out ParcelClient[] clients, out ParcelServer server);
                try
                {
                    await ConnectionTest(clients, server);
                    await PacketTest(clients, server);
                    await SyncedObjectTest(clients, server);
                }
                catch (Exception ex)
                {
                    throw;
                }
                finally
                {
                    Cleanup(clients, server);
                }
            }).GetAwaiter().GetResult();
        }

        private void Setup(int clientCount, out ParcelClient[] clients, out ParcelServer server)
        {
            Peer serverPeer = new PeerBuilder().SetAddress("localhost").SetPort(9999).AddProperty("name", "Server");

            FileLogger serverLogger = new FileLogger(Path.Combine(Path.GetTempPath(), $"/Parcel/Tests/E2E_server.log"));

            ParcelSettings serverSettings = new ParcelSettingsBuilder().SetPeer(serverPeer).SetUpdatesPerSecond(20)
                .SetUnreliablePacketGroupSize(4).SetReliablePacketGroupSize(10).SetConnectionTimeout(2500).SetDisconnectionTimeout(250000)
                .SetNetworkAdapter<UdpNetworkAdapter>().AddNetworkDebugger(new NetworkDebugger(serverLogger));

            server = new ParcelServer(serverSettings);

            clients = new ParcelClient[clientCount];

            for (int i = 0; i < clientCount; i++)
            {
                Peer clientPeer = new PeerBuilder().SetAddress("localhost").SetPort(9998 - i).AddProperty("name", $"Client {i}");

                FileLogger clientLogger = new FileLogger(Path.Combine(Path.GetTempPath(), $"/Parcel/Tests/E2E_client{i}.log"));

                ParcelSettings clientSettings = new ParcelSettingsBuilder().SetPeer(clientPeer).SetUpdatesPerSecond(20)
                    .SetUnreliablePacketGroupSize(4).SetReliablePacketGroupSize(10).SetConnectionTimeout(2500).SetDisconnectionTimeout(250000)
                    .SetNetworkAdapter<UdpNetworkAdapter>().AddNetworkDebugger(new NetworkDebugger(clientLogger));

                clients[i] = new ParcelClient(clientSettings);
            }

            SyncedObjectPatcher.Patch();
        }

        private async Task ConnectionTest(ParcelClient[] clients, ParcelServer server)
        {
            foreach (ParcelClient client in clients)
            {
                ConnectionResult result = await client.ConnectTo(server.Self.GetConnectionToken());
                Assert.IsTrue(result.Status == ConnectionStatus.Success);
                Assert.AreEqual(server.Self, client.Remote);
            }
        }

        private async Task PacketTest(ParcelClient[] clients, ParcelServer server)
        {
            foreach (ParcelClient client in clients)
            {
                Task<object>[] tasks = new Task<object>[150];
                for (int i = 0; i < tasks.Length; i++)
                {
                    TestPacket packet = new TestPacket(client.NetworkSettings.MillisecondsPerUpdate / 4);
                    client.Send(packet);
                    tasks[i] = packet.GetResult();
                }
                object[] results = await Task.WhenAll(tasks);

                for (int i = 0; i < tasks.Length; i++)
                {
                    string returnData = (string)results[i];
                    //Return data should be the name of the client who sent the packet originally
                    Assert.AreEqual(client.Self["name"], returnData);
                }

                for (int i = 0; i < tasks.Length; i++)
                {
                    TestPacket packet = new TestPacket(server.NetworkSettings.MillisecondsPerUpdate / 4);
                    server.Send(packet, client.Self);
                    tasks[i] = packet.GetResult();
                }

                results = await Task.WhenAll(tasks);

                for (int i = 0; i < tasks.Length; i++)
                {
                    string returnData = (string)results[i];
                    //Return data should be the name of the client who sent the packet originally
                    Assert.AreEqual(server.Self["name"], returnData);
                }
            }
        }

        private async Task SyncedObjectTest(ParcelClient[] clients, ParcelServer server)
        {
            Dictionary<Peer, TestSyncedObject> soDict = await CreateSyncedObjects();
            await AddSyncedObjectSubscriptions(soDict);

            await PerformSyncedObjectModification(soDict, (TestSyncedObject tso) => { tso.Int = 420; });
            await PerformSyncedObjectModification(soDict, (TestSyncedObject tso) => { tso.String = "Hello World"; });
            await PerformSyncedObjectModification(soDict, (TestSyncedObject tso) => { tso.Int = int.MaxValue; });
            await PerformSyncedObjectModification(soDict, (TestSyncedObject tso) => { tso.String = null; });

            await RemoveSyncedObjectSubscriptions(soDict);
            await DestroySyncedObjects(soDict);

            //Create Synced Objects
            async Task<Dictionary<Peer, TestSyncedObject>> CreateSyncedObjects()
            {
                bool failed = false;
                CancellationTokenSource cancellationToken = new CancellationTokenSource();
                Task failAfter3 = Task.Run(async () =>
                {
                    failed = false;
                    await Task.Delay(3000);
                    failed = true;
                }, cancellationToken.Token);

                Dictionary<Peer, TestSyncedObject> syncedObjects = new Dictionary<Peer, TestSyncedObject>();
                Task[] tasks = new Task[clients.Length];

                for (int i = 0; i < clients.Length; i++)
                {
                    TestSyncedObject so = server.CreateSyncedObject<TestSyncedObject>(clients[i].Self);
                    syncedObjects.Add(clients[i].Self, so);
                    int index = i;
                    tasks[i] = Task.Run(async () =>
                    {
                        while (!clients[index].TryGetSyncedObject<TestSyncedObject>(so.ID, out _))
                            await Task.Delay(clients[index].NetworkSettings.MillisecondsPerUpdate);
                    });
                }

                await Task.WhenAny(failAfter3, Task.WhenAll(tasks));
                cancellationToken.Cancel();
                if (failed)
                    Assert.Fail();

                return syncedObjects;
            }

            //Add subscriptions
            async Task AddSyncedObjectSubscriptions(Dictionary<Peer, TestSyncedObject> syncedObjects)
            {
                bool failed = false;
                CancellationTokenSource cancellationToken = new CancellationTokenSource();
                Task failAfter3 = Task.Run(async () =>
                {
                    failed = false;
                    await Task.Delay(3000);
                    failed = true;
                }, cancellationToken.Token);

                List<Task> tasks = new List<Task>();

                for (int i = 0; i < clients.Length; i++)
                {
                    TestSyncedObject so = syncedObjects[clients[i].Self];

                    for (int j = 0; j < clients.Length; j++)
                    {
                        if (clients[i].Self == clients[j].Self)
                            continue;

                        so.AddSubscriptions(clients[j].Self);
                        int index = j;
                        tasks.Add(Task.Run(async () =>
                        {
                            while (!clients[index].TryGetSyncedObject<TestSyncedObject>(so.ID, out _))
                                await Task.Delay(clients[index].NetworkSettings.MillisecondsPerUpdate);
                        }));
                    }
                }

                await Task.WhenAny(failAfter3, Task.WhenAll(tasks));
                cancellationToken.Cancel();
                if (failed)
                    Assert.Fail();
            }

            //Perform Modifications
            async Task PerformSyncedObjectModification(Dictionary<Peer, TestSyncedObject> syncedObjects, Action<TestSyncedObject> modificationAction)
            {
                bool failed = false;
                CancellationTokenSource cancellationToken = new CancellationTokenSource();
                Task failAfter3 = Task.Run(async () =>
                {
                    failed = false;
                    await Task.Delay(3000);
                    failed = true;
                }, cancellationToken.Token);

                List<Task> tasks = new List<Task>();

                for (int i = 0; i < clients.Length; i++)
                {
                    TestSyncedObject so = syncedObjects[clients[i].Self];

                    for (int j = 0; j < clients.Length; j++)
                    {
                        if (clients[i].Self == clients[j].Self)
                            continue;

                        modificationAction(so);
                        TestSyncedObject clone = (TestSyncedObject)so.Clone();
                        int index = j;
                        tasks.Add(Task.Run(async () =>
                        {
                            while (clients[index].TryGetSyncedObject<TestSyncedObject>(so.ID, out TestSyncedObject mso) && !mso.Equals(clone))
                                await Task.Delay(clients[index].NetworkSettings.MillisecondsPerUpdate);
                        }));
                    }
                }

                await Task.WhenAny(failAfter3, Task.WhenAll(tasks));
                cancellationToken.Cancel();
                if (failed)
                    Assert.Fail();
            }

            //Remove Subscriptions
            async Task RemoveSyncedObjectSubscriptions(Dictionary<Peer, TestSyncedObject> syncedObjects)
            {
                bool failed = false;
                CancellationTokenSource cancellationToken = new CancellationTokenSource();
                Task failAfter3 = Task.Run(async () =>
                {
                    failed = false;
                    await Task.Delay(3000);
                    failed = true;
                }, cancellationToken.Token);

                List<Task> tasks = new List<Task>();

                for (int i = 0; i < clients.Length; i++)
                {
                    TestSyncedObject so = syncedObjects[clients[i].Self];

                    for (int j = 0; j < clients.Length; j++)
                    {
                        if (clients[i].Self == clients[j].Self)
                            continue;

                        so.RemoveSubscriptions(clients[j].Self);
                        int index = j;
                        tasks.Add(Task.Run(async () =>
                        {
                            while (clients[index].TryGetSyncedObject<TestSyncedObject>(so.ID, out _))
                                await Task.Delay(clients[index].NetworkSettings.MillisecondsPerUpdate);
                        }));
                    }
                }

                await Task.WhenAny(failAfter3, Task.WhenAll(tasks));
                cancellationToken.Cancel();
                if (failed)
                    Assert.Fail();
            }

            //Destroy Synced Objects
            async Task DestroySyncedObjects(Dictionary<Peer, TestSyncedObject> syncedObjects)
            {
                bool failed = false;
                CancellationTokenSource cancellationToken = new CancellationTokenSource();
                Task failAfter3 = Task.Run(async () =>
                {
                    failed = false;
                    await Task.Delay(3000);
                    failed = true;
                }, cancellationToken.Token);

                Task[] tasks = new Task[clients.Length];

                for (int i = 0; i < clients.Length; i++)
                {
                    syncedObjects.Remove(clients[i].Self, out TestSyncedObject so);
                    Assert.IsTrue(server.DestroySyncedObject(so.ID));

                    int index = i;
                    tasks[i] = Task.Run(async () =>
                    {
                        while (clients[index].TryGetSyncedObject<TestSyncedObject>(so.ID, out _))
                            await Task.Delay(clients[index].NetworkSettings.MillisecondsPerUpdate);
                    });
                }

                await Task.WhenAny(failAfter3, Task.WhenAll(tasks));
                cancellationToken.Cancel();
                if (failed)
                    Assert.Fail();
            }
        }

        private void Cleanup(ParcelClient[] clients, ParcelServer server)
        {
            foreach (ParcelClient client in clients)
            {
                client.Disconnect();
                client.Dispose();
            }
            server.Dispose();
        }


        private class TestPacket : Packet
        {
            private static ConcurrentDictionary<string, Result> ClientResultsDictionary = new ConcurrentDictionary<string, Result>();
            private static ConcurrentDictionary<string, Result> ServerResultsDictionary = new ConcurrentDictionary<string, Result>();

            private int _resultCheckTime;
            private bool _sent;
            private string GUID { get; set; }
            private object ReturnData { get; set; }

            private TestPacket() { }

            public TestPacket(int resultCheckTime)
            {
                this._resultCheckTime = resultCheckTime;
                this.GUID = Guid.NewGuid().ToString();
            }

            private TestPacket(string guid, object returnData)
            {
                this.GUID = guid;
                this.ReturnData = returnData;
            }

            public async Task<object> GetResult()
            {
                //Wait until the packet is sent
                while (!this._sent)
                    await Task.Delay(this._resultCheckTime);

                ConcurrentDictionary<string, Result> resultsDictionary = this.IsServer ? ServerResultsDictionary : ClientResultsDictionary;

                //Wait until response is received
                Result result = null;
                while (resultsDictionary.TryGetValue(this.GUID, out result) && result.Status == false)
                {
                    await Task.Delay(this._resultCheckTime);
                }
                resultsDictionary.TryRemove(this.GUID, out result);
                return result?.ReturnData;
            }

            protected internal override void OnSend()
            {
                if (this.ReturnData == null)
                {
                    ConcurrentDictionary<string, Result> resultsDictionary = this.IsServer ? ServerResultsDictionary : ClientResultsDictionary;
                    resultsDictionary.TryAdd(this.GUID, new Result());
                }
                this._sent = true;
            }

            protected internal override void OnReceive()
            {
                if (this.IsServer)
                {
                    if (this.ReturnData != null)
                    {
                        if (ServerResultsDictionary.TryGetValue(this.GUID, out Result result))
                        {
                            result.ReturnData = this.ReturnData;
                            result.Status = true;
                        }
                    }
                    else
                    {
                        string senderName = (string)this.Sender["name"];
                        TestPacket returnPacket = new TestPacket(this.GUID, senderName);
                        this.Server.Send(returnPacket, Sender);
                    }
                }
                else
                {
                    if (this.ReturnData != null)
                    {
                        if (ClientResultsDictionary.TryGetValue(this.GUID, out Result result))
                        {
                            result.ReturnData = this.ReturnData;
                            result.Status = true;
                        }
                    }
                    else
                    {
                        string senderName = (string)this.Sender["name"];
                        TestPacket returnPacket = new TestPacket(this.GUID, senderName);
                        this.Client.Send(returnPacket);
                    }
                }
            }

            private class Result
            {
                public bool Status { get; set; }
                public object ReturnData { get; set; }

                public Result()
                {
                    this.Status = false;
                    this.ReturnData = null;
                }
            }
        }

        private class TestSyncedObject : SyncedObject, ICloneable
        {
            public string String { get; set; }
            public int Int { get; set; }

            public TestSyncedObject()
            {
                this.String = null;
                this.Int = 0;
            }

            public object Clone()
            {
                return this.MemberwiseClone();
            }

            public override bool Equals(object obj)
            {
                return base.Equals(obj) && obj is TestSyncedObject so && so.String == this.String && so.Int == this.Int;
            }
        }

    }
}

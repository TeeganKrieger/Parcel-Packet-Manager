using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Parcel.Tests
{
    [TestClass()]
    public class ParcelSettingsTests
    {
        public void CreateUsingBuilderTest()
        {
            Peer settingsPeer = new PeerBuilder().SetAddress("localhost").SetPort(9991);

            ParcelSettings settings = new ParcelSettingsBuilder().SetPeer(settingsPeer).SetNetworkAdapter<UdpNetworkAdapter>();

            Assert.IsNotNull(settings);
            Assert.AreEqual(settingsPeer, settings.Peer);
            Assert.AreEqual(typeof(UdpNetworkAdapter), settings.NetworkAdapterType);
        }

        public void CreateNoPeer()
        {
            bool caught = false;
            try
            {
                ParcelSettings settings = new ParcelSettingsBuilder().SetNetworkAdapter<UdpNetworkAdapter>();
            }
            catch (InvalidOperationException ex)
            {
                caught = true;
            }
            Assert.AreEqual(true, caught);
        }

        public void CreateNoAdapter()
        {
            bool caught = false;

            Peer settingsPeer = new PeerBuilder().SetAddress("localhost").SetPort(9991);
            
            try
            { 
            ParcelSettings settings = new ParcelSettingsBuilder().SetPeer(settingsPeer);
            }
            catch (InvalidOperationException ex)
            {
                caught = true;
            }
            Assert.AreEqual(true, caught);
        }
    }
}
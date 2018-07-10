using System;
using System.Collections.Generic;
using System.Threading;
using TorrentSwifter.Helpers;
using TorrentSwifter.Logging;

namespace TorrentSwifter.Peers
{
    internal static class PeerManager
    {
        #region Fields
        private static bool isRunning = false;

        private static List<PeerConnectionTCP> pendingTcpConnections = new List<PeerConnectionTCP>();
        private static List<PeerConnectionTCP> tcpConnections = new List<PeerConnectionTCP>();
        #endregion

        #region Internal Methods
        internal static void Initialize()
        {
            if (isRunning)
                return;

            var thread = new Thread(UpdateLoop);
            thread.Priority = ThreadPriority.BelowNormal;
            thread.Name = "PeerManagerThread";

            isRunning = true;
            thread.Start();
        }

        internal static void Uninitialize()
        {
            if (!isRunning)
                return;

            isRunning = false;
            DisconnectAll();
        }

        internal static void DisconnectAll()
        {
            lock (pendingTcpConnections)
            {
                foreach (var connection in pendingTcpConnections)
                {
                    connection.Disconnect();
                }
                pendingTcpConnections.Clear();
            }

            lock (tcpConnections)
            {
                foreach (var connection in tcpConnections)
                {
                    connection.Disconnect();
                }
                tcpConnections.Clear();
            }
        }

        internal static void AddPendingConnection(PeerConnectionTCP connection)
        {
            lock (pendingTcpConnections)
            {
                pendingTcpConnections.Add(connection);
            }
        }

        internal static PeerID GetPeerID()
        {
            var assemblyVersion = AssemblyHelper.GetAssemblyVersion(typeof(PeerManager));
            int majorVersion = Clamp(assemblyVersion.Major, 0, 9);
            int minorVersion = Clamp(assemblyVersion.Minor, 0, 9);
            int buildVersion = Clamp(assemblyVersion.Build, 0, 9);
            int revisionVersion = Clamp(assemblyVersion.Revision, 0, 9);

            var idBytes = new byte[20];
            idBytes[0] = (byte)'-';
            idBytes[1] = (byte)'s';
            idBytes[2] = (byte)'w';
            idBytes[3] = (byte)('0' + majorVersion);
            idBytes[4] = (byte)('0' + minorVersion);
            idBytes[5] = (byte)('0' + buildVersion);
            idBytes[6] = (byte)('0' + revisionVersion);
            idBytes[7] = (byte)'-';

            var guid = Guid.NewGuid();
            var guidBytes = guid.ToByteArray();
            Buffer.BlockCopy(guidBytes, 0, idBytes, 8, 12);

            return new PeerID(idBytes);
        }
        #endregion

        #region Private Methods
        private static void UpdateLoop()
        {
            while (isRunning)
            {
                try
                {
                    UpdatePendingConnections();
                    UpdateConnections();

                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    Log.LogErrorException(ex);
                }
            }
        }

        private static void AddConnection(PeerConnectionTCP connection)
        {
            lock (tcpConnections)
            {
                tcpConnections.Add(connection);
            }
        }

        private static void UpdatePendingConnections()
        {
            lock (pendingTcpConnections)
            {
                int pendingCount = pendingTcpConnections.Count;
                for (int i = 0; i < pendingCount; i++)
                {
                    var connection = pendingTcpConnections[i];
                    connection.Update();

                    if (!connection.IsConnected && !connection.IsConnecting)
                    {
                        pendingTcpConnections.RemoveAt(i);
                        i--;
                        pendingCount--;
                    }
                    else if (connection.IsConnected && connection.IsHandshakeReceived)
                    {
                        pendingTcpConnections.RemoveAt(i);
                        i--;
                        pendingCount--;

                        AddConnection(connection);
                    }
                }
            }
        }

        private static void UpdateConnections()
        {
            lock (tcpConnections)
            {
                int connectionCount = tcpConnections.Count;
                for (int i = 0; i < connectionCount; i++)
                {
                    var connection = tcpConnections[i];
                    connection.Update();

                    if (!connection.IsConnected)
                    {
                        tcpConnections.RemoveAt(i);
                        i--;
                        connectionCount--;
                    }
                }
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            return (value < min ? min : (value > max ? max : value));
        }
        #endregion
    }
}

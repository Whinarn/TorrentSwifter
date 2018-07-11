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
        #endregion
    }
}

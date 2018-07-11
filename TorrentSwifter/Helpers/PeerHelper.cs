using System;
using TorrentSwifter.Peers;

namespace TorrentSwifter.Helpers
{
    // TODO: Add support to figure out client info from remote peer IDs

    internal static class PeerHelper
    {
        internal static PeerID GetNewPeerID()
        {
            var assemblyVersion = AssemblyHelper.GetAssemblyVersion(typeof(PeerHelper));
            int majorVersion = MathHelper.Clamp(assemblyVersion.Major, 0, 9);
            int minorVersion = MathHelper.Clamp(assemblyVersion.Minor, 0, 9);
            int buildVersion = MathHelper.Clamp(assemblyVersion.Build, 0, 9);
            int revisionVersion = MathHelper.Clamp(assemblyVersion.Revision, 0, 9);

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
    }
}

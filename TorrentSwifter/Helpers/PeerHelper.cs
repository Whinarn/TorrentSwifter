using System;
using System.Collections.Generic;
using System.Text;
using TorrentSwifter.Peers;

namespace TorrentSwifter.Helpers
{
    // TODO: Add support to figure out client info from remote peer IDs

    internal static class PeerHelper
    {
        #region Consts
        private const string ClientID = "sw";

        private const string ShadowBase64 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz.-";
        #endregion

        #region Fields
        #region Azureus-Style Clients
        // List retrieved from https://wiki.theory.org/index.php/BitTorrentSpecification#peer_id
        private static readonly Dictionary<string, string> azureusStyleClients = new Dictionary<string, string>()
        {
            { "7T", "aTorrent for Android" },
            { "AB", "AnyEvent::BitTorrent" },
            { "AG", "Ares" },
            { "A~", "Ares" },
            { "AR", "Arctic" },
            { "AV", "Avicora" },
            { "AT", "Artemis" },
            { "AX", "BitPump" },
            { "AZ", "Azureus" },
            { "BB", "BitBuddy" },
            { "BC", "BitComet" },
            { "BE", "Baretorrent" },
            { "BF", "Bitflu" },
            { "BG", "BTG" },
            { "BL", "BitCometLite" }, // 6 digit version number // Also BitBlinder
            { "BP", "BitTorrent Pro" },
            { "BR", "BitRocket" },
            { "BS", "BTSlave" },
            { "BT", "Mainline BitTorrent" },
            { "Bt", "Bt" },
            { "BW", "BitWombat" },
            { "BX", "~Bittorrent X" },
            { "CD", "Enhanced CTorrent" },
            { "CT", "CTorrent" },
            { "DE", "DelugeTorrent" },
            { "DP", "Propagate Data Client" },
            { "EB", "EBit" },
            { "ES", "Electric Sheep" },
            { "FC", "FileCroc" },
            { "FD", "Free Download Manager" },
            { "FT", "FoxTorrent" },
            { "FX", "Freebox BitTorrent" },
            { "GS", "GSTorrent" },
            { "HK", "Hekate" },
            { "HL", "Halite" },
            { "HM", "hMule" },
            { "HN", "Hydranode" },
            { "IL", "iLivid" },
            { "JS", "Justseed.it client" },
            { "JT", "JavaTorrent" },
            { "KG", "KGet" },
            { "KT", "KTorrent" },
            { "LC", "LeechCraft" },
            { "LH", "LH-ABC" },
            { "LP", "Lphant" },
            { "LT", "libtorrent" },
            { "lt", "libTorrent" },
            { "LW", "LimeWire" },
            { "MK", "Meerkat" },
            { "MO", "MonoTorrent" },
            { "MP", "MooPolice" },
            { "MR", "Miro" },
            { "MT", "MoonlightTorrent" },
            { "NB", "Net::BitTorrent" },
            { "NX", "Net Transport" },
            { "OS", "OneSwarm" },
            { "OT", "OmegaTorrent" },
            { "PB", "Protocol::BitTorrent" },
            { "PD", "Pando" },
            { "PI", "PicoTorrent" },
            { "PT", "PHPTracker" },
            { "qB", "qBittorrent" },
            { "QD", "QQDownload" },
            { "QT", "Qt 4" },
            { "RT", "Retriever" },
            { "RZ", "RezTorrent" },
            { "S~", "Shareaza" },
            { "SB", "~Swiftbit" },
            { "SD", "Thunder" },
            { "SM", "SoMud" },
            { "SP", "BitSpirit" },
            { "SS", "SwarmScope" },
            { "ST", "SymTorrent" },
            { "st", "sharktorrent" },
            { "SZ", "Shareaza" },
            { "TB", "Torch" },
            { "TE", "terasaur Seed Bank" },
            { "TL", "Tribler" },
            { "TN", "TorrentDotNET" },
            { "TR", "Transmission" },
            { "TS", "Torrentstorm" },
            { "TT", "TuoTu" },
            { "UL", "uLeecher!" },
            { "UM", "µTorrent Mac" },
            { "UT", "µTorrent" },
            { "UW", "µTorrent Web" },
            { "VG", "Vagaa" },
            { "WD", "WebTorrent Desktop" },
            { "WT", "BitLet" },
            { "WW", "WebTorrent" },
            { "WY", "FireTorrent" },
            { "XF", "Xfplay" },
            { "XL", "Xunlei" },
            { "XS", "XSwifter" },
            { "XT", "XanTorrent" },
            { "XX", "Xtorrent" },
            { "ZT", "ZipTorrent" },
            { ClientID, "TorrentSwifter" }
        };
        #endregion

        #region Shad0w-Style Clients
        private static readonly Dictionary<char, string> shadowStyleClients = new Dictionary<char, string>()
        {
            { 'A', "ABC" },
            { 'O', "Osprey Permaseed" },
            { 'Q', "BTQueue" },
            { 'R', "Tribler" },
            { 'S', "Shadow's client" },
            { 'T', "BitTornado" },
            { 'U', "UPnP NAT Bit Torrent" }
        };
        #endregion
        #endregion

        #region Internal Methods
        internal static PeerID GetNewPeerID()
        {
            var assemblyVersion = AssemblyHelper.GetAssemblyVersion(typeof(PeerHelper));
            int majorVersion = MathHelper.Clamp(assemblyVersion.Major, 0, 9);
            int minorVersion = MathHelper.Clamp(assemblyVersion.Minor, 0, 9);
            int buildVersion = MathHelper.Clamp(assemblyVersion.Build, 0, 9);
            int revisionVersion = MathHelper.Clamp(assemblyVersion.Revision, 0, 9);

            var idBytes = new byte[20];
            idBytes[0] = (byte)'-';
            idBytes[1] = (byte)ClientID[0];
            idBytes[2] = (byte)ClientID[1];
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

        internal static bool TryGetPeerClientInfo(PeerID peerID, out string clientName, out string clientVersion)
        {
            clientName = null;
            clientVersion = null;

            byte[] idBytes = peerID.ID;
            if (idBytes == null || idBytes.Length != 20)
                return false;

            char[] idChars = new char[20];
            for (int i = 0; i < idChars.Length; i++)
            {
                idChars[i] = (char)idBytes[i];
            }

            if (TryGetAzureusStyleClientInfo(idChars, out clientName, out clientVersion))
                return true;
            else if (TryGetOtherClientInfo(idChars, out clientName, out clientVersion))
                return true;
            else if (TryGetShadowStyleClientInfo(idChars, out clientName, out clientVersion))
                return true;
            else
                return false;
        }
        #endregion

        #region Private Methods
        private static bool TryGetAzureusStyleClientInfo(char[] idChars, out string clientName, out string clientVersion)
        {
            clientName = null;
            clientVersion = null;

            if (idChars[0] != '-' || idChars[7] != '-')
                return false;

            for (int i = 3; i < 7; i++)
            {
                if (!char.IsDigit(idChars[i]))
                    return false;
            }

            string clientID = new string(idChars, 1, 2);
            if (azureusStyleClients.TryGetValue(clientID, out clientName))
            {
                clientVersion = string.Format("{0}.{1}.{2}.{3}", idChars[3], idChars[4], idChars[5], idChars[6]);
                return true;
            }

            return false;
        }

        private static bool TryGetShadowStyleClientInfo(char[] idChars, out string clientName, out string clientVersion)
        {
            clientName = null;
            clientVersion = null;

            char clientID = idChars[0];
            if (shadowStyleClients.TryGetValue(clientID, out clientName))
            {
                var versionParts = new string[5];
                int versionPartCount = 0;
                for (int i = 1; i < 6; i++)
                {
                    if (idChars[i] == '-')
                        break;

                    int versionPartInteger;
                    if (!TryParseShadowInteger(idChars[i], out versionPartInteger))
                        return false;

                    versionParts[versionPartCount++] = versionPartInteger.ToString();
                }
                clientVersion = string.Join(".", versionParts);
                return true;
            }
            
            return false;
        }

        private static bool TryGetOtherClientInfo(char[] idChars, out string clientName, out string clientVersion)
        {
            // TODO: Parse clients like BitComment, BitLord, XBT Client, Opera, MLdonkey, Bits on Wheels, Queen Bee, BitTyrant, TorrenTopia, BitSpirit, Rufus, G3 Torrent, FlashGet, BitCometLite, 

            clientName = null;
            clientVersion = null;
            return false;
        }

        private static bool TryParseShadowInteger(char c, out int value)
        {
            int charIndex = ShadowBase64.IndexOf(c);
            if (charIndex != -1)
            {
                value = charIndex;
                return true;
            }
            else
            {
                value = 0;
                return false;
            }
        }
        #endregion
    }
}

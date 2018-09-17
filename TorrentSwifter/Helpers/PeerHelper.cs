#region License
/*
MIT License

Copyright (c) 2018 Mattias Edlund

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion

using System;
using System.Collections.Generic;
using TorrentSwifter.Peers;

namespace TorrentSwifter.Helpers
{
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

            char lastVersionChar = 'S';
#if DEBUG
            lastVersionChar = 'A';
#endif

            var idBytes = new byte[20];
            idBytes[0] = (byte)'-';
            idBytes[1] = (byte)ClientID[0];
            idBytes[2] = (byte)ClientID[1];
            idBytes[3] = (byte)('0' + majorVersion);
            idBytes[4] = (byte)('0' + minorVersion);
            idBytes[5] = (byte)('0' + buildVersion);
            idBytes[6] = (byte)lastVersionChar;
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

            if (!AreCharsDigits(idChars, 3, 3))
                return false;

            string clientID = new string(idChars, 1, 2);
            if (!azureusStyleClients.TryGetValue(clientID, out clientName))
            {
                clientName = string.Format("Unknown ({0})", clientID);
            }

            char lastVersionChar = idChars[6];
            if (char.IsDigit(lastVersionChar))
            {
                clientVersion = string.Format("{0}.{1}.{2}.{3}", idChars[3], idChars[4], idChars[5], idChars[6]);
            }
            else if (lastVersionChar == 'A' || lastVersionChar == 'a')
            {
                clientVersion = string.Format("{0}.{1}.{2} Alpha", idChars[3], idChars[4], idChars[5]);
            }
            else if (lastVersionChar == 'B' || lastVersionChar == 'b')
            {
                clientVersion = string.Format("{0}.{1}.{2} Beta", idChars[3], idChars[4], idChars[5]);
            }
            else
            {
                clientVersion = string.Format("{0}.{1}.{2}", idChars[3], idChars[4], idChars[5]);
            }
            return true;
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
            // Detect BitComent (older versions) and BitLord
            if (idChars[0] == 'e' && idChars[1] == 'x' && idChars[2] == 'b' && idChars[3] == 'c' && AreCharsDigits(idChars, 4, 4))
            {
                if (idChars[8] == 'L' && idChars[9] == 'O' && idChars[10] == 'R' && idChars[11] == 'D')
                {
                    clientName = "BitLord";
                }
                else
                {
                    clientName = "BitComet";
                }

                int versionMajor, versionMinor;
                string versionMajorText = new string(idChars, 4, 2);
                string versionMinorText = new string(idChars, 6, 2);

                if (int.TryParse(versionMajorText, out versionMajor) && int.TryParse(versionMinorText, out versionMinor))
                {
                    clientVersion = string.Format("{0}.{1}", versionMajor, versionMinor);
                    return true;
                }
            }

            // Detect XBT Client
            if (idChars[0] == 'X' && idChars[1] == 'B' && idChars[2] == 'T' && AreCharsDigits(idChars, 3, 3) && idChars[7] == '-')
            {
                clientName = "XBT Client";
                clientVersion = string.Format("{0}.{1}.{2}", idChars[3], idChars[4], idChars[5]);
                return true;
            }

            // Detect Opera
            if (idChars[0] == 'O' && idChars[1] == 'P' && AreCharsDigits(idChars, 2, 4))
            {
                clientName = "Opera";
                clientVersion = string.Format("{0}.{1}.{2}.{3}", idChars[2], idChars[3], idChars[4], idChars[5]);
                return true;
            }

            // Detect MLdonkey
            if (idChars[0] == '-' && idChars[1] == 'M' && idChars[2] == 'L')
            {
                int dashIndex = FindCharInArray(idChars, 3, '-');
                if (dashIndex != -1 && TryParseDottedVersion(idChars, 3, (dashIndex - 3), out clientVersion))
                {
                    clientName = "MLdonkey";
                    return true;
                }
            }

            // Detect Mainline-style client
            if (idChars[0] == 'M' && TryParseVersion(idChars, 1, 3, '-', out clientVersion))
            {
                clientName = "Mainline Client";
                return true;
            }
            else if (idChars[0] == 'Q' && TryParseVersion(idChars, 1, 3, '-', out clientVersion))
            {
                clientName = "Queen Bee";
                return true;
            }

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

        private static bool AreCharsDigits(char[] chars, int offset, int count)
        {
            bool result = true;
            for (int i = offset; i < (offset + count); i++)
            {
                if (!char.IsDigit(chars[i]))
                {
                    result = false;
                    break;
                }
            }
            return result;
        }

        private static bool TryParseDottedVersion(char[] chars, int offset, int count, out string version)
        {
            bool isValid = true;
            bool previousWasDot = true;
            int dotCount = 0;
            for (int i = offset; i < (offset + count); i++)
            {
                char c = chars[i];
                if (char.IsDigit(c))
                {
                    previousWasDot = false;
                }
                else if (c == '.')
                {
                    if (!previousWasDot)
                    {
                        previousWasDot = true;
                        ++dotCount;
                    }
                    else
                    {
                        isValid = false;
                        break;
                    }
                }
                else
                {
                    isValid = false;
                    break;
                }
            }

            if (isValid && !previousWasDot && dotCount > 0)
            {
                version = new string(chars, offset, count);
                return true;
            }

            version = null;
            return false;
        }

        private static bool TryParseVersion(char[] chars, int startOffset, int partCount, char separationChar, out string version)
        {
            bool lastWasSeparation = true;
            int endIndex = -1;
            int foundPartCount = 0;
            for (int i = startOffset; i < chars.Length; i++)
            {
                char c = chars[i];
                if (char.IsDigit(c))
                {
                    lastWasSeparation = false;
                }
                else if (c == separationChar)
                {
                    if (!lastWasSeparation && foundPartCount < partCount)
                    {
                        lastWasSeparation = true;
                        ++foundPartCount;
                    }
                    else
                    {
                        endIndex = i;
                        break;
                    }
                }
                else
                {
                    endIndex = i;
                    break;
                }
            }

            if (!lastWasSeparation && endIndex != -1 && foundPartCount == partCount)
            {
                version = new string(chars, startOffset, (endIndex - startOffset));
                if (separationChar != '.')
                {
                    version = version.Replace(separationChar, '.');
                }
                return true;
            }

            version = null;
            return false;
        }

        private static int FindCharInArray(char[] chars, int startOffset, char c)
        {
            int result = -1;
            for (int i = startOffset; i < chars.Length; i++)
            {
                if (chars[i] == c)
                {
                    result = i;
                    break;
                }
            }
            return result;
        }
        #endregion
    }
}

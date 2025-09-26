using System;
using System.Security.Cryptography;
using static System.Convert;
using static System.Text.Encoding;
using Steamworks;

namespace ArtAi.util
{
    public static class SteamUtil
    {
        public static string GetUserIdHash()
        {
            try
            {
                return SteamUser.GetSteamID().GetAccountID().m_AccountID.ToString();
            }
            catch (InvalidOperationException)
            {
                return "unknown";
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ArtAi.util;
using ArtAi.util.json;
using Verse;

namespace ArtAi.Server
{
    // Result of GET <server url>/info: the local server reports its status there,
    // other servers (the public one) answer with an HTTP error, which still means they are reachable
    public class ServerCheck
    {
        public enum Result { NoAnswer, Answers, LocalServer }

        public readonly Result Kind;
        // Kind == LocalServer
        public readonly ServerStatus Status;
        // Kind == NoAnswer
        public readonly string Error;

        private ServerCheck(Result kind, ServerStatus status, string error)
        {
            Kind = kind;
            Status = status;
            Error = error;
        }

        // Blocks for up to the HTTP timeout: call from a background thread
        public static ServerCheck Run(string url)
        {
            try
            {
                var info = HttpUtil.DoGetText(url + "/info").FromJson<InfoRs>();
                return info?.server == "artai-local"
                    ? new ServerCheck(Result.LocalServer, new ServerStatus(info.state, info.args, info.message), null)
                    : new ServerCheck(Result.Answers, null, null);
            }
            catch (ServerUnavailableException e)
            {
                return new ServerCheck(Result.NoAnswer, null, e.Message);
            }
            catch (Exception)
            {
                // HTTP error code or unexpected content: something answers at this address
                return new ServerCheck(Result.Answers, null, null);
            }
        }

        // main thread
        public string Text()
        {
            switch (Kind)
            {
                case Result.LocalServer:
                    return "AiArtTestLocal".Translate(Status.Text());
                case Result.Answers:
                    return "AiArtTestAnswers".Translate();
                default:
                    return "AiArtTestNoAnswer".Translate(Error);
            }
        }

        [DataContract]
        private class InfoRs
        {
            [DataMember] public string server { get; set; }
            [DataMember] public string state { get; set; }
            [DataMember] public List<string> args { get; set; }
            [DataMember] public string message { get; set; }
        }
    }
}

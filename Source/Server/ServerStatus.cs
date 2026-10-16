using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ArtAi.Server
{
    // What the local server reports about itself (state "downloading", args ["3.1", "11.3", "27"]).
    // Shown translated: key AiArtServerState_<state>, args fill {0}, {1}...;
    // message is the server's English text, used for unknown states.
    public class ServerStatus
    {
        public readonly string State;
        public readonly List<string> Args;
        public readonly string Message;

        public ServerStatus(string state, List<string> args, string message)
        {
            State = state;
            Args = args ?? new List<string>();
            Message = message;
        }

        // main thread
        public string Text()
        {
            string key = "AiArtServerState_" + State;
            if (State == null || !key.CanTranslate())
            {
                return Message ?? "";
            }
            return key.Translate(Args.Select(arg => (NamedArgument)arg).ToArray());
        }
    }
}

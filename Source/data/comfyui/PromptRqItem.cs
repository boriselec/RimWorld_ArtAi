using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ArtAi.data.comfyui
{
    [DataContract]
    public class PromptRqItem
    {
        [DataMember]
        public PromptRqItemInput inputs { get; set; }

        [DataMember]
        public string class_type { get; set; }

        [DataMember]
        public Dictionary<string, object> __unknown { get; set; }
    }

    [DataContract]
    public class PromptRqItemInput
    {
        [DataMember]
        public string text { get; set; }

        [DataMember]
        public Dictionary<string, object> __unknown { get; set; }
    }
}

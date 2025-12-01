using System.Runtime.Serialization;

namespace ArtAi.data.comfyui
{
    [DataContract]
    public class PromptRs
    {
        [DataMember]
        public string prompt_id { get; set; }
    }
}


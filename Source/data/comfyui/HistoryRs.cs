using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace ArtAi.data.comfyui
{
    [DataContract]
    public class HistoryRsItem
    {
        // Custom tag, not in ComfyUI
        [DataMember]
        public int? artAiQueuePosition { get; set; }

        // Custom tag (local server): "error" means the generation failed
        [DataMember]
        public string artAiStatus { get; set; }

        // Custom tag (local server): error text or why it takes long (e.g. model download progress), in English
        [DataMember]
        public string artAiMessage { get; set; }

        // Custom tag (local server): values for the translated text of artAiStatus
        [DataMember]
        public List<string> artAiArgs { get; set; }

        [DataMember]
        public Dictionary<string, HistoryRsOutput> outputs { get; set; }

        public string Filename()
        {
            return outputs
                ?.Values
                .FirstOrDefault(o => o?.images != null && o.images.Any())
                ?.images
                ?.FirstOrDefault()
                ?.filename;
        }
    }

    [DataContract]
    public class HistoryRsOutput
    {
        [DataMember]
        public List<HistoryRsImage> images { get; set; }
    }

    [DataContract]
    public class HistoryRsImage
    {
        [DataMember]
        public string filename { get; set; }
    }
}

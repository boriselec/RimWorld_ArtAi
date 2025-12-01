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

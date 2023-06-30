using System;
using Newtonsoft.Json;

namespace amsTool
{
    public class RootLocator
    {
        public List<StreamingLocator> streamingLocators { get; set; }
    }

    public class StreamingLocator
    {
        public string name { get; set; }
        public string assetName { get; set; }
        public DateTime created { get; set; }
        public DateTime endTime { get; set; }
        public string streamingLocatorId { get; set; }
        public string streamingPolicyName { get; set; }
    }

}

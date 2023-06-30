using System;
using Newtonsoft.Json;

namespace amsTool
{
    public class Properties
    {
        public string assetId { get; set; }
        public DateTime created { get; set; }
        public DateTime lastModified { get; set; }
        public string container { get; set; }
        public string storageAccountName { get; set; }
        public string storageEncryptionFormat { get; set; }
        public string alternateId { get; set; }
        public string description { get; set; }
    }

    public class Root
    {
        public List<Value> value { get; set; }

        [JsonProperty("@odata.nextLink")]
        public string odatanextLink { get; set; }
    }

    public class SystemData
    {
        public string createdBy { get; set; }
        public string createdByType { get; set; }
        public DateTime createdAt { get; set; }
        public string lastModifiedBy { get; set; }
        public string lastModifiedByType { get; set; }
        public DateTime lastModifiedAt { get; set; }
    }

    public class Value
    {
        public string name { get; set; }
        public string id { get; set; }
        public string type { get; set; }
        public Properties properties { get; set; }
        public SystemData systemData { get; set; }
    }

}

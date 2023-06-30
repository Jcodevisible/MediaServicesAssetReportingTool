using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace amsTool
{
    public class outputJson
    {
        public List<assetData> value { get; set; }
        
        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
    
    public class assetData
    {
        public string assetName { get; set; }
        public string assetContainerName { get; set; }
        public string assetStorageAccount { get; set; }
        public string assetDescription { get; set; }
        public string assetCreationDate { get; set; }
        public string assetStorageEncryptionFormat { get; set; }
        //public streamingLocators streamingLocators { get; set; }
        public bool encoded { get; set; }
        public List<streamingLocators> streamingLocators { get; set; }
    }

    public class streamingLocators
    {
        public string name { get; set; }
        public string ID { get; set; }

        public string streamingPolicy { get; set; }
        //public streamingPolicy policy { get; set; }

    }

    //public class streamingPolicy
    //{
    //    public string data { get; set; }
    //}
}

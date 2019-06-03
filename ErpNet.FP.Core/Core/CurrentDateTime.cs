
using Newtonsoft.Json;

namespace ErpNet.FP.Core
{
    public class CurrentDateTime : Credentials
    {
        [JsonProperty(Required = Required.Always)]
        public System.DateTime DeviceDateTime = System.DateTime.MinValue;
    }
}

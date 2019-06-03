
using Newtonsoft.Json;

namespace ErpNet.FP.Core
{
    public class TransferAmount : Credentials
    {
        [JsonProperty(Required = Required.Always)]
        public decimal Amount = 0m;
    }
}

namespace ErpNet.FP.Core
{

    using Newtonsoft.Json;

    public class TransferAmount : Credentials
    {
        [JsonProperty(Required = Required.Always)]
        public decimal Amount = 0m;
    }
}

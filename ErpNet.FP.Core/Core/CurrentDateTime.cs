namespace ErpNet.FP.Core
{

    using Newtonsoft.Json;

    public class CurrentDateTime : Credentials
    {
        [JsonProperty(Required = Required.Always)]
        public System.DateTime DeviceDateTime = System.DateTime.MinValue;
    }
}

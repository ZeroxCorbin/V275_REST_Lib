namespace V275_REST_Lib.Models
{
    public class Inspection
    {
        public string? name { get; set; }
        public int port { get; set; }
        public string? state { get; set; }
        public string? device { get; set; }
        public bool connected { get; set; }
        public string? runningFwVersion { get; set; }
        public string? systemModel { get; set; }
        public string? deviceSerialNumber { get; set; }
        public string? printerModel { get; set; }
        public bool peelAndPresentMode { get; set; }
    }
}

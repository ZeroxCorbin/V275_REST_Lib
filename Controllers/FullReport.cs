using Newtonsoft.Json.Linq;

namespace V275_REST_Lib.Controllers;

public class FullReport
{
    public byte[]? Image { get; set; } = [];

    public JObject? Report { get; set; }
    public string ReportJSON { get; set; } = string.Empty;

    public JObject? Job { get; set; }
    public string JobJSON { get; set; } = string.Empty;

    public bool OK { get; set; } = false;
}

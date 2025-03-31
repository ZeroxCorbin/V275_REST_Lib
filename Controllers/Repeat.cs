using V275_REST_Lib.Models;

namespace V275_REST_Lib.Controllers;

public class Repeat
{
    public int Number { get; set; } = -1;
    public FullReport? FullReport { get; set; }
    public Label Label { get; set; }
    public Events_System? SetupDetectEvent { get; set; }

    public Repeat(int number, Label label)
    {
        Number = number;
        Label = label;
    }
}

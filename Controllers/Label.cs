using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.GS1;
using ImageMagick;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace V275_REST_Lib.Controllers;

public class Label
{
    private byte[]? _image = [];
    public byte[]? Image
    {
        get => _image;
        set
        {
            _image = value;
            if (_image == null || _image.Length == 0)
                return;
            
            using var image = new ImageMagick.MagickImage(_image);
            //Convert Density to DPI for (X)
            if (image.Density.ChangeUnits(ImageMagick.DensityUnit.PixelsPerInch) is Density d)
            {
                //Convert PPI to DPI
                Dpi = (int)d.X;
            }
        }
    }
    public List<JToken> Sectors { get;  }
    public LabelHandlers Handler { get; }
    public AvailableTables DesiredGS1Table { get;}
    public int Dpi { get; set; }

    public Action<Repeat> RepeatAvailable { get; }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="image"></param>
    /// <param name="dpi">Must be set if using the simulator API.</param>
    /// <param name="sectors">If null, ignore. If empty, auto detect. If not empty, restore.</param>
    /// <param name="table"></param>
    public Label(Action<Repeat> repeatAvailable, List<JToken> sectors, LabelHandlers handler, AvailableTables desiredTable)
    {
        RepeatAvailable = repeatAvailable;
        Handler = handler;
        DesiredGS1Table = desiredTable;
        Sectors = sectors;
    }
}

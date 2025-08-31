using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;

namespace V275_REST_Lib.Printer;

public class Controller : IDisposable
{
    private byte[] Image;
    private int Count;
    private string Data;
    PrintDocument pd;
    private bool disposedValue;

    public void Print(string imagePath, int count, string printerName, string data)
    {
        Image = File.ReadAllBytes(imagePath);
        Count = count;
        Data = data;

        if (pd != null)
        {
            pd.PrintPage -= PrintPage;
            pd.Dispose();
        }

        pd = new PrintDocument();
        pd.PrintPage += PrintPage;

        pd.PrinterSettings.PrinterName = printerName;
        pd.Print();
    }

    public void Print(byte[] image, int count, string printerName, string data)
    {
        Image = image;
        Count = count;
        Data = data;

        if (pd != null)
        {
            pd.PrintPage -= PrintPage;
            pd.Dispose();
        }

        pd = new PrintDocument();
        pd.PrintPage += PrintPage;

        pd.PrinterSettings.PrinterName = printerName;
        pd.Print();
    }


    private void PrintPage(object o, PrintPageEventArgs e)
    {
        using var img = System.Drawing.Image.FromStream(new MemoryStream(Image));
        //if (!string.IsNullOrEmpty(Data))
        //{

        //    using var g = Graphics.FromImage(img);
        //    var dataLength = g.MeasureString(Data, new Font("Arial", 8));
        //    g.DrawString(Data, new Font("Arial", 8), Brushes.Black, new Point(img.Width - (int)dataLength.Width - 100, 20));
        //}
        e.Graphics?.DrawImage(img, new Point(0, 0));


        if (Count-- > 1)
            e.HasMorePages = true;
        else
        {
            if (pd != null)
            {
                pd.PrintPage -= PrintPage;
            }
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                if (pd != null)
                {
                    pd.PrintPage -= PrintPage;
                    pd.Dispose();
                    pd = null;
                }
                Image = null;
                Data = null;
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~Controller()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

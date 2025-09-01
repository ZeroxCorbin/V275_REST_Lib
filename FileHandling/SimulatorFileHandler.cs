﻿using System.Collections.Generic;
using System.IO;

namespace V275_REST_Lib.Simulator;

public class SimulatorFileHandler
{
    public SimulatorFileHandler(string path) => SimulatorImageDirectory = path;
    
    public string SimulatorImageDirectory { get; private set; }

    public bool SimulatorImageDirectoryExists => Directory.Exists(SimulatorImageDirectory);

    public List<string> Images { get; set; } = [];

    public bool HasImages { get { UpdateImageList(); return Images.Count > 0; } }

    public void UpdateImageList()
    {
        Images.Clear();

        if (SimulatorImageDirectoryExists)
        {
            foreach (var file in Directory.GetFiles(SimulatorImageDirectory))
            {
                var ext = Path.GetExtension(file);

                if (ext.Equals(".bmp") ||
                    ext.Equals(".png") ||
                    ext.Equals(".tif") ||
                    ext.Equals(".tiff") ||
                    ext.Equals(".jpg") ||
                    ext.Equals(".webp"))

                    Images.Add(file);
            }
        }
    }

    public bool DeleteAllImages()
    {
        if (!HasImages)
            return true;

        var ok = true;
        foreach (var file in Images)
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
                ok = false;
            }
        }
        return ok;
    }

    public bool CopyImage(string file, string prepend)
    {

        if (SimulatorImageDirectoryExists)
        {
            File.Copy(file, Path.Combine(SimulatorImageDirectory, prepend + Path.GetFileName(file)));
            return true;
        }
        else
            return false;
    }

    public bool SaveImage(string fileName, byte[] imageData)
    {

        if (SimulatorImageDirectoryExists)
        {
            File.WriteAllBytes(Path.Combine(SimulatorImageDirectory, fileName), imageData);
            return true;
        }
        else
            return false;
    }
}

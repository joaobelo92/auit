using UnityEngine;
using System;
using System.IO;
using System.Threading.Tasks;

public static class LoggingDataWriter
{
    public enum FileType
    {
        debug = 0, aggregated = 1, detailed = 2
    }

    public static void WriteLines(FileType fileType, string filename, string[] data)
    {
        var _ = WriteLinesAsyncCrossPlatform(fileType, filename, data);
    }

    public static async Task WriteLinesAsyncCrossPlatform(FileType fileType, string filename, string[] data)
    {
        var folder = CreateFileTypeFolderIfMissingCrossPlatform(fileType);
        using (StreamWriter writer = File.AppendText(folder.FullName + "/" + filename))
        {
            string line = "";
            for (int i = 0; i < data.Length; i++)
            {
                line += data[i];
            }
            await writer.WriteLineAsync(line);
        }
    }

    public static DirectoryInfo CreateFileTypeFolderIfMissingCrossPlatform(FileType fileType)
    {
        switch (fileType)
        {
            case FileType.debug:
                return Directory.CreateDirectory(Application.persistentDataPath + "/debug/");
            case FileType.aggregated:
                return Directory.CreateDirectory(Application.persistentDataPath + "/aggregated/");
            case FileType.detailed:
                return Directory.CreateDirectory(Application.persistentDataPath + "/detailed/");
            default:
                return null;
        }
    }
}
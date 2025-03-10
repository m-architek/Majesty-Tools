using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

static class StrTool
{


    /**
        First argument is operation: 
            * STRT to TXT:  --export (-e),
            * TXT to STRT: --import (-i).
        Second argument is input file/dricetory path.
        Third argument is output file/directory path.
    */
    static void Main(string[] args)
    {
        string operation = args[0];
        string input = args[1];
        string output = args[2];

        Console.WriteLine("Input: " + input);
        Console.WriteLine("Output: " + output);

        switch(operation) 
        {
            case "--export":
            case "-e":
                Export(input, output);
                break;
            case "--import":
            case "-i":
                Import(input, output);
                break;
            default:
                Console.WriteLine("ERROR: Unknown operation: " + operation);
                break;
        }

    }

    private static void Export(string input, string output) 
    {
        if(File.Exists(input))
        {
            var lines = StrLib.Read(input);
            TxtLib.Write(lines, output);
        }
        else if(Directory.Exists(input))
        {
            var files = Directory.GetFiles(input);

            Console.WriteLine($"Export {files.Length} files from directory {input} to directory {output}");
            Directory.CreateDirectory(output);
            foreach (string file in files)
            {
                var outputFile = Path.Combine(
                    output, 
                    $"{Path.GetFileNameWithoutExtension(file)}.TXT"
                );
                var lines = StrLib.Read(file);
                TxtLib.Write(lines, outputFile);
            }            
        }
        else
        {
            Console.WriteLine("ERROR: Invalid input path: " + input);
        }
    }

    private static void Import(string input, string output) 
    {

        if(File.Exists(input))
        {
            var lines = TxtLib.Read(input);
            StrLib.Write(lines, output);
        }
        else if(Directory.Exists(input))
        {
            var files = Directory.GetFiles(input);

            Console.WriteLine($"Import {files.Length} files from directory {input} to directory {output}");
            Directory.CreateDirectory(output);
            foreach (string file in files)
            {
                var outputFile = Path.Combine(
                    output, 
                    $"{Path.GetFileNameWithoutExtension(file)}.STRT"
                );
                var lines = TxtLib.Read(file);
                StrLib.Write(lines, outputFile);
            }            
        }
        else
        {
            Console.WriteLine("ERROR: Invalid input path: " + input);
        }
    }
}

static class TxtLib
{
    private static Encoding encoding = Array.Find(Encoding.GetEncodings(), ei => ei.Name == "windows-1250")
            .GetEncoding();

    private static string EOL = "<EOL>";

    public static byte[][] Read(string path)
    {
        return File.ReadAllText(path, encoding)
            .Split(EOL + Environment.NewLine)
            .Where(line => !String.IsNullOrWhiteSpace(line))
            .Select(line => encoding.GetBytes(line))
            .ToArray();
    }

    public static void Write(byte[][] lines, string path)
    {
        var encodedLines = lines.Select(line => encoding.GetString(line))
            .Select(line => line + EOL);
        File.WriteAllLines(path, encodedLines, encoding);
    }
}

static class StrLib
{
    public static byte[][] Read(string path)
    {
        var fs = new FileStream(path, FileMode.Open);
        var reader = new BinaryReader(fs);

        /**
            File header
        */
        var length = reader.ReadUInt16(); // Number of lines
        reader.ReadByte(); // Unicode mode this flag will be 0x08, non unicode is 0x00
        var unkFlag = reader.ReadByte(); // Unkmark, 0x02 for HD, 0x00 for PL

        /**
            Content header
        */
        var lineOffsets = new List<uint>();
        for (int i = 0; i < length; i++)
        {
            var lineOffset = unkFlag == (byte) 0x00 ? reader.ReadUInt16() : reader.ReadUInt32();
            lineOffsets.Add(lineOffset);
        }

        /**
            Content
        */
        var lines = new List<byte[]>();
        for (int i = 0; i < length; i++)
        {
            var startPos = lineOffsets[i];
            var endPos = i + 1 == length ? reader.BaseStream.Length : lineOffsets[i + 1];
            var lineLength = (int)(endPos - startPos - 1); // minus null byte

            var line = reader.ReadBytes(lineLength);
            lines.Add(line);
            reader.ReadByte(); // null byte
        }
        return lines.ToArray();
    }

    public static void Write(byte[][] lines, string path)
    {
        var fs = new FileStream(path, FileMode.Create);
        var writer = new BinaryWriter(fs);

        /**
            File header (total 4 bytes):
                * Number of lines (ushort -> 2 bytes)
                * Encoding flag: 0x00 - ASCII, 0x08 - Unicode (1 byte)
                * Flag "start of text": 0x02 (1 byte)
        */
        writer.Write((ushort) lines.Length);
        writer.Write((byte) 0x00);
        writer.Write((byte) 0x02);

        /**
            Content header (total n. of lines * 4 bytes):
                * Placement of each line form the beggining of the file (uint -> 4 bytes)
            Calculation formula:
                file header length + content header length + combined length of previous lines
        */
        uint contentShift = 0;
        foreach (byte[] line in lines)
        {
            uint fileHeader = 4;
            uint contentHeader = (uint) lines.Length * 4;
            uint offset = fileHeader + contentHeader + contentShift;
            writer.Write(offset);

            // previous value + current line size + null byte at the end of each line
            contentShift = contentShift + (uint) line.Length + 1; 
        }

        /**
            Content:
                * all lines in order with null byte at the end of each line
        */
        foreach (byte[] line in lines)
        {        
            writer.Write(line);
            writer.Write('\0');
        }
    }
}

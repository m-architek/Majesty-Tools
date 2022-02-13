using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

class CamTool
{
    /**
        First argument is operation: 
            * Unpack CAM:  --unpack (-u),
            * Pack CAM: --pack (-p),
            * Unpack CAM using base64 file names: --unpack_base64 (-ub),
            * Pack CAM using base64 file names: --pack_base64 (-pb).
        Second argument is input file path. For "pack" operation it should be directory with "CamTool.index" file.
        Third argument is output file path. For "unpack" operation it would be directory with "CamTool.index" file in it.
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
            case "--unpack":
            case "-u":
                Unpack(input, output);
                break;
            case "--pack":
            case "-p":
                Pack(input, output);
                break;
            case "--unpack_base64":
            case "-ub":
                ContentLib.base64FileNames = true;
                Unpack(input, output);
                break;
            case "--pack_base64":
            case "-pb":
                ContentLib.base64FileNames = true;
                Pack(input, output);
                break;
            default:
                Console.WriteLine("ERROR: Unknown operation: " + operation);
                break;
        }

    }

    private static void Unpack(string input, string output) 
    {
        var camFile = CamLib.Read(input);
        ContentLib.Write(camFile, output);
    }

    private static void Pack(string input, string output) 
    {
        var camFile = ContentLib.Read(input);
        CamLib.Write(camFile, output);
    }
}

public static class ContentLib {
    private static string indexFileName = "CamTool.index";
    private static Encoding indexEncoding = Encoding.ASCII;

    public static bool base64FileNames = false;

    public static void Write(CamFile camFile, string path)
    {
        Directory.CreateDirectory(path);
        WriteIndex(camFile, path);
        WriteContent(camFile, path);
    }


    public static CamFile Read(string path)
    {
        var index = ReadIndex(path);
        return ReadContent(path, index);
    }

    private static void WriteIndex(CamFile camFile, string path) {
        var indexFilePath = Path.Combine(path, indexFileName);
        var fs = new FileStream(indexFilePath, FileMode.Create);
        var writer = new StreamWriter(fs, indexEncoding);

        Console.WriteLine($"Write index for {camFile.Sections.Length} sections");
        writer.WriteLine(camFile.Sections.Length); // 1st line: number of sections
        foreach (var section in camFile.Sections) {
            writer.WriteLine(section.Files.Length); // one line for each section: number of files in section
        }
        foreach (var section in camFile.Sections) {
            foreach (var file in section.Files) {
                var encodedFileName = EncodeFileName(file.Name);
                writer.WriteLine(encodedFileName); // one line for each file in each section: file name without extension
            }
        }
        writer.Flush();
        writer.Close();
    }

    //** Returns ordered file names (without extension) for each section */
    private static string[][] ReadIndex(string path) {
        var indexFilePath = Path.Combine(path, indexFileName);
        var fs = new FileStream(indexFilePath, FileMode.Open);
        var reader = new StreamReader(fs, indexEncoding);
        var numberOfSections = Int32.Parse(reader.ReadLine());
        Console.WriteLine($"Read index for {numberOfSections} sections");

        var sectionSizes = new List<int>();
        for (int i = 0; i < numberOfSections; i++) {
            var sectionSize = Int32.Parse(reader.ReadLine());
            Console.WriteLine($"Section {i}: {sectionSize} files");
            sectionSizes.Add(sectionSize);
        }

        var index = new List<string[]>();
        foreach (var sectionSize in sectionSizes) {
            var sectionIndex = new List<string>();
            for (int i = 0; i < sectionSize; i++) {
                var fileName = reader.ReadLine();
                // Console.WriteLine($"File {i}: {fileName}");
                sectionIndex.Add(fileName);
            }
            index.Add(sectionIndex.ToArray());
        }
        return index.ToArray();
    }

    private static void WriteContent(CamFile camFile, string path) {
        Console.WriteLine($"Write content of {camFile.Sections.Length} sections");
        for (int i = 0; i < camFile.Sections.Length; i++) {
            var section = camFile.Sections[i];
            var sectionPath = Path.Combine(path, i.ToString());

            Directory.CreateDirectory(sectionPath);
            for (int j = 0; j < section.Files.Length; j++) {
                var file = section.Files[j];
                var encodedFileName = EncodeFileName(file.Name);
                var filePath = Path.Combine(sectionPath, $"{encodedFileName}.{section.Extension}");

                // Console.WriteLine($"Write file {filePath} with size {file.Data.Length}");
                var fs = new FileStream(filePath, FileMode.Create);
                var writer = new BinaryWriter(fs);
                writer.Write(file.Data);
                writer.Flush();
                writer.Close();
            }
        }
    }

    private static CamFile ReadContent(string path, string[][] index) {
        var numberOfSections = index.Length;
        Console.WriteLine($"Read content of {numberOfSections} sections");

        var sections = new List<CamSection>();
        for (int i = 0; i < numberOfSections; i++) {
            var section = new CamSection();
            var sectionPath = Path.Combine(path, i.ToString());
            var fileExtensions = Directory.GetFiles(sectionPath)
                .Select(file => Path.GetExtension(file).Remove(0,1))
                .Distinct()
                .ToArray();
            if (fileExtensions.Length != 1) {
                throw new Exception($"ERROR: More then one extension found for section {i}");
            }
            section.Extension = fileExtensions[0];
            Console.WriteLine($"Section {i}: {section.Extension}");

            var sectionFileNames = index[i];
            if (Directory.GetFiles(sectionPath).Length != sectionFileNames.Length) {
                throw new Exception($"ERROR: Invalid index or number of files for section {i}");
            }
            var files = new List<CamSectionFile>();
            foreach (var fileName in sectionFileNames) {
                var filePath = Path.Combine(sectionPath, $"{fileName}.{section.Extension}");
                
                var file = new CamSectionFile();
                file.Name = DecodeFileName(fileName);
                file.Data = File.ReadAllBytes(filePath);
                // Console.WriteLine($"File {filePath} with size {file.Data.Length}");
                files.Add(file);
            }
            section.Files = files.ToArray();
            sections.Add(section);
        }

        var camFile = new CamFile();
        camFile.Sections = sections.ToArray();
        return camFile;
    }

    private static string EncodeFileName(byte[] fileName) {
        if (base64FileNames) {
            return Convert.ToBase64String(fileName).Replace('/', '_');
        } else {
            return Encoding.ASCII.GetString(fileName).Trim('\0');
        }
    }

    private static byte[] DecodeFileName(string fileName) {
        if (base64FileNames) {
            return Convert.FromBase64String(fileName.Replace('_', '/'));
        } else {
            return Encoding.ASCII.GetBytes(fileName.PadRight(20, '\0'));
        }
    }
}

public static class CamLib
{
    private static readonly byte[] FixHeader = new byte[] { 0x43, 0x59, 0x4C, 0x42, 0x50, 0x43, 0x20, 0x20, 0x01, 0x00, 0x01, 0x00 };
    private static readonly byte[] Pause = new byte[] { 0x00, 0x00, 0x00, 0x00 };

    public static CamFile Read(string path)
    {
        var fs = new FileStream(path, FileMode.Open);
        var reader = new BinaryReader(fs);

        // File header
        reader.ReadBytes(FixHeader.Length); // FixHeader: 12 bytes
        var sectionCount = reader.ReadUInt32(); // SectionCount: 4 bytes
        reader.ReadUInt32(); // ContentHeaderLength: 4 bytes

        Console.WriteLine("SectionCount: " + sectionCount);
        var sectionsExtensions = new List<string>();
        for (int i = 0; i < sectionCount; i++) {
            var sectionExtension = Encoding.ASCII.GetString(reader.ReadBytes(4)); // SectionExtension: 4 bytes
            reader.ReadUInt32(); // SectionHeaderOffset: 4 bytes
            Console.WriteLine($"Section {i}: {sectionExtension}");
            sectionsExtensions.Add(sectionExtension);
        }

        // Content header
        var sectionsFileNames = new List<byte[][]>();
        var sectionsFileSizes = new List<uint[]>();
        for (int i = 0; i < sectionCount; i++) {
            var filesCount = reader.ReadUInt32(); // SectionFilesCount: 4 bytes
            Console.WriteLine($"Section {i}: {filesCount} files");
            reader.ReadBytes(4); // Pause: 4 bytes

            var fileNames = new List<byte[]>();
            var fileOffsets = new List<uint>(); 
            var fileSizes = new List<uint>(); 
            for (int j = 0; j < filesCount; j++) {
                var fileName = reader.ReadBytes(20); // FileName with padding: 20 
                var fileOffset = reader.ReadUInt32(); // FileOffset: 4 bytes
                var fileSize = reader.ReadUInt32(); // FileSize: 4 bytes
                // Console.WriteLine($"File {fileName} at offset {fileOffset} with size {fileSize}");

                fileNames.Add(fileName);
                fileOffsets.Add(fileOffset);
                fileSizes.Add(fileSize);
            }

            sectionsFileNames.Add(fileNames.ToArray());
            sectionsFileSizes.Add(fileSizes.ToArray());
        }

        // Content
        var sections = new List<CamSection>();  
        for (int i = 0; i < sectionCount; i++) {
            var fileNames = sectionsFileNames[i];
            var fileSizes = sectionsFileSizes[i];

            var files = new List<CamSectionFile>();
            for (int j = 0; j < fileNames.Length; j++) {
                var fileSize = (int) fileSizes[j];
                var fileData = reader.ReadBytes(fileSize);

                var file = new CamSectionFile();
                file.Name = fileNames[j];
                file.Data = fileData;
                files.Add(file);
            }

            var section = new CamSection();
            section.Extension = sectionsExtensions[i];
            section.Files = files.ToArray();
            sections.Add(section);
        }

        // EOF
        if (reader.BaseStream.Position != reader.BaseStream.Length) {
            throw new Exception("ERROR: EOF not reached");
        }

        var camFile = new CamFile();
        camFile.Sections = sections.ToArray();
        return camFile;
    }

    public static void Write(CamFile camFile, string path)
    {
        var fs = new FileStream(path, FileMode.Create);
        var writer = new BinaryWriter(fs);

        var fileHeaderLength = CalculateFileHeaderLength(camFile);
        var contentHeaderLength = CalculateContentHeaderLength(camFile);
        
        // File header
        writer.Write(FixHeader); // FixHeader: 12 bytes
        writer.Write((uint) camFile.Sections.Length); // SectionCount: 4 bytes
        writer.Write((uint) contentHeaderLength); // ContentHeaderLength: 4 bytes

        var secttionHeaderShift = 0;
        foreach (var section in camFile.Sections) {
            writer.Write(Encoding.ASCII.GetBytes(section.Extension)); // SectionExtension: 4 bytes

            var sectionHeaderOffset = fileHeaderLength + secttionHeaderShift;
            writer.Write((uint) sectionHeaderOffset); // SectionHeaderOffset: 4 bytes
            secttionHeaderShift = secttionHeaderShift + CalculateSectionHeaderLength(section);
        }

        // Content header
        var fileShift = 0;
        foreach (var section in camFile.Sections) {
            writer.Write((uint) section.Files.Length); // SectionFilesCount: 4 bytes
            writer.Write(Pause); // Pause: 4 bytes

            foreach (var file in section.Files) {
                writer.Write(file.Name); // FileName with padding: 20 

                var fileOffset = fileHeaderLength + contentHeaderLength + fileShift;
                writer.Write((uint) fileOffset); // FileOffset: 4 bytes
                writer.Write((uint) file.Data.Length); // FileSize: 4 bytes
                fileShift = fileShift + file.Data.Length;
            }
        }

        // Content
        foreach (var section in camFile.Sections) {
            foreach (var file in section.Files) {
                writer.Write(file.Data);
            }
        }
    }

    // FileHeaderLength: FixHeader + SectionCount + ContentOffset + for each section (SectionExtension + SectionHeaderOffset)
    private static int CalculateFileHeaderLength(CamFile camFile) {
        return 12 + 4 + 4 + camFile.Sections.Length * (4 + 4);
    }

    // ContentHeaderLength: sum of header lengths of all sections
    private static int CalculateContentHeaderLength(CamFile camFile) {
        var contentHeaderLength = 0;
        foreach (var section in camFile.Sections) {
            contentHeaderLength = contentHeaderLength + CalculateSectionHeaderLength(section);
        }
        return contentHeaderLength;
    }

    // SectionHeaderLength: SectionFilesCount + Pause + for each file (FileName with padding + FileOffset + FileSize)
    private static int CalculateSectionHeaderLength(CamSection section) {
        return 4 + 4 + section.Files.Length * (20 + 4 + 4);
    }
}

public class CamFile
{
    public CamSection[] Sections { get; set; }
}

public class CamSection
{
    public string Extension { get; set; }
    public CamSectionFile[] Files { get; set; }
}

public class CamSectionFile
{
    public byte[] Name { get; set; }
    public byte[] Data { get; set; }
}

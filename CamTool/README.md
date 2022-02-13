## CamTool

Application allwos to unpack and pack CAM files.

### Usage

First argument is operation: 
* Unpack CAM:  --unpack (-u),
* Pack CAM: --pack (-p),
* Unpack CAM using base64 file names: --unpack_base64 (-ub),
* Pack CAM using base64 file names: --pack_base64 (-pb).

Second argument is input file path. For "pack" operation it should be directory with "CamTool.index" file.

Third argument is output file path. For "unpack" operation it would be directory with "CamTool.index" file in it.

For example:
```
mono CamTool.exe -u gpltext.cam gpltext
```
Unpacks gpltext.cam into directory gpltext.

### Unpacked content structure

Target unpack directory contains "CamTool.index" file and number of directories equal to number of sections in given CAM file.

"CamTool.index" file is required to maintain files order in CAM file. Although I don't know, if file order really matters for game itself, in my modifications I try to maintain it. It is also useful for testing tool itself (unpack and pack again should result in exact same file which wouldn't be the case, when file order in archive is different).

"CamTool.index" file structure:
* first line is number of sections
* then one line for each section is section files count
* then one line for each file name in all sections maintaining section and file order. File names here are without extensions.

If you want to add or remove file from CAM file you need add or remove it from correct section directory (one section can contain files with only one file extension). Then modify "CamTool.index" file by adjusting files count in this section and add/remove name of you file to/from correct place in index. 

### Base64 file names

If you have problems with unpacking CAM file because of invalid file name (that your file system is nor supporting) you can choose to unpack this file using base64 file names. This will cause all files to have their names encoded in base64, which should solve this prbolem, although making file editing more difficult.

Note, that to pack back you will need use pack option with base64 file names enabled.

### CAM file structure

For detail view of CAM file structure please look into code.

In general, I distiguish three main blocks of CAM file:
* File header - contains number of sections, their extensions, and offset of each section header,
* Content header - contains section headers:
    * Each section header contains number of files in section and names of those files together with their size and offset,
* Content - bytes data of each file.

Offset is always counted from beggining of the file. Each section contain files with only one extension.

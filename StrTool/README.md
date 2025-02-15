## StrTool

Application allwos to convert STRT files to editable TXT files and back.

### Usage

First argument is operation: 
* STRT to TXT:  --export (-e),
* TXT to STRT: --import (-i).

Second argument is input file path.

Third argument is output file path.

For example:
```
mono StrTool.exe -e QUES.STRT QUES.TXT
```
Exports QUES.STRT to editable QUES.TXT.

Input and output path can also be directory. Application will process all files in given input directory and save it in given output directory.

### Encoding

TXT files should be edited using windows-1250 encoding.

### STRT file structure

For detail view of STRT file structure please look into code.

In general, I distiguish three main blocks of STRT file:
* File header - contains number of lines,
* Content header - contains offset of each line,
* Content - bytes data of each line.

Offset is always counted from beggining of the file.

### Compatibility

STRT files of different Majesty versions can differ between each other.

I found following differences between HD version and Polish version of game (PL original release):
* File header of HD version ends with '0x02' flag, while for PL version it is '0x00'
* For HD version line offset is descibed os 2 bytes uint, while for PL verion is 4 bytes uint

Since I wanted support exporting from both versions, I decided to use flag at the end of file header to determine file version and expect size of line offset accordingly.

Import always use values form HD version.

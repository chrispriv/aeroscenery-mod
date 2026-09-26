# TTC payload compression

The built-in converter writes an RFC 1950 zlib stream at offset 0x100 (CMF/FLG `78 01`,
raw DEFLATE, Adler-32). That is the layout Aerofly FS4 and the working sample tiles use.

LZHAM inside a tmcompress chunk is smaller, but FS4 and the scenery decoder would not
read those files. `lzham_x64.dll` is not required.

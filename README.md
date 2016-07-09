# hybrid-arts-midi-file-fixer
Repairs Hybris Arts SyncTrack (around 1987) .SNG midi-files that have a very specific corruption issue (all 1a/0d bytes missing).

This project is useful for documenting the old Hybrid-Arts .SNG format and for showing one approach automating the repair of corrupt binary files.

This program is built around [Be.HexEditor] (https://sourceforge.net/projects/hexbox/files/hexbox/) by Bernhard Elbl with a few mods to meet my needs.

My original new code is in **FormFixHa** and **FormFixTracks**.

In the menu, use Tools->Fix Hybrid Arts Files.

After build, the executable file for Windows will be in: `\Projects\HaFixer\Be.HexEditor\bin\Debug\Be.HexEditor.exe`
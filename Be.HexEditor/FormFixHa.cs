using System;
using System.Collections.Generic;
using System.Drawing;
using Be.Windows.Forms;
using System.Windows.Forms;
using Be.HexEditor.Properties; // S.S.

namespace Be.HexEditor
{
  public partial class FormFixHybridArts : Form
  {
    #region Global Vars

    public IByteProvider _byteProvider { get; } = null;
    public HexBox _hexBox { get; } = null;

    // offset to a 4-byte * 60 tracks block of memory containing the addresses of each midi-track within the file.
    // The "base" address is stored at offset 0x069c in the midi-file. The end address is stored at 0x06a0.
    // The base address represents the first physical track added to a midi-file but could map to any "user level" track by its
    // slot-position in the offsets table at 0x069c. By adding the table length for this slot to the address for this slot, you get the
    // next track's address in physical memory (file offset).
    // so if I have 00 07 fd 02 stored in the offsets table in slot five, then take the lengths table slot five number of bytes (say it's 100) and add that
    // I get 00 07 fd 66. So the actual tracks data should start at (00 07 fd 66) - (00 07 fd 02) + 0x18ec.
    // In SyncTrack all of the tracks begin at 0x18ec. A "00 90" preceeds the data and is found at 0x18ea.
    //
    // Base addresses:
    // SyncTrack: 00 07 fd 02
    // EditTrack: 00 0f 91 52
    // Ludwig:    00 00 01 00
    // EZ Track:  00 05 dc 02
    //
    // Track Data (00 90 signature) (add 2 for actual start of physical track at the lowest offset into the SNG file)
    // SyncTrack: 18 ea
    // EditTrack: 1b 4a
    // EZ Track:  06 e4
    //
    private const int TRACK_COUNT = 60;
    private const int DEF_TICK_VARIATION = 12; // ticks of variation allowed per 96-tick beat (used when fixing track-timing) (divisible by both 2 (half-note) and 3 (triplet))
    private const UInt32 DEF_LENGTHS_OFFSET = 0x03c4;
    private const UInt32 DEF_ADDRESSES_OFFSET = 0x04b8;
    private const UInt32 DEF_MUTES_OFFSET = 0x05ac;
    private const UInt32 DEF_TRACK_FIXED_FLAGS_OFFSET = 0x05e8; // block of 60 bytes, purpose is unknown, they all default to 00, we set bit 5 as "track-fixed" flag
    private const UInt32 TRACK_FIXED = 0x20; // bit 5 is "track-fixed" flag
    private const UInt32 DEF_TRACKS_FILE_OFFSET = 0x18ea; // 1b4a for SyncTrack file converted to EditTrack
    private const UInt32 DEF_TRACKS_BASE_ADDRESS = 0x0007fd02; // 00 0a 72 52 for a SyncTrack midi file converted to EditTrack
    private const UInt32 DEF_TRACK_NAMES_OFFSET = 0x4; // 16 bytes * 60 tracks
    private const UInt32 DEF_BASE_ADDRESS_STORAGE_OFFSET = 0x069c;
    private const UInt32 DEF_END_ADDRESS_STORAGE_OFFSET = 0x06a0;
    //        private const UInt32 DEF_UNKNOWN1_OFFSET = 0x06a4; // ???? number of tracks plus 1???
    //        private const UInt32 DEF_0011_MARKER_OFFSET = 0x06a8; // 0011 stored here - start of section
    private const UInt32 MAX_LENGTH_COMBINATIONS = 4;
    private const UInt32 MAX_ADDRESS_COMBINATIONS = 6;
    private const UInt32 MAX_LENGTH = 0x0000ffff;
    private const UInt32 MAX_ADDRESS = 0x00ffffff;
    // track data starts immediately after this (NOTE: the lowest track in memory may not have a timestamp if the timestamp is 00!)
    private const UInt16 TRACKS_START_MAGIC_NUMBER = 0x0090;
    // if checkBoxUseOldFormat is unchecked - the might be in front of the tracks! At 0x1b4a => 00 33 00 d8 for example
    // where d8 is the block-length in bytes
    private const UInt16 POSSIBLE_SECTION_BEFORE_TRACKS_MAGIC_NUMBER = 0x0033;

    public const UInt32 MAX_TRACK_LENGTH = 0x0000ffff;

    public int TotalTracksInSong { get; set; } = 0;
    public int TickVariation { get; set; } = DEF_TICK_VARIATION;
    public UInt32 TableAddressesOffset { get; set; } = DEF_ADDRESSES_OFFSET;
    // offset to a 4-byte * 60 tracks block of memory containing the lengths of each midi-track within the file.
    public UInt32 TableLengthsOffset { get; set; } = DEF_LENGTHS_OFFSET;
    public UInt32 TracksFileOffset { get; set; } = DEF_TRACKS_FILE_OFFSET;
    public UInt32 TracksBaseAddress { get; set; } = DEF_TRACKS_BASE_ADDRESS;
    public UInt32 TrackNamesOffset { get; set; } = DEF_TRACK_NAMES_OFFSET;
    public System.Collections.Specialized.StringCollection TrackFilters { get; set; }

    private int _totalTracksLength = 0;
    private int _baseTracksLength = 0;
    private int _goodTrackCount = 0;
    private int _lengthSolutionNumberToUse = 1;
    private UInt32 _index = 0;
    private bool _fixingTracks = false;

    // need these to avoid repeating steps or deleting permutations arrays until lengths/addresses have been resolved
    private bool _lengthsPadded = false;
    private bool _addressesPadded = false;
    private bool _lengthsAndAddressesResolved = false;

    // Records the address and value (1A or 0D) we insert to auto-pad the #bytes or addresses tables
    List<UInt32[]> _lengthChanges = new List<UInt32[]>();
    List<UInt32[]> _addressChanges = new List<UInt32[]>();
    #endregion

    #region Form Hooks

    public FormFixHybridArts(HexBox hexBox, System.Collections.Specialized.StringCollection trackFilters)
    {
      InitializeComponent();
      _hexBox = hexBox;
      _byteProvider = _hexBox.ByteProvider;
      TrackFilters = trackFilters;
    }

    private void FormFixHybridArts_FormClosing(object sender, FormClosingEventArgs e)
    {
      Settings.Default.Filters = TrackFilters;
      Settings.Default.Save();
    }

    private void FormFixHybridArts_Load(object sender, EventArgs e)
    {
      this.Text = Program.ApplictionForm.FileName;

      // add 60 tracks
      dataGridView1.RowCount = (int)TRACK_COUNT;
      //            for (int ii = 0; ii < TRACK_COUNT; ii++)
      //                dataGridView1.Rows.Add();

      // display track (row) numbers
      foreach (DataGridViewRow r in dataGridView1.Rows)
        dataGridView1.Rows[r.Index].HeaderCell.Value = (r.Index + 1).ToString();

      textBoxTrackLengths.Text = String.Format("{0:X8}", TableLengthsOffset);
      textBoxTrackOffsets.Text = String.Format("{0:X8}", TableAddressesOffset);
      textBoxTotalTracksInSong.Text = "Press \"Fix...\"";

      checkBoxUseSyncTrackOffsets_CheckedChanged(null, null); // initialize base-address and track-offset as per the checkBox

      textBoxTotalTracksLength.Text = _totalTracksLength.ToString();
      _lengthSolutionNumberToUse = 1;
      LoadLengthsAndOffsets();

      textBoxGoodTrackCount.Text = "Unknown";

      // go into insert mode in the hex-editor...
      _hexBox.InsertActive = true;
    }

    private void buttonQuit_Click(object sender, EventArgs e)
    {
      if ((_lengthsPadded || _addressesPadded) && !_lengthsAndAddressesResolved)
      {
        if (_lengthChanges.Count > 0 || _addressChanges.Count > 0)
        {
          DialogResult result1 = MessageBox.Show("WARNING: Hex-editor fields were padded but have not been resolved!\n" +
              "If you exit this form you MUST manually re-load the file..." +
              "\n\nQuit anyway?",
              "Song: " + System.IO.Path.GetFileNameWithoutExtension(Program.ApplictionForm.FileName),
              MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

          if (result1 == DialogResult.No)
            return;
        }
      }

      Close();
    }

    private void checkBoxUseSyncTrackOffsets_CheckedChanged(object sender, EventArgs e)
    {
      if (checkBoxUseOldFormat.Checked)
      {
        TracksFileOffset = DEF_TRACKS_FILE_OFFSET;
        TracksBaseAddress = DEF_TRACKS_BASE_ADDRESS;
      }
      else
      {
        TracksFileOffset = 0x1b4a; // 0x1b4a might point to a section 00 33 00 NN
        TracksBaseAddress = 0xa7252;
      }

      textBoxTracksFileOffset.Text = String.Format("{0:X8}", TracksFileOffset);
      textBoxBaseAddress.Text = String.Format("{0:X8}", TracksBaseAddress);
    }

    private void numericVariation_ValueChanged(object sender, EventArgs e)
    {
      if (sender is NumericUpDown)
      {
        var c = sender as NumericUpDown;
        TickVariation = (int)c.Value;
      }
    }

    // Really, this button should only be enabled after all tracks have been repaired since the data won't make any since until
    // a step-wise, sequential repair is performed...
    private void buttonExamineTrack_Click(object sender, EventArgs e)
    {
      if (dataGridView1.SelectedRows.Count != 1)
      {
        MessageBox.Show("Click to the left side of a track to select it first!");
        return;
      }

      // Note: decided to allow a non-fixed track to be examined... since while auto-looping repairing tracks - you may want to
      // Cancel and just have a look at what's there - all tracks at highter file-offsets than the most-recent repaired track
      // except for the very next "broken track" will be useless to examine...

      // if (_fixingTracks || !TrackFixed(dataGridView1.SelectedRows[0].Index) || !GoodMagicNumber() )
      if (_fixingTracks || !IsGoodMagicNumber())
        return;

      int resumeAtEventIndex = -1; // not used here
      ExamineTrack(dataGridView1.SelectedRows[0].Index, -1, ref resumeAtEventIndex, false);
    }

    private void buttonFixTracks_Click(object sender, EventArgs e)
    {
      FixTracks();
    }

    private void buttonReset_Click(object sender, EventArgs e)
    {
      TracksReset();
    }

    private void buttonDeleteSelected_Click(object sender, EventArgs e)
    {
      if (dataGridView1.SelectedRows.Count != 1)
      {
        MessageBox.Show("Click to the left side of a track to select it first!");
        return;
      }

      if (_fixingTracks || !IsGoodMagicNumber())
        return;

      DeleteTrack(dataGridView1.SelectedRows[0].Index);
    }

    private void buttonFixOffsetsAndLengths_Click(object sender, EventArgs e)
    {
      // Read the current values of "constants" such as the track base address and file-offset from the user
      // text boxes... rarely changed but it can come in handy!
      TracksBaseAddress = Convert.ToUInt32(textBoxBaseAddress.Text, 16);
      TracksFileOffset = Convert.ToUInt32(textBoxTracksFileOffset.Text, 16);
      TableAddressesOffset = Convert.ToUInt32(textBoxTrackOffsets.Text, 16);
      TableLengthsOffset = Convert.ToUInt32(textBoxTrackLengths.Text, 16);

      //            Program.ApplictionForm.DisableFileSave = true;

      // FYI: Ran into a problem wher the lengths and addresses have been initially padded byt not resolved and the end-address is over-range
      // so we prompt the user to manually edit the hex-bax data and press Fix again - but we had two problems:
      // 1) the User should not be allowed to save the file with "place-holder" table values as yet unresolved.
      // 2) the program should not clear the permutations arrays or call PaddLengths() or PadAddresses() if that's already been done...
      //    we should go straight to FixOffsetsAndLengths()...

      buttonFixTracks.Enabled = false;
      buttonExamineSelected.Enabled = false;
      buttonDeleteSelected.Enabled = false;
      buttonResetTracks.Enabled = false;

      if (PadLengths())
        if (PadAddresses())
          if (FixOffsetsAndLengths())
          {
            buttonFixTracks.Enabled = true; // enable fix-tracks button
            buttonExamineSelected.Enabled = true;
            buttonDeleteSelected.Enabled = true;
            buttonResetTracks.Enabled = true;

            // display total tracks
            TotalTracksInSong = (int)GetTrackCount();
            textBoxTotalTracksInSong.Text = TotalTracksInSong.ToString();

            MessageBox.Show("Length and Address tables OK!\n(Next, press the \"Fix Tracks\" button...)");
            //                        Program.ApplictionForm.DisableFileSave = false;
            _fixingTracks = false; // this will block the button from working
          }
    }
    #endregion

    #region Load Lengths And Offsets

    // Loads FormFixHa's dataGridView with length and offset data from the HexBox
    private void LoadLengthsAndOffsets()
    {
      UInt32 saveIndex = _index; // save

      //            _lengthsAndAddressesResolved = false;
      //            _fixingTracks = false;

      _index = TrackNamesOffset;
      foreach (DataGridViewRow r in dataGridView1.Rows)
        dataGridView1.Rows[r.Index].Cells[0].Value = GetString(16);

      _totalTracksLength = 0;
      _goodTrackCount = 0;

      _index = TableLengthsOffset;
      foreach (DataGridViewRow r in dataGridView1.Rows)
      {
        UInt32 val = GetWord();
        dataGridView1.Rows[r.Index].Cells[1].Value = String.Format("{0:X8}", val);
        if (val > MAX_LENGTH)
          dataGridView1.Rows[r.Index].Cells[1].Style.BackColor = Color.Yellow;
        else if (val == 0)
          dataGridView1.Rows[r.Index].Cells[1].Style.BackColor = Color.LightGray;
        else
          dataGridView1.Rows[r.Index].Cells[1].Style.BackColor = Color.White;

        _totalTracksLength += (int)val;
      }

      textBoxTotalTracksLength.Text = _totalTracksLength.ToString();

      _index = TableAddressesOffset;
      foreach (DataGridViewRow r in dataGridView1.Rows)
      {
        UInt32 val = GetWord();
        dataGridView1.Rows[r.Index].Cells[2].Value = String.Format("{0:X8}", val);
        if (val == TracksBaseAddress)
          dataGridView1.Rows[r.Index].Cells[2].Style.BackColor = Color.LightGreen;
        else if (val > MAX_ADDRESS)
          dataGridView1.Rows[r.Index].Cells[2].Style.BackColor = Color.Yellow;
        else if (val == 0)
          dataGridView1.Rows[r.Index].Cells[2].Style.BackColor = Color.LightGray;
        else
          dataGridView1.Rows[r.Index].Cells[2].Style.BackColor = Color.White;
      }

      _index = saveIndex; // restore
    }

    // Musings:
    // The lowest address track-address in memory is the base address (value stored at 069C). We add the track's length
    // to get the address of the next track that follows this track in memory - it could be anywhere in the table though,
    // in any "slot". To fix the address table we first must have all bytes below the address table at the correct offsets.
    // (Fix the length-table before fixing the addresses, which follow the lengths). All addresses will be non-zero and
    // one of them will equal the base address. Addresses are four bytes but we presume that the MSB is always 00 for all
    // 60 slots - this lets us pinpoint data that has been left-shifted by a missing 1A or 1D because the 00 will be
    // in the LSB of the previous address and this address will have a non-zero byte in its MSB... voila!
    // 
    // If a byte is missing, the last byte of this address has to be 00 and the next non-zero address will be > MAX_ADDRESS
    // Two bytes could be missing if the next-to last byte is 00 and the next non-zero address is > MAX_ADDRESS
    // If this is the last slot, we can't check "next MAX_ADDRESS" above so check for a shifted mute flag instead.
    //
    // If THIS address is > MAX_ADDRESS, something is wrong - a preceeding "last length" or address has not been padded.
    // If this address is 0, check its corresponding length. If length is 0 also, this is an empty slot; otherwise, we need
    // to pad this slot with 1A if the next non-zero slot is > MAX_ADDRESS.
    // pad initially with 1a and create permutations array of 1a/0d combinations to try later

    // Pads the 60 4-byte length entries at TableLengthsOffset as-needed with an initial "place-holder" 1A
    // byte.
    private bool PadLengths()
    {
      if (_lengthsPadded)
        return true; // already done

      //            Program.ApplictionForm.DisableFileSave = true; // don't allow writing file until lengths/addresses resolve!

      int count = TRACK_COUNT; // # of fields to scan, 60 tracks

      if (_byteProvider.Length < TableLengthsOffset + (count * 4) + 4)
      {
        MessageBox.Show("File length is too short!");
        return false;
      }

      _baseTracksLength = 0;

      try
      {
        _lengthChanges.Clear();

        UInt32 prevVal = 0;

        _index = TableLengthsOffset;

        // NOTE: if value is 0, we don't cross-check with the address-table (like we do in PadOffsets())
        // because the address-table FOLLOWS this lengths table in memory and is mis-alligned at that point...
        for (int ii = 0; ii < count; ii++)
        {
          UInt32 val1 = GetWord(); // auto-incerments _index

          if (val1 > MAX_LENGTH) // Check if previous empty slot needs padding because this slot is > MAX_LENGTH
          {
            if (ii == 0)
            {
              MessageBox.Show("First length-table entry is over 0x0000ffff. Byte(s) possibly missing\n" +
                  "before length-table OR a length actually is very large - will try to continue but\n" +
                  "you may need to hand-pad the length-table...");
              _lengthsPadded = true;
              return true;
            }

            if (prevVal != 0)
            {
              MessageBox.Show("Length-table entry is over 0x0000ffff at track: " + (ii + 1) + " and previous length is non-zero.\n" +
                  "More than one empty slot before this slot but one of them had a single 1A or 0D in it?\n" +
                  "OR a length actually is very large - will try to continue...\n" +
                  "but you may need to hand-pad the length-table...");
              _lengthsPadded = true;
              return true;
            }

            // found a bad length... (previous length has a missing 1A or 0D in it)
            PadLength1A(ii - 1);

            MessageBox.Show("Length at track: " + (ii + 1) + " is  over 0x0000ffff.\n" +
                "Trying to fix this by padding the previous all-0 length...\n" +
                "(assuming a single 1A or 0D was there...)");
          }
          else if (val1 != 0 && (val1 & 0xff) == 0) // This slot needs padding if the next non-zero slot following it is > MAX_LENGTH
          {
            // have val1 and val2, both non-zero so that we can detect a byte-shift
            UInt32 val2;
            int iSlot = ii + 1;
            GetNonZeroLengthSlot(ref iSlot, out val2);

            if (iSlot < 0) // ran out of slots?
            {
              UInt32 addrVal;
              int iAddrSlot = 0;
              GetNonZeroAddressSlot(ref iAddrSlot, out addrVal);

              // ...pad if remaining length slots were 0 but first address-table slot is over MAX_ADDRESS
              if (iAddrSlot >= 0 && addrVal > MAX_ADDRESS)
              {
                // found a bad length... (this length has a missing 1A or 0D in it)
                PadLength1A(ii);
                //MessageBox.Show("Padded missing byte in track: " + (ii + 1) + " with 1A");
              }

              break; // no more non-zero lengths, we're done!
            }

            if (val2 > MAX_LENGTH)
            {
              // found a bad length... (this length has a missing 1A or 0D in it)
              PadLength1A(ii);
              //MessageBox.Show("Padded missing length byte in track: " + (ii + 1) + " with 1A");
            }
          }

          prevVal = val1;
        }

        //if (_lengthChanges.Count > 0)
        //    MessageBox.Show("Lengths padded with 1A: " + _lengthChanges.Count);
        //else
        //    MessageBox.Show("No padding needed in lengths table...");
        _lengthsPadded = true;
        return true;
      }
      catch
      {
        MessageBox.Show("Exception in PadLengths()!");
        return false;
      }
      finally
      {
        // re-evaluate everything... (including _totalTracksLength)
        LoadLengthsAndOffsets();
        _baseTracksLength = _totalTracksLength;
        PrintLengthChangesArray();
      }
    }

    // pad initially with 1a and create permutations array of 1a/0d combinations to try later
    private void PadLength1A(int iSlot)
    {
      UInt32 index = TableLengthsOffset + (UInt32)(iSlot * 4);

      // we insert 1A in byte 1 of iSlot (byte 0 is the least significant)...
      // (bytes 2 and 3 we assume are 00 - if any tracks longer than ffff in any of my songs - we need to re-consider the algorithm...)
      _byteProvider.InsertBytes(index + 2, new byte[] { 0x1a });

      _hexBox.Select(index, 4); // update hexBox and select length we just padded

      // read the modified previous track's length word and save it to a byte array
      // then substitute our possible missing chars for the remaining 3 permutations
      UInt32 len1 = GetWord(index);
      byte[] bytes = BitConverter.GetBytes(len1);
      bytes[1] = 0x0d;
      UInt32 len2 = BitConverter.ToUInt32(bytes, 0);
      bytes[1] = bytes[0];
      bytes[0] = 0x1a;
      UInt32 len3 = BitConverter.ToUInt32(bytes, 0);
      bytes[0] = 0x0d;
      UInt32 len4 = BitConverter.ToUInt32(bytes, 0);

      // len1 has the current value that's in the hex-edit control
      // 2 + MAX_LENGTH_COMBINATIONS
      _lengthChanges.Add(new UInt32[2 + 4] { (UInt32)iSlot, index, len1, len2, len3, len4 }); // add 4 possible 1a/0d combinations
    }

    // returns by reference the first lengths-slot and its value given a 0-based starting-slot
    // returns slot = -1 if no occupied slots found
    private void GetNonZeroLengthSlot(ref int slot, out UInt32 val)
    {
      UInt32 addr = TableLengthsOffset + (UInt32)(slot * 4);
      val = 0;

      for (int ii = slot; ii < TRACK_COUNT; ii++)
      {
        val = GetWord(addr);

        if (val != 0)
        {
          slot = ii;
          return;
        }

        addr += 4;
      }

      slot = -1;
    }

    // Pads the 60 4-byte address entries at TableAddressesOffset as-needed with an initial "place-holder" 1A
    // byte.
    private bool PadAddresses()
    {
      if (_addressesPadded)
        return true; // already done

      //            Program.ApplictionForm.DisableFileSave = true; // don't allow writing file until lengths/addresses resolve!

      int count = TRACK_COUNT; // # of fields to scan, 60 tracks

      if (_byteProvider.Length < TableAddressesOffset + (count * 4) + 4)
      {
        MessageBox.Show("File length is too short!");
        return false;
      }

      try
      {
        _addressChanges.Clear();

        UInt32 prevVal = 0;

        _index = TableAddressesOffset;

        for (int ii = 0; ii < count; ii++)
        {
          UInt32 val1 = GetWord(); // auto-incerments _index

          if (val1 == 0)
          {
            // check corresponding length-slot (already padded) - if it's not 0 also, we have a missing
            // address here, some combination of 1A/0D
            UInt32 val2 = GetWord(TableLengthsOffset + (ii * 4));
            if (val2 != 0)
            {
              // pad it with 1A and add to add to _addressChanges
              PadAddress1A(ii);

              // our gridView needs to be re-loaded because inserting a byte pushed all addresses down by one...
              LoadLengthsAndOffsets();

              MessageBox.Show("Address is 0 at track " + (ii + 1) + " but it's length is " + String.Format("{0:X8}\n", val2) + " hex." +
                  "\nPadding with 1A... (will this work??? not if two or more bytes missing like 00 1A 0D 0D...)");
            }
          }
          else if (val1 > MAX_ADDRESS) // Check if previous empty slot needs padding because this slot is > MAX_ADDRESS
          {
            if (ii == 0)
            {
              MessageBox.Show("Aborting: First address-table entry is over 0x00ffffff.\n\n" +
                  "Byte(s) missing before address-table?");
              return false;
            }

            if (prevVal != 0)
            {
              MessageBox.Show("Aborting: address-table entry is over 0x00ffffff at track: " + (ii + 1) + " and previous address is non-zero.\n" +
                  "More than one empty slot before this slot but one of them had a single 1A or 0D in it?");
              return false;
            }

            // found a bad address... (previous address has a missing 1A or 0D in it)
            PadAddress1A(ii - 1);

            MessageBox.Show("Address at track: " + (ii + 1) + " is  over 0x00ffffff.\n" +
                "Trying to fix this by padding the previous all-0 address...\n" +
                "(assuming a single 1A or 0D was there...)");
          }
          else if ((val1 & 0xff) == 0) // This slot needs padding if the next non-zero slot following it is > MAX_ADDRESS
          {
            // have val1 and val2, both non-zero so that we can detect a byte-shift
            UInt32 val2;
            int iSlot = ii + 1;
            GetNonZeroAddressSlot(ref iSlot, out val2);

            if (iSlot < 0) // ran out of slots?
            {
              // If byte missing from last address-slot, track-mute flags will be left-shifted 1 byte... (normally ff if unmuted).
              // NOTE: this test will FAIL TO CATCH the last bad address if 00 was originally in DEF_MUTES_OFFSET (muted-state?)
              if (_byteProvider.ReadByte(DEF_MUTES_OFFSET - 1) != 0)
              {
                // found a bad address... (this address has a missing 1A or 0D in it)
                PadAddress1A(ii);
                //MessageBox.Show("Padded missing address byte in track: " + (ii + 1) + " with 1A");
              }

              break; // no more non-zero addresses, we're done!
            }

            if (val2 > MAX_ADDRESS)
            {
              // found a bad address... (this address has a missing 1A or 0D in it)
              PadAddress1A(ii);
              //MessageBox.Show("Padded missing address byte in track: " + (ii + 1) + " with 1A");
            }
          }

          prevVal = val1;
        }
        //if (_addressChanges.Count > 0)
        //    MessageBox.Show("Addresses padded with 1A: " + _addressChanges.Count);
        //else
        //    MessageBox.Show("No padding needed in addresses table...");

        _addressesPadded = true;
        return true;
      }
      catch
      {
        MessageBox.Show("Exception in PadAddresses()!");
        return false;
      }
      finally
      {
        // re-evaluate everything...
        LoadLengthsAndOffsets();
        PrintAddressChangesArray();
      }
    }

    private void PadAddress1A(int iSlot)
    {
      UInt32 index = TableAddressesOffset + (UInt32)(iSlot * 4);

      // we insert 1A in byte 1 slot (byte 0 is the least significant)...
      _byteProvider.InsertBytes(index + 1, new byte[] { 0x1a });

      _hexBox.Select(index, 4); // update hexBox and select address we just padded

      // read the modified previous track's address word and save it to a byte array
      // then substitute our possible missing chars for the remaining 3 permutations
      UInt32 addr1 = GetWord(index);
      byte[] bytes = BitConverter.GetBytes(addr1);
      bytes[2] = 0x0d;
      UInt32 addr2 = BitConverter.ToUInt32(bytes, 0);
      bytes[2] = bytes[1];
      bytes[1] = 0x1a;
      UInt32 addr3 = BitConverter.ToUInt32(bytes, 0);
      bytes[1] = 0x0d;
      UInt32 addr4 = BitConverter.ToUInt32(bytes, 0);
      bytes[1] = bytes[0];
      bytes[0] = 0x1a;
      UInt32 addr5 = BitConverter.ToUInt32(bytes, 0);
      bytes[0] = 0x0d;
      UInt32 addr6 = BitConverter.ToUInt32(bytes, 0);

      // addr1 has the current value that's in the hex-edit control
      // 2 + MAX_ADDRESS_COMBINATIONS
      _addressChanges.Add(new UInt32[2 + 6] { (UInt32)iSlot, index, addr1, addr2, addr3, addr4, addr5, addr6 }); // add 4 possible 1a/0d combinations

    }

    // returns by reference the first addresses-slot and its value given a 0-based starting-slot
    // returns slot = -1 if no occupied slots found
    private void GetNonZeroAddressSlot(ref int slot, out UInt32 val)
    {
      UInt32 addr = TableAddressesOffset + (UInt32)(slot * 4);
      val = 0;

      for (int ii = slot; ii < TRACK_COUNT; ii++)
      {
        val = GetWord(addr);

        if (val != 0)
        {
          slot = ii;
          return;
        }

        addr += 4;
      }

      slot = -1;
    }

    private void PrintLengthChangesArray()
    {
      // print list of length changes made on right status panel
      if (_lengthChanges.Count > 0)
      {
        // Display list of possible lengths
        string sDisp = "Lengths...\n";
        for (int ii = 0; ii < _lengthChanges.Count; ii++)
        {
          sDisp += "Track " + (_lengthChanges[ii][0] + 1).ToString() + ", " + String.Format("{0:X4}\n", _lengthChanges[ii][1]);

          for (int jj = 2; jj < MAX_LENGTH_COMBINATIONS + 2; jj++)
            sDisp += String.Format("{0:X8}\n", _lengthChanges[ii][jj]);

          sDisp += "\n";
        }
        labelLengths.Text = sDisp;
      }
    }

    private void PrintAddressChangesArray()
    {
      // print list of address changes made on right status panel
      if (_addressChanges.Count > 0)
      {
        string sDisp = "Offsets...\n";
        for (int ii = 0; ii < _addressChanges.Count; ii++)
        {
          sDisp += "Track " + (_addressChanges[ii][0]).ToString() + ", " + String.Format("{0:X4}\n", _addressChanges[ii][1]);

          for (int jj = 2; jj < MAX_ADDRESS_COMBINATIONS + 2; jj++)
            sDisp += String.Format("{0:X8}\n", _addressChanges[ii][jj]);

          sDisp += "\n";
        }

        labelOffsets.Text = sDisp;
      }
    }
    #endregion

    #region Fix Offsets And Lengths

    private bool FixOffsetsAndLengths()
    {
      if (_lengthsAndAddressesResolved)
        return true;

      bool ret = false;

      // Here, we've padded the length table and/or addresses table with 1A chars as-needed and
      // recorded the offsets we changes in _lengthChanges and _addressChanges.  Now we need to
      // check the base-address in DEF_BASE_ADDRESS_STORAGE_OFFSET to see that it's DEF_TRACKS_BASE_ADDRESS,
      // and then locate our base track (that has that address) and start adding its corresponding length
      // then searching for that address to ensure it's valid... if not found we have to see if it's
      // found if that length had a 1A added when we try it as a 1D... if still not found we need to substitute
      // 0D in the addresses we added 1A to... for both a length with 1A and with 0D... finally when all have been found
      // we should check for duplicate addresses and void all changes if two are identical...
      // check base address
      UInt32 storedBaseAddress = GetWord(DEF_BASE_ADDRESS_STORAGE_OFFSET);
      if (storedBaseAddress != TracksBaseAddress)
      {
        int maxIns = 10; // try to insert up to 10 midi-channel "0d" to see if it aligns the base-address

        // try to fix by padding 0D in midi-channel block at 0624 hex...
        int ii;
        for (ii = 0; ii < maxIns; ii++)
        {
          _byteProvider.InsertBytes(0x0624 + 7, new byte[] { 0x0d });
          storedBaseAddress = GetWord(DEF_BASE_ADDRESS_STORAGE_OFFSET);
          if (storedBaseAddress == TracksBaseAddress)
            break;

        }

        if (ii >= maxIns)
        {
          _byteProvider.DeleteBytes(0x0624 + 7, maxIns); // remove what we inserted - did not work

          MessageBox.Show("ABORTING!!!!\nBase address at " + String.Format("{0:X8}", DEF_BASE_ADDRESS_STORAGE_OFFSET) + " should be " +
              String.Format("{0:X8}", TracksBaseAddress) + "\nbut is " + String.Format("{0:X8}\n\n", storedBaseAddress) +
              "Usually a missing midi-channel byte \"0D\" in 60-byte block at 0624 hex.\n" +
              "Tried inserting 5 0x0D midi-channels and the base address is still not positioned correctly!\n\n" +
              "EDIT THIS NOW IN OPEN HEX EDITOR AND PRESS FIX BUTTON AGAIN...");

          return false;
        }
      }

      // check end address
      UInt32 storedEndAddress = GetWord(DEF_END_ADDRESS_STORAGE_OFFSET);

      // this is the "target" we are aiming for by trial-and-error - computing all combinations...
      // if we miss the target, then the stored end address is mis-aligned or there is more than one
      // missing byte in a length-entry (which would make it 0 so TODO is to detect and handle this!)
      UInt32 referenceLength = storedEndAddress - storedBaseAddress;

      if (_lengthChanges.Count > 0)
      {
        //MessageBox.Show("End address at " + String.Format("{0:X8}", DEF_END_ADDRESS_STORAGE_OFFSET) +
        //    " should be: " + String.Format("{0:X8}", TracksBaseAddress + _totalTracksLength) + "\nbut is:" + String.Format("{0:X8}", storedEndAddress));

        // here we should try different permutations of data in the _lengthChanges List
        // we could first subtrack the 1a length and add back the 0d length then check... etc
        // if 1 length changed we have 1 try
        // if 2 lengths changed we have 2 to the 2nd power - 1 = 3 tries
        // if 3 lengths changed we have 2 to the 3rd power - 1 = 7 tries
        // etc.

        // we need to save old table because FixLengths() expects the original padded values to be in the table
        if (_lengthSolutionNumberToUse == 1)
        {
          // copy length table to preserve it
          _hexBox.Select(TableLengthsOffset, TRACK_COUNT * 4);
          _hexBox.Copy();
          _hexBox.Select(TableLengthsOffset, 0);
        }
        else
        {
          // paste back our original table
          _hexBox.InsertActive = true; // make sure we are in insert mode rather than replace...
          _hexBox.Select(TableLengthsOffset, TRACK_COUNT * 4);
          _hexBox.ScrollByteIntoView();
          _hexBox.Paste(); // replace selected zone in hexBox with clipboard text (paste in the new track)
          _hexBox.Invalidate();
          _hexBox.Select(TableLengthsOffset, 0);
        }

        // ok here we go...
        // let's fix the lengths first...
        int lenRet = FixLengths(referenceLength, (UInt32)_baseTracksLength, _lengthSolutionNumberToUse);

        if (lenRet > 1)
        {
          //MessageBox.Show("There are " + lenRet + " length-table combinations that add up to " + referenceLength + ".\n" +
          //    "About to test solution " + _lengthSolutionNumberToUse + " together with the address-table...");

          if (_lengthSolutionNumberToUse <= lenRet)
          {
            if (_lengthSolutionNumberToUse == 1)
              MessageBox.Show("Found " + lenRet + " combinations of 1a/0d out of " + _lengthChanges.Count + ".\n" +
                  "Using first solution... if this fails, Press Fix Offsets And Lengths again to try successive solutions...");
            else
              MessageBox.Show("Using length-solution: " + _lengthSolutionNumberToUse);

            _lengthSolutionNumberToUse++;

            // next time user presses Fix Lengths And Offsets, the next solution will be tried
          }
          else
          {
            MessageBox.Show("Cannot make any length-solution work!");
            _lengthSolutionNumberToUse = 1;
            Clipboard.Clear();
          }
        }
        else if (lenRet == 0)
          MessageBox.Show("No combinations of 1a and 0d in lengths equals\n" +
              "the end address minus the base address! (" + referenceLength + ")\n\n" +
              "CHECK FOR ONE OR MORE PRECEEDING LENGTHS THAT END IN 00\n" +
              "AND TRY PADDING THEM ONE AT A TIME MANUALLY!\n\n" +
                           "Delta end minus base = " + referenceLength + "\n" +
                           "Sum of length-table lengths = " + GetSumOfTableLengths());
        //else if (lenRet == -1)
        //    MessageBox.Show("No padding in length-table so no solutions to try...!");
        //else if (lenRet == 1)
        //    MessageBox.Show("Found a unique combination of 1a/0d across  " + _lengthChanges.Count + " corrupt length words!\nFixing lengths...");
      }

      // re-evaluate and load this form's info...
      LoadLengthsAndOffsets(); // update _totalTracksLength

      if (referenceLength == _totalTracksLength)
      {
        // now let's fix the addresses... we locate the base address in the addresses list, then add its
        // corresponding length - that should give the next address which should be in the table. If it is, keep going,
        // but if not - search for it in the addressChanges entries - it should be there. That gives us the track #
        // where we can store the address and also get its length to add to it to locate the next address, and so-on.
        // Finally, the last address plus length should = the stored end address. Also keep a countdown of tracks to
        // know when to compare to the end address...

        // NOTE: instead of writing lengths and addresses into the hex control then reloading, maybe we should just
        // change it in this form's gridView cells - call a routine to reevaluate top boxes but that won't re-load
        // the hex control into the gridView... then we need a routine to save the gridView to the hex control and from
        // there it can be saved to file...
        //
        // UPDATE: found that we have to write the hex control to do the padding-phase
        // anyway - so may as well keep writing to it...
        // Check for a length with no matching address or an address with no matching length
        int track = FindAddress(TracksBaseAddress);

        //MessageBox.Show("base track is " + track);

        if (track == -2)
          MessageBox.Show("Error in FindAddress()!");
        else if (track < 0)
          MessageBox.Show("Base address not found in track address table!");
        else // found the base track
        {
          int trackCount = GetTrackCount(); // # of populated tracks

          //MessageBox.Show("trackCount is " + trackCount);

          UInt32 prevAddress = TracksBaseAddress;
          UInt32 nextAddress = prevAddress;
          UInt32 trackCounter = 0;

          for (;;)
          {
            // good place here to show the track-order in memory in the gridView??????

            UInt32 length;
            try { length = GetWord(TableLengthsOffset + (track * 4)); }
            catch { length = 0; MessageBox.Show("Error getting length..."); }

            //MessageBox.Show("length for track " + track + " is " + length);

            if (length == 0)
            {
              MessageBox.Show("Error finding length!");
              break;
            }

            nextAddress = prevAddress + length;

            if (++trackCounter >= trackCount)
            {
              // check end-address against computed value, the last address better be at end address!
              if (nextAddress == storedEndAddress)
              {
                // populate the column of file offsets for the tracks
                DisplayAllFileOffsets();

                //MessageBox.Show("All addresses and lengths are OK!");
                ret = true;
              }
              else
                MessageBox.Show("Last computed address " + String.Format("{0:X8}\n", nextAddress) +
                    "does not match end address " + String.Format("{0:X8}", storedEndAddress));
              break;
            }

            track = FindAddress(nextAddress); // returns -1 when finished if address not found

            if (track == -2)
            {
              MessageBox.Show("Error in FindAddress()!");
              break;
            }

            //MessageBox.Show("track counter normal: " + trackCounter);

            if (track == -1) // ...track was not in the address table...
            {
              //MessageBox.Show("Diagnostic trackCounter: " + trackCounter + " trackCount: " + trackCount); // 34/48 first time

              // track is -1 (not found) (it will come here for every bad address in the table)
              // address we want is not in table - need to search for it in addressChanges List...
              int tempTrack = SearchAddressChanges(nextAddress);

              if (tempTrack < 0)
              {
                // address not in _addressChanges array
                PrintAddressChangesArray();
                MessageBox.Show("Aborting...No address found in permutations array!\n\nnextAddress: " + String.Format("{0:X8} ", nextAddress) +
                    "prevAddress: " + String.Format("{0:X8}\n", prevAddress) + "length: " + String.Format("{0:X8}", length));
                break;
              }

              //MessageBox.Show("Found address in permutations array!\n\nnextAddress: " + String.Format("{0:X8} ", nextAddress) +
              //    "length: " + String.Format("{0:X8} ", length) + "track: " + (tempTrack + 1).ToString());

              // Compute offset into address table
              UInt32 tableAddr = TableAddressesOffset + (UInt32)(tempTrack * 4);

              // Update data in the hex-control
              PutWord(tableAddr, nextAddress);

              // Update track so length of this track can be obtained at the top of the for loop...
              track = tempTrack;
            }
            // else track >= 0

            //MessageBox.Show("Next address " + String.Format("{0:X8}", nextAddress) + " found at track " + track+1);

            prevAddress = nextAddress;
          }
        }
      }
      else
        MessageBox.Show("referenceLength != _totalTracksLength!\n" +
            "(means that storedEndAddress - storedBaseAddress != sum of track-lengths...)\n\n" +
            "referenceLength: " + referenceLength.ToString() + "\n" +
            "_totalTracksLength: " + _totalTracksLength.ToString() + "\n\n" +
            "(Probably a misplaced 1a/0d in length-table, look for one or more lengths that\n" +
            "ends with 00 and should not - OR at 06A0 make sure the end-offset is three-bytes!");

      if (ret)
      {
        if (AllLengthsHaveAddress())
          _lengthsAndAddressesResolved = true;
      }

      // re-evaluate and load this form's info...
      LoadLengthsAndOffsets();

      return ret;
    }

    // returns the track-index if found or -1 if not found
    private int SearchAddressChanges(UInt32 nextAddress)
    {
      for (int ii = 0; ii < _addressChanges.Count; ii++)
        for (int jj = 2; jj < MAX_ADDRESS_COMBINATIONS + 2; jj++)
          if (nextAddress == _addressChanges[ii][jj])
            return (int)_addressChanges[ii][0]; // return the track-index
      return -1;
    }

    private void DisplayAllFileOffsets()
    {
      UInt32 addr = TableAddressesOffset;

      for (int ii = 0; ii < TRACK_COUNT; ii++, addr += 4)
        DisplayFileOffset(ii, GetWord(addr));
    }

    //private void DisplayAllFileOffsets()
    //{
    //    dataGridView1.Refresh();

    //    // populate the column of file offsets for the tracks
    //    foreach (DataGridViewRow r in dataGridView1.Rows)
    //    {
    //        UInt32 addr;

    //        try { addr = Convert.ToUInt32(r.Cells[2].Value.ToString(), 16); } // 16 is base
    //        catch { addr = 0; }

    //        DisplayFileOffset(r.Index, addr);
    //    }
    //}

    private void DisplayFileOffset(int row, UInt32 addr)
    {
      if (row < dataGridView1.Rows.Count)
      {
        if (addr != 0)
        {
          // Computer 0-based file offset of each track
          addr -= TracksBaseAddress;
          addr += TracksFileOffset + 2; // (+ 2 is for 0090 magic number!)                                                  
          dataGridView1.Rows[row].Cells[3].Value = String.Format("{0:X8}", addr); // Write file-offset to column 3
        }
        else
          SetRowGray(row); // shade empty track gray
      }
    }

    private void SetRowGray(int row)
    {
      foreach (DataGridViewColumn c in dataGridView1.Columns)
      {
        dataGridView1.Rows[row].Cells[c.Index].Style.BackColor = Color.LightGray;
        dataGridView1.Rows[row].Cells[c.Index].Value = ""; // Blank if unknown
      }
    }

    // Returns 0-based track-index given address to find, -1 is "not found"
    private int FindAddress(UInt32 searchAddr)
    {
      UInt32 addr = TableAddressesOffset;

      try
      {
        for (int ii = 0; ii < TRACK_COUNT; ii++, addr += 4)
          if (GetWord(addr) == searchAddr)
            return ii;

        return -1; // not found
      }
      catch
      {
        return -2;
      }
    }

    // checks to be sure each non-zero length has a non-zero address and vice-versa
    private bool AllLengthsHaveAddress()
    {
      UInt32 addrLen = TableLengthsOffset;
      UInt32 addrOff = TableAddressesOffset;

      try
      {
        for (int ii = 0; ii < TRACK_COUNT; ii++)
        {
          UInt32 off = GetWord(addrOff);
          UInt32 len = GetWord(addrLen);

          if (off == 0 && len != 0)
          {
            MessageBox.Show("Found Address is 0 with non-zero length at track: " + ii / 4 + "\nTry replacing address with 000D0D0D, 000D0D1A, Etc.");
            return false;
          }
          if (off != 0 && len == 0)
          {
            MessageBox.Show("Found Length is 0 with non-zero address at track: " + ii / 4 + "\nTry replacing length with 00000D0D, 00000D1A, Etc.");
            return false;
          }

          addrLen += 4;
          addrOff += 4;
        }

        return true;
      }
      catch
      {
        MessageBox.Show("Exception in AllLengthsHaveAddresses()");
        return false;
      }
    }

    // checks to be sure each non-zero length has a non-zero address and vice-versa
    //private bool AllLengthsHaveAddress()
    //{
    //    // Check for a length with no matching address or an address with no matching length
    //    foreach (DataGridViewRow r in dataGridView1.Rows)
    //    {
    //        UInt32 length, addr;
    //        try { length = Convert.ToUInt32(dataGridView1.Rows[r.Index].Cells[1].Value.ToString(), 16); } // 16 is base
    //        catch { length = 0; }
    //        try { addr = Convert.ToUInt32(dataGridView1.Rows[r.Index].Cells[2].Value.ToString(), 16); }
    //        catch { addr = 0; }

    //        if (addr == 0 && length != 0)
    //        {
    //            MessageBox.Show("Found Address is 0 with non-zero length at track: " + r.Index + "\nTry replacing address with 000D0D0D, 000D0D1A, Etc.");
    //            return false;
    //        }
    //        if (addr != 0 && length == 0)
    //        {
    //            MessageBox.Show("Found Length is 0 with non-zero address at track: " + r.Index + "\nTry replacing length with 00000D0D, 00000D1A, Etc.");
    //            return false;
    //        }
    //    }

    //    return true;
    //}

    // Returns Track given address to find
    //private int FindAddress(UInt32 searchAddr)
    //{
    //    foreach (DataGridViewRow r in dataGridView1.Rows)
    //    {
    //        try
    //        {
    //            if (searchAddr == Convert.ToUInt32(dataGridView1.Rows[r.Index].Cells[2].Value.ToString(), 16)) // 16 is base
    //                return r.Index;
    //        }
    //        catch { return -2; }
    //    }
    //    return -1;
    //}

    private int GetSumOfTableLengths()
    {
      UInt32 addr = TableLengthsOffset;
      int sum = 0;
      for (int ii = 0; ii < TRACK_COUNT; ii++, addr += 4)
        sum += (int)GetWord(addr);
      return sum;
    }

    // Returns Number of tracks that have a length > 0
    private int GetTrackCount()
    {
      int trackCount = 0;
      UInt32 addr = TableLengthsOffset;
      for (int ii = 0; ii < TRACK_COUNT; ii++, addr += 4)
        if (GetWord(addr) != 0)
          trackCount++;
      return trackCount;
    }

    // Returns Number of tracks that have a length > 0
    //private UInt32 GetTrackCount()
    //{
    //    UInt32 trackCount = 0;
    //    foreach (DataGridViewRow r in dataGridView1.Rows)
    //    {
    //        try
    //        {
    //            if (Convert.ToUInt32(dataGridView1.Rows[r.Index].Cells[1].Value.ToString(), 16) > 0)
    //                trackCount++;
    //        } // 16 is base
    //        catch { return 0; }
    //    }
    //    return trackCount;
    //}

    // return -1 if the length-table was ok and did not have any missing bytes originally,
    // return 0 if no solution was found or the positive count of the number of solutions that makes all lengths add
    // up to end-address - start-address of all tracks.
    //
    // usually there is 1 solution - if there are more - reload song and call this again, incrementing solutionIndex
    //
    // This routine actually writes a length-table solution into the hexBox so you will have to re-populate the hex-box to try alternate
    // solutions if this one fails... (usually there is only one length-table solution anyway...)
    //
    // baseLength is the length of the original padded length-table before we start changing it
    private int FixLengths(UInt32 referenceLength, UInt32 baseLength, int solutionNumberToUse)
    {
      if (_lengthChanges.Count == 0)
        return -1;

      // Subtract off the original padded place-holder length for each padded length entry so we can add all combinations below...
      for (int ii = 0; ii < _lengthChanges.Count; ii++)
        baseLength -= _lengthChanges[ii][2]; // subtract the 00001AXX original, padded place-holder length

      // initilize the two-bit "counters" (really indexes associated with each _lengthChanges list-entry that
      // points to offset 2, 3, 4 or 5 in that entry. These offsets each contain a pre-computed trial-length.
      // (offset 0 is the track number and offset 1 is the file-offset to the track's length-slot)
      int[] counters = new int[_lengthChanges.Count];
      int[] saveCounters = new int[_lengthChanges.Count]; // list of two-bit pointers to one-of: "len1, len2, len3 or len4"
      for (int ii = 0; ii < _lengthChanges.Count; ii++)
        counters[ii] = 0;
      int foundCount = 0;

      do
      {
        if (TestLengths(counters, baseLength, referenceLength))
        {
          // we save one of, possibly many, sets of counters that works based on solutionNumberToUse... we then try that
          // solution elsewhere - and the user presses Fix Offsets And Lengths again to try successive solutions
          // "Usually" there's just one solution!
          //
          // (this is a set of 2-bit pointers or indices to one of "len1, len2, len3 or len4"
          // into each padded length, describing a set that makes the total length add up to the correct track-length)
          //
          // There could, in-theory be many solutions that "add up" correctly, but only one will fit with the address-table
          if (++foundCount == solutionNumberToUse)
            counters.CopyTo(saveCounters, 0); // have to use CopyTo()!
        }
      } while (IncCounters(0, _lengthChanges.Count, 3, ref counters));

      if (foundCount > 0)
      {
        // TODO need to give user option to try different solutions... then restore the length-table and try another if that
        // does not work...

        // for each bad length slot...
        //string sDisp = "Modified:\n";
        for (int ii = 0; ii < _lengthChanges.Count; ii++)
        {
          //UInt32 trackNum = _lengthChanges[ii][0];
          UInt32 fileOffset = _lengthChanges[ii][1];
          //saveCounters[ii] is a two-bit index that points to len1, len2, len3 or len4
          UInt32 newLength = _lengthChanges[ii][saveCounters[ii] + 2]; // +2 to get past trackNum and fileOffset...

          // display the track # and file-offset and the solution found for this slot
          //sDisp += "Track " + trackNum + ", " + String.Format("fileOffset: {0:X4}, ", fileOffset) +
          //    String.Format("newLength: {0:X8}\n", newLength);

          // now actually write our solution to the hex-edit control
          PutWord(fileOffset, newLength);
        }
        //MessageBox.Show(sDisp);
      }

      return foundCount;
    }

    // referenceLength is the stored end-address minus the stored base-address
    // baseLength is _totalTracksLength minus the place-holder lengths for tracks where the length was in-error...
    // counters is an int-array of indexes representing one of four possible sets of lengths to try on this call...
    private bool TestLengths(int[] counters, UInt32 baseLength, UInt32 referenceLength)
    {
      // diagnostic to test the IncCounters routine
      //string s = "Counters (" + counters.Length + "): ";

      //for (int ii = counters.Length-1; ii >= 0; ii--)
      //    s += counters[ii] + " ";

      //MessageBox.Show(s);

      // here we need to apply a block of counters (indexes) to try the lengths combination
      // they represent to find a match with the stored end-offset minus the base offset
      // (total combined track length in bytes)

      //                    for (int ii = 0; ii < _lengthChanges.Count; ii++)
      //                    {
      //                        tempLength -= _lengthChanges[ii][2]; // subtract the 1axx plac-holder length
      //                    }

      // counters index can be 0-3 for _lengthChanges and 0-5 for _addressChanges
      // Need to add 2 to that to get index.. so to use: trial_length = _lengthChanges[ii][counters[ii] + 2]
      for (int ii = 0; ii < _lengthChanges.Count; ii++)
      {
        baseLength += _lengthChanges[ii][counters[ii] + 2];
        //MessageBox.Show(String.Format("counters[ii] = {0:X4}, ", counters[ii]));
        //MessageBox.Show(String.Format("_lengthChanges[ii][counters[ii] + 2] = {0:X4}, ", _lengthChanges[ii][counters[ii] + 2]));
      }

      return baseLength == referenceLength;
    }
    #endregion

    #region Auto-Fix Tracks Main Loops

    private void FixTracks()
    {
      if (!_lengthsPadded || !_addressesPadded || !_lengthsAndAddressesResolved)
      {
        MessageBox.Show("Need to click \"Fix Offsets And Lengths\" first...");
        return;
      }

      if (_fixingTracks || !IsGoodMagicNumber())
        return;

      _fixingTracks = true;

      //MessageBox.Show("SUCCESS! Found magic number for tracks...\n" +
      //    "Magic Number: " + String.Format("{0:X4}\n", TRACKS_START_MAGIC_NUMBER) +
      //    "Is at file offset: " + String.Format("{0:X8}", addr));

      // we must fix the tracks in the order they appear in the file. the base-track is lowest
      // and others follow but are not always in the same order they appear in the length/addresses tables
      // in the file-header - so load the file-offsets we expect into a KeyValue array and sort it low-to-high
      // with key being the 0-based track index (r.Index)
      var list = new List<KeyValuePair<int, UInt32>>();

      // add DataGridViewRow row indices and file-offsets to a list of KeyValue pairs
      foreach (DataGridViewRow r in dataGridView1.Rows)
      {
        UInt32 trackFileOffset;
        try { trackFileOffset = Convert.ToUInt32(dataGridView1.Rows[r.Index].Cells[3].Value.ToString(), 16); }
        catch { trackFileOffset = 0; }

        // only add non-zero entries!
        if (trackFileOffset != 0)
          list.Add(new KeyValuePair<int, UInt32>(r.Index, trackFileOffset));
      }

      // use TrackSortCompareDelegate delegate below to sort lower file-offsets first
      list.Sort(TrackSortCompareDelegate);

      // update total tracks in song (set previously in program but good to have a refresh...)
      TotalTracksInSong = list.Count;
      textBoxTotalTracksInSong.Text = TotalTracksInSong.ToString();

      var badList = new List<KeyValuePair<int, UInt32>>(); // list of just the bad ones that will need checking...

      int badTracks = 0;
      int goodTracks = 0;

      // First - read the "track fixed" flags so that we don't have to load tracks that have been fixed - NOTE: we can't check
      // end-of-track marker vs. length and file-offset because the EOT marker might still appear 00 00 00 00 at file-offset + length
      // but still be missing a byte. This is because the first byte of the next track can be 00... and be left-shifted. So - we set
      // bit 5 at the 05E8 60-byte block to indicate "track fixed"
      foreach (var item in list)
      {
        int index = item.Key;

        if (IsTrackFixed(index))
        {
          // set names field green if track is ok
          dataGridView1.Rows[index].Cells[0].Style.BackColor = Color.LightGreen;
          goodTracks++;
        }
        else
        {
          // set names field yellow if track's condition is unknown
          dataGridView1.Rows[index].Cells[0].Style.BackColor = Color.Yellow;
          badList.Add(item); // build a list of just bad tracks
          badTracks++;
        }
      }

      // Display what we have from the "good-track" flags to start with...
      textBoxGoodTrackCount.Text = goodTracks.ToString();

      DialogResult result1;

      if (badTracks > 0)
      {
        // just go fix tracks if they are all bad...
        if (goodTracks > 0)
        {
          result1 = MessageBox.Show("Scan of 60 \"Track Repaired\" flags (05E8 hex):" +
              "\n\nRepaired: " + goodTracks +
              "\nUnrepaired: " + badTracks +
              "\nEmpty: " + (TRACK_COUNT - goodTracks - badTracks).ToString() +
              "\n\nPress OK to resume repairs, Cancel to quit...",
              "Song: " + System.IO.Path.GetFileNameWithoutExtension(Program.ApplictionForm.FileName),
              MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);

          if (result1 == DialogResult.Cancel)
          {
            _fixingTracks = false;
            return; // abort
          }
        }
      }
      else
      {
        result1 = MessageBox.Show("No bad tracks found!" +
            "\n\nRepaired tracks: " + goodTracks +
            "\nEmpty tracks: " + (TRACK_COUNT - goodTracks).ToString() +
            "\n\n(Press the \"Reset Tracks\" button if you want to start repairs over...",
            "Song: " + System.IO.Path.GetFileNameWithoutExtension(Program.ApplictionForm.FileName),
            MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

        _fixingTracks = false;

        _goodTrackCount = goodTracks;
        return; // abort
      }

      // don't reset goodTracks - we will add to the ones already fixed during the continuing repair-process
      // below...

      badTracks = 0;

      using (var formFixTracks = new FormFixTracks(this))
      {
        foreach (var item in badList)
        {
          UInt32 trackAddr, trackFileOffset;
          int trackLength, trackIndex;

          trackIndex = item.Key;
          trackFileOffset = item.Value;

          string trackName = dataGridView1.Rows[item.Key].Cells[0].Value.ToString();
          try { trackLength = Convert.ToInt32(dataGridView1.Rows[trackIndex].Cells[1].Value.ToString(), 16); } // 16 is base
          catch { trackLength = 0; }
          try { trackAddr = Convert.ToUInt32(dataGridView1.Rows[trackIndex].Cells[2].Value.ToString(), 16); }
          catch { trackAddr = 0; }

          if (trackLength == 0 || trackAddr == 0 || trackFileOffset == 0 || TrackFilters == null || trackIndex >= TrackFilters.Count)
          {
            MessageBox.Show("Bad parameters for track: " + (trackIndex + 1).ToString());
            _fixingTracks = false;
            break; // quit
          }

          // formFixTracks.LoadTrack returns the "broken" track's length (determined from the end-of-track marker of 00 00 00 00)
          // "trackLength" is obtained from the lengths-table in the song-header block (repaired earlier by other algorithms in this file)
          int brokenTrackLength = formFixTracks.LoadTrack(trackName, trackLength, trackAddr, trackFileOffset, trackIndex, TrackFilters[trackIndex]);

          if (brokenTrackLength < 0)
          {
            _fixingTracks = false;
            break; // quit
          }

          int fixNotesRet = -10;
          int fixTimingRet = -10;

          // ref parameters - on input to AutoFixTiming() they set the initial values, on exit they have the values that resolved track-timing
          int divisor = FormFixTracks.MIN_DIVISOR; // min divisor (always starts at 96)
          int variation = FormFixTracks.MAX_VARIATION; // max variation permitted (always starts at 0)

          // only auto-analyze normal-sized tracks...
          if (trackLength <= MAX_TRACK_LENGTH)
          {
            // track's expected length is not the same as the actual length? try to fix...
            if (brokenTrackLength != trackLength) // Could use formFixTracks.MissingByteCounter also...
            {
              int row = 0; // start at the first event...

              // AutoFixEvents Returns:
              // -1 if all good, -2 if error, -3 if canceled, -4 = bad data cell at row, -5 no data to process,
              // -6 = more broken bytes than expected, 0-N index of bad event
              // pass in starting row index, set silent-mode flag true (don't ask any questions!)

              if (formFixTracks.AutoFixEvents(ref row, true) == -1)
              {
                // returns -1 if success, -3 if error, -4 if user-cancel
                fixNotesRet = formFixTracks.AutoFixNotes(0, true);

                if (fixNotesRet == -1)
                  // -4 = cancel, -3 = error, -2 = solution not found, -1 = timing fixed, 0-N timing error at row-index
                  fixTimingRet = formFixTracks.AutoFixTiming(0, ref divisor, ref variation); // start at event row-index 0
              }
            }
            else // track events ok but need to check timing
            {
              // returns -1 if success, -3 if error, -4 if user-cancel
              fixNotesRet = formFixTracks.AutoFixNotes(0, true);

              if (fixNotesRet == -1)
                // -4 = cancel, -3 = error, -2 = solution not found, -1 = timing fixed, 0-N timing error at row-index
                fixTimingRet = formFixTracks.AutoFixTiming(0, ref divisor, ref variation); // start at event row-index 0
            }
          }

          // clear the "Time Fix" field in dataGridView1
          dataGridView1.Rows[trackIndex].Cells[5].Value = "";

          if (fixNotesRet == -1 && fixTimingRet == -1) // if the auto-repair process succeeded, we can replace this track and move on to the next one...
          {
            // At this point the event-count, timing or both was auto-repaired - replace the track in hexBox and move on...

            // Copy repaired track to clipboard
            int bytesCopied = formFixTracks.CopyTrackToClipboard();

            if (bytesCopied != trackLength)
            {
              SetStatusRedOrYellow(trackIndex, true); // red
              break; // abort
            }

            // ReplaceTrack replaces the existing track-data in the hexBox control with new data on the Windows clipboard.
            //
            // Increments goodTracks and sets the track green - prints its own error-messages
            // Returns true if good
            if (!ReplaceTrack(formFixTracks, trackLength, trackAddr, trackFileOffset, trackIndex))
            {
              SetStatusRedOrYellow(trackIndex, true); // red
              break; // abort
            }
          }
          else // need to visually examine the track-data...
          {
            // We only show the dialog if the end-of-track-marker is not located where the table-length requires it to be (i.e. missing
            // 1A or 0D bytes in the track-events data)...
            DialogResult val = formFixTracks.ShowDialog(); // show modally

            // update vars for gridView column 5
            divisor = formFixTracks.Divisor;
            variation = formFixTracks.Variation;

            // update TrackFilters string-collection (user can change it on the Form)
            TrackFilters[trackIndex] = formFixTracks.EventFilters;

            // breaks below... instead don't we want to ask user if we should go on to next? and color yellow if cancel too? since
            // its good-flag is not yet set... need to add to goodTracks and set green if its fixed after OK - check properties????
            if (val == DialogResult.OK) // trackLength, trackAddr, trackFileOffset, trackNumber
            {
              // ReplaceTrack increments goodTracks and sets the track green - prints its own error-messages
              // Returns true if good
              if (!ReplaceTrack(formFixTracks, trackLength, trackAddr, trackFileOffset, trackIndex))
              {
                SetStatusRedOrYellow(trackIndex, true); // red
                break; // abort
              }
            }
            else // cancel pressed while fixing a track with either missing bytes or bad timing...
            {
              // if track has events fixed but not timing, allow user to keep on to the next unfixed track
              if (brokenTrackLength == trackLength)
              {
                SetStatusRedOrYellow(trackIndex, false); // yellow

                result1 = MessageBox.Show("Timing not fixed, press OK to continue to next track, Cancel to abort...",
                    "Track " + (trackIndex + 1).ToString() + ": " + dataGridView1.Rows[trackIndex].Cells[0].Value.ToString(),
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

                if (result1 == DialogResult.Cancel)
                  break;
              }
              else
              {
                SetStatusRedOrYellow(trackIndex, true); // red
                break; // track missing events MUST be fixed as ordered in memory! Have to abort
              }

              // otherwise keep going...

              // set the "Time Fix" field in dataGridView1
              dataGridView1.Rows[item.Key].Cells[5].Value = formFixTracks.Divisor + ", " + formFixTracks.Variation;
            }
          }

          // set the "Time Fix" field in dataGridView1
          dataGridView1.Rows[trackIndex].Cells[5].Value = divisor + ", " + variation;

          SetStatusGreen(trackIndex, formFixTracks.TotalTimeTicks);
          goodTracks++;
        }
      }

      if (goodTracks == TotalTracksInSong)
        MessageBox.Show("All " + goodTracks + " tracks OK!");
      else if (goodTracks > 0)
        MessageBox.Show(goodTracks + " tracks OK of " + TotalTracksInSong);

      _goodTrackCount = goodTracks;
      textBoxGoodTrackCount.Text = _goodTrackCount.ToString();

      _fixingTracks = false;
    }
    #endregion

    #region Delete Track

    private void DeleteTrack(int index)
    {
      try
      {
        UInt32 tableOffset = (UInt32)index * 4;
        UInt32 length = GetWord(TableLengthsOffset + tableOffset);
        UInt32 address = GetWord(TableAddressesOffset + tableOffset);

        if (length == 0 || address == 0)
        {
          MessageBox.Show("Track is empty, deleting name only!");
          string s = "                "; // max name length is 16!
          PutString(TrackNamesOffset + (index * 16), s);
          dataGridView1.SelectedRows[0].Cells[0].Value = s;
          return;
        }

        if (!IsTrackFixed(index))
        {
          MessageBox.Show("This track has not been fixed yet - Press Fix Tracks button!");
          return;
        }

        // to delete track, just change its length by -length...
        if (ModifyTrackLength(index, address, -(int)length))
        {
          // dataGridView1.Rows.RemoveAt(index); // delete this track from dataGridView1 (done in the re-load)

          UInt32 fileOffset = address - TracksBaseAddress + TracksFileOffset + 2; // (+ 2 is for 0090 magic number!)

          // now physically cut the track's data from hexBox
          _hexBox.Select(fileOffset, length);
          _hexBox.Cut(); // cut old track
          _hexBox.Invalidate();

          PutString(TrackNamesOffset + (index * 16), "                "); // max name length is 16!

          LoadLengthsAndOffsets(); // reload
        }
      }
      catch
      {
        MessageBox.Show("Error deleting track...");
      }
    }
    #endregion

    #region Examine Track
    // Returns -1 if error or user-cancel button pressed on FormFixTracks or the zero-based row-index of the last event that was hand-edited
    // viewIndex is the selected event when FormFixTracks pops up
    private int ExamineTrack(int trackIndex, int initialSelectedEventIndex, ref int resumeAtEventRow, bool bInitialAutoInsertMode)
    {
      //MessageBox.Show("SUCCESS! Found magic number for tracks...\n" +
      //    "Magic Number: " + String.Format("{0:X4}\n", TRACKS_START_MAGIC_NUMBER) +
      //    "Is at file offset: " + String.Format("{0:X8}", addr));

      _fixingTracks = true; // lock

      int ret = -1;

      try
      {
        using (FormFixTracks formFixTracks = new FormFixTracks(this))
        {
          UInt32 trackAddr, trackFileOffset;
          int trackLength;

          string trackName = dataGridView1.Rows[trackIndex].Cells[0].Value.ToString();
          try { trackLength = Convert.ToInt32(dataGridView1.Rows[trackIndex].Cells[1].Value.ToString(), 16); } // 16 is base
          catch { trackLength = 0; }
          try { trackAddr = Convert.ToUInt32(dataGridView1.Rows[trackIndex].Cells[2].Value.ToString(), 16); }
          catch { trackAddr = 0; }
          try { trackFileOffset = Convert.ToUInt32(dataGridView1.Rows[trackIndex].Cells[3].Value.ToString(), 16); }
          catch { trackFileOffset = 0; }

          if (trackLength == 0 || trackAddr == 0 || trackFileOffset == 0 || TrackFilters == null || trackIndex >= TrackFilters.Count)
          {
            MessageBox.Show("Bad parameters for track: " + (trackIndex + 1).ToString());
            return -1;
          }

          int lengthRead = formFixTracks.LoadTrack(trackName, trackLength, trackAddr, trackFileOffset, trackIndex, TrackFilters[trackIndex]);

          if (lengthRead < 0)
            return -1; // User pressed cancel on end-of-track marker dialog prompt...

          formFixTracks.InitialSelectedEventIndex = initialSelectedEventIndex;
          formFixTracks.InitialAutoInsertMode = bInitialAutoInsertMode;

          DialogResult result = formFixTracks.ShowDialog(); // show modally

          // update TrackFilters string-collection (user can change it on the Form)
          TrackFilters[trackIndex] = formFixTracks.EventFilters;

          // if Ok was clicked then FormFixTracks.cs copied our data to the clipboard
          // ... so select the old track and replace it with the new.
          if (result == DialogResult.OK)
          {
            if (ReplaceTrack(formFixTracks, trackLength, trackAddr, trackFileOffset, trackIndex))
            {
              if (formFixTracks.MissingByteCount == 0)
              {
                // set names field green if track is ok
                if (!IsTrackFixed(trackIndex))
                {
                  _goodTrackCount++;
                  SetStatusGreen(trackIndex, formFixTracks.TotalTimeTicks);
                }
              }
              else
              {
                // set names field yellow
                if (IsTrackFixed(trackIndex))
                {
                  _goodTrackCount--;
                  SetStatusRedOrYellow(trackIndex, false);
                }
              }
            }
            else
            {
              if (IsTrackFixed(trackIndex))
                _goodTrackCount--;

              SetStatusRedOrYellow(trackIndex, true); // red
            }

            textBoxGoodTrackCount.Text = _goodTrackCount.ToString();

            resumeAtEventRow = formFixTracks.ResumeAtEventRow; // pass back by ref so user can control where a timing analysis should resume

            ret = formFixTracks.LastEditedIndex;
          }
          else
            ret = -1;
        }
      }
      finally
      {
        _fixingTracks = false;
      }

      return ret;
    }
    #endregion

    #region Replace Track

    //trackLength, trackAddr, trackFileOffset, trackNumber
    // returns true if success
    private bool ReplaceTrack(FormFixTracks f, int trackLength, UInt32 trackAddr, UInt32 trackFileOffset, int trackIndex)
    {
      bool bRet = false;

      //!!!!!!!!!!!!!!!!!!!!!!!!!!
      //if (trackIndex == 18)
      //{
      //    f.ShowDialog();
      //    return false;
      // }

      // if Ok was clicked then FormFixTracks.cs copied our data to the clipboard
      // ... so select the old track and replace it with the new.
      if (_hexBox.CanCut() && _hexBox.CanPaste() || trackFileOffset > 0)
      {
        try
        {
          _hexBox.InsertActive = true; // make sure we are in insert mode rather than replace...
          _hexBox.Select(trackFileOffset, f.OriginalBrokenTrackLength);
          _hexBox.ScrollByteIntoView();
          _hexBox.Paste(); // replace selected zone in hexBox with clipboard text (paste in the new track)
          _hexBox.Invalidate();

          // track-dialog modified original track-length? (only should happen if track was
          // corrupt and had to be hand-modified before it was loaded - and after fixing errors,
          // the table-length still did not jibe with the "actual" track length as measured using the
          // end-of-track marker...) - So we need to modify the length in the length-table as well as
          // the addresses of all tracks that follow this one in memory (higher addresses)...
          //
          // Note" an "address" is really the "base" offset plus the lengths of all the tracks
          // before this one. Each "address", when you add its corresponding length-table entry, should
          // have the "next address" somewhere in the address-table - it could be in any of the 59 other
          // 4-byte slots...
          if (f.TrackLength != trackLength)
          {
            try
            {
              // now we need to possibly update the lengths and offsets in both gridView and in the hexBox control...
              int deltaLen = f.TrackLength - trackLength; // allowed to be negative!

              if (ModifyTrackLength(trackIndex, trackAddr, deltaLen))
                bRet = true;

              LoadLengthsAndOffsets(); // refresh gridView with updated offsets and length
            }
            catch { MessageBox.Show("Problem modifying length/address tables for pasted track!"); }
          }
          else
            bRet = true;
        }
        catch { MessageBox.Show("Problem pasting text to hexBox!"); }
      }
      else
        MessageBox.Show("Unable to replace track!");

      return bRet;
    }

    // deltaLen can be negative and is in bytes!
    private bool ModifyTrackLength(int trackIndex, UInt32 trackAddr, int deltaLen)
    {
      // modify length
      int length;
      try { length = Convert.ToInt32(dataGridView1.Rows[trackIndex].Cells[1].Value.ToString(), 16); }
      catch { length = -1; } // flag of -1

      if (length == -1)
        return false;

      length += deltaLen;

      if (length < 0)
        length = 0;

      dataGridView1.Rows[trackIndex].Cells[1].Value = String.Format("{0:X8}", length);
      PutWord(TableLengthsOffset + (trackIndex * 4), (UInt32)length);

      // if deltaLen is the same as the track-length, we are effectively deleting the track so set its table-address 0 also...
      if (length == 0)
      {
        // update the address in the hexBox
        PutWord(TableAddressesOffset + (trackIndex * 4), 0);
        SetRowGray(trackIndex); // shade empty track gray
      }

      int address;

      // modify addresses and file-offsets greater than trackAddr
      foreach (DataGridViewRow r in dataGridView1.Rows)
      {
        try { address = Convert.ToInt32(dataGridView1.Rows[r.Index].Cells[2].Value.ToString(), 16); }
        catch { address = 0; }

        if (address > trackAddr)
        {
          address += deltaLen;

          // update the address in the hexBox
          PutWord(TableAddressesOffset + (r.Index * 4), (UInt32)address);

          // update the address in the gridView
          dataGridView1.Rows[r.Index].Cells[2].Value = String.Format("{0:X8}", address);

          // re-compute the file-offset in the gridView
          DisplayFileOffset(r.Index, (UInt32)address);
        }
      }

      // modify end address
      address = (int)GetWord(DEF_END_ADDRESS_STORAGE_OFFSET) + deltaLen;
      PutWord(DEF_END_ADDRESS_STORAGE_OFFSET, (UInt32)address);

      _totalTracksLength += deltaLen;
      textBoxTotalTracksLength.Text = _totalTracksLength.ToString();

      return true;
    }
    #endregion

    #region Reset All Track-Fixed Flags

    private void TracksReset()
    {
      this.TrackFilters.Clear();
      for (int ii = 0; ii < 60; ii++)
        TrackFilters.Add("9X 8X 10");

      // Clear "track-fixed" flag bit in hexBox data block - I chose an innocuous bit that seems to
      // be unused in Hybrid Arts .SNG files.
      for (int ii = 0; ii < TRACK_COUNT; ii++)
      {
        UInt32 fixedFlag = _byteProvider.ReadByte(DEF_TRACK_FIXED_FLAGS_OFFSET + ii) & ~TRACK_FIXED;
        _byteProvider.WriteByte(DEF_TRACK_FIXED_FLAGS_OFFSET + ii, (byte)fixedFlag);
      }

      // set color of all track names yellow
      foreach (DataGridViewRow r in dataGridView1.Rows)
      {
        UInt32 trackFileOffset;
        try { trackFileOffset = Convert.ToUInt32(dataGridView1.Rows[r.Index].Cells[3].Value.ToString(), 16); }
        catch { trackFileOffset = 0; }

        // only add non-zero entries!
        if (trackFileOffset != 0)
          r.Cells[0].Style.BackColor = Color.Yellow;
      }

      _fixingTracks = false;
    }
    #endregion

    #region Misc Routines

    private bool IsTrackFixed(int index)
    {
      try
      {
        UInt32 fixedFlag = _byteProvider.ReadByte(DEF_TRACK_FIXED_FLAGS_OFFSET + index) & TRACK_FIXED;

        if (fixedFlag == 0)
          return false;
      }
      catch
      {
        return false;
      }

      return true;
    }

    // check for "new" section at this location 00 33
    //        TracksFileOffset = 0x1b4a; // 0x1b4a might point to a section 00 33 00 NN
    //            if (_byteProvider.Length >= TracksFileOffset + 8)
    //            {
    //                UInt16 marker = GetShort(TracksFileOffset);
    //                if (marker == POSSIBLE_SECTION_BEFORE_TRACKS_MAGIC_NUMBER)
    //                {
    //                    UInt16 length = GetShort(TracksFileOffset + 2);
    //                    if (_byteProvider.Length >= TracksFileOffset + 8 + length)
    //                        TracksFileOffset += length;
    //                }
    //}
    private bool IsGoodMagicNumber()
    {
      UInt32 addr = TracksFileOffset;

      if (_byteProvider.Length < addr + 2 + 4)
      {
        MessageBox.Show("File is too short to be a Hybrid Arts SyncTrack midi .SNG file!");
        return false;
      }

      // Read the magic #
      UInt16 magic = GetShort(addr);

      if (magic == TRACKS_START_MAGIC_NUMBER)
        return true;

      if (checkBoxUseOldFormat.Checked)
      {
        while (addr >= 0)
        {
          if (_byteProvider.ReadByte(addr) == TRACKS_START_MAGIC_NUMBER)
          {
            addr--;
            if (addr >= 0 && _byteProvider.ReadByte(addr) == 0)
            {
              string s = "WRONG LOCATION for magic number (0x" + String.Format("{0:X4}", TRACKS_START_MAGIC_NUMBER) + ")!!!!!\n\n" +
                  "It's at offset: " + String.Format("{0:X8}\n", addr) +
                  "Missing " + (TracksFileOffset - addr) + " bytes! (0D or 1A hex)\n\n" +
                  "1: Might be missing one or more midi-chan 0D in 60 byte-block at 0624...\n" +
                  "2: If 11 not at 06A9 may need to insert 0D at 06A7...\n" +
                  "3: If 00 4E 1F 5F is not at 0A96 may need to insert 1A at 09BC (09B9: 30 00 XX 1A 00...)\n\n" +
                  "21 should be at 084F, 18 at 0853, 00 00 00 at 0872, ff ff ff ff at 0978,\n" +
                  "30 at 09B9, 00 4E 1F 5F at 0A96 and 60 should be at 0C61.";

              Clipboard.SetText(s); // handy!
              MessageBox.Show(s);
              break;
            }
          }
          addr--;
        }

        if (addr < 0)
          MessageBox.Show("NO MAGIC NUMBER FOUND!!!!! Wrong file?\n" +
              "Magic Number: " + String.Format("{0:X4}\n", TRACKS_START_MAGIC_NUMBER) +
              "Should be at: " + String.Format("{0:X8}", addr));
      }
      else // file has been converted to newer format by EditTrack... might have a section 00 33 before track section 00 90
      {
        if (magic == POSSIBLE_SECTION_BEFORE_TRACKS_MAGIC_NUMBER)
        {
          if (_byteProvider.Length >= addr + 8)
          {
            UInt16 length = GetShort(addr + 2);
            if (_byteProvider.Length >= addr + 8 + length)
            {
              addr += (UInt32)(length + 4);
              magic = GetShort(addr);
              if (magic == TRACKS_START_MAGIC_NUMBER)
              {
                TracksFileOffset = addr;
                return true;
              }
            }
          }
        }
      }

      return false;
    }

    private void SetStatusGreen(int row, UInt32 time)
    {
      // Set the "fixed flag"
      if (row < TRACK_COUNT)
      {
        UInt32 fixedFlag = _byteProvider.ReadByte(DEF_TRACK_FIXED_FLAGS_OFFSET + row);

        if ((fixedFlag & TRACK_FIXED) == 0)
          _byteProvider.WriteByte(DEF_TRACK_FIXED_FLAGS_OFFSET + row, (byte)(fixedFlag | TRACK_FIXED));

        // set names field green if track is ok
        dataGridView1.Rows[row].Cells[0].Style.BackColor = Color.LightGreen;
        // Add Time Ticks column data (4)
        dataGridView1.Rows[row].Cells[4].Value = String.Format("{0:d}", time);
        dataGridView1.Update();
      }
    }

    private void SetStatusRedOrYellow(int row, bool bRed)
    {
      // Set the "fixed flag"
      if (row < TRACK_COUNT)
      {
        UInt32 fixedFlag = _byteProvider.ReadByte(DEF_TRACK_FIXED_FLAGS_OFFSET + row);

        if ((fixedFlag & TRACK_FIXED) != 0)
          _byteProvider.WriteByte(DEF_TRACK_FIXED_FLAGS_OFFSET + row, (byte)(fixedFlag & ~TRACK_FIXED));

        // set names field green if track is ok
        if (bRed)
          dataGridView1.Rows[row].Cells[0].Style.BackColor = Color.Red;
        else
          dataGridView1.Rows[row].Cells[0].Style.BackColor = Color.Yellow;

        // Clear time field
        dataGridView1.Rows[row].Cells[4].Value = "";
        dataGridView1.Update();
      }
    }

    // This is my (Scott Swift) recursive solution to making a "super counter" out of
    // the combined two-bit indexes into the _lengthChanges List or the three-bit
    // indexes into the _addressChanges List... if there is one length that had a 1a
    // byte inserted, four possible combinations (of 1a or 0d) are counted out... if two lengths were 
    // wrong, we get sixteen possible combinations, etc. For addresses the number of
    // possibilities runs up fast. 
    private bool IncCounters(int n, int maxCounters, int maxCountPerCounter, ref int[] counters)
    {
      if (++counters[n] > maxCountPerCounter)
      {
        counters[n] = 0;
        if (n + 1 < maxCounters)
          return IncCounters(n + 1, maxCounters, maxCountPerCounter, ref counters);
        return false;
      }
      return true;
    }

    private UInt16 GetShort(long addr)
    {
      UInt16 acc = 0;
      for (int ii = 0; ii < 2; ii++)
      {
        acc <<= 8;
        acc |= _byteProvider.ReadByte(addr++);
      }
      return acc;
    }

    private UInt32 GetWord()
    {
      UInt32 acc = 0;
      for (int ii = 0; ii < 4; ii++)
      {
        acc <<= 8;
        acc |= _byteProvider.ReadByte(_index++);
      }
      return acc;
    }

    private UInt32 GetWord(long addr)
    {
      UInt32 acc = 0;
      for (int ii = 0; ii < 4; ii++)
      {
        acc <<= 8;
        acc |= _byteProvider.ReadByte(addr++);
      }
      return acc;
    }

    private void PutShort(long addr, UInt16 value)
    {
      for (int ii = 0; ii < 2; ii++)
      {
        _byteProvider.WriteByte(addr + 1 - ii, (byte)(value & 0x00ff));
        value >>= 8;
      }
    }

    private void PutWord(long addr, UInt32 value)
    {
      for (int ii = 0; ii < 4; ii++)
      {
        _byteProvider.WriteByte(addr + 3 - ii, (byte)(value & 0x000000ff));
        value >>= 8;
      }
    }

    private string GetString(int length)
    {
      string s = String.Empty;
      for (int ii = 0; ii < length; ii++)
        s += (char)_byteProvider.ReadByte(_index++);
      return s;
    }

    private void PutString(long addr, string sIn)
    {
      int len = sIn.Length;

      for (int ii = 0; ii < len; ii++)
        _byteProvider.WriteByte(addr + ii, (byte)sIn[ii]);
    }
    #endregion

    #region Track Sort Compare Delagate

    // Sort by-Value delegate for KeyValue pair used to sort track file-offsets, lowest offset first...
    static int TrackSortCompareDelegate(KeyValuePair<int, UInt32> a, KeyValuePair<int, UInt32> b)
    {
      return a.Value.CompareTo(b.Value);
    }
    #endregion
  }

  #region Old Algorithms
  // Old algorithm...

  //DialogResult result1 = MessageBox.Show("Timing prescan fail at event " + (failEventIndex + 1).ToString() +
  //    "\nPress Cancel to mark track and move on to next track...",
  //    "Track " + (trackIndex + 1).ToString() + ": " + dataGridView1.Rows[trackIndex].Cells[0].Value.ToString(),
  //    MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

  //if (result1 == DialogResult.Cancel)
  //{
  //    // Set the track-name label to show the index we failed on... (for easy reference if you want to select and Examine the track!)
  //    string s = dataGridView1.Rows[trackIndex].Cells[0].Value.ToString();
  //    s = s.Insert(0, "(" + (failEventIndex + 1).ToString() + ")");
  //    dataGridView1.Rows[trackIndex].Cells[0].Value = s;
  //    dataGridView1.Rows[trackIndex].Cells[0].Style.BackColor = Color.Yellow; // fail
  //    return true; // abort
  //}

  //// otherwise run the alternative tests...

  //UInt32 cumulativeTime = 0;
  //int orangeEventCount = 0;
  //byte timeStamp;
  //int maxState = -1;

  //// this loop finds 1a/0d events to process
  //while (addr < maxAddr)
  //{
  //    timeStamp = _byteProvider.ReadByte(addr);

  //    if (timeStamp == 0x1a || timeStamp == 0x0d)
  //    {
  //        UInt32 tempTime1, tempTime2;
  //        int onBeatCount1 = 0;
  //        int onBeatCount2 = 0;
  //        int failCount = 0;
  //        int state = -1;
  //        int variation = 0;
  //        UInt32 divisor = 96; // 96 = 1 beat 96/2 = 1/2 beat 96/3 = 1/3 beat
  //        long tempAddr;

  //        // this loop changes the algorithm "slop" on what's considered to be an "on the beat" event
  //        // if we can't make a determination using the present settings... and tries again.
  //        for (;;)
  //        {
  //            tempAddr = addr;
  //            tempTime1 = cumulativeTime;
  //            onBeatCount1 = ReadFourNoteOn(ref tempAddr, maxAddr, ref tempTime1, variation, divisor, 0x1a);

  //            tempAddr = addr;
  //            tempTime2 = cumulativeTime;
  //            onBeatCount2 = ReadFourNoteOn(ref tempAddr, maxAddr, ref tempTime2, variation, divisor, 0x0d);

  //            // prints this 7 times!!!!!!!!!!!!!
  //            //if (trackIndex == 2 && ((int)(addr - fileOffset) / 4) + 1 == 379)
  //            //{
  //            //    // always 1A never 0D!!!!!!!!!!!!!!!!!!!!! 0 0
  //            //    MessageBox.Show("event: " + String.Format("{0:X8}", GetWord(addr - 3)) + ", obC1: " + onBeatCount1 + ", obC2: " + onBeatCount2 + ", ct+0d: " + String.Format("{0:f3}", (double)(cumulativeTime + 0x0d) / (double)96)); // 179.135
  //            //}

  //            // if counts are both 0 or the same, we have to try some other settings... (one bigger than the other
  //            // is checked later...)
  //            if (onBeatCount1 == onBeatCount2)
  //            {

  //                // Monitor number of times we "just don't know" (i.e. when both counts are the same) - in these cases, we want to allow increasing
  //                // slippage and try 1/3 beat resolution instead of 1/2... Most tracks are quantizes exactly to the 1/2 or 1/3 beat
  //                // or "humanized" a random # ticks around the exact "on-bbeat" tick... a completely randomly track (like a person
  //                // who may have played a piano solo with the metroknome-tick silent) - we really can do nothing "automated" to fix that!

  //                switch (++state)
  //                {
  //                    case 0:
  //                        divisor = 48;
  //                        variation = 0;
  //                        continue;
  //                    case 1:
  //                        divisor = 32;
  //                        variation = 0;
  //                        continue;
  //                    case 2:
  //                        divisor = 24;
  //                        variation = 0;
  //                        continue;
  //                    case 3:
  //                        divisor = 16;
  //                        variation = 0;
  //                        continue;
  //                    case 4:
  //                        divisor = 96;
  //                        variation = 8;
  //                        continue;
  //                    case 5:
  //                        divisor = 48;
  //                        variation = 6;
  //                        continue;
  //                    case 6:
  //                        divisor = 32;
  //                        variation = 5;
  //                        continue;
  //                    case 7:
  //                        divisor = 24;
  //                        variation = 4;
  //                        continue;
  //                    case 8:
  //                        divisor = 16;
  //                        variation = 3;
  //                        continue;
  //                    default:
  //                        failCount++;
  //                        failEventIndex = ((int)(addr - fileOffset) / 4);
  //                        break;
  //                }
  //            }

  //            break; // very important! :-)
  //        } // end for(;;)

  //        // keep track of the highest non-fail state we reach as we walk through the track...
  //        if (state > maxState)
  //            maxState = state;

  //        // On fail, bring the track up so we can hand-edit it
  //        if (failCount > 0 && failEventIndex >= 0)
  //        {
  //            // Set the track-name label to show the index we failed on... (for easy reference if you want to select and Examine the track!)
  //            string s = dataGridView1.Rows[trackIndex].Cells[0].Value.ToString();
  //            s = s.Insert(0, "(" + (failEventIndex + 1).ToString() + ")");
  //            dataGridView1.Rows[trackIndex].Cells[0].Value = s;
  //            dataGridView1.Rows[trackIndex].Cells[0].Style.BackColor = Color.Yellow; // fail

  //            //    using (FormFixTracks formFixTracks = new FormFixTracks(this))
  //            //    {
  //            //        string trackName = dataGridView1.Rows[trackIndex].Cells[0].Value.ToString();

  //            //        if (trackLength == 0 || trackAddr == 0 || fileOffset == 0 || TrackFilters == null || trackIndex >= TrackFilters.Count)
  //            //        {
  //            //            MessageBox.Show("Bad parameters for track: " + (trackIndex + 1).ToString());
  //            //            return false;
  //            //        }

  //            //        formFixTracks.LoadTrack(trackName, trackLength, trackAddr, fileOffset, (UInt32)trackIndex, TrackFilters[trackIndex]);

  //            //        formFixTracks.InitialIndex = failEventIndex;

  //            //        DialogResult result = formFixTracks.ShowDialog(); // show modally

  //            //        // update TrackFilters string-collection (user can change it on the Form)
  //            //        TrackFilters[trackIndex] = formFixTracks.EventFilters;

  //            //        // if Ok was clicked and formFixTracks.LastEditedValue has a timestamp of 1a or 0d - try again, otherwise abort
  //            //        if (result == DialogResult.OK)
  //            //        {
  //            //            // better here just to do the regular clipboard paste stuff from ExamineTrack??????????? let user edit anything
  //            //            // then fully re-process this track...

  //            //            //if (formFixTracks.LastEditedValue != 0)
  //            //            //{
  //            //            //    timeStamp = (byte)(formFixTracks.LastEditedValue & 0xff);
  //            //            //    if (timeStamp == 0x1a || timeStamp == 0x0d)
  //            //            //    {
  //            //            //        _byteProvider.WriteByte(addr, timeStamp);
  //            //            //        cumulativeTime += timeStamp;
  //            //            //    }
  //            //            //}
  //            //            //addr += 4;
  //            //            continue;
  //            //        }

  //            //        DialogResult result1 = MessageBox.Show("Press OK to process next track...",
  //            //            "Track " + (trackIndex + 1).ToString() + ": " + trackName,
  //            //            MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);

  //            //        if (result1 == DialogResult.Cancel)
  //            //            return false; // abort
  //            //    }

  //            return true; // move on to next track
  //        }

  //        // if we had more or the same "on-beat" events with the original timeStamp, go back to it - otherwise
  //        // keep the replacement...
  //        if (onBeatCount1 >= onBeatCount2)
  //        {
  //            _byteProvider.WriteByte(addr, 0x1a);
  //            cumulativeTime = tempTime1;
  //        }
  //        else
  //        {
  //            _byteProvider.WriteByte(addr, 0x0d);
  //            cumulativeTime = tempTime2;
  //        }

  //        orangeEventCount++;
  //        addr = tempAddr; // resume following this four note-on event-set (or possibly less than four if 1a/0d!)
  //    }
  //    else
  //    {
  //        cumulativeTime += timeStamp;
  //        addr += 4;
  //    }
  //}

  //// stop processing track after first 4-note-on-event-block fail - no point in going forward until and unless it's fixed!

  ////            if (failCount > 0)
  ////            {
  ////                if (MessageBox.Show("Had " + failCount + " events out of " + timestampsCheckedCount +
  ////                    " where we could not determine if 1A or 0D fits as the timestamp!", "Track " + (trackIndex + 1).ToString(),
  ////                    MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1) == DialogResult.Cancel)
  ////                    return false;
  ////            }

  //switch (maxState)
  //{
  //    case -1:
  //        dataGridView1.Rows[trackIndex].Cells[0].Style.BackColor = Color.LightGreen;
  //        break;
  //    case 0 - 4:
  //        dataGridView1.Rows[trackIndex].Cells[0].Style.BackColor = Color.LightPink;
  //        break;
  //    case 5 - 8:
  //        dataGridView1.Rows[trackIndex].Cells[0].Style.BackColor = Color.Pink;
  //        break;
  //    default:
  //        dataGridView1.Rows[trackIndex].Cells[0].Style.BackColor = Color.Yellow;
  //        break;
  //}

  //return true;
  //}

  // Read ahead 4 note-on time-stamps or up to the next 1A/0D time-stamp or to end-of-track
  // Return the number of events "on the beat" within "variation" (in ticks)
  // We also need to return cumulativeTime and addr as reference vars...
  //
  // variation is the factor of "slop" to allow in an event's total-time in calling it "on the beat"
  // variation can be 0 if you want onBeatCounter to count beats exactly on the 1/2 or 1/3 beat, or > 0
  // to allow timing to deviate from the 1/2 beat or 1/3 beat by X ticks
  // Set divisor to 48 to test to 1/2 beat resolution or 32 for 1/3 beat resolution
  //
  // Only call this with addr pointed at a timestamp of 1a or 0d!
  // Exits with addr pointed to the next event we want to read and check for 1a/0d timestamp...
  //private int ReadFourNoteOn(ref long addr, long maxAddr, ref UInt32 cumulativeTime, int variation, UInt32 divisor, byte timeStamp)
  //{
  //    int onBeatCounter = 0;
  //    UInt32 remainder;
  //    int noteOnCounter = 0;
  //    bool bFirst = true;

  //    // loop for the next 4 note-on events (including this one with 1a/0d) - return onBeatCounter = 0-4
  //    // quit early in another event with 1a/0d is encountered or if we reach the end-of-track
  //    for (; ;)
  //    {
  //        if (addr >= maxAddr)
  //            return onBeatCounter;

  //        // don't start on the entry event (the event that has the 1a/0d) - we add it below!
  //        // our first event is always a 1A or 0D which we want to add to the cumulative time,
  //        // but we do not want to add the next timestamp that's 1A or 0D - just quit if it's before
  //        // four note-on events...
  //        if (bFirst)
  //            bFirst = false; // on first event we pass in the timeStamp (1a or 0d)
  //        else // subsequent events we read the timeStamp associated with this event
  //        {
  //            timeStamp = _byteProvider.ReadByte(addr);

  //            // if two events in a row with 1a/0d or before a note-on event, we will need to manually edit the track!
  //            if (timeStamp == 0x1a || timeStamp == 0x0d)
  //                return onBeatCounter;
  //        }

  //        cumulativeTime += timeStamp; // add successive time-stamps

  //        // check for a note on event 0x9X
  //        if ((_byteProvider.ReadByte(addr - 1) & 0xf0) == 0x90)
  //        {
  //            // check for velocity > 0 (note-on)
  //            if (_byteProvider.ReadByte(addr - 3) != 0)
  //            {
  //                // Check for half and third beat (96 ticks per 1/4 beat) exact resolution
  //                remainder = (cumulativeTime % divisor);

  //                // adjust to get negative deviation from "on-beat"
  //                if (remainder > divisor / 2)
  //                    remainder = divisor - remainder;

  //                // check absolute +/- deviation against allowed "variation"
  //                if (remainder <= variation)
  //                    onBeatCounter++;

  //                // count note-on events... exit loop on 4
  //                if (++noteOnCounter >= 4)
  //                {
  //                    addr += 4;
  //                    break;
  //                }
  //            }
  //        }

  //        addr += 4;
  //    } // end for (;;)

  //    return onBeatCounter;
  //}
  //private void buttonFixTiming_Click(object sender, EventArgs e)
  //{
  //    if (TotalTracksInSong != _goodTrackCount)
  //    {
  //        MessageBox.Show("All tracks must be repaired via Fix Tracks before this step!");
  //        return;
  //    }

  //    for (int ii = 0; ii < TRACK_COUNT; ii++)
  //        if (!FixTrackTiming(ii))
  //            break;
  //}

  //// Track timing is fixed by replacing missing 1A/0D timestamps with 1A - here we analyze every
  //// track (if all tracks have been repaired) for timing errors and replace some of the 1A bytes
  //// with 0D bytes. The premise is that the track cumulative-time frequently falls on the beat
  //// or on a 1/2 beat or 1/3 beat, so if that's the pattern before a 1A timestamp but not after,
  //// then with 1D replacing the 1A the pattern resumes, we keep the 1D...

  //// If cumulative-time % 96 (96 ticks per quarter-beat) is 0, 48 or 32, we count it as "on the beat"
  //// We check a 10-event window (after and including the event with the 1A) and count events "on the beat".
  //// Replace the byte with 0D, recompute cumulative-times and if there are more events "on the beat",
  //// keep the 0D else put back the 1A and go to the next 1A or 0D... repeat until "length" events checked
  //// (except the last one)... then do the next track.
  //private bool FixTrackTiming(int trackIndex)
  //{
  //    if (_fixingTracks || !GoodMagicNumber())
  //        return false;

  //    UInt32 tableOffset = (UInt32)trackIndex * 4;
  //    int trackLength = (int)GetWord(TableLengthsOffset + tableOffset);
  //    UInt32 trackAddr = GetWord(TableAddressesOffset + tableOffset);

  //    if (trackLength < 4 || trackAddr == 0)
  //        return true; // not an error but an empty track

  //    UInt32 fileOffset = trackAddr - TracksBaseAddress + TracksFileOffset + 2; // (+ 2 is for 0090 magic number!)

  //    if (fileOffset + trackLength > _hexBox.ByteProvider.Length)
  //        return false;

  //    long maxAddr = fileOffset + trackLength - 4; // don't include the end-of-track marker!

  //    // prescan track - if timing good, move on to next track
  //    int resumeAtEventIndex = 0;

  //    int delta = 6; // # ticks note-on timing can vary from 8-note-on-event average befor being flages as "off-beat"
  //    int divisor = 96 / 4; // resolution used for timing analysis - there are 96 ticks per beat so 96/4 is a quarter-beat

  //    for (;;)
  //    {
  //        long addr = fileOffset + 3; // point to first event's timestamp
  //        long resumeAtAddr = addr + (resumeAtEventIndex * 4);

  //        // on return, addr points to a miss-timed note-on event
  //        if (PrescanTrackTiming(trackIndex, ref addr, maxAddr, resumeAtAddr, delta, divisor))
  //        {
  //            dataGridView1.Rows[trackIndex].Cells[0].Style.BackColor = Color.LawnGreen; // good
  //            return true;
  //        }

  //        int failEventIndex = ((int)(addr - fileOffset) / 4);

  //        // here is where resumeAtEventIndex is progressively set higher by the user by clicking to the left of a particuler
  //        // event in the track in FormFixTracks before clicking OK.
  //        int lastEditedIndex = ExamineTrack(trackIndex, failEventIndex, ref resumeAtEventIndex, false);

  //        if (lastEditedIndex < 0) // error or cancel?
  //        {
  //            DialogResult result1 = MessageBox.Show("Press OK to continue to next track, Cancel to quit...",
  //                "Track " + (trackIndex + 1).ToString() + ": " + dataGridView1.Rows[trackIndex].Cells[0].Value.ToString(),
  //                MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

  //            if (result1 == DialogResult.Cancel)
  //                return false;

  //            return true;
  //        }

  //        // if OK pressed in FormFixTracks, then the hand-edited data has replaced the old track-data - go
  //        // check the timing again until all good... or Cancel
  //    }
  //}
  //
  // analyze timing - returns ref addr pointed to first "bad-looking" timestamp,
  // returns bool true if entire track is ok
  // delta is max permissable deviation from average off-beat variation (in ticks) (6 is typical)
  // divisor is 96 = 1 beat, 96/2 = 1/2,  beat 96/3 = 1/3 beat (96/2 is typical)
  // resumeAtAddr suppresses calling an event's timing "bad" until that address is reached (allows the user to
  // progressively set higher addresses as the track is manually examined and repaired through repeated calls to ExamineTrack())
  //private bool PrescanTrackTiming(int trackIndex, ref long addr, long maxAddr, long resumeAtAddr, int delta, int divisor)
  //{
  //    int cumulativeTime = 0;
  //    int averageVariation = 0;
  //    int variation = 0;
  //    byte timeStamp;
  //    bool bFirst = true;

  //    // this loop finds 1a/0d events to process
  //    while (addr < maxAddr)
  //    {
  //        timeStamp = _byteProvider.ReadByte(addr);

  //        cumulativeTime += timeStamp;

  //        //if (timeStamp == 0x1a || timeStamp == 0x0d)
  //        //{
  //        //}

  //        // check for a note on event 0x9X
  //        if ((_byteProvider.ReadByte(addr - 1) & 0xf0) == 0x90)
  //        {
  //            // check for velocity > 0 (note-on)
  //            if (_byteProvider.ReadByte(addr - 3) != 0)
  //            {
  //                // Compute variation away from an exact half-beat (48 ticks)
  //                variation = (cumulativeTime % divisor);

  //                // adjust to get negative deviation from "on-beat"
  //                if (variation > divisor / 2)
  //                    variation = divisor - variation;

  //                // first note-on in the track quick-primes our average!
  //                if (bFirst)
  //                {
  //                    averageVariation = variation;
  //                    bFirst = false;
  //                }
  //                else
  //                {
  //                    // 8-note variation filter
  //                    averageVariation = averageVariation - (averageVariation >> 3) + (variation >> 3);

  //                    // check this note's off-beat variation against average
  //                    if (addr >= resumeAtAddr && variation > averageVariation + delta)
  //                        return false; // return with ref addr at first detected off-beat note-on event...
  //                }
  //            }
  //        }

  //        addr += 4;
  //    }

  //    return true;
  //}
  //
  //// returns # bytes copied, -1 if error
  //public int CopyTrackToClipboard(byte [] buffer)
  //{
  //    if (buffer == null)
  //        return -1;

  //    int trackLength = buffer.Length;

  //    DataObject da = new DataObject();

  //    // set string buffer clipbard data
  //    string sBuffer = System.Text.Encoding.ASCII.GetString(buffer, 0, trackLength);

  //    da.SetData(typeof(string), sBuffer);

  //    //set memorystream (BinaryData) clipboard data
  //    System.IO.MemoryStream ms = new System.IO.MemoryStream(buffer, 0, trackLength, false, true);
  //    da.SetData("BinaryData", ms);

  //    Clipboard.SetDataObject(da, true);

  //    return trackLength;
  //}

  #endregion
}

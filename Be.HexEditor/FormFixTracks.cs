using System;
using System.Collections.Generic;
using System.Drawing;
using Be.Windows.Forms;
using System.Windows.Forms;

namespace Be.HexEditor
{
    public partial class FormFixTracks : Form
    {
        #region GlobalVars

        internal const int MAX_NOTE_COLORS = 12;
        private const UInt32 MAX_BYTE_LIMIT = 0x00020000;

        //private const UInt32 TEMPO = 120; // beats/minute
        private const int TICKS_PER_BEAT = 96;
        private const string INS_CODE = "0D";

        // these are for the Fix Timing algorithm
        public const int MIN_DIVISOR = 24;
        public const int MAX_VARIATION = 6;

        public const int INITIAL_DIVISOR = 24;
        public const int INITIAL_VARIATION = 6;

        private IByteProvider _byteProvider;
        private HexBox _hexBox;
        private FormFixHybridArts _parentForm;

        private int _divisor = 0;
        public int Divisor { get { return _divisor; } } // property

        private int _variation = 0;
        public int Variation { get { return _variation; } } // property

        // length of track specified in the length-table in the header of the .SNG file.
        // This IS the proper original track-length we are aiming to re-gain. If the track itself is corrupt,
        // and we hand-alter it - we have to modify this and write it back to the table when this form exits.
        private int _trackLength = 0;
        public int TrackLength { get { return _trackLength; } } // property

        // byte-sum from _trackFileOffset up through to the 4-byte end-of-track marker
        // (this DOES NOT change if events are added or removed!)
        // (we use this in ReplaceTrack in the parent form to cut the old track data)
        private int _originalBrokenTrackLength = 0;
        public int OriginalBrokenTrackLength { get { return _originalBrokenTrackLength; } } // property

        // byte-sum from _trackFileOffset up through to the 4-byte end-of-track marker
        // (this WILL change if events are added or removed!)
        private int _brokenTrackLength = 0;
        public int BrokenTrackLength { get { return _brokenTrackLength; } } // property

        // _missingByteCounter = _trackLength - _brokenTrackLength
        private int _missingByteCount = 0; // normally positive but can go negative if bytes are removed from a track
        public int MissingByteCount { get { return _missingByteCount; } } // property

        // set this property to auto-select a particular event when the dialog is shown
        public int InitialSelectedEventIndex { get; set; } = -1;

        public int ResumeAtEventRow { get; set; } = 0;

        public UInt32 LastEditedValue { get; set; } = 0;
        public int LastEditedIndex { get; set; } = -1;

        public bool InitialAutoInsertMode = true;

        private UInt32 _totalTimeTicks = 0;
        public UInt32 TotalTimeTicks { get { return _totalTimeTicks; } } // property

        private int _initialBadRow = -1; // 0-based row-index
        private int _trackIndex = 0;
        private bool _endOfTrackShiftFlag = false;

        private bool _bEventsFixed = false;
        private bool _bNotesFixed = false;
        private bool _bTimingFixed = false;

        private UInt32 _index = 0;
        private UInt32 _trackFileOffset = 0;
        private UInt32 _trackAddr = 0;
        private string _trackName = String.Empty;

        // global list of individual allowed-event code strings obtained by:
        // _filterTokens = EventFilters.Split(new char[] { ' ', ',' });
        private string[] _filterTokens = null;

        // These are event filters that represent "allowed" midi-event codes in the
        // track. 9X is note on/off in Sync-Track and 1X is a "time-delay" 40 is tempo-track.
        // Use space to separate filters... put an X id the secont nibble is "don't care"
        // (midi-channel in 9X for instance, can be any hex digit and it's ok)
        // The filter for each track is saved in Settings for the main hex-editor project
        // as a StringCollection called "Filters" - this means filters are saved only for the
        // current song you are fixing - fix that file completely before starting the next
        public string EventFilters { get; set; } = "9X 8X 10";

        private NoteColors noteColors = new NoteColors();
        #endregion

        #region FormHooks

        public FormFixTracks(FormFixHybridArts parentForm)
        {
            InitializeComponent();

            _parentForm = parentForm;
            _hexBox = _parentForm._hexBox;
            _byteProvider = _hexBox.ByteProvider;
        }

        private void FormFixTracks_Shown(object sender, EventArgs e)
        {
            checkBoxAutoInsert0D.Checked = InitialAutoInsertMode;

            if (InitialSelectedEventIndex >= 0)
                Hilight(InitialSelectedEventIndex);
            else
                AnalyzeTrack(); // check for bad events
        }

        private void buttonReapplyFilters_Click(object sender, EventArgs e)
        {
            AnalyzeTrack(); // check for bad events
        }

        /// <summary>
        /// Called when you press enter after clicking a cell twice to edit it
        /// </summary>
        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (sender is DataGridView)
            {
                DataGridView dgv = sender as DataGridView;

                if (dgv != null)
                {
                    int ret = ShiftDataAfterEdit(dgv, e.RowIndex);

                    if (ret != -1)
                    {
                        MessageBox.Show("dataGridView1_CellEndEdit(): Bad data in cell at row: " + ret);
                        return;
                    }

                    // don't do this if manual edit mode - very annoying!
                    if (checkBoxAutoInsert0D.Checked)
                        AnalyzeTrack(e.RowIndex); // check for bad events

                    RefreshColorsAndTimes(); // nice to have the times all re-adjust :-)
                }
            }
        }

        // this button lets us set a new track-length after inserting, deleting rows - it should be used only if the
        // original track was corrupt and is being truncated - make sure you add a 00 00 00 00 end-of-track marker!
        private void buttonSyncLengthToList_Click(object sender, EventArgs e)
        {
            _trackLength = dataGridView1.Rows.Count * 4;
            textBoxTrackLength.Text = _trackLength.ToString();
            _brokenTrackLength = _trackLength;
            SetMissingByteCount(0);
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            // user manually edited the tracklength - we force that length without deleting any events
            // 
            if (textBoxTrackLength.Modified)
            {
                int tempLen = Convert.ToInt32(textBoxTrackLength.Text);
                if (tempLen < _trackLength)
                {
                    _trackLength = tempLen;
                    _brokenTrackLength = _trackLength;
                    _missingByteCount = 0;
                    MessageBox.Show("Track truncated to " + _trackLength + " bytes!");
                }
            }

            if (_brokenTrackLength == 0 || _trackFileOffset == 0)
                return;

            if (_missingByteCount == 0)
            {
                // put our data from column 0 on the clipboard
                if (CopyTrackToClipboard() != _trackLength)
                    MessageBox.Show("Error in TrackCopy()!");

                // this is set by clicking to the left of an event and tells the timing analyzer to ignore
                // out of time events prior to this event...
                try { ResumeAtEventRow = Convert.ToInt32(textBoxResumeAt.Text) - 1; }
                catch { ResumeAtEventRow = 0; textBoxResumeAt.Text = (1).ToString(); }
            }
            else
            {
                MessageBox.Show("Track is not fixed! Missing Bytes: " + _missingByteCount);
                DialogResult = DialogResult.Cancel;
            }
        }

        // note: the gridview does NOT like having rows removed and I tried various things to get it working...
        private void buttonDelete_Click(object sender, EventArgs e)
        {
            // user can select and delete more than one row!

            if (dataGridView1.SelectedRows.Count == 0) return;

            // disable event handlers (if enabled)
            this.dataGridView1.CellEndEdit -= dataGridView1_CellEndEdit;

            try
            {
                int selectedCount = dataGridView1.SelectedRows.Count;
                while (selectedCount > 0)
                {
                    dataGridView1.Rows.RemoveAt(dataGridView1.SelectedRows[0].Index);
                    _trackLength -= 4;
                    _brokenTrackLength -= 4;
                    selectedCount--;
                }

                textBoxTrackLength.Text = _trackLength.ToString();
                SetMissingByteCount(_trackLength - _brokenTrackLength);
            }
            finally
            {
                // re-enable event handlers
                this.dataGridView1.CellEndEdit += dataGridView1_CellEndEdit;
            }

            // not working
            //            RefreshRowNumbers(0);
        }

        private void buttonInsert_Click(object sender, EventArgs e)
        {
            // user added one row (multiselect is false)
            if (dataGridView1.Rows.Count == 0 || dataGridView1.SelectedRows.Count != 1) return;
            dataGridView1.Rows.Insert(dataGridView1.SelectedRows[0].Index, "AAAA9F00");
            _trackLength += 4;
            _brokenTrackLength += 4;
            textBoxTrackLength.Text = _trackLength.ToString();
            SetMissingByteCount(_trackLength - _brokenTrackLength);

            dataGridView1.Refresh();
            dataGridView1.Parent.Refresh();
            // not working
            //            RefreshRowNumbers(0);
            //            if (_missingByteCount == 0)
            //                RefreshColorsAndTimes(); // "Track is OK!";
        }

        private void buttonReset_Click(object sender, EventArgs e)
        {
            _bEventsFixed = _bNotesFixed = _bTimingFixed = false;

            ResumeAtEventRow = 0;
            textBoxResumeAt.Text = (ResumeAtEventRow + 1).ToString();
        }

        private void textBoxResumeAt_TextChanged(object sender, EventArgs e)
        {
            int rowStartIndex;
            try { rowStartIndex = Convert.ToInt32(textBoxResumeAt.Text) - 1; }
            catch { rowStartIndex = 1; textBoxResumeAt.Text = (1).ToString(); }

            ResumeAtEventRow = rowStartIndex;
        }

        private void textBoxFixTimingDivisor_TextChanged(object sender, EventArgs e)
        {
            try { _divisor = Convert.ToInt32(textBoxFixTimingDivisor.Text); }
            catch { _divisor = INITIAL_DIVISOR; }
        }

        private void textBoxFixTimingVariation_TextChanged(object sender, EventArgs e)
        {
            try { _variation = Convert.ToInt32(textBoxFixTimingVariation.Text); }
            catch { _variation = INITIAL_VARIATION; }
        }

        private void textBoxFilters_TextChanged(object sender, EventArgs e)
        {
            EventFilters = textBoxFilters.Text;

            try { _filterTokens = EventFilters.Split(new char[] { ' ', ',' }); }
            catch { _filterTokens = null; }
        }

        #endregion

        #region LoadTrack

        // Call this before the dialog is shown...
        public int LoadTrack(string trackName, int trackLength, UInt32 trackAddr, UInt32 trackFileOffset, int trackIndex, string eventFilters)
        {
            // set globals
            _trackName = trackName;
            _trackLength = trackLength;
            _trackAddr = trackAddr;
            _trackFileOffset = trackFileOffset;
            _trackIndex = trackIndex;
            EventFilters = eventFilters;

            return LoadTrack();
        }

        // HOW WE KNOW HOW MANY BYTES ARE MISSING IN THE TRACK (1A or 0D are the only possible bytes)... We have the original track
        // length from the repaired "lengths" table... and we can count the actual # byts that are in the track using the end-of-track
        // "00 00 00 00" marker...
        public int LoadTrack()
        {
            // disable event handlers (if enabled)
            this.dataGridView1.CellEndEdit -= dataGridView1_CellEndEdit;

            try
            {
                // clear
                dataGridView1.Rows.Clear();

                _totalTimeTicks = 0;
                _missingByteCount = 0;
                _index = 0;
                _brokenTrackLength = 0;
                _originalBrokenTrackLength = 0;
                _initialBadRow = -1;
                _endOfTrackShiftFlag = false;
                _bEventsFixed = false;
                _bNotesFixed = false;
                _bTimingFixed = false;

                InitialAutoInsertMode = true;
                LastEditedIndex = -1;
                LastEditedValue = 0;
                InitialSelectedEventIndex = -1;
                ResumeAtEventRow = 0;

                this.Text = "Track " + (_trackIndex + 1) + ": " + _trackName;
                textBoxTrackLength.Text = _trackLength.ToString();
                textBoxTableOffset.Text = String.Format("{0:X8}", _trackAddr);
                textBoxFileOffset.Text = String.Format("{0:X8}", _trackFileOffset);
                textBoxFilters.Text = EventFilters; // this has a text-changed event that breaks the string into _filterTokens
                textBoxTicksPerBeat.Text = TICKS_PER_BEAT.ToString();
                textBoxMissingByteCount.Text = _missingByteCount.ToString(); // init to 0
                textBoxResumeAt.Text = (ResumeAtEventRow + 1).ToString();

                // event handlers should trigger here and set _divisor and _variation
                textBoxFixTimingDivisor.Text = INITIAL_VARIATION.ToString();
                textBoxFixTimingVariation.Text = INITIAL_DIVISOR.ToString();

                labelResultantDivisor.Text = "";
                labelResultantVariation.Text = "";
                labelEndOfTrackShift.Text = "";

                // read _hexBox at track
                int endOfTrackCounter = 0;
                int totalByteCount = 0;
                UInt32 acc = 0;
                bool bHaveEndOfTrackMarker = false;
                UInt32 addr = _trackFileOffset;
                UInt32 fileLen = (UInt32)_byteProvider.Length;
                byte val;

                string[] filterTokens = EventFilters.Split(new char[] { ' ', ',' });

                for (;;)
                {
                    // Read track from the hex control up to the end-of-track marker. We want to add new rows, 4-bytes per row,
                    // until the EOT marker or end-of-file is reached...

                    if (bHaveEndOfTrackMarker || totalByteCount > MAX_BYTE_LIMIT || addr >= fileLen)
                    {
                        // ...add any odd bytes that may be in the accumulator, padding lsbs with 0s
                        if ((totalByteCount & 3) != 0)
                        {
                            int temp = totalByteCount;

                            while ((temp & 3) != 0)
                            {
                                acc <<= 8;
                                temp++; // don't want to inc the real totalByteCount since _brokenTrackLength gets set to it later...
                            }

                            int rowIndex = dataGridView1.Rows.Add();
                            dataGridView1.Rows[rowIndex].HeaderCell.Value = (rowIndex + 1).ToString();
                            dataGridView1.Rows[rowIndex].Cells[0].Value = String.Format("{0:X8}", acc);
                        }

                        // finally - we have to have enough rows to accomodate all of the missing 1A/0D
                        // bytes... (we have to have a place to shift them during Auto Fix)
                        int neededRows = _trackLength / 4;

                        if ((_trackLength % 4) > 0)
                            neededRows++;

                        while (dataGridView1.Rows.Count < neededRows)
                        {
                            int rowIndex = dataGridView1.Rows.Add();
                            dataGridView1.Rows[rowIndex].HeaderCell.Value = (rowIndex + 1).ToString();
                            dataGridView1.Rows[rowIndex].Cells[0].Value = "00000000";
                        }

                        break;
                    }

                    val = _byteProvider.ReadByte(addr++);

                    // accumulate 4-byte words
                    acc <<= 8;
                    acc |= val;

                    totalByteCount++;

                    // ...add every 4th byte-accumulation to the gridView
                    if ((totalByteCount & 3) == 0)
                    {
                        int rowIndex = dataGridView1.Rows.Add();
                        dataGridView1.Rows[rowIndex].HeaderCell.Value = (rowIndex + 1).ToString();
                        dataGridView1.Rows[rowIndex].Cells[0].Value = String.Format("{0:X8}", acc);
                        acc = 0;
                    }

                    if (val == 0)
                    {
                        // set an end-of-track flag if we hit 4 consecutive 00s
                        if (++endOfTrackCounter >= 4)
                        {
                            // Check the byte after this sequence of 00 00 00 00
                            if (!_endOfTrackShiftFlag && addr < fileLen)
                            {
                                byte byteAfterMarker = _byteProvider.ReadByte(addr); // might read into the next track...

                                if (byteAfterMarker != 0)
                                    bHaveEndOfTrackMarker = true; // have a 4-byte EOT marker shifted in and counted at this point...
                                else
                                {
                                    uint valA, valB;
                                    if (addr + 4 <= fileLen)
                                        valA = GetWord(addr);
                                    else
                                        valA = uint.MaxValue;

                                    if (addr + 5 <= fileLen)
                                        valB = GetWord(addr + 1);
                                    else
                                        valB = uint.MaxValue;

                                    // Here we try to automate a formerly tedious process a bit... we used to have to ask for user input on every
                                    // track but it soon became apparent that the word that has 000010f0 fits 75% of tracks and we can cruise right
                                    // along with no questions asked :-)

                                    if (valA == 0x000010F0)
                                        bHaveEndOfTrackMarker = true;
                                    else if (valB == 0x000010F0)
                                    {
                                        endOfTrackCounter--; // go one more byte
                                        _endOfTrackShiftFlag = true; // global flag so we can display that we shifted one
                                    }
                                    else
                                    {

                                        // have to check limits or we easily read beyond the file...
                                        string sA;
                                        if (valA != uint.MaxValue)
                                            sA = String.Format("{0:X8} (next track)\n\n", valA);
                                        else
                                            sA = "********\n\n";

                                        string sB;
                                        if (valB != uint.MaxValue)
                                            sB = String.Format("{0:X8} (next track)\n\n", valB);
                                        else
                                            sB = "********\n\n";

                                        DialogResult result1 = MessageBox.Show("End-of-track marker:\n\n" +
                                            "A: " +
                                            String.Format("{0:X8}", GetWord(addr - 8)) +
                                            String.Format("({0:X8})", GetWord(addr - 4)) +
                                            sA +
                                            "B: " +
                                            String.Format("{0:X8}", GetWord(addr - 8 + 1)) +
                                            String.Format("({0:X8})", GetWord(addr - 4 + 1)) +
                                            sB + "If A is OK choose YES, if B is OK choose NO",
                                            "Track " + (_trackIndex + 1).ToString() + ": " + _trackName,
                                            MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button3);

                                        if (result1 == DialogResult.Cancel)
                                            return -1; // flag to abort...

                                        if (result1 == DialogResult.No)
                                        {
                                            endOfTrackCounter--; // go one more byte
                                            _endOfTrackShiftFlag = true; // global flag so we can display that we shifted one
                                        }
                                        else
                                            bHaveEndOfTrackMarker = true;
                                    }
                                }
                            }
                            else
                                bHaveEndOfTrackMarker = true;
                        }
                    }
                    else if (endOfTrackCounter != 0)
                        endOfTrackCounter = 0;
                }

                _brokenTrackLength = _originalBrokenTrackLength = totalByteCount; // set global count

                // _missingByteCounter is normally positive or 0, can be negative if the track has more bytes than the table's length says
                // we should expect...
                SetMissingByteCount(_trackLength - _brokenTrackLength);

                // call this AFTER setting _missingByteCount
                RefreshColorsAndTimes();

                if (_endOfTrackShiftFlag)
                    labelEndOfTrackShift.Text = "End-of-track marker (00 00 00 00) follows 00 timestamp...";
                else
                    labelEndOfTrackShift.Text = "";

                if (!bHaveEndOfTrackMarker)
                    MessageBox.Show("This track has no 00 00 00 00 end-of track marker!");
                else if (acc != 0)
                    MessageBox.Show("Accumulator is not 0! Previous track to this in memory may be corrupt...");
                else if (_missingByteCount == 0)
                    _bEventsFixed = true;
            }
            finally
            {
                // re-enable event handlers
                this.dataGridView1.CellEndEdit += dataGridView1_CellEndEdit;
            }

            return _brokenTrackLength;
        }

        private void SetMissingByteCount(int newCount)
        {
            if (_missingByteCount != newCount)
            {
                _missingByteCount = newCount;

                if (_missingByteCount == 0)
                    textBoxMissingByteCount.BackColor = Color.LightGreen;
                else
                    textBoxMissingByteCount.BackColor = Color.Yellow;

                textBoxMissingByteCount.Text = _missingByteCount.ToString();
                RefreshColorsAndTimes();
            }
        }
        #endregion

        #region AutoFixTiming

        private void AutoFixTiming()
        {
            if (!_bEventsFixed)
            {
                MessageBox.Show("Fix events first!");
                return;
            }

            if (_bTimingFixed)
            {
                DialogResult result1 = MessageBox.Show("Timing already fixed... Reset?",
                    "Track " + (_trackIndex + 1).ToString() + ": " + _trackName,
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

                if (result1 == DialogResult.Cancel)
                    return; // cancel;

                ResumeAtEventRow = 0;
                textBoxResumeAt.Text = (ResumeAtEventRow + 1).ToString();

                _bTimingFixed = false;
            }

            // ref parameters - on input to AutoFixTiming() they set the initial values, on exit they have the values that resolved track-timing
            int divisor = _divisor; // starting divisor
            int variation = _variation; // max variation permitted (in ticks)

            labelResultantDivisor.Text = "";
            labelResultantVariation.Text = "";

            int timingRet = AutoFixTiming(ResumeAtEventRow, ref divisor, ref variation);

            if (timingRet >= 0)
                MessageBox.Show("Solution not found at event: " + (timingRet + 1).ToString());
            else if (timingRet == -1)
            {
                // show the values that solved the issue
                labelResultantDivisor.Text = divisor.ToString();
                labelResultantVariation.Text = variation.ToString();

                _bTimingFixed = true;

                ResumeAtEventRow = 0;
                textBoxResumeAt.Text = (ResumeAtEventRow + 1).ToString();

                MessageBox.Show("Timing fixed!");
            }
            else if (timingRet == -2)
                MessageBox.Show("Error, solution not found!");
            else if (timingRet == -3)
                MessageBox.Show("Error in AutoFixTiming!");
            // else -4 is Cancel
        }

        public int AutoFixTiming(int rowStartIndex, ref int divisor, ref int variation)
        {
            // limit-check
            if (rowStartIndex < 0 || rowStartIndex >= dataGridView1.Rows.Count)
                return -3;

            int ret = -2; // no solution

            // put bytes into buffer
            byte[] buffer = EventsToByteArray(dataGridView1.Rows.Count * 4);

            if (buffer == null || buffer.Length < 4)
                return -3; // error

            // disable event handlers (if enabled)
            this.dataGridView1.CellEndEdit -= dataGridView1_CellEndEdit;

            try
            {
                int minDivisor = divisor; // typically 24
                int maxVariation = variation; // typically 6

                int currentVariation = 0; // start with no variation
                int currentDivisor = TICKS_PER_BEAT;

                while (currentVariation <= maxVariation)
                {
                    Application.DoEvents();

                    currentDivisor = TICKS_PER_BEAT;
                    ret = AutoFixTiming(rowStartIndex, currentDivisor, currentVariation, buffer);

                    if (ret == -1)
                        break;

                    Application.DoEvents();

                    currentDivisor = TICKS_PER_BEAT / 2;
                    ret = AutoFixTiming(rowStartIndex, currentDivisor, currentVariation, buffer);

                    if (ret == -1)
                        break;

                    Application.DoEvents();

                    currentDivisor = TICKS_PER_BEAT / 4;
                    ret = AutoFixTiming(rowStartIndex, currentDivisor, currentVariation, buffer);

                    if (ret == -1)
                        break;

                    Application.DoEvents();

                    // try triplet-timing last
                    currentDivisor = TICKS_PER_BEAT / 3;
                    ret = AutoFixTiming(rowStartIndex, currentDivisor, currentVariation, buffer);

                    if (ret == -1)
                        break;

                    currentVariation++;
                }

                // Update what we have...
                if (ret == -1 || ret >= 0)
                {
                    ByteArrayToEvents(buffer);
                    RefreshColorsAndTimes();

                    if (ret >= 0)
                        Hilight(ret); // show user the row with the error
                }

                variation = currentVariation;
                divisor = currentDivisor;
            }
            finally
            {
                // re-enable event handlers
                this.dataGridView1.CellEndEdit += dataGridView1_CellEndEdit;
            }

            return ret;
        }

        // divisor should be 96 (whole-beat), 48 (half-beat), 32 (1/3 beat) etc.
        // variation is in allowed ticks of deviation off-beat
        // rowStartIndex is set by selecting to the left of an event before pressing FixTiming and allows us to bypass
        // failing on earlier "orange" events (events that have a 1A or 0D in the time-stamp or next-event's velocity-byte (byte 3))
        //
        // -3 = error, -2 = solution not found, -1 = timing fixed, 0-N timing error at row-index
        private int AutoFixTiming(int rowStartIndex, int divisor, int variation, byte[] buffer)
        {
            // Preliminary Concept:
            // here we should have complete 4-byte events in each row of the dataGridView, we want to go through the events,
            // adding timestamps until a note-on with a cumulative time that is not evenly divisible by 96/2. Then we want to
            // locate the first event behind this note-on with a 1A or 0D in bytes 0, 2 or 3. If 1A or 0D is in byte 0,
            // replace with the opposite (if 1A put in a 0D, etc.). We need to save the cumulative time up to but not including
            // an event with 1a/0d - then add times from this event to the note-on and check for 0-remainder - if that's not it,
            // we need to swap bytes 2 and 3 in the next event with the time-byte and try for 0-remainder... If the 1a/0d is in
            // byte 2 or 3, swap accordingly...
            //
            // Final Concept - We move events to a byte array and scan 4 byte events, if not a 1a/0d we add the timestamp and keep
            // moving; otherwise, we call AddTimestampsThroughNextNoteOn() up to four times. First we test the existing timestamp up
            // to the next note-on, then we test 1a if the existing stamp is not 1a, 0d if the existing stamp is not 0d, and finally
            // we test the highest byte of the next event as the timestamp for this event. Also gave the user the ability to change the
            // test divisor and allowed ticks of variation as well as the starting event to begin flagging failures.  So if we fail
            // in a large block of "time-delay" events, we can simply set the row-selection past that and test those events.

            try
            {
                int length = buffer.Length;

                int cumulativeTime = 0;

                int startIndex = rowStartIndex * 4;

                for (int ii = 0; ii < length; ii += 4)
                {
                    // Have an event where we replaced a timestamp with 1a or 0d?
                    if (ThisOrNextEventHas1a0d(buffer, ii, startIndex))
                    {
                        if (TryDifferentTimestamps(buffer, ref cumulativeTime, ref ii, startIndex, divisor, variation))
                            continue;

                        // failure...
                        // can't find a solution!
                        int row = ii / 4;
                        if (row >= dataGridView1.Rows.Count)
                            return -2; // Error, solution not found!

                        return row;
                    }
                    else
                        cumulativeTime += buffer[ii + 3]; // add this event's timestamp
                }
            }
            catch
            {
                return -3; // error
            }

            return -1; // success, All timing fixed!
        }

        // recursively called from AddTimestampsThroughNextNoteOn()!
        private bool TryDifferentTimestamps(byte[] buffer, ref int cumulativeTime, ref int ii, int startIndex, int divisor, int variation)
        {
            // b0 => +3 timestamp
            // b1 => +2 event (midi-channel in low nibble)
            // b2 => +1 note
            // b3 => +0 velocity (0 if note-off)

            byte b0 = buffer[ii + 3]; // timestamp (byte 0)

            // will need to check velocity (byte 3) of next event for 1a/0d as well
            byte b3N;

            if (ii + 4 + 0 < buffer.Length)
                b3N = buffer[ii + 4 + 0];
            else
                b3N = 0;

            // NOTE: AddTimestampsThroughNextNoteOn() only changes cumulativeTime and ii if it returns true!
            // if we reach end-of-buffer with no note-on, returns true with ii set to buffer.Length

            // try the existing timestamp first - it may be ok!
            if (AddTimestampsThroughNextNoteOn(buffer, ref cumulativeTime, ref ii, b0, divisor, variation, startIndex))
                return true;

            int saveIndex = ii; // AddTimestampsThroughNextNoteOn() might increase ii so save the original...

            if (b0 == 0x1a && AddTimestampsThroughNextNoteOn(buffer, ref cumulativeTime, ref ii, 0x0d, divisor, variation, startIndex))
            {
                buffer[saveIndex + 3] = 0x0d; // timestamp
                return true;
            }

            if (b0 == 0x0d && AddTimestampsThroughNextNoteOn(buffer, ref cumulativeTime, ref ii, 0x1a, divisor, variation, startIndex))
            {
                buffer[saveIndex + 3] = 0x1a; // timestamp
                return true;
            }

            if (AddTimestampsThroughNextNoteOn(buffer, ref cumulativeTime, ref ii, b3N, divisor, variation, startIndex))
            {
                bool bIsNoteOn = (buffer[saveIndex + 2] & 0xf0) == 0x90; // event (byte 3) upper nibble is 0x9? (note-on)

                // don't accidentally turn a note-off (note-on with velocity 0) into a note-on by making velocity non-zero!
                if (bIsNoteOn && b3N == 0 && b0 != 0)
                    return false;

                // exchange velocity in next event with timestamp from this event
                buffer[saveIndex + 3] = b3N; // timestamp
                buffer[saveIndex + 4 + 0] = b0; // next event's velocity
                return true;
            }

            // Note: don't need to restore anything on fail since we directly pass the bytes we try as timestamps in
            // the AddTimestampsThroughNextNoteOn() parameter list - the test byte is never placed in the buffer until true is returned
            // (above)

            return false;
        }

        // Call this to test out the effect of changing a timestamp on some event up through the next note-on...
        // Returns true if we found a good combination. Only changes cumulativeTime and bufferIndex when true is returned!
        private bool AddTimestampsThroughNextNoteOn(byte[] buffer, ref int cumulativeTime, ref int bufferIndex, byte testByte, int divisor, int variation, int startIndex)
        {
            int testTime = testByte; // start out with the time-byte we pass in to test...
            int length = buffer.Length;

            // Each event is 4-bytes in the buffer ordered as timestamp (ii+0), event (ii+1), note (ii+2), velocity (ii+3).
            for (int ii = bufferIndex; ii < length; ii += 4)
            {
                // b0 => +3 timestamp
                // b1 => +2 event (midi-channel in low nibble)
                // b2 => +1 note
                // b3 => +0 velocity (0 if note-off)
                if (ii > bufferIndex)
                    testTime += buffer[ii + 3]; // add timestamp to cumulative time

                int sumTime = cumulativeTime + testTime;

                // if note-on (code 0x9X) and velocity > 0, check to see if the time at this note-on falls exactly on a half-beat
                // if so, return true and return the new cumulative time and the new buffer index where the calling routine should continue...
                if ((buffer[ii + 2] & 0xf0) == 0x90 && buffer[ii + 0] != 0)
                {
                    // Side note: problem if we have 1A in velocity of next event making it appear as a note-on when it is not - we stop to check for on-beat
                    // on what's actually a note off! So don't auto-fix inserting 1A/0D as velocity - always insert 1A as a timestamp then later allow the
                    // FixTiming() process to move bytes around!

                    int remainder = (sumTime % divisor);

                    // get skew off-beat in either time-direction
                    if (remainder > divisor / 2)
                        remainder = divisor - remainder;

                    // if off-beat skew <= "variation (in ticks) we call this note-on event "close-enough to on-beat to pass" :-)
                    if (remainder <= variation)
                    {
                        bufferIndex = ii;
                        cumulativeTime = sumTime;
                        return true;
                    }

                    //******************************************************************** TEST
                    // here I think, if this note-on is not in the same event as bufferIndex, we need to check it for 1A/0D - or will we eventually
                    // get to it since we return false????
                    // DO WE NEED RECURSIVE CHECK NOTE-ON ALSO IF IT HAS 1A OR 0D AS TIMESTAMP AND IS NOT THE FIRST EVENT????????????????????????????????????????????????????????????
                    if (ii > bufferIndex)
                    {
                        if (ThisOrNextEventHas1a0d(buffer, ii, startIndex))
                        {
                            int tempTime = sumTime; //??????????????????????????? need ????????????????????????????

                            if (TryDifferentTimestamps(buffer, ref tempTime, ref ii, startIndex, divisor, variation))
                            {
                                cumulativeTime = tempTime;
                                return true;
                            }
                        }
                    }
                    //******************************************************************** TEST

                    return false;
                }

                // If event is not a note on, we need to recursively check events that follow this event for the 1A/0D pattern
                // so that if we have many "repaired events" between this one and the next note-on, we try all combinations
                // of possible timestamps to get a cumulative-time through the note-on that's "on-beat"

                // Check next event for a timestamp of 1a or 0d
                int tempIndex = ii + 4;

                if (tempIndex >= buffer.Length)
                    break;

                if (ThisOrNextEventHas1a0d(buffer, tempIndex, startIndex))
                {
                    int tempTime = sumTime;

                    if (TryDifferentTimestamps(buffer, ref tempTime, ref tempIndex, startIndex, divisor, variation))
                    {
                        bufferIndex = tempIndex;
                        cumulativeTime = tempTime;
                        return true;
                    }
                }
            }

            // if we run out of buffer with no note-on having been detected, return true with bufferIndex set to length
            bufferIndex = length;
            cumulativeTime += testTime;
            return true; // note-on not found but return true anyway (we don't care all that much about abberations in note duration at end-of-track anyway)
        }

        private bool ThisOrNextEventHas1a0d(byte[] buffer, int ii, int startIndex)
        {
            if (ii < startIndex)
                return false;

            // b0 => +3 timestamp
            // b1 => +2 event (midi-channel in low nibble)
            // b2 => +1 note
            // b3 => +0 velocity (0 if note-off)

            byte b0 = buffer[ii + 3]; // timestamp (byte 0)

            // will need to check velocity (byte 3) of next event for 1a/0d as well
            byte b3N;

            if (ii + 4 + 0 < buffer.Length)
                b3N = buffer[ii + 4 + 0];
            else
                b3N = 0;

            if (b0 == 0x1a || b0 == 0x0d || b3N == 0x1a || b3N == 0x0d)
                return true;

            return false;
        }
        #endregion

        #region AutoFixNotes
        
        private void AutoFixNotes()
        {
            if (!_bEventsFixed)
            {
                MessageBox.Show("Fix events first!");
                return;
            }

            if (_bNotesFixed)
            {
                DialogResult result1 = MessageBox.Show("Notes already fixed... Reset?",
                    "Track " + (_trackIndex + 1).ToString() + ": " + _trackName,
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

                if (result1 == DialogResult.Cancel)
                    return; // cancel;

                ResumeAtEventRow = 0;
                textBoxResumeAt.Text = (ResumeAtEventRow + 1).ToString();

                _bNotesFixed = false;
            }

            // returns -1 if success, -3 if error, -4 if user-cancel
            int notesRet = AutoFixNotes(ResumeAtEventRow, false);

            if (notesRet != -1) // error or cancel?
            {
                if (notesRet == -3)
                    MessageBox.Show("Error in AutoFixNotes()!");
                // else -4 is Cancel

                return;
            }

            ResumeAtEventRow = 0;
            textBoxResumeAt.Text = (ResumeAtEventRow + 1).ToString();

            MessageBox.Show("Note-order fixed!");

            _bNotesFixed = true;
        }

        // Repairs misplaced 1a/0d in a note on/off timestamp or velocity which makes the note the opposite of what
        // it originally was...
        // returns -1 if success, -3 if error, -4 if user-cancel
        public int AutoFixNotes(int rowStartIndex, bool bSilentMode)
        {
            dataGridView1.ClearSelection();

            // put bytes into buffer
            byte[] buffer = EventsToByteArray(dataGridView1.Rows.Count * 4);

            if (buffer == null)
                return -3; // error

            int length = buffer.Length;

            if (length < 4)
                return -3; // error

            try
            {
                // b0 => +3 timestamp
                // b1 => +2 event (midi-channel in low nibble)
                // b2 => +1 note
                // b3 => +0 velocity (0 if note-off)

                // create a "do not process" flag-array
                var hasBeenProcessed = new byte[length];
                for (int ii = 0; ii < length; ii++)
                    hasBeenProcessed[ii] = 0;

                int startIndex = rowStartIndex * 4;

                // go through to find each event that's a note on or off
                for (int ii = startIndex; ii < length - 4; ii += 4)
                {
                    if (hasBeenProcessed[ii / 4] != 0) continue; // don't go back over items we processed!

                    byte event1 = (byte)(buffer[ii + 2] & 0xf0);

                    bool bNoteEvent1 = (event1 == 0x90) || (event1 == 0x80);

                    if (bNoteEvent1)
                    {
                        byte velocity1 = buffer[ii + 0];
                        bool bNoteOn1 = bNoteEvent1 && velocity1 != 0;
                        bool bNoteOff1 = bNoteEvent1 && (velocity1 == 0 || event1 == 0x80);
                        byte note1 = buffer[ii + 1];
                        byte midi1 = (byte)(event1 & 0x0f); // you can have notes going to different midi-channels in the same track!

                        // returns 1 if prev timestamp 0 and current velocity 1a/0d (this note-on should possibly be a note-off) (this event is orange)
                        // returns 2 if prev timestamp 1a/0d and current velocity 0 (this note-off should possibly be a note-on) (previous event is orange)
                        // otherwise returns 0
                        int ret1 = Is1a0dEvent(buffer, ii, event1, velocity1);

                        // from this note on/off - seek the next note on/off for the same note
                        for (int jj = ii + 4; jj < length - 4; jj += 4)
                        {
                            int row = jj / 4;

                            if (hasBeenProcessed[row] != 0) continue; // don't go back over items we processed!

                            byte event2 = (byte)(buffer[jj + 2] & 0xf0);

                            bool bNoteEvent2 = (event2 == 0x90) || (event2 == 0x80);
                            byte note2 = buffer[jj + 1];
                            byte midi2 = (byte)(event2 & 0x0f);

                            if (bNoteEvent2 && note2 == note1 && midi2 == midi1)
                            {
                                byte velocity2 = buffer[jj + 0];
                                bool bNoteOn2 = bNoteEvent2 && velocity2 != 0;
                                bool bNoteOff2 = bNoteEvent2 && (velocity2 == 0 || event2 == 0x80);

                                // returns 1 if prev timestamp 0 and current velocity 1a/0d (this note-on should possibly be a note-off) (this event is orange)
                                // returns 2 if prev timestamp 1a/0d and current velocity 0 (this note-off should possibly be a note-on) (previous event is orange)
                                // otherwise returns 0
                                int ret2 = Is1a0dEvent(buffer, jj, event2, velocity2);

                                // have noteOn1/noteOn2 but noteOn2 should be a noteOff or noteOff1/noteOff2 but noteOff2 should be a noteOn...
                                if (bNoteOn1 && bNoteOn2 && ret2 == 1)
                                {
                                    // turn event2 into a note-off
                                    // swap previous event to event2's timestamp with current event2 velocity
                                    int idx = jj - 4 + 3; // previous event to event2's timestamp
                                    buffer[jj + 0] = buffer[idx];
                                    buffer[idx] = velocity2;
                                }
                                else if (bNoteOff1 && bNoteOff2 && ret1 == 2)
                                {
                                    // turn event1 into a note-on
                                    // swap previous event to event1's timestamp with current event1 velocity
                                    int idx = ii - 4 + 3; // previous event to event1's timestamp
                                    buffer[jj + 0] = buffer[idx];
                                    buffer[idx] = velocity1;
                                }

                                hasBeenProcessed[row] = 1; // set event "already processed" flag

                                break;
                            }
                        }
                    }
                }

                ByteArrayToEvents(buffer);
                return -1; // ok
            }
            catch
            {
                MessageBox.Show("Exception in FixRogueNotes()!");
                return -3; // error
            }
        }

        // Return a code to help us decide if a note-on should really be a note-off,
        // or if a note-off (by velocity) should really be a note-on. Call this after we
        // already know that it's a note-event...
        //
        // index is a byte-index - to make it an event index, use multiples of 4 
        //
        // returns 1 if prev timestamp 0 and current velocity 1a/0d (this note-on should possibly be a note-off) (this event is orange)
        // returns 2 if prev timestamp 1a/0d and current velocity 0 (this note-off should possibly be a note-on) (previous event is orange)
        // otherwise returns 0
        public int Is1a0dEvent(byte[] buffer, int index, byte currentEvent, byte currentVelocity)
        {
            // b0 => +3 timestamp
            // b1 => +2 event (midi-channel in low nibble)
            // b2 => +1 note
            // b3 => +0 velocity (0 if note-off)
            if (index > buffer.Length - 4 || index < 4)
                return 0;

            // if this is an 8 code - a "real" note-off as opposed to a note-off by velocity,
            // we don't need or want any velocity-time swapping to happen
            if (currentEvent == 0x80)
                return 0;

            // get previous timestamp
            byte prevTimestamp = buffer[index - 4 + 3];

            if (prevTimestamp == 0 && (currentVelocity == 0x1a || currentVelocity == 0x0d))
                return 1;

            if (currentVelocity == 0 && (prevTimestamp == 0x1a || prevTimestamp == 0x0d))
                return 2;

            return 0;
        }

        //private bool DoDialog(byte[] hasBeenProcessed, int ii, int jj)
        //{
        //    int idx1 = ii / 4;
        //    int idx2 = jj / 4;

        //    Hilight(idx1); // highlight first event

        //    DialogResult result1 = MessageBox.Show("Need to hand-repair/delete note(s) then retry...\n\n1st note-event at: " +
        //        (idx1 + 1).ToString() + "\n2nd note-event at: " + (idx2 + 1).ToString() +
        //        "\n\nClick OK if events are OK or Cancel to Abort...",
        //        "Track " + (_trackIndex + 1).ToString() + ": " + _trackName,
        //        MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);

        //    if (result1 == DialogResult.Cancel)
        //        return true; // cancel;

        //    // manually approved...
        //    hasBeenProcessed[idx2] = 1; // set event "already processed" flag

        //    // Set restart point - if we Cancel to manually edit, we can restart where we left off...
        //    ResumeAtEventRow = idx1;
        //    textBoxResumeAt.Text = (ResumeAtEventRow + 1).ToString();

        //    return false;
        //}

        public void Hilight(int row)
        {
            dataGridView1.ClearSelection();

            if (row >= 0 && row < dataGridView1.Rows.Count)
            {

                dataGridView1.Rows[row].Cells[0].Selected = true;

                if (row - 5 >= 0)
                    dataGridView1.FirstDisplayedScrollingRowIndex = row - 5;
                else
                    dataGridView1.FirstDisplayedScrollingRowIndex = row;
            }
        }

        #endregion

        #region AutoFixEvents

        // pressing one button kicks off 3 repair algorithms sequentially
        private void buttonAutoFixEvents_Click(object sender, EventArgs e)
        {
            AutoFixEvents();

            if (_bEventsFixed)
            {
                AutoFixNotes();

                if (_bNotesFixed)
                    AutoFixTiming();
            }
        }

        private void AutoFixEvents()
        {
            if (_bEventsFixed)
            {
                DialogResult result1 = MessageBox.Show("Events already fixed... Reset?",
                    "Track " + (_trackIndex + 1).ToString() + ": " + _trackName,
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

                if (result1 == DialogResult.Cancel)
                    return; // cancel;

                ResumeAtEventRow = 0;
                textBoxResumeAt.Text = (ResumeAtEventRow + 1).ToString();

                _bEventsFixed = false;
                _bNotesFixed = false;
                _bTimingFixed = false;
            }

            int row = _initialBadRow;

            int ret = AutoFixEvents(ref row, false);

            // -1 if all good, -2 if error, -3 if canceled, -4 = bad data cell at row, -5 no data to process,
            // -6 = more broken bytes than expected, 0-N index of bad event
            switch (ret)
            {
                case 0:
                    MessageBox.Show("Bad byte found at index 0! (need to hand-edit this one, nothing above it to insert 0D into...)");
                    break;
                case -1:
                    RefreshColorsAndTimes(); // "Track is OK!";
                    ResumeAtEventRow = 0;
                    textBoxResumeAt.Text = (ResumeAtEventRow + 1).ToString();
                    _bEventsFixed = true;
                    break;
                case -2:
                    MessageBox.Show("Error in QuickCheckForBadEventCode()!");
                    break;
                case -3: // user pressed Cancel - we still want to refresh colors and timing...
                    if (checkBoxAutoInsert0D.Checked)
                        checkBoxAutoInsert0D.Checked = false; // go to manual editing

                    RefreshColorsAndTimes(); // set the note-on/off colors
                    break;
                case -4:
                    MessageBox.Show("buttonAutoFix_Click():1: Bad data in cell at row: " + (row + 1));
                    break;
                case -5:
                    MessageBox.Show("No data to process!");
                    break;
                case -6:
                    MessageBox.Show("We found more broken bytes than expected. _brokenTrackLength may be short by one." +
                        " Try checking \"Disable End-Of-Marker Shift\". Or - may need to add an event-type to the Allowed Types box?\n\n" +
                        "_brokenTrackLength = " + _brokenTrackLength + ", trackLength = " + _trackLength);
                    break;
                default: // returned value is > 0, (if all broken events were padded-out, _missingByteCount will be 0!)
                    AnalyzeTrack(); // check for bad events (also can change _trackLength to # bytes actually in the gridView data column!)
                    break;
            }
        }

        // -1 if all good, -2 if error, -3 if canceled, -4 = bad data cell at row, -5 no data to process,
        // -6 = more broken bytes than expected, 0-N index of bad event
        // pass in row as _initialBadRow - upon exit it has the row where we failed
        //
        // set bSilentMode to just "cancel" if any errors encountered, otherwise we bring up a user-prompt
        public int AutoFixEvents(ref int row, bool bSilentMode)
        {
            if (dataGridView1.Rows.Count <= 0)
            {
                row = -1;
                return -5; // no data to process
            }

            // disable event handler (if enabled)
            dataGridView1.CellEndEdit -= dataGridView1_CellEndEdit;

            try
            {
                dataGridView1.ClearSelection();

                int index;

                // When the form was shown, we called AnalyzeTrack()... and if it found a bad byte and if it inserted a 0D in the previous event's timestamp,
                // we need to propogate that byte through the remainder of the track and begin here at the following index...
                if (row < 0 || !checkBoxAutoInsert0D.Checked)
                    index = 0;
                else
                {
                    index = row; // set starting row from passed-in parameter

                    int ret = ShiftDataAfterEdit(dataGridView1, index); // decrements _missingByteCount

                    if (ret != -1)
                    {
                        row = ret;
                        return -4; // buttonAutoFix_Click():1: Bad data in cell at row: " + (row + 1)
                    }
                    // index++; Go ahead and re-check same event again (see below!)
                }

                // re-enable auto-insert 0D
                if (!checkBoxAutoInsert0D.Checked)
                    checkBoxAutoInsert0D.Checked = true;

                for (;;)
                {
                    // Search 4-byte event-codes (next to rightmost byte) in column 0 until an event code is detected that does not
                    // match one of the Allowed Event Codes, auto insert either 0D into the timestamp byte of the event that preceeds
                    // that code or 1A as the velocity field in this event.                
                    index = SearchForBadEvent(index, bSilentMode); // Returns: -3 = abort, -2 error, -1 no errors, 0-N index of the event we inserted a byte into.

                    // if event 0 is bad we cant add 0D to a previous event because there is none!
                    if (index < 1) // quit if error, user-cancel, all remaining events are ok... or if byte at index 0 is bad.
                        break;

                    if (_missingByteCount == 0)
                    {
                        // if the routines fixed all expected bad bytes we should have broken out above
                        // at the same time _missingByteCount decrements to 0 - and we never get here...
                        //
                        // unless possibly the end-of-track-marker is skewed by one byte and _brokenTrackLength
                        // is too short by one...
                        //
                        // or if the track is totally corrupt in some other way than one missing byte in an event
                        // if there was both a 0D and 1A in a single event we have problems!
                        row = index;
                        return -6; // We found more broken bytes than expected. _brokenTrackLength may be short by one.
                    }

                    // here we have a bad event code (out of alignment...) at "index"
                    // and we've either inserted a 0D at index-1 or 1A at index - now we need to ripple-propogate that byte down
                    // through every row (4-byte event code)...
                    if (checkBoxAutoInsert0D.Checked)
                    {
                        int ret = ShiftDataAfterEdit(dataGridView1, index); // decrements _missingByteCount

                        if (ret != -1)
                        {
                            row = ret;
                            return -4; // buttonAutoFix_Click():1: Bad data in cell at row: " + (row + 1)
                        }
                    }

                    // Found that we DO need to recheck same event if it was missing more than one byte and we just inserted one
                    // of them... SO MUST recheck!
                    //index++; // don't need to recheck event we just fixed!
                }

                return index; // -1 if all good, -2 if error, -3 if canceled, 0-N index of bad event
            }
            finally { dataGridView1.CellEndEdit += dataGridView1_CellEndEdit; }
        }

        /// <summary>
        /// Searches events from startOffset for a bad event-code, highlights it in yellow
        /// and inserts a 0D in the previous event. Returns -1 if no bad events
        /// Returns -3 if user-cancel, -2 if error, -1 if no bad events, 0-N index of first bad event
        /// </summary>
        private int SearchForBadEvent(int startOffset, bool bSilentMode)
        {
            // Note: badEventCodeCounter is only counting the number of "yellow" cells - where the event-code
            // does not line-up - this is not the same as the number of missing bytes in the track which
            // is computed via _trackLength - _brokenTrackLength (see above)

            int ret = -1;

            for (int ii = startOffset; ii < dataGridView1.Rows.Count; ii++)
            {
                string eventCode = dataGridView1.Rows[ii].Cells[0].Value.ToString();

                if (eventCode.Length != 8)
                {
                    dataGridView1.Rows[ii].Cells[0].Style.BackColor = Color.Red;

                    if (!bSilentMode)
                        MessageBox.Show("Cancelling due to bad event length at: " + (ii + 1).ToString() + "\nEvent: \"" + eventCode.ToString() + "\"");

                    ret = -3; // cancel
                    break;
                }

                if (eventCode == "00000000")
                {
                    // report EOT marker as a bad event (return its index) if _missingByteCount == 1
                    if (ii == dataGridView1.Rows.Count - 1 && _missingByteCount == 1)
                    {
                        // bad event found
                        dataGridView1.Rows[ii].Cells[0].Style.BackColor = Color.Yellow;

                        int ret2 = InsertCode(ii - 1, 6, INS_CODE); // returns -1 if 0D inserted, -2 if error, 0 if not inserted
                        if (ret2 == -1) ret = ii - 1; // return index of event we inserted 0D into
                        if (ret2 == -2 || ret == 0) ret = -2; // error
                        break;
                    }

                    dataGridView1.Rows[ii].Cells[0].Style.BackColor = Color.LightGray;
                }
                else
                {
                    // -3 = abort, -2 error, -1 this event is ok, 0-N bad event index
                    ret = DetectBadEventCode(ii, bSilentMode);

                    if (ret == -3)
                        break; // user-cancel

                    if (ret == -2)
                    {
                        MessageBox.Show("Refresh Colors Error!");

                        dataGridView1.Rows[ii].Cells[0].Style.BackColor = Color.Red;
                        break;
                    }

                    if (ret >= 0)
                    {
                        // bad event found
                        dataGridView1.Rows[ii].Cells[0].Style.BackColor = Color.Yellow;
                        break;
                    }

                    // ret == -1 (no error so go check next event (keep looping))
                    dataGridView1.Rows[ii].Cells[0].Style.BackColor = Color.White; // event-code position is ok
                }

                dataGridView1.Rows[ii].Cells[0].Style.ForeColor = Color.Black;
            }

            return ret;
        }

        /// <summary>
        /// Scans events for a bad code as determined from the Allowed Codes filter-list.
        /// Can prompt the user to add a new Allowed Code to the list and auto-inserts a 0D
        /// in the preceeding event.
        ///
        /// Returns: -3 = abort, -2 error, -1 this event is ok, 0-N bad event index
        /// </summary>
        private int DetectBadEventCode(int ii, bool bSilentMode)
        {
            if (_filterTokens == null)
            {
                MessageBox.Show("Check Allowed Event edit-box (9X 10 is default)!");
                return -3;
            }

            string sCodeNorm, sCodeShift;
            bool bIsAllowedNorm, bIsAllowedShift;

            string eventCode = dataGridView1.Rows[ii].Cells[0].Value.ToString();

            // "vvnnEEtt" sCodeNorm will have EE, sCodeShift will have nn (usually a note if EE is 9X)
            sCodeNorm = eventCode.Substring(4, 2);
            bIsAllowedNorm = IsAllowedEventCode(sCodeNorm, _filterTokens);
            sCodeShift = eventCode.Substring(2, 2);
            bIsAllowedShift = IsAllowedEventCode(sCodeShift, _filterTokens);

            // Have four possibilities:
            // 1) !bIsAllowedShift && !bIsAllowedNorm => Ask user if we should add sCodeA to Allowed list
            // 2) !bIsAllowedShift && bIsAllowedNorm => OK! Move on to next event
            // 3) bIsAllowedShift && !bIsAllowedNorm => Insert 0D as the new timestamp for the previous event and ripple bytes through
            // 4) bIsAllowedShift && bIsAllowedNorm => Ask user if sCodeA might be a timestamp that just happens to also pass the allowed-events
            //                                   filter... if not do (2) - if it is, do (3)
            //
            // Another common possibility is when sCodeNorm is not an event and sCodeShift is identical to the previous event-code,
            // like:
            //                   45 3A 9E 6D  (9E event is ok)
            //                   5D 9E 93 vv  (9E event is left-shifted due to missing 1A or 0D - likely a 1A if velocity)
            //
            // We want to insert 0D in front of previous event's 6D and do it automatically, without asking any questions... note that the 93 appears to
            // be a valid event but is NOT, it's a timestamp! So we want to automatically repair:
            //                        if (bIsAllowedShift && sCodeNorm != sCodePrev && sCodeShift == sCodePrev)

            if (bIsAllowedShift) // insert 0D in previous event... if it belongs as velocity in this event, timing-fix will handle that for us later!
            {
                if (bIsAllowedNorm) // insert 0D in previous event?
                {
                    string prevEventCode = "", sCodePrev = "";

                    if (ii - 1 >= 0)
                    {
                        prevEventCode = dataGridView1.Rows[ii - 1].Cells[0].Value.ToString();

                        if (prevEventCode.Length == 8)
                            sCodePrev = prevEventCode.Substring(4, 2);
                    }

                    if (sCodePrev.Length == 2 && sCodeNorm != sCodePrev && sCodeShift == sCodePrev)
                    {
                        // returns -1 if 0D inserted, -2 if error, 0 if not inserted
                        int ret = InsertCode(ii - 1, 6, INS_CODE);
                        if (ret == -1) return ii - 1; // return index of event we inserted 0D into
                        if (ret == -2 || ret == 0) return -2; // error
                    }
                    else if (!bSilentMode)
                    {
                        // Ask user if sCodeA might be a timestamp that just happens to also pass the allowed-events filter...
                        // if not OK! Move on to next event - if it is, Insert 0D as the new timestamp for the previous event and ripple bytes through

                        // allow some space at top
                        int idx = ii - 10;
                        if (idx < 0) idx = 0;

                        dataGridView1.Rows[ii].Cells[0].Style.BackColor = Color.Yellow;
                        dataGridView1.FirstDisplayedScrollingRowIndex = idx; // scroll to index
                        dataGridView1.Refresh();
                        this.Update();

                        DialogResult result1 = MessageBox.Show("Event-code looks OK but it may be a time-stamp left-shifted into the event-code position..." +
                            "\n\nIndex: " + (ii + 1).ToString() +
                            "\nEvent: " + eventCode +
                            "\n\nIs event OK?\n" +
                            "(Answering NO will insert a 0D as previous event's timestamp!)",
                            "Track " + (_trackIndex + 1).ToString() + ": " + _trackName,
                            MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

                        if (result1 == DialogResult.Cancel)
                            return -3; // abort

                        // If NO, insert 0D as the new timestamp for the previous event and ripple bytes through
                        if (result1 == DialogResult.No)
                        {
                            // returns -1 if 0D inserted, -2 if error, 0 if not inserted
                            int ret = InsertCode(ii - 1, 6, INS_CODE);
                            if (ret == -1) return ii - 1; // return index of event we inserted 0D into
                            if (ret == -2 || ret == 0) return -2; // error
                        }

                        // Event is ok - set resume-point
                        ResumeAtEventRow = ii / 4 + 1;
                        textBoxResumeAt.Text = (ResumeAtEventRow + 1).ToString();
                    }
                    else
                        return -3; // if bSilentMode we quit if there are any fixes that require user-interaction!
                }
                else // Insert 0D as the new timestamp for the previous event and ripple bytes through
                {
                    // returns -1 if 0D inserted, -2 if error, 0 if not inserted
                    int ret = InsertCode(ii - 1, 6, INS_CODE);
                    if (ret == -1) return ii - 1; // return index of event we inserted 0D into
                    if (ret == -2 || ret == 0) return -2; // error
                }
            }
            else if (!bIsAllowedNorm) // ask if we should add code to allowed-list
            {
                if (bSilentMode)
                    return -3; // if bSilentMode we quit if there are any fixes that require user-interaction!

                // allow some space at top
                int idx = ii - 10;
                if (idx < 0) idx = 0;

                dataGridView1.Rows[ii].Cells[0].Style.BackColor = Color.Yellow;
                dataGridView1.FirstDisplayedScrollingRowIndex = idx; // scroll to index
                dataGridView1.Refresh();
                this.Update();

                string sCodeTwoShift = eventCode.Substring(0, 2);
                bool bIsAllowedTwoShift = IsAllowedEventCode(sCodeTwoShift, _filterTokens);

                DialogResult result1;

                if (bIsAllowedTwoShift)
                {
                    result1 = MessageBox.Show("Might be missing TWO bytes before " + sCodeTwoShift.ToUpper() + "\n\n" +
                        "Index: " + (ii + 1).ToString() + "\n" +
                        "Event: " + eventCode + "\n\n" +
                        "Click Cancel to disable \"Auto Insert 0D\" and hand-edit,\n" +
                        "Click YES to add " + sCodeNorm.ToUpper() + " to Allowed Event Codes and continue.\n" +
                        "Click NO to insert a 0D/1A as previous event's timestamp and this event's velocity.\n",
                        "Track " + (_trackIndex + 1).ToString() + ": " + _trackName,
                        MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);

                    if (result1 == DialogResult.No)
                    {
                        // Insert 0D as the new timestamp for the previous event, 1A for this event velocity and ripple bytes through
                        // Start with the 1A here and insert the 0D below...

                        // returns -1 if 0D inserted, -2 if error, 0 if not inserted
                        int ret = InsertCode(ii - 1, 6, "1A");
                        if (ret == -1) return ii - 1; // return index of event we inserted 0D into
                        if (ret == -2 || ret == 0) return -2; // error
                    }
                }
                else
                {
                    result1 = MessageBox.Show("Might have a new event-code " + sCodeNorm.ToUpper() + " (not in Allowed Event Codes List)...\n\n" +
                        "Index: " + (ii + 1).ToString() + "\n" +
                        "Event: " + eventCode + "\n\n" +
                        "Click Cancel to disable \"Auto Insert 0D\" and hand-edit,\n" +
                        "Click YES to add " + sCodeNorm.ToUpper() + " to Allowed Event Codes and continue.\n" +
                        "Click NO to insert a 0D a previous event's timestamp.\n",
                        "Track " + (_trackIndex + 1).ToString() + ": " + _trackName,
                        MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                }

                // disable auto-insert 0D and abort...
                if (result1 == DialogResult.Cancel)
                {
                    checkBoxAutoInsert0D.Checked = false;
                    return -3; // abort
                }

                if (result1 == DialogResult.No)
                {
                    // Insert 0D as the new timestamp for the previous event and ripple bytes through
                    // returns -1 if 0D inserted, -2 if error, 0 if not inserted
                    int ret = InsertCode(ii - 1, 6, INS_CODE);
                    if (ret == -1) return ii - 1; // return index of event we inserted 0D into
                    if (ret == -2 || ret == 0) return -2; // error
                }
                else if (result1 == DialogResult.Yes)
                    // Add a new event filter (space seperated) and keep going...
                    textBoxFilters.Text += " " + sCodeNorm.ToUpper(); // text-changed event-handler should add this to _filterTokens for us...


                // Event is ok - set resume-point
                ResumeAtEventRow = ii / 4 + 1;
                textBoxResumeAt.Text = (ResumeAtEventRow + 1).ToString();
            }

            if (dataGridView1.Rows[ii].Cells[0].Style.BackColor != Color.White)
                dataGridView1.Rows[ii].Cells[0].Style.BackColor = Color.White;

            return -1; // (!bIsAllowedShift && bIsAllowedNorm) this event looks ok, so keep going...
        }

        /// <summary>
        /// Returns true if sIn has a midi-event we want to recognize
        /// </summary>
        private bool IsAllowedEventCode(string sIn, string[] filterTokens)
        {
            sIn = sIn.ToLower();

            // want to return true if sIn has a match to one of the filter-strings
            // in the list. Example: pass "97" in sIn and true is returned because
            // filterTokens has '9X' in it (X is don't care)
            for (int ii = 0; ii < filterTokens.Length; ii++)
            {
                filterTokens[ii] = filterTokens[ii].ToLower();

                bool bMatch1 = false;
                bool bMatch2 = false;

                if (filterTokens[ii].Length == 2 && sIn.Length == 2)
                {
                    if (filterTokens[ii][0] == 'x' || filterTokens[ii][0] == sIn[0])
                        bMatch1 = true;
                    if (filterTokens[ii][1] == 'x' || filterTokens[ii][1] == sIn[1])
                        bMatch2 = true;
                    if (bMatch1 && bMatch2)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Insert sCode at the timestamp position of the 4-byte hex event at index
        /// Returns -1 if 0D inserted, -2 if error, 0 if not inserted
        /// </summary>
        private int InsertCode(int rowIndex, int position, string sCode)
        {
            // For a note-event, 1A and 0D are probably not velocity or a note-codes and 0D is
            // is a pretty short time-stamp... so the most-likley missing code is a 0D for the
            // time-stamp in the previous event, For other event-types, a missing time-stamp
            // is also a fair guess so lets try it...
            try
            {
                if (rowIndex >= dataGridView1.Rows.Count || position < 0 || position > 8)
                    return 0;

                string eventCode = dataGridView1.Rows[rowIndex].Cells[0].Value.ToString();

                if (eventCode.Length != 8 || eventCode == "00000000")
                    return 0; // don't shuffle bytes... no insertion

                if (checkBoxAutoInsert0D.Checked)
                    dataGridView1.Rows[rowIndex].Cells[0].Value = eventCode.Insert(position, sCode);

                return -1; // code inserted, ok to right-shift byte through remaining events
            }
            catch { MessageBox.Show("Exception in InsertCode()"); }

            return -2; // error
        }

        /// <summary>
        /// Shifts byte in at msb of hex string and returns a byte shifted out the lsb
        /// </summary>
        private int ShiftDataAfterEdit(DataGridView dgv, int rowIndex)
        {
            if (rowIndex >= dataGridView1.Rows.Count)
            {
                MessageBox.Show("ShiftDataAfterEdit(): rowIndex >= Rows.Count!");
                return -2;
            }

            // disable event handler (if enabled)
            dgv.CellEndEdit -= dataGridView1_CellEndEdit;

            try
            {
                if (dataGridView1.Rows[rowIndex].Cells[0].Style.BackColor != Color.White)
                    dataGridView1.Rows[rowIndex].Cells[0].Style.BackColor = Color.White; // clear yellow color

                string s = dgv.Rows[rowIndex].Cells[0].Value.ToString();

                string[] filterTokens = EventFilters.Split(new char[] { ' ', ',' });

                byte newByte = 0;

                if (s.Length == 8)  // handle case of user hand-editing a location (just save it back)
                {
                    dgv.Rows[rowIndex].Cells[0].Value = s;

                    try { LastEditedValue = Convert.ToUInt32(s, 16); }
                    catch { LastEditedValue = 0; }
                    LastEditedIndex = rowIndex;
                }
                else if (s.Length == 10) // ripple bytes down if two chars (one hex byte) added
                {
                    // walk forward, from this point on, propogating the rightmost byte of each 4-byte word
                    for (int ii = rowIndex; ii < dgv.Rows.Count; ii++)
                    {
                        try { s = dgv.Rows[ii].Cells[0].Value.ToString(); }
                        catch { return ii; } // error

                        if (ii == rowIndex)
                        {
                            newByte = Convert.ToByte(s.Substring(8, 2), 16);  // save lsb (rightmost byte)
                            s = s.Substring(0, 8); // cut off lsb of value we just inserted text into
                        }
                        else
                            newByte = ShiftRight(ref s, newByte);

                        dgv.Rows[ii].Cells[0].Value = s;
                    }

                    // decrement counter
                    if (_missingByteCount > 0)
                        SetMissingByteCount(_missingByteCount - 1);
                }
                else if (s.Length == 6) // ripple bytes up if two chars (one hex byte) deleted
                {
                    // walk backward, from end to the edit-location, propogating the lefttmost byte of each 4-byte word
                    // (we feed in a 00 at the end of the list to kick things off)
                    for (int ii = dgv.Rows.Count - 1; ii >= rowIndex; ii--)
                    {
                        s = dgv.Rows[ii].Cells[0].Value.ToString();

                        if (ii == rowIndex)
                            dgv.Rows[ii].Cells[0].Value = s + String.Format("{0:X2}", newByte); // back to one we deleted two hex chars from
                        else
                        {
                            newByte = ShiftLeft(ref s, newByte);
                            dgv.Rows[ii].Cells[0].Value = s;
                        }
                    }

                    // increment counter
                    SetMissingByteCount(_missingByteCount + 1);
                }
            }
            //catch { MessageBox.Show("Exception in ShiftDataAfterEdit!"); }
            finally { dgv.CellEndEdit += dataGridView1_CellEndEdit; } // re-enable event handler

            return -1; // success
        }

        // shifts byte in at msb of hex string and returns a byte shifted out the lsb
        private byte ShiftRight(ref string s, byte newByte)
        {
            byte retByte = 0;
            int len = s.Length;

            if (len >= 2)
            {
                len -= 2;

                try
                {
                    retByte = Convert.ToByte(s.Substring(len, 2), 16);  // save lsb (rightmost byte)
                    s = String.Format("{0:X2}", newByte) + s.Substring(0, len); // push in new byte at msb (leftmost)
                }
                catch { }
            }
            return retByte;
        }

        // shifts byte in at lsb of hex string and returns a byte shifted out the msb
        private byte ShiftLeft(ref string s, byte newByte)
        {
            byte retByte = 0;
            int len = s.Length;

            if (len >= 2)
            {
                len -= 2;

                try
                {
                    retByte = Convert.ToByte(s.Substring(0, 2), 16);  // save msb (lefttmost byte)
                    s = s.Substring(2, len) + String.Format("{0:X2}", newByte); // push in new byte at lsb (rightmost)
                }
                catch { }
            }
            return retByte;
        }
        #endregion

        #region AnalyzeTrack

        private void AnalyzeTrack(int offset = 0)
        {
            dataGridView1.CellEndEdit -= dataGridView1_CellEndEdit;

            try
            {
                // Set the yellow color on any cells that have a misalligned event code (xx 00 EE xx). Valid events
                // are checked against the filter-list (user can set via textBoxFilters) - don't try to insert any codes
                // to fix it at this point
                bool bSaveChecked = checkBoxAutoInsert0D.Checked;
                checkBoxAutoInsert0D.Checked = false;

                int ret = SearchForBadEvent(offset, true); // Returns: -3 = abort, -2 error, -1 no errors, 0-N index of the event that has a problem

                checkBoxAutoInsert0D.Checked = bSaveChecked;

                if (ret == -2)
                    return; // quit if error

                if (ret == -3)
                {
                    if (checkBoxAutoInsert0D.Checked)
                        checkBoxAutoInsert0D.Checked = false; // go to manual editing

                    RefreshColorsAndTimes(); // set the note-on/off colors

                    return; // quit if user-cancel or a bad event detected with bQuietMode set in SearchForBadEvent() above...
                }


                _initialBadRow = ret;

                // no bad events but still have missing or excess bytes? Something's wrong - track is corrupt
                // go through dialog that lets user actually clean-up the track and modify the length and address tables in the main hexBox
                // to fit the new track... (takes a long time to update the gridView!) - don't do this unless offset is 0 so we check the entire
                // track.
                if (offset == 0 && _initialBadRow < 0 && _missingByteCount != 0)
                {
                    // in this situation, we need to shrink the length to the last event plus 4-byte EOT marker
                    // ...the address table in the hexBox needs updating as well as the end-address. Only need to change this length
                    // in the length-table - rest are ok. Then re-load everything in FormFixHa via LoadLengthsAndOffsets().
                    DialogResult result1 = MessageBox.Show("Corrupt track, no bad events but still have " + _missingByteCount + " (missing or excess) bytes...\n" +
                        "(Make sure you answered correctly on the EOT alignment question!)\n\nClicking OK might change the track-length! (go ahead, try it :-) )",
                        "Track " + (_trackIndex + 1).ToString() + ": " + _trackName,
                        MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);

                    if (result1 == DialogResult.Cancel)
                        return; // abort

                    // fix...
                    // _missingByteCount will be positive if we need to delete bytes from the track-length...
                    // but will be negative if we need to add bytes to the track-length.
                    //
                    // Change our track-length... when this form closes, FormFixHa will detect the change in _trackLength
                    // from when the form opened and will initiate changes to the length/address tables for the song.
                    //
                    // Since when we load the track, we now load the larger of the two possible lengths, either the length
                    // specified in the table or the length mandated by the ends-of-track marker, we will never need to
                    // add rows to gridView1, only remove them if _missingByteCount is positive...
                    if (_missingByteCount != 0)
                    {
                        int count = dataGridView1.Rows.Count;

                        int ii;
                        for (ii = 0; ii < count; ii++)
                            if (dataGridView1.Rows[ii].Cells[0].Value.ToString() == "00000000")
                                break;

                        if (ii >= count) // No EOT marker? Add one...
                            dataGridView1.Rows.Add("00000000", 0, 0);
                        else // otherwise; remove rows after the EOT marker
                        {
                            count = dataGridView1.Rows.Count - 1 - ii;

                            while (count-- > 0)
                            {
                                try { dataGridView1.Rows.RemoveAt(dataGridView1.Rows.Count - 1); }
                                catch { }
                            }
                        }

                        _trackLength = dataGridView1.Rows.Count * 4;
                        textBoxTrackLength.Text = _trackLength.ToString();

                        SetMissingByteCount(0);
                    }
                }

                if (_missingByteCount == 0)
                    RefreshColorsAndTimes(); // set the note-on/off colors
                else
                    ScrollToYellow();
            }
            finally { dataGridView1.CellEndEdit += dataGridView1_CellEndEdit; }
        }
        #endregion

        #region RefreshColorsAndTimes

        // highlights note-on/off in color
        private void RefreshColorsAndTimes()
        {
            if (_missingByteCount != 0 || NoteColors.colors.Count < MAX_NOTE_COLORS)
                return;

            // put bytes into buffer
            byte[] buffer = EventsToByteArray(dataGridView1.Rows.Count * 4);

            if (buffer == null || buffer.Length < 4)
                return;

            dataGridView1.ClearSelection();

            List<uint[]> onTimes = new List<uint[]>();

            _totalTimeTicks = 0;

            Color fColor = Color.Black;
            Color bColor = Color.White;

            int length = buffer.Length;

            for (int ii = 0; ii < length; ii += 4)
            {
                int row = ii / 4;

                // b0 => +3 timestamp
                // b1 => +2 event (midi-channel in low nibble)
                // b2 => +1 note
                // b3 => +0 velocity (0 if note-off)

                byte b0 = buffer[ii + 3];
                byte b1 = buffer[ii + 2];
                byte b2 = buffer[ii + 1];
                byte b3 = buffer[ii + 0];

                if (b0 != 0 || b1 != 0 || b2 != 0 || b3 != 0)
                {
                    _totalTimeTicks += b0; // cumulative time

                    dataGridView1.Rows[row].Cells[3].Value = ""; // we fill in the note-on time below after receiving its corresponding note-off event
                    dataGridView1.Rows[row].Cells[2].Value = String.Format("{0:f3}", (double)(b0) / (double)(TICKS_PER_BEAT));
                    dataGridView1.Rows[row].Cells[1].Value = String.Format("{0:f3}", (double)(_totalTimeTicks) / (double)(TICKS_PER_BEAT));

                    if (bColor != Color.White)
                        bColor = Color.White; // default background color

                    if (fColor != Color.Black)
                        fColor = Color.Black; // default foreground color

                    byte code = (byte)(b1 & 0xf0);

                    if (code == 0x90 || code == 0x80) // note-on or note-off?
                    {
                        int note = b2 % MAX_NOTE_COLORS;

                        if (note <= 11)
                        {
                            byte midi = (byte)(b1 & 0x0f);
                            byte vel = b3;

                            bColor = NoteColors.colors[note].c;

                            if (code == 0x90 && vel > 0) // on
                            {
                                fColor = Color.White;
                                onTimes.Add(new uint[] { b2, midi, _totalTimeTicks }); // save raw-note and time
                            }
                            else if (code == 0x80 || (code == 0x90 && vel == 0)) // off
                            {
                                fColor = Color.Silver;

                                // find note-on entry in list and compute note-on time
                                if (onTimes.Count > 0)
                                {
                                    // look for this note in the list of notes currently on
                                    // (NOTE: should do this in reverse? - should really not matter since this is a single track's data and it's also
                                    // keyed to the midi-channel...) <= BUT can have bad events - can't really "expect" it to be a particular way...
                                    //foreach (uint[] x in onTimes)
                                    //    if (x[0] == b2 && x[1] == midi)
                                    //    {
                                    //        // compute and display note-on time
                                    //        row.Cells[3].Value = String.Format("{0:f3}", (double)(_totalTimeTicks - x[2]) / (double)(TICKS_PER_BEAT));
                                    //        onTimes.Remove(x);
                                    //        break;
                                    //    }
                                    for (int jj = onTimes.Count - 1; jj >= 0; jj--)
                                    {
                                        if (onTimes[jj][0] == b2 && onTimes[jj][1] == midi)
                                        {
                                            // compute and display note-on time
                                            dataGridView1.Rows[row].Cells[3].Value = String.Format("{0:f3}", (double)(_totalTimeTicks - onTimes[jj][2]) / (double)(TICKS_PER_BEAT));
                                            onTimes.RemoveAt(jj);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // make an event we had to change orange to stand out... these may need tweaking!
                    if (b0 == 0x1a || b1 == 0x1a || b2 == 0x1a || b3 == 0x1a || b0 == 0xd || b1 == 0xd || b2 == 0xd || b3 == 0xd)
                        bColor = Color.Orange;
                }
                else // error
                {
                    fColor = Color.Red;
                    bColor = Color.Black;
                }

                if (dataGridView1.Rows[row].Cells[0].Style.BackColor != bColor)
                    dataGridView1.Rows[row].Cells[0].Style.BackColor = bColor;

                if (dataGridView1.Rows[row].Cells[0].Style.ForeColor != fColor)
                    dataGridView1.Rows[row].Cells[0].Style.ForeColor = fColor;
            }
        }
        #endregion

        #region MiscRoutines

        private void ScrollToYellow()
        {
            foreach (DataGridViewRow r in dataGridView1.Rows)
            {
                if (r.Cells[0].Style.BackColor == Color.Yellow)
                {
                    int idx = r.Index - 10;

                    if (idx < 0)
                        idx = r.Index;

                    dataGridView1.FirstDisplayedScrollingRowIndex = idx;
                    break;
                }
            }
        }

        // returns # bytes copied, -1 if error
        public int CopyTrackToClipboard()
        {
            // Convert track's hex event text from column 0 of actual dataGridView into byte-buffer (4-byte hex events) 
            byte[] buffer = EventsToByteArray(_trackLength);

            if (buffer == null)
                return -1;

            int newLength = buffer.Length;

            if (newLength != _trackLength)
                return -1;

            DataObject da = new DataObject();

            // set string buffer clipbard data
            string sBuffer = System.Text.Encoding.ASCII.GetString(buffer, 0, newLength);

            da.SetData(typeof(string), sBuffer);

            //set memorystream (BinaryData) clipboard data
            System.IO.MemoryStream ms = new System.IO.MemoryStream(buffer, 0, newLength, false, true);
            da.SetData("BinaryData", ms);

            Clipboard.SetDataObject(da, true);

            return newLength;
        }


        public bool ByteArrayToEvents(byte[] buffer)
        {
            int rowCount = dataGridView1.Rows.Count;

            if (buffer == null || rowCount != buffer.Length / 4)
            {
                MessageBox.Show("ByteArrayToEvents() failed!");
                return false;
            }

            UInt32 acc;

            int wordIndex = 0;

            for (int ii = 0; ii < rowCount; ii++, wordIndex += 4)
            {
                acc = 0;

                for (int jj = 0; jj < 4; jj++)
                {
                    acc <<= 8;
                    acc |= buffer[wordIndex + jj];
                }

                try
                {
                    dataGridView1.Rows[ii].Cells[0].Value = String.Format("{0:X8}", acc);
                }
                catch
                {
                    MessageBox.Show("Bad hex conversion in ByteArrayToEvents() at: " + (ii + 1).ToString());
                    return false;
                }
            }

            return true;
        }

        private byte[] EventsToByteArray(int numberOfBytesInTrack)
        {
            if (numberOfBytesInTrack < 4)
            {
                MessageBox.Show("EventsToByteArray() numberOfBytesInTrack < 4!");
                return null;
            }

            UInt32 val;
            UInt32 offset = 0;

            byte[] buffer = new byte[numberOfBytesInTrack];

            foreach (DataGridViewRow r in dataGridView1.Rows)
            {
                try
                {
                    val = Convert.ToUInt32(dataGridView1.Rows[r.Index].Cells[0].Value.ToString(), 16);
                }
                catch
                {
                    MessageBox.Show("Bad 4-byte hex event at: " + (r.Index + 1).ToString() + ", fix it or press Cancel!");
                    return null;
                }

                if (offset <= numberOfBytesInTrack - 4)
                {
                    for (int ii = 3; ii >= 0; ii--)
                    {
                        buffer[offset + ii] = (byte)(val & 0xff);
                        val >>= 8;
                    }

                    offset += 4;
                }
                else
                {
                    // write an end of track marker
                    buffer[numberOfBytesInTrack - 4] = 0;
                    buffer[numberOfBytesInTrack - 3] = 0;
                    buffer[numberOfBytesInTrack - 2] = 0;
                    buffer[numberOfBytesInTrack - 1] = 0;
                    MessageBox.Show("TrackLength (" + numberOfBytesInTrack + ") is shorter than # events in grid (" +
                        (dataGridView1.Rows.Count * 4).ToString() + ").\n" +
                        "Truncating track and writing end-of-track marker...",
                        "Track " + (_trackIndex + 1).ToString() + ": " + _trackName);
                    break;
                }
            }

            return buffer;
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

        //private void RefreshRowNumbers(int index)
        //{
        //    for (int ii = index+1; ii < dataGridView1.Rows.Count; ii++)
        //        dataGridView1.Rows[ii].HeaderCell.Value = (ii + 1).ToString();
        //}
        #endregion

        private void textBoxTrackLength_TextChanged(object sender, EventArgs e)
        {
        }
    }

    #region Class NoteColors

    class NoteColors
    {
        public class NoteColor
        {
            public Color c;
        }

        static public List<NoteColor> colors = new List<NoteColor>();

        public NoteColors()
        {
            if (colors == null)
                return;

            colors.Clear();

            for (int ii = 0; ii < FormFixTracks.MAX_NOTE_COLORS; ii++)
                colors.Add(new NoteColor());

            if (colors.Count != FormFixTracks.MAX_NOTE_COLORS)
                return;

            colors[0].c = Color.FromArgb(0, 0, 128);
            colors[1].c = Color.FromArgb(0, 128, 0);
            colors[2].c = Color.FromArgb(0, 128, 128);
            colors[3].c = Color.FromArgb(128, 0, 0);
            colors[4].c = Color.FromArgb(128, 0, 128);
            colors[5].c = Color.FromArgb(128, 128, 0);
            colors[6].c = Color.FromArgb(128, 128, 255);
            colors[7].c = Color.FromArgb(128, 255, 128);
            colors[8].c = Color.FromArgb(128, 255, 255);
            colors[9].c = Color.FromArgb(255, 128, 128);
            colors[10].c = Color.FromArgb(255, 128, 255);
            colors[11].c = Color.FromArgb(255, 255, 128);
        }
    }
    #endregion

    #region CodeSnippets

    //public int GetRogueNotes()
    //{
    //    // we add 2 ints to each new list-item: note-event-index, note-number
    //    var onEvents = new List<uint[]>();
    //    var offEvents = new List<uint[]>();

    //    // put bytes into buffer
    //    byte[] buffer = EventsToByteArray(dataGridView1.Rows.Count * 4);

    //    if (buffer == null || buffer.Length < 4)
    //        return -3; // error

    //    int length = buffer.Length;


    //    // b0 => +3 timestamp
    //    // b1 => +2 event (midi-channel in low nibble)
    //    // b2 => +1 note
    //    // b3 => +0 velocity (0 if note-off)

    //    // go through track and create seperate lists for note-on and note-off events
    //    for (uint ii = 0; ii < length; ii += 4)
    //    {
    //        bool bNoteOn = (buffer[ii + 2] & 0xf0) == 0x90;
    //        byte velocity = buffer[ii + 0];
    //        byte note = buffer[ii + 1];

    //        if (bNoteOn && velocity != 0) // note on
    //            onEvents.Add(new uint[] { ii/4, note });
    //        else if (bNoteOn && velocity == 0 || (buffer[ii + 2] & 0xf0) == 0x80) // note off
    //            offEvents.Add(new uint[] { ii/4, note });
    //    }

    //    // now what??? need to weed out good note-on/off pairs from lists - start at 0 in note-ons
    //    // and search for first corresponding note-off that occurred at higher event-index than this.            

    //    for (int ii = 0; ii < onEvents.Count; ii++)
    //    {
    //        uint onIdx = onEvents[ii][0];
    //        byte onNote = (byte)onEvents[ii][1];

    //        for (int jj = 0; jj < offEvents.Count; jj++)
    //        {
    //            if (offEvents[jj][0] > onIdx && offEvents[jj][1] == onNote)
    //            {
    //                // note: decrement the loop indexes since we remove items, successive items are moved up!
    //                onEvents.RemoveAt(ii--);
    //                offEvents.RemoveAt(jj--);
    //                break;
    //            }
    //        }
    //    }

    //    // what we are left with is the orphaned on/off notes
    //    if (onEvents.Count > 0 || offEvents.Count > 0)
    //    {
    //        string s1 = "";

    //        if (onEvents.Count > 0)
    //        {
    //            s1 = "orphaned on: " + onEvents.Count + "\n";

    //            foreach (uint[] val in onEvents)
    //                s1 += val[0] + " = " + String.Format("{{0:X2}\n", val[1]);
    //        }

    //        string s2 = "";

    //        if (offEvents.Count > 0)
    //        {
    //            s2 += "orphaned off: " + offEvents.Count + "\n";

    //            foreach (uint[] val in offEvents)
    //                s2 += val[0] + " = " + String.Format("{0:X2}\n", val[1]);
    //        }

    //        MessageBox.Show(s1 + s2);
    //    }

    //    return -1;
    //}
    #endregion
}

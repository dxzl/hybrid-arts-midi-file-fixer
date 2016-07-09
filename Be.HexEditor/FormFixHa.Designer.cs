namespace Be.HexEditor
{
    partial class FormFixHybridArts
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormFixHybridArts));
            this.panelButtons = new System.Windows.Forms.Panel();
            this.buttonDeleteSelected = new System.Windows.Forms.Button();
            this.textBoxGoodTrackCount = new System.Windows.Forms.TextBox();
            this.labelGoodTrackCount = new System.Windows.Forms.Label();
            this.buttonResetTracks = new System.Windows.Forms.Button();
            this.textBoxTotalTracksInSong = new System.Windows.Forms.TextBox();
            this.buttonExamineSelected = new System.Windows.Forms.Button();
            this.labelTotalTracksInSong = new System.Windows.Forms.Label();
            this.buttonFixTracks = new System.Windows.Forms.Button();
            this.labelOffsets = new System.Windows.Forms.Label();
            this.labelLengths = new System.Windows.Forms.Label();
            this.textBoxTotalTracksLength = new System.Windows.Forms.TextBox();
            this.labelTotalTrackLength = new System.Windows.Forms.Label();
            this.buttonFixOffsetsAndLengths = new System.Windows.Forms.Button();
            this.buttonExit = new System.Windows.Forms.Button();
            this.panelMain = new System.Windows.Forms.Panel();
            this.checkBoxUseOldFormat = new System.Windows.Forms.CheckBox();
            this.textBoxTracksFileOffset = new System.Windows.Forms.TextBox();
            this.labelTracksFileOffset = new System.Windows.Forms.Label();
            this.textBoxBaseAddress = new System.Windows.Forms.TextBox();
            this.labelBaseAddress = new System.Windows.Forms.Label();
            this.textBoxTrackOffsets = new System.Windows.Forms.TextBox();
            this.textBoxTrackLengths = new System.Windows.Forms.TextBox();
            this.labelTrackOffsets = new System.Windows.Forms.Label();
            this.labelTrackLengths = new System.Windows.Forms.Label();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.TrackName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TrackLengths = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TrackOffsets = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FileOffsets = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TimeTicks = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TimeFix = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panelButtons.SuspendLayout();
            this.panelMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // panelButtons
            // 
            this.panelButtons.Controls.Add(this.buttonDeleteSelected);
            this.panelButtons.Controls.Add(this.textBoxGoodTrackCount);
            this.panelButtons.Controls.Add(this.labelGoodTrackCount);
            this.panelButtons.Controls.Add(this.buttonResetTracks);
            this.panelButtons.Controls.Add(this.textBoxTotalTracksInSong);
            this.panelButtons.Controls.Add(this.buttonExamineSelected);
            this.panelButtons.Controls.Add(this.labelTotalTracksInSong);
            this.panelButtons.Controls.Add(this.buttonFixTracks);
            this.panelButtons.Controls.Add(this.labelOffsets);
            this.panelButtons.Controls.Add(this.labelLengths);
            this.panelButtons.Controls.Add(this.textBoxTotalTracksLength);
            this.panelButtons.Controls.Add(this.labelTotalTrackLength);
            this.panelButtons.Controls.Add(this.buttonFixOffsetsAndLengths);
            this.panelButtons.Controls.Add(this.buttonExit);
            this.panelButtons.Dock = System.Windows.Forms.DockStyle.Right;
            this.panelButtons.Location = new System.Drawing.Point(630, 0);
            this.panelButtons.Name = "panelButtons";
            this.panelButtons.Size = new System.Drawing.Size(212, 579);
            this.panelButtons.TabIndex = 1;
            // 
            // buttonDeleteSelected
            // 
            this.buttonDeleteSelected.Enabled = false;
            this.buttonDeleteSelected.Location = new System.Drawing.Point(3, 323);
            this.buttonDeleteSelected.Name = "buttonDeleteSelected";
            this.buttonDeleteSelected.Size = new System.Drawing.Size(206, 23);
            this.buttonDeleteSelected.TabIndex = 36;
            this.buttonDeleteSelected.Text = "&Delete Selected Track";
            this.buttonDeleteSelected.UseVisualStyleBackColor = true;
            this.buttonDeleteSelected.Click += new System.EventHandler(this.buttonDeleteSelected_Click);
            // 
            // textBoxGoodTrackCount
            // 
            this.textBoxGoodTrackCount.Location = new System.Drawing.Point(9, 87);
            this.textBoxGoodTrackCount.Name = "textBoxGoodTrackCount";
            this.textBoxGoodTrackCount.ReadOnly = true;
            this.textBoxGoodTrackCount.Size = new System.Drawing.Size(90, 20);
            this.textBoxGoodTrackCount.TabIndex = 35;
            // 
            // labelGoodTrackCount
            // 
            this.labelGoodTrackCount.AutoSize = true;
            this.labelGoodTrackCount.Location = new System.Drawing.Point(6, 71);
            this.labelGoodTrackCount.Name = "labelGoodTrackCount";
            this.labelGoodTrackCount.Size = new System.Drawing.Size(72, 13);
            this.labelGoodTrackCount.TabIndex = 34;
            this.labelGoodTrackCount.Text = "Good Tracks:";
            // 
            // buttonResetTracks
            // 
            this.buttonResetTracks.Enabled = false;
            this.buttonResetTracks.Location = new System.Drawing.Point(3, 113);
            this.buttonResetTracks.Name = "buttonResetTracks";
            this.buttonResetTracks.Size = new System.Drawing.Size(206, 23);
            this.buttonResetTracks.TabIndex = 33;
            this.buttonResetTracks.Text = "&Reset Tracks";
            this.buttonResetTracks.UseVisualStyleBackColor = true;
            this.buttonResetTracks.Click += new System.EventHandler(this.buttonReset_Click);
            // 
            // textBoxTotalTracksInSong
            // 
            this.textBoxTotalTracksInSong.Location = new System.Drawing.Point(9, 32);
            this.textBoxTotalTracksInSong.Name = "textBoxTotalTracksInSong";
            this.textBoxTotalTracksInSong.ReadOnly = true;
            this.textBoxTotalTracksInSong.Size = new System.Drawing.Size(90, 20);
            this.textBoxTotalTracksInSong.TabIndex = 14;
            // 
            // buttonExamineSelected
            // 
            this.buttonExamineSelected.Enabled = false;
            this.buttonExamineSelected.Location = new System.Drawing.Point(3, 294);
            this.buttonExamineSelected.Name = "buttonExamineSelected";
            this.buttonExamineSelected.Size = new System.Drawing.Size(206, 23);
            this.buttonExamineSelected.TabIndex = 32;
            this.buttonExamineSelected.Text = "E&xamine Selected Track";
            this.buttonExamineSelected.UseVisualStyleBackColor = true;
            this.buttonExamineSelected.Click += new System.EventHandler(this.buttonExamineTrack_Click);
            // 
            // labelTotalTracksInSong
            // 
            this.labelTotalTracksInSong.AutoSize = true;
            this.labelTotalTracksInSong.Location = new System.Drawing.Point(6, 16);
            this.labelTotalTracksInSong.Name = "labelTotalTracksInSong";
            this.labelTotalTracksInSong.Size = new System.Drawing.Size(83, 13);
            this.labelTotalTracksInSong.TabIndex = 13;
            this.labelTotalTracksInSong.Text = "Tracks In Song:";
            // 
            // buttonFixTracks
            // 
            this.buttonFixTracks.Enabled = false;
            this.buttonFixTracks.Location = new System.Drawing.Point(3, 191);
            this.buttonFixTracks.Name = "buttonFixTracks";
            this.buttonFixTracks.Size = new System.Drawing.Size(206, 23);
            this.buttonFixTracks.TabIndex = 31;
            this.buttonFixTracks.Text = "Fix &Tracks";
            this.buttonFixTracks.UseVisualStyleBackColor = true;
            this.buttonFixTracks.Click += new System.EventHandler(this.buttonFixTracks_Click);
            // 
            // labelOffsets
            // 
            this.labelOffsets.AutoSize = true;
            this.labelOffsets.Location = new System.Drawing.Point(109, 402);
            this.labelOffsets.Name = "labelOffsets";
            this.labelOffsets.Size = new System.Drawing.Size(43, 13);
            this.labelOffsets.TabIndex = 30;
            this.labelOffsets.Text = "Offsets:";
            // 
            // labelLengths
            // 
            this.labelLengths.AutoSize = true;
            this.labelLengths.Location = new System.Drawing.Point(6, 402);
            this.labelLengths.Name = "labelLengths";
            this.labelLengths.Size = new System.Drawing.Size(48, 13);
            this.labelLengths.TabIndex = 29;
            this.labelLengths.Text = "Lengths:";
            // 
            // textBoxTotalTracksLength
            // 
            this.textBoxTotalTracksLength.Location = new System.Drawing.Point(112, 32);
            this.textBoxTotalTracksLength.Name = "textBoxTotalTracksLength";
            this.textBoxTotalTracksLength.ReadOnly = true;
            this.textBoxTotalTracksLength.Size = new System.Drawing.Size(90, 20);
            this.textBoxTotalTracksLength.TabIndex = 28;
            // 
            // labelTotalTrackLength
            // 
            this.labelTotalTrackLength.AutoSize = true;
            this.labelTotalTrackLength.Location = new System.Drawing.Point(109, 16);
            this.labelTotalTrackLength.Name = "labelTotalTrackLength";
            this.labelTotalTrackLength.Size = new System.Drawing.Size(93, 13);
            this.labelTotalTrackLength.TabIndex = 27;
            this.labelTotalTrackLength.Text = "Combined Length:";
            // 
            // buttonFixOffsetsAndLengths
            // 
            this.buttonFixOffsetsAndLengths.Location = new System.Drawing.Point(3, 162);
            this.buttonFixOffsetsAndLengths.Name = "buttonFixOffsetsAndLengths";
            this.buttonFixOffsetsAndLengths.Size = new System.Drawing.Size(206, 23);
            this.buttonFixOffsetsAndLengths.TabIndex = 22;
            this.buttonFixOffsetsAndLengths.Text = "Fix &Offsets And Lengths";
            this.buttonFixOffsetsAndLengths.UseVisualStyleBackColor = true;
            this.buttonFixOffsetsAndLengths.Click += new System.EventHandler(this.buttonFixOffsetsAndLengths_Click);
            // 
            // buttonExit
            // 
            this.buttonExit.Location = new System.Drawing.Point(3, 367);
            this.buttonExit.Name = "buttonExit";
            this.buttonExit.Size = new System.Drawing.Size(206, 23);
            this.buttonExit.TabIndex = 19;
            this.buttonExit.Text = "&Exit";
            this.buttonExit.UseVisualStyleBackColor = true;
            this.buttonExit.Click += new System.EventHandler(this.buttonQuit_Click);
            // 
            // panelMain
            // 
            this.panelMain.Controls.Add(this.checkBoxUseOldFormat);
            this.panelMain.Controls.Add(this.textBoxTracksFileOffset);
            this.panelMain.Controls.Add(this.labelTracksFileOffset);
            this.panelMain.Controls.Add(this.textBoxBaseAddress);
            this.panelMain.Controls.Add(this.labelBaseAddress);
            this.panelMain.Controls.Add(this.textBoxTrackOffsets);
            this.panelMain.Controls.Add(this.textBoxTrackLengths);
            this.panelMain.Controls.Add(this.labelTrackOffsets);
            this.panelMain.Controls.Add(this.labelTrackLengths);
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelMain.Location = new System.Drawing.Point(0, 0);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(630, 71);
            this.panelMain.TabIndex = 11;
            // 
            // checkBoxUseOldFormat
            // 
            this.checkBoxUseOldFormat.AutoSize = true;
            this.checkBoxUseOldFormat.Checked = true;
            this.checkBoxUseOldFormat.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxUseOldFormat.Location = new System.Drawing.Point(448, 35);
            this.checkBoxUseOldFormat.Name = "checkBoxUseOldFormat";
            this.checkBoxUseOldFormat.Size = new System.Drawing.Size(128, 17);
            this.checkBoxUseOldFormat.TabIndex = 34;
            this.checkBoxUseOldFormat.Text = "Use Old .SNG Format";
            this.checkBoxUseOldFormat.UseVisualStyleBackColor = true;
            this.checkBoxUseOldFormat.CheckedChanged += new System.EventHandler(this.checkBoxUseSyncTrackOffsets_CheckedChanged);
            // 
            // textBoxTracksFileOffset
            // 
            this.textBoxTracksFileOffset.Location = new System.Drawing.Point(333, 32);
            this.textBoxTracksFileOffset.Name = "textBoxTracksFileOffset";
            this.textBoxTracksFileOffset.Size = new System.Drawing.Size(90, 20);
            this.textBoxTracksFileOffset.TabIndex = 12;
            // 
            // labelTracksFileOffset
            // 
            this.labelTracksFileOffset.AutoSize = true;
            this.labelTracksFileOffset.Location = new System.Drawing.Point(330, 16);
            this.labelTracksFileOffset.Name = "labelTracksFileOffset";
            this.labelTracksFileOffset.Size = new System.Drawing.Size(93, 13);
            this.labelTracksFileOffset.TabIndex = 11;
            this.labelTracksFileOffset.Text = "Tracks File-Offset:";
            // 
            // textBoxBaseAddress
            // 
            this.textBoxBaseAddress.Location = new System.Drawing.Point(237, 32);
            this.textBoxBaseAddress.Name = "textBoxBaseAddress";
            this.textBoxBaseAddress.Size = new System.Drawing.Size(90, 20);
            this.textBoxBaseAddress.TabIndex = 10;
            // 
            // labelBaseAddress
            // 
            this.labelBaseAddress.AutoSize = true;
            this.labelBaseAddress.Location = new System.Drawing.Point(234, 16);
            this.labelBaseAddress.Name = "labelBaseAddress";
            this.labelBaseAddress.Size = new System.Drawing.Size(75, 13);
            this.labelBaseAddress.TabIndex = 9;
            this.labelBaseAddress.Text = "Base Address:";
            // 
            // textBoxTrackOffsets
            // 
            this.textBoxTrackOffsets.Location = new System.Drawing.Point(99, 32);
            this.textBoxTrackOffsets.Name = "textBoxTrackOffsets";
            this.textBoxTrackOffsets.Size = new System.Drawing.Size(90, 20);
            this.textBoxTrackOffsets.TabIndex = 8;
            // 
            // textBoxTrackLengths
            // 
            this.textBoxTrackLengths.Location = new System.Drawing.Point(3, 32);
            this.textBoxTrackLengths.Name = "textBoxTrackLengths";
            this.textBoxTrackLengths.Size = new System.Drawing.Size(90, 20);
            this.textBoxTrackLengths.TabIndex = 7;
            // 
            // labelTrackOffsets
            // 
            this.labelTrackOffsets.AutoSize = true;
            this.labelTrackOffsets.Location = new System.Drawing.Point(99, 16);
            this.labelTrackOffsets.Name = "labelTrackOffsets";
            this.labelTrackOffsets.Size = new System.Drawing.Size(90, 13);
            this.labelTrackOffsets.TabIndex = 6;
            this.labelTrackOffsets.Text = "Addresses Offset:";
            // 
            // labelTrackLengths
            // 
            this.labelTrackLengths.AutoSize = true;
            this.labelTrackLengths.Location = new System.Drawing.Point(3, 16);
            this.labelTrackLengths.Name = "labelTrackLengths";
            this.labelTrackLengths.Size = new System.Drawing.Size(79, 13);
            this.labelTrackLengths.TabIndex = 5;
            this.labelTrackLengths.Text = "Lengths Offset:";
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToResizeColumns = false;
            this.dataGridView1.AllowUserToResizeRows = false;
            this.dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.ColumnHeader;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.TrackName,
            this.TrackLengths,
            this.TrackOffsets,
            this.FileOffsets,
            this.TimeTicks,
            this.TimeFix});
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(0, 71);
            this.dataGridView1.MultiSelect = false;
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.Size = new System.Drawing.Size(630, 508);
            this.dataGridView1.TabIndex = 12;
            // 
            // TrackName
            // 
            this.TrackName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.TrackName.HeaderText = "Track Name";
            this.TrackName.Name = "TrackName";
            // 
            // TrackLengths
            // 
            this.TrackLengths.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.TrackLengths.HeaderText = "Length";
            this.TrackLengths.Name = "TrackLengths";
            // 
            // TrackOffsets
            // 
            this.TrackOffsets.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.TrackOffsets.HeaderText = "Offset";
            this.TrackOffsets.Name = "TrackOffsets";
            // 
            // FileOffsets
            // 
            this.FileOffsets.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.FileOffsets.HeaderText = "File Offset";
            this.FileOffsets.Name = "FileOffsets";
            // 
            // TimeTicks
            // 
            this.TimeTicks.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.TimeTicks.HeaderText = "Time Ticks";
            this.TimeTicks.Name = "TimeTicks";
            // 
            // TimeFix
            // 
            this.TimeFix.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.TimeFix.HeaderText = "Time Fix";
            this.TimeFix.Name = "TimeFix";
            this.TimeFix.ReadOnly = true;
            // 
            // FormFixHybridArts
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(842, 579);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.panelMain);
            this.Controls.Add(this.panelButtons);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormFixHybridArts";
            this.Text = "Fix Hybrid Arts Midi SNG Files";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormFixHybridArts_FormClosing);
            this.Load += new System.EventHandler(this.FormFixHybridArts_Load);
            this.panelButtons.ResumeLayout(false);
            this.panelButtons.PerformLayout();
            this.panelMain.ResumeLayout(false);
            this.panelMain.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel panelButtons;
        private System.Windows.Forms.Button buttonFixOffsetsAndLengths;
        private System.Windows.Forms.Button buttonExit;
        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.TextBox textBoxTracksFileOffset;
        private System.Windows.Forms.Label labelTracksFileOffset;
        private System.Windows.Forms.TextBox textBoxBaseAddress;
        private System.Windows.Forms.Label labelBaseAddress;
        private System.Windows.Forms.TextBox textBoxTrackOffsets;
        private System.Windows.Forms.TextBox textBoxTrackLengths;
        private System.Windows.Forms.Label labelTrackOffsets;
        private System.Windows.Forms.Label labelTrackLengths;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.TextBox textBoxTotalTracksLength;
        private System.Windows.Forms.Label labelTotalTrackLength;
        private System.Windows.Forms.Label labelLengths;
        private System.Windows.Forms.Label labelOffsets;
        private System.Windows.Forms.Button buttonFixTracks;
        private System.Windows.Forms.TextBox textBoxTotalTracksInSong;
        private System.Windows.Forms.Label labelTotalTracksInSong;
        private System.Windows.Forms.Button buttonExamineSelected;
        private System.Windows.Forms.Button buttonResetTracks;
        private System.Windows.Forms.CheckBox checkBoxUseOldFormat;
        private System.Windows.Forms.TextBox textBoxGoodTrackCount;
        private System.Windows.Forms.Label labelGoodTrackCount;
        private System.Windows.Forms.Button buttonDeleteSelected;
        private System.Windows.Forms.DataGridViewTextBoxColumn TrackName;
        private System.Windows.Forms.DataGridViewTextBoxColumn TrackLengths;
        private System.Windows.Forms.DataGridViewTextBoxColumn TrackOffsets;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileOffsets;
        private System.Windows.Forms.DataGridViewTextBoxColumn TimeTicks;
        private System.Windows.Forms.DataGridViewTextBoxColumn TimeFix;
    }
}
namespace Be.HexEditor
{
    partial class FormFixTracks
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormFixTracks));
            this.panelButtons = new System.Windows.Forms.Panel();
            this.buttonSyncLengthToList = new System.Windows.Forms.Button();
            this.buttonReset = new System.Windows.Forms.Button();
            this.labelResultantDivisor = new System.Windows.Forms.Label();
            this.labelResultantVariation = new System.Windows.Forms.Label();
            this.labelFixTimingVariation = new System.Windows.Forms.Label();
            this.textBoxFixTimingVariation = new System.Windows.Forms.TextBox();
            this.labelFixTimingDivisor = new System.Windows.Forms.Label();
            this.textBoxFixTimingDivisor = new System.Windows.Forms.TextBox();
            this.textBoxResumeAt = new System.Windows.Forms.TextBox();
            this.labelResumeAt = new System.Windows.Forms.Label();
            this.buttonInsert = new System.Windows.Forms.Button();
            this.buttonDelete = new System.Windows.Forms.Button();
            this.checkBoxAutoInsert0D = new System.Windows.Forms.CheckBox();
            this.buttonAutoFixEvents = new System.Windows.Forms.Button();
            this.labelHelp = new System.Windows.Forms.Label();
            this.textBoxFilters = new System.Windows.Forms.TextBox();
            this.buttonOk = new System.Windows.Forms.Button();
            this.labelFilters = new System.Windows.Forms.Label();
            this.buttonReapplyFilters = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.labelEndOfTrackShift = new System.Windows.Forms.Label();
            this.panelMain = new System.Windows.Forms.Panel();
            this.textBoxTicksPerBeat = new System.Windows.Forms.TextBox();
            this.labelTicksPerBeat = new System.Windows.Forms.Label();
            this.textBoxFileOffset = new System.Windows.Forms.TextBox();
            this.labelTracksFileOffset = new System.Windows.Forms.Label();
            this.textBoxMissingByteCount = new System.Windows.Forms.TextBox();
            this.labelMissingByteCount = new System.Windows.Forms.Label();
            this.textBoxTrackLength = new System.Windows.Forms.TextBox();
            this.textBoxTableOffset = new System.Windows.Forms.TextBox();
            this.labelTrackLength = new System.Windows.Forms.Label();
            this.labelTableOffset = new System.Windows.Forms.Label();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.TrackName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CumulativeTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DeltaTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.NoteOnTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panelButtons.SuspendLayout();
            this.panelMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // panelButtons
            // 
            this.panelButtons.Controls.Add(this.buttonSyncLengthToList);
            this.panelButtons.Controls.Add(this.buttonReset);
            this.panelButtons.Controls.Add(this.labelResultantDivisor);
            this.panelButtons.Controls.Add(this.labelResultantVariation);
            this.panelButtons.Controls.Add(this.labelFixTimingVariation);
            this.panelButtons.Controls.Add(this.textBoxFixTimingVariation);
            this.panelButtons.Controls.Add(this.labelFixTimingDivisor);
            this.panelButtons.Controls.Add(this.textBoxFixTimingDivisor);
            this.panelButtons.Controls.Add(this.textBoxResumeAt);
            this.panelButtons.Controls.Add(this.labelResumeAt);
            this.panelButtons.Controls.Add(this.buttonInsert);
            this.panelButtons.Controls.Add(this.buttonDelete);
            this.panelButtons.Controls.Add(this.checkBoxAutoInsert0D);
            this.panelButtons.Controls.Add(this.buttonAutoFixEvents);
            this.panelButtons.Controls.Add(this.labelHelp);
            this.panelButtons.Controls.Add(this.textBoxFilters);
            this.panelButtons.Controls.Add(this.buttonOk);
            this.panelButtons.Controls.Add(this.labelFilters);
            this.panelButtons.Controls.Add(this.buttonReapplyFilters);
            this.panelButtons.Controls.Add(this.buttonCancel);
            this.panelButtons.Dock = System.Windows.Forms.DockStyle.Right;
            this.panelButtons.Location = new System.Drawing.Point(630, 0);
            this.panelButtons.Name = "panelButtons";
            this.panelButtons.Size = new System.Drawing.Size(212, 579);
            this.panelButtons.TabIndex = 1;
            // 
            // buttonSyncLengthToList
            // 
            this.buttonSyncLengthToList.Location = new System.Drawing.Point(3, 195);
            this.buttonSyncLengthToList.Name = "buttonSyncLengthToList";
            this.buttonSyncLengthToList.Size = new System.Drawing.Size(206, 23);
            this.buttonSyncLengthToList.TabIndex = 54;
            this.buttonSyncLengthToList.Text = "&Sync Track Length To List";
            this.buttonSyncLengthToList.UseVisualStyleBackColor = true;
            this.buttonSyncLengthToList.Click += new System.EventHandler(this.buttonSyncLengthToList_Click);
            // 
            // buttonReset
            // 
            this.buttonReset.Location = new System.Drawing.Point(3, 76);
            this.buttonReset.Name = "buttonReset";
            this.buttonReset.Size = new System.Drawing.Size(206, 26);
            this.buttonReset.TabIndex = 53;
            this.buttonReset.Text = "&Reset All";
            this.buttonReset.UseVisualStyleBackColor = true;
            this.buttonReset.Click += new System.EventHandler(this.buttonReset_Click);
            // 
            // labelResultantDivisor
            // 
            this.labelResultantDivisor.AutoSize = true;
            this.labelResultantDivisor.Location = new System.Drawing.Point(13, 312);
            this.labelResultantDivisor.Name = "labelResultantDivisor";
            this.labelResultantDivisor.Size = new System.Drawing.Size(25, 13);
            this.labelResultantDivisor.TabIndex = 52;
            this.labelResultantDivisor.Text = "(nn)";
            // 
            // labelResultantVariation
            // 
            this.labelResultantVariation.AutoSize = true;
            this.labelResultantVariation.Location = new System.Drawing.Point(13, 286);
            this.labelResultantVariation.Name = "labelResultantVariation";
            this.labelResultantVariation.Size = new System.Drawing.Size(25, 13);
            this.labelResultantVariation.TabIndex = 51;
            this.labelResultantVariation.Text = "(nn)";
            // 
            // labelFixTimingVariation
            // 
            this.labelFixTimingVariation.AutoSize = true;
            this.labelFixTimingVariation.Location = new System.Drawing.Point(92, 286);
            this.labelFixTimingVariation.Name = "labelFixTimingVariation";
            this.labelFixTimingVariation.Size = new System.Drawing.Size(115, 13);
            this.labelFixTimingVariation.TabIndex = 50;
            this.labelFixTimingVariation.Text = "Timing \"Max Variation\"";
            // 
            // textBoxFixTimingVariation
            // 
            this.textBoxFixTimingVariation.Location = new System.Drawing.Point(52, 309);
            this.textBoxFixTimingVariation.Name = "textBoxFixTimingVariation";
            this.textBoxFixTimingVariation.Size = new System.Drawing.Size(35, 20);
            this.textBoxFixTimingVariation.TabIndex = 49;
            this.textBoxFixTimingVariation.Text = "6";
            this.textBoxFixTimingVariation.TextChanged += new System.EventHandler(this.textBoxFixTimingVariation_TextChanged);
            // 
            // labelFixTimingDivisor
            // 
            this.labelFixTimingDivisor.AutoSize = true;
            this.labelFixTimingDivisor.Location = new System.Drawing.Point(92, 312);
            this.labelFixTimingDivisor.Name = "labelFixTimingDivisor";
            this.labelFixTimingDivisor.Size = new System.Drawing.Size(103, 13);
            this.labelFixTimingDivisor.TabIndex = 48;
            this.labelFixTimingDivisor.Text = "Timing \"Min Divisor\"";
            // 
            // textBoxFixTimingDivisor
            // 
            this.textBoxFixTimingDivisor.Location = new System.Drawing.Point(52, 283);
            this.textBoxFixTimingDivisor.Name = "textBoxFixTimingDivisor";
            this.textBoxFixTimingDivisor.Size = new System.Drawing.Size(35, 20);
            this.textBoxFixTimingDivisor.TabIndex = 47;
            this.textBoxFixTimingDivisor.Text = "24";
            this.textBoxFixTimingDivisor.TextChanged += new System.EventHandler(this.textBoxFixTimingDivisor_TextChanged);
            // 
            // textBoxResumeAt
            // 
            this.textBoxResumeAt.Location = new System.Drawing.Point(127, 489);
            this.textBoxResumeAt.MaxLength = 10;
            this.textBoxResumeAt.Name = "textBoxResumeAt";
            this.textBoxResumeAt.Size = new System.Drawing.Size(68, 20);
            this.textBoxResumeAt.TabIndex = 45;
            this.textBoxResumeAt.Text = "1";
            this.textBoxResumeAt.TextChanged += new System.EventHandler(this.textBoxResumeAt_TextChanged);
            // 
            // labelResumeAt
            // 
            this.labelResumeAt.AutoSize = true;
            this.labelResumeAt.Location = new System.Drawing.Point(6, 473);
            this.labelResumeAt.Name = "labelResumeAt";
            this.labelResumeAt.Size = new System.Drawing.Size(173, 13);
            this.labelResumeAt.TabIndex = 44;
            this.labelResumeAt.Text = "Resume event/note/timing fixes at:";
            // 
            // buttonInsert
            // 
            this.buttonInsert.Location = new System.Drawing.Point(3, 166);
            this.buttonInsert.Name = "buttonInsert";
            this.buttonInsert.Size = new System.Drawing.Size(206, 23);
            this.buttonInsert.TabIndex = 43;
            this.buttonInsert.Text = "&Insert Before Selected";
            this.buttonInsert.UseVisualStyleBackColor = true;
            this.buttonInsert.Click += new System.EventHandler(this.buttonInsert_Click);
            // 
            // buttonDelete
            // 
            this.buttonDelete.Location = new System.Drawing.Point(3, 137);
            this.buttonDelete.Name = "buttonDelete";
            this.buttonDelete.Size = new System.Drawing.Size(206, 23);
            this.buttonDelete.TabIndex = 42;
            this.buttonDelete.Text = "&Delete Selected";
            this.buttonDelete.UseVisualStyleBackColor = true;
            this.buttonDelete.Click += new System.EventHandler(this.buttonDelete_Click);
            // 
            // checkBoxAutoInsert0D
            // 
            this.checkBoxAutoInsert0D.AutoSize = true;
            this.checkBoxAutoInsert0D.Checked = true;
            this.checkBoxAutoInsert0D.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxAutoInsert0D.Location = new System.Drawing.Point(9, 51);
            this.checkBoxAutoInsert0D.Name = "checkBoxAutoInsert0D";
            this.checkBoxAutoInsert0D.Size = new System.Drawing.Size(94, 17);
            this.checkBoxAutoInsert0D.TabIndex = 40;
            this.checkBoxAutoInsert0D.Text = "Auto Insert 0D";
            this.checkBoxAutoInsert0D.UseVisualStyleBackColor = true;
            // 
            // buttonAutoFixEvents
            // 
            this.buttonAutoFixEvents.Location = new System.Drawing.Point(3, 254);
            this.buttonAutoFixEvents.Name = "buttonAutoFixEvents";
            this.buttonAutoFixEvents.Size = new System.Drawing.Size(206, 23);
            this.buttonAutoFixEvents.TabIndex = 39;
            this.buttonAutoFixEvents.Text = "Auto Fix &Events";
            this.buttonAutoFixEvents.UseVisualStyleBackColor = true;
            this.buttonAutoFixEvents.Click += new System.EventHandler(this.buttonAutoFixEvents_Click);
            // 
            // labelHelp
            // 
            this.labelHelp.AutoSize = true;
            this.labelHelp.Location = new System.Drawing.Point(11, 341);
            this.labelHelp.Name = "labelHelp";
            this.labelHelp.Size = new System.Drawing.Size(189, 117);
            this.labelHelp.TabIndex = 35;
            this.labelHelp.Text = resources.GetString("labelHelp.Text");
            // 
            // textBoxFilters
            // 
            this.textBoxFilters.Location = new System.Drawing.Point(3, 25);
            this.textBoxFilters.Name = "textBoxFilters";
            this.textBoxFilters.Size = new System.Drawing.Size(206, 20);
            this.textBoxFilters.TabIndex = 33;
            this.textBoxFilters.TextChanged += new System.EventHandler(this.textBoxFilters_TextChanged);
            // 
            // buttonOk
            // 
            this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOk.Location = new System.Drawing.Point(3, 515);
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size(206, 23);
            this.buttonOk.TabIndex = 32;
            this.buttonOk.Text = "&Ok";
            this.buttonOk.UseVisualStyleBackColor = true;
            this.buttonOk.Click += new System.EventHandler(this.buttonOk_Click);
            // 
            // labelFilters
            // 
            this.labelFilters.AutoSize = true;
            this.labelFilters.Location = new System.Drawing.Point(3, 9);
            this.labelFilters.Name = "labelFilters";
            this.labelFilters.Size = new System.Drawing.Size(199, 13);
            this.labelFilters.TabIndex = 34;
            this.labelFilters.Text = "Allowed Event Codes (space seperated):";
            // 
            // buttonReapplyFilters
            // 
            this.buttonReapplyFilters.Location = new System.Drawing.Point(3, 108);
            this.buttonReapplyFilters.Name = "buttonReapplyFilters";
            this.buttonReapplyFilters.Size = new System.Drawing.Size(206, 23);
            this.buttonReapplyFilters.TabIndex = 21;
            this.buttonReapplyFilters.Text = "Reapply &Filters";
            this.buttonReapplyFilters.UseVisualStyleBackColor = true;
            this.buttonReapplyFilters.Click += new System.EventHandler(this.buttonReapplyFilters_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(3, 544);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(206, 23);
            this.buttonCancel.TabIndex = 19;
            this.buttonCancel.Text = "&Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // labelEndOfTrackShift
            // 
            this.labelEndOfTrackShift.AutoSize = true;
            this.labelEndOfTrackShift.ForeColor = System.Drawing.Color.OrangeRed;
            this.labelEndOfTrackShift.Location = new System.Drawing.Point(12, 55);
            this.labelEndOfTrackShift.Name = "labelEndOfTrackShift";
            this.labelEndOfTrackShift.Size = new System.Drawing.Size(89, 13);
            this.labelEndOfTrackShift.TabIndex = 37;
            this.labelEndOfTrackShift.Text = "End-of-track Shift";
            // 
            // panelMain
            // 
            this.panelMain.Controls.Add(this.textBoxTicksPerBeat);
            this.panelMain.Controls.Add(this.labelTicksPerBeat);
            this.panelMain.Controls.Add(this.textBoxFileOffset);
            this.panelMain.Controls.Add(this.labelTracksFileOffset);
            this.panelMain.Controls.Add(this.textBoxMissingByteCount);
            this.panelMain.Controls.Add(this.labelMissingByteCount);
            this.panelMain.Controls.Add(this.textBoxTrackLength);
            this.panelMain.Controls.Add(this.textBoxTableOffset);
            this.panelMain.Controls.Add(this.labelTrackLength);
            this.panelMain.Controls.Add(this.labelTableOffset);
            this.panelMain.Controls.Add(this.labelEndOfTrackShift);
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelMain.Location = new System.Drawing.Point(0, 0);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(630, 71);
            this.panelMain.TabIndex = 11;
            // 
            // textBoxTicksPerBeat
            // 
            this.textBoxTicksPerBeat.Location = new System.Drawing.Point(442, 25);
            this.textBoxTicksPerBeat.Name = "textBoxTicksPerBeat";
            this.textBoxTicksPerBeat.ReadOnly = true;
            this.textBoxTicksPerBeat.Size = new System.Drawing.Size(90, 20);
            this.textBoxTicksPerBeat.TabIndex = 14;
            // 
            // labelTicksPerBeat
            // 
            this.labelTicksPerBeat.AutoSize = true;
            this.labelTicksPerBeat.Location = new System.Drawing.Point(439, 9);
            this.labelTicksPerBeat.Name = "labelTicksPerBeat";
            this.labelTicksPerBeat.Size = new System.Drawing.Size(80, 13);
            this.labelTicksPerBeat.TabIndex = 13;
            this.labelTicksPerBeat.Text = "Ticks Per Beat:";
            // 
            // textBoxFileOffset
            // 
            this.textBoxFileOffset.Location = new System.Drawing.Point(99, 25);
            this.textBoxFileOffset.Name = "textBoxFileOffset";
            this.textBoxFileOffset.ReadOnly = true;
            this.textBoxFileOffset.Size = new System.Drawing.Size(90, 20);
            this.textBoxFileOffset.TabIndex = 12;
            // 
            // labelTracksFileOffset
            // 
            this.labelTracksFileOffset.AutoSize = true;
            this.labelTracksFileOffset.Location = new System.Drawing.Point(96, 9);
            this.labelTracksFileOffset.Name = "labelTracksFileOffset";
            this.labelTracksFileOffset.Size = new System.Drawing.Size(57, 13);
            this.labelTracksFileOffset.TabIndex = 11;
            this.labelTracksFileOffset.Text = "File Offset:";
            // 
            // textBoxMissingByteCount
            // 
            this.textBoxMissingByteCount.Location = new System.Drawing.Point(291, 25);
            this.textBoxMissingByteCount.Name = "textBoxMissingByteCount";
            this.textBoxMissingByteCount.ReadOnly = true;
            this.textBoxMissingByteCount.Size = new System.Drawing.Size(90, 20);
            this.textBoxMissingByteCount.TabIndex = 10;
            // 
            // labelMissingByteCount
            // 
            this.labelMissingByteCount.AutoSize = true;
            this.labelMissingByteCount.Location = new System.Drawing.Point(288, 9);
            this.labelMissingByteCount.Name = "labelMissingByteCount";
            this.labelMissingByteCount.Size = new System.Drawing.Size(100, 13);
            this.labelMissingByteCount.TabIndex = 9;
            this.labelMissingByteCount.Text = "Missing Byte Count:";
            // 
            // textBoxTrackLength
            // 
            this.textBoxTrackLength.Location = new System.Drawing.Point(195, 25);
            this.textBoxTrackLength.Name = "textBoxTrackLength";
            this.textBoxTrackLength.Size = new System.Drawing.Size(90, 20);
            this.textBoxTrackLength.TabIndex = 8;
            // 
            // textBoxTableOffset
            // 
            this.textBoxTableOffset.Location = new System.Drawing.Point(6, 25);
            this.textBoxTableOffset.Name = "textBoxTableOffset";
            this.textBoxTableOffset.ReadOnly = true;
            this.textBoxTableOffset.Size = new System.Drawing.Size(90, 20);
            this.textBoxTableOffset.TabIndex = 7;
            // 
            // labelTrackLength
            // 
            this.labelTrackLength.AutoSize = true;
            this.labelTrackLength.Location = new System.Drawing.Point(192, 9);
            this.labelTrackLength.Name = "labelTrackLength";
            this.labelTrackLength.Size = new System.Drawing.Size(74, 13);
            this.labelTrackLength.TabIndex = 6;
            this.labelTrackLength.Text = "Track Length:";
            // 
            // labelTableOffset
            // 
            this.labelTableOffset.AutoSize = true;
            this.labelTableOffset.Location = new System.Drawing.Point(3, 9);
            this.labelTableOffset.Name = "labelTableOffset";
            this.labelTableOffset.Size = new System.Drawing.Size(68, 13);
            this.labelTableOffset.TabIndex = 5;
            this.labelTableOffset.Text = "Table Offset:";
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToResizeColumns = false;
            this.dataGridView1.AllowUserToResizeRows = false;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.TrackName,
            this.CumulativeTime,
            this.DeltaTime,
            this.NoteOnTime});
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(0, 71);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.Size = new System.Drawing.Size(630, 508);
            this.dataGridView1.TabIndex = 12;
            // 
            // TrackName
            // 
            this.TrackName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.TrackName.HeaderText = "Raw Data";
            this.TrackName.MaxInputLength = 10;
            this.TrackName.MinimumWidth = 100;
            this.TrackName.Name = "TrackName";
            this.TrackName.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.TrackName.ToolTipText = "Raw event data (4-bytes)";
            // 
            // CumulativeTime
            // 
            this.CumulativeTime.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.CumulativeTime.HeaderText = "Cumulative Time (beats)";
            this.CumulativeTime.MinimumWidth = 100;
            this.CumulativeTime.Name = "CumulativeTime";
            this.CumulativeTime.ReadOnly = true;
            this.CumulativeTime.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.CumulativeTime.ToolTipText = "Sum of delta-times in ticks * 120 beats per min / 96 ticks per beat ";
            // 
            // DeltaTime
            // 
            this.DeltaTime.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.DeltaTime.HeaderText = "Delta Time (beats)";
            this.DeltaTime.MinimumWidth = 100;
            this.DeltaTime.Name = "DeltaTime";
            this.DeltaTime.ReadOnly = true;
            this.DeltaTime.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.DeltaTime.ToolTipText = "Delta-time to next event in ticks * 120 beats per min / 96 ticks per beat ";
            // 
            // NoteOnTime
            // 
            this.NoteOnTime.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.NoteOnTime.HeaderText = "Note On Time (beats)";
            this.NoteOnTime.MinimumWidth = 100;
            this.NoteOnTime.Name = "NoteOnTime";
            this.NoteOnTime.ReadOnly = true;
            // 
            // FormFixTracks
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(842, 579);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.panelMain);
            this.Controls.Add(this.panelButtons);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormFixTracks";
            this.Text = "Fix Track - Name  Goes  Here";
            this.Shown += new System.EventHandler(this.FormFixTracks_Shown);
            this.panelButtons.ResumeLayout(false);
            this.panelButtons.PerformLayout();
            this.panelMain.ResumeLayout(false);
            this.panelMain.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel panelButtons;
        private System.Windows.Forms.Button buttonReapplyFilters;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.TextBox textBoxFileOffset;
        private System.Windows.Forms.Label labelTracksFileOffset;
        private System.Windows.Forms.TextBox textBoxMissingByteCount;
        private System.Windows.Forms.Label labelMissingByteCount;
        private System.Windows.Forms.TextBox textBoxTrackLength;
        private System.Windows.Forms.TextBox textBoxTableOffset;
        private System.Windows.Forms.Label labelTrackLength;
        private System.Windows.Forms.Label labelTableOffset;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button buttonOk;
        private System.Windows.Forms.Label labelFilters;
        private System.Windows.Forms.TextBox textBoxFilters;
        private System.Windows.Forms.Label labelHelp;
        private System.Windows.Forms.Label labelEndOfTrackShift;
        private System.Windows.Forms.Button buttonAutoFixEvents;
        private System.Windows.Forms.CheckBox checkBoxAutoInsert0D;
        private System.Windows.Forms.TextBox textBoxTicksPerBeat;
        private System.Windows.Forms.Label labelTicksPerBeat;
        private System.Windows.Forms.DataGridViewTextBoxColumn TrackName;
        private System.Windows.Forms.DataGridViewTextBoxColumn CumulativeTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn DeltaTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn NoteOnTime;
        private System.Windows.Forms.Button buttonInsert;
        private System.Windows.Forms.Button buttonDelete;
        private System.Windows.Forms.TextBox textBoxResumeAt;
        private System.Windows.Forms.Label labelResumeAt;
        private System.Windows.Forms.Label labelFixTimingDivisor;
        private System.Windows.Forms.TextBox textBoxFixTimingDivisor;
        private System.Windows.Forms.Label labelFixTimingVariation;
        private System.Windows.Forms.TextBox textBoxFixTimingVariation;
        private System.Windows.Forms.Label labelResultantDivisor;
        private System.Windows.Forms.Label labelResultantVariation;
        private System.Windows.Forms.Button buttonReset;
        private System.Windows.Forms.Button buttonSyncLengthToList;
    }
}
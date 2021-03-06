namespace VssSvnConverter
{
	partial class SimpleUI
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
			this.components = new System.ComponentModel.Container();
			this.buttonBuildList = new System.Windows.Forms.Button();
			this.buttonBuildVersions = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.button3 = new System.Windows.Forms.Button();
			this.button4 = new System.Windows.Forms.Button();
			this.buttonCleanupWC = new System.Windows.Forms.Button();
			this.buttonImport = new System.Windows.Forms.Button();
			this.button7 = new System.Windows.Forms.Button();
			this.button8 = new System.Windows.Forms.Button();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.button9 = new System.Windows.Forms.Button();
			this.button10 = new System.Windows.Forms.Button();
			this.buttonStopImport = new System.Windows.Forms.Button();
			this.buttonImportContinue = new System.Windows.Forms.Button();
			this.buttonTryCensors = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.timerHungDetector = new System.Windows.Forms.Timer(this.components);
			this.fileSystemConfigWatcher = new System.IO.FileSystemWatcher();
			this.labelActiveDriver = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.button1 = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.fileSystemConfigWatcher)).BeginInit();
			this.SuspendLayout();
			// 
			// buttonBuildList
			// 
			this.buttonBuildList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonBuildList.Location = new System.Drawing.Point(12, 12);
			this.buttonBuildList.Name = "buttonBuildList";
			this.buttonBuildList.Size = new System.Drawing.Size(185, 23);
			this.buttonBuildList.TabIndex = 0;
			this.buttonBuildList.Tag = "build-list";
			this.buttonBuildList.Text = "1. Build List";
			this.buttonBuildList.UseVisualStyleBackColor = true;
			this.buttonBuildList.Click += new System.EventHandler(this.buildList_Click);
			// 
			// buttonBuildVersions
			// 
			this.buttonBuildVersions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonBuildVersions.Location = new System.Drawing.Point(12, 41);
			this.buttonBuildVersions.Name = "buttonBuildVersions";
			this.buttonBuildVersions.Size = new System.Drawing.Size(226, 23);
			this.buttonBuildVersions.TabIndex = 2;
			this.buttonBuildVersions.Tag = "build-versions";
			this.buttonBuildVersions.Text = "2. Build versions";
			this.buttonBuildVersions.UseVisualStyleBackColor = true;
			this.buttonBuildVersions.Click += new System.EventHandler(this.buildList_Click);
			this.buttonBuildVersions.Paint += new System.Windows.Forms.PaintEventHandler(this.buttonBuildVersions_Paint);
			// 
			// button2
			// 
			this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.button2.Location = new System.Drawing.Point(12, 70);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(226, 23);
			this.button2.TabIndex = 3;
			this.button2.Tag = "build-links";
			this.button2.Text = "3. Build links info";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.buildList_Click);
			// 
			// button3
			// 
			this.button3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.button3.Location = new System.Drawing.Point(53, 99);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(144, 23);
			this.button3.TabIndex = 5;
			this.button3.Tag = "build-cache";
			this.button3.Text = "4. Build cache";
			this.button3.UseVisualStyleBackColor = true;
			this.button3.Click += new System.EventHandler(this.buildList_Click);
			// 
			// button4
			// 
			this.button4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.button4.Location = new System.Drawing.Point(12, 128);
			this.button4.Name = "button4";
			this.button4.Size = new System.Drawing.Size(226, 23);
			this.button4.TabIndex = 7;
			this.button4.Tag = "build-commits";
			this.button4.Text = "5. Build commits";
			this.button4.UseVisualStyleBackColor = true;
			this.button4.Click += new System.EventHandler(this.buildList_Click);
			// 
			// buttonCleanupWC
			// 
			this.buttonCleanupWC.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCleanupWC.Location = new System.Drawing.Point(12, 174);
			this.buttonCleanupWC.Name = "buttonCleanupWC";
			this.buttonCleanupWC.Size = new System.Drawing.Size(226, 23);
			this.buttonCleanupWC.TabIndex = 9;
			this.buttonCleanupWC.Tag = "build-wc";
			this.buttonCleanupWC.Text = "6. Build/cleanup wc";
			this.buttonCleanupWC.UseVisualStyleBackColor = true;
			this.buttonCleanupWC.Click += new System.EventHandler(this.buildList_Click);
			// 
			// buttonImport
			// 
			this.buttonImport.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonImport.Location = new System.Drawing.Point(12, 203);
			this.buttonImport.Name = "buttonImport";
			this.buttonImport.Size = new System.Drawing.Size(75, 23);
			this.buttonImport.TabIndex = 10;
			this.buttonImport.Tag = "import-new";
			this.buttonImport.Text = "7. Import";
			this.buttonImport.UseVisualStyleBackColor = true;
			this.buttonImport.Click += new System.EventHandler(this.buildList_Click);
			// 
			// button7
			// 
			this.button7.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.button7.Location = new System.Drawing.Point(12, 232);
			this.button7.Name = "button7";
			this.button7.Size = new System.Drawing.Size(226, 23);
			this.button7.TabIndex = 13;
			this.button7.Tag = "build-scripts";
			this.button7.Text = "8. Build scripts";
			this.button7.UseVisualStyleBackColor = true;
			this.button7.Click += new System.EventHandler(this.buildList_Click);
			// 
			// button8
			// 
			this.button8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.button8.Image = global::VssSvnConverter.Properties.Resources.refresh_small;
			this.button8.Location = new System.Drawing.Point(203, 12);
			this.button8.Name = "button8";
			this.button8.Size = new System.Drawing.Size(35, 23);
			this.button8.TabIndex = 1;
			this.button8.Tag = "build-list-stats";
			this.toolTip1.SetToolTip(this.button8, "Refilter and reapply stats");
			this.button8.UseVisualStyleBackColor = true;
			this.button8.Click += new System.EventHandler(this.buildList_Click);
			// 
			// button9
			// 
			this.button9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.button9.Image = global::VssSvnConverter.Properties.Resources.refresh_small;
			this.button9.Location = new System.Drawing.Point(203, 99);
			this.button9.Name = "button9";
			this.button9.Size = new System.Drawing.Size(35, 23);
			this.button9.TabIndex = 6;
			this.button9.Tag = "build-cache-stats";
			this.toolTip1.SetToolTip(this.button9, "Recalc cached statistic");
			this.button9.UseVisualStyleBackColor = true;
			this.button9.Click += new System.EventHandler(this.buildList_Click);
			// 
			// button10
			// 
			this.button10.Location = new System.Drawing.Point(12, 99);
			this.button10.Name = "button10";
			this.button10.Size = new System.Drawing.Size(35, 23);
			this.button10.TabIndex = 4;
			this.button10.Tag = "build-cache-clear-errors";
			this.button10.Text = "CE";
			this.toolTip1.SetToolTip(this.button10, "Remove errors from cache");
			this.button10.UseVisualStyleBackColor = true;
			this.button10.Click += new System.EventHandler(this.buildList_Click);
			// 
			// buttonStopImport
			// 
			this.buttonStopImport.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonStopImport.Location = new System.Drawing.Point(209, 203);
			this.buttonStopImport.Name = "buttonStopImport";
			this.buttonStopImport.Size = new System.Drawing.Size(29, 23);
			this.buttonStopImport.TabIndex = 12;
			this.buttonStopImport.Tag = "import-stop";
			this.buttonStopImport.Text = "S";
			this.toolTip1.SetToolTip(this.buttonStopImport, "Stop import after next commit");
			this.buttonStopImport.UseVisualStyleBackColor = true;
			this.buttonStopImport.Click += new System.EventHandler(this.buttonStopImport_Click);
			// 
			// buttonImportContinue
			// 
			this.buttonImportContinue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonImportContinue.Location = new System.Drawing.Point(93, 203);
			this.buttonImportContinue.Name = "buttonImportContinue";
			this.buttonImportContinue.Size = new System.Drawing.Size(110, 23);
			this.buttonImportContinue.TabIndex = 11;
			this.buttonImportContinue.Tag = "import";
			this.buttonImportContinue.Text = "7. Import (continue)";
			this.buttonImportContinue.UseVisualStyleBackColor = true;
			this.buttonImportContinue.Click += new System.EventHandler(this.buildList_Click);
			// 
			// buttonTryCensors
			// 
			this.buttonTryCensors.Location = new System.Drawing.Point(12, 329);
			this.buttonTryCensors.Name = "buttonTryCensors";
			this.buttonTryCensors.Size = new System.Drawing.Size(226, 23);
			this.buttonTryCensors.TabIndex = 17;
			this.buttonTryCensors.Tag = "try-censors";
			this.buttonTryCensors.Text = "Try Censore";
			this.buttonTryCensors.UseVisualStyleBackColor = true;
			this.buttonTryCensors.Click += new System.EventHandler(this.buildList_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 313);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(31, 13);
			this.label1.TabIndex = 16;
			this.label1.Text = "Test:";
			// 
			// timerHungDetector
			// 
			this.timerHungDetector.Interval = 1000;
			this.timerHungDetector.Tick += new System.EventHandler(this.timerHungDetector_Tick);
			// 
			// fileSystemConfigWatcher
			// 
			this.fileSystemConfigWatcher.EnableRaisingEvents = true;
			this.fileSystemConfigWatcher.SynchronizingObject = this;
			this.fileSystemConfigWatcher.Changed += new System.IO.FileSystemEventHandler(this.fileSystemConfigWatcher_Changed);
			this.fileSystemConfigWatcher.Created += new System.IO.FileSystemEventHandler(this.fileSystemConfigWatcher_Changed);
			// 
			// labelActiveDriver
			// 
			this.labelActiveDriver.AutoSize = true;
			this.labelActiveDriver.Location = new System.Drawing.Point(12, 158);
			this.labelActiveDriver.Name = "labelActiveDriver";
			this.labelActiveDriver.Size = new System.Drawing.Size(69, 13);
			this.labelActiveDriver.TabIndex = 8;
			this.labelActiveDriver.Text = "Active driver:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 264);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(23, 13);
			this.label2.TabIndex = 14;
			this.label2.Text = "Git:";
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(12, 280);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(226, 23);
			this.button1.TabIndex = 15;
			this.button1.Tag = "git-fast-import";
			this.button1.Text = "Generate Fast Import file";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.buildList_Click);
			// 
			// SimpleUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(250, 360);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.labelActiveDriver);
			this.Controls.Add(this.buttonStopImport);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.buttonTryCensors);
			this.Controls.Add(this.buttonImportContinue);
			this.Controls.Add(this.button10);
			this.Controls.Add(this.button9);
			this.Controls.Add(this.button7);
			this.Controls.Add(this.buttonImport);
			this.Controls.Add(this.buttonCleanupWC);
			this.Controls.Add(this.button4);
			this.Controls.Add(this.button3);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.buttonBuildVersions);
			this.Controls.Add(this.button8);
			this.Controls.Add(this.buttonBuildList);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.Name = "SimpleUI";
			this.ShowIcon = false;
			this.Text = "Converter";
			this.TopMost = true;
			this.Load += new System.EventHandler(this.SimpleUI_Load);
			((System.ComponentModel.ISupportInitialize)(this.fileSystemConfigWatcher)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonBuildList;
		private System.Windows.Forms.Button buttonBuildVersions;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Button button3;
		private System.Windows.Forms.Button button4;
		private System.Windows.Forms.Button buttonCleanupWC;
		private System.Windows.Forms.Button buttonImport;
		private System.Windows.Forms.Button button7;
		private System.Windows.Forms.Button button8;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.Button button9;
		private System.Windows.Forms.Button button10;
		private System.Windows.Forms.Button buttonImportContinue;
		private System.Windows.Forms.Button buttonTryCensors;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button buttonStopImport;
		private System.Windows.Forms.Timer timerHungDetector;
		private System.IO.FileSystemWatcher fileSystemConfigWatcher;
		private System.Windows.Forms.Label labelActiveDriver;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button button1;
	}
}
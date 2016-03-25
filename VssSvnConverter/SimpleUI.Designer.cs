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
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.button3 = new System.Windows.Forms.Button();
			this.button4 = new System.Windows.Forms.Button();
			this.button5 = new System.Windows.Forms.Button();
			this.button6 = new System.Windows.Forms.Button();
			this.button7 = new System.Windows.Forms.Button();
			this.button8 = new System.Windows.Forms.Button();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.button9 = new System.Windows.Forms.Button();
			this.button10 = new System.Windows.Forms.Button();
			this.buttonStopImport = new System.Windows.Forms.Button();
			this.button11 = new System.Windows.Forms.Button();
			this.buttonTryCensors = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
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
			// button1
			// 
			this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.button1.Location = new System.Drawing.Point(12, 41);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(226, 23);
			this.button1.TabIndex = 2;
			this.button1.Tag = "build-versions";
			this.button1.Text = "2. Build versions";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.buildList_Click);
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
			// button5
			// 
			this.button5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.button5.Location = new System.Drawing.Point(12, 157);
			this.button5.Name = "button5";
			this.button5.Size = new System.Drawing.Size(226, 23);
			this.button5.TabIndex = 8;
			this.button5.Tag = "build-wc";
			this.button5.Text = "6. Build/cleanup wc";
			this.button5.UseVisualStyleBackColor = true;
			this.button5.Click += new System.EventHandler(this.buildList_Click);
			// 
			// button6
			// 
			this.button6.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.button6.Location = new System.Drawing.Point(12, 186);
			this.button6.Name = "button6";
			this.button6.Size = new System.Drawing.Size(75, 23);
			this.button6.TabIndex = 9;
			this.button6.Tag = "import-new";
			this.button6.Text = "7. Import";
			this.button6.UseVisualStyleBackColor = true;
			this.button6.Click += new System.EventHandler(this.buildList_Click);
			// 
			// button7
			// 
			this.button7.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.button7.Location = new System.Drawing.Point(12, 215);
			this.button7.Name = "button7";
			this.button7.Size = new System.Drawing.Size(226, 23);
			this.button7.TabIndex = 12;
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
			this.buttonStopImport.Location = new System.Drawing.Point(209, 186);
			this.buttonStopImport.Name = "buttonStopImport";
			this.buttonStopImport.Size = new System.Drawing.Size(29, 23);
			this.buttonStopImport.TabIndex = 11;
			this.buttonStopImport.Tag = "import-stop";
			this.buttonStopImport.Text = "S";
			this.toolTip1.SetToolTip(this.buttonStopImport, "Stop import after next commit");
			this.buttonStopImport.UseVisualStyleBackColor = true;
			this.buttonStopImport.Click += new System.EventHandler(this.buttonStopImport_Click);
			// 
			// button11
			// 
			this.button11.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.button11.Location = new System.Drawing.Point(93, 186);
			this.button11.Name = "button11";
			this.button11.Size = new System.Drawing.Size(110, 23);
			this.button11.TabIndex = 10;
			this.button11.Tag = "import";
			this.button11.Text = "7. Import (continue)";
			this.button11.UseVisualStyleBackColor = true;
			this.button11.Click += new System.EventHandler(this.buildList_Click);
			// 
			// buttonTryCensors
			// 
			this.buttonTryCensors.Location = new System.Drawing.Point(12, 265);
			this.buttonTryCensors.Name = "buttonTryCensors";
			this.buttonTryCensors.Size = new System.Drawing.Size(226, 23);
			this.buttonTryCensors.TabIndex = 14;
			this.buttonTryCensors.Tag = "try-censors";
			this.buttonTryCensors.Text = "Try Censore";
			this.buttonTryCensors.UseVisualStyleBackColor = true;
			this.buttonTryCensors.Click += new System.EventHandler(this.buildList_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 249);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(31, 13);
			this.label1.TabIndex = 13;
			this.label1.Text = "Test:";
			// 
			// SimpleUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(250, 300);
			this.Controls.Add(this.buttonStopImport);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.buttonTryCensors);
			this.Controls.Add(this.button11);
			this.Controls.Add(this.button10);
			this.Controls.Add(this.button9);
			this.Controls.Add(this.button7);
			this.Controls.Add(this.button6);
			this.Controls.Add(this.button5);
			this.Controls.Add(this.button4);
			this.Controls.Add(this.button3);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.button8);
			this.Controls.Add(this.buttonBuildList);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "SimpleUI";
			this.ShowIcon = false;
			this.Text = "Converter";
			this.TopMost = true;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonBuildList;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Button button3;
		private System.Windows.Forms.Button button4;
		private System.Windows.Forms.Button button5;
		private System.Windows.Forms.Button button6;
		private System.Windows.Forms.Button button7;
		private System.Windows.Forms.Button button8;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.Button button9;
		private System.Windows.Forms.Button button10;
		private System.Windows.Forms.Button button11;
		private System.Windows.Forms.Button buttonTryCensors;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button buttonStopImport;
	}
}
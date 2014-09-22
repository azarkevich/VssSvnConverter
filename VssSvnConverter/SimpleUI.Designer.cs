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
			this.buttonBuildList = new System.Windows.Forms.Button();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.button3 = new System.Windows.Forms.Button();
			this.button4 = new System.Windows.Forms.Button();
			this.button5 = new System.Windows.Forms.Button();
			this.button6 = new System.Windows.Forms.Button();
			this.button7 = new System.Windows.Forms.Button();
			this.button8 = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// buttonBuildList
			// 
			this.buttonBuildList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonBuildList.Location = new System.Drawing.Point(12, 12);
			this.buttonBuildList.Name = "buttonBuildList";
			this.buttonBuildList.Size = new System.Drawing.Size(111, 23);
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
			this.button1.Size = new System.Drawing.Size(152, 23);
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
			this.button2.Size = new System.Drawing.Size(152, 23);
			this.button2.TabIndex = 3;
			this.button2.Tag = "build-links";
			this.button2.Text = "3. Build links";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.buildList_Click);
			// 
			// button3
			// 
			this.button3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.button3.Location = new System.Drawing.Point(12, 99);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(152, 23);
			this.button3.TabIndex = 4;
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
			this.button4.Size = new System.Drawing.Size(152, 23);
			this.button4.TabIndex = 5;
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
			this.button5.Size = new System.Drawing.Size(152, 23);
			this.button5.TabIndex = 6;
			this.button5.Tag = "build-wc";
			this.button5.Text = "6. Build wc";
			this.button5.UseVisualStyleBackColor = true;
			this.button5.Click += new System.EventHandler(this.buildList_Click);
			// 
			// button6
			// 
			this.button6.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.button6.Location = new System.Drawing.Point(12, 186);
			this.button6.Name = "button6";
			this.button6.Size = new System.Drawing.Size(152, 23);
			this.button6.TabIndex = 7;
			this.button6.Tag = "import";
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
			this.button7.Size = new System.Drawing.Size(152, 23);
			this.button7.TabIndex = 8;
			this.button7.Tag = "build-scripts";
			this.button7.Text = "8. Build scripts";
			this.button7.UseVisualStyleBackColor = true;
			this.button7.Click += new System.EventHandler(this.buildList_Click);
			// 
			// button8
			// 
			this.button8.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.button8.Image = global::VssSvnConverter.Properties.Resources.refresh_small;
			this.button8.Location = new System.Drawing.Point(129, 12);
			this.button8.Name = "button8";
			this.button8.Size = new System.Drawing.Size(35, 23);
			this.button8.TabIndex = 1;
			this.button8.Tag = "build-list-stats";
			this.button8.UseVisualStyleBackColor = true;
			this.button8.Click += new System.EventHandler(this.buildList_Click);
			// 
			// SimpleUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(176, 249);
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
	}
}
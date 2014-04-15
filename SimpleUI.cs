using System;
using System.Drawing;
using System.Windows.Forms;

namespace VssSvnConverter
{
	public partial class SimpleUI : Form
	{
		Options _opts;
		public SimpleUI(Options opts)
		{
			_opts = opts;
			InitializeComponent();
		}

		private void buildList_Click(object sender, EventArgs e)
		{
			Program.ProcessStage(_opts, (sender as Button).Tag as string);

			(sender as Button).BackColor = Color.PaleGreen;
		}
	}
}

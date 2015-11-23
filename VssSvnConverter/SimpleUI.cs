using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace VssSvnConverter
{
	public partial class SimpleUI : Form
	{
		public SimpleUI()
		{
			InitializeComponent();
		}

		private void buildList_Click(object sender, EventArgs e)
		{
			var btn = (Button)sender;
			btn.BackColor = Color.Yellow;
			Controls.Cast<Control>().ToList().ForEach(c => c.Enabled = false);

			new Thread(() => {
				Color color;
				try
				{
					Program.ProcessStage(btn.Tag as string, false);
					color = Color.PaleGreen;
				}
				catch (Exception ex)
				{
					Console.WriteLine("ERROR: {0}", ex.Message);
					color = Color.LightCoral;
				}

				Action action = () => {
					btn.BackColor = color;
					Controls.Cast<Control>().ToList().ForEach(c => c.Enabled = true);
				};
				Invoke(action);
			}).Start();
		}
	}
}

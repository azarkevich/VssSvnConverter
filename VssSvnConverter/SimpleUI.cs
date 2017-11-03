using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VssSvnConverter.Core;

namespace VssSvnConverter
{
	public partial class SimpleUI : Form
	{
		readonly Brush _progressBrush = new SolidBrush(Color.FromArgb(50, Color.ForestGreen));

		public SimpleUI()
		{
			InitializeComponent();

			var cfg = Program.GetConfigPath();
			fileSystemConfigWatcher.Path = Path.GetDirectoryName(cfg);
			fileSystemConfigWatcher.Filter = Path.GetFileName(cfg);
		}

		private void SimpleUI_Load(object sender, EventArgs e)
		{
			ReparseConfig();
		}

		private void buildList_Click(object sender, EventArgs e)
		{
			var btn = (Button)sender;
			btn.BackColor = Color.Yellow;
			Controls
				.Cast<Control>()
				.Where(c => (c.Tag as string) != "import-stop")
				.ToList()
				.ForEach(c => c.Enabled = false)
			;

			// progress tracker
			var currentProgressValue = 0;
			Action<float> trackProgress = v => {

				var newProgressValue = (int)(v * 1000);
				if (currentProgressValue != newProgressValue)
				{
					currentProgressValue = newProgressValue;
					btn.Invalidate();
				}
			};

			PaintEventHandler progressPainter = (o, paintArgs) => {

				var rect = RectangleF.Inflate(paintArgs.ClipRectangle, -3, -3);

				// shape to progress
				rect = new RectangleF(rect.Location, new SizeF((int)(rect.Width * currentProgressValue / 1000.0), rect.Height));

				paintArgs.Graphics.FillRectangle(_progressBrush, rect);
			};
			btn.Paint += progressPainter;

			new Thread(() => {
				Color color;
				try
				{
					Program.ProcessStage(btn.Tag as string, false, trackProgress);
					color = Color.PaleGreen;
				}
				catch (Exception ex)
				{
					Console.WriteLine("ERROR: {0}", ex.Message);
					color = Color.LightCoral;
				}

				Action action = () => {
					btn.Paint -= progressPainter;
					btn.BackColor = color;
					Controls.Cast<Control>().ToList().ForEach(c => c.Enabled = true);
					btn.Invalidate();
				};
				Invoke(action);
			}).Start();
		}

		void buttonStopImport_Click(object sender, EventArgs e)
		{
			Importer.StopImport = true;
		}

		readonly TimeSpan _hung = TimeSpan.FromSeconds(120);
		bool _hungDetected;

		private void timerHungDetector_Tick(object sender, EventArgs e)
		{
			if (_hungDetected)
			{
				if (!Importer.DogWatch.HasValue || (DateTimeOffset.Now - Importer.DogWatch.Value) < _hung)
				{
					buttonImport.BackColor = Color.White;
					buttonImportContinue.BackColor = Color.White;
					_hungDetected = false;
				}
			}
			else
			{
				if (Importer.DogWatch.HasValue && (DateTimeOffset.Now - Importer.DogWatch.Value) > _hung)
				{
					buttonImport.BackColor = Color.LightPink;
					buttonImportContinue.BackColor = Color.LightPink;
					_hungDetected = true;

					Console.WriteLine("HUNG ????");
				}
			}
		}

		async void fileSystemConfigWatcher_Changed(object sender, FileSystemEventArgs e)
		{
			await Task.Delay(500);
			ReparseConfig();
		}

		void ReparseConfig()
		{
			var opts = new Options(new string[0]);
			opts.ReadConfig(Program.GetConfigPath());

			labelActiveDriver.Text = string.Format("Active driver: {0}", opts.ImportDriver);
		}

		private void buttonBuildVersions_Paint(object sender, PaintEventArgs e)
		{

		}
	}
}

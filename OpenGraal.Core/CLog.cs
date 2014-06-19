using System;
using System.IO;
using System.Text;
using System.Threading;

namespace OpenGraal.Core
{
	public class CLog
	{
		private StreamWriter file;
		private bool enabled;
		private string homepath = null;
		private string filename = null;

		public CLog(string file, bool enabled = true) //: enabled( enabled ), filename( file ), file( 0 )
		{
			this.homepath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
			this.filename = file.ToString();
			// Open the file now.
			this.file = new StreamWriter(this.filename, true);
			if (this.file == null)
				this.enabled = false;
		}

		~CLog()
		{
			if (this.file != null)
			{
				this.file.Flush();
				this.file.Close();
			}
		}

		public bool Out(string format, bool displayMessage = true)
		{
			// Display message.
			if (displayMessage)
				Console.Write(format.ToString());

			// Log output to file.
			if (enabled == true && this.file != null)
			{
				this.file.Write(format.ToString());
				this.file.Flush();
				return true;
			}
			else
				return false;
		}

		public void clear()
		{
			this.file.Close();

			this.file = new StreamWriter(homepath + filename, false);
			if (null == file)
				enabled = false;
		}
	}
}
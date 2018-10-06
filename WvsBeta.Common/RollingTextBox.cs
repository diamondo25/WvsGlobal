using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace WvsBeta.Common
{
    public partial class RollingTextBox : TextBox
    {
        public int MaxLines { get; set; } = 15;
        private ConcurrentQueue<string> _rollingLog;

        public RollingTextBox()
        {
            InitializeComponent();
            _rollingLog = new ConcurrentQueue<string>();

            if (Lines.Length > MaxLines)
                Lines.Skip(Lines.Length - MaxLines).ForEach(_rollingLog.Enqueue);
            else
                Lines.ForEach(_rollingLog.Enqueue);

        #if DEBUG
            AddLine("Running in DEBUG mode.");
        #else
            AddLine("Running in RELEASE mode.");
        #endif
        }

        public void AddLine(string text)
        {
            while (_rollingLog.Count >= MaxLines) _rollingLog.TryDequeue(out string trap);

            _rollingLog.Enqueue(text);

            ForceUpdate();
        }

        public void ForceUpdate()
        {
            try
            {
                if (this.InvokeRequired)
                {
                    var tmp = _rollingLog.ToArray();
                    this.Invoke((MethodInvoker) delegate { Lines = tmp; });
                }
                else
                    Lines = _rollingLog.ToArray();
            }
            catch { }
        }

        public new void Clear()
        {
            base.Clear();
            _rollingLog = new ConcurrentQueue<string>();
            ForceUpdate();
        }
    }
}

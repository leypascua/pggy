using System;
using System.Collections.Generic;
using System.CommandLine.IO;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pggy.Cli.Infrastructure
{
    public class ChildProcessBuilder : IDisposable
    {
        private readonly string _path;
        private readonly Dictionary<string, string> _options = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _vars = new Dictionary<string, string>();
        private IStandardStreamWriter _stdout;
        private IStandardStreamWriter _stderr;
        private Process _process = null;
        private bool disposedValue;
        private bool _redirectStdin;
        private bool _redirectStdOut;
        private bool _redirectStdErr;

        static ChildProcessBuilder()
        {
            SetBasePath(Environment.CurrentDirectory);
        }

        public ChildProcessBuilder(string path)
        {
            _path = Path.IsPathRooted(path) ?
                path : 
                Path.Combine(BasePath, path);

            if (!File.Exists(_path))
            {
                throw new FileNotFoundException($"The target executable does not exist: '{_path}'");
            }
        }

        public static void SetBasePath(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"The path '{path}' does not exist.");
            }

            BasePath = path;
        }

        public static string BasePath { get; private set; }

        public ChildProcessBuilder Option(string o)
        {
            _options.Add(o, string.Empty);
            return this;
        }

        public ChildProcessBuilder Option<T>(string name, T value)
        {
            _options.Add(name, value.ToString());
            return this;
        }

        public ChildProcessBuilder SetVar<T>(string name, T value)
        {
            _vars.Add(name, value.ToString());
            return this;
        }
        public ChildProcessBuilder SetStdOut(IStandardStreamWriter stdout)
        {
            _redirectStdOut = stdout != null;
            _stdout = stdout;
            return this;
        }

        public ChildProcessBuilder RedirectStdIn(bool val)
        {
            _redirectStdin = val;
            return this;
        }

        public ChildProcessBuilder RedirectStdOut(bool val)
        {
            _redirectStdOut = val;
            return this;
        }

        public ChildProcessBuilder SetStdErr(IStandardStreamWriter stderr)
        {
            _redirectStdErr = stderr != null;
            _stderr = stderr;
            return this;
        }

        public Process Start()
        {
            var psi = new ProcessStartInfo
            {
                FileName = _path,
                Arguments = BuildArgsFrom(_options),
                RedirectStandardInput = _redirectStdin,
                RedirectStandardOutput = _redirectStdOut,
                RedirectStandardError = _redirectStdErr,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Normal,
                CreateNoWindow = false,
                StandardOutputEncoding = _redirectStdOut ? Encoding.UTF8 : null,
                StandardInputEncoding = _redirectStdin ?  Encoding.UTF8 : null
            };

            foreach (var v in _vars)
            {
                psi.Environment.Add(v.Key, v.Value);
            }

            _process = new Process { StartInfo = psi };

            if (_stdout != null)
            {
                _process.OutputDataReceived += OnProcessOutputReceived;
            }

            if (_stderr != null)
            {
                _process.ErrorDataReceived += OnProcessErrorReceived;
            }

            _process.Start();

            return _process;
        }

        private void OnProcessErrorReceived(object sender, DataReceivedEventArgs e)
        {
            if (_stderr == null) return;
            _stderr.Write(e.Data);
        }

        private void OnProcessOutputReceived(object sender, DataReceivedEventArgs e)
        {
            if (_stdout == null) return;

            _stdout.Write(e.Data);
        }

        private string BuildArgsFrom(Dictionary<string, string> options)
        {
            var sb = new StringBuilder();

            foreach (var key in options.Keys)
            {
                if (sb.Length > 0)
                {
                    sb.Append(" ");
                }

                string optionText = string.IsNullOrEmpty(options[key]) ?
                    key : $"{key} {options[key]}";

                sb.Append(optionText);
            }

            return sb.ToString().Trim();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                if (_process != null)
                {
                    try
                    {
                        _process.OutputDataReceived -= OnProcessOutputReceived;
                        _process.Dispose();
                    }
                    finally
                    {
                        _process = null;
                    }
                }
                
                disposedValue = true;
            }
        }

        ~ChildProcessBuilder()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

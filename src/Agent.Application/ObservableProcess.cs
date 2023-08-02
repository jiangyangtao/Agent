using System.Diagnostics;
using System.Reactive;
using System.Reactive.Subjects;
using System.Text;

namespace Agent.Application
{
    /// <summary>
    ///  响应式Process
    /// </summary>
    public class ObservableProcess : IDisposable
    {
        public ObservableProcess(string command, string arguments)
        {

            var startinfo = new ProcessStartInfo(command)
            {
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,

            };
            var process = new Process { StartInfo = startinfo };
            process.OutputDataReceived += OnReceived;
            process.ErrorDataReceived += OnReceived;
            process.Exited += OnExist;



            _standardInput = Observer.Create<string>(x =>
            {
                process.StandardInput.WriteLine(x); process.StandardInput.Flush();
            });




            _process = process;

            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }


        private readonly Process _process;



        private Subject<string> _standardOutput = new Subject<string>();

        /// <summary>
        ///  Process的标准输出
        /// </summary>
        /// <value></value>
        public Subject<string> StandardOutput { get { return _standardOutput; } }

        private Subject<string> _standardError = new Subject<string>();

        /// <summary>
        ///  Process的标准错误
        /// </summary>
        /// <value></value>
        public Subject<string> StandardError { get { return _standardError; } }

        private IObserver<string> _standardInput;

        /// <summary>
        ///  Process的标准输入
        /// </summary>
        /// <value></value>
        public IObserver<string> StandardInput { get { return _standardInput; } }


        /// <summary>
        /// 退出状态码
        /// </summary>
        /// <value></value>
        public AsyncSubject<int> ExistCode { get { return _exitCode; } }

        private AsyncSubject<int> _exitCode = new();


        void OnReceived(object s, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data)) return;

            StandardOutput.OnNext(ReparseAsciiDataAsUtf8(e.Data));
        }

        void OnExist(object sender, EventArgs e)
        {
            _exitCode.OnNext(_process.ExitCode);
        }

        static string ReparseAsciiDataAsUtf8(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var bytes = new byte[input.Length * 2];
            int i = 0;
            foreach (char c in input)
            {
                bytes[i] = (byte)(c & 0xFF);
                i++;

                var msb = (byte)(c & 0xFF00 >> 16);
                if (msb > 0)
                {
                    bytes[i] = msb;
                    i++;
                }
            }

            var ret = Encoding.UTF8.GetString(bytes, 0, i);
            return ret;
        }

        /// <summary>
        ///  释放资源
        /// </summary>
        public void Dispose()
        {
            _process.CancelErrorRead();
            _process.CancelOutputRead();
            _process.OutputDataReceived -= OnReceived;
            _process.ErrorDataReceived -= OnReceived;
            _process.Exited -= OnExist;
            _process.Dispose();
            _standardError.Dispose();
            _standardOutput.Dispose();
            _exitCode.Dispose();
        }

        /// <summary>
        ///  启动进程
        /// </summary>
        public void Start()
        {
            this._process.Start();
            this._process.BeginOutputReadLine();
            this._process.BeginErrorReadLine();
        }

        /// <summary>
        /// 杀死进程
        /// </summary>
        public void Stop(bool stopByKill = true)
        {
            if (_process.StartInfo.CreateNoWindow == true && stopByKill == false)
            {
                throw new InvalidOperationException("non gui application must stop by kill");
            }

            if (stopByKill == true)
            {
                _process.Kill();
            }
            else
            {
                _process.CloseMainWindow();
            }
            _process.WaitForExit();
            _exitCode.OnNext(_process.ExitCode);
        }

        /// <summary>
        ///  进程Id
        /// </summary>
        /// <value></value>
        public int ProcessId
        {
            get { return _process.Id; }
        }
    }
}

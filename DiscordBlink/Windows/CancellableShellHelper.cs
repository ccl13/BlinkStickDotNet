using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DiscordBlink
{
    public class CancellableShellHelper
    {
        #region SetConsoleCtrlHandler

        /// <summary>
        /// Adds or removes an application-defined HandlerRoutine function from the list of handler functions for the calling process.
        /// </summary>
        /// <see cref="https://docs.microsoft.com/en-us/windows/console/setconsolectrlhandler"/>
        /// <param name="Handler"></param>
        /// <param name="Add"></param>
        /// <returns></returns>
        [System.Runtime.InteropServices.DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        /// <summary>
        /// An application-defined function used with the SetConsoleCtrlHandler function. A console process uses this function to handle control signals received by the process. When the signal is received, the system creates a new thread in the process to execute the function.
        /// </summary>
        /// <see cref="https://docs.microsoft.com/en-us/windows/console/handlerroutine"/>
        /// <param name="CtrlType"></param>
        /// <returns></returns>
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        /// <see cref="https://docs.microsoft.com/en-us/windows/console/handlerroutine"/>
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        #endregion

        /// <summary>
        /// Token for handling exit
        /// </summary>
        public CancellationTokenSource ProcessCancellationTokenSource { get; } = new CancellationTokenSource();

        public CancellationToken CancellationToken { get { return ProcessCancellationTokenSource.Token; } }

        public Action WaitAfterCancel { get; set; } = null;

        public HandlerRoutine HandleConsoleExit { get; set; } = null;

        public void OnProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine("Flushing before exiting...");
            ProcessCancellationTokenSource.Cancel();
            WaitAfterCancel?.Invoke();
            Console.WriteLine("I'm out of here");
        }

        private bool DefaultHandleConsoleExit(CtrlTypes ctrlType)
        {
            OnProcessExit(null, null);
            return false;
        }

        public void SetupCancelHandler(EventHandler onProcessExit)
        {
            if (HandleConsoleExit == null)
            {
                HandleConsoleExit = new HandlerRoutine(DefaultHandleConsoleExit);
            }
            SetConsoleCtrlHandler(HandleConsoleExit, true);
            AppDomain.CurrentDomain.ProcessExit += onProcessExit;
        }

        public void SetupCancelHandler()
        {
            if (HandleConsoleExit == null)
            {
                HandleConsoleExit = new HandlerRoutine(DefaultHandleConsoleExit);
            }
            SetConsoleCtrlHandler(HandleConsoleExit, true);
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
        }

    }
}

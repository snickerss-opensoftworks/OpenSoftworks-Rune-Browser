using System;
using System.Windows;
using System.Windows.Threading;

namespace RuneS
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            System.Windows.Media.RenderOptions.ProcessRenderMode =
                System.Windows.Interop.RenderMode.Default;
            DispatcherUnhandledException += OnDispatcherException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainException;
        }

        private void OnDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[RuneS] UI exception: " + e.Exception);
            if (!(e.Exception is OutOfMemoryException || e.Exception is StackOverflowException))
                e.Handled = true;
        }

        private void OnDomainException(object sender, UnhandledExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[RuneS] Domain exception: " + e.ExceptionObject);
        }
    }
}

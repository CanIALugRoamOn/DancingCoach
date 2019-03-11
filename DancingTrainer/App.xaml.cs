using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace DancingTrainer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += CurrentDomain_UnhandledException; ;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            File.WriteAllText("testtest.txt", e.ExceptionObject.ToString());
        }

        // https://stackoverflow.com/questions/793100/globally-catch-exceptions-in-a-wpf-application
        //private static Logger _logger = LogManager.GetCurrentClassLogger();

        //protected override void OnStartup(StartupEventArgs e)
        //{
        //    base.OnStartup(e);

        //    SetupExceptionHandling();
        //}

        //private void SetupExceptionHandling()
        //{
        //    AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        //        LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

        //    DispatcherUnhandledException += (s, e) =>
        //        LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");

        //    TaskScheduler.UnobservedTaskException += (s, e) =>
        //        LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
        //}

        //private void LogUnhandledException(Exception exception, string source)
        //{
        //    string message = $"Unhandled exception ({source})";
        //    try
        //    {
        //        System.Reflection.AssemblyName assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
        //        message = string.Format("Unhandled exception in {0} v{1}", assemblyName.Name, assemblyName.Version);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error(ex, "Exception in LogUnhandledException");
        //    }
        //    finally
        //    {
        //        _logger.Error(exception, message);
        //        File.WriteAllText(@"C:\Users\roman\Desktop\testtest.txt", exception.ToString());
        //    }
        //}
    }
}

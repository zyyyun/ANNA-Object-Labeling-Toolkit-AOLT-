using ASLTv1.Forms;
using ASLTv1.Services;

namespace ASLTv1
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            LogService.Initialize();
            LogService.AuditAppStart();
            try
            {
                ApplicationConfiguration.Initialize();
                Application.Run(new MainForm());
            }
            finally
            {
                LogService.AuditAppStop();
                LogService.CloseAndFlush();
            }
        }
    }
}

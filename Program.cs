using AOLTv1.Forms;
using AOLTv1.Services;

namespace AOLTv1
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            if (!LicenseService.IsAuthorized(out string denyReason))
            {
                MessageBox.Show(denyReason, "AOLT - 인증 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.Run(new MainForm());
        }
    }
}

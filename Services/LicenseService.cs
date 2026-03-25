using System.Net.NetworkInformation;

namespace AOLTv1.Services
{
    internal static class LicenseService
    {
        private static readonly DateTime ExpirationDate = new DateTime(2026, 5, 31, 23, 59, 59);

        private static readonly HashSet<string> AllowedMacAddresses = new(StringComparer.OrdinalIgnoreCase)
        {
            "2C-F0-5D-B5-7C-EE", // IFEZ PC 1
            "2C-F0-5D-B5-7C-71", // IFEZ PC 2
            "F0-2F-74-32-33-77", // 본인 PC
            "D8-5E-D3-94-B4-F5", // 박연구원 PC
        };

        public static bool IsAuthorized(out string denyReason)
        {
            if (DateTime.Now > ExpirationDate)
            {
                denyReason = $"IFEZ 데모 사용 기간이 만료되었습니다.\n\n만료일: {ExpirationDate:yyyy-MM-dd}";
                return false;
            }

            var macAddresses = GetMacAddresses();
            if (!macAddresses.Any(mac => AllowedMacAddresses.Contains(mac)))
            {
                denyReason = "등록되지 않은 PC입니다.\n관리자에게 문의해 주세요.\n\n";
                return false;
            }

            denyReason = string.Empty;
            return true;
        }

        private static List<string> GetMacAddresses()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up
                           && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(nic => nic.GetPhysicalAddress().ToString())
                .Where(mac => !string.IsNullOrEmpty(mac))
                .Select(mac => string.Join("-", Enumerable.Range(0, mac.Length / 2)
                    .Select(i => mac.Substring(i * 2, 2))))
                .ToList();
        }
    }
}

using System.Security.Cryptography;

namespace ASLTv1.Helpers
{
    /// <summary>
    /// KISA 가이드 준수 PBKDF2-HMAC-SHA256 해싱 유틸리티.
    /// 라이선스 검증 및 민감 데이터 해싱에 사용.
    /// </summary>
    public static class SecurityHelper
    {
        private const int SALT_SIZE = 16;
        private const int HASH_SIZE = 32;
        private const int ITERATIONS = 310_000;

        /// <summary>
        /// 입력 문자열을 PBKDF2-HMAC-SHA256으로 해싱합니다.
        /// </summary>
        /// <param name="input">해싱할 입력 문자열</param>
        /// <returns>Base64 인코딩된 (Hash, Salt) 튜플</returns>
        public static (string Hash, string Salt) HashSecret(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentNullException(nameof(input));

            byte[] salt = RandomNumberGenerator.GetBytes(SALT_SIZE);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                input, salt, ITERATIONS,
                HashAlgorithmName.SHA256, HASH_SIZE);

            return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
        }

        /// <summary>
        /// 입력 문자열이 저장된 해시와 일치하는지 검증합니다.
        /// 타이밍 공격 방지를 위해 FixedTimeEquals를 사용합니다.
        /// </summary>
        /// <param name="input">검증할 입력 문자열</param>
        /// <param name="storedHash">저장된 Base64 해시</param>
        /// <param name="storedSalt">저장된 Base64 솔트</param>
        /// <returns>일치하면 true</returns>
        public static bool VerifySecret(string input, string storedHash, string storedSalt)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
                return false;

            byte[] salt = Convert.FromBase64String(storedSalt);
            byte[] candidateHash = Rfc2898DeriveBytes.Pbkdf2(
                input, salt, ITERATIONS,
                HashAlgorithmName.SHA256, HASH_SIZE);

            return CryptographicOperations.FixedTimeEquals(
                candidateHash, Convert.FromBase64String(storedHash));
        }
    }
}

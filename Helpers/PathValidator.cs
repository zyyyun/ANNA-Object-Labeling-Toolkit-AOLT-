namespace ASLTv1.Helpers
{
    /// <summary>
    /// 파일 경로 트래버설 방지 유틸리티.
    /// 모든 파일 I/O 전에 경로가 허용된 디렉토리 내에 있는지 검증.
    /// </summary>
    public static class PathValidator
    {
        /// <summary>
        /// 파일 경로가 허용된 기본 디렉토리 내에 있는지 검증합니다.
        /// Path.GetFullPath 정규화를 통해 ../ 경로 트래버설을 차단합니다.
        /// </summary>
        /// <param name="filePath">검증할 파일 경로</param>
        /// <param name="allowedBaseDir">허용된 기본 디렉토리</param>
        /// <returns>경로가 안전하면 true, 트래버설 시도 또는 잘못된 경로면 false</returns>
        public static bool IsPathSafe(string filePath, string allowedBaseDir)
        {
            if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(allowedBaseDir))
                return false;

            try
            {
                string fullPath = Path.GetFullPath(filePath);
                string fullBase = Path.GetFullPath(allowedBaseDir);

                // 기본 디렉토리가 경로 구분자로 끝나지 않으면 추가하여 접두사 일치를 정확히 함
                if (!fullBase.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    fullBase += Path.DirectorySeparatorChar;

                return fullPath.StartsWith(fullBase, StringComparison.OrdinalIgnoreCase);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}

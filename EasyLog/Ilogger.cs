namespace EasyLog;

public interface Ilogger
{
    void Log(string jobName, string sourcePath, string targetPath, long fileSize, long transferTimeMs, long   encryptionTimeMs = 0);
}
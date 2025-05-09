namespace OnlineMarket.Core.Common.Infra;

public sealed class LoggingHelper
{
    public static ILogging Init(bool logging, int delay, string fileName = "json")
    { 
        if(logging)
        {
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var path = Path.Combine(docPath, $"{fileName}.data");
            // make sure file is created
            var file = File.Open(path.ToString(), FileMode.OpenOrCreate, FileAccess.ReadWrite);
            file.Close();
            // make sure file is truncated
            file = File.Open(path.ToString(), FileMode.Truncate, FileAccess.ReadWrite);
            file.Close();
            StreamWriter outputFile = new(path)
            {
                // disable write to disk on every append
                AutoFlush = false
            };
            var loggingInstance = new JsonLogging(outputFile, delay);
            loggingInstance.InitLoggingTask();
            return loggingInstance;
        }
        else
        {
            return DEFAULT_NO_LOGGING;
        }
    }

    private class DefaultNoLogging : ILogging
    {
        public void Dispose(){ }
    }

    private static readonly DefaultNoLogging DEFAULT_NO_LOGGING = new(); 
}



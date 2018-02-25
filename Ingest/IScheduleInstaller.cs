namespace Ingest
{
    public interface IScheduleInstaller
    {
        bool IsInstalled { get; }
        bool installTask(string execPath, string arguments);
        void runTask();
        void deleteTaskAndTriggers();
    }
}
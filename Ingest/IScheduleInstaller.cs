namespace Ingest
{
    public interface IScheduleInstaller
    {
        bool IsInstalled { get; }
        bool installTaskAsService(string execPath, string arguments);
        void installUserTask(string execPath, string arguments);
        void runTask();
        void deleteTaskAndTriggers();
    }
}
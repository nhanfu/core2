using Hangfire;

namespace Core.BgService
{
    public class DeleteDowloadService
    {
        protected readonly IWebHostEnvironment host;
        public DeleteDowloadService(IWebHostEnvironment _host)
        {
            host = _host;
        }

        [Obsolete]
        public void ScheduleJob()
        {
            RecurringJob.AddOrUpdate<DeleteDowloadService>(x => x.DeleteProcesses(), Cron.Daily(3, 0));
        }

        public void DeleteProcesses()
        {
            var absolutePath = host.WebRootPath + "\\excel\\Download\\";
            DeleteAllFilesInDirectory(absolutePath);
        }

        public void DeleteAllFilesInDirectory(string directoryPath)
        {
            DirectoryInfo directory = new DirectoryInfo(directoryPath);
            foreach (FileInfo file in directory.GetFiles())
            {
                file.Delete();
            }
        }
    }
}

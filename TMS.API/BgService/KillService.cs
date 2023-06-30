using Microsoft.EntityFrameworkCore;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Hangfire;

namespace TMS.API.BgService
{
    public class KillService
    {
        protected readonly IConfiguration _configuration;
        public KillService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [Obsolete]
        public void ScheduleJob()
        {
            RecurringJob.AddOrUpdate<KillService>(x => x.KillBlockedProcesses(), Cron.Minutely);
        }

        public void KillBlockedProcesses()
        {
            var connectionString = _configuration.GetConnectionString("Default");
            var serverConnection = new ServerConnection();
            serverConnection.ConnectionString = connectionString;
            var server = new Server(serverConnection);
            server.ConnectionContext.Connect();
            var query = "SELECT distinct blocking_session_id FROM sys.dm_exec_requests WHERE blocking_session_id <> 0";
            var results = server.ConnectionContext.ExecuteWithResults(query);
            for (int i = 0; i < results.Tables[0].Rows.Count; i++)
            {
                var blockingSessionId = int.Parse(results.Tables[0].Rows[i]["blocking_session_id"].ToString());
                server.KillProcess(blockingSessionId);
            }
        }
    }
}

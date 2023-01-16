using Newtonsoft.Json;
using UiEcommer.Models;

namespace UiEcommer.Client
{
    public class Client
    {
        public string EntityName { get; set; }
        public IConfiguration _configuration { get; set; }
        public Client(string entityName, IConfiguration configuration)
        {
            EntityName = entityName;
            _configuration = configuration;
        }

        public async Task<List<T>> GetList<T>(string subUrl) where T : class
        {
            var request = new HttpClient();
            request.DefaultRequestHeaders.Add("Pragma", "no-cache");
            request.DefaultRequestHeaders.Add("Cache-Control", "no-store, no-cache, must-revalidate, post-check=0, pre-check=0");
            var rs = await request.GetAsync($"{_configuration["Api"]}/api/{EntityName}/{subUrl}");
            var stringRs = await rs.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<OdataResult<T>>(stringRs);
            return data.value;
        }

        public async Task<T> FirstOfDefault<T>(string subUrl) where T : class
        {
            var request = new HttpClient();
            request.DefaultRequestHeaders.Add("Pragma", "no-cache");
            request.DefaultRequestHeaders.Add("Cache-Control", "no-store, no-cache, must-revalidate, post-check=0, pre-check=0");
            var rs = await request.GetAsync($"{_configuration["Api"]}/api/{EntityName}/{subUrl}");
            var stringRs = await rs.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<OdataResult<T>>(stringRs);
            return data.value.FirstOrDefault();
        }
    }
}

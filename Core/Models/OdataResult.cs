using System.Collections.Generic;

namespace Core.Models
{
    public class Odata
    {
        public int? Count { get; set; }
    }

    public class OdataResult<T>
    {
        public Odata Odata { get; set; }

#if NETCOREAPP
        public object Value { get; set; }
#endif
#if !NETCOREAPP
        public List<T> Value { get; set; }
#endif
        public string Query { get; set; }
        public string Sql { get; set; }

        public OdataResult()
        {
            Odata = new Odata();
            Value = new List<T>();
        }
    }

    public class WebSocketResponse<T>
    {
        public string QueueName { get; set; }
        public T Data { get; set; }
        public List<T> DataList { get; set; }
    }
}

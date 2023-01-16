using System.Text.Json.Serialization;

namespace UiEcommer.Models
{
    public class Odata
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        public int? count { get; set; }
    }

    public class OdataResult<T>
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        public Odata odata { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        public List<T> value { get; set; }
        public OdataResult()
        {
            odata = new Odata();
            value = new List<T>();
        }
    }

    public class WebSocketResponse<T>
    {
        public int EntityId { get; set; }
        public T Data { get; set; }
        public List<T> DataList { get; set; }
    }
}

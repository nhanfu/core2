using Core.Enums;
using System;

namespace TMS.UI.Business.Settings
{
    [Serializable]
    internal class ApiException : Exception
    {
        public ApiException()
        {
        }

        public ApiException(string message) : base(message)
        {
        }

        public ApiException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public HttpStatusCode StatusCode { get; set; }
    }
}
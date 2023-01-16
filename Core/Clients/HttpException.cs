using Bridge.Html5;
using Core.Enums;
using System;

namespace Core.Clients
{
    public class HttpException : Exception
    {
        public XMLHttpRequest XHR { get; internal set; }
        public HttpStatusCode StatusCode => (HttpStatusCode)XHR.Status;
        public HttpException()
        {
        }

        public HttpException(string message) : base(message)
        {
        }

        public HttpException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class TmpException
    {
        public HttpStatusCode StatusCode { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public TmpException InnerException { get; set; }
    }
}

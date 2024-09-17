using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Prevueit.Lib.Models.Shared
{
    public class ResponseModel
    {
        public bool IsSuccess { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string Message { get; set; }
    }
    public class ResponseModel<t> : ResponseModel
    {
        public t ResponseData { get; set; }
    }
}

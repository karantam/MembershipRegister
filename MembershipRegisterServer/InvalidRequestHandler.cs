using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MembershipRegisterServer
{
    internal class InvalidRequestHandler : RequestHandler
    {
        public const string NAME = "/InvalidRequestHandler";

        public void Handle(HttpListenerContext context)
        {
            HttpListenerResponse response = context.Response;

            // set status as 404 not found to indicate failure
            response.StatusCode = (int)HttpStatusCode.NotFound;

            //creating and sending a response
            string message = "Invalid request";
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            response.OutputStream.Write(messageBytes, 0, messageBytes.Length);
            response.Close();

            Program.Log($"Invalid request from user. Request string: {context.Request.RawUrl}");
        }

        public string GetName()
        {
            return NAME;
        }
    }
}

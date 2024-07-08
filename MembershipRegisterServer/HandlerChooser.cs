using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MembershipRegisterServer
{
    internal class HandlerChooser
    {
        private Dictionary<string, RequestHandler> HandlersList;

        /*
         * HandlerChooser class contains a list of request handlers
         */
        public HandlerChooser()
        {
            HandlersList = new Dictionary<string, RequestHandler> ();
            //Adding a default handler for invalid requests
            this.AddHandler(new InvalidRequestHandler());
        }

        /*
         * AddHandler method is used to add or replace request handlers
         */
        public void AddHandler(RequestHandler handlerpar)
        {
            // Add a new handler if it dosent exist yet
            if (!HandlersList.ContainsKey(handlerpar.GetName()))
            {
                HandlersList.Add(handlerpar.GetName(), handlerpar);
            }
            else
            {
                //Or Replace the old one if it does
                HandlersList[handlerpar.GetName()] = handlerpar;
            }
        }

        /*
         * HandleContext method selects which handler is used for the request
         */
        public void HandleContext(HttpListenerContext context)
        {
            //Getting the requested handler
            HttpListenerRequest request = context.Request;
            HttpListenerBasicIdentity identity = (HttpListenerBasicIdentity)context.User.Identity;
            string HandlerName = request.Url.AbsolutePath;
            //Authenticate user name and password
            if (identity != null && Database.Instance.CheckUser(identity.Name, identity.Password))
            //if (identity.Name == "user" && identity.Password == "pass")
            {
                RequestHandler handler;
                //If requested handler exists use it
                if (HandlersList.ContainsKey(HandlerName))
                {
                    handler = HandlersList[HandlerName];
                }
                else
                {
                    //If it doesn't exist use the default handler
                    handler = HandlersList[InvalidRequestHandler.NAME];
                }
                handler.Handle(context);
                //this.InvokeHandler(handler, context);
            }
            else
            {
                HttpListenerResponse response = context.Response;

                // set status as 401 unauthorized to indicate failed authentication
                response.StatusCode = (int)HttpStatusCode.Unauthorized;

                //creating and sending a response
                string message = "Access denied";
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                response.OutputStream.Write(messageBytes, 0, messageBytes.Length);
                response.Close();

                Program.Log($"Invalid username or password from user. Request string: {context.Request.RawUrl}");
            }
        }

        /*
         * InvokeHandler method starts a new thread to handle the request
         */
        private void InvokeHandler(RequestHandler handler, HttpListenerContext context)
        {
            RequestCommand command = new(handler, context);
            Task handleRequestTask = new(command.Execute);
            handleRequestTask.Start();
        }

        /*
         * RequestCommand is a helper class for InvokeHandler method
         */
        public class RequestCommand
        {
            private RequestHandler handler;
            private HttpListenerContext context;

            public RequestCommand(RequestHandler handlerpar, HttpListenerContext contextpar)
            {
                this.handler = handlerpar;
                this.context = contextpar;
            }

            public void Execute()
            {
                this.handler.Handle(this.context);
            }
        }
    }
}

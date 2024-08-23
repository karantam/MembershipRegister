using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MembershipRegisterServer
{
    internal class UserRequestHandler : RequestHandler
    {
        public const string NAME = "/User";

        public void Handle(HttpListenerContext context)
        {
            HttpListenerResponse response = context.Response;
            HttpListenerRequest request = context.Request;
            string[] status;
            int code;
            string message;

            if (request.HttpMethod == "POST")
            {
                //Handle POST requests
                status = HandlePOSTRequest(context);
                code = int.Parse(status[0]);
                message = status[1];
            }
            else if (request.HttpMethod == "GET")
            {
                //Handle GET requests
                status = HandleGETRequest(context);
                code = int.Parse(status[0]);
                message = status[1];
            }
            else
            {
                code = (int)HttpStatusCode.NotFound;
                message = "Request not supported";
            }

            response.StatusCode = code;

            //creating and sending a response
            if (code >= 400)
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                response.OutputStream.Write(messageBytes, 0, messageBytes.Length);
                response.Close();
                Program.Log($"ERROR in /User: {code} {message}");
            }
            else
            {
                Program.Log(message);
            }
        }

        public string GetName()
        {
            return NAME;
        }

        /*
         * HandlePOSTRequest processe the data sent by user to add a new member into the database
         */
        public string[] HandlePOSTRequest(HttpListenerContext context)
        {
            string[] status = new string[2];
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            HttpListenerBasicIdentity identity = (HttpListenerBasicIdentity)context.User.Identity;
            int code;
            string statusMessage;

            if (request.ContentType == null)
            {
                code = 400;
                statusMessage = "No Content-Type in request";
            }
            else if (request.ContentType == "application/json")
            {
                StreamReader reader = new(request.InputStream, Encoding.UTF8);
                string body = reader.ReadToEnd();
                request.InputStream.Close();
                reader.Close();
                Program.Log(body);
                try
                {
                    JObject jObjMember = JObject.Parse(body);

                    // Checking if the request has an action entry and if it does replacing the default empty string with the desired action
                    string action = "";
                    if (jObjMember.TryGetValue("action", out JToken? actiontoken))
                    {
                        action = actiontoken.ToString().Trim();
                    }

                    // Performing the default action of creating a new member
                    if (string.IsNullOrWhiteSpace(action))
                    {
                        if (Database.Instance.IsAdmin(identity.Name))
                        {
                            // Creating an user object from the user data
                            if(jObjMember.TryGetValue("username", out JToken? nametoken) && jObjMember.TryGetValue("password", out JToken? passwordtoken) && jObjMember.TryGetValue("email", out JToken? emailtoken) 
                                && jObjMember.TryGetValue("role", out JToken? roletoken))
                            {
                                string name = nametoken.ToString().Trim();
                                string password = passwordtoken.ToString().Trim();
                                string email = emailtoken.ToString().Trim();
                                string role = roletoken.ToString().Trim();
                                Boolean admin = false;
                                if (role == "admin")
                                {
                                    admin = true;
                                }
                                // Cheking that username, password and email are of valid length and form
                                if (string.IsNullOrWhiteSpace(name) || name.Length < 5 || name.Length > 20 || string.IsNullOrWhiteSpace(password) || password.Length < 5 || password.Length > 20
                                    || string.IsNullOrWhiteSpace(email) || email.Length < 5 || email.Length > 50 || !email.Contains('@'))
                                {
                                    code = 400;
                                    statusMessage = "Invalid username, password or email";
                                }
                                else
                                {
                                    User user = new User(name, password, email);
                                    status = Database.Instance.CreateUser(user, admin);
                                    code = int.Parse(status[0]);
                                    statusMessage = status[1];
                                }
                            }
                            else
                            {
                                code = 400;
                                statusMessage = "No valid JSON in request body";
                            }
                        }
                        else
                        {
                            code = 403;
                            statusMessage = "Request denied. Only admins can create new users";
                        }
                    }
                    // Giving an error for an unknown action
                    else
                    {
                        code = 400;
                        statusMessage = "Invalid action";
                    }
                }
                catch (Exception e)
                {
                    Program.Log(e.ToString());
                    code = 400;
                    statusMessage = "Request body was not in proper JSON format";
                }
            }
            else
            {
                code = 415;
                statusMessage = "Content-Type must be application/json";
            }

            if (code < 400)
            {
                response.StatusCode = code;
                byte[] messageBytes = Encoding.UTF8.GetBytes(statusMessage);
                response.OutputStream.Write(messageBytes, 0, messageBytes.Length);
                response.Close();
            }

            status[0] = code.ToString();
            status[1] = statusMessage;
            return status;
        }

        /*
         * HandleGETRequest method returns all users from the database to the user
         */
        public string[] HandleGETRequest(HttpListenerContext context)
        {
            string[] status = new string[2];
            HttpListenerResponse response = context.Response;
            int code = (int)HttpStatusCode.OK;
            string statusMessage = "";

            List<User> people;
            people = Database.Instance.GetAllUsers();
            JsonArray responseMessage = new();
            foreach (User user in people)
            {
                responseMessage.Add(user.ToJsonObject());
            }

            response.StatusCode = code;
            String messageStr = responseMessage.ToString();
            byte[] messageBytes = Encoding.UTF8.GetBytes(messageStr);
            response.OutputStream.Write(messageBytes, 0, messageBytes.Length);
            response.Close();
            Program.Log("GET request processed in /User");

            status[0] = code.ToString();
            status[1] = statusMessage;
            return status;
        }
    }
}

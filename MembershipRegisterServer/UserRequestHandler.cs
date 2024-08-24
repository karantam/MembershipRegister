using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;

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
                    // Cheking if the request came from an admin an saving that information to the isDmin boolean
                    Boolean isAdmin = Database.Instance.IsAdmin(identity.Name);

                    // Performing the default action of creating a new member
                    if (string.IsNullOrWhiteSpace(action))
                    {
                        if (isAdmin)
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
                    // Deleting the given user from the database
                    else if (action == "delete")
                    {
                        if (isAdmin)
                        {
                            // Getting the username of the user to be deleted
                            if (jObjMember.TryGetValue("username", out JToken? nametoken))
                            {
                                string name = nametoken.ToString().Trim();

                                if (string.IsNullOrWhiteSpace(name))
                                {
                                    code = 400;
                                    statusMessage = "Username of the user to be deleted was empty or null";
                                }
                                else if(identity.Name == name)
                                {
                                    code = 400;
                                    statusMessage = "User can't delete itself";
                                }
                                else
                                {
                                    status = Database.Instance.RemoveUser(name);
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
                            statusMessage = "Request denied. Only admins can delete users";
                        }
                    }
                    // Changing the role(admin or user) of the given user in the database
                    else if (action == "change role")
                    {
                        if (isAdmin)
                        {
                            // Getting the username and new role of the user
                            if (jObjMember.TryGetValue("username", out JToken? nametoken) && jObjMember.TryGetValue("role", out JToken? roletoken))
                            {
                                string name = nametoken.ToString().Trim();
                                string role = roletoken.ToString().Trim();
                                Boolean admin = false;
                                if (role == "admin")
                                {
                                    admin = true;
                                }

                                if (string.IsNullOrWhiteSpace(name))
                                {
                                    code = 400;
                                    statusMessage = "Username was empty or null";
                                }
                                else
                                {
                                    status = Database.Instance.ChangeUserRole(name, admin);
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
                            statusMessage = "Request denied. Only admins can give or remove admin rights";
                        }
                    }
                    // Changing the password of the given user in the database
                    else if (action == "change password")
                    {
                        // Getting the username and new password of the user
                        if (jObjMember.TryGetValue("username", out JToken? nametoken) && jObjMember.TryGetValue("password", out JToken? passwordtoken))
                        {
                            string name = nametoken.ToString().Trim();
                            string password = passwordtoken.ToString().Trim();

                            if (string.IsNullOrWhiteSpace(name))
                            {
                                code = 400;
                                statusMessage = "Username was empty or null";
                            }
                            // Cheking that the password is of valid length
                            else if (string.IsNullOrWhiteSpace(password) || password.Length < 5 || password.Length > 20)
                            {
                                code = 400;
                                statusMessage = "Invalid password";
                            }
                            else if (isAdmin || identity.Name == name)
                            {
                                status = Database.Instance.ChangeUserPassword(name, password);
                                code = int.Parse(status[0]);
                                statusMessage = status[1];
                            }
                            else
                            {
                                code = 403;
                                statusMessage = "Request denied. Only admins can change other users passwords";
                            }
                        }
                        else
                        {
                            code = 400;
                            statusMessage = "No valid JSON in request body";
                        }
                    }
                    // Changing the username and email of the given user in the database
                    else if (action == "edit user")
                    {
                        // Getting the username and new password of the user
                        if (jObjMember.TryGetValue("oldusername", out JToken? oldnametoken) && jObjMember.TryGetValue("newusername", out JToken? newnametoken) && jObjMember.TryGetValue("email", out JToken? emailtoken))
                        {
                            string oldname = oldnametoken.ToString().Trim();
                            string newname = newnametoken.ToString().Trim();
                            string email = emailtoken.ToString().Trim();

                            if (string.IsNullOrWhiteSpace(oldname) || string.IsNullOrWhiteSpace(newname))
                            {
                                code = 400;
                                statusMessage = "Username was empty or null";
                            }
                            // Cheking that username, password and email are of valid length and form
                            else if (newname.Length < 5 || newname.Length > 20 || string.IsNullOrWhiteSpace(email) || email.Length < 5 || email.Length > 50 || !email.Contains('@'))
                            {
                                code = 400;
                                statusMessage = "Invalid username or email";
                            }
                            else if (isAdmin || identity.Name == oldname)
                            {
                                status = Database.Instance.EditUser(oldname, newname, email);
                                code = int.Parse(status[0]);
                                statusMessage = status[1];
                            }
                            else
                            {
                                code = 403;
                                statusMessage = "Request denied. Only admins can change other users information";
                            }
                        }
                        else
                        {
                            code = 400;
                            statusMessage = "No valid JSON in request body";
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

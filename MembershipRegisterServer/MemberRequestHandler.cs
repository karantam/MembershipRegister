using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MembershipRegisterServer
{
    internal class MemberRequestHandler : RequestHandler
    {
        public const string NAME = "/Member";

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
                Program.Log($"ERROR in /Member: {code} {message}");
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
            int code;
            string statusMessage;
            int j = 0;
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

                    // Creating a member object from the user data
                    if (jObjMember.TryGetValue("id", out JToken? idtoken) && jObjMember.TryGetValue("firstname", out JToken? firstnametoken) && jObjMember.TryGetValue("lastname", out JToken? lastnametoken)
                        && jObjMember.TryGetValue("birthdate", out JToken? birthdatetoken) && jObjMember.TryGetValue("address", out JToken? addresstoken) && jObjMember.TryGetValue("phone", out JToken? phonetoken)
                        && jObjMember.TryGetValue("email", out JToken? emailtoken))
                    {
                        string id = idtoken.ToString();
                        string firstname = firstnametoken.ToString();
                        string lastname = lastnametoken.ToString();
                        string birthdate = birthdatetoken.ToString();
                        string address = addresstoken.ToString();
                        string phone = phonetoken.ToString();
                        string email = emailtoken.ToString();
                        //if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(firstname) || string.IsNullOrWhiteSpace(lastname) || string.IsNullOrWhiteSpace(birthdate) ||
                        //    string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(email))
                        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(firstname) || string.IsNullOrWhiteSpace(lastname))
                        {
                            code = 400;
                            statusMessage = "Members id firstname and lastname can't be empty";
                        }
                        else
                        {
                            List<KeyValuePair<string, string>> groups = new();
                            for (int i = 0; i > -1; i++)
                            {
                                if (jObjMember.TryGetValue($"team:{i}", out JToken? teamtoken) && jObjMember.TryGetValue($"position:{i}", out JToken? positiontoken))
                                {
                                    if (teamtoken != null && positiontoken != null)
                                    {
                                        string team = teamtoken.ToString();
                                        string position = positiontoken.ToString();
                                        //if (string.IsNullOrWhiteSpace(team) || string.IsNullOrWhiteSpace(position))
                                        if (string.IsNullOrWhiteSpace(team) || groups.Contains(new KeyValuePair<string, string>(team, position)))
                                        {
                                            j++;
                                        }
                                        else
                                        {

                                            groups.Add(new KeyValuePair<string, string>(team, position));
                                        }
                                    }
                                    else
                                    {
                                        j++;
                                    }
                                }
                                else
                                {
                                    i = -2;
                                }
                            }
                            // If birthdate was given try to parse is as DateTime
                            if (string.IsNullOrWhiteSpace(birthdate))
                            {
                                Member member = new(id, firstname, lastname, null, address, phone, email, groups);
                                status = Database.Instance.CreateMember(member);
                                code = int.Parse(status[0]);
                                statusMessage = status[1];
                            }
                            else if (DateTime.TryParse(birthdate, out DateTime birth))
                            {
                                Member member = new(id, firstname, lastname, birth, address, phone, email, groups);
                                status = Database.Instance.CreateMember(member);
                                code = int.Parse(status[0]);
                                statusMessage = status[1];
                            }
                            else
                            {
                                code = 400;
                                statusMessage = "birthdate was not in a valid format";
                            }
                            //Member member = new(id, firstname, lastname, birthdate, address, phone, email, groups);
                            //status = Database.Instance.CreateMember(member);
                            //code = int.Parse(status[0]);
                            //statusMessage = status[1];
                        }
                    }
                    else
                    {
                        code = 400;
                        statusMessage = "No valid member JSON in request body";
                    }
                }
                catch(Exception e)
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
                if (j == 0)
                {
                    response.StatusCode = code;
                    response.Close();
                }
                else
                {
                    statusMessage = $"Member created, but {j} groups were not created as they contained empty/null group names or duplicate entries";
                    response.StatusCode = code;
                    byte[] messageBytes = Encoding.UTF8.GetBytes(statusMessage);
                    response.OutputStream.Write(messageBytes, 0, messageBytes.Length);
                    response.Close();
                }
            }

            status[0] = code.ToString();
            status[1] = statusMessage;
            return status;
        }

        /*
         * HandleGETRequest method returns all members from the database to the user
         */
        public string[] HandleGETRequest(HttpListenerContext context)
        {
            string[] status = new string[2];
            HttpListenerResponse response = context.Response;
            int code = (int)HttpStatusCode.OK;
            string statusMessage = "";

            List<Member> people;
            people = Database.Instance.GetMembers();
            JsonArray responseMessage = new();
            foreach (Member member in people)
            {
                responseMessage.Add(member.ToJsonObject());
            }

            response.StatusCode = code;
            String messageStr = responseMessage.ToString();
            byte[] messageBytes = Encoding.UTF8.GetBytes(messageStr);
            response.OutputStream.Write(messageBytes, 0, messageBytes.Length);
            response.Close();
            Program.Log("GET request processed in /Member");

            status[0] = code.ToString();
            status[1] = statusMessage;
            return status;
        }
    }
}

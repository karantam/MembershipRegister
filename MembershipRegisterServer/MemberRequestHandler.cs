using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
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
            int k = 0;
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
                        // Creating a member object from the user data
                        if (jObjMember.TryGetValue("id", out JToken? idtoken) && jObjMember.TryGetValue("firstname", out JToken? firstnametoken) && jObjMember.TryGetValue("lastname", out JToken? lastnametoken)
                            && jObjMember.TryGetValue("birthdate", out JToken? birthdatetoken) && jObjMember.TryGetValue("address", out JToken? addresstoken) && jObjMember.TryGetValue("phone", out JToken? phonetoken)
                            && jObjMember.TryGetValue("email", out JToken? emailtoken))
                        {
                            string id = idtoken.ToString().Trim();
                            string firstname = firstnametoken.ToString().Trim();
                            string lastname = lastnametoken.ToString().Trim();
                            string birthdate = birthdatetoken.ToString().Trim();
                            string address = addresstoken.ToString().Trim();
                            string phone = phonetoken.ToString().Trim();
                            string email = emailtoken.ToString().Trim();
                            //if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(firstname) || string.IsNullOrWhiteSpace(lastname) || string.IsNullOrWhiteSpace(birthdate) ||
                            //    string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(email))
                            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(firstname) || string.IsNullOrWhiteSpace(lastname))
                            {
                                code = 400;
                                statusMessage = "Members id, firstname and lastname can't be empty";
                            }
                            else
                            {
                                List<KeyValuePair<string, string>> groups = new();
                                for (int i = 0; i > -1; i++)
                                {
                                    if (jObjMember.TryGetValue($"team:{i}", out JToken? teamtoken) && jObjMember.TryGetValue($"position:{i}", out JToken? positiontoken))
                                    {
                                        string team = teamtoken.ToString().Trim();
                                        string position = positiontoken.ToString().Trim();
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
                                        i = -2;
                                    }
                                }
                                if (string.IsNullOrWhiteSpace(birthdate))
                                {
                                    Member member = new(id, firstname, lastname, null, address, phone, email, groups);
                                    status = Database.Instance.CreateMember(member);
                                    code = int.Parse(status[0]);
                                    statusMessage = status[1];
                                }
                                // If birthdate was given try to parse is as DateTime
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
                    // Adding groups to a member
                    if (action == "addgroup")
                    {
                        // Creating a member object from the user data
                        if (jObjMember.TryGetValue("id", out JToken? idtoken))
                        {
                            string id = idtoken.ToString().Trim();
                            if (string.IsNullOrWhiteSpace(id))
                            {
                                code = 400;
                                statusMessage = "Members id can't be empty";
                            }
                            else
                            {
                                List<KeyValuePair<string, string>> groups = new();
                                for (int i = 0; i > -1; i++)
                                {
                                    if (jObjMember.TryGetValue($"team:{i}", out JToken? teamtoken) && jObjMember.TryGetValue($"position:{i}", out JToken? positiontoken))
                                    {
                                        string team = teamtoken.ToString().Trim();
                                        string position = positiontoken.ToString().Trim();
                                        //if (string.IsNullOrWhiteSpace(team) || string.IsNullOrWhiteSpace(position))
                                        if (string.IsNullOrWhiteSpace(team) || groups.Contains(new KeyValuePair<string, string>(team, position)))
                                        {
                                            k++;
                                        }
                                        else
                                        {

                                            groups.Add(new KeyValuePair<string, string>(team, position));
                                        }
                                    }
                                    else
                                    {
                                        i = -2;
                                    }
                                }
                                status = Database.Instance.AddGroup(id, groups);
                                code = int.Parse(status[0]);
                                statusMessage = status[1];
                                k += int.Parse(status[2]);
                            }
                        }
                        else
                        {
                            code = 400;
                            statusMessage = "No valid JSON in request body";
                        }
                    }

                    // Updating the data of the given member
                    else if (action == "edit")
                    {
                        // Creating a member object from the user data and getting the original member id
                        if (jObjMember.TryGetValue("oldid", out JToken? oldidtoken) && jObjMember.TryGetValue("id", out JToken? idtoken) && jObjMember.TryGetValue("firstname", out JToken? firstnametoken) && jObjMember.TryGetValue("lastname", out JToken? lastnametoken)
                            && jObjMember.TryGetValue("birthdate", out JToken? birthdatetoken) && jObjMember.TryGetValue("address", out JToken? addresstoken) && jObjMember.TryGetValue("phone", out JToken? phonetoken)
                            && jObjMember.TryGetValue("email", out JToken? emailtoken))
                        {
                            string oldid = oldidtoken.ToString().Trim();
                            string id = idtoken.ToString().Trim();
                            string firstname = firstnametoken.ToString().Trim();
                            string lastname = lastnametoken.ToString().Trim();
                            string birthdate = birthdatetoken.ToString().Trim();
                            string address = addresstoken.ToString().Trim();
                            string phone = phonetoken.ToString().Trim();
                            string email = emailtoken.ToString().Trim();

                            if (string.IsNullOrWhiteSpace(oldid) || string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(firstname) || string.IsNullOrWhiteSpace(lastname))
                            {
                                code = 400;
                                statusMessage = "Members id, firstname and lastname can't be empty";
                            }
                            else
                            {
                                List<KeyValuePair<string, string>> groups = new();

                                if (string.IsNullOrWhiteSpace(birthdate))
                                {
                                    Member member = new(id, firstname, lastname, null, address, phone, email, groups);
                                    status = Database.Instance.EditMember(oldid, member);
                                    code = int.Parse(status[0]);
                                    statusMessage = status[1];
                                }
                                // If birthdate was given try to parse is as DateTime
                                else if (DateTime.TryParse(birthdate, out DateTime birth))
                                {
                                    Member member = new(id, firstname, lastname, birth, address, phone, email, groups);
                                    status = Database.Instance.EditMember(oldid, member);
                                    code = int.Parse(status[0]);
                                    statusMessage = status[1];
                                }
                                else
                                {
                                    code = 400;
                                    statusMessage = "birthdate was not in a valid format";
                                }
                            }
                        }
                        else
                        {
                            code = 400;
                            statusMessage = "No valid JSON in request body";
                        }
                    }

                    // Updating given group
                    else if (action == "editgroup")
                    {
                        // Getting the member id and old and new group
                        if (jObjMember.TryGetValue("id", out JToken? idtoken) && jObjMember.TryGetValue($"oldteam", out JToken? oldteamtoken) && jObjMember.TryGetValue($"oldposition", out JToken? oldpositiontoken)
                            && jObjMember.TryGetValue($"newteam", out JToken? newteamtoken) && jObjMember.TryGetValue($"newposition", out JToken? newpositiontoken))
                        {
                            string id = idtoken.ToString().Trim();
                            string oldteam = oldteamtoken.ToString().Trim();
                            string oldposition = oldpositiontoken.ToString().Trim();
                            string newteam = newteamtoken.ToString().Trim();
                            string newposition = newpositiontoken.ToString().Trim();

                            if (string.IsNullOrWhiteSpace(id))
                            {
                                code = 400;
                                statusMessage = "Members id can't be empty";
                            }
                            else if (string.IsNullOrWhiteSpace(oldteam) || string.IsNullOrWhiteSpace(newteam))
                            {
                                code = 400;
                                statusMessage = "group name can't be empty";
                            }
                            else
                            {
                                KeyValuePair<string, string> oldgroup = new KeyValuePair<string, string>(oldteam, oldposition);
                                KeyValuePair<string, string> newgroup = new KeyValuePair<string, string>(newteam, newposition);
                                status = Database.Instance.EditGroup(id, oldgroup, newgroup);
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

                    // Deleting given member from the database
                    else if (action == "delete")
                    {
                        // Getting the member id
                        if (jObjMember.TryGetValue("id", out JToken? idtoken))
                        {
                            string id = idtoken.ToString().Trim();

                            if (string.IsNullOrWhiteSpace(id))
                            {
                                code = 400;
                                statusMessage = "Members id can't be empty";
                            }
                            else
                            {
                                status = Database.Instance.RemoveMember(id);
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

                    // Deleting groups from a given member
                    else if (action == "deletegroup")
                    {
                        // Getting the member id
                        if (jObjMember.TryGetValue("id", out JToken? idtoken))
                        {
                            string id = idtoken.ToString().Trim();

                            if (string.IsNullOrWhiteSpace(id))
                            {
                                code = 400;
                                statusMessage = "Members id can't be empty";
                            }
                            else
                            {
                                List<KeyValuePair<string, string>> groups = new();
                                for (int i = 0; i > -1; i++)
                                {
                                    if (jObjMember.TryGetValue($"team:{i}", out JToken? teamtoken) && jObjMember.TryGetValue($"position:{i}", out JToken? positiontoken))
                                    {
                                        string team = teamtoken.ToString().Trim();
                                        string position = positiontoken.ToString().Trim();
                                        groups.Add(new KeyValuePair<string, string>(team, position));
                                    }
                                    else
                                    {
                                        i = -2;
                                    }
                                }
                                status = Database.Instance.RemoveGroup(id, groups);
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

                    // Giving an error if action was given but wasn't addgroup, edit, editgroup, delete or deletegroup
                    else
                    {
                        code = 400;
                        statusMessage = "Invalid action";
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
                if (j > 0)
                {
                    statusMessage = $"Member created, but {j} groups were not created as they contained empty/null group names or duplicate entries";
                    response.StatusCode = code;
                    byte[] messageBytes = Encoding.UTF8.GetBytes(statusMessage);
                    response.OutputStream.Write(messageBytes, 0, messageBytes.Length);
                    response.Close();
                }
                else if (k > 0)
                {
                    statusMessage = $"{k} groups were not added as they contained empty/null group names or duplicate entries";
                    response.StatusCode = code;
                    byte[] messageBytes = Encoding.UTF8.GetBytes(statusMessage);
                    response.OutputStream.Write(messageBytes, 0, messageBytes.Length);
                    response.Close();
                }
                else
                {
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

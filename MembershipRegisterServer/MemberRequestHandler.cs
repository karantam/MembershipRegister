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
            string[] status = new string[2];
            int code = (int)HttpStatusCode.OK;
            string message = "";

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
            int code = (int)HttpStatusCode.OK;
            string statusMessage = "";
            if (request.ContentType == null)
            {
                code = 400;
                statusMessage = "No Content-Type in request";
            }
            else if (request.ContentType == "application/json")
            {
                StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8);
                string body = reader.ReadToEnd();
                request.InputStream.Close();
                reader.Close();
                string jsonMember = JsonSerializer.Serialize(body);
                Program.Log(body);
                //Program.Log(jsonMember);
                JObject jObjMember = JObject.Parse(body);
                // Creating a member object from the user data
                string id = (string)jObjMember.SelectToken("id");
                string firstname = (string)jObjMember.SelectToken("firstname");
                string lastname = (string)jObjMember.SelectToken("lastname");
                string birthdate = (string)jObjMember.SelectToken("birthdate");
                string address = (string)jObjMember.SelectToken("address");
                string phone = (string)jObjMember.SelectToken("phone");
                string email = (string)jObjMember.SelectToken("email");
                List<KeyValuePair<string, string>> groups = new List<KeyValuePair<string, string>>();
                for (int i = 0; i > -1; i++)
                {
                    Program.Log($"{i}");
                    if (jObjMember.TryGetValue($"team:{i}", out JToken Teamtoken) && jObjMember.TryGetValue($"position:{i}", out JToken Positiontoken))
                    {
                        groups.Add(new KeyValuePair<string, string>((string)Teamtoken, (string)Positiontoken));
                    }
                    else
                    {
                        i = -2;
                    }
                }
                Member member = new Member(id,firstname, lastname, birthdate, address, phone, email, groups);
                status = Database.Instance.CreateMember(member);
                code = int.Parse(status[0]);
                statusMessage = status[1];


            }
            else
            {
                code = 415;
                statusMessage = "Content-Type must be application/json";
            }

            if (code < 400)
            {
                response.StatusCode = code;
                response.Close();
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
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            int code = (int)HttpStatusCode.OK;
            string statusMessage = "";

            List<Member> people = new List<Member>();
            people = Database.Instance.GetMembers();
            JsonArray responseMessage = new JsonArray();
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

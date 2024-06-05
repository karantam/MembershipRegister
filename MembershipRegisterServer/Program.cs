using System.Net;

namespace MembershipRegisterServer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Log("Launching Server...");

            
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8001/");

            HandlerChooser chooser = new HandlerChooser();

            listener.Start();
            Boolean running = true;

            //HttpListenerContext context = listener.GetContext();
            //chooser.HandleContext(context);

            //HttpListenerRequest request = context.Request;
            //HttpListenerResponse response = context.Response;
            Task.Factory.StartNew(() => Listen(listener, chooser));
            try
            {
                while (running)
                {
                    String shutdown = Console.ReadLine();
                    if (shutdown.Equals("/quit"))
                    {
                        //Setting running to false and waiting 3 second so ongoing requests can finish before closing the HttpListener
                        running = false;
                        Thread.Sleep(3000);
                    }
                    else
                    {
                        Log("Type /quit to shut down the server");
                    }
                }
            }
            finally
            {
                listener.Stop();
                //listener.Close();
                Log("Server has been shut down");
            }
            

            /*
             * Database tests
             *
            Database.Instance.Open("Dataa.db");
            List<KeyValuePair<string, string>> teams = new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>("hallitus", "pomo"),
                new KeyValuePair<string, string>("PR", ""),
                new KeyValuePair<string, string>("HR", "Konsultti")
            };
            List<KeyValuePair<string, string>> teams2 = new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>("Siivous", "Harjoittelija")
            };
            List<KeyValuePair<string, string>> teamsadd = new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>("R&D", "Sijainen"),
                new KeyValuePair<string, string>("R&D", "Sijainen2"),
                new KeyValuePair<string, string>("R&D", "Sijainen3")
            };
            List<KeyValuePair<string, string>> teamsremove = new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>("R&D", "Sijainen2"),
                new KeyValuePair<string, string>("R&D", "Sijainen3")
            };
            Member member = new Member("a'aaaaaaaaa", "Matti", "Meikalaine", DateTime.Now.ToShortDateString(), "Tie 1", "12312", "email@email", teams);
            Member member2 = new Member("qq'qqqqqqqq", "Antti", "Vuori", DateTime.Now.ToShortDateString(), "Tie 4", "63462", "email2@email2", teams2);
            Database.Instance.CreateMember(member);
            Database.Instance.CreateMember(member2);
            Database.Instance.AddGroup("qq'qqqqqqqq", teamsadd);
            Database.Instance.RemoveGroup("qq'qqqqqqqq", teamsremove);
            Database.Instance.GetMember();
            Database.Instance.RemoveMember("a'aaaaaaaaa");
            Database.Instance.GetMember();
            Member member2edited = new Member("qq'ooo", "Jussi", "Meri", DateTime.Now.ToShortDateString(), "Tie 24", "63000", "email2@email2.com", teams2);
            Database.Instance.EditMember("qq'qqqqqqqq", member2edited);
            Database.Instance.EditGroup("qq'ooo", teams2[0], teams[0]);
            Database.Instance.GetMember();
            Database.Instance.CloseDB();

             */

        }

        /*
         * Log method is the print method for all server printouts
         */
        public static void Log(string message)
        {
            Console.WriteLine(DateTime.Now + " " + message);
        }

        public static void Listen(HttpListener listener, HandlerChooser chooser) {
            try
            {
                while (listener.IsListening)
                {
                    Task<HttpListenerContext> context = listener.GetContextAsync();
                    Task.Factory.StartNew(() => chooser.HandleContext(context.Result));
                }
            }
            catch (Exception e)
            {
                //Log(e.ToString());
            }
        }
    }
}
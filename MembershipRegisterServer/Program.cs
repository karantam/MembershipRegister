using System.Net;

namespace MembershipRegisterServer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Log("Launching Server...");

            Database.Instance.Open("Dataa.db");

            HttpListener listener = new();
            listener.Prefixes.Add("https://localhost:8001/");
            listener.AuthenticationSchemes = AuthenticationSchemes.Basic;

            HandlerChooser chooser = new();
            chooser.AddHandler(new MemberRequestHandler());

            listener.Start();
            Boolean running = true;
            Task.Factory.StartNew(() => ListenAsync(listener, chooser));
            Log("Server is online");
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
                //listener.Stop();
                listener.Close();
                Database.Instance.CloseDB();
                Log("Server has been shut down");
            }
        }

        /*
         * Log method is the print method for all server printouts
         */
        public static void Log(string message)
        {
            Console.WriteLine(DateTime.Now + " " + message);
        }

        public static async void ListenAsync(HttpListener listener, HandlerChooser chooser) {
            try
            {
                while (listener.IsListening)
                {
                    //Task<HttpListenerContext> context = listener.GetContextAsync();
                    HttpListenerContext context = await listener.GetContextAsync();
                    _ = Task.Factory.StartNew(() => chooser.HandleContext(context));
                }
            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
        }
    }
}
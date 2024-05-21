namespace MembershipRegisterServer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Log("Launching Chatserver...");
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
            Member member = new Member("aaaaaaaaaa", "Matti", "Meikalaine", DateTime.Now.ToShortDateString(), "Tie 1", "12312", "email@email", teams);
            Member member2 = new Member("qqqqqqqqqq", "Antti", "Vuori", DateTime.Now.ToShortDateString(), "Tie 4", "63462", "email2@email2", teams2);
            Database.Instance.CreateMember(member);
            Database.Instance.CreateMember(member2);
            Database.Instance.AddGroup("qqqqqqqqqq", teamsadd);
            Database.Instance.RemoveGroup("qqqqqqqqqq", teamsremove);
            Database.Instance.GetMember();
            Database.Instance.RemoveMember("aaaaaaaaaa");
            Database.Instance.GetMember();
            Database.Instance.CloseDB();

        }

        /*
         * Log method is the print method for all server printouts
         */
        public static void Log(string message)
        {
            Console.WriteLine(DateTime.Now + " " + message);
        }
    }
}
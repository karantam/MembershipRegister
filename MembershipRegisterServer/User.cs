using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MembershipRegisterServer
{
    public class User
    {
        private string username, password, email;

        public User(string usernamepar, string passwordpar, string emailpar)
        {
            this.username = usernamepar;
            this.password = passwordpar;
            this.email = emailpar;
        }

        //Get methods
        public string GetUserID()
        {
            return this.username;
        }

        public string GetPassword()
        {
            return this.password;
        }

        public string GetEmail()
        {
            return this.email;
        }
    }
}

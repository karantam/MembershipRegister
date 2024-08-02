using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MembershipRegisterServer
{
    public class User
    {
        private string username, password, salt, email;

        public User(string usernamepar, string passwordpar, string emailpar)
        {
            this.username = usernamepar;
            this.password = passwordpar;
            this.salt = "";
            this.email = emailpar;
        }

        public User(string usernamepar, string passwordpar, string saltpar, string emailpar)
        {
            this.username = usernamepar;
            this.password = passwordpar;
            this.salt = saltpar;
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

        public string GetSalt()
        {
            return this.salt;
        }

        public string GetEmail()
        {
            return this.email;
        }
    }
}

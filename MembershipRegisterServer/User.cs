using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MembershipRegisterServer
{
    public class User
    {
        private string username, password, salt, email, role;

        public User(string usernamepar, string emailpar)
        {
            this.username = usernamepar;
            this.password = "";
            this.salt = "";
            this.email = emailpar;
            this.role = "user";
        }

        public User(string usernamepar, string passwordpar, string emailpar)
        {
            this.username = usernamepar;
            this.password = passwordpar;
            this.salt = "";
            this.email = emailpar;
            this.role = "user";
        }

        public User(string usernamepar, string passwordpar, string saltpar, string emailpar)
        {
            this.username = usernamepar;
            this.password = passwordpar;
            this.salt = saltpar;
            this.email = emailpar;
            this.role = "user";
        }

        public JsonObject ToJsonObject()
        {
            JsonObject json;
            json = new()
            {
                { "username", this.username },
                { "email", this.email },
                { "role", this.role }
            };

            return json;
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

        public string GetRole()
        {
            return this.role;
        }

        // Set method for role
        public void SetRole(Boolean admin)
        {
            if (admin)
            {
                this.role = "admin";
            }
        }
    }
}

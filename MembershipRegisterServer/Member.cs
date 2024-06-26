﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MembershipRegisterServer
{
    public class Member
    {
        private string memberID, firstname, lastname, address, phone, email;
        private DateTime? birth;
        List<KeyValuePair<string, string>> groups;

        // Constructor for the Member object
        public Member(string memberIDpar, string firstnamepar, string lastnamepar, DateTime? birthpar, string addresspar, string phonepar, string emailpar, List<KeyValuePair<string, string>> groupspar)
        {
            this.memberID = memberIDpar;
            this.firstname = firstnamepar;
            this.lastname = lastnamepar;
            //this.birthdate = birthdatepar;
            this.birth = birthpar;
            this.address = addresspar;
            this.phone = phonepar;
            this.email = emailpar;
            this.groups = groupspar;
        }

        public JsonObject ToJsonObject()
        {
            JsonObject json;
            if (this.birth != null)
            {
                json = new()
            {
                { "id", this.memberID },
                { "firstname", this.firstname },
                { "lastname", this.lastname },
                //{ "birthdate", this.birthdate },
                { "birthdate", this.birth?.ToString("dd.MM.yyyy") },
                { "address", this.address },
                { "phone", this.phone },
                { "email", this.email }
            };
            }
            else
            {
                json = new()
            {
                { "id", this.memberID },
                { "firstname", this.firstname },
                { "lastname", this.lastname },
                //{ "birthdate", this.birthdate },
                { "birthdate", null },
                { "address", this.address },
                { "phone", this.phone },
                { "email", this.email }
            };
            }
            for (int i = 0; i< this.groups.Count; i++)
            {
                json.Add($"team:{i}", this.groups[i].Key);
                json.Add($"position:{i}", this.groups[i].Value);
            }

            return json;
        }

        // Get methods for the Member object
        public string GetMemberID()
        {
            return this.memberID;
        }

        public string GetFirstname()
        {
            return this.firstname;
        }

        public string GetLasttname()
        {
            return this.lastname;
        }

        /*
        public string GetBirthdate()
        {
            return this.birthdate;
        }
        */

        public DateTime? GetBirthdate()
        {
            return this.birth;
        }

        public int GetBirthdateAsInt()
        {
            if(this.birth != null)
            {
                return int.Parse(this.birth?.ToString("yyyyMMdd"));
            }
            else
            {
                // 0 is used to represent null value
                return 0;
            }
        }

        public string GetAddress()
        {
            return this.address;
        }

        public string GetPhone()
        {
            return this.phone;
        }

        public string GetEmail()
        {
            return this.email;
        }

        public List<KeyValuePair<string, string>> GetGroups()
        {
            return this.groups;
        }
    }
}

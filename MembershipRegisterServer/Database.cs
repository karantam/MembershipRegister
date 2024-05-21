using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Data.Sqlite;

/*
 * Database class handles database operations
 */

namespace MembershipRegisterServer
{
    public class Database
    {
        SqliteConnection dbConnection;
        SqliteCommand dbCommand;
        SqliteDataReader dbReader;
        SqliteTransaction dbTransaction;

        /*private static Database singleton = null;
        private static readonly object padlock = new object();

        public static Database getInstance()
        {
            lock (padlock)
            {
                if (null == singleton)
                {
                    singleton = new Database();
                }
                return singleton;
            }
        }*/

        private static readonly Lazy<Database> lazy = new(() => new Database());

        public static Database Instance { get { return lazy.Value; } }

        private Database()
        {
        }

        /*
         * open method checks if the database exists and if it dosen't calls
         * initializeDatabase method to create it. Then it creates a connection to the
         * database
         */
        public void Open(string dbName) //throws SQLException
        {
            FileInfo dbFile = new FileInfo(dbName);
            Boolean exists = dbFile.Exists;
            String path = dbFile.FullName;
            //String database = "jdbc:sqlite:" + path;
            string database = @"Data Source=" + path;
            dbConnection = new SqliteConnection(database);
            //dbConnection.Open();
            dbCommand = new SqliteCommand("", dbConnection);
            if (!exists) {
                InitializeDatabase();
            } else {
                Program.Log("Using already existing database");
            }
        }

        /*
         * initializeDatabase method creates the database
         */
        private Boolean InitializeDatabase() //throws SqlException
        {
            if (null != dbConnection) {
                // Creating tables in the database.
                string memberDB = "CREATE TABLE members (memberID varchar(50) PRIMARY KEY, firstname varchar(50) NOT NULL, familyname varchar(50) NOT NULL, birthdate varchar(50), address varchar(50), phone varchar(50), email varchar(50))";
                string groupDB = "CREATE TABLE teams (id INTEGER PRIMARY KEY AUTOINCREMENT, memberID varchar(50) NOT NULL, team varchar(50) NOT NULL, position varchar(50))";
                dbConnection.Open();
                dbTransaction = dbConnection.BeginTransaction();
                dbCommand.Transaction = dbTransaction;
                try {
                    //dbCommand = dbConnection.CreateCommand();
                    dbCommand.CommandText = memberDB;
                    dbCommand.ExecuteNonQuery();
                    dbCommand.CommandText = groupDB;
                    dbCommand.ExecuteNonQuery();
                    Program.Log("DB successfully created");
                    dbTransaction.Commit();
                    dbConnection.Close();
                    return true;
                } catch (Exception e)
                {
                    Program.Log(e.ToString());
                    //Program.Log("ERROR: SQLException while creating the database");
                    dbTransaction.Rollback();
                    
                }
                finally
                {
                    dbConnection.Close();
                }
            }

            Program.Log("DB creation failed");
            return false;
        }

        /*
         * closeDB method closes the database connection
         */
        public void CloseDB() //throws SQLException
        {
            if (null != dbConnection) {
                dbConnection.Close();
                Program.Log("closing database connection");
                dbConnection = null;
            }
        }

        /*
         * CreateMember method saves a new member into the database
         */
        public Boolean CreateMember(Member member)
        {
            //Boolean exists = false;
            if (!MemberExists(member.GetMemberID()))
            {
                String memberdata = $"INSERT INTO members (memberID,firstname,familyname,birthdate,address,phone,email) VALUES('{member.GetMemberID().Replace("'", "''")}','{member.GetFirstname().Replace("'", "''")}','{member.GetLasttname().Replace("'", "''")}','{member.GetBirthdate().Replace("'", "''")}','{member.GetAddress().Replace("'", "''")}','{member.GetPhone().Replace("'", "''")}','{member.GetEmail().Replace("'", "''")}')";
                List<KeyValuePair<string, string>> groups = member.GetGroups();
                dbConnection.Open();
                dbTransaction = dbConnection.BeginTransaction();
                dbCommand.Transaction = dbTransaction;
                try
                {
                    //dbCommand = dbConnection.CreateCommand();
                    dbCommand.CommandText = memberdata;
                    dbCommand.ExecuteNonQuery();
                    for (int i = 0; i<groups.Count; i++)
                    {
                        String teamdata = $"INSERT INTO teams (memberID,team,position) VALUES('{member.GetMemberID().Replace("'", "''")}','{groups[i].Key.Replace("'", "''")}','{groups[i].Value.Replace("'", "''")}')";
                        dbCommand.CommandText = teamdata;
                        dbCommand.ExecuteNonQuery();
                    }
                    dbTransaction.Commit();
                    Program.Log("Member created");

                    //exists = true;
                    return true;
                }
                catch (Exception e)
                {
                    Program.Log(e.ToString());
                    //Program.Log("ERROR: SQLException while creating the member");
                    dbTransaction.Rollback();
                    return false;
                }
                finally
                {
                    dbConnection.Close();
                }
            }
            else
            {
                Program.Log("Member creation failed. MemberID already in use");
                return false;
            }
            //return exists;
        }

        /*
         * RemoveMember method removes a member from the membees table and all entries related to the member from the teams table
         */
        public Boolean RemoveMember(String memberID)
        {
            if (MemberExists(memberID))
            {
                String memberdata = $"DELETE FROM members WHERE memberID = '{memberID.Replace("'", "''")}'";
                String groupddata = $"DELETE FROM teams WHERE memberID = '{memberID.Replace("'", "''")}'";
                dbConnection.Open();
                dbTransaction = dbConnection.BeginTransaction();
                dbCommand.Transaction = dbTransaction;
                try
                {
                    //dbCommand = dbConnection.CreateCommand();
                    dbCommand.CommandText = memberdata;
                    dbCommand.ExecuteNonQuery();
                    dbCommand.CommandText = groupddata;
                    dbCommand.ExecuteNonQuery();
                    dbTransaction.Commit();
                    Program.Log("Member deleted");

                    return true;
                }
                catch (Exception e)
                {
                    Program.Log(e.ToString());
                    //Program.Log("ERROR: SQLException while creating the member");
                    dbTransaction.Rollback();
                    return false;
                }
                finally
                {
                    dbConnection.Close();
                }
            }
            else
            {
                Program.Log("Member does not exist");
                return false;
            }
        }

        /*
         * AddGroup method adds a new group for a member in the database
         */
        public Boolean AddGroup(String memberID, List<KeyValuePair<string, string>> newgroups)
        {
            if (MemberExists(memberID))
            {
                for (int i = 0; i < newgroups.Count; i++)
                {
                    if (!GroupExists(memberID, newgroups[i].Key, newgroups[i].Value))
                    {
                        dbConnection.Open();
                        dbTransaction = dbConnection.BeginTransaction();
                        dbCommand.Transaction = dbTransaction;
                        try
                        {
                            String teamdata = $"INSERT INTO teams (memberID,team,position) VALUES('{memberID.Replace("'", "''")}','{newgroups[i].Key.Replace("'", "''")}','{newgroups[i].Value.Replace("'", "''")}')";
                            dbCommand.CommandText = teamdata;
                            dbCommand.ExecuteNonQuery();
                            dbTransaction.Commit();
                            dbConnection.Close();
                            Program.Log("Group added");
                        }
                        catch (Exception e)
                        {
                            Program.Log(e.ToString());
                            //Program.Log("ERROR: SQLException while creating the member");
                            dbTransaction.Rollback();
                            return false;
                        }
                        finally
                        {
                            dbConnection.Close();
                        }
                    }
                    else
                    {
                        Program.Log("Duplicate entry not added into the database");
                    }
                }
                Program.Log("All non duplicate groups added");
                return true;
            }
            else
            {
                Program.Log("Member does not exist in the database");
                return false;
            }
        }

        /*
         * RemoveGroup method removes a group from a member in the database
         */
        public Boolean RemoveGroup(String memberID, List<KeyValuePair<string, string>> removedgroups)
        {
            if (MemberExists(memberID))
            {
                for (int i = 0; i < removedgroups.Count; i++)
                {
                    if (GroupExists(memberID, removedgroups[i].Key, removedgroups[i].Value))
                    {
                        dbConnection.Open();
                        dbTransaction = dbConnection.BeginTransaction();
                        dbCommand.Transaction = dbTransaction;
                        try
                        {
                            String teamdata = $"DELETE FROM teams WHERE memberID = '{memberID.Replace("'", "''")}' AND team = '{removedgroups[i].Key.Replace("'", "''")}' AND position = '{removedgroups[i].Value.Replace("'", "''")}'";
                            dbCommand.CommandText = teamdata;
                            dbCommand.ExecuteNonQuery();
                            dbTransaction.Commit();
                            dbConnection.Close();
                            Program.Log("Group deleted");
                        }
                        catch (Exception e)
                        {
                            Program.Log(e.ToString());
                            //Program.Log("ERROR: SQLException while creating the member");
                            dbTransaction.Rollback();
                            return false;
                        }
                        finally
                        {
                            dbConnection.Close();
                        }
                    }
                    else
                    {
                        Program.Log("Group to be deleted doesn't exist");
                    }
                }
                Program.Log("All given groups deleted");
                return true;
            }
            else
            {
                Program.Log("Member does not exist in the database");
                return false;
            }
        }

        /*
         * GetMember method retrieves all member information from the database.
         */
        public List<Member> GetMember() //throws SQLException
        {
            String query = "select * from members";
            List<Member> people = new List<Member>();
            dbConnection.Open();
            try {
                dbCommand = dbConnection.CreateCommand();
                dbCommand.CommandText = query;
                dbReader = dbCommand.ExecuteReader();
                while (dbReader.Read()) {
                    for(int i = 0; i < 7; i++)
                    {
                        Console.Write($"{dbReader.GetString(i)} ");
                    }
                    String query2 = $"SELECT team, position FROM teams WHERE memberID = '{dbReader.GetString(0).Replace("'", "''")}'";
                    SqliteCommand dbCommand2 = dbConnection.CreateCommand(); 
                    dbCommand2.CommandText = query2;
                    SqliteDataReader dbReader2 = dbCommand2.ExecuteReader();
                    List<KeyValuePair<string, string>> teams = new List<KeyValuePair<string, string>>();
                    while (dbReader2.Read())
                    {
                        Console.Write($"({dbReader2.GetString(0)} : {dbReader2.GetString(1)}) ");

                        teams.Add(new KeyValuePair<string, string>(dbReader2.GetString(0), dbReader2.GetString(1)));
                    }
                    Console.WriteLine();
                    people.Add(new Member(dbReader.GetString(0), dbReader.GetString(1), dbReader.GetString(2), dbReader.GetString(3), dbReader.GetString(4), dbReader.GetString(5), dbReader.GetString(6), teams));
                    
                }

            } catch (Exception e) {
                Program.Log(e.ToString());
                //Program.Log("ERROR: SQLException while reading user information from database");
            }
            finally
            {
                dbConnection.Close();
            }
            return people;
        }

        /*
         * MemberExists method checks if the given memberID already exists.
         */
        public Boolean MemberExists(String memberID) //throws SQLException
        {
            // selecting memberID with the given name from the members table
            //String memberIDExists = "SELECT memberID FROM members WHERE memberID = '" + memberID.Replace("'", "''") + "'";
            String memberIDExists = $"SELECT memberID FROM members WHERE memberID = '{memberID.Replace("'", "''")}'";
            Boolean exists = false;
            dbConnection.Open();
            try
            {
                dbCommand = dbConnection.CreateCommand();
                dbCommand.CommandText = memberIDExists;
                dbReader = dbCommand.ExecuteReader();
                exists = dbReader.Read();
            }
            catch (Exception e)
            {
                Program.Log(e.ToString());
                //Program.Log("SQLException while checking if a memberID exists.");
            }
            finally
            {
                dbConnection.Close();
            }
            return exists;
        }

        /*
         * GroupExists method checks if the given member already has the given group and position combination.
         */
        public Boolean GroupExists(String memberID, String group, String position) //throws SQLException
        {
            // selecting entries with the given memberID, team and position
            String memberIDExists = $"SELECT memberID FROM teams WHERE memberID = '{memberID.Replace("'", "''")}' AND team = '{group.Replace("'", "''")}' AND position = '{position.Replace("'", "''")}'";
            Boolean exists = false;
            dbConnection.Open();
            try
            {
                SqliteCommand dbCommandG = dbConnection.CreateCommand();
                dbCommandG.CommandText = memberIDExists;
                dbReader = dbCommandG.ExecuteReader();
                exists = dbReader.Read();
            }
            catch (Exception e)
            {
                Program.Log(e.ToString());
                //Program.Log("SQLException while checking if a memberID exists.");
            }
            finally
            {
                dbConnection.Close();
            }
            return exists;
        }

    }
}

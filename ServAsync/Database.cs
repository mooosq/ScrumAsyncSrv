using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Npgsql;

namespace ServAsync
{
    class Database
    {
        // Obtain connection string information from the portal
        //
        private static string Host = "localhost";
        private static string User = "postgres";
        private static string DBname = "IO_database";
        private static string Password = "admin";
        private static string Port = "12345";

        string connString = String.Format("Server={0};Username={1};Database={2};Port={3};Password={4};SSLMode=Prefer", Host, User, DBname, Port, Password);
        TextBox txtBox;

        public Database(TextBox txtBox)
        {
            this.txtBox = txtBox;
        }

        public void setupDatabase()
        {
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();

                using (var command = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS users(name VARCHAR(50) not null, password VARCHAR(50) not null, primary key (name))", conn))
                {
                    command.ExecuteNonQuery();
                }

                using (var command = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS user_activity(name VARCHAR(50) not null, is_active BOOLEAN, foreign key (name) REFERENCES users(name))", conn))
                {
                    command.ExecuteNonQuery();
                }

                using (var command = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS high_scores(name VARCHAR(50) not null, best_score INTEGER, foreign key (name) REFERENCES users(name))", conn))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public bool userExists(String name)
        {
            String commandText;

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                commandText = string.Format("SELECT name FROM users WHERE name=\'{0}\'", name);

                using (var command = new NpgsqlCommand(commandText, conn))
                {
                    var reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        public String userPassword(String name)
        {
            String commandText;

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();

                if (userExists(name))
                {
                    commandText = string.Format("select password from users where name=\'{0}\'", name);

                    using (var command = new NpgsqlCommand(commandText, conn))
                    {
                        var reader = command.ExecuteReader();

                        //text = string.Format("HASLOOOO=({0})\n", reader.GetString(0));
                        //txtBox.Dispatcher.Invoke(delegate { txtBox.Text += text; });

                        if (reader.Read())
                        {
                            return reader.GetString(0);
                        }
                    }
                }
                txtBox.Dispatcher.Invoke(delegate { txtBox.Text += $"[{DateTime.Now}] Tried to log in to none existing user\n"; });
                return "";
            }
        }
        

        public bool addUser(String name, String password)
        {
            String commandText;

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();

                if(!userExists(name))
                {
                    commandText = string.Format("insert into users(name, password) values (\'{0}\', \'{1}\')", name, password);

                    using (var command = new NpgsqlCommand(commandText, conn))
                    {
                        var reader = command.ExecuteReader();
                    }
                    return true;
                }
                else
                {
                    txtBox.Dispatcher.Invoke(delegate { txtBox.Text += $"[{DateTime.Now}] Tried to add user that already exist\n"; });
                    return false;
                }
            }
        }

    }

}

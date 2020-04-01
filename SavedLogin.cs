using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpChromium
{
    class SavedLogin
    {
        public string Url;
        public string Username;
        public string Password;

        public SavedLogin(string url, string user, string pass)
        {
            Url = url;
            Username = user;
            Password = pass;
        }

        public void Print()
        {
            string user = Environment.GetEnvironmentVariable("USERNAME");
            Console.WriteLine("--- Chromium Credential (User: {0}) ---", user);
            Console.WriteLine("URL      : {0}", Url);
            Console.WriteLine("Username : {0}", Username);
            Console.WriteLine("Password : {0}", Password);
            Console.WriteLine();
        }
    }
}

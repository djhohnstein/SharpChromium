# SharpChrome

## Introduction

SharpChrome is a .NET 2.0 CLR project to retrieve data from Google Chrome. Currently, it can extract:

- Cookies (in JSON format)
- History (with associated cookies for each history item)
- Saved Logins

If SharpChrome is run from a high integrity context (such as Administrator), it will retrieve Chrome data for each user with Chrome installed.

Note: All cookies returned are in JSON format. If you have the extension "EditThisCookie" installed, you can simply copy and paste into the "Import" seciton of this browser addon to ride the extraction session.

## Usage

```
Usage:
    .\SharpChrome.exe arg0 [arg1 arg2 ...]

Arguments:
    all       - Retrieve all Chrome Bookmarks, History, Cookies and Logins.
    full      - The same as 'all'
    logins    - Retrieve all saved credentials that have non-empty passwords.
    history   - Retrieve user's history with a count of each time the URL was
                visited, along with cookies matching those items.
    cookies [domain1.com domain2.com] - Retrieve the user'scookies in JSON format.
                                        If domains are passed, then return only
                                        cookies matching those domains.
```

## Examples

Retrieve cookies associated with Google Docs and Github
```
.\SharpChrome.exe cookies docs.google.com github.com
```

Retrieve history items and their associated cookies.
```
.\SharpChrome.exe history
```

Retrieve saved logins (Note: Only displays those with non-empty passwords):
```
.\SharpChrome.exe logins
```

## Special Thanks

A large thanks to @plainprogrammer for their C#-SQLite project which allowed for native parsing of the SQLite files without having to reflectively load a DLL. Without their work this project would be nowhere near as clean as it is. That project can be found here: https://github.com/plainprogrammer/csharp-sqlite

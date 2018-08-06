# SharpChrome

## Introduction

SharpChrome is a .NET 2.0 CLR project to retrieve data from Google Chrome. Currently, it can extract:

- Cookies (in JSON format)
- History (with associated cookies for each history item)
- Saved Logins

If SharpChrome is run from a high integrity context (such as Administrator), it will retrieve Chrome data for each user with Chrome installed.

Note: All cookies returned are in JSON format. If you have the extension "EditThisCookie" installed, you can simply copy and paste into the "Import" seciton of this browser addon to ride the extraction session.

## Advantages

This rewrite has several advantages to previous implementations, which include:

- No Type compilation or reflection required
- .NET 2.0 compliant, so it can run across all Windows platforms
- Cookies are displayed in JSON format, for easy importing into EditThisCookie.
- No downloading SQLite assemblies from remote resources.

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
![cookies](images/cookies.png)

Retrieve history items and their associated cookies.
```
.\SharpChrome.exe history
```
![history](images/history_item.png)

Retrieve saved logins (Note: Only displays those with non-empty passwords):
```
.\SharpChrome.exe logins
```
![logins](images/logins.png)


## Notes on the SQLite Parser

The SQLite database parser is slightly bugged. This is due to the fact that the parser correctly detects data blobs as type System.Byte[], but it does not correctly detect columns of type System.Byte[]. As a result, the byte arrays get cast to the string literal "System.Byte[]", which is wrong. I haven't gotten to the root of this cause, but as a quick and dirty workaround I have encoded all blob values as Base64 strings. Thus if you wish to retrieve a value from a column whose regular data values would be a byte array, you'll need to Base64 decode them first.


## Special Thanks

A large thanks to @plainprogrammer for their C#-SQLite project which allowed for native parsing of the SQLite files without having to reflectively load a DLL. Without their work this project would be nowhere near as clean as it is. That project can be found here: https://github.com/plainprogrammer/csharp-sqlite

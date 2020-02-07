# Digital First Careers - Session Client

This project provides a session client which allows session management for Shell apps

## Getting Started

This is a self-contained Visual Studio 2019 solution containing a number of projects (web application, service and repository layers, with associated unit test and integration test projects).

### Installing

Clone the project and open the solution in Visual Studio 2019.

## Local Config Files

Once you have cloned the public repo you need to rename the appsettings files by removing the -template part from the configuration file names listed below.

| Location | Repo Filename | Rename to |
|-------|-------|-------|
| DFC.Session.Package.IntegrationTests | appsettings-template.json | appsettings.json |
| DFC.Session.Package | appsettings-template.json | appsettings.json |

## Configuring to run locally

The project contains a number of "appsettings-template.json" files which contain sample appsettings for the web app and the integration test projects. To use these files, rename them to "appsettings.json" and edit and replace the configuration item values with values suitable for your environment.


## Deployments

This package can be used as part of a larger solution that need to make use of session in Composite Shell

To use this package you will need to supply configuration settings from the hosting app.
For a example of how to do this please take a look at the Intergration Test that is part of this solution.


## Using this package
To use this package, you must create a section in your appsettings in the following format:
  ```
  "SessionConfig" : {
    "ApplicationName": "yourApplicationName",
    "Salt" : "somevalue" //optional parameter 
  }
  ```

  In your startup, call the following extension method:

  ```
  var serviceProvider = new ServiceCollection().AddSessionServices(sessionConfig);
  ```

  The three methods available on the ISessionClient are:
  1. NewSession() - Returns you a new DfcSessionObject to send to method 2
  2. CreateCookie() - Creates a cookie based on the DfcSessionObject and adds it to the HttpResponse
  3. TryFindSessionCode() - Finds the sessionId from 3 sources (in order of precedence):
      1. Cookie
      2. QueryString
      3. FormData

## Built With

* Microsoft Visual Studio 2019
* .Net Standard 2.0


# NotesFunction

An Azure Function for managing notes.

Stack:
- Azure Functions
- .NET7
- CosmosDB

This is the *v1* function - has standalone auth handled in isolation by the function via a table of users in CosmosDB.

The *master* and *v2* branches authorize users via Zitadel.

## Dependencies:

- The [`DZarsky.CommonLibraries.AzureFunctions`](https://nuget.dzarsky.eu/packages/dzarsky.commonlibraries.azurefunctions/1.1.1) library (version <1.2.0)
- Azure account
- CosmosDB instance

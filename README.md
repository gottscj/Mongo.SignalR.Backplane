# Mongo.SignalR.Backplane
MongoDB backplane for SignalR scale out

# Installation

To install Hangfire MongoDB Storage, run the following command in the Nuget Package Manager Console:

```
PM> Install-Package Mongo.SignalR.Backplane
```

## Usage ASP.NET Core

```csharp
var client = new MongoClient("mongodb://localhost:27017");

builder.Services
    .AddSignalR()
    .AddMongoBackplane(client); 
```
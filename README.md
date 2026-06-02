# Dibix
Seamlessly create use case oriented REST APIs based on T-SQL stored procedures.

[![Build Status](https://img.shields.io/azure-devops/build/serviceware/dibix/2/main)](https://dev.azure.com/serviceware/Dibix/_build/latest?definitionId=2&branchName=main) [![Test Status](https://img.shields.io/azure-devops/tests/serviceware/dibix/2/main)](https://dev.azure.com/serviceware/Dibix/_build/latest?definitionId=2&branchName=main) [![Code coverage](https://img.shields.io/azure-devops/coverage/serviceware/dibix/2/main)](https://dev.azure.com/serviceware/Dibix/_build/latest?definitionId=2&branchName=main)

## Artifacts
| Name | Version |
| - | - |
| [Dibix](https://www.nuget.org/packages/Dibix) | [![Dibix](https://img.shields.io/nuget/v/Dibix.svg)](https://www.nuget.org/packages/Dibix) |
| [Dibix.Dapper](https://www.nuget.org/packages/Dibix.Dapper) | [![Dibix.Dapper](https://img.shields.io/nuget/v/Dibix.Dapper.svg)](https://www.nuget.org/packages/Dibix.Dapper) |
| [Dibix.Http.Client](https://www.nuget.org/packages/Dibix.Http.Client) | [![Dibix.Http.Client](https://img.shields.io/nuget/v/Dibix.Http.Client.svg)](https://www.nuget.org/packages/Dibix.Http.Client) |
| [Dibix.Http.Server](https://www.nuget.org/packages/Dibix.Http.Server) | [![Dibix.Http.Server](https://img.shields.io/nuget/v/Dibix.Http.Server.svg)](https://www.nuget.org/packages/Dibix.Http.Server) |
| [Dibix.Http.Server.AspNet](https://www.nuget.org/packages/Dibix.Http.Server.AspNet) | [![Dibix.Http.Server.AspNet](https://img.shields.io/nuget/v/Dibix.Http.Server.AspNet.svg)](https://www.nuget.org/packages/Dibix.Http.Server.AspNet) |
| [Dibix.Http.Server.AspNetCore](https://www.nuget.org/packages/Dibix.Http.Server.AspNetCore) | [![Dibix.Http.Server.AspNetCore](https://img.shields.io/nuget/v/Dibix.Http.Server.AspNetCore.svg)](https://www.nuget.org/packages/Dibix.Http.Server.AspNetCore) |
| [Dibix.Sdk](https://www.nuget.org/packages/Dibix.Sdk) | [![Dibix.Sdk](https://img.shields.io/nuget/v/Dibix.Sdk.svg)](https://www.nuget.org/packages/Dibix.Sdk) |
| [Dibix.Testing](https://www.nuget.org/packages/Dibix.Testing) | [![Dibix.Testing](https://img.shields.io/nuget/v/Dibix.Testing.svg)](https://www.nuget.org/packages/Dibix.Testing) |
| [Dibix.Worker.Abstractions](https://www.nuget.org/packages/Dibix.Worker.Abstractions) | [![Dibix.Worker.Abstractions](https://img.shields.io/nuget/v/Dibix.Worker.Abstractions.svg)](https://www.nuget.org/packages/Dibix.Worker.Abstractions) |
| [Dibix.Http.Host](https://hub.docker.com/r/servicewareit/dibix-http-host) | [![Dibix.Http.Host](https://img.shields.io/docker/v/servicewareit/dibix-http-host?label=docker&sort=semver)](https://hub.docker.com/r/servicewareit/dibix-http-host/tags) |
| [Dibix.Worker.Host](https://hub.docker.com/r/servicewareit/dibix-worker-host) | [![Dibix.Worker.Host](https://img.shields.io/docker/v/servicewareit/dibix-worker-host?label=docker&sort=semver)](https://hub.docker.com/r/servicewareit/dibix-worker-host/tags) |

## Background
The aim of Dibix is to rapidly create use case oriented REST APIs without writing any boilerplate code, unlike the general approach of designing ASP<span>.</span>NET APIs by writing controllers and actions. It strictly focuses on a hand-written T-SQL stored procedure, which is described with a bit of metadata markup. The APIs and contracts involved are specified in a declarative JSON format. Basically, each URL defined in an API endpoint results in invoking the SQL stored procedure, materializing the relational result into a hierarchical result and then return that to the client.

## Getting started

### Creating a project
Dibix follows a database first approach therefore most of the work is done in a [SQL server database project](https://visualstudio.microsoft.com/vs/features/ssdt). 
This is where you create use case oriented stored procedures which will later turn into working REST APIs.<br/>
We currently offer to split your artifacts into two separate projects:
- Component.Database (DDL)<br/>
Contains [DDL (Data definition language)](https://en.wikipedia.org/wiki/Data_definition_language).<br/>
We consider this the default behavior of a database project, where tables, stored procedures, etc. are defined and its intention is to publish these database artifacts to the target database at some point.
- Component.Database.DML (DML)<br/>
Contains [DML (Data manipulation language)](https://en.wikipedia.org/wiki/Data_manipulation_language).<br/>
This project should contain only stored procedures. These will not be published to the target database and instead their statement body will be extracted and compiled into an assembly.

Since DDL gets published at the target database, this means that basically any simple T-SQL statement will end up inside a stored procedure. So far we don't have an exact idea if this is good or bad. The advantage of DDL over DML is that DDL can be easily devop'd at the customer site using [SSMS](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms), whereas the DML is compiled into an assembly and therefore harder to patch, especially during development.

### Configuring the project
Dibix provides [MSBuild](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild) targets to integrate it seamlessly into the database project build pipeline. The idea is to install the [Dibix.Sdk NuGet package](https://www.nuget.org/packages/Dibix.Sdk) into your project, which will automatically add the necessary imports.<br/>
Unfortunately NuGet is [not supported](https://github.com/NuGet/Home/issues/545) in database projects (yet?). Therefore the import has to happen manually. Please check if there is any existing documentation in the product you are working on or ask [me](mailto:tommy.lohse@helpline.de) for assistance.


## Creating a REST API
In this walkthrough, we try to create the following endpoints, that make up a RESTful API:<br />
| Number | Method | URL | Description |
| - | - | - | - |
| [GetPersons](#getpersons) | **GET** | api/Person|Get a list of persons |
| [GetPerson](#getperson) | **GET** | api/Person/{personId} | Get details of a person |
| [CreatePerson](#createperson) | **POST** | api/Person | Create a person |
| [UpdatePerson](#updateperson) | **PUT** | api/Person/{personId} | Update a person |
| [UpdatePersonName](#updatepersonname) | **PATCH** | api/Person/{personId}/Name | Update the name of a person (partial update) |
| [DeletePersons](#deletepersons) | **DELETE** | api/Person?personIds={personIds} | Delete multiple persons |

### Contracts
- Ensure, that there is a folder named "Contracts" at the root of the project
- Create a new .json file named "Person.json" with the following content:
```json
{
  "AccessRights": [
    { "Read": 1 },
    { "Write": 2 },
    { "Execute": 4 }
  ],
  "Gender": [
    "Unsure",
    "Male",
    "Female"
  ],
  "BankAccount": {
    "Id": "uuid",
    "Name": "string"
  },
  "PersonInfo": {
    "Id": {
      "type": "int32",
      "isPartOfKey": "true"
    },
    "Name": "string"
  },
  "PersonDetail": {
    "Id": {
      "type": "int32",
      "isPartOfKey": "true"
    },
    "Name": "string",
    "Gender": "Gender",
    "AccessRights": "AccessRights",
    "BankAccounts": "BankAccounts*",
    "PetId": "int64?"
  },
  "CreatePersonRequest": {
    "Name": "string",
    "Gender": "Gender",
    "AccessRights": "AccessRights",
    "PetId": "int64?"
  },
  "Pet":{
    "Name": "string",
    "Kind": "byte"
  },
  "UpdatePersonRequest": {
    "Name": "string",
    "Gender": "Gender",
    "AccessRights": "AccessRights",
    "Pets": "Pet*"
  }
}
```
The previous example demonstrates the following things:
- Flagged enums (AccessRights)
- Unflagged enums (Gender)
- Primitive types (uuid, string, int32, int64)
- Contract references (#Gender, #AccessRights #BankAccounts; always prefixed with '#')
- Array properties (#BankAccounts*; always suffixed with '*')
- Primary keys ('isPartOfKey')

### User-defined types
To pass in multiple ids for the 'DeletePerson' endpoint, we need to create a user-defined table type. Create a new .sql file name 'udt_intset.sql' with the following content:
```sql
-- @Name IdSet
CREATE TYPE [dbo].[udt_intset] AS TABLE
(
  [id] INT NOT NULL PRIMARY KEY
)
```

To pass in multiple items of `Pet` to the `UpdatePerson` endpoint, we need another user-defined table type. Create a new .sql file name 'udt_petset.sql' with the following content:
```sql
-- @Name PetSet
CREATE TYPE [dbo].[udt_petset] AS TABLE
(
  [position] TINYINT      NOT NULL PRIMARY KEY
, [type]     TINYINT      NOT NULL
, [name]     NVARCHAR(50) NOT NULL
)
```

### HTTP endpoints
- Ensure, that there is a folder named "Endpoints" at the root of the project
- Create a new .json file named "Person.json" with the following content:
```json
{
  "Person": [
    {
      "method": "GET",
      "target": "GetPersons"
    },
    {
      "method": "GET",
      "target": "GetPerson",
      "childRoute": "{personId}"
    },
    {
      "method": "POST",
      "target": "CreatePerson",
      "childRoute": "{personId}",
      "body": "CreatePersonRequest",
      "params": {
        "accessrights": "BODY.Rights"
      }
    },
    {
      "method": "PUT",
      "target": "CreatePerson",
      "childRoute": "{personId}",
      "body": "UpdatePersonRequest",
      "params": {
        "pets": {
          "source": "BODY.Pets",
          "items": {
            "position": "ITEM.$INDEX",
            "type": "ITEM.Kind"
          }
        }
      }
    },
    {
      "method": "PATCH",
      "target": "UpdatePersonName",
      "childRoute": "{personId}/Name/{name}"
    },
    {
      "method": "DELETE",
      "target": "DeletePersons"
    }
  ]
}
```

### Stored procedures
In the following sections, each endpoint is implemented using a stored procedure. Each procedure is decorated with a few metadata properties inside T-SQL comments in the header.

#### GetPersons
##### Stored Procedures\getpersons.sql
```sql
-- @Name GetPersons
-- @Return PersonInfo
CREATE PROCEDURE [dbo].[getpersons]
AS
    SELECT [id]   = [p].[personid]
         , [name] = [p].[name]
    FROM (VALUES (1, N'Luke')
               , (2, N'Maria')) AS [p]([personid], [name])
```
##### Remarks
The previous example describes two metadata properties:
- @Name controls the name of the target
- @Return describes an output.<br />For each SELECT a @Return hint has to be defined. The @Return property has several sub properties. In the previous statement we rely on the default which is equivalent to 'ClrTypes:PersonInfo Mode:Multiple'. This means, that multiple rows are returned and each should be mapped to the 'PersonInfo' contract.

##### HTTP request
```http
GET /api/Person
```

##### HTTP response body
```json
[
  {
    "id": 1,
    "name": "Luke"
  },
  {
    "id": 2,
    "name": "Maria"
  }
]
```

#### GetPerson
##### Stored Procedures\getperson.sql
```sql
-- @Name GetPerson
-- @Return ClrTypes:PersonDetail;BankAccount SplitOn:id Mode:Single
CREATE PROCEDURE [dbo].[getperson] @personid INT
AS
    SELECT [id]           = [p].[personid]
         , [name]         = [p].[name]
         , [gender]       = [p].[gender]
         , [accessrights] = [p].[accessrights]
         , [petid]        = [p].[petid]
         , [id]           = [b].[bankaccountid]
         , [name]         = [b].[name]
    FROM (VALUES (1, N'Luke',  1 /* Male */,   7 /* All */,  10)
               , (2, N'Maria', 2 /* Female */, 1 /* Read */, NULL)) AS [p]([personid], [name], [gender], [accessrights], [petid])
    LEFT JOIN (VALUES (100, N'Personal', 1)
                    , (101, N'Savings', 1)) AS [b]([bankaccountid], [name], [personid]) ON [p].[personid] = [b].[personid]
    WHERE [p].[personid] = @personid
```

##### Remarks
The previous sample is a bit trickier. Here we expect a single result of the 'PersonDetail' contract. The related entity 'BankAccount' is loaded within the same query. This requires that two entity contracts are specified for the 'ClrTypes' property combined with the ';' separator. The 'SplitOn' is also required to mark where the next related entity starts. In this case 'id' is the bank account id column. If you have more related entities, the split on columns are combined with a ',' separator.<br />
*Important*: If you are working with multi map, make sure to define a key on each parent entity using the `isPartOfKey` property as defined in the contracts [above](#contracts). Otherwise you might end up with duplicated results.

##### HTTP request
```http
GET /api/Person/1
```

##### HTTP response body
```json
{
  "Id": 1,
  "Name": "Luke",
  "Gender": 1,
  "AccessRights": 7,
  "BankAccounts": [
    {
      "Id": 100,
      "Name": "Personal"
    },
    {
      "Id": 101,
      "Name": "Savings"
    }
  ],
  "PetId": 10
}
```

#### CreatePerson
##### Stored Procedures\createperson.sql
```sql
-- @Name CreatePerson
-- @Return ClrTypes:int Mode:Single
CREATE PROCEDURE [dbo].[createperson] @name NVARCHAR(255), @gender TINYINT, @accessrights TINYINT, @petid BIGINT
AS
    DECLARE @personid INT = 1

    DECLARE @persons TABLE
    (
        [personid]     INT           NOT NULL
      , [name]         NVARCHAR(128) NOT NULL
      , [gender]       TINYINT       NOT NULL
      , [accessrights] TINYINT       NOT NULL
      , [petid]        BIGINT        NULL
      , PRIMARY KEY([personid])
    )
    INSERT INTO @persons ([personid], [name], [gender], [accessrights], [petid])
    VALUES (@personid, @name, @gender, @accessrights, @petid)

    SELECT @personid
```

##### HTTP request
```http
POST /api/Person
```
```json
{
  "Name": "Luke",
  "Gender": 1,
  "Rights": 7,
  "PetId": 10
}
```

##### Remarks
As you can see here the stored procedure parameter `accessrights` doesn't match a property on the body. It will however be mapped from `Rights`, because a custom parameter mapping using the `BODY` source was defined in the endpoint configuration [above](#http-endpoints). This is useful if the names of the client property and the parameter name in the target stored procedure differ.

##### HTTP response body
```json
1
```

#### UpdatePerson
##### Stored Procedures\updateperson.sql
```sql
-- @Name UpdatePerson
CREATE PROCEDURE [dbo].[updateperson] @personid INT, @name NVARCHAR(255), @gender TINYINT, @accessrights TINYINT, @pets [dbo].[udt_petset] READONLY
AS
    UPDATE @persons SET [name] = @name, [gender] = @gender, [accessrights] = @accessrights
    WHERE [personid] = @personid

    -- Do something with @pets, like MERGE
```

##### HTTP request
```http
PUT /api/Person/1
```
```json
{
  "Name": "Luke",
  "Gender": 1,
  "AccessRights": 7,
  "Pets": [
    {
      "Name": "Pet",
      "Kind": 1
    }
  ]
}
```

##### Remarks
The body contains a collection property named `Pets`. Collections will be mapped to a UDT, which needs to exist in the target database. In this case `[dbo].[udt_petset]`. The properties of the collection items will be mapped to matching columns of the UDT.<br />
For this endpoint there are some custom parameter mappings defined in the endpoint configuration [above](#http-endpoints):
- The `position` column of the UDT just serves as a primary key and will be mapped from the index of the item in the collection. This is done using the internal `$INDEX` property on the `ITEM` source.
- The `type` column of the UDT will be mapped from the `Kind` property of each instance of `Pet`.
- The `name` column doesn't require a mapping and will be automatically mapped from the matching `Name` property of each instance of `Pet`.

#### UpdatePersonName
##### Stored Procedures\updatepersonname.sql
```sql
-- @Name UpdatePersonName
CREATE PROCEDURE [dbo].[updatepersonname] @personid INT, @name NVARCHAR(255)
AS
    UPDATE @persons SET [name] = @name
    WHERE [personid] = @personid
```

##### HTTP request
```http
PATCH /api/Person/1/Name/Luke
```

#### DeletePersons
##### Stored Procedures\deletepersons.sql
```sql
-- @Name DeletePersons
CREATE PROCEDURE [dbo].[deletepersons] @personids [dbo].[udt_intset] READONLY
AS
    DELETE [p]
    FROM @persons AS [p]
    INNER JOIN @personids AS [pi] ON [p].[personid] = [pi].[personid]
```

##### HTTP request
```http
DELETE /api/Person?personIds[]=1&personIds[]=2
```

## Compiling the project
Once you have created all the necessary artifacts, you can build the database project. With the Dibix MSBuild targets automatically integrated into the build pipeline, you end up with a couple of additional files along with the `.dacpac` file in your output directory:
1. An `<Area>.dbx` endpoint package file that contains everything to feed the [Dibix.Http.Host](#dibix-http-host) with the REST endpoints and their SQL targets defined in this project.
2. An `<OutputName>.dll` assembly, that contains only the C# accessors for the SQL artifacts defined in the project. This can be useful in any C# application, such an integration test project or backend application, like the [Dibix Worker Host](#dibix-worker-host), for example.
3. An `<Area>.Client.dll` assembly, that contains the C# http client which can be used, to contact the REST endpoints, defined within the project. See [this section](#consuming-endpoints) for more details.
4. The [OpenAPI](https://www.openapis.org/) definition as `<Area>.yml` and `<Area>.json`.

## Hosting
There are currently two hosting applications for different purposes. You can download both as zip from the [latest release](https://github.com/Serviceware/Dibix/releases/latest). See below for more detail.

### Dibix Http Host
This application hosts REST endpoint packages generated by database projects. For first time use, these are the minimum steps, that must be configured in the `appsettings.json` file within the root folder of the application:
1. The connection string to the database (`Database:ConnectionString`)
2. The URL of the OIDC authority used to verify incoming JWT bearer tokens (`Authentication:Authority`)

To register a package, place it in the `Packages` folder and add it to the `Hosting:Packages` section in the `appsettings.json`.

#### Configuration Reference
| Setting | Description |
| - | - |
| `Database:ConnectionString` | SQL Server connection string |
| `Authentication:Authority` | OIDC authority URL for JWT validation |
| `Authentication:Audience` | Expected JWT audience (default: `dibix`) |
| `CORS:AllowedOrigins` | Array of allowed CORS origins |
| `Hosting:Packages` | Array of package names to load |
| `Hosting:Extension` | Name (without `.dll`) of a host extension assembly placed in the `Extension` subfolder of the application. It is loaded in an isolated `AssemblyLoadContext` and must implement `IHttpHostExtension` to hook into host configuration (custom JWT/bearer setup, claims transformation, diagnostics, etc.). |
| `Hosting:ExternalHostName` | The public host name under which the deployment is reachable, used to build absolute URLs the host can't infer from the incoming request. It is required (and only consumed) in non-`Development` environments to construct the MCP protected-resource metadata URL, and is also exposed to host extensions (`IHttpHostExtensionConfigurationBuilder.ExternalHostName`) for their own URL generation. |
| `Hosting:EnvironmentName` | A free-form, deployment-specific name for this host instance, defined by whoever operates the deployment (e.g. the customer). It is currently surfaced as the MCP server's resource name, advertised to MCP clients as `"{EnvironmentName} MCP Server"` (defaults to `Dibix` when unset). Despite the name, it is unrelated to the ASP.NET Core environment (`Development`/`Staging`/`Production`). |
| `Hosting:UseStdio` | Selects the MCP transport: `false` (default) serves MCP over HTTP/SSE alongside the REST endpoints; `true` runs the host in stdio mode, serving MCP over stdin/stdout instead of mapping the HTTP endpoint. **stdio is an experimental proof of concept** — see the note below. |

#### MCP Integration
The Dibix Http Host includes an [MCP (Model Context Protocol)](https://modelcontextprotocol.io/) server that exposes the hosted endpoints as MCP tools/resources/prompts (see the `modelContextProtocolType` action property) so AI assistants can invoke them. It is enabled in every environment.

The MCP resource URL is derived from the bound server URLs in the `Development` environment (preferring `http` to avoid certificate issues in VS Code, the preferred test client), and from `Hosting:ExternalHostName` in all other environments.

The transport is selected by `Hosting:UseStdio`:
- `false` (default) — MCP is mapped over HTTP/SSE and protected by the same JWT authority as the REST endpoints. This is the intended, fully-supported transport.
- `true` — the host runs in stdio mode and serves MCP over stdin/stdout (the HTTP MCP endpoint is not mapped).

> **Note:** The stdio transport is an experimental proof of concept, added only because Claude Desktop does not support SSE. It is **not** fully implemented and may never be: the rest of `Dibix.Http.Host` is built around an `HttpContext`, which does not exist in stdio mode, so a large part of the request pipeline does not apply. Authorization in particular is a different story — the code flow that secures the SSE transport cannot work over stdio. Treat HTTP/SSE as the supported transport and stdio as a best-effort experiment.

### Dibix Worker Host
This application hosts worker assemblies that can contain long running background jobs, such as a simple worker or [Service Broker](https://learn.microsoft.com/de-de/sql/database-engine/configure-windows/sql-server-service-broker) message subscribers.

These workers can be developed using the abstractions defined in the [`Dibix.Worker.Abstractions` nuget package](https://www.nuget.org/packages/Dibix.Worker.Abstractions).
For first time use, the only required setting in the `appsettings.json` file is the connection string to the database (`Database:ConnectionString`)

To register a worker assembly, place it in the `Workers` folder and add it to the `Hosting:Workers` section in the `appsettings.json`.

## Consuming endpoints
If the project contains any HTTP endpoints, a client assembly and an [OpenAPI](https://github.com/OAI/OpenAPI-Specification) document are also created during compilation. The client assembly contains a service interface and implementation for each endpoint defined in the project along with their referenced contracts. A host project can consume these client assemblies and register the implementation in the DI container to make the interface available to consumers via IoC. <br />
The implementation is based on the [Dibix.Http.Client](https://www.nuget.org/packages/Dibix.Http.Client) runtime and the generated services may require a few dependencies:
| Type | Required | Implementation(s) |
| - | - | - |
| [`IHttpClientFactory`](https://github.com/Serviceware/Dibix/blob/main/src/Dibix.Http.Client/Client/IHttpClientFactory.cs) | Optional |[`DefaultHttpClientFactory`](https://github.com/Serviceware/Dibix/blob/main/src/Dibix.Http.Client/Client/DefaultHttpClientFactory.cs) |
| [`IHttpAuthorizationProvider`](https://github.com/Serviceware/Dibix/blob/main/src/Dibix.Http.Client/Client/IHttpAuthorizationProvider.cs) | Required (if endpoint requires authorization) | - |

The OpenAPI document will be generated in YAML and JSON format and can be used to generate other artifacts, for example clients in other languages like TypeScript.

## Syntax reference
### Stored procedure
In this section, the markup properties to declare input and output of the stored procedure is explained in more detail. The documentation is still in progress. You can also have a look at [these tests](tests/Dibix.Sdk.Tests.Database/Tests/Syntax) for more examples.

#### Name
PascalCase naming is recommended for referencing actions in API definitions.
If all lower case naming is used in T-SQL, this enables you to generate a PascalCase name for the action.
```sql
-- @Name GetPersons
```
```json
{
  "Person": [
    {
      "target": "GetPersons"
    }
  ]
}
```

#### Namespace
Allows to group actions into a separate (relative) namespace.
```sql
-- @Name GetPersons
-- @Namespace Group
```
```json
{
  "Person": [
    {
      "target": "Group.GetPersons"
    }
  ]
}
```

#### Return
Describes the output of a stored procedure. For each SELECT statement, a `@Return` hint should be defined.

**Properties:**
| Property | Description |
| - | - |
| `ClrTypes` | The CLR type(s) to map the columns of the result to. Multiple types separated by `;` for multi-mapping (combined with `SplitOn`). |
| `SplitOn` | Column name(s) at which the flat row is split into the next related entity when multi-mapping (comma-separated, one entry per additional type beyond the first). |
| `Mode` | The cardinality of the result (see below). Optional — defaults to `Multiple`. |
| `Name` | Name of the property the result is exposed as on the generated grid result type. Required (and only meaningful) when the procedure has more than one `@Return`. |
| `Converter` | Name of a custom converter to post-process/transform the mapped result. |
| `ResultType` | Projection target type the mapped result is converted into (currently only supported on grid results with `Mode:Multiple`). |

**`Mode` values:**
| Value | Meaning |
| - | - |
| `Multiple` | A list of rows (`IEnumerable<T>`). **This is the default** when `Mode` is omitted. |
| `Single` | Exactly one row is expected; throws if zero or more than one is returned. |
| `SingleOrDefault` | At most one row; returns `null`/`default` when none is returned, throws if more than one. |

```sql
-- Simple return (defaults to Mode:Multiple)
-- @Return ClrTypes:PersonInfo

-- Single result with multi-map
-- @Return ClrTypes:PersonDetail;BankAccount SplitOn:id Mode:Single

-- Grid result with named outputs (each SELECT becomes a named property)
-- @Return ClrTypes:PersonDetail Mode:Single Name:Person
-- @Return ClrTypes:AddressInfo Name:Addresses
```

#### Async
Generates an asynchronous accessor method instead of a synchronous one. Use it for any procedure where you want non-blocking, `async`/`await`-friendly database access; stream/file-upload parameters additionally require it.

When `@Async` is set, the generated accessor:
- is named with an `Async` suffix (e.g. `GetPersonsAsync`),
- returns `Task` (no result) or `Task<T>` (with a result) instead of `void`/`T`,
- takes an additional `CancellationToken cancellationToken = default` parameter,
- calls the async runtime methods (`QueryManyAsync`, `QuerySingleAsync`, `ExecuteAsync`, …) and awaits them with `.ConfigureAwait(false)`.

```sql
-- @Name GetPersons
-- @Async
-- @Return PersonInfo
CREATE PROCEDURE [dbo].[getpersons]
AS
BEGIN
    SELECT [id]   = [p].[personid]
         , [name] = [p].[name]
    FROM [dbo].[person] AS [p]
END
```
Generates roughly:
```csharp
public static async Task<IEnumerable<PersonInfo>> GetPersonsAsync(this IDatabaseAccessorFactory databaseAccessorFactory, Action<DatabaseAccessorOptions> configure = null, CancellationToken cancellationToken = default)
```

#### FileResult
Turns the procedure into a binary file response that is **streamed** back to the client rather than materialized and serialized as JSON. Instead of mapping the result to a contract, the accessor returns a `Dibix.FileEntity` whose `Data` is a `Stream`, so the body is written straight to the HTTP response without buffering the whole payload in memory — useful for images, downloads and other large/binary content.

The procedure must contain exactly **one** `SELECT` (no `@Return` declarations) producing these columns:

| Column | Required | Type | Purpose |
| - | - | - | - |
| `[type]` | yes | `NVARCHAR`/`NCHAR` | The MIME type (or file extension) of the content. |
| `[data]` | yes | `VARBINARY` | The binary content. **Must be the last column** in the SELECT. |
| `[filename]` | no | `NVARCHAR`/`NCHAR` | Suggested file name (used for the `Content-Disposition` header). |
| `[length]` | no | `BIGINT` | Content length in bytes. |

```sql
-- @Name GetImage
-- @FileResult
-- @Async
CREATE PROCEDURE [dbo].[getimage] @id INT
AS
BEGIN
    SELECT [type]     = N'image/png'
         , [filename] = [i].[name]
         , [data]     = [i].[imagedata]
    FROM [dbo].[image] AS [i]
    WHERE [i].[id] = @id
END
```

##### JSON file result
`@FileResult Json` is for the case where the response is still a regular (JSON) contract, but you want the client to treat it as a downloadable file with a file name (e.g. an "export as JSON" endpoint). Unlike a plain `@FileResult`, the result is **not** a binary `FileEntity` — it is your normal contract, mapped from the `@Return` declarations as usual, plus a file name carried alongside it.

To use it, mark the contract with `$isJsonFileResult` (which makes the generated class implement `IJsonFileMetadata`, exposing a `FileName` property), and provide the file name from SQL:
- with a single (or merged) result, include a `[filename]` `NVARCHAR` column in the SELECT;
- with multiple results, add a dedicated `@Return ClrTypes:string Mode:Single Name:FileName` whose SELECT returns only a `[filename]` column.

```jsonc
// Contracts: mark the contract as a JSON file result
{
  "ExportResult": {
    "$isJsonFileResult": true,
    "FileName": "string",
    "Items": "PersonInfo*"
  }
}
```
```sql
-- @Name ExportPersons
-- @Return ClrTypes:ExportResult Mode:SingleOrDefault
-- @Return ClrTypes:PersonInfo Name:Items
-- @MergeGridResult
-- @FileResult Json
CREATE PROCEDURE [dbo].[exportpersons]
AS
BEGIN
    SELECT [filename] = N'persons.json'

    SELECT [id]   = [p].[personid]
         , [name] = [p].[name]
    FROM [dbo].[person] AS [p]
END
```

#### GeneratedResultTypeName
A procedure with more than one named `@Return` produces a *grid result*: the SDK generates a container type with one property per result (each result's `Name` becomes a property). By default that generated type is named `{ProcedureName}Result` and placed in the project's domain model namespace. `@GeneratedResultTypeName` overrides that name; a namespace path may be included (e.g. `Grid.PersonGrid`).

```sql
-- @Name GetPersonGrid
-- @Return ClrTypes:PersonDetail Mode:Single Name:Person
-- @Return ClrTypes:PersonInfo Name:Related
-- @GeneratedResultTypeName PersonGrid
CREATE PROCEDURE [dbo].[getpersongrid] @personid INT
AS
BEGIN
    SELECT [id]   = [p].[personid]
         , [name] = [p].[name]
    FROM [dbo].[person] AS [p]
    WHERE [p].[personid] = @personid

    SELECT [id]   = [r].[personid]
         , [name] = [r].[name]
    FROM [dbo].[person] AS [r]
    WHERE [r].[personid] <> @personid
END
```
Without the markup the generated type would be `GetPersonGridResult`. It only applies when a grid result type is actually generated — it has no effect together with [`@MergeGridResult`](#mergegridresult) (which returns the first result's type rather than generating a container). The related `@ResultTypeName` markup is the opposite choice: it reuses an existing contract as the result type instead of generating one.

#### MergeGridResult
A [grid result](#generatedresulttypename) normally returns a generated container type with one property per result (e.g. `result.Person`, `result.Addresses`). `@MergeGridResult` removes that wrapper — instead, the **first** result *is* the root object that gets returned, and every subsequent named result is mapped onto a (collection) property of that same root object. Because no container type is generated, `@GeneratedResultTypeName` does not apply here.

In other words it merges the additional result sets into the first one to form a single object graph, which is handy when the related collections can't be loaded in one multi-mapped query (for example because they each need their own `SELECT`).

Rules (enforced at build time):
- The first `@Return` must use `Mode:Single` or `Mode:SingleOrDefault` (it becomes the single root object).
- The first `@Return` must **not** specify a `Name` (it is the root, not a named property).
- Each following `@Return` must specify a `Name` matching a property on the root contract that holds it.

```sql
-- @Name GetPerson
-- @Return ClrTypes:PersonDetail Mode:Single
-- @Return ClrTypes:BankAccount Name:BankAccounts
-- @MergeGridResult
CREATE PROCEDURE [dbo].[getperson] @personid INT
AS
BEGIN
    SELECT [id]   = [p].[personid]
         , [name] = [p].[name]
    FROM [dbo].[person] AS [p]
    WHERE [p].[personid] = @personid

    SELECT [id]   = [b].[bankaccountid]
         , [name] = [b].[name]
    FROM [dbo].[bankaccount] AS [b]
    WHERE [b].[personid] = @personid
END
```
Here the first `SELECT` yields the `PersonDetail` root and the second populates its `BankAccounts` collection, so the accessor returns a single `PersonDetail` (not a grid wrapper).

#### GenerateInputClass
By default each stored procedure parameter becomes an individual argument on the generated accessor method. `@GenerateInputClass` instead generates a dedicated input class that bundles all parameters into a single object, and the accessor takes one `input` parameter of that type (its values are applied via `.SetFromTemplate(input)`).

This is useful when a procedure has many parameters (a single object is far easier to construct and pass around than a long positional argument list) and when the parameter set maps naturally onto a request/DTO object. It also gives each parameter a property where attributes such as `[Obfuscated]` (from `@Obfuscate`) can be applied.

```sql
-- @Name CreatePerson
-- @GenerateInputClass
CREATE PROCEDURE [dbo].[createperson] @name NVARCHAR(255), @gender TINYINT, @accessrights TINYINT, @petid BIGINT
AS
BEGIN
    ...
END
```
Generates an input class and a method that takes it:
```csharp
public sealed class CreatePersonInput
{
    public string name { get; set; }
    public byte gender { get; set; }
    public byte accessrights { get; set; }
    public long? petid { get; set; }
}

public static void CreatePerson(this IDatabaseAccessorFactory databaseAccessorFactory, [InputClass] CreatePersonInput input, Action<DatabaseAccessorOptions> configure = null)
```

#### ClrType (Parameter Hint)
Overrides the CLR type a parameter is generated as, when the SQL type alone is ambiguous — for example a `VARBINARY` you want to accept as a `stream`, or a `TINYINT` that really represents an enum/contract type. Only valid on **primitive** SQL types (not user-defined table types).
```sql
CREATE PROCEDURE [dbo].[uploadfile]
    /* @ClrType stream */ @data   VARBINARY(MAX)
  , /* @ClrType MyEnum */ @status TINYINT
```
With `stream`, the parameter is generated as a `System.IO.Stream` instead of a `byte[]`, so the caller can stream the payload straight into the procedure without buffering it. The procedure above generates:
```csharp
public static async Task UploadFileAsync(this IDatabaseAccessorFactory databaseAccessorFactory, System.IO.Stream data, MyEnum status, Action<DatabaseAccessorOptions> configure = null, CancellationToken cancellationToken = default)
{
    using IDatabaseAccessor accessor = databaseAccessorFactory.Create("UploadFile", configure);
    ParametersVisitor @params = accessor.Parameters()
                                        .SetFromTemplate(new
                                        {
                                            data,
                                            status
                                        })
                                        .Build();
    await accessor.ExecuteAsync(UploadFileCommandText, CommandType.StoredProcedure, @params, cancellationToken).ConfigureAwait(false);
}
```
(The accessor is `async` here because stream parameters require [`@Async`](#async).)

#### Obfuscate (Parameter Hint)
Obfuscates the value supplied by the client **before** it is passed into the stored procedure. The accessor runs the inbound value through `TextObfuscator.Obfuscate` (a reversible transformation), so the stored procedure receives and stores the obfuscated form, not the original plaintext. This is the inbound counterpart of the `obfuscated` contract property (see [Contract](#contract)), which deobfuscates the value again on the way out during response serialization.
```sql
CREATE PROCEDURE [dbo].[login]
                     @username NVARCHAR(128)
  , /* @Obfuscate */ @password NVARCHAR(128)
```

#### Unbuffered
Reads the result lazily, one row at a time, instead of buffering it into an in-memory list. Internally this sets the Dapper `buffered` flag to `false`, so the result is enumerated directly off the open data reader.

Its primary purpose is to let the consumer **process the result row by row** as it streams from the database. This has an important consequence for connection lifetime: because rows are read on demand, the underlying `IDatabaseAccessor` (and its database connection) must stay open until the result has been fully enumerated. The consumer therefore takes over controlling the disposal of the `IDatabaseAccessor` — it must not be disposed before the result is fully read, otherwise enumeration fails against a closed connection. (With buffered, default queries the result is fully materialized before the accessor is disposed, so the caller doesn't need to worry about this.)
```sql
-- @Name GetLargeList
-- @Unbuffered
-- @Return ClrTypes:LargeRecord
CREATE PROCEDURE [dbo].[getlargelist]
AS
BEGIN
    SELECT ...
    FROM [dbo].[largetable]
END
```

### Contract
In this section the schema for defining contracts is described. For now you can use [the JSON schema](src/Dibix.Sdk.CodeGeneration/Schema/dibix.contracts.schema.json) as a reference or have a look at [these tests](tests/Dibix.Sdk.Tests.Database/Contracts) as samples.

A property can be declared in short form (`"Name": "string"`) or in object form (`"Name": { "type": "string", ... }`) when you need to set any of the options below. Note that **nullability is expressed on the `type`** with a `?` suffix (e.g. `"int64?"`, `"string?"`), not by any of these options.

**Entity properties:**
| Property | Description |
| - | - |
| `type` | The data type (required). Append `?` to make it nullable (e.g. `int32?`). |
| `isPartOfKey` | Defines that this property is part of the entity's key. See [below](#ispartofkey-and-multi-mapping). |
| `isOptional` | Declares the property optional in the generated OpenAPI schema (excluded from the schema's `required` set), which makes it optional for *inbound* payloads. It does **not** change serialization, and is **independent of nullability** (a non-nullable property can be optional, a nullable one required). See [optional vs IfNotEmpty](#optional-vs-ifnotempty). |
| `default` | A default value (boolean, number or string) emitted into the OpenAPI schema as the property's documented default. |
| `serialize` | Controls whether the property is written during JSON serialization. See [below](#serialization-behavior). |
| `enumFormat` | How an enum property is serialized: `Number` (default, the numeric value e.g. `1`) or `String` (the member name e.g. `"Male"`). |
| `obfuscated` | Marks the property so its value is **deobfuscated** when materialized into the response. It is the reverse of the `@Obfuscate` parameter hint: `@Obfuscate` obfuscates an inbound value on its way into the stored procedure, `obfuscated` reverses that for an outbound value on its way to the client. |
| `isDiscriminator` | See [below](#discriminators). |
| `kind` | For `datetime`/`datetime?` only. `utc` causes the materialized `DateTime` to have its `Kind` set to `Utc` (via `DateTime.SpecifyKind`), so it serializes with a UTC offset. |
| `isRelativeHttpsUrl` | See [below](#relative-https-urls). |

##### Serialization behavior
The `serialize` option maps to the `SerializationBehavior` of the generated property:
- `Always` (default) — the property is always written to the JSON output.
- `IfNotEmpty` — the property is omitted from the output when it carries no value: `null`/default for scalars, or an empty collection for arrays. This reduces payload size and makes the property *optional outbound* — it surfaces as `undefined` on the client (e.g. in TypeScript). It is the natural choice for EAV-style properties that are only relevant for certain discriminator values (e.g. `valueInteger`, `valueBoolean`, … on a typed-value contract), so each response carries only the members that actually apply.
- `Never` — the property is never written to the JSON output. Useful for values that are needed for mapping (e.g. a discriminator or a key) but should not be exposed to the client. (Discriminator properties get this behavior automatically.)

##### Optional vs IfNotEmpty
`serialize: IfNotEmpty` and `isOptional` both leave the property out of the OpenAPI schema's `required` set, but they are otherwise different things:
- `IfNotEmpty` shapes the **outbound** payload — the value is physically omitted from the response when empty (payload reduction, `undefined` on the client).
- `isOptional` only relaxes the **schema contract** so the property may be absent from an *inbound* payload; it does not change serialization (the value is still always written). Its only observable effect is the OpenAPI `required` exclusion.

##### isPartOfKey and multi-mapping
`isPartOfKey` declares which property (or combination of properties) uniquely identifies an entity. It is required for correct **deduplication during multi-mapping**.

When a parent entity is loaded together with a one-to-many child collection (via a `LEFT JOIN`), the flat result set repeats the parent row once per child. The multi-mapper uses the key to recognize that those repeated rows are the *same* parent, collapse them into a single instance, and attach the distinct children to it. Without a key the mapper cannot tell the repeated parent rows apart and you end up with duplicated parent objects in the result. This is why the multi-map samples [above](#getperson) mark `Id` with `isPartOfKey`.

##### Discriminators
`isDiscriminator` marks a property as the discriminator used to build a **recursive (self-referencing) hierarchy** during mapping — typically a `ParentId`-style foreign key that points at another row in the same result set. The mapper indexes the rows by their key and uses the discriminator value to nest each entity under its parent, turning a flat list into a tree. Each entity may have at most one discriminator, and a discriminator property is automatically excluded from serialization (`serialize: Never`).

##### Relative HTTPS URLs
`isRelativeHttpsUrl` applies only to `uri`/`uri?` properties and is a **client-side** convenience: in the generated HTTP client, a property marked with it is read through a converter that resolves the relative path returned by the server into an absolute `https://{host}/...` URI (when the client is configured to make response URIs absolute). The server stores/returns a relative path; the client turns it into a fully-qualified HTTPS URL.

**Entity-level properties:**
| Property | Description |
| - | - |
| `$wcfNs` | A WCF DataContract namespace (an `http(s)` URL). When set, the generated class is annotated with `[DataContract(Namespace = "...")]` and its properties with `[DataMember]`, for WCF/`DataContractSerializer` compatibility. |
| `$isJsonFileResult` | Marks the contract as the result of a [JSON file result](#fileresult) procedure (`@FileResult Json`). The generated class implements `IJsonFileMetadata`, which requires a `FileName` property carrying the suggested download file name. |

### Endpoint
In this section the schema for defining endpoints is described. For the sake of completeness, you can use [the JSON schema](src/Dibix.Sdk.CodeGeneration/Schema/dibix.endpoints.schema.json) as a reference.

An endpoint JSON starts with a root object. Each property inside the root object  maps to an endpoint. An endpoint is similar to a controller in ASP.NET. The property name defines the name of the endpoint. Along with the area name (based on the component name), it controls the URL of the API: `api/{areaName}/{endpointName}`.

```json
{
  "EndpointName": [
    {
      "method": "GET",
      "target": "GetEntity",
      "childRoute": "{id}"
    }
  ]
}
```

Each endpoint object consists of an array, in which the respective actions are defined. To ensure a RESTful API, each action is distinguished by its HTTP verb, which follows [CRUD](https://en.wikipedia.org/wiki/Create,_read,_update_and_delete) operations, and a unique path.
To extend the path to the API, the `childRoute` property can be used, which is appended to the path base, extending the route template as such: `api/{areaName}/{endpointName}/{childRoute}`.

#### Target
The target property should contain the name of the stored procedure that is invoked by this API action.

#### Action Properties Reference
| Property | Description |
| - | - |
| `method` | HTTP method: `GET`, `POST`, `PUT`, `PATCH`, `DELETE` |
| `target` | Name of the target stored procedure (matching its `@Name`), or a reflection target into a foreign assembly in the form `Type.Method,Assembly`. Reflection targets are a legacy mechanism, retained only for existing (grandfathered) targets. |
| `childRoute` | Additional route path (e.g., `{id}`, `{id}/details`) |
| `operationId` | Custom OpenAPI operation ID |
| `description` | Description for OpenAPI documentation |
| `body` | Request body type or body configuration |
| `params` | Parameter source mappings |
| `securitySchemes` | Security scheme(s) for the action |
| `authorization` | Authorization target or `none` |
| `fileResponse` | File response configuration |
| `response` | Response type configuration |
| `modelContextProtocolType` | MCP type: `Tool`, `Resource`, or `Prompt` |

#### Body Configuration
In the common case `body` is just the contract the request payload is deserialized into:
```json
{
  "body": "CreatePersonRequest"
}
```
When you need more control, `body` can instead be an object. Its options are:

| Option | Description |
| - | - |
| `contract` | The contract type the request body is deserialized into (the object form of the simple `"body": "CreatePersonRequest"`). Optional: if you omit it and only set `mediaType`, the body is treated as a raw stream instead (see the raw upload example below). |
| `mediaType` | The expected request `Content-Type` (e.g. `application/json`, `image/*`). Defaults to `application/json`. |
| `maxContentLength` | Maximum accepted request body size in bytes. Larger requests are rejected before the action runs. |
| `binder` | Fully-qualified name of a custom body binder implementing `IFormattedInputBinder<TSource, TTarget>`. Use it when the raw payload doesn't map one-to-one onto the target parameters and you need custom binding logic. The bound target parameter is the one annotated `[InputClass]` (see [`@GenerateInputClass`](#generateinputclass)). |
| `treatAsFile` | `true` keeps the `contract` — the server still deserializes the body as that contract — but represents the request body as a binary file (`type: string, format: binary`) in the OpenAPI document, and makes the generated client's body parameter a `Stream`. Use it when the caller uploads the payload as a file even though it is a (JSON) contract. It does not change how the body is read on the server. |

A structured contract body with a size limit:
```json
{
  "body": {
    "contract": "CreatePersonRequest",
    "maxContentLength": 104857600
  }
}
```
A JSON contract that callers upload as a file (`treatAsFile`):
```json
{
  "body": {
    "contract": "ImportRequest",
    "treatAsFile": true
  }
}
```
A genuine raw binary upload — omit `contract`, set `mediaType`, and bind the raw body stream to a parameter via the [`BODY`](#body) intrinsics:
```json
{
  "body": {
    "mediaType": "image/*"
  },
  "params": {
    "data": "BODY.$RAW"
  }
}
```

#### File Response
Whereas the [`@FileResult`](#fileresult) SQL markup makes a procedure *produce* a file, `fileResponse` is the endpoint-side configuration controlling how that file is *served*. Its options:

| Option | Description |
| - | - |
| `mediaType` | Sets the response `Content-Type` for the (binary `FileEntity`) result. |
| `cache` | Defaults to `true`. When `true`, the response is sent with `Cache-Control: public, max-age=31536000, immutable` (cache for a year); `false` adds no caching headers. |
| `indentJson` | Only applies to [JSON file results](#fileresult) — `true` pretty-prints the JSON, `false` (default) writes it compact. Ignored for binary files. |
| `dispositionType` | The `Content-Disposition` type: `attachment` (default) prompts a download, `inline` lets the browser render it in place. The file name comes from the result's `[filename]`/`FileName`. |

```json
{
  "fileResponse": {
    "mediaType": "image/png",
    "cache": false,
    "dispositionType": "attachment"
  }
}
```

#### Response Configuration
By default the response type is inferred from the stored procedure's `@Return` declaration, and the [HTTP status code](#http-status-code) is `200`/`204`. The `response` property lets you describe responses explicitly, keyed by status code. This is mainly needed for endpoints where the response can't be inferred — most notably [reflection targets](#action-properties-reference), which have no `@Return` markup to derive the type from. Each value can be:
- a type reference (`"PersonDetail"`) — the response body contract for that status,
- `null` — an empty response for that status,
- or an object with `type`, `description` and/or `autoDetect`.

```json
{
  "response": {
    "200": "PersonDetail"
  }
}
```

##### Error response auto-detection
Endpoints signal errors with the T-SQL `THROW` statement, encoding the HTTP status code and a custom error code in the 6-digit error number (see [HTTP status code](#http-status-code)). At build time Dibix **scans the stored procedure body for `THROW` statements**, decodes each error number into its status code + error code + message, and automatically declares the corresponding error responses in the OpenAPI document and generated client — so you don't have to list them by hand. (Only client errors, `4xx`, are auto-detected this way.)

The `autoDetect` sub-property tunes this per status code:

- `autoDetect: { errorCode, errorMessage }` — explicitly declare an error response that the scanner can't infer on its own (for example when the `THROW` lives in a called procedure or uses a non-literal error number). It documents that status code with the given error code and message.
  ```json
  {
    "response": {
      "404": {
        "autoDetect": {
          "errorCode": 1,
          "errorMessage": "Person not found"
        }
      }
    }
  }
  ```
- `autoDetect: false` — suppress the auto-detected response for that status code (e.g. an internal `THROW` you don't want to surface as a documented response). If `autoDetect` is the only property of the response object, the status code is dropped from the responses entirely.
  ```json
  {
    "response": {
      "404": {
        "autoDetect": false
      }
    }
  }
  ```

### Project Configuration (dibix.json)
The `dibix.json` file configures project-level settings. Use [the JSON schema](src/Dibix.Sdk/Schema/dibix.configuration.schema.json) as a reference.

```json
{
  "Endpoints": {
    "BaseUrl": "https://localhost/api",
    "ParameterSources": {
      "CLAIM": {
        "UserId": {
          "type": "string",
          "claimName": "sub"
        },
        "Name": "string"
      },
      "CUSTOM": {
        "TenantId": "uuid"
      }
    },
    "Converters": [
      "MYCONVERTER",
      {
        "name": "CLAIMCONVERTER",
        "requiredClaims": ["Name"]
      }
    ],
    "CustomSecuritySchemes": {
      "ApiKey": {
        "type": "Header",
        "headerName": "X-API-Key"
      },
      "CustomBearer": "Bearer"
    },
    "Templates": {
      "Default": {
        "Action": {
          "securitySchemes": "CustomBearer"
        }
      },
      "Authorization": {
        "AdminOnly": {
          "target": "CheckAdminAccess"
        }
      }
    }
  }
}
```

**Configuration sections:**
| Section | Description |
| - | - |
| `Endpoints.BaseUrl` | Base URL for generated client |
| `Endpoints.ParameterSources` | Custom parameter sources (CLAIM, custom) |
| `Endpoints.Converters` | Declares the value converters the project's endpoints may reference via the `converter` option in their parameter mappings. See [below](#converters). |
| `Endpoints.CustomSecuritySchemes` | Custom security schemes (Header, Bearer) |
| `Endpoints.Templates.Default` | Default action settings (security, authorization) |
| `Endpoints.Templates.Authorization` | Reusable authorization templates |
| `SqlCodeAnalysis.NamingConventionPrefix` | Prefix for SQL naming convention checks |

##### Converters
A *converter* transforms a parameter value while it is being resolved from its source — referenced by name from a parameter mapping, e.g. `"searchText": { "source": "QUERY.q", "converter": "FULLTEXTSEARCH" }`. `Endpoints.Converters` registers the converter names available to the project.

An entry can be a bare name, or an object with `requiredClaims`:
```json
"Converters": [
  "MYCONVERTER",
  {
    "name": "CLAIMCONVERTER",
    "requiredClaims": ["Name"]
  }
]
```
`requiredClaims` lists the JWT claims the converter depends on at runtime. Any action that uses a parameter bound through that converter automatically inherits those claims as **required claims** of the endpoint — they are propagated into the endpoint metadata so the host can ensure the token carries them (and make them available to the converter). The same `requiredClaims` mechanism exists on custom `ParameterSources`.

### HTTP status code
By default Dibix endpoints return [200 OK](https://httpstatuses.com/200) for operations that have a result and [204 NoContent](https://httpstatuses.com/204) for those that do not return a result.<br />
However sometimes you need to return a different HTTP status code, for example to indicate that the request is invalid. 
Ideally you could return a different response body along with a specific HTTP status code, however this is not an easy task and gets very complex with the current way how response types are declared and also validated with the according T-SQL output statements.<br />
Therefore currently it's only possible to return a specific HTTP status code (supported are currently some client and some server errors) along with an additional error code and a message, both which are returned as custom HTTP response headers. 

To return an error response, use the T-SQL [THROW](https://docs.microsoft.com/en-us/sql/t-sql/language-elements/throw-transact-sql) statement
#### 4xx client error
Supported:
Code|Name|Sample use cases
| - | - | - |
| [400](https://httpstatuses.com/400) | BadRequest | Client syntax error (malformed request) |
| [401](https://httpstatuses.com/401) | Unauthorized | Either the request is missing credentials or the credentials were not accepted |
| [403](https://httpstatuses.com/403) | Forbidden | The authorized user is not allowed to access the current resource |
| [404](https://httpstatuses.com/404) | NotFound | Resource with given ID not found, Feature not available/configured |
| [409](https://httpstatuses.com/409) | Conflict | The resource is currently locked by another request (might resolve by retry) |
| [422](https://httpstatuses.com/422) | UnprocessableEntity | The client content was not accepted because of a semantic error (i.E. schema validation) |

##### SQL
```sql
THROW 404017, N'Service not available', 1
```
The error code of the THROW statement is used to indicate the HTTP status code (first three digits) and a custom error code (last three digits) for the application/feature, which can be used for custom handling or resolve a translation for the error message on the client.<br />
##### HTTP response
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not found",
  "status": 404,
  "detail": "Service not available",
  "code": 17
}
```

#### 5xx server error (Supported: [504](https://httpstatuses.com/504))
For server errors, custom error codes are not supported, since they quite possibly cannot be fixed/handled by the client and could also disclose sensitive information.<br />

Supported:
Code|Name|Sample use cases
| - | - | - |
| [504](https://httpstatuses.com/504) | GatewayTimeout | External service did not respond in time |

##### SQL
```sql
THROW 504000, N'Request with id '' + @id + '' timed out', 1
```
##### HTTP response
``` http
HTTP/1.1 504 Gateway Timeout
```

### Builtin parameter source providers
This section describes known parameter sources that are already registered and can help to dynamically map a stored procedure parameter from. They are used in the [endpoint definition json](#http-endpoints) and are accessible within the parameter configuration.

#### QUERY
This source provides access to the query string arguments.

#### PATH
This source provides access to the path segment arguments. For example use `PATH.userId` to access the `userId` parameter in the URL `User/{userId}`.

#### BODY
This source provides access to the properties on a JSON object supplied in the body. It requires the body property to be set on the action definition to specify the expected contract of the body.

Sample:
```json
{
  "Person": [
    {
      "method": "POST",
      "target": "CreatePerson",
      "body": "CreatePersonRequest",
      "params": {
        "accessrights": "BODY.Rights"
      }
    }
  ]
}
```

In addition to the body's own properties, `BODY` exposes a few intrinsic properties describing the raw request body — useful for [raw uploads](#body-configuration) where the body has no contract:
| Property | Value |
| - | - |
| `$RAW` | The raw request body as a `Stream` (bind to a `stream` parameter). |
| `$MEDIATYPE` | The request `Content-Type`. |
| `$FILENAME` | The uploaded file name (from the `Content-Disposition` header). |
| `$LENGTH` | The request body length in bytes. |

#### HEADER
This source provides access to the request headers. For example `HEADER.Authorization`.

#### REQUEST
This source provides access to the HTTP request. It supports the following properties:
PropertyName|Type|Value
| - | - | - |
| Language | string | The value provided in the `Accept-Language` header |

#### ENV
This source provides access to the server environment. It supports the following properties:
PropertyName|Type|Value
| - | - | - |
| MachineName | string | The value of [`System.Environment.MachineName`](https://docs.microsoft.com/en-us/dotnet/api/system.environment.machinename) |
| CurrentProcessId | int | The value of [`System.Diagnostics.Process.GetCurrentProcess()`](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.getcurrentprocess)[`.Id`](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.id)
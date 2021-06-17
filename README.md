# Dibix
---
Seamlessly create use case oriented REST APIs based on T-SQL stored procedures.

[![Build Status](https://img.shields.io/azure-devops/build/serviceware/dibix/2/master)](https://dev.azure.com/serviceware/Dibix/_build/latest?definitionId=2&branchName=master)

## Packages
---
| Package | NuGet |
| - | - |
| [Dibix](https://azdops.serviceware.net/sw/platform/Common/_packaging?_a=package&feed=3faf84d3-2d7c-4a03-b5d2-4d12a0b38b6c&package=494b3d58-8bc0-4cc8-a992-7b68795991c7&preferRelease=true) | [![Dibix package in Dibix feed in Azure Artifacts](https://azdops.serviceware.net/sw/platform/_apis/public/Packaging/Feeds/3faf84d3-2d7c-4a03-b5d2-4d12a0b38b6c/Packages/494b3d58-8bc0-4cc8-a992-7b68795991c7/Badge)](https://azdops.serviceware.net/sw/platform/Common/_packaging?_a=package&feed=3faf84d3-2d7c-4a03-b5d2-4d12a0b38b6c&package=494b3d58-8bc0-4cc8-a992-7b68795991c7&preferRelease=true) |
| [Dibix.Dapper](https://azdops.serviceware.net/sw/platform/Common/_packaging?_a=package&feed=3faf84d3-2d7c-4a03-b5d2-4d12a0b38b6c&package=b9902ba8-2ab4-4539-9252-55f73031715c&preferRelease=true) | [![Dibix.Dapper package in Dibix feed in Azure Artifacts](https://azdops.serviceware.net/sw/platform/_apis/public/Packaging/Feeds/3faf84d3-2d7c-4a03-b5d2-4d12a0b38b6c/Packages/b9902ba8-2ab4-4539-9252-55f73031715c/Badge)](https://azdops.serviceware.net/sw/platform/Common/_packaging?_a=package&feed=3faf84d3-2d7c-4a03-b5d2-4d12a0b38b6c&package=b9902ba8-2ab4-4539-9252-55f73031715c&preferRelease=true) |
| [Dibix.Http.Client](https://azdops.serviceware.net/sw/platform/Common/_packaging?_a=package&feed=3faf84d3-2d7c-4a03-b5d2-4d12a0b38b6c&package=47fb1206-5545-44a3-8446-68d1dced0b2b&preferRelease=trueBadge) | [![Dibix.Http.Client package in Dibix feed in Azure Artifacts](https://azdops.serviceware.net/sw/platform/_apis/public/Packaging/Feeds/3faf84d3-2d7c-4a03-b5d2-4d12a0b38b6c/Packages/47fb1206-5545-44a3-8446-68d1dced0b2b/Badge)](https://azdops.serviceware.net/sw/platform/Common/_packaging?_a=package&feed=3faf84d3-2d7c-4a03-b5d2-4d12a0b38b6c&package=47fb1206-5545-44a3-8446-68d1dced0b2b&preferRelease=true) |
| [Dibix.Http.Server](https://azdops.serviceware.net/sw/platform/Common/_packaging?_a=package&feed=3faf84d3-2d7c-4a03-b5d2-4d12a0b38b6c&package=1d668041-4281-45b4-be77-516118a4da10&preferRelease=true) | [![Dibix.Http.Server package in Dibix feed in Azure Artifacts](https://azdops.serviceware.net/sw/platform/_apis/public/Packaging/Feeds/3faf84d3-2d7c-4a03-b5d2-4d12a0b38b6c/Packages/1d668041-4281-45b4-be77-516118a4da10/Badge)](https://azdops.serviceware.net/sw/platform/Common/_packaging?_a=package&feed=3faf84d3-2d7c-4a03-b5d2-4d12a0b38b6c&package=1d668041-4281-45b4-be77-516118a4da10&preferRelease=true) |
| [Dibix.Sdk](https://azdops.serviceware.net/sw/platform/Common/_packaging?_a=package&feed=3faf84d3-2d7c-4a03-b5d2-4d12a0b38b6c&package=2e509adc-51d7-424c-a0d5-d3c59f057c6e&preferRelease=true) | [![Dibix.Sdk package in Dibix feed in Azure Artifacts](https://azdops.serviceware.net/sw/platform/_apis/public/Packaging/Feeds/3faf84d3-2d7c-4a03-b5d2-4d12a0b38b6c/Packages/2e509adc-51d7-424c-a0d5-d3c59f057c6e/Badge)](https://azdops.serviceware.net/sw/platform/Common/_packaging?_a=package&feed=3faf84d3-2d7c-4a03-b5d2-4d12a0b38b6c&package=2e509adc-51d7-424c-a0d5-d3c59f057c6e&preferRelease=true) |
| [Dibix.Testing](https://azdops.serviceware.net/sw/platform/Common/_packaging?_a=package&feed=3faf84d3-2d7c-4a03-b5d2-4d12a0b38b6c&package=396326b6-92a7-4cc3-b076-6982bcaf6a9f&preferRelease=true) | [![Dibix.Testing package in Dibix feed in Azure Artifacts](https://azdops.serviceware.net/sw/platform/_apis/public/Packaging/Feeds/3faf84d3-2d7c-4a03-b5d2-4d12a0b38b6c/Packages/396326b6-92a7-4cc3-b076-6982bcaf6a9f/Badge)](https://azdops.serviceware.net/sw/platform/Common/_packaging?_a=package&feed=3faf84d3-2d7c-4a03-b5d2-4d12a0b38b6c&package=396326b6-92a7-4cc3-b076-6982bcaf6a9f&preferRelease=true) |

## Background
---
The aim of Dibix is to rapidly create use case oriented REST APIs without writing any boilerplate code, unlike the general approach of designing ASP<span>.</span>NET APIs by writing controllers and actions. It strictly focuses on a hand-written T-SQL stored procedure, which is described with a bit of metadata markup. The APIs and contracts involved are specified in a declarative JSON format. Basically, each URL defined in an API endpoint results in invoking the SQL stored procedure, materializing the relational result into a hierarchical result and then return that to the client.

## Getting started
---

### Creating a project
Dibix follows a database first approach therefore most of the work is done in a [SQL server database project](https://visualstudio.microsoft.com/vs/features/ssdt/). 
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
Dibix provides [MSBuild](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild) targets to integrate it seamlessly into the database project build pipeline. The idea is to install the [NuGet](https://tfs.pmcs-helpline.com/tfs/Helpline/Common/_packaging?_a=feed&feed=Dibix) package "Dibix.Sdk" into your project, which will automatically add the necessary imports.<br/>
Unfortunately NuGet is [not supported](https://github.com/NuGet/Home/issues/545) in database projects (yet?). Therefore the import has to happen manually. Please check if there is any existing documentation in the product you are working on or ask [me](mailto:tommy.lohse@helpline.de) for assistance.


## Creating a REST API
---
In this walkthrough, we try to create the following endpoints, that make up a RESTful API:<br />
Number|Method|URL|Description
-|-|-|-
[GetPersons](#getpersons)|**GET**|api/Person|Get a list of persons
[GetPerson](#getperson)|**GET**|api/Person/{personId}|Get details of a person
[CreatePerson](#createperson)|**POST**|api/Person|Create a person
[UpdatePerson](#updateperson)|**PUT**|api/Person/{personId}|Update a person
[UpdatePersonName](#updatepersonname)|**PATCH**|api/Person/{personId}/Name|Update the name of a person (partial update)
[DeletePersons](#deletepersons)|**DELETE**|api/Person?personIds={personIds}|Delete multiple persons

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
    "Gender": "#Gender",
    "AccessRights": "#AccessRights",
    "BankAccounts": "#BankAccounts*",
    "PetId": "int64?"
  },
  "CreatePersonRequest": {
    "Name": "string",
    "Gender": "#Gender",
    "AccessRights": "#AccessRights",
    "PetId": "int64?"
  },
  "Pet":{
    "Name": "string",
    "Kind": "byte"
  },
  "UpdatePersonRequest": {
    "Name": "string",
    "Gender": "#Gender",
    "AccessRights": "#AccessRights",
    "Pets": "#Pet*"
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

### Compiling the project
Once you have created all the necessary artifacts, you can build the database project. With the Dibix MSBuild targets automatically integrated into the build pipeline, you end up with a .dll next to your .dacpac file in your output directory. The assembly contains everything required to feed a web server with the REST API definitions.<br />
As of right now Dibix itself does not contain a runtime implementation for the web server. So again, please check if there is any existing documentation in the product you are working on or ask [me](mailto:tommy.lohse@helpline.de) for assistance.

## Syntax reference
---
### HTTP status code
By default Dibix endpoints return [200 OK](https://httpstatuses.com/200) for operations that have a result and [204 NoContent](https://httpstatuses.com/204) for those that do not return a result.<br />
However sometimes you need to return a different HTTP status code, for example to indicate that the request is invalid. 
Ideally you could return a different response body along with a specific HTTP status code, however this is not an easy task and gets very complex with the current way how response types are declared and also validated with the according T-SQL output statements.<br />
Therefore currently it's only possible to return a specific HTTP status code (supported are currently some client and some server errors) along with an additional error code and a message, both which are returned as custom HTTP response headers. 

To return an error response, use the T-SQL [THROW](https://docs.microsoft.com/en-us/sql/t-sql/language-elements/throw-transact-sql) statement
#### 4xx client error
Supported:
Code|Name|Sample use cases
-|-|-
[400](https://httpstatuses.com/400)|BadRequest|Client syntax error (malformed request)
[401](https://httpstatuses.com/401)|Unauthorized|Either the request is missing credentials or the credentials were not accepted
[403](https://httpstatuses.com/403)|Forbidden|The authorized user is not allowed to access the current resource
[404](https://httpstatuses.com/404)|NotFound|Resource with given ID not found, Feature not available/configured
[409](https://httpstatuses.com/409)|Conflict|The resource is currently locked by another request (might resolve by retry)
[422](https://httpstatuses.com/422)|UnprocessableEntity|The client content was not accepted because of a semantic error (i.E. schema validation)

##### SQL
```sql
THROW 404017, N'Service not available', 1
```
The error code of the THROW statement is used to indicate the HTTP status code (first three digits) and a custom error code (last three digits) for the application/feature, which can be used for custom handling or resolve a translation for the error message on the client.<br />
##### HTTP response
``` http
HTTP/1.1 404 Not Found
X-Error-Code: 17
X-Error-Description: Service not available
```

#### 5xx server error (Supported: [504](https://httpstatuses.com/504))
For server errors, custom error codes are not supported, since they quite possibly cannot be fixed/handled by the client and could also disclose sensitive information.<br />

Supported:
Code|Name|Sample use cases
-|-|-
[504](https://httpstatuses.com/504)|GatewayTimeout|External service did not respond in time

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

#### HEADER
This source provides access to the request headers. For example `HEADER.Authorization`.

#### REQUEST
This source provides access to the HTTP request. It supports the following properties:
PropertyName|Type|Value
-|-|-
Language|string|The value provided in the `Accept-Language` header

#### ENV
This source provides access to the server environment. It supports the following properties:
PropertyName|Type|Value
-|-|-
MachineName|string|The value of [`System.Environment.MachineName`](https://docs.microsoft.com/en-us/dotnet/api/system.environment.machinename)
CurrentProcessId|int|The value of [`System.Diagnostics.Process.GetCurrentProcess()`](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.getcurrentprocess)[`.Id`](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.id)

---

If you read until here, you are entering a dead end, meaning a totally incomplete section. Sorry.

### Stored procedure
In this section, the markup properties to declare input and output of the stored procedure is explained in more detail. The documentation is still in progress. You can also have a look at [these tests](/tests/Dibix.Sdk.Tests.Database/Tests/Syntax) for more examples.

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

To be continued...

### Contract
In this section the schema for defining contracts is described. The documentation is still in progress. For now you can use [the JSON schema](/src/Dibix.Sdk/CodeGeneration/Schema/dibix.contracts.schema.json) as a reference or have a look at [these tests](/tests/Dibix.Sdk.Tests.Database/Contracts) as samples.

### Endpoint
In this section the schema for defining endpoints is described. The documentation is still in progress. For now you can use [the JSON schema](/src/Dibix.Sdk/CodeGeneration/Schema/dibix.endpoints.schema.json) as a reference.

## Roadmap
---
- [ ] Continue writing this documentation 🤓
- [ ] Dibix.Server<br />
This is the overall goal, which drives the whole project, since the idea at the end is to have an independent lightweight server application, that consumes only declarative API registrations rather than DLLs.
- [ ] Make it work with NuGet (see [above](#configuring-the-project))
- [x] Complete [OpenAPI](https://github.com/OAI/OpenAPI-Specification) generator<br />
Right now a JSON and YAML file is generated for each project. It's not published to an output folder yet and can be found in the intermediate folder of the project (`obj\Debug`). The document itself is not yet completed and can currently only be consumed for contract generation (components.schemas).
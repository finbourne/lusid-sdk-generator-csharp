using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Request = WireMock.RequestBuilders.Request;

namespace Finbourne.Sdk.Extensions.Tests.Unit;

static class WireMockTestsExtensions
{
    public static KeyValuePair<string, OpenApiResponse> GetFirstSuccessfulResponse(this OpenApiResponses openApiResponses)
    {
        return openApiResponses.First(kvp =>
        {
            var statusCode = int.Parse(kvp.Key);
            return statusCode is >= 200 and < 300;
        });
    }
}

public class WireMockTests
{
    private WireMockServer _server;
    
    [SetUp]
    public void StartMockServer()
    {
        _server = WireMockServer.Start();
    }

    [Test]
    public void VerifyRequestsForAllEndpoints()
    {
        var filePath = "../../../swagger.json";
        using var stream = File.OpenRead(filePath);
        OpenApiDocument openApiDoc = new OpenApiStreamReader().Read(stream, out var diagnostic);

        if (diagnostic.Errors.Count > 0)
        {
            Assert.Fail(string.Join("; ", diagnostic.Errors));
        }

        var types = typeof(ApiClient).Assembly.GetTypes();
        var apis = types.Where(x => x.IsClass && x.Namespace == "TO_BE_REPLACED_PROJECT_NAME.Api").ToList();
        var failures = new List<string>();

        foreach (var endpoint in openApiDoc.Paths)
        {
            foreach (var operation in endpoint.Value.Operations)
            {
                OperationType method = operation.Key;
                var operationId = operation.Value.OperationId;
                var apiName = operation.Value.Tags[0].Name;
                var expectedApiName = Regex.Replace($"{apiName}Api", " ", "");
                var apiType = apis.SingleOrDefault(x => x.Name == expectedApiName);
                if (apiType == null)
                {
                    throw new Exception($"did not find api class for '{expectedApiName}'");
                }

                try
                {
                    // get sdk method to call
                    var constructor = apiType.GetConstructors()[1];
                    var apiInstance = constructor.Invoke(new object[]{_server.Urls[0]});
                    var apiInstanceMethod = apiType.GetMethod(operationId);
                    var parameterInfos = apiInstanceMethod.GetParameters();
                    var returnType = apiInstanceMethod.ReturnType;
                    
                    SetUpServer(operation, returnType);
                    
                    var parametersDict = new Dictionary<string, (ParameterLocation, object)>();
                    var body = operation.Value.RequestBody?.Content?.Values?.FirstOrDefault();
                    object bodyExample = null;
                    if (body != null)
                    {
                        bodyExample = GetJTokenFromOpenApiMediaType(body) ?? GetDefaultExampleForType(body.Schema);
                    }
                    var arguments = GetArguments(operation.Value.Parameters, parametersDict, bodyExample, parameterInfos);

                    // act
                    apiInstanceMethod!.Invoke(apiInstance, arguments.ToArray());
                
                    // assert
                    VerifyRequest(endpoint.Key, method, body, bodyExample, parametersDict);
                }
                catch (Exception e)
                {
                    failures.Add($"{operationId} ({method} {endpoint.Key}) failed: {e}");
                }

                _server.Reset();
            }
        }

        if (failures.Any())
        {
            Assert.Fail($"The were errors with the following endpoints:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
        }
    }

    private void VerifyRequest(
        string path,
        OperationType method, 
        OpenApiMediaType body, 
        object bodyExample, 
        Dictionary<string, (ParameterLocation, object)> parametersDict)
    {
        var request = _server.LogEntries.Single();
        
        // verify correct method used
        Assert.That(request.RequestMessage.Method.ToLower(), Is.EqualTo(method.ToString().ToLower()));
        
        // verify correct body sent
        if (body != null)
        {
            if (body.Schema.Format == "byte")
            {
                Assert.That(request.RequestMessage.DetectedBodyTypeFromContentType, Is.EqualTo("Bytes"));
                Assert.That(request.RequestMessage.BodyAsBytes, Is.EqualTo(bodyExample));
            }
            else
            {
                Assert.That(request.RequestMessage.Body, Is.EqualTo(JsonConvert.SerializeObject(bodyExample)));
            }
        }

        // verify correct parameters sent
        foreach (var parameter in parametersDict)
        {
            switch (parameter.Value.Item1) {
                case ParameterLocation.Query:
                    var sentQueryParameterKey = request.RequestMessage.Query.Keys.SingleOrDefault(x => x == parameter.Key);

                    // not enough type information passed to the csharp openapischema class to generate examples for arrays
                    // (only get that the type is array - not what it is an array of)
                    // therefore any query parameters which are arrays will be empty, and this does not end up as a query parameter on the request
                    // so ignore for now
                    // future work could be to look up the type from the json file
                    if (parameter.Value.Item2 as JArray is { } items && !items.Any())
                    {
                        Assert.That(sentQueryParameterKey, Is.Null);
                        continue;
                    }
                    Assert.That(sentQueryParameterKey, Is.Not.Null, $"unable to find matching sent parameter for '{parameter.Key}'");
                    var sentQueryParameters = request.RequestMessage.Query[sentQueryParameterKey];
                    if (sentQueryParameters.Count > 1)
                    {
                        throw new NotImplementedException("verification check not implemented for case where there is more than one value for a query parameter");
                    }
                    Assert.That(sentQueryParameters[0].ToLower(), Is.EqualTo(parameter.Value.Item2.ToString().ToLower()));
                    break;
                case ParameterLocation.Header:
                    var sentHeader = request.RequestMessage.Headers.SingleOrDefault(x => x.Key == parameter.Key);
                    Assert.That(sentHeader.Value.Single(), Is.EqualTo(parameter.Value.Item2.ToString()));
                    break;
                case ParameterLocation.Path:
                    var sentPathSegments = request.RequestMessage.PathSegments;
                    var pathSegments = path.Split('/').Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    var index = -1;
                    for (var i = 0; i < pathSegments.Length; i++)
                    {
                        if (pathSegments[i].Equals($"{{{parameter.Key}}}"))
                        {
                            index = i;
                        }
                    }
                    Assert.That(index, Is.GreaterThan(-1), $"unable to find matching segment for parameter '{parameter.Key}' in {string.Join(',', sentPathSegments)}");
                    Assert.That(sentPathSegments[index], Is.EqualTo(parameter.Value.Item2));
                    break;
                case ParameterLocation.Cookie:
                    throw new NotImplementedException("cookie parameters not expected");
                default:
                    throw new ArgumentOutOfRangeException(nameof(parameter.Value.Item1), $"unexpected value '{parameter.Value.Item1}'");
            }
        }
    }

    private static IEnumerable<object> GetArguments(
        IList<OpenApiParameter> parameters,
        IDictionary<string, (ParameterLocation, object)> parameterDict,
        object bodyExample,
        ParameterInfo[] parameterInfos)
    {
        foreach (var parameter in parameters)
        {
            var parameterLocation = parameter.In;
            object example;
            if (parameterLocation == ParameterLocation.Header && parameter.Name == "Content-Length")
            {
                if (bodyExample is byte[] bytes)
                {
                    example = bytes.Length;
                }
                else
                {
                    throw new NotImplementedException("so far Content-Length header value only calculated for body of type byte[]");
                }
            }
            else
            {
                example = GetObjectFromOpenApiParameter(parameter) ?? GetDefaultExampleForType(parameter.Schema);
            }

            parameterDict.Add(parameter.Name, (parameterLocation.Value, example));
        }
        
        // get the list of arguments in the order they are called
        var arguments = new object[parameterInfos.Length];
        for (var i = 0; i < parameterInfos.Length; i++)
        {
            var name = parameterInfos[i].Name;
            if (name is "operationIndex" or "opts")
            {
                arguments[i] = null;
                continue;
            }

            var matchingKey = parameterDict.Keys.SingleOrDefault(x =>
                string.Equals(Regex.Replace(x, "-", ""), name, StringComparison.InvariantCultureIgnoreCase));
            if (matchingKey != default)
            {
                var parameter = parameterDict[matchingKey].Item2;
                if (parameter != null && parameter is JToken jToken)
                {
                    var parameterType = parameterInfos[i].ParameterType;
                    try
                    {
                        parameter = jToken.ToObject(parameterType);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                }
                arguments[i] = parameter;
                continue;
            }
            
            // if can't match the parameter with a name it must be the body
            if (bodyExample != null && bodyExample is JToken jTokenBody)
            {
                var parameterType = parameterInfos[i].ParameterType;
                try
                {
                    bodyExample = jTokenBody.ToObject(parameterType);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
            arguments[i] = bodyExample;
        }
        
        return arguments;
    }

    private void SetUpServer(KeyValuePair<OperationType, OpenApiOperation> operation, Type returnType)
    {
        var firstSuccessfulResponse = operation.Value.Responses.GetFirstSuccessfulResponse();
        var response = firstSuccessfulResponse.Value;
        if (response.Content?.Count > 0)
        {
            foreach (var kvp in response.Content)
            {
                var requestBuilder = Request.Create().WithHeader("Accept", kvp.Key);
                var responseProvider = GetResponseProviderForContentReturned(firstSuccessfulResponse.Key, kvp.Value, returnType);
                _server
                    .Given(requestBuilder)
                    .RespondWith(responseProvider);
            }
        }
        else
        {
            _server
                .Given(Request.Create())
                .RespondWith(Response.Create()
                    .WithStatusCode(firstSuccessfulResponse.Key));
        }
    }

    private static IResponseBuilder GetResponseProviderForContentReturned(
        string statusCode,
        OpenApiMediaType openApiMediaType,
        Type returnType)
    {
        var responseProvider = Response.Create()
            .WithStatusCode(statusCode);
        var json = GetJsonFromOpenApiMediaType(openApiMediaType);
        if (json != null)
        {
            responseProvider.WithBody(json);
        }
        else
        {
            var generatedResponse = GetDefaultExampleForType(openApiMediaType.Schema);
            if (openApiMediaType.Schema.Format == "byte")
            {
                responseProvider.WithBody((byte[])generatedResponse);
            }
            else if (generatedResponse is JToken token)
            {
                object body;
                string bodyString = "";
                try
                {
                    bodyString = token.ToString();
                    body = token.ToObject(returnType);
                    
                }
                catch (Exception e)
                {
                    Console.WriteLine(bodyString);
                    Console.WriteLine(e);
                    throw;
                }
                var response = JsonConvert.SerializeObject(body);
                responseProvider.WithBody(response);
            }
            else
            {
                responseProvider.WithBody(generatedResponse.ToString());
            }
        }
        return responseProvider;
    }

    private static string? GetJsonFromOpenApiMediaType(OpenApiMediaType openApiMediaType)
    {
        var jToken = GetJTokenFromOpenApiMediaType(openApiMediaType);
        return jToken == null ? null : JsonConvert.SerializeObject(jToken);
    }
    
    private static JToken? GetJTokenFromOpenApiMediaType(OpenApiMediaType openApiMediaType)
    {
        var example = openApiMediaType.Example ?? openApiMediaType.Examples?.Values?.FirstOrDefault()?.Value ?? openApiMediaType.Schema.Example;
        return example == null ? null : GetJTokenFromOpenApiType(example);
    }
    
    private static object? GetObjectFromOpenApiParameter(OpenApiParameter openApiParameter)
    {
        var example = openApiParameter.Example ?? openApiParameter.Examples?.Values?.FirstOrDefault()?.Value ?? openApiParameter.Schema.Example;
        if (example == null) return null;
        var jToken = GetJTokenFromOpenApiType(example);
        return jToken;
    }

    private static JToken GetJTokenFromOpenApiType(IOpenApiAny propertyValue)
    {
        switch (propertyValue.AnyType)
        {
            case AnyType.Primitive:
                return (propertyValue as IOpenApiPrimitive).PrimitiveType switch
                {
                    PrimitiveType.Integer => JToken.FromObject((propertyValue as OpenApiPrimitive<int>).Value),
                    PrimitiveType.Long => JToken.FromObject((propertyValue as OpenApiPrimitive<long>).Value),
                    PrimitiveType.Float => JToken.FromObject((propertyValue as OpenApiPrimitive<float>).Value),
                    PrimitiveType.Double => JToken.FromObject((propertyValue as OpenApiPrimitive<double>).Value),
                    PrimitiveType.String => JToken.FromObject((propertyValue as OpenApiPrimitive<string>).Value),
                    PrimitiveType.Byte => JToken.FromObject((propertyValue as OpenApiPrimitive<byte>).Value),
                    PrimitiveType.Binary => JToken.FromObject((propertyValue as OpenApiPrimitive<byte[]>).Value),
                    PrimitiveType.Boolean => JToken.FromObject((propertyValue as OpenApiPrimitive<bool>).Value),
                    PrimitiveType.DateTime => JToken.FromObject((propertyValue as OpenApiPrimitive<DateTimeOffset>).Value),
                    PrimitiveType.Date or PrimitiveType.Password =>
                        throw new NotImplementedException(),
                    _ => throw new ArgumentOutOfRangeException()
                };
            case AnyType.Null:
                return null;
            case AnyType.Array:
                var jArray = new JArray();
                foreach (var item in propertyValue as OpenApiArray)
                {
                    jArray.Add(GetJTokenFromOpenApiType(item));
                }

                return jArray;
            case AnyType.Object:
                var jobj = new JObject();
                var openApiObject = propertyValue as OpenApiObject;
                foreach (var property in openApiObject)
                {
                    var jToken = GetJTokenFromOpenApiType(property.Value);
                    jobj.Add(property.Key, jToken);
                }

                return jobj;
            default:
                throw new ArgumentOutOfRangeException(nameof(propertyValue.AnyType), $"unexpected type '{propertyValue.AnyType}'");
        }
    }

    private static object GetDefaultExampleForType(OpenApiSchema openApiSchema)
    {
        var random = new Random(0);
        var type = openApiSchema.Format ?? openApiSchema.Type;
        return type switch
        {
            "string" => Guid.NewGuid().ToString(),
            "int32" => random.Next(1, 100),
            "integer" => random.Next(1, 100),
            "double" => random.NextDouble() * 100.0,
            "byte" => Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
            "binary" => Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
            "object" => (object)GetJTokenFromOpenApiSchema(openApiSchema),
            "array" => GetJTokenFromOpenApiSchema(openApiSchema),
            "date-time" => DateTime.UtcNow,
            "uri" => "http://localhost/a/b/c",
            "boolean" => true,
            _ => throw new Exception($"no hardcoded example for type '{type}' and no example provided in openapi spec")
        };
    }

    private static JToken GetJTokenFromOpenApiSchema(OpenApiSchema openApiSchema)
    {
        switch (openApiSchema.Type)
        {
            case "array":
                if (!(openApiSchema.Items?.Properties?.Count > 0))
                {
                    return new JArray();
                }
                var jObject = new JObject();
                foreach (var kvp in openApiSchema.Items.Properties)
                {
                    jObject.Add(kvp.Key, GetJTokenFromOpenApiSchema(kvp.Value));
                }
                return new JArray { jObject };

            case "object":
                var jObj = new JObject();
                foreach (var kvp in openApiSchema.Properties)
                {
                    jObj.Add(kvp.Key, GetJTokenFromOpenApiSchema(kvp.Value));
                }
                return jObj;
            
            default:
                return JToken.FromObject(GetDefaultExampleForType(openApiSchema));
        }
    }

    [TearDown]
    public void ShutdownServer()
    {
        _server.Stop();
    }
}

﻿using Xunit;
using Swashbuckle.AspNetCore.Swagger;
using Newtonsoft.Json.Linq;
using Shouldly;
using System.Collections.Generic;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.Filters.Test.TestFixtures.Fakes;
using Swashbuckle.AspNetCore.Filters.Test.TestFixtures.Fakes.Examples;
using Microsoft.AspNetCore.Mvc;
using System;
using NSubstitute;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace Swashbuckle.AspNetCore.Filters.Test.Examples
{
    public class ServiceProviderExamplesOperationFilterTests : BaseOperationFilterTests
    {
        private readonly IOperationFilter sut;
        private readonly IServiceProvider serviceProvider;

        public ServiceProviderExamplesOperationFilterTests()
        {
            var options = Options.Create(new MvcJsonOptions());
            var serializerSettingsDuplicator = new SerializerSettingsDuplicator(options);

            var jsonFormatter = new JsonFormatter();

            var requestExample = new RequestExample(jsonFormatter, serializerSettingsDuplicator);
            var responseExample = new ResponseExample(jsonFormatter, serializerSettingsDuplicator);

            serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IExamplesProvider<PersonResponse>)).Returns(new PersonResponseAutoExample());

            sut = new ServiceProviderExamplesOperationFilter(serviceProvider, requestExample, responseExample);
        }

        [Fact]
        public void Apply_SetsResponseExamples_FromMethodAttributes()
        {
            // Arrange
            var response = new OpenApiResponse { Content = new Dictionary<string, OpenApiMediaType> { { "application/json", new OpenApiMediaType() } } };
            var operation = new OpenApiOperation { OperationId = "foobar", Responses = new OpenApiResponses() };
            operation.Responses.Add("200", response);
            var filterContext = FilterContextFor(typeof(FakeActions), nameof(FakeActions.AnnotatedWithSwaggerResponseAttribute));
            SetSwaggerResponses(operation, filterContext);

            // Act
            sut.Apply(operation, filterContext);

            // Assert
            var actualExample = JsonConvert.DeserializeObject<PersonResponse>(((OpenApiRawString)response.Content["application/json"].Example).Value);

            var expectedExample = (PersonResponse)new PersonResponseAutoExample().GetExamples();
            actualExample.Id.ShouldBe(expectedExample.Id);
            actualExample.FirstName.ShouldBe(expectedExample.FirstName);
        }

        [Fact]
        public void Apply_SetsResponseExamples_WhenMethodNotAnnotated()
        {
            // Arrange
            var response = new OpenApiResponse { Content = new Dictionary<string, OpenApiMediaType> { { "application/json", new OpenApiMediaType() } } };
            var operation = new OpenApiOperation { OperationId = "foobar", Responses = new OpenApiResponses() };
            operation.Responses.Add("200", response);
            var supportedResponseTypes = new List<ApiResponseType> { new ApiResponseType { StatusCode = 200, Type = typeof(PersonResponse) } };
            var filterContext = FilterContextFor(typeof(FakeActions), nameof(FakeActions.PersonResponseNotAnnotated), supportedResponseTypes: supportedResponseTypes);
            SetSwaggerResponses(operation, filterContext);

            // Act
            sut.Apply(operation, filterContext);

            // Assert
            var actualExample = JsonConvert.DeserializeObject<PersonResponse>(((OpenApiRawString)response.Content["application/json"].Example).Value);

            var expectedExample = (PersonResponse)new PersonResponseAutoExample().GetExamples();
            actualExample.Id.ShouldBe(expectedExample.Id);
            actualExample.FirstName.ShouldBe(expectedExample.FirstName);
        }

        [Fact]
        public void Apply_SetsResponseExamples_FromControllerAttributes()
        {
            // Arrange
            var response = new OpenApiResponse { Content = new Dictionary<string, OpenApiMediaType> { { "application/json", new OpenApiMediaType() } } };
            var operation = new OpenApiOperation { OperationId = "foobar", Responses = new OpenApiResponses() };
            operation.Responses.Add("200", response);
            var filterContext = FilterContextFor(typeof(FakeControllers.SwaggerResponseController), nameof(FakeControllers.SwaggerResponseController.None));
            SetSwaggerResponses(operation, filterContext);

            // Act
            sut.Apply(operation, filterContext);

            // Assert
            var actualExample = JsonConvert.DeserializeObject<PersonResponse>(((OpenApiRawString)response.Content["application/json"].Example).Value);

            var expectedExample = (PersonResponse)new PersonResponseAutoExample().GetExamples();
            actualExample.Id.ShouldBe(expectedExample.Id);
            actualExample.FirstName.ShouldBe(expectedExample.FirstName);
        }

        [Fact]
        public void Apply_DoesNotSetResponseExamples_FromMethodAttributes_WhenSwaggerResponseExampleAttributePresent()
        {
            // Arrange
            var response = new OpenApiResponse { Content = new Dictionary<string, OpenApiMediaType> { { "application/json", new OpenApiMediaType() } } };
            var operation = new OpenApiOperation { OperationId = "foobar", Responses = new OpenApiResponses() };
            operation.Responses.Add("200", response);
            var filterContext = FilterContextFor(typeof(FakeActions), nameof(FakeActions.AnnotatedWithSwaggerResponseExampleAttribute));
            SetSwaggerResponses(operation, filterContext);

            // Act
            sut.Apply(operation, filterContext);

            // Assert
            var example = response.Content["application/json"].Example;
            example.ShouldBeNull();
        }

        [Fact]
        public void Apply_SetsResponseExamples_FromMethodAttributes_WithGenericType()
        {
            // Arrange
            var response = new OpenApiResponse { Content = new Dictionary<string, OpenApiMediaType> { { "application/json", new OpenApiMediaType() } } };
            var operation = new OpenApiOperation { OperationId = "foobar", Responses = new OpenApiResponses() };
            operation.Responses.Add("200", response);
            var filterContext = FilterContextFor(typeof(FakeActions), nameof(FakeActions.GenericAnnotatedWithSwaggerResponseAttribute));
            SetSwaggerResponses(operation, filterContext);
            serviceProvider.GetService(typeof(IExamplesProvider<IEnumerable<string>>)).Returns(new ListStringExample());

            // Act
            sut.Apply(operation, filterContext);

            // Assert
            var actualExample = JsonConvert.DeserializeObject<List<string>>(((OpenApiRawString)response.Content["application/json"].Example).Value);
            actualExample[0].ShouldBe("Hello");
            actualExample[1].ShouldBe("there");
        }

        [Fact]
        public void Apply_SetsRequestExamples_FromMethodAttributes()
        {
            // Arrange
            serviceProvider.GetService(typeof(IExamplesProvider<PersonRequest>)).Returns(new PersonRequestAutoExample());
            var requestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    { "application/json", new OpenApiMediaType() }
                }
            };
            var operation = new OpenApiOperation { OperationId = "foobar", RequestBody = requestBody };
            var parameterDescriptions = new List<ApiParameterDescription>() { new ApiParameterDescription { Type = typeof(PersonRequest) } };
            var filterContext = FilterContextFor(typeof(FakeActions), nameof(FakeActions.PersonRequestUnannotated), parameterDescriptions);

            // Act
            sut.Apply(operation, filterContext);

            // Assert
            var actualExample = JsonConvert.DeserializeObject<PersonRequest>(((OpenApiRawString)requestBody.Content["application/json"].Example).Value);
            var expectedExample = (PersonRequest)new PersonRequestAutoExample().GetExamples();
            AssertPersonRequestExampleMatches(actualExample, expectedExample);
        }

        [Fact]
        public void Apply_DoesNotSetRequestExamples_FromMethodAttributes_WhenSwaggerRequestExampleAttributePresent()
        {
            // Arrange
            serviceProvider.GetService(typeof(IExamplesProvider<PersonRequest>)).Returns(new PersonRequestAutoExample());
            var requestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    { "application/json", new OpenApiMediaType() }
                }
            };
            var operation = new OpenApiOperation { OperationId = "foobar", RequestBody = requestBody };
            var parameterDescriptions = new List<ApiParameterDescription>() { new ApiParameterDescription { Type = typeof(PersonRequest) } };
            var filterContext = FilterContextFor(typeof(FakeActions), nameof(FakeActions.AnnotatedWithSwaggerRequestExampleAttribute), parameterDescriptions);

            // Act
            sut.Apply(operation, filterContext);

            // Assert
            requestBody.Content["application/json"].Example.ShouldBeNull();
        }

        [Fact]
        public void Apply_SetsRequestExamplesForInterface_FromMethodAttributes()
        {
            // Arrange
            serviceProvider.GetService(typeof(IExamplesProvider<IPersonRequest>)).Returns(new IPersonRequestAutoExample());
            var requestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    { "application/json", new OpenApiMediaType() }
                }
            };
            var operation = new OpenApiOperation { OperationId = "foobar", RequestBody = requestBody };
            var parameterDescriptions = new List<ApiParameterDescription>() { new ApiParameterDescription { Type = typeof(IPersonRequest) } };
            var filterContext = FilterContextFor(typeof(FakeActions), nameof(FakeActions.IPersonRequestUnannotated), parameterDescriptions);

            // Act
            sut.Apply(operation, filterContext);

            // Assert
            var actualExample = JsonConvert.DeserializeObject<PersonRequest>(((OpenApiRawString)requestBody.Content["application/json"].Example).Value);
            var expectedExample = (PersonRequest)new PersonRequestAutoExample().GetExamples();
            AssertPersonRequestExampleMatches(actualExample, expectedExample);
        }

        private static void AssertPersonRequestExampleMatches(PersonRequest actualExample, PersonRequest expectedExample)
        {
            actualExample.Title.ShouldBe(expectedExample.Title);
            actualExample.FirstName.ShouldBe(expectedExample.FirstName);
            actualExample.Age.ShouldBe(expectedExample.Age);
        }

        [Fact]
        public void Apply_WhenRequestIsAntInt_ShouldNotThrowException()
        {
            // Arrange
            var requestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    { "application/json", new OpenApiMediaType() }
                }
            };
            var operation = new OpenApiOperation { OperationId = "foobar", RequestBody = requestBody };
            var parameterDescriptions = new List<ApiParameterDescription>() { new ApiParameterDescription { Type = typeof(int) } };

            var filterContext = FilterContextFor(typeof(FakeActions), nameof(FakeActions.RequestTakesAnInt), parameterDescriptions);

            // Act
            sut.Apply(operation, filterContext);
        }

        [Fact]
        public void Apply_WhenRequestIsANullableEnum_ShouldNotThrowException()
        {
            // Arrange
            serviceProvider.GetService(typeof(IExamplesProvider<Title?>)).Returns(new TitleExample());
            var requestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    { "application/json", new OpenApiMediaType() { Schema = new OpenApiSchema { Reference = new OpenApiReference { Id = "definitions/Title" } } } }
                }
            };
            var operation = new OpenApiOperation { OperationId = "foobar", RequestBody = requestBody };

            var parameterDescriptions = new List<ApiParameterDescription>() { new ApiParameterDescription { Type = typeof(Title?) } };
            var filterContext = FilterContextFor(typeof(FakeActions), nameof(FakeActions.RequestTakesANullableEnum), parameterDescriptions);

            // Act
            sut.Apply(operation, filterContext);

            // Assert
            var actualParameterExample = Enum.Parse(typeof(Title), ((OpenApiRawString)requestBody.Content["application/json"].Example).Value);
            var expectedExample = new TitleExample().GetExamples().Value;
            actualParameterExample.ShouldBe(expectedExample);
        }

        [Fact]
        public void Apply_WhenPassingDictionary_ShouldSetExampleOnRequestSchema()
        {
            // Arrange
            serviceProvider.GetService(typeof(IExamplesProvider<Dictionary<string, object>>)).Returns(new DictionaryAutoRequestExample());
            var requestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    { "application/json", new OpenApiMediaType() { Schema = new OpenApiSchema { Reference = new OpenApiReference { Id = "definitions/object" } } } }
                }
            };
            var operation = new OpenApiOperation { OperationId = "foobar", RequestBody = requestBody };
            var parameterDescriptions = new List<ApiParameterDescription>() { new ApiParameterDescription { Type = typeof(Dictionary<string, object>) } };
            var filterContext = FilterContextFor(typeof(FakeActions), nameof(FakeActions.DictionaryRequestAttribute), parameterDescriptions);

            // Act
            sut.Apply(operation, filterContext);

            // Assert
            var actualExample = JsonConvert.DeserializeObject<Dictionary<string, object>>(((OpenApiRawString)requestBody.Content["application/json"].Example).Value);
            actualExample["PropertyInt"].ShouldBe(1);
            actualExample["PropertyString"].ShouldBe("Some string");
        }

        [Fact]
        public void Apply_SetsResponseExamples_CorrectlyFormatsJsonExample()
        {
            // Arrange
            var response = new OpenApiResponse { Content = new Dictionary<string, OpenApiMediaType> { { "application/json", new OpenApiMediaType() } } };
            var operation = new OpenApiOperation { OperationId = "foobar", Responses = new OpenApiResponses() };
            operation.Responses.Add("200", response);
            var filterContext = FilterContextFor(typeof(FakeActions), nameof(FakeActions.AnnotatedWithSwaggerResponseAttribute));
            SetSwaggerResponses(operation, filterContext);

            // Act
            sut.Apply(operation, filterContext);

            // Assert
            var example = response.Content["application/json"].Example;

            var formatedExample = RenderOpenApiObject(example);
            formatedExample.EndsWith('"').ShouldBeFalse();
            formatedExample.StartsWith('"').ShouldBeFalse();
        }

        [Fact]
        public void Apply_SetsResponseExamples_CorrectlyFormatsXmlExample()
        {
            // Arrange
            var response = new OpenApiResponse { Content = new Dictionary<string, OpenApiMediaType> { { "application/xml", new OpenApiMediaType() } } };
            var operation = new OpenApiOperation { OperationId = "foobar", Responses = new OpenApiResponses() };
            operation.Responses.Add("200", response);
            var filterContext = FilterContextFor(typeof(FakeActions), nameof(FakeActions.AnnotatedWithSwaggerResponseAttribute));
            SetSwaggerResponses(operation, filterContext);

            // Act
            sut.Apply(operation, filterContext);

            // Assert
            var example = response.Content["application/xml"].Example;

            var formatedExample = RenderOpenApiObject(example);
            formatedExample.EndsWith('"').ShouldBeTrue();
            formatedExample.StartsWith('"').ShouldBeTrue();
        }
    }
}

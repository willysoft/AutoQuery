using AutoQuery.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json.Serialization;

namespace AutoQuery.AspNetCore.Tests
{
    public class EnableFieldProjectionAttributeTests
    {
        private readonly Mock<IOptions<JsonOptions>> _mockJsonOptions;

        public EnableFieldProjectionAttributeTests()
        {
            _mockJsonOptions = new Mock<IOptions<JsonOptions>>();
            _mockJsonOptions.Setup(o => o.Value).Returns(new JsonOptions());
        }

        [Theory]
        [InlineData("Name,Age", "John", 30, "123 Street", true, true, false)]
        [InlineData("", "John", 30, "123 Street", true, true, true)]
        [InlineData("Name,InvalidField", "John", 30, "123 Street", true, false, false)]
        [InlineData("Name,address", "John", 30, "123 Street", true, false, true)]
        [InlineData(null, "John", 30, "123 Street", true, true, true)]
        public void OnActionExecuted_FiltersFields(string fields, string name, int age, string address, bool hasName, bool hasAge, bool hasAddress)
        {
            // Arrange
            var filter = new EnableFieldProjectionAttribute();
            var queryOptions = new Mock<IQueryOptions>();
            queryOptions.Setup(q => q.Fields).Returns(fields);
            var context = CreateActionExecutedContext(queryOptions.Object, new TestObject { Name = name, Age = age, Address = address });

            // Act
            filter.OnActionExecuting(context.Item1);
            filter.OnActionExecuted(context.Item2);

            // Assert
            var result = context.Item2.Result as ObjectResult;
            Assert.NotNull(result);

            if (string.IsNullOrEmpty(fields))
            {
                var filteredResult = result.Value as TestObject;
                Assert.NotNull(filteredResult);
                Assert.Equal(name, filteredResult.Name);
                Assert.Equal(age, filteredResult.Age);
                Assert.Equal(address, filteredResult.Address);
            }
            else
            {
                var filteredResult = result.Value as Dictionary<string, object>;
                Assert.NotNull(filteredResult);
                Assert.Equal(hasName, filteredResult.ContainsKey("name"));
                Assert.Equal(hasAge, filteredResult.ContainsKey("age"));
                Assert.Equal(hasAddress, filteredResult.ContainsKey("ADDress"));
            }
        }

        [Fact]
        public void OnActionExecuted_FiltersFieldsInEnumerable()
        {
            // Arrange
            var filter = new EnableFieldProjectionAttribute();
            var queryOptions = new Mock<IQueryOptions>();
            queryOptions.Setup(q => q.Fields).Returns("Name,address");
            var context = CreateActionExecutedContext(queryOptions.Object, new List<TestObject>
            {
                new TestObject { Name = "John", Age = 30, Address = "123 Street" },
                new TestObject { Name = "Jane", Age = 25, Address = "456 Avenue" }
            });

            // Act
            filter.OnActionExecuting(context.Item1);
            filter.OnActionExecuted(context.Item2);

            // Assert
            var result = context.Item2.Result as ObjectResult;
            Assert.NotNull(result);
            var filteredResult = result.Value as List<Dictionary<string, object>>;
            Assert.NotNull(filteredResult);
            Assert.Equal(2, filteredResult.Count);
            Assert.True(filteredResult[0].ContainsKey("name"));
            Assert.False(filteredResult[0].ContainsKey("age"));
            Assert.True(filteredResult[0].ContainsKey("ADDress"));
            Assert.True(filteredResult[1].ContainsKey("name"));
            Assert.False(filteredResult[1].ContainsKey("age"));
            Assert.True(filteredResult[1].ContainsKey("ADDress"));
        }

        [Fact]
        public void OnActionExecuted_FiltersFieldsInPagedResponse()
        {
            // Arrange
            var filter = new EnableFieldProjectionAttribute();
            var queryOptions = new Mock<IQueryOptions>();
            queryOptions.Setup(q => q.Fields).Returns("Name,Age");
            var pagedResponse = new PagedResponse<TestObject>
            {
                TotalPages = 1,
                Page = 1,
                Count = 2,
                Items = new List<TestObject>
                {
                    new TestObject { Name = "John", Age = 30, Address = "123 Street" },
                    new TestObject { Name = "Jane", Age = 25, Address = "456 Avenue" }
                }
            };
            var context = CreateActionExecutedContext(queryOptions.Object, pagedResponse);

            // Act
            filter.OnActionExecuting(context.Item1);
            filter.OnActionExecuted(context.Item2);

            // Assert
            var result = context.Item2.Result as ObjectResult;
            Assert.NotNull(result);
            var filteredResult = result.Value as Dictionary<string, object?>;
            Assert.NotNull(filteredResult);
            Assert.Equal(pagedResponse.TotalPages, filteredResult["totalPages"]);
            Assert.Equal(pagedResponse.Page, filteredResult["page"]);
            Assert.Equal(pagedResponse.Count, filteredResult["count"]);
            var filteredResultItems = filteredResult["items"] as List<Dictionary<string, object>>;
            Assert.NotNull(filteredResultItems);
            Assert.Equal(2, filteredResultItems.Count);
            Assert.True(filteredResultItems[0].ContainsKey("name"));
            Assert.True(filteredResultItems[0].ContainsKey("age"));
            Assert.False(filteredResultItems[0].ContainsKey("ADDress"));
            Assert.True(filteredResultItems[1].ContainsKey("name"));
            Assert.True(filteredResultItems[1].ContainsKey("age"));
            Assert.False(filteredResultItems[1].ContainsKey("ADDress"));
        }

        [Fact]
        public void OnActionExecuted_NonSuccessStatusCode_DoesNotFilter()
        {
            // Arrange
            var filter = new EnableFieldProjectionAttribute();
            var queryOptions = new Mock<IQueryOptions>();
            queryOptions.Setup(q => q.Fields).Returns("Name,Age");
            var context = CreateActionExecutedContext(queryOptions.Object, new TestObject { Name = "John", Age = 30, Address = "123 Street" }, 400);

            // Act
            filter.OnActionExecuting(context.Item1);
            filter.OnActionExecuted(context.Item2);

            // Assert
            var result = context.Item2.Result as ObjectResult;
            Assert.NotNull(result);
            var filteredResult = result.Value as TestObject;
            Assert.NotNull(filteredResult);
            Assert.Equal("John", filteredResult.Name);
            Assert.Equal(30, filteredResult.Age);
            Assert.Equal("123 Street", filteredResult.Address);
        }

        [Fact]
        public void OnActionExecuted_NullResult_DoesNotFilter()
        {
            // Arrange
            var filter = new EnableFieldProjectionAttribute();
            var queryOptions = new Mock<IQueryOptions>();
            queryOptions.Setup(q => q.Fields).Returns("Name,Age");
            var context = CreateActionExecutedContext(queryOptions.Object, null);

            // Act
            filter.OnActionExecuting(context.Item1);
            filter.OnActionExecuted(context.Item2);

            // Assert
            var result = context.Item2.Result as ObjectResult;
            Assert.Null(result);
        }

        [Fact]
        public void OnActionExecuted_EmptyEnumerable_DoesNotFilter()
        {
            // Arrange
            var filter = new EnableFieldProjectionAttribute();
            var queryOptions = new Mock<IQueryOptions>();
            queryOptions.Setup(q => q.Fields).Returns("Name,Age");
            var context = CreateActionExecutedContext(queryOptions.Object, new List<TestObject>());

            // Act
            filter.OnActionExecuting(context.Item1);
            filter.OnActionExecuted(context.Item2);

            // Assert
            var result = context.Item2.Result as ObjectResult;
            Assert.NotNull(result);
            var filteredResult = result.Value as List<Dictionary<string, object>>;
            Assert.NotNull(filteredResult);
            Assert.Empty(filteredResult);
        }

        private (ActionExecutingContext, ActionExecutedContext) CreateActionExecutedContext(IQueryOptions queryOptions, object? result, int statusCode = 200)
        {
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var serviceProvider = new ServiceCollection().AddSingleton(_mockJsonOptions.Object).BuildServiceProvider();
            httpContext.RequestServices = serviceProvider;
            httpContext.Response.StatusCode = statusCode;

            var actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?> { { "queryOptions", queryOptions } }, new Mock<Controller>().Object);
            var actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new Mock<Controller>().Object)
            {
                Result = result != null ? new ObjectResult(result) : null
            };

            return (actionExecutingContext, actionExecutedContext);
        }

        private class TestObject
        {
            public string Name { get; set; } = null!;
            public int Age { get; set; }
            [JsonPropertyName("ADDress")]
            public string Address { get; set; } = null!;
        }

        public class PagedResponse<T>
        {
            /// <summary>
            /// 總頁數
            /// </summary>
            public int? TotalPages { get; set; }

            /// <summary>
            /// 當前頁數
            /// </summary>
            public int? Page { get; set; }

            /// <summary>
            /// 總筆數
            /// </summary>
            public int Count { get; set; }

            /// <summary>
            /// 目前頁面資料清單
            /// </summary>
            public IReadOnlyList<T> Items { get; set; } = new List<T>();
        }
    }
}

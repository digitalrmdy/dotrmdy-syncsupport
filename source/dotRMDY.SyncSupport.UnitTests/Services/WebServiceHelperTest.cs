using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using dotRMDY.SyncSupport.Services.Implementations;
using dotRMDY.SyncSupport.UnitTests.TestHelpers.Models;
using dotRMDY.TestingTools;
using FakeItEasy;
using FluentAssertions;
using Refit;
using Xunit;

namespace dotRMDY.SyncSupport.UnitTests.Services;

public class WebServiceHelperTest : SutSupportingTest<WebServiceHelper>
{
	[Fact]
	public async Task ExecuteCall_WithCancellation_ReturnsResult()
	{
		// Arrange
		var cancellationTokenSource = new CancellationTokenSource();
		var cancellationToken = cancellationTokenSource.Token;
		var call = A.Fake<Func<CancellationToken, Task<IApiResponse>>>();
		var apiResponse = A.Fake<IApiResponse>();
		A.CallTo(() => call(cancellationToken)).Returns(Task.FromResult(apiResponse));

		// Act
		var result = await Sut.ExecuteCall(call, cancellationToken);

		// Assert
		result.Should().NotBeNull();
		A.CallTo(() => call(cancellationToken)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task ExecuteCall_WithResultObj_ReturnsResult()
	{
		// Arrange
		var cancellationTokenSource = new CancellationTokenSource();
		var cancellationToken = cancellationTokenSource.Token;
		var call = A.Fake<Func<CancellationToken, Task<IApiResponse<SuccessResult>>>>();
		var apiResponse = A.Fake<IApiResponse<SuccessResult>>();
		A.CallTo(() => call(cancellationToken)).Returns(Task.FromResult(apiResponse));
		A.CallTo(() => apiResponse.Error).Returns(null);
		A.CallTo(() => apiResponse.Content).Returns(new SuccessResult
		{
			A = "A",
			B = true,
			C = 1
		});

		// Act
		var result = await Sut.ExecuteCall<SuccessResult>(call, cancellationToken);

		// Assert
		result.Should().NotBeNull();
		A.CallTo(() => call(cancellationToken)).MustHaveHappenedOnceExactly();
		result.Data.Should().NotBeNull();
		result.Data!.A.Should().Be("A");
		result.Data!.B.Should().BeTrue();
		result.Data!.C.Should().Be(1);
	}

	[Fact]
	public async Task ExecuteCall_WithError_ReturnsErrorResult_WrongPayload()
	{
		// Arrange
		var cancellationTokenSource = new CancellationTokenSource();
		var cancellationToken = cancellationTokenSource.Token;
		var call = A.Fake<Func<CancellationToken, Task<IApiResponse<SuccessResult>>>>();
		var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
		response.Content = new StringContent("{\"code\":\"code\",\"message\":\"message\",\"technicalMessage\":\"technicalMessage\"}");
		var refitSettings = new RefitSettings();
		var apiException = await ApiException.Create(new HttpRequestMessage(), HttpMethod.Get, response, refitSettings);
		var apiResponse = new ApiResponse<SuccessResult>(response, null, refitSettings, apiException);
		A.CallTo(() => call(cancellationToken)).ReturnsLazily(() => apiResponse);

		// Act
		var result = await Sut.ExecuteCall<SuccessResult, ErrorTestResult?>(call, cancellationToken);

		// Assert
		A.CallTo(() => call(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
		result.Should().NotBeNull();
		result.Data.Should().BeNull();
		result.ErrorData.Should().NotBeNull();
		result.ErrorData!.Id.Should().BeNull();
		result.ErrorData!.B.Should().BeNull();
		result.ErrorData!.C.Should().BeNull();
	}

	[Fact]
	public async Task ExecuteCall_WithError_ReturnsErrorResult_CorrectPayload()
	{
		// Arrange
		var cancellationTokenSource = new CancellationTokenSource();
		var cancellationToken = cancellationTokenSource.Token;
		var call = A.Fake<Func<CancellationToken, Task<IApiResponse<SuccessResult>>>>();
		var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
		response.Content = new StringContent(JsonSerializer.Serialize(new ErrorTestResult
		{
			Id = "id",
			B = true,
			C = 5
		}));
		var refitSettings = new RefitSettings();
		var apiException = await ApiException.Create(new HttpRequestMessage(), HttpMethod.Get, response, refitSettings);
		var apiResponse = new ApiResponse<SuccessResult>(response, null, refitSettings, apiException);
		A.CallTo(() => call(cancellationToken)).ReturnsLazily(() => apiResponse);

		// Act
		var result = await Sut.ExecuteCall<SuccessResult, ErrorTestResult?>(call, cancellationToken);

		// Assert
		A.CallTo(() => call(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
		result.Should().NotBeNull();
		result.Data.Should().BeNull();
		result.ErrorData.Should().NotBeNull();
		result.ErrorData!.Id.Should().Be("id");
		result.ErrorData!.B.Should().BeTrue();
		result.ErrorData!.C.Should().Be(5);
	}
}
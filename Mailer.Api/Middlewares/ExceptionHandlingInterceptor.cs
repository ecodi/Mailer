using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Mailer.Exceptions;
using Microsoft.Extensions.Logging;
using Polly;

namespace Mailer.API.Middlewares
{
    public class ErrorHandlingInterceptor : Interceptor
    {
        private readonly ILogger<ErrorHandlingInterceptor> _logger;

        public ErrorHandlingInterceptor(ILogger<ErrorHandlingInterceptor> logger)
        {
            _logger = logger;
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                return await base.UnaryServerHandler(request, context, continuation);
            }
            catch (Exception ex)
            {
                throw ConvertedException(ex);
            }
        }

        public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream, ServerCallContext context,
            ClientStreamingServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                return await base.ClientStreamingServerHandler(requestStream, context, continuation);
            }
            catch (Exception ex)
            {
                throw ConvertedException(ex);
            }
        }

        public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
            TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context,
            ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                await base.ServerStreamingServerHandler(request, responseStream, context, continuation);
            }
            catch (Exception ex)
            {
                throw ConvertedException(ex);
            }
        }

        public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context,
            DuplexStreamingServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                await base.DuplexStreamingServerHandler(requestStream, responseStream, context, continuation);
            }
            catch (Exception ex)
            {
                throw ConvertedException(ex);
            }
        }

        private Exception ConvertedException(Exception exception)
        {
            var (code, message) = exception switch
            {
                NotFoundException _ => (StatusCode.NotFound, "Object not found"),
                InvalidVersionException _ => (StatusCode.FailedPrecondition, "Data modified by third party"),
                ValidationException ve => (StatusCode.InvalidArgument, ve.Message),
                MailerValidationException ve => (StatusCode.InvalidArgument, ve.Message),
                ExecutionRejectedException _ => (StatusCode.Unavailable, "External dependent service not available"),
                HttpRequestException _ => (StatusCode.Unavailable, "External dependent service not available"),
                _ => (StatusCode.Internal, "Internal server error")
            };

            _logger.LogError(exception, exception.Message);
            return new RpcException(new Status(code, message));
        }
    }
}

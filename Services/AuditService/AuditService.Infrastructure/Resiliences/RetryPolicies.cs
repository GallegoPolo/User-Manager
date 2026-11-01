using MongoDB.Driver;
using Polly;
using Polly.Retry;

namespace AuditService.Infrastructure.Resiliences
{
    public static class RetryPolicies
    {
        // Política para LEITURA (menos crítico - 3 tentativas)
        public static AsyncRetryPolicy CreateMongoReadPolicy()
        {
            return Policy
                .Handle<MongoException>()
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        Console.WriteLine(
                            $"[RETRY {retryCount}/3] Erro ao ler do MongoDB. " +
                            $"Tentando novamente em {timeSpan.TotalSeconds}s..."
                        );
                    }
                );
        }
    }
}

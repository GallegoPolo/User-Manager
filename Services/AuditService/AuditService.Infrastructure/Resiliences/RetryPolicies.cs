using MongoDB.Driver;
using Polly;
using Polly.Retry;

namespace AuditService.Infrastructure.Resiliences
{
    public static class RetryPolicies
    {
        // Política para ESCRITA (mais crítico - 4 tentativas)
        public static AsyncRetryPolicy CreateMongoWritePolicy()
        {
            return Policy
                .Handle<MongoException>() // Erro de conexão MongoDB
                .Or<TimeoutException>()    // Timeout de rede
                .WaitAndRetryAsync(
                    retryCount: 4,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    // 1ª tentativa: 2s, 2ª: 4s, 3ª: 8s, 4ª: 16s
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        Console.WriteLine(
                            $"[RETRY {retryCount}/4] Erro ao salvar no MongoDB. " +
                            $"Tentando novamente em {timeSpan.TotalSeconds}s... " +
                            $"Erro: {exception.Message}"
                        );
                    }
                );
        }

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

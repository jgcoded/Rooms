namespace p2p_api.Workers;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using p2p_api.Models;

/*
    This class periodically updates the shared secret between
    the P2P Credentials API and the coturn secret via a shared
    SQLite DB.

    Reference from coturn:
    https://github.com/coturn/coturn/blob/master/examples/scripts/restapi/shared_secret_maintainer.pl
*/
public class DatabaseWorker : IHostedService, IDisposable
{
    private int executionCount = 0;
    private readonly ILogger<DatabaseWorker> logger;
    private Timer timer = null!;

    private string oldSecret = string.Empty;
    private string currentSecret = string.Empty;

    private readonly string databaseConnectionString;

    private readonly string realm;

    private readonly TimeSpan timerTimeSpan;

    private ReferenceHolder databaseSecret;

    public DatabaseWorker(ReferenceHolder databaseSecret, IOptions<DatabaseWorkerOptions> workerOptions, ILogger<DatabaseWorker> logger)
    {
        var options = workerOptions.Value;

        var builder = new SqliteConnectionStringBuilder(options.DatabaseConnectionString)
        {
            Mode = SqliteOpenMode.ReadWriteCreate
        };
        this.databaseConnectionString = builder.ToString();
        this.realm = options.Realm;
        this.databaseSecret = databaseSecret;
        this.timerTimeSpan = TimeSpan.FromSeconds(options.TimerTimeSpanSeconds);
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Database Worker running.");

        using (var connection = new SqliteConnection(this.databaseConnectionString))
        {
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
                CREATE TABLE IF NOT EXISTS turn_secret (realm varchar(127), value varchar(127))
            ";
            await command.ExecuteNonQueryAsync();

            command = connection.CreateCommand();
            command.CommandText =
            @"
                delete from turn_secret
            ";
            int changes = await command.ExecuteNonQueryAsync();
            this.logger.LogInformation($"DatabaseWorker deleted {changes} rows");
        }

        this.timer = new Timer(DoWork, null, TimeSpan.Zero, this.timerTimeSpan);
    }

    private static string GenerateString()
    {
        var random = new Random();
        var builder = new System.Text.StringBuilder(32);
        while (builder.Length < builder.Capacity)
        {
            builder.Append(((char)random.Next(40, 126)));
        }

        return builder.ToString();
    }

    private void DoWork(object? state)
    {
        using (var connection = new SqliteConnection(this.databaseConnectionString))
        {
            connection.Open();
            if (this.currentSecret.Length > 0)
            {
                if (this.oldSecret.Length > 0) {
                    var command = connection.CreateCommand();
                    command.CommandText =
                    $@"
                        delete from turn_secret where value = '{this.oldSecret}'
                    ";
                    int deleted = command.ExecuteNonQuery();
                    this.logger.LogInformation("DatabaseWorker deleted old secret: {Count}", deleted);
                } // No old secret yet

                this.oldSecret = this.currentSecret;
            } // else first invocation

            this.currentSecret = GenerateString();
            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText =
            $@"
                insert into turn_secret values('{this.realm}','{this.currentSecret}')
            ";
            int inserted = insertCommand.ExecuteNonQuery();
            this.logger.LogInformation("DatabaseWorker inserted new secret: {Count}", inserted);
            this.databaseSecret.Data = currentSecret;
        }

        var count = Interlocked.Increment(ref this.executionCount);
        this.logger.LogInformation(
            "DatabaseWorker Execution Count: {Count}", count);
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        this.logger.LogInformation("DatabaseWorker is stopping.");

        this.timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        this.timer?.Dispose();
    }
}
namespace Rooms.Models;

public class DatabaseWorkerOptions
{
    public const string DatabaseWorker = "DatabaseWorker";

    public int TimerTimeSpanSeconds { get; set; }

    public string DatabaseConnectionString { get; set; } = string.Empty;

    public string Realm { get; set; } = string.Empty;
}
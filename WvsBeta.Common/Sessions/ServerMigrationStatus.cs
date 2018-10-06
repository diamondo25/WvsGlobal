namespace WvsBeta.Common.Sessions
{
    public enum ServerMigrationStatus
    {
        StartMigration,
        // Tell the server it stopped listening, so it should start
        StartListening,

        DataTransferRequest = 50,
        DataTransferResponse,
        DataTransferResponseChunked,
        DataTransferResponseChunkedDone,

        FinishedInitialization = 100,
        PlayersMigrated,
    }

    public enum ServerMigrationDataType
    {
        Parties,
        Messengers,
        Characters,
        Map,
    }
}

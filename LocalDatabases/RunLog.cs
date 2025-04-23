using Logging.lib;
using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace V275_REST_Lib.LocalDatabases
{
    public class RunLogDatabase
    {
        private bool disposedValue;

        private SQLiteConnection Connection { get; set; } = null;

        public RunLogDatabase() { }
        public RunLogDatabase(string dbFilePath) => Open(dbFilePath);
        public RunLogDatabase Open(string dbFilePath)
        {
            Logger.LogInfo($"Opening Database: {dbFilePath}");

            if (string.IsNullOrEmpty(dbFilePath))
                return null;
            try
            {
                Connection ??= new SQLiteConnection(dbFilePath);

                _ = Connection.CreateTable<RunLogReportData>();


                return this;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                return null;
            }
        }
        public void Close() => Connection?.Dispose();

        public List<RunLogReportData> SelectAllRunEntries() => Connection.CreateCommand("select * from reportData").ExecuteQuery<RunLogReportData>();
 
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Connection?.Dispose();
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~RunDatabase()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

    }
}

using System;
using System.Data.SQLite;
using Dapper;
using AeroScenery.Data.Models;
using System.Globalization;
using log4net;

namespace AeroScenery.Data
{
    public class SchemaUpgrader
    {
        private readonly ILog log = LogManager.GetLogger("AeroScenery");

        private string dbPath;

        public SchemaUpgrader(string dbPath)
        {
            this.dbPath = dbPath;
        }

        public void UpgradeToLatestSchema(int currentSchemaVersion)
        {
            switch (currentSchemaVersion)
            {
                case 1:
                    this.UpgradeToVersion2();
                    break;
                case 2:
                    this.UpgradeToVersion3();
                    break;
                case 3:
                    this.UpgradeToVersion4();
                    break;
                case 4:
                    this.UpgradeToVersion5();
                    break;
                case 5:
                    this.UpgradeToVersion6();
                    break;
                case 6:
                    this.UpgradeToVersion7();
                    break;
                case 7:
                    this.UpgradeToVersion8();
                    break;
                case 8:
                    break;
            }
        }

        private void SaveNewSchemaVersion(int newSchemaVersion)
        {
            using (var con = DbConnection())
            {
                var dbVersion = new DatabaseVersion();
                dbVersion.DatabaseVersionId = newSchemaVersion;
                dbVersion.UpgradedOn = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                var insertSql = @"INSERT INTO DatabaseVersion (DatabaseVersionId, UpgradedOn) VALUES (@DatabaseVersionId, @UpgradedOn);";

                // Add a column to store GridSquare level
                con.Open();
                con.Query(insertSql, dbVersion);
                con.Close();
            }
        }

        private void UpgradeToVersion2()
        {
            log.Info("Updating database to version 2");

            using (var con = DbConnection())
            {
                // Add a column to store GridSquare level
                con.Open();
                con.Execute(@"ALTER TABLE GridSquares ADD COLUMN Level INTEGER DEFAULT 9;");
                con.Close();
            }


            this.SaveNewSchemaVersion(2);
            this.UpgradeToVersion3();
        }

        private void UpgradeToVersion3()
        {
            log.Info("Updating database to version 3");

            using (var con = DbConnection())
            {
                con.Open();
                con.Execute(
                    @"create table FSCloudPortAirports
                      (
                        ICAO            TEXT PRIMARY KEY,
                        Latitude        REAL,
                        Longitude       REAL,
                        Runways         INTEGER,
                        Buildings       INTEGER,
                        StaticAircraft  INTEGER,
                        Name            TEXT,
                        LastModified    TEXT,
                        LastCached      TEXT
                      )");

                con.Execute(@"CREATE UNIQUE INDEX ix_FSCloudPortAirports_ICAO ON FSCloudPortAirports (ICAO ASC);");
            }

            this.SaveNewSchemaVersion(3);
            this.UpgradeToVersion4();
        }

        private void UpgradeToVersion4()
        {
            log.Info("Updating database to version 4");

            using (var con = DbConnection())
            {
                // Add a column for airport url
                con.Open();
                con.Execute(@"ALTER TABLE FSCloudPortAirports ADD COLUMN Url TEXT DEFAULT '';");
                con.Close();
            }


            this.SaveNewSchemaVersion(4);
            this.UpgradeToVersion5();
        }

        private void UpgradeToVersion5()
        {
            log.Info("Updating database to version 5");

            using (var con = DbConnection())
            {
                // Add a column for airport url
                con.Open();
                con.Execute(@"ALTER TABLE GridSquares ADD COLUMN Fixed INTEGER DEFAULT 0;");
                con.Close();
            }


            this.SaveNewSchemaVersion(5);
            this.UpgradeToVersion6();
        }

        private void UpgradeToVersion6()
        {
            log.Info("Updating database to version 6");

            using (var con = DbConnection())
            {
                con.Open();
                con.Execute(
                    @"create table if not exists OurAirports
                      (
                        Ident           TEXT PRIMARY KEY,
                        Type            TEXT,
                        Name            TEXT,
                        Latitude        REAL,
                        Longitude       REAL,
                        ElevationFt     INTEGER,
                        GpsCode         TEXT,
                        Url             TEXT
                      )");
                con.Execute(@"CREATE INDEX if not exists ix_OurAirports_LatLon ON OurAirports (Latitude, Longitude);");
                con.Execute(
                    @"create table if not exists OurAirportsImport
                      (
                        Id                 INTEGER PRIMARY KEY,
                        FileLength         INTEGER,
                        FileLastWriteUtc   TEXT,
                        ImportedUtc        TEXT,
                        RowCount           INTEGER
                      )");
                con.Close();
            }

            this.SaveNewSchemaVersion(6);
            this.UpgradeToVersion7();
        }

        private void UpgradeToVersion7()
        {
            log.Info("Updating database to version 7");

            using (var con = DbConnection())
            {
                con.Open();
                var hasUrl = false;
                foreach (var row in con.Query("PRAGMA table_info(OurAirports);"))
                {
                    if (string.Equals(Convert.ToString(row.name), "Url", StringComparison.OrdinalIgnoreCase))
                    {
                        hasUrl = true;
                        break;
                    }
                }

                if (!hasUrl)
                {
                    con.Execute(@"ALTER TABLE OurAirports ADD COLUMN Url TEXT;");
                }

                con.Close();
            }

            this.SaveNewSchemaVersion(7);
            this.UpgradeToVersion8();
        }

        private void UpgradeToVersion8()
        {
            log.Info("Updating database to version 8");

            using (var con = DbConnection())
            {
                con.Open();
                con.Execute(
                    @"create table if not exists OurRunways
                      (
                        Id              INTEGER PRIMARY KEY,
                        AirportIdent    TEXT,
                        LengthFt        INTEGER,
                        WidthFt         INTEGER,
                        Surface         TEXT,
                        LeIdent         TEXT,
                        HeIdent         TEXT
                      )");
                con.Execute(@"CREATE INDEX if not exists ix_OurRunways_AirportIdent ON OurRunways (AirportIdent);");
                con.Execute(
                    @"create table if not exists OurRunwaysImport
                      (
                        Id                 INTEGER PRIMARY KEY,
                        FileLength         INTEGER,
                        FileLastWriteUtc   TEXT,
                        ImportedUtc        TEXT,
                        RowCount           INTEGER
                      )");
                con.Close();
            }

            this.SaveNewSchemaVersion(8);
        }

        private SQLiteConnection DbConnection()
        {
            return new SQLiteConnection("Data Source=" + dbPath);
        }
    }
}

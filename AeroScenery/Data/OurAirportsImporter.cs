using AeroScenery.Data.Models;
using log4net;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace AeroScenery.Data
{
    public class OurAirportsImporter
    {
        public const string DownloadUrl = "https://ourairports.com/data/";
        private readonly ILog log = LogManager.GetLogger("AeroScenery");

        public static string GetCsvDirectory()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "AeroScenery",
                "ourairports");
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static string GetCsvPath()
        {
            return Path.Combine(GetCsvDirectory(), "airports.csv");
        }

        public static string GetRunwaysCsvPath()
        {
            return Path.Combine(GetCsvDirectory(), "runways.csv");
        }

        public List<OurAirport> ReadAirports(string csvPath)
        {
            var airports = new List<OurAirport>();

            using (var parser = new TextFieldParser(csvPath))
            {
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;
                parser.TrimWhiteSpace = true;

                if (parser.EndOfData)
                {
                    return airports;
                }

                var header = parser.ReadFields();
                var identIx = IndexOf(header, "ident");
                var typeIx = IndexOf(header, "type");
                var nameIx = IndexOf(header, "name");
                var latIx = IndexOf(header, "latitude_deg");
                var lonIx = IndexOf(header, "longitude_deg");
                var elevIx = IndexOf(header, "elevation_ft");
                var gpsIx = IndexOf(header, "gps_code");
                var wikiIx = IndexOf(header, "wikipedia_link");

                if (identIx < 0 || typeIx < 0 || nameIx < 0 || latIx < 0 || lonIx < 0)
                {
                    throw new InvalidDataException("The selected file is not an OurAirports airports.csv (missing columns).");
                }

                while (!parser.EndOfData)
                {
                    var fields = parser.ReadFields();
                    if (fields == null || fields.Length <= Math.Max(identIx, Math.Max(typeIx, Math.Max(latIx, lonIx))))
                    {
                        continue;
                    }

                    var type = fields[typeIx];
                    if (string.Equals(type, "closed", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(type, "balloonport", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    double lat;
                    double lon;
                    if (!double.TryParse(fields[latIx], NumberStyles.Float, CultureInfo.InvariantCulture, out lat)
                        || !double.TryParse(fields[lonIx], NumberStyles.Float, CultureInfo.InvariantCulture, out lon))
                    {
                        continue;
                    }

                    var ident = fields[identIx];
                    if (string.IsNullOrWhiteSpace(ident))
                    {
                        continue;
                    }

                    int? elevation = null;
                    int elevationFt;
                    if (elevIx >= 0 && elevIx < fields.Length
                        && int.TryParse(fields[elevIx], NumberStyles.Integer, CultureInfo.InvariantCulture, out elevationFt))
                    {
                        elevation = elevationFt;
                    }

                    string gpsCode = OptionalField(fields, gpsIx);
                    string wikipediaUrl = OptionalField(fields, wikiIx);

                    airports.Add(new OurAirport
                    {
                        Ident = ident.Trim(),
                        Type = type,
                        Name = fields[nameIx],
                        Latitude = lat,
                        Longitude = lon,
                        ElevationFt = elevation,
                        GpsCode = gpsCode,
                        Url = wikipediaUrl
                    });
                }
            }

            log.InfoFormat("Read {0} OurAirports rows from {1}", airports.Count, csvPath);
            return airports;
        }

        public List<OurRunway> ReadRunways(string csvPath)
        {
            var runways = new List<OurRunway>();

            using (var parser = new TextFieldParser(csvPath))
            {
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;
                parser.TrimWhiteSpace = true;

                if (parser.EndOfData)
                {
                    return runways;
                }

                var header = parser.ReadFields();
                var idIx = IndexOf(header, "id");
                var identIx = IndexOf(header, "airport_ident");
                var lengthIx = IndexOf(header, "length_ft");
                var widthIx = IndexOf(header, "width_ft");
                var surfaceIx = IndexOf(header, "surface");
                var closedIx = IndexOf(header, "closed");
                var leIx = IndexOf(header, "le_ident");
                var heIx = IndexOf(header, "he_ident");

                if (idIx < 0 || identIx < 0)
                {
                    throw new InvalidDataException("The selected file is not an OurAirports runways.csv (missing columns).");
                }

                while (!parser.EndOfData)
                {
                    var fields = parser.ReadFields();
                    if (fields == null || fields.Length <= Math.Max(idIx, identIx))
                    {
                        continue;
                    }

                    if (IsClosedFlag(OptionalField(fields, closedIx)))
                    {
                        continue;
                    }

                    long id;
                    if (!long.TryParse(fields[idIx], NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
                    {
                        continue;
                    }

                    var airportIdent = OptionalField(fields, identIx);
                    if (string.IsNullOrEmpty(airportIdent))
                    {
                        continue;
                    }

                    runways.Add(new OurRunway
                    {
                        Id = id,
                        AirportIdent = airportIdent,
                        LengthFt = OptionalInt(fields, lengthIx),
                        WidthFt = OptionalInt(fields, widthIx),
                        Surface = OptionalField(fields, surfaceIx),
                        LeIdent = OptionalField(fields, leIx),
                        HeIdent = OptionalField(fields, heIx)
                    });
                }
            }

            log.InfoFormat("Read {0} OurRunways rows from {1}", runways.Count, csvPath);
            return runways;
        }

        public static string[] TypesForZoom(double zoom)
        {
            if (zoom < 7)
            {
                return new string[0];
            }

            if (zoom < 9)
            {
                return new[] { "large_airport" };
            }

            if (zoom < 11)
            {
                return new[] { "large_airport", "medium_airport" };
            }

            if (zoom < 13)
            {
                return new[] { "large_airport", "medium_airport", "small_airport" };
            }

            return new[] { "large_airport", "medium_airport", "small_airport", "heliport", "seaplane_base" };
        }

        public static string FormatType(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return "";
            }

            return type.Replace('_', ' ');
        }

        public static string FormatRunway(OurRunway runway)
        {
            if (runway == null)
            {
                return "";
            }

            string ends = runway.LeIdent ?? "";
            if (!string.IsNullOrEmpty(runway.HeIdent))
            {
                ends = string.IsNullOrEmpty(ends)
                    ? runway.HeIdent
                    : ends + "/" + runway.HeIdent;
            }

            string length = runway.LengthFt.HasValue
                ? string.Format(CultureInfo.InvariantCulture, "{0:N0} ft", runway.LengthFt.Value)
                : "";

            string surface = FormatType(runway.Surface);

            var parts = new List<string>();
            if (!string.IsNullOrEmpty(ends))
            {
                parts.Add(ends);
            }
            if (!string.IsNullOrEmpty(length))
            {
                parts.Add(length);
            }
            if (!string.IsNullOrEmpty(surface))
            {
                parts.Add(surface);
            }

            return string.Join("  ·  ", parts.ToArray());
        }

        private static bool IsClosedFlag(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return value == "1"
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
        }

        private static int? OptionalInt(string[] fields, int index)
        {
            var text = OptionalField(fields, index);
            int value;
            if (string.IsNullOrEmpty(text)
                || !int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return null;
            }

            return value;
        }

        private static string OptionalField(string[] fields, int index)
        {
            if (index < 0 || index >= fields.Length || string.IsNullOrWhiteSpace(fields[index]))
            {
                return null;
            }

            return fields[index].Trim();
        }

        private static int IndexOf(string[] header, string name)
        {
            for (int i = 0; i < header.Length; i++)
            {
                if (string.Equals(header[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}

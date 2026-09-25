namespace AeroScenery.Data.Models
{
    public class OurAirport
    {
        public string Ident { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int? ElevationFt { get; set; }
        public string GpsCode { get; set; }
        public string Url { get; set; }
    }

    public class OurRunway
    {
        public long Id { get; set; }
        public string AirportIdent { get; set; }
        public int? LengthFt { get; set; }
        public int? WidthFt { get; set; }
        public string Surface { get; set; }
        public string LeIdent { get; set; }
        public string HeIdent { get; set; }
    }

    public class OurAirportsImportInfo
    {
        public long FileLength { get; set; }
        public string FileLastWriteUtc { get; set; }
        public string ImportedUtc { get; set; }
        public int RowCount { get; set; }
    }
}

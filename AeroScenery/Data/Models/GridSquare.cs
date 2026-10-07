namespace AeroScenery.Data.Models
{
    public class GridSquare
    {
        public long GridSquareId { get; set; }
        public string Name { get; set; }
        public double NorthLatitude { get; set; }
        public double EastLongitude { get; set; }
        public double WestLongitude { get; set; }
        public double SouthLatitude { get; set; }
        public int Level { get; set; }
        public int Fixed { get; set; }

        public string ConvertedUtc { get; set; }
        public string ImageSource { get; set; }
        public int? ImageZoomLevel { get; set; }
        public int WaterMasking { get; set; }
        public string WaterMaskingParams { get; set; }
        public int ImageProcessing { get; set; }
        public string ImageProcessingParams { get; set; }
        public string ImageProcessingRgb { get; set; }
        public int OsmDownloaded { get; set; }
        public int ElevationDownloaded { get; set; }
        public int ShiftCorrection { get; set; }
        public int? ShiftCorrectionLevel { get; set; }
    }
}

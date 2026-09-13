namespace AeroScenery.Common
{
    public class GeoCoordinate
    {
        public double Latitude;
        public double Longitude;
        public double? Altitude;

        public GeoCoordinate()
        {

        }

        public GeoCoordinate(double latitude, double longitude)
        {
            this.Latitude = latitude;
            this.Longitude = longitude;
        }

        public GeoCoordinate(double latitude, double longitude, double altitude)
        {
            this.Latitude = latitude;
            this.Longitude = longitude;
            this.Altitude = altitude;
        }
    }
}

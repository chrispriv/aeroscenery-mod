using AeroScenery.Controls;
using AeroScenery.Data.Models;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace AeroScenery.UI
{
    public class FSCloudPortMarkerManager
    {
        public GMapControl GMapControl { get; set; }

        private GMapOverlay airportMarkers;

        private Dictionary<string, FSCloudPortAirport> airportLookup;

        private FSCloudPortAirportPopup fsCloudPortPopup;

        private PopperContainer containerForFSCloudPortPopup;

        public bool PopupShown { get; set; }

        public int ClickCount { get; set; }

        public FSCloudPortMarkerManager()
        {
            airportMarkers = new GMapOverlay("FSCloudPortAirportMarkers");
            airportLookup = new Dictionary<string, FSCloudPortAirport>();

            // FSCloudPort popup window
            this.fsCloudPortPopup = new FSCloudPortAirportPopup();
            this.containerForFSCloudPortPopup = new PopperContainer(fsCloudPortPopup);
            this.containerForFSCloudPortPopup.AutoClose = false;

            this.fsCloudPortPopup.CloseClicked += FsCloudPortPopup_CloseClicked;

            this.containerForFSCloudPortPopup.Opened += ContainerForFSCloudPortPopup_Opened;
            this.containerForFSCloudPortPopup.Closed += ContainerForFSCloudPortPopup_Closed;
        }

        private void ContainerForFSCloudPortPopup_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            this.PopupShown = false;
        }

        private void ContainerForFSCloudPortPopup_Opened(object sender, EventArgs e)
        {
            this.ClickCount = 0;
            this.PopupShown = true;
        }

        private void FsCloudPortPopup_CloseClicked(object sender, EventArgs e)
        {
            this.containerForFSCloudPortPopup.Close();
        }

        public Func<RectLatLng, double, IList<FSCloudPortAirport>> LoadAirportsInView { get; set; }

        public Func<string, IList<OurRunway>> LoadRunwaysForAirport { get; set; }

        public void UpdateFSCloudPortMarkers()
        {
            airportMarkers.Markers.Clear();
            //#MOD_k
            int index = this.GMapControl.Overlays.IndexOf(airportMarkers);
            if (index >= 0)
            {
                this.GMapControl.Overlays.RemoveAt(index);
            }

            this.GMapControl.Overlays.Add(airportMarkers);

            var mapBounds = this.GMapControl.ViewArea;

            if (this.GMapControl.Zoom >= 7 && mapBounds != null)
            {
                IList<FSCloudPortAirport> airports = null;
                if (this.LoadAirportsInView != null)
                {
                    airports = this.LoadAirportsInView(mapBounds, this.GMapControl.Zoom);
                    this.airportLookup.Clear();
                    if (airports != null)
                    {
                        foreach (var airport in airports)
                        {
                            if (!string.IsNullOrEmpty(airport.ICAO) && !this.airportLookup.ContainsKey(airport.ICAO))
                            {
                                this.airportLookup.Add(airport.ICAO, airport);
                            }
                        }
                    }
                }

                if (airports == null)
                {
                    return;
                }

                foreach (var airport in airports)
                {
                    var point = new PointLatLng(airport.Latitude, airport.Longitude);
                    var marker = new GMarkerGoogle(point, new Bitmap(Properties.Resources.windsock));
                    marker.Tag = airport.ICAO;
                    airportMarkers.Markers.Add(marker);
                }
            }

        }

        public IList<FSCloudPortAirport> Airports
        {
            set
            {
                foreach (var airport in value)
                {
                    airportLookup.Add(airport.ICAO, airport);
                }
            }
        }

        public void RemoveAllFSCloudPortMarkers()
        {
            airportMarkers.Markers.Clear();
            if (this.GMapControl != null && this.GMapControl.Overlays.Contains(airportMarkers))
            {
                this.GMapControl.Overlays.Remove(airportMarkers);
            }
        }

        public void ShowAirportPopup(string icao, Form form, Point location)
        {
            FSCloudPortAirport airport = null;

            if (this.airportLookup.TryGetValue(icao, out airport))
            {
                this.fsCloudPortPopup.Airport = airport;
                if (this.LoadRunwaysForAirport != null)
                {
                    this.fsCloudPortPopup.SetRunways(this.LoadRunwaysForAirport(icao));
                }
                else
                {
                    this.fsCloudPortPopup.SetRunways(null);
                }

                this.containerForFSCloudPortPopup.Show(form, location);

            }

        }

        public void CloseAirportPopup()
        {
            this.containerForFSCloudPortPopup.Close();
        }



    }
}

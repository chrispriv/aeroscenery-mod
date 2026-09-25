using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using AeroScenery.Data;
using AeroScenery.Data.Models;

namespace AeroScenery.UI
{
    public partial class FSCloudPortAirportPopup : UserControl
    {
        public event EventHandler CloseClicked;

        private FSCloudPortAirport airport;
        private Label runwayDetailsLabel;


        public FSCloudPortAirportPopup()
        {
            InitializeComponent();
            this.lastUpdatedLabel.AutoEllipsis = true;
            this.lastUpdatedLabel.ForeColor = Color.RoyalBlue;
            this.lastUpdatedLabel.Cursor = Cursors.Hand;
            this.lastUpdatedLabel.Click += WikipediaLabel_Click;

            this.runwayDetailsLabel = new Label();
            this.runwayDetailsLabel.Font = new Font("Segoe UI", 9.75F);
            this.runwayDetailsLabel.Location = new Point(15, 116);
            this.runwayDetailsLabel.Size = new Size(343, 54);
            this.runwayDetailsLabel.TextAlign = ContentAlignment.TopCenter;
            this.runwayDetailsLabel.Visible = false;
            this.Controls.Add(this.runwayDetailsLabel);
            this.runwayDetailsLabel.BringToFront();
        }

        public FSCloudPortAirport Airport
        {
            get
            {
                return this.airport;
            }
            set
            {
                this.airport = value;
                this.icaoLabel.Text = value.ICAO;
                this.nameLabel.Text = value.Name;
                this.aircraftLabel.Visible = false;
                this.downloadButton.Visible = false;
                this.runwaysLabel.Text = OurAirportsImporter.FormatType(value.Type);
                this.buildingsLabel.Text = value.ElevationFt.HasValue
                    ? string.Format("{0} ft", value.ElevationFt.Value)
                    : "";

                bool hasWikipedia = !string.IsNullOrWhiteSpace(value.Url);
                this.lastUpdatedLabel.Visible = hasWikipedia;
                this.lastUpdatedLabel.Text = hasWikipedia ? value.Url : "";
                this.SetRunways(null);
            }
        }

        public void SetRunways(IList<OurRunway> runways)
        {
            if (this.runwayDetailsLabel == null)
            {
                return;
            }

            if (runways == null || runways.Count == 0)
            {
                this.runwayDetailsLabel.Visible = false;
                this.runwayDetailsLabel.Text = "";
                return;
            }

            const int maxLines = 3;
            var text = new StringBuilder();
            int shown = Math.Min(maxLines, runways.Count);
            for (int i = 0; i < shown; i++)
            {
                if (i > 0)
                {
                    text.AppendLine();
                }

                text.Append(OurAirportsImporter.FormatRunway(runways[i]));
            }

            if (runways.Count > maxLines)
            {
                text.AppendLine();
                text.AppendFormat("+ {0} more", runways.Count - maxLines);
            }

            this.runwayDetailsLabel.Text = text.ToString();
            this.runwayDetailsLabel.Visible = true;
        }


        private void WikipediaLabel_Click(object sender, EventArgs e)
        {
            if (this.airport == null || string.IsNullOrWhiteSpace(this.airport.Url))
            {
                return;
            }

            try
            {
                Process.Start(this.airport.Url);
            }
            catch
            {
            }
        }

        private void DownloadButton_Click(object sender, EventArgs e)
        {
            if (CloseClicked != null)
            {
                CloseClicked(this, e);
            }
        }

        private void CloseLabel_Click(object sender, EventArgs e)
        {
            if (CloseClicked != null)
            {
                CloseClicked(this, e);
            }
        }
    }
}

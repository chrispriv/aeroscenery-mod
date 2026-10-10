using System;
using System.Drawing;
using System.Windows.Forms;
using AeroScenery.Data.Models;

namespace AeroScenery.UI
{
    /// <summary>
    /// Read-only details for one Aerofly grid square (Generate AID/TMC, OSM, elevation).
    /// </summary>
    public class GridSquareInfoForm : Form
    {
        public GridSquareInfoForm(string tileName, GridSquare record)
        {
            this.Text = "Tile " + tileName;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.Font = new Font("Segoe UI", 9.75F);
            this.ClientSize = new Size(378, 344);
            this.Padding = new Padding(12);

            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 2;
            layout.RowCount = 14;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.AutoSize = true;

            bool hasGenerate = record != null && !String.IsNullOrEmpty(record.ConvertedUtc);

            this.AddRow(layout, 0, "Tile", tileName);
            this.AddRow(layout, 1, "Generate AID / TMC", hasGenerate ? record.ConvertedUtc + " UTC" : "No record yet");
            this.AddRow(layout, 2, "Image source", Display(hasGenerate ? record.ImageSource : null));
            this.AddRow(layout, 3, "Image detail (zoom)", hasGenerate && record.ImageZoomLevel.HasValue
                ? record.ImageZoomLevel.Value.ToString()
                : "—");
            this.AddRow(layout, 4, "Enhanced water masking", YesNo(hasGenerate ? record.WaterMasking : 0));
            this.AddRow(layout, 5, "Fade out / Replace 100%", hasGenerate && record.WaterMasking == 1
                ? Display(record.WaterMaskingParams)
                : "—");
            this.AddRow(layout, 6, "Adjust stitched images", YesNo(hasGenerate ? record.ImageProcessing : 0));
            this.AddRow(layout, 7, "Brightness / Contrast / Saturation / Sharpness",
                hasGenerate && record.ImageProcessing == 1 ? Display(record.ImageProcessingParams) : "—");
            this.AddRow(layout, 8, "Red / Green / Blue",
                hasGenerate && record.ImageProcessing == 1 ? Display(record.ImageProcessingRgb) : "—");
            this.AddRow(layout, 9, "Shift correction", ShiftText(hasGenerate ? record : null));
            this.AddRow(layout, 10, "OSM download", YesNo(record != null ? record.OsmDownloaded : 0));
            this.AddRow(layout, 11, "Elevation data download", YesNo(record != null ? record.ElevationDownloaded : 0));

            var closeButton = new Button();
            closeButton.Text = "Close";
            closeButton.DialogResult = DialogResult.OK;
            closeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            closeButton.Location = new Point(this.ClientSize.Width - 96, this.ClientSize.Height - 36);
            closeButton.Size = new Size(84, 26);
            this.AcceptButton = closeButton;
            this.CancelButton = closeButton;

            layout.Location = new Point(12, 12);
            layout.Size = new Size(this.ClientSize.Width - 24, this.ClientSize.Height - 52);
            this.Controls.Add(layout);
            this.Controls.Add(closeButton);
        }

        private void AddRow(TableLayoutPanel layout, int row, string label, string value)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var nameLabel = new Label();
            nameLabel.AutoSize = true;
            nameLabel.Text = label;
            nameLabel.Margin = new Padding(0, 4, 8, 4);
            var valueLabel = new Label();
            valueLabel.AutoSize = true;
            valueLabel.Text = value;
            valueLabel.Margin = new Padding(0, 4, 0, 4);
            layout.Controls.Add(nameLabel, 0, row);
            layout.Controls.Add(valueLabel, 1, row);
        }

        private static string YesNo(int value)
        {
            return value == 1 ? "Yes" : "No";
        }

        private static string Display(string value)
        {
            return String.IsNullOrWhiteSpace(value) ? "—" : value;
        }

        private static string ShiftText(GridSquare record)
        {
            if (record == null || record.ShiftCorrection != 1)
            {
                return "No";
            }

            if (record.ShiftCorrectionLevel.HasValue)
            {
                return "Yes, " + record.ShiftCorrectionLevel.Value.ToString();
            }

            return "Yes";
        }
    }
}

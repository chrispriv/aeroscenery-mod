using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AeroScenery.FlightPathVisualizer.Instruments
{
    public class ElevationProfileOverlayControl : UserControl
    {
        public List<double> ElevationProfilePoints { get; set; } = new List<double>();

        public ElevationProfileOverlayControl()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }
        public double AircraftAltitudeMeters { get; set; }
        public double AircraftHeadingDegrees { get; set; }
        public double AircraftVerticalSpeedMs { get; set; }
        public bool ShowInFeet { get; set; } = false;


        /*
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (ElevationProfilePoints == null || ElevationProfilePoints.Count < 2)
                return;

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int w = Width;
            int h = Height;

            // Datenbereich bestimmen
            double minElevation = ElevationProfilePoints.Where(p => !double.IsNaN(p)).DefaultIfEmpty(0).Min();
            double maxElevation = ElevationProfilePoints.Where(p => !double.IsNaN(p)).DefaultIfEmpty(1).Max();

            double maxReference = Math.Max(maxElevation, AircraftAltitudeMeters);
            double minDisplay = Math.Floor(minElevation / 200.0) * 200.0;
            double maxDisplay = Math.Ceiling(maxReference / 200.0) * 200.0;
            double range = maxDisplay - minDisplay;
            if (range == 0) range = 1; // zur Sicherheit

            // Polygon vorbereiten
            var polygonPoints = new List<PointF>();
            polygonPoints.Add(new PointF(0, h)); // Start unten links

            for (int i = 0; i < ElevationProfilePoints.Count; i++)
            {
                double value = ElevationProfilePoints[i];
                if (double.IsNaN(value)) continue;

                float x = i * w / (ElevationProfilePoints.Count - 1);
                float y = h - (float)((value - minDisplay) / range * h);
                polygonPoints.Add(new PointF(x, y));
            }

            polygonPoints.Add(new PointF(w, h)); // Ende unten rechts

            // Fläche zeichnen
            using (var fillBrush = new SolidBrush(Color.FromArgb(160, Color.Orange)))
            {
                g.FillPolygon(fillBrush, polygonPoints.ToArray());
            }

            // Höhenlinie über dem Polygon
            using (var linePen = new Pen(Color.DarkOrange, 2f))
            {
                for (int i = 1; i < ElevationProfilePoints.Count; i++)
                {
                    if (double.IsNaN(ElevationProfilePoints[i - 1]) || double.IsNaN(ElevationProfilePoints[i]))
                        continue;

                    float x0 = (i - 1) * w / (ElevationProfilePoints.Count - 1);
                    float x1 = i * w / (ElevationProfilePoints.Count - 1);

                    float y0 = h - (float)((ElevationProfilePoints[i - 1] - minDisplay) / range * h);
                    float y1 = h - (float)((ElevationProfilePoints[i] - minDisplay) / range * h);

                    g.DrawLine(linePen, x0, y0, x1, y1);
                }
            }

            // Flughöhenlinie (gelb gestrichelt)
            float aircraftY = h - (float)((AircraftAltitudeMeters - minDisplay) / range * h);
            using (var altitudePen = new Pen(Color.Yellow, 1.5f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
            {
                g.DrawLine(altitudePen, 0, aircraftY, w, aircraftY);
            }
        }
        */

        /*
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (ElevationProfilePoints == null || ElevationProfilePoints.Count < 2)
                return;

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int w = Width;
            int h = Height;

            // Zeichenzonen (Padding)
            int topPadding = (int)(h * 0.30); // 0.10
            int bottomPadding = (int)(h * 0.25);// 0.15
            int usableHeight = h - topPadding - bottomPadding;

            // Datenbereich
            double minElevation = ElevationProfilePoints.Where(p => !double.IsNaN(p)).DefaultIfEmpty(0).Min();
            double maxElevation = ElevationProfilePoints.Where(p => !double.IsNaN(p)).DefaultIfEmpty(1).Max();
            double maxReference = Math.Max(maxElevation, AircraftAltitudeMeters);
            double minDisplay = Math.Floor(minElevation / 200.0) * 200.0;
            double maxDisplay = Math.Ceiling(maxReference / 200.0) * 200.0;
            double range = maxDisplay - minDisplay;
            if (range == 0) range = 1;

            // Konvertierungsfunktion
            Func<double, float> ElevationToY = (elev) =>
            {
                return topPadding + (float)((maxDisplay - elev) / range * usableHeight);
            };

            // Polygon
            var polygonPoints = new List<PointF> { new PointF(0, h - bottomPadding) };
            for (int i = 0; i < ElevationProfilePoints.Count; i++)
            {
                double value = ElevationProfilePoints[i];
                if (double.IsNaN(value)) continue;

                float x = i * w / (ElevationProfilePoints.Count - 1);
                float y = ElevationToY(value);
                polygonPoints.Add(new PointF(x, y));
            }
            polygonPoints.Add(new PointF(w, h - bottomPadding));

            using (var fillBrush = new SolidBrush(Color.FromArgb(160, Color.Orange)))
                g.FillPolygon(fillBrush, polygonPoints.ToArray());

            // Höhenlinie
            using (var linePen = new Pen(Color.DarkOrange, 2f))
            {
                for (int i = 1; i < ElevationProfilePoints.Count; i++)
                {
                    if (double.IsNaN(ElevationProfilePoints[i - 1]) || double.IsNaN(ElevationProfilePoints[i]))
                        continue;

                    float x0 = (i - 1) * w / (ElevationProfilePoints.Count - 1);
                    float x1 = i * w / (ElevationProfilePoints.Count - 1);
                    float y0 = ElevationToY(ElevationProfilePoints[i - 1]);
                    float y1 = ElevationToY(ElevationProfilePoints[i]);

                    g.DrawLine(linePen, x0, y0, x1, y1);
                }
            }

            // Flughöhe
            float aircraftY = ElevationToY(AircraftAltitudeMeters);
            using (var pen = new Pen(Color.Yellow, 1.5f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                g.DrawLine(pen, 0, aircraftY, w, aircraftY);

            // --- Beschriftungen: Höhe links ---
            using (var font = new Font("Segoe UI", 8))
            using (var brush = new SolidBrush(Color.White))
            using (var gridPen = new Pen(Color.Gray, 1f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
            {
                int numTicks = 5;
                for (int i = 0; i <= numTicks; i++)
                {
                    double elev = minDisplay + i * (range / numTicks);
                    float y = ElevationToY(elev);

                    g.DrawLine(gridPen, 0, y, w, y);
                    string label = ShowInFeet
                        ? $"{elev * 3.28084:F0} ft"
                        : $"{elev:F0} m";
                    g.DrawString(label, font, brush, 4, y - 8);
                }
            }

            // --- Distanz-Beschriftung unten ---
            using (var font = new Font("Segoe UI", 8))
            using (var brush = new SolidBrush(Color.LightGray))
            {
                int numSegments = 5;
                double totalDistanceKm = 10.0;
                for (int i = 0; i <= numSegments; i++)
                {
                    float x = i * w / numSegments;
                    string label = $"{(i * totalDistanceKm / numSegments):F1} km";
                    SizeF size = g.MeasureString(label, font);
                    g.DrawString(label, font, brush, x - size.Width / 2, h - bottomPadding + 2);
                }
            }
        }
        */

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (ElevationProfilePoints == null || ElevationProfilePoints.Count < 2)
                return;

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int w = Width;
            int h = Height;

            // ░░░░░ Daten analysieren ░░░░░
            double maxElevation = ElevationProfilePoints.Max();
            double minElevation = ElevationProfilePoints.Min();

            // NaN-Werte filtern
            if (double.IsNaN(maxElevation) || double.IsNaN(minElevation))
                return;

            // Dynamischer Höhenlinien-Abstand basierend auf Höhenunterschied
            double range = maxElevation - minElevation;
            double tickSpacing;

            if (range < 600)
                //tickSpacing = 100;
                tickSpacing = 500;
            else if (range < 1200)
                //tickSpacing = 200;
                tickSpacing = 500;
            else if (range < 3000)
                tickSpacing = 500;
            else
                tickSpacing = 1000;

            double startElev = Math.Floor(minElevation / tickSpacing) * tickSpacing;
            double aircraftElev = AircraftAltitudeMeters;
            double maxReference = Math.Max(maxElevation, aircraftElev);
            double endElev = Math.Ceiling(maxReference / tickSpacing) * tickSpacing;

            double groundElevation = ElevationProfilePoints[0]; // unter dem Flugzeug
            double aircraftElevAgl = AircraftAltitudeMeters - groundElevation;

            // Zeichnungshöhe definieren (Platz oben/unten für Labels lassen)
            int marginTop = 50; //20
            int marginBottom = 50; //20
            int usableHeight = h - marginTop - marginBottom;

            // Lokale Funktion für Y-Koordinaten
            float ElevationToY(double elev)
            {
                return marginTop + (float)((endElev - elev) / (endElev - startElev) * usableHeight);
            }

            // ░░░░░ Gelände als Polygon füllen ░░░░░
            //using (var fillBrush = new SolidBrush(Color.FromArgb(100, Color.SaddleBrown)))
            using (var fillBrush = new SolidBrush(Color.FromArgb(200, 160, 82, 45))) // gleiche Farbe dunkelbraun wie bei HUD
            {
                var polygonPoints = new List<PointF>();

                // Startlinie unten links
                //polygonPoints.Add(new PointF(0, ElevationToY(startElev)));
                polygonPoints.Add(new PointF(0, h)); // unterer linker Rand (ganz unten)

                for (int i = 0; i < ElevationProfilePoints.Count; i++)
                {
                    double elev = ElevationProfilePoints[i];
                    if (double.IsNaN(elev)) continue;

                    float x = i * w / (ElevationProfilePoints.Count - 1);
                    float y = ElevationToY(elev);
                    polygonPoints.Add(new PointF(x, y));
                }

                // Endlinie unten rechts
                //polygonPoints.Add(new PointF(w, ElevationToY(startElev)));
                polygonPoints.Add(new PointF(w, h)); // unterer rechter Rand (ganz unten)

                // Füllen
                if (polygonPoints.Count >= 3)
                    g.FillPolygon(fillBrush, polygonPoints.ToArray());
            }

            // ░░░░░ Profil-Linie Orange ░░░░░
            //using (var pen = new Pen(Color.Orange, 2))
            using (var pen = new Pen(Color.White, 2))
            {
                for (int i = 1; i < ElevationProfilePoints.Count; i++)
                {
                    double y0Val = ElevationProfilePoints[i - 1];
                    double y1Val = ElevationProfilePoints[i];

                    if (double.IsNaN(y0Val) || double.IsNaN(y1Val))
                        continue;

                    float x0 = (i - 1) * w / (ElevationProfilePoints.Count - 1);
                    float x1 = i * w / (ElevationProfilePoints.Count - 1);

                    float y0 = ElevationToY(y0Val);
                    float y1 = ElevationToY(y1Val);

                    g.DrawLine(pen, x0, y0, x1, y1);
                }
            }

            // ░░░░░ Höhenlinien & Labels ░░░░░
            //using var gridPen = new Pen(Color.FromArgb(80, 200, 200, 200), 1);
            //using var font = new Font("Segoe UI", 8);
            //using var brush = new SolidBrush(Color.White);

            using (var font = new Font("Arial", 9))
            using (var brush = new SolidBrush(Color.White))
            using (var gridPen = new Pen(Color.Gray, 1f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })

                for (double elev = startElev; elev <= endElev; elev += tickSpacing)
            {
                float y = ElevationToY(elev);
                g.DrawLine(gridPen, 0, y, w, y);

                string label = ShowInFeet
                    ? $"{elev * 3.28084:F0} ft"
                    : $"{elev:F0} m";

                //g.DrawString(label, font, brush, 4, y - 8);
                g.DrawString(label, font, brush, 8, y - 8);
                }

            // ░░░░░ Flugzeug-Höhenlinie ░░░░░
            if (!double.IsNaN(aircraftElev))
            {
                float y = ElevationToY(aircraftElev);
                //using var aircraftPen = new Pen(Color.Red, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
                //using (var aircraftPen = new Pen(Color.Yellow, 1.5f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                using (var aircraftPen = new Pen(Color.Black, 1.5f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                    g.DrawLine(aircraftPen, 0, y, w, y);

                string aircraftLabel = ShowInFeet
                    ? $"{aircraftElev * 3.28084:F0} ft (ALT)"
                    : $"{aircraftElev:F0} m (ALT)";

                string aircraftLabelAgl = ShowInFeet
                    ? $"{aircraftElevAgl * 3.28084:F0} ft (AGL)"
                    : $"{aircraftElevAgl:F0} m (AGL)";

                //using (var brush = new SolidBrush(Color.LightGray))
                //using (var brush = new SolidBrush(Color.Yellow))
                using (var font = new Font("Arial", 10, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.Black))
                {
                    //g.DrawString(aircraftLabel, font, brush, 4, y + 2);
                    g.DrawString(aircraftLabel, font, brush, w - 110, y - 18);
                    g.DrawString(aircraftLabelAgl, font, brush, w - 110, y + 2);
                    //#DEVL!!!!!!
                    //g.DrawString("360° (HDG)", font, brush, w - 100, h - marginBottom + 2);
                    //g.DrawString("360° (HDG)", font, brush, 8, 10);
                    //g.DrawString("360° (HDG)", font, brush, w - 100, 10);
                    g.DrawString($"{AircraftHeadingDegrees: 0}° (HDG)", font, brush, 60 , y - 18);
                    //g.DrawString($"{AircraftVerticalSpeedMs: 0} ms (VS)", font, brush, w /2 , y + 2);

                }

            }

            // ░░░░░ Horizontale Distanzmarkierung unten ░░░░░
            int kmLabelInterval = 2;
            int numLabels = 10 / kmLabelInterval; // für 10 km
            for (int i = 0; i <= numLabels; i++)
            {
                float x = i * w / numLabels;
                string label = $"{i * kmLabelInterval} km";
                using (var font = new Font("Arial", 9))
                using (var brush = new SolidBrush(Color.LightGray))
                    //g.DrawString(label, font, brush, x - 10, h - marginBottom + 2);
                    g.DrawString(label, font, brush, x + 2, h - marginBottom / 2 + 0);
            }


            // ░░░░░ Flugzeugsymbol am linken Rand ░░░░░
            float aircraftY = ElevationToY(AircraftAltitudeMeters);

            using (var pen = new Pen(Color.Black, 2))
            {
                PointF p1 = new PointF(5, aircraftY);
                PointF p2 = new PointF(0, aircraftY - 5);
                PointF p3 = new PointF(0, aircraftY + 5);

                g.DrawPolygon(pen, new[] { p1, p2, p3 });
                g.FillPolygon(Brushes.White, new[] { p1, p2, p3 }); // optional ausfüllen
            }

            // ░░░░░ Vertical Speed Indicator ░░░░░
            float verticalSpeedY = Math.Min((float)Math.Abs(AircraftVerticalSpeedMs) * 2, h /4);
            float posVerticalSpeedX = w - 16;

            using (var pen = new Pen(Color.Black, 2))
            {
                if (AircraftVerticalSpeedMs >= 0.5)
                {
                    PointF p1 = new PointF(posVerticalSpeedX + 5, aircraftY - 4 - verticalSpeedY - 5);
                    PointF p2 = new PointF(posVerticalSpeedX + 0, aircraftY - 4 - verticalSpeedY - 0);

                    PointF p3 = new PointF(posVerticalSpeedX + 0, aircraftY - 4 - 0);
                    PointF p4 = new PointF(posVerticalSpeedX + 10, aircraftY - 4 - 0);

                    PointF p5 = new PointF(posVerticalSpeedX + 10, aircraftY - 4 - verticalSpeedY - 0);

                    g.DrawPolygon(pen, new[] { p1, p2, p3, p4, p5 });
                    g.FillPolygon(Brushes.White, new[] { p1, p2, p3, p4, p5 }); // optional ausfüllen
                }
                else if (AircraftVerticalSpeedMs <= -0.5) 
                {
                    PointF p1 = new PointF(posVerticalSpeedX + 5, aircraftY + 2 + verticalSpeedY + 5);
                    PointF p2 = new PointF(posVerticalSpeedX + 0, aircraftY + 2 + verticalSpeedY + 0);

                    PointF p3 = new PointF(posVerticalSpeedX + 0, aircraftY + 2 + 0);
                    PointF p4 = new PointF(posVerticalSpeedX + 10, aircraftY + 2 + 0);

                    PointF p5 = new PointF(posVerticalSpeedX + 10, aircraftY + 2 + verticalSpeedY + 0);

                    g.DrawPolygon(pen, new[] { p1, p2, p3, p4, p5 });
                    g.FillPolygon(Brushes.White, new[] { p1, p2, p3, p4, p5 }); // optional ausfüllen
                }

            }

        }

    }
}

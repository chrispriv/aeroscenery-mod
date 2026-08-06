using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Printing;

namespace AeroScenery.FlightPathVisualizer.Instruments
{
    public class HudOverlayControl : UserControl
    {
        public double Pitch { get; set; } // In Grad, z. B. -10 bis +10
        public double Roll { get; set; }  // In Grad, z. B. -45 bis +45

        public double AltitudeFt { get; set; }
        public double SpeedKt { get; set; }
        public double VerticalSpeedFtM { get; set; }
        public double HeadingDeg { get; set; }
        public double ElevationFt { get; set; }

        public HudOverlayControl()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint|
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int width = Width;
            int height = Height;
            float centerX = width / 2f;
            float centerY = height / 2f;

            //float pitchOffset = (float)(-Pitch * 7.0); 
            float pitchOffset = (float)(Pitch * 7.0); // Pitch-Skalierung (Pixel pro Grad): 7 statt 5 
            float rollAngle = (float)(-Roll); // Gegenuhrzeigersinn

            using (var pen = new Pen(Color.White, 2)) // Color.White statt Color.LimeGreen
            {
                // ==== 1. Pitchleiter mit Rollrotation ====
                
                g.TranslateTransform(centerX, centerY);
                g.RotateTransform(rollAngle);
                g.TranslateTransform(0, pitchOffset);

                // Braune Fläche als künstlicher Horizont-Untergrund
                using (Brush brownBrush = new SolidBrush(Color.FromArgb(200, 160, 82, 45)))
                {
                    //var rectBrown = new RectangleF(-Width, 0, Width * 2, Height);
                    var rectBrown = new RectangleF(-Width, 0, Width * 2, Height * 4);
                    g.FillRectangle(brownBrush, rectBrown);
                }

                for (int pitch = -85; pitch <= 85; pitch += 5) // +/- 85 statt 45
                {
                    if (pitch == 0) continue; // Mitte wird später gezeichnet

                    float y = -pitch * 7.0f; // vertikale Position: 7 statt 5

                    int lineLength = (pitch % 10 == 0) ? 60 : 40;
                    int notchSize = 10;

                    // Hauptlinie
                    g.DrawLine(pen, -lineLength, y, lineLength, y);

                    // Knickmarkierung: kleiner Winkel links/rechts
                    if (pitch > 0)
                    {
                        // Steigflug: Linien nach unten
                        g.DrawLine(pen, -lineLength, y, -lineLength + notchSize, y + notchSize);
                        g.DrawLine(pen, lineLength, y, lineLength - notchSize, y + notchSize);
                    }
                    else
                    {
                        // Sinkflug: Linien nach oben
                        g.DrawLine(pen, -lineLength, y, -lineLength + notchSize, y - notchSize);
                        g.DrawLine(pen, lineLength, y, lineLength - notchSize, y - notchSize);
                    }

                    // Pitch-Wert
                    //string label = $"{Math.Abs(pitch)}°";
                    string label = $"{pitch}°"; // Minus ebenfalls anzeigen im Sinkflug 
                    var font = new Font("Arial", 10);
                    var size = g.MeasureString(label, font);
                    g.DrawString(label, font, Brushes.White, -lineLength - size.Width - 5, y - size.Height / 2); // Brushes.White statt Brushes.LimeGreen
                    g.DrawString(label, font, Brushes.White, lineLength + 5, y - size.Height / 2);
                }

                // ==== 2. Horizontlinie ====
                g.DrawLine(pen, -100, 0, 100, 0);

                g.ResetTransform();


                // ROLLKREIS (oben zentriert)
                float rollRadius = 100f;
                float rollTopMargin = 24; // Abstand vom oberen Rand: 24 statt 60 genommen
                PointF centerTop = new PointF(centerX, rollTopMargin + rollRadius);

                using (var rollPen = new Pen(Color.Black, 2)) // Color.Black statt Color.DarkGreen
                {
                    g.DrawArc(rollPen, centerTop.X - rollRadius, centerTop.Y - rollRadius, rollRadius * 2, rollRadius * 2, 180, 180);


                    // Markierungen bei typischen Bank-Winkeln
                    int[] bankAngles = { -90, -60, -45, -30, -20, -10, 0, 10, 20, 30, 45, 60, 90 };
                    foreach (int angle in bankAngles)
                    {
                        double rad = angle * Math.PI / 180;
                        float x1 = centerTop.X + (float)(rollRadius * Math.Sin(rad));
                        float y1 = centerTop.Y - (float)(rollRadius * Math.Cos(rad));
                        float len = (angle % 30 == 0) ? 10 : 6;

                        float x2 = centerTop.X + (float)((rollRadius - len) * Math.Sin(rad));
                        float y2 = centerTop.Y - (float)((rollRadius - len) * Math.Cos(rad));

                        g.DrawLine(rollPen, x1, y1, x2, y2);
                    }

                    // ... nach Zeichnung der Pitchlinien etc.
                    //g.ResetTransform(); // ⬅ Wichtig! Zurücksetzen der Rotation

                    // Marker-Winkel in Radiant
                    double rollRad = Roll * Math.PI / 180.0;

                    // Position auf dem Kreis (von innen nach außen zeigend)
                    float markerInnerRadius = rollRadius - 20;
                    float markerOuterRadius = rollRadius - 5;

                    // Drei Punkte berechnen
                    PointF p1 = new PointF(
                        centerTop.X + (float)(markerOuterRadius * Math.Sin(rollRad)),
                        centerTop.Y - (float)(markerOuterRadius * Math.Cos(rollRad)));

                    PointF p2 = new PointF(
                        centerTop.X + (float)(markerInnerRadius * Math.Sin(rollRad + 0.05)),
                        centerTop.Y - (float)(markerInnerRadius * Math.Cos(rollRad + 0.05)));

                    PointF p3 = new PointF(
                        centerTop.X + (float)(markerInnerRadius * Math.Sin(rollRad - 0.05)),
                        centerTop.Y - (float)(markerInnerRadius * Math.Cos(rollRad - 0.05)));

                    // Rotes Dreieck zeichnen
                    using (var redBrush = new SolidBrush(Color.Red))
                    {
                        g.FillPolygon(redBrush, new[] { p1, p2, p3 });
                    }

                    // Roll-Skala-Beschriftung in Rot (außen am Halbkreis)
                    using (var font = new Font("Arial", 9, FontStyle.Bold))
                    using (var redBrush = new SolidBrush(Color.LightGray))
                    {
                        int[] rollMarks = new int[] { -90, -60, -45, -30, -20, -10, 0, 10, 20, 30, 45, 60, 90 };

                        float labelRadius = rollRadius + 12; // Etwas außerhalb des Halbkreises

                        foreach (int angle in rollMarks)
                        {
                            double rad = angle * Math.PI / 180.0;

                            // Position berechnen
                            float x = centerTop.X + (float)(labelRadius * Math.Sin(rad));
                            float y = centerTop.Y - (float)(labelRadius * Math.Cos(rad));

                            string label = angle.ToString();

                            // Textgröße zur Zentrierung
                            SizeF textSize = g.MeasureString(label, font);
                            g.DrawString(label, font, redBrush, x - textSize.Width / 2, y - textSize.Height / 2);
                        }
                    }



                    // ==== 3. Zentrales Flugzeugsymbol ====
                    float fuselageRadius = 6;
                    g.FillEllipse(Brushes.Black, centerX - fuselageRadius, centerY - fuselageRadius, fuselageRadius * 2, fuselageRadius * 2);
                    g.DrawLine(rollPen, centerX - 30, centerY, centerX + 30, centerY); // Flügel
                    g.DrawLine(rollPen, centerX, centerY - 15, centerX, centerY); // Heckleitwerk
                   
                }
                /*
                // === DIGITALE ANZEIGEN (verfeinerte Positionen) ===

                string FormatValue(double value, string unit = "") => $"{Math.Round(value)}{unit}";

                // Feinjustierte Randabstände
                int marginSide = 10;     // halbierter Abstand zu Rand
                int marginBottom = 8;
                int marginTop = 4;
                int spacingY = 38;
                int boxWidth = 80;
                int boxHeight = 22;

                // Links oben (SPD)
                int posXLeft = marginSide;
                int posYTop = marginTop;

                // Rechts oben (ALT, VS)
                int posXRight = Width - boxWidth - marginSide;
                int posYRightTop = marginTop;

                // Links unten (HDG)
                int posYBottom = Height - boxHeight - marginBottom;

                // Rechts unten (AGL)
                int posYRightBottom = Height - boxHeight - marginBottom;

                //using (var font = new Font("Consolas", 9.5f, FontStyle.Bold))
                using (var font = new Font("Segeo UI", 8f, FontStyle.Bold)) // gleiche Schriftart wie GUI
                using (var brush = new SolidBrush(Color.Black))
                using (var penBox = new Pen(Color.Black, 1))
                {
                    // SPEED (links oben)
                    g.DrawString("SPD", font, brush, posXLeft, posYTop);
                    var speedRect = new Rectangle(posXLeft, posYTop + 14, boxWidth, boxHeight);
                    g.DrawRectangle(penBox, speedRect);
                    g.DrawString(FormatValue(SpeedKt, " kt"), font, brush, speedRect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

                    // HDG (links unten)
                    g.DrawString("HDG", font, brush, posXLeft, posYBottom - 14);
                    var hdgRect = new Rectangle(posXLeft, posYBottom, boxWidth, boxHeight);
                    g.DrawRectangle(penBox, hdgRect);
                    g.DrawString(FormatValue(HeadingDeg, "°"), font, brush, hdgRect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

                    // ALTITUDE (rechts oben)
                    g.DrawString("ALT", font, brush, posXRight, posYRightTop);
                    var altRect = new Rectangle(posXRight, posYRightTop + 14, boxWidth, boxHeight);
                    g.DrawRectangle(penBox, altRect);
                    g.DrawString(FormatValue(AltitudeFt, " ft"), font, brush, altRect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

                    // VS (rechts unten)
                    g.DrawString("VS", font, brush, posXRight, posYRightBottom - 14);
                    var vsRect = new Rectangle(posXRight, posYRightBottom, boxWidth, boxHeight);
                    g.DrawRectangle(penBox, vsRect);
                    g.DrawString(FormatValue(VerticalSpeedFtM, " ft/m"), font, brush, vsRect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

                    // AGL (ober von VS), angezeigt nur wenn Elevation Data verfügbar
                    if (ElevationFt > -100)
                    {
                        g.DrawString("AGL", font, brush, posXRight, posYRightBottom  - 56);
                        var aglRect = new Rectangle(posXRight, posYRightBottom + 14 - 56, boxWidth, boxHeight);
                        g.DrawRectangle(penBox, aglRect);
                        g.DrawString(FormatValue(AltitudeFt - ElevationFt, " ft"), font, brush, aglRect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                    }
                }

                */

                // Formatierungsfunktion
                string FormatValue(double value, string unit = "") => $"{Math.Round(value)}{unit}";

                // Abstände & Maße
                int marginSide = 10;
                int marginBottom = 8;
                int marginTop = 4;
                // int spacingY = 38;
                int boxWidth = 80;
                int boxHeight = 22;

                // Positionen
                int posXLeft = marginSide;
                int posYTop = marginTop;
                int posXRight = Width - boxWidth - marginSide;
                int posYRightTop = marginTop;
                int posYBottom = Height - boxHeight - marginBottom;
                int posYRightBottom = Height - boxHeight - marginBottom;

                // Zeichenmittel
                using (var font = new Font("Segoe UI", 8f, FontStyle.Bold))
                using (var textBrush = new SolidBrush(Color.White))
                using (var bgBrush = new SolidBrush(Color.FromArgb(160, 80, 80, 80))) // halbtransparent grau
                using (var penBox = new Pen(Color.White, 1))
                {
                    // Zeichenmethode für Label + Rechteck
                    void DrawHudBox(string label, string value, int x, int y)
                    {
                        // Label
                        g.DrawString(label, font, textBrush, x, y);

                        // Rechteck mit Text darunter
                        var rect = new Rectangle(x, y + 14, boxWidth, boxHeight);
                        g.FillRectangle(bgBrush, rect);
                        g.DrawRectangle(penBox, rect);
                        g.DrawString(value, font, textBrush, rect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                    }

                    // SPEED (links oben)
                    DrawHudBox("SPD", FormatValue(SpeedKt, " kt"), posXLeft, posYTop);

                    // HDG (links unten)
                    DrawHudBox("HDG", FormatValue(HeadingDeg, "°"), posXLeft, posYBottom - 14);

                    // ALT (rechts oben)
                    DrawHudBox("ALT", FormatValue(AltitudeFt, " ft"), posXRight, posYRightTop);

                    // VS (rechts unten)
                    DrawHudBox("VS", FormatValue(VerticalSpeedFtM, " ft/m"), posXRight, posYRightBottom - 14);

                    // AGL (oberhalb VS), nur wenn verfügbar
                    if (ElevationFt > -100)
                    {
                        DrawHudBox("AGL", FormatValue(AltitudeFt - ElevationFt, " ft"), posXRight, posYRightBottom - 56);
                    }
                }



            }
        }

        /*
        protected override void OnPaint2(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var width = Width;
            var height = Height;
            
            // Fixe seitliche Marker links/rechts
            int margin = 20;

            // Horizontlinie (Pitch und Roll)
            float centerX = width / 2f;
            float centerY = height / 2f;

            // Pitch verschiebt Linie vertikal (max ±10 Grad → z. B. ±50 px)
            float pitchOffset = (float)(-Pitch * 3.0);  // z. B. ±50 Pixel

            // Roll dreht die Linie
            float rollAngle = (float)(-Roll); // Gegenuhrzeigersinn
       
            using (var pen = new Pen(Color.Black, 2))
            {
                // Zeichenbefehle hier drin
                g.DrawLine(pen, margin, 0, margin, height);
                g.DrawLine(pen, width - margin, 0, width - margin, height);

                g.TranslateTransform(centerX, centerY + pitchOffset);
                g.RotateTransform(rollAngle);
                g.DrawLine(pen, -100, 0, 100, 0);
                g.ResetTransform();
            }
        }
        */

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // HudOverlayControl
            // 
            this.Name = "HudOverlayControl";
            this.ResumeLayout(false);

        }
    }
}

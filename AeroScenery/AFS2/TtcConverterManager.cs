using AeroScenery.Controls;
using AeroScenery.UI;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AeroScenery.AFS2
{
    /// <summary>
    /// Runs the built-in converter over every .tmc in a stitched tiles directory.
    ///
    /// The conversion runs in this process. There is no external tool to launch, no GPU and no
    /// desktop session: it reports its own progress and either returns or throws.
    /// </summary>
    public class TtcConverterManager
    {
        private readonly ILog log = LogManager.GetLogger("AeroScenery");

        /// <param name="rawDirectory">Optional raw PNG folder. Existing files are overwritten, not deleted first.</param>
        /// <param name="maxThreads">Built-in converter workers. Null uses <see cref="TtcConverter.DefaultThreads"/>.</param>
        /// <param name="mobileDirectory">When writeEtc2 is set, ETC2 tiles go here (##-geoconvert-ttc-mobile).</param>
        public async Task<TtcConversionResult> ConvertAllAsync(string stitchedTilesDirectory,
            string ttcDirectory, MainForm mainForm, string rawDirectory = null, int? maxThreads = null,
            string mobileDirectory = null, bool writeDxt1 = true, bool writeEtc2 = false)
        {
            var total = new TtcConversionResult();

            if (!Directory.Exists(stitchedTilesDirectory))
            {
                var messageBox = new CustomMessageBox("No stiched images found for this grid square and this image detail (zoom) level.\nRun the 'Download Image Tiles' and 'Stitch Image Tiles' actions first.",
                    "AeroScenery",
                    MessageBoxIcon.Error);

                messageBox.ShowDialog();
                return total;
            }

            var tmcFiles = Directory.EnumerateFiles(stitchedTilesDirectory, "*.tmc").ToList();

            if (tmcFiles.Count == 0)
            {
                var messageBox = new CustomMessageBox("No TCM file was found for this grid square and this image detail (zoom) level.\nRun the 'Download Image Tiles', 'Stitch Image Tiles' and 'Generate AID / TMC Files' actions first.",
                    "AeroScenery",
                    MessageBoxIcon.Error);

                messageBox.ShowDialog();
                return total;
            }

            foreach (string tmcFilename in tmcFiles)
            {
                log.Info(String.Format("Converting {0}", tmcFilename));

                // Start the step clock on the UI thread *before* Task.Run. BeginInvoke would
                // queue behind tile-progress posts and the step would never look started.
                Action startStep = () => mainForm.UpdateChildTaskLabel("Running GeoConvert");
                if (mainForm.InvokeRequired)
                {
                    mainForm.Invoke(startStep);
                }
                else
                {
                    startStep();
                }

                var stopwatch = Stopwatch.StartNew();
                TtcConversionResult result;
                int lastLevel = 0;
                int lastDone = 0;
                int lastTotal = 0;
                bool reportProgress = true;

                try
                {
                    // One conversion stays on one thread for its whole life. The PNG decoder
                    // underneath is a WPF BitmapDecoder, which is bound to the thread that created
                    // it, so this must not be split across awaits.
                    int threads = maxThreads.GetValueOrDefault(TtcConverter.DefaultThreads());
                    if (threads < 1)
                    {
                        threads = 1;
                    }

                    IProgress<TtcConversionProgress> progress = null;
                    Action createProgress = () =>
                    {
                        DateTime lastUi = DateTime.MinValue;
                        progress = new Progress<TtcConversionProgress>(p =>
                        {
                            if (!reportProgress)
                            {
                                return;
                            }

                            lastLevel = p.Level;
                            lastDone = p.TilesDone;
                            lastTotal = p.TilesTotal;

                            bool lastTile = p.TilesTotal > 0 && p.TilesDone >= p.TilesTotal;
                            var now = DateTime.UtcNow;
                            if (!lastTile && lastUi != DateTime.MinValue && (now - lastUi).TotalMilliseconds < 250)
                            {
                                return;
                            }

                            lastUi = now;
                            mainForm.UpdateChildTaskProgress(String.Format(
                                "Converting Level {0} - {1} of {2} tiles",
                                p.Level, p.TilesDone, p.TilesTotal));
                        });
                    };

                    if (mainForm.InvokeRequired)
                    {
                        mainForm.Invoke(createProgress);
                    }
                    else
                    {
                        createProgress();
                    }

                    result = await Task.Run(() =>
                    {
                        var converter = new TtcConverter
                        {
                            MaxThreads = threads,
                            WriteDxt1 = writeDxt1,
                            WriteEtc2 = writeEtc2,
                            MobileDirectory = mobileDirectory
                        };
                        return converter.Convert(tmcFilename, ttcDirectory, progress);
                    });

                    reportProgress = false;
                }
                catch (Exception ex)
                {
                    log.Error(String.Format("Could not convert {0}", tmcFilename), ex);

                    var messageBox = new CustomMessageBox(String.Format("{0} could not be converted.\n\n{1}",
                            Path.GetFileName(tmcFilename), ex.Message),
                        "AeroScenery",
                        MessageBoxIcon.Error);

                    messageBox.ShowDialog();
                    continue;
                }

                if (lastTotal > 0)
                {
                    mainForm.UpdateChildTaskProgress(String.Format(
                        "Finished Level {0} - {1} of {2} tiles",
                        lastLevel, lastDone, lastTotal));
                }
                else
                {
                    mainForm.UpdateChildTaskProgress("Finished conversion");
                }

                stopwatch.Stop();

                total.FilesWritten.AddRange(result.FilesWritten);
                total.BytesWritten += result.BytesWritten;
                total.SourceRestarts += result.SourceRestarts;
                total.Elapsed += result.Elapsed;

                log.Info(String.Format("Converted {0} in {1:hh\\:mm\\:ss}, {2} files, {3:n1} MB in {4}",
                    Path.GetFileName(tmcFilename),
                    stopwatch.Elapsed,
                    result.FilesWritten.Count,
                    result.BytesWritten / (1024.0 * 1024.0),
                    ttcDirectory));

                if (result.SourceRestarts > 0)
                {
                    // Not a failure, but it means the source was re-decoded from the top one or
                    // more times, which is the difference between one pass and several.
                    log.WarnFormat("{0} source decoder restarts while converting {1}",
                        result.SourceRestarts, Path.GetFileName(tmcFilename));
                }

                if (result.FilesWritten.Count == 0)
                {
                    log.WarnFormat("{0} produced no tiles - no source image covers its regions",
                        Path.GetFileName(tmcFilename));
                }
            }

            return total;
        }
    }
}

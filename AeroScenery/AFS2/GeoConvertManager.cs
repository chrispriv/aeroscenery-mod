using AeroScenery.Controls;
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
    /// A GeoConvert process that has been started and not waited on yet.
    /// </summary>
    public class GeoConvertRun
    {
        public Process Process { get; set; }

        public string TMCFilename { get; set; }

        public string TTCDirectory { get; set; }

        public Stopwatch Stopwatch { get; set; }
    }

    //#MOD_k
    // The public class GeoConvertManager has been completely replaced; it now runs asynchronously, and the GeoConvert process is stopped based on monitoring of CPU utilisation and memory usage
    public class GeoConvertManager
    {
        private readonly ILog log = LogManager.GetLogger("AeroScenery");

        // --------------------------------------------------------------------
        // Process monitoring settings
        // --------------------------------------------------------------------

        // Memory and CPU are sampled at this interval.
        private const int ProcessMeasurementIntervalMilliseconds = 500;

        // Both the memory and CPU completion conditions must remain true
        // for this amount of time before GeoConvert is terminated.
        //private const int CompletionInactivitySeconds = 2;
        private const int CompletionInactivitySeconds = 60;

        // Small memory fluctuations are ignored.
        // This prevents normal Windows memory management activity from
        // resetting the memory inactivity timer.
        //private const long MemoryChangeToleranceBytes = 1024 * 1024; // 1 MB
        private const long MemoryChangeToleranceBytes = 1024; // 1 KB

        // The CPU usage must fall by at least this percentage relative to
        // the highest CPU usage observed for this GeoConvert process.
        //
        // Example:
        //
        // Peak CPU = 80 %
        // CPU drop threshold = 40 %
        // Completion threshold = 48 %
        //
        // Therefore the CPU must fall from 80 % to 48 % or lower.
        //private const double CpuDropThresholdPercent = 40.0;
        private const double CpuDropThresholdPercent = 40.0;


        /// <summary>
        /// Starts GeoConvert for every TMC file in the stitched tiles directory.
        ///
        /// If useWrapper is true, runs are processed sequentially:
        /// one process is started, monitored and terminated before the next
        /// process is started.
        ///
        /// If useWrapper is false, all processes are started first and then
        /// handed back to the caller so that they can run in parallel.
        ///
        /// The external GeoConvert wrapper is no longer required. The
        /// useWrapper parameter is retained for now as a compatibility switch
        /// for the execution mode and can be renamed later.
        /// </summary>
        public async Task<List<GeoConvertRun>> RunGeoConvertAsync(
            string stitchedTilesDirectory,
            string ttcDirectory,
            MainForm mainForm,
            bool useWrapper)
        {
            var pendingRuns = new List<GeoConvertRun>();

            if (!Directory.Exists(stitchedTilesDirectory))
            {
                // Tile download directory does not exist.
                var messageBox = new CustomMessageBox(
                    "No stitched images found for this grid square and this image detail (zoom) level.\n" +
                    "Run the 'Download Image Tiles' and 'Stitch Image Tiles' actions first.",
                    "AeroScenery",
                    MessageBoxIcon.Error);

                messageBox.ShowDialog();

                return pendingRuns;
            }

            var tmcFiles = Directory
                .EnumerateFiles(stitchedTilesDirectory, "*.tmc")
                .ToList();

            if (tmcFiles.Count == 0)
            {
                // No TMC file was found for this grid square.
                var messageBox = new CustomMessageBox(
                    "No TCM file was found for this grid square and this image detail (zoom) level.\n" +
                    "Run the 'Download Image Tiles', 'Stitch Image Tiles' and 'Generate AID / TMC Files' actions first.",
                    "AeroScenery",
                    MessageBoxIcon.Error);

                messageBox.ShowDialog();

                return pendingRuns;
            }

            if (tmcFiles.Count > 1 && useWrapper)
            {
                // The old wrapper provided sequential processing.
                // We now implement the same behaviour directly in this class.
                log.WarnFormat(
                    "{0} TMC files to convert and sequential GeoConvert processing is enabled",
                    tmcFiles.Count);
            }

            foreach (string tmcFilename in tmcFiles)
            {
                string geoconvertPath = String.Format(
                    "{0}aerofly_fs_2_geoconvert",
                    AeroSceneryManager.Instance.Settings.AFS2SDKDirectory);

                string geoconvertFilename = String.Format(
                    "{0}\\aerofly_fs_2_geoconvert.exe",
                    geoconvertPath);

                if (!File.Exists(geoconvertFilename))
                {
                    log.Error(String.Format(
                        "Could not find GeoConvert in {0}",
                        geoconvertFilename));

                    var messageBox = new CustomMessageBox(
                        String.Format(
                            "Could not find GeoConvert in {0} \n\n" +
                            "Please check the path of the Aerofly FS2 SDK containing the GeoConvert App under Settings.",
                            geoconvertFilename),
                        "AeroScenery",
                        MessageBoxIcon.Error);

                    messageBox.ShowDialog();

                    continue;
                }

                // GeoConvert itself is now always started directly.
                // The external GeoConvertWrapper is no longer required.
                string processFilename = geoconvertFilename;
                string processArguments = tmcFilename;

                mainForm.UpdateChildTaskLabel("Running GeoConvert");

                log.Info(String.Format(
                    "Running GeoConvert on {0}",
                    tmcFilename));

                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = processFilename,
                        Arguments = processArguments,

                        // Keep the console window visible, as before.
                        UseShellExecute = true,
                        RedirectStandardOutput = false,
                        CreateNoWindow = false,

                        // GeoConvert expects to run from its SDK directory.
                        WorkingDirectory = geoconvertPath
                    }
                };

                var geoConvertRun = new GeoConvertRun
                {
                    Process = proc,
                    TMCFilename = tmcFilename,
                    TTCDirectory = ttcDirectory,
                    Stopwatch = Stopwatch.StartNew()
                };

                try
                {
                    proc.Start();
                }
                catch (Exception ex)
                {
                    log.Error(
                        String.Format(
                            "Could not start GeoConvert for {0}",
                            tmcFilename),
                        ex);

                    var messageBox = new CustomMessageBox(
                        String.Format(
                            "GeoConvert could not be started.\n\n{0}",
                            ex.Message),
                        "AeroScenery",
                        MessageBoxIcon.Error);

                    messageBox.ShowDialog();

                    proc.Dispose();

                    continue;
                }

                if (useWrapper)
                {
                    // Sequential mode:
                    // Wait for this GeoConvert process to finish its actual
                    // work before starting the next TMC file.
                    //
                    // The process is terminated automatically once both
                    // memory and CPU indicate that the conversion has finished.
                    await this.WaitForRunsAsync(
                        new List<GeoConvertRun> { geoConvertRun },
                        mainForm);
                }
                else
                {
                    // Parallel mode:
                    // Keep the process running and start the next TMC file.
                    // All pending processes are waited for later.
                    pendingRuns.Add(geoConvertRun);
                }
            }

            return pendingRuns;
        }


        /// <summary>
        /// Waits for all supplied GeoConvert runs to finish.
        ///
        /// In sequential mode this method normally receives one run at a time.
        /// In parallel mode all running processes are monitored simultaneously.
        ///
        /// The method returns only after every supplied GeoConvert process has
        /// completed its actual work and has been terminated.
        /// </summary>
        public async Task WaitForRunsAsync(
            List<GeoConvertRun> geoConvertRuns,
            MainForm mainForm)
        {
            if (geoConvertRuns.Count == 0)
            {
                return;
            }

            mainForm.UpdateChildTaskLabel("Waiting For GeoConvert");

            // Every GeoConvert process gets its own monitoring task.
            // Task.WhenAll() ensures that this method only returns after all
            // processes have completed.
            var waitTasks = geoConvertRuns
                .Select(geoConvertRun =>
                    this.WaitForGeoConvertRunAsync(geoConvertRun))
                .ToList();

            await Task.WhenAll(waitTasks);
        }


        /// <summary>
        /// Monitors one GeoConvert process until it either exits normally or
        /// its memory and CPU usage indicate that the conversion has finished.
        ///
        /// Memory alone is deliberately NOT sufficient to terminate GeoConvert.
        ///
        /// Large GeoConvert jobs can keep their memory footprint unchanged for
        /// several minutes while still consuming substantial CPU resources.
        ///
        /// The process is therefore considered finished only when:
        ///
        /// 1. Memory usage has remained stable within the configured tolerance.
        /// 2. CPU usage has dropped sufficiently below the highest CPU usage
        ///    observed for this process.
        /// 3. Both conditions have remained true for the configured period.
        /// </summary>
        private async Task WaitForGeoConvertRunAsync(
            GeoConvertRun geoConvertRun)
        {
            var proc = geoConvertRun.Process;

            bool terminatedByApplication = false;

            try
            {
                if (!proc.HasExited)
                {
                    proc.Refresh();

                    // --------------------------------------------------------
                    // Initial memory measurement.
                    // --------------------------------------------------------

                    long previousMemory = proc.WorkingSet64;

                    // --------------------------------------------------------
                    // Initial CPU measurement.
                    // --------------------------------------------------------
                    //
                    // TotalProcessorTime represents the actual CPU time
                    // consumed by this particular GeoConvert process.
                    //
                    // We compare two consecutive measurements to calculate
                    // the CPU utilisation during the sampling interval.
                    //
                    // These variables are local to this monitoring task.
                    // Therefore multiple GeoConvert processes can be monitored
                    // simultaneously without influencing each other.
                    // --------------------------------------------------------

                    TimeSpan previousCpuTime =
                        proc.TotalProcessorTime;

                    DateTime previousCpuMeasurement =
                        DateTime.UtcNow;

                    // Highest CPU utilisation observed for this process.
                    //
                    // This value is deliberately retained for the complete
                    // lifetime of the process. It therefore represents the
                    // actual workload level reached by this GeoConvert run.
                    double peakCpuUsage = 0.0;

                    // Timestamp when memory first became stable.
                    DateTime? memoryStableSince = null;

                    // Timestamp when CPU first dropped sufficiently below
                    // the observed CPU peak.
                    DateTime? cpuDropSince = null;


                    while (!proc.HasExited)
                    {
                        // Do not block the UI thread while monitoring the process.
                        await Task.Delay(
                            ProcessMeasurementIntervalMilliseconds);

                        if (proc.HasExited)
                        {
                            break;
                        }

                        proc.Refresh();


                        // ====================================================
                        // MEMORY MEASUREMENT
                        // ====================================================

                        long currentMemory = proc.WorkingSet64;

                        long memoryDifference =
                            Math.Abs(
                                currentMemory -
                                previousMemory);

                        previousMemory = currentMemory;

                        bool memoryStable =
                            memoryDifference <=
                            MemoryChangeToleranceBytes;


                        // ====================================================
                        // CPU MEASUREMENT
                        // ====================================================

                        TimeSpan currentCpuTime =
                            proc.TotalProcessorTime;

                        DateTime currentCpuMeasurement =
                            DateTime.UtcNow;

                        double elapsedMilliseconds =
                            (
                                currentCpuMeasurement -
                                previousCpuMeasurement
                            ).TotalMilliseconds;

                        double cpuMilliseconds =
                            (
                                currentCpuTime -
                                previousCpuTime
                            ).TotalMilliseconds;

                        double cpuUsage = 0.0;

                        if (elapsedMilliseconds > 0)
                        {
                            // Convert process CPU time into a percentage
                            // comparable with the total CPU percentage shown
                            // by Windows Task Manager.
                            //
                            // Dividing by Environment.ProcessorCount
                            // normalises the value to a 0-100 % scale for the
                            // complete machine.
                            cpuUsage =
                                (
                                    cpuMilliseconds /
                                    elapsedMilliseconds
                                ) *
                                (
                                    100.0 /
                                    Environment.ProcessorCount
                                );
                        }

                        // Protect against very small timing differences
                        // resulting in an unexpected negative value.
                        if (cpuUsage < 0)
                        {
                            cpuUsage = 0;
                        }

                        // Store the current values for the next measurement.
                        previousCpuTime = currentCpuTime;
                        previousCpuMeasurement =
                            currentCpuMeasurement;


                        // ====================================================
                        // CPU PEAK
                        // ====================================================

                        if (cpuUsage > peakCpuUsage)
                        {
                            peakCpuUsage = cpuUsage;

                            /*
                            log.Debug(String.Format(
                                "GeoConvert {0}: new CPU peak {1:0.0} %.",
                                Path.GetFileName(
                                    geoConvertRun.TMCFilename),
                                peakCpuUsage));
                            */
                        }


                        // ====================================================
                        // MEMORY STABILITY TIMER
                        // ====================================================

                        if (memoryStable)
                        {
                            // Memory is currently stable.
                            if (!memoryStableSince.HasValue)
                            {
                                memoryStableSince =
                                    DateTime.UtcNow;

                                /*
                                log.Debug(String.Format(
                                    "GeoConvert {0}: memory became stable at {1:N0} MB. " +
                                    "Current CPU: {2:0.0} %, CPU peak: {3:0.0} %.",
                                    Path.GetFileName(
                                        geoConvertRun.TMCFilename),
                                    currentMemory / 1024.0 / 1024.0,
                                    cpuUsage,
                                    peakCpuUsage));
                                */
                            }
                        }
                        else
                        {
                            // Memory changed again.
                            //
                            // The memory stability timer must therefore be
                            // restarted.
                            if (memoryStableSince.HasValue)
                            {
                                /*
                                log.Debug(String.Format(
                                    "GeoConvert {0}: memory became active again at {1:N0} MB. " +
                                    "Current CPU: {2:0.0} %.",
                                    Path.GetFileName(
                                        geoConvertRun.TMCFilename),
                                    currentMemory / 1024.0 / 1024.0,
                                    cpuUsage));
                                */
                            }

                            memoryStableSince = null;
                        }


                        // ====================================================
                        // CPU DROP TIMER
                        // ====================================================

                        bool cpuHasDropped =
                            peakCpuUsage > 0 &&
                            cpuUsage <=
                            peakCpuUsage *
                            (
                                1.0 -
                                CpuDropThresholdPercent / 100.0
                            );

                        if (cpuHasDropped)
                        {
                            // CPU has fallen sufficiently below the peak.
                            //
                            // Start the CPU stability timer if this is the
                            // first measurement fulfilling the condition.
                            if (!cpuDropSince.HasValue)
                            {
                                cpuDropSince =
                                    DateTime.UtcNow;

                                /*
                                log.Debug(String.Format(
                                    "GeoConvert {0}: CPU dropped to {1:0.0} % " +
                                    "from peak {2:0.0} %. " +
                                    "CPU completion threshold is {3:0.0} %.",
                                    Path.GetFileName(
                                        geoConvertRun.TMCFilename),
                                    cpuUsage,
                                    peakCpuUsage,
                                    peakCpuUsage *
                                    (
                                        1.0 -
                                        CpuDropThresholdPercent / 100.0
                                    )));
                                */
                            }
                        }
                        else
                        {
                            // CPU has risen again above the completion
                            // threshold.
                            //
                            // The CPU stability timer must therefore be
                            // restarted.
                            if (cpuDropSince.HasValue)
                            {
                                /*
                                log.Debug(String.Format(
                                    "GeoConvert {0}: CPU became active again at {1:0.0} %. " +
                                    "Peak remains {2:0.0} %.",
                                    Path.GetFileName(
                                        geoConvertRun.TMCFilename),
                                    cpuUsage,
                                    peakCpuUsage));
                                */
                            }

                            cpuDropSince = null;
                        }


                        // ====================================================
                        // COMPLETION TEST
                        // ====================================================

                        bool memoryConditionSatisfied =
                            memoryStableSince.HasValue &&
                            (
                                DateTime.UtcNow -
                                memoryStableSince.Value
                            ).TotalSeconds >=
                            CompletionInactivitySeconds;

                        bool cpuConditionSatisfied =
                            cpuDropSince.HasValue &&
                            (
                                DateTime.UtcNow -
                                cpuDropSince.Value
                            ).TotalSeconds >=
                            CompletionInactivitySeconds;


                        // Both conditions must be satisfied.
                        //
                        // This is the important difference from the previous
                        // memory-only implementation:
                        //
                        // Constant memory + high CPU
                        //     -> GeoConvert is still working.
                        //
                        // Constant memory + CPU drop
                        //     -> GeoConvert is considered finished, but only
                        //        after both conditions remained true long
                        //        enough.
                        if (memoryConditionSatisfied &&
                            cpuConditionSatisfied)
                        {
                            log.Info(String.Format(
                                "GeoConvert {0}: memory remained stable for {1} seconds at {2:N0} MB " +
                                "and CPU remained below the completion threshold for {1} seconds " +
                                "at {3:0.0} % (peak {4:0.0} %). " +
                                "Terminating process {5}.",
                                Path.GetFileName(
                                    geoConvertRun.TMCFilename),
                                CompletionInactivitySeconds,
                                currentMemory / 1024.0 / 1024.0,
                                cpuUsage,
                                peakCpuUsage,
                                proc.Id));

                            try
                            {
                                if (!proc.HasExited)
                                {
                                    proc.Kill();

                                    terminatedByApplication = true;
                                }
                            }
                            catch (InvalidOperationException)
                            {
                                // The process exited between the
                                // HasExited check and Kill().
                            }

                            break;
                        }
                    }
                }


                // Wait until Windows has actually finished terminating the
                // process.
                //
                // This also works with older .NET Framework versions where
                // Process.WaitForExitAsync() is not available.
                await Task.Run(() => proc.WaitForExit());

                geoConvertRun.Stopwatch.Stop();


                // ============================================================
                // RESULT INFORMATION
                // ============================================================

                var ttcFileCount =
                    Directory.Exists(
                        geoConvertRun.TTCDirectory)
                        ? Directory
                            .EnumerateFiles(
                                geoConvertRun.TTCDirectory,
                                "*.ttc")
                            .Count()
                        : 0;

                var message = String.Format(
                    "GeoConvert finished {0} in {1:hh\\:mm\\:ss} with exit code {2}, {3} ttc files in {4}",
                    Path.GetFileName(
                        geoConvertRun.TMCFilename),
                    geoConvertRun.Stopwatch.Elapsed,
                    proc.ExitCode,
                    ttcFileCount,
                    geoConvertRun.TTCDirectory);


                if (terminatedByApplication)
                {
                    // Exit code is not meaningful here because the process
                    // was intentionally terminated by our application.
                    //
                    // The number of generated TTC files is a more useful
                    // indicator of whether GeoConvert actually completed.

                    if (ttcFileCount > 0)
                    {
                        log.Info(String.Format(
                            "GeoConvert completed and was terminated after " +
                            "stable memory usage and CPU drop. " +
                            "{0} ttc files in {1}",
                            ttcFileCount,
                            geoConvertRun.TTCDirectory));
                    }
                    else
                    {
                        log.Warn(String.Format(
                            "GeoConvert was terminated after stable memory " +
                            "usage and CPU drop, but no ttc files were found " +
                            "in {0}",
                            geoConvertRun.TTCDirectory));
                    }
                }
                else
                {
                    // This branch is used when GeoConvert exits by itself
                    // before our application terminates it.

                    if (proc.ExitCode == 0 &&
                        ttcFileCount > 0)
                    {
                        log.Info(message);
                    }
                    else
                    {
                        log.Warn(message);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(
                    String.Format(
                        "Error while monitoring GeoConvert process for {0}",
                        geoConvertRun.TMCFilename),
                    ex);
            }
            finally
            {
                proc.Dispose();
            }
        }
    }



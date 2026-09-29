using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace AeroScenery.AFS2
{
    public class TtcConversionProgress
    {
        public int Level { get; set; }
        public int TilesDone { get; set; }
        public int TilesTotal { get; set; }
        public string CurrentFile { get; set; }
    }

    public class TtcConversionResult
    {
        public List<string> FilesWritten { get; set; }
        public long BytesWritten { get; set; }
        public TimeSpan Elapsed { get; set; }

        /// <summary>
        /// How many times a decoder had to start over. Should be 0: it means something asked for
        /// source rows it had already gone past, and paid a full re-inflate of the PNG for them.
        /// </summary>
        public int SourceRestarts { get; set; }

        public TtcConversionResult()
        {
            FilesWritten = new List<string>();
        }
    }

    /// <summary>
    /// Converts a .tmc into .ttc tiles, in process, with no GeoConvert and no GPU.
    ///
    /// The shape of this is dictated by memory. The reference implementation in
    /// tools/ttc/convert_tmc.py samples the deepest level, keeps the whole level in RAM, and
    /// derives each shallower level by halving it - which is the right algorithm and the wrong
    /// storage: a full level-9 square would need about 13 GB. So the levels here cascade instead
    /// of accumulating. A tile row is halved into its parent's row as soon as it is finished, and
    /// a parent row is emitted as soon as both of its child rows have arrived, so at most one row
    /// of tiles per level is ever live.
    ///
    /// Everything moves north to south, which is what lets the source be streamed: the band of
    /// source rows a tile row needs only ever advances. Grid y counts northward, so north to south
    /// means DESCENDING ty, and that ordering is load-bearing rather than cosmetic.
    ///
    /// One deliberate difference from the reference. When a shallow tile has no children, the
    /// reference resamples it from the source; here the deeper level's work range is widened up
    /// front so that every parent has all four children, and halving is always used. Two reasons:
    /// resampling a shallow tile mid-pass would seek backwards in the source and cost a full
    /// re-inflate, and halving is a box filter where sampling is nearest neighbour, so it is the
    /// better image anyway. On inputs whose levels nest - which is what AeroScenery writes - the
    /// two produce identical output, so cross_sample.py still validates this end to end.
    /// </summary>
    public class TtcConverter
    {
        private const int TileSize = TtcTileWriter.TileSize;

        /// <summary>
        /// How many threads the BC1 encoder may use. 1 is serial, 0 or less means as many as the
        /// machine has.
        ///
        /// A setting rather than a constant on purpose. Saturating the machine is not always what
        /// the user wants: being able to keep working while a square builds can matter more than
        /// finishing it soonest, and only the person at the keyboard knows which.
        /// </summary>
        public int MaxThreads { get; set; }

        /// <summary>
        /// Whether a pure black source pixel counts as no data. See TileSampler.Sample.
        ///
        /// Off by default, because it changes output and the Python reference does not do it.
        /// </summary>
        public bool BlackIsMissing { get; set; }

        /// <summary>
        /// Where photoscenery stops at the sea, or null to keep everything a source reaches.
        ///
        /// Null by default, so the shipped behaviour and the byte-for-byte cross-check against the
        /// Python reference are both untouched. The reference knows nothing about a coastline.
        /// </summary>
        public Coastline Coast { get; set; }

        /// <summary>Texel size of the rasterised coastline, in km. See CoastlineField.</summary>
        public double CoastTexelKm { get; set; }

        /// <summary>
        /// A correction for the haze and the cloud Bing carries over water, or null to take the
        /// source as it comes. See WaterFixField, and tools/ttc/water_fix.py which measures it.
        ///
        /// Null by default, for the same reason Coast is: it changes output, and the Python
        /// reference does not know about it.
        /// </summary>
        public WaterFixField Water { get; set; }

        public bool WriteDxt1 { get; set; }
        public bool WriteEtc2 { get; set; }
        public string MobileDirectory { get; set; }

        public TtcConverter()
        {
            MaxThreads = 1;
            CoastTexelKm = CoastlineField.DefaultTexelKm;
            WriteDxt1 = true;
            WriteEtc2 = false;
        }

        /// <summary>Half the logical processors, which leaves the machine usable.</summary>
        public static int DefaultThreads()
        {
            return Math.Max(1, Environment.ProcessorCount / 2);
        }

        private class LevelBuffer
        {
            public int Level;

            // What gets computed, which may be wider than what gets written so that every parent
            // has four children.
            public int WorkX0, WorkX1;

            // What the .tmc actually asked for. Only these are written.
            public int OutX0, OutX1, OutY0, OutY1;

            public bool WantMask;

            // The parent row currently being assembled, indexed by tileX - WorkX0.
            public byte[][] Rgb;
            public bool[][] Covered;
            public bool[] Touched;
            public int PendingTy = Int32.MinValue;

            public int Width { get { return WorkX1 - WorkX0; } }
        }

        public TtcConversionResult Convert(string tmcPath, string outputDirectory,
            IProgress<TtcConversionProgress> progress = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = new TtcConversionResult();
            var stopwatch = Stopwatch.StartNew();

            TmcDocument doc = TmcReader.Parse(tmcPath);

            string sourceFolder = doc.FolderSourceFiles;
            if (String.IsNullOrEmpty(sourceFolder) || !Directory.Exists(sourceFolder))
            {
                sourceFolder = Path.GetDirectoryName(Path.GetFullPath(tmcPath));
            }

            if (String.IsNullOrEmpty(outputDirectory))
            {
                outputDirectory = doc.FolderDestinationTtc;
            }
            Directory.CreateDirectory(outputDirectory);

            string rawDirectory = null;
            if (doc.WriteRawFiles && !String.IsNullOrEmpty(doc.FolderDestinationRaw))
            {
                rawDirectory = doc.FolderDestinationRaw;
                Directory.CreateDirectory(rawDirectory);
            }

            var regions = new List<TmcRegion>(doc.Regions);
            regions.Sort((a, b) => a.Level.CompareTo(b.Level));

            var sources = SourceImage.OpenFolder(sourceFolder);
            if (sources.Count == 0)
            {
                throw new InvalidDataException("no .aid files in " + sourceFolder);
            }

            try
            {
                var levels = BuildLevels(regions);
                int deepest = levels[levels.Count - 1].Level;
                int shallowest = levels[0].Level;

                var byLevel = new Dictionary<int, LevelBuffer>();
                foreach (var lb in levels)
                {
                    byLevel[lb.Level] = lb;
                }

                LevelBuffer deep = byLevel[deepest];
                int deepY0, deepY1;
                DeepRowRange(levels, out deepY0, out deepY1);

                // The cut, rasterised once for the whole square, over the box CoastFieldBox gives.
                CoastlineField coastField = null;
                if (Coast != null && !Coast.IsEmpty)
                {
                    double west, east, south, north;
                    CoastFieldBox(levels, out west, out east, out south, out north);
                    coastField = CoastlineField.Build(Coast, west, east, south, north,
                        CoastTexelKm);
                }

                var progressState = new TtcConversionProgress
                {
                    Level = deepest,
                    TilesTotal = deep.Width * (deepY1 - deepY0)
                };

                var rgb = new byte[TileSize * TileSize * 3];
                var covered = new bool[TileSize * TileSize];

                // North to south, which for this grid means descending ty.
                for (int ty = deepY1 - 1; ty >= deepY0; ty--)
                {
                    for (int tx = deep.WorkX0; tx < deep.WorkX1; tx++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        TileSampler.Sample(sources, deepest, tx, ty, rgb, covered, TileSize,
                            true, BlackIsMissing, coastField, Water);
                        EmitTile(byLevel, shallowest, deepest, tx, ty, rgb, covered,
                            outputDirectory, result, MaxThreads, rawDirectory,
                            MobileDirectory, WriteDxt1, WriteEtc2);

                        progressState.TilesDone++;
                        if (progress != null)
                        {
                            progressState.Level = deepest;
                            progressState.CurrentFile = TtcTileName.ForTile(deepest, tx, ty);
                            progress.Report(progressState);
                        }
                    }
                }

                // Whatever is still half-assembled, from the deepest parent level upwards.
                for (int lv = deepest - 1; lv >= shallowest; lv--)
                {
                    LevelBuffer lb;
                    if (byLevel.TryGetValue(lv, out lb))
                    {
                        FlushRow(byLevel, shallowest, lb, outputDirectory, result, MaxThreads, rawDirectory,
                            MobileDirectory, WriteDxt1, WriteEtc2);
                    }
                }

                foreach (var s in sources)
                {
                    result.SourceRestarts += s.Restarts;
                }
            }
            finally
            {
                foreach (var s in sources)
                {
                    s.Dispose();
                }
            }

            stopwatch.Stop();
            result.Elapsed = stopwatch.Elapsed;
            return result;
        }

        /// <summary>
        /// Works out, per level, what to write and what to compute.
        ///
        /// A level's work range is widened to hold every child its parent level needs, walking
        /// from the shallowest down, so the widening propagates all the way to the deepest level -
        /// which is the one actually sampled from the source.
        /// </summary>
        private static List<LevelBuffer> BuildLevels(List<TmcRegion> regions)
        {
            var levels = new List<LevelBuffer>();

            foreach (var r in regions)
            {
                int x0, x1, y0, y1;
                AFS2World.TileRange(r.West, r.East, r.South, r.North, r.Level,
                    out x0, out x1, out y0, out y1);

                levels.Add(new LevelBuffer
                {
                    Level = r.Level,
                    OutX0 = x0, OutX1 = x1, OutY0 = y0, OutY1 = y1,
                    WorkX0 = x0, WorkX1 = x1,
                    WantMask = r.WriteImagesWithMask
                });
            }

            for (int i = 1; i < levels.Count; i++)
            {
                LevelBuffer parent = levels[i - 1];
                LevelBuffer child = levels[i];

                // Only the immediately deeper level can be halved into this one; a gap in levels
                // means the shallower one has to stand on its own, and widening would be a lie.
                if (child.Level != parent.Level + 1)
                {
                    continue;
                }

                child.WorkX0 = Math.Min(child.WorkX0, parent.WorkX0 * 2);
                child.WorkX1 = Math.Max(child.WorkX1, parent.WorkX1 * 2);
            }

            foreach (var lb in levels)
            {
                lb.Rgb = new byte[lb.Width][];
                lb.Covered = new bool[lb.Width][];
                lb.Touched = new bool[lb.Width];
            }

            return levels;
        }

        /// <summary>The deepest level's row range, widened the same way the columns were.</summary>
        private static void DeepRowRange(List<LevelBuffer> levels, out int y0, out int y1)
        {
            y0 = levels[0].OutY0;
            y1 = levels[0].OutY1;

            for (int i = 1; i < levels.Count; i++)
            {
                if (levels[i].Level == levels[i - 1].Level + 1)
                {
                    y0 = Math.Min(levels[i].OutY0, y0 * 2);
                    y1 = Math.Max(levels[i].OutY1, y1 * 2);
                }
                else
                {
                    y0 = levels[i].OutY0;
                    y1 = levels[i].OutY1;
                }
            }
        }

        /// <summary>
        /// The lon/lat box a .tmc's coastline field is rasterised over.
        ///
        /// It is the WORK range, not what the .tmc asked for. The work range is widened so that
        /// every parent has four children, so the tiles that get sampled reach past the requested
        /// box, and a field that stopped at the box would be answering from its edge texels
        /// exactly where the cut runs.
        ///
        /// Public because the converter is not the only caller that needs it. A tool that asks
        /// "does the cut touch this square at all" has to bound the field over THIS box: bounding
        /// over the .tmc's box calls a square wholly landward that the converter would still shave
        /// at its seaward edge. One definition, so the answer and the output cannot disagree.
        /// </summary>
        public static void CoastFieldBox(string tmcPath, out double west, out double east,
            out double south, out double north)
        {
            TmcDocument doc = TmcReader.Parse(tmcPath);
            var regions = new List<TmcRegion>(doc.Regions);
            regions.Sort((a, b) => a.Level.CompareTo(b.Level));
            CoastFieldBox(BuildLevels(regions), out west, out east, out south, out north);
        }

        private static void CoastFieldBox(List<LevelBuffer> levels, out double west,
            out double east, out double south, out double north)
        {
            LevelBuffer deep = levels[levels.Count - 1];
            int y0, y1;
            DeepRowRange(levels, out y0, out y1);

            west = AFS2World.LonOfGridX(deep.WorkX0, deep.Level);
            east = AFS2World.LonOfGridX(deep.WorkX1, deep.Level);
            south = AFS2World.LatOfGridY(y0, deep.Level);
            north = AFS2World.LatOfGridY(y1, deep.Level);
        }

        /// <summary>
        /// Writes a finished tile if it was asked for, and halves it into its parent either way -
        /// a tile outside the requested range still exists to make its parent whole.
        /// </summary>
        private static void EmitTile(Dictionary<int, LevelBuffer> byLevel, int shallowest,
            int level, int tx, int ty, byte[] rgb, bool[] covered,
            string outputDirectory, TtcConversionResult result, int threads, string rawDirectory,
            string mobileDirectory, bool writeDxt1, bool writeEtc2)
        {
            LevelBuffer lb;
            if (byLevel.TryGetValue(level, out lb)
                && tx >= lb.OutX0 && tx < lb.OutX1 && ty >= lb.OutY0 && ty < lb.OutY1)
            {
                var names = TtcTileWriter.Write(outputDirectory, level, tx, ty, rgb, covered,
                    lb.WantMask, threads, rawDirectory, mobileDirectory, writeDxt1, writeEtc2);
                foreach (var n in names)
                {
                    result.FilesWritten.Add(Path.GetFileName(n));
                    result.BytesWritten += new FileInfo(n).Length;
                }
            }

            if (level <= shallowest)
            {
                return;
            }

            LevelBuffer parent;
            if (!byLevel.TryGetValue(level - 1, out parent))
            {
                return;
            }

            int ptx = tx >> 1;
            int pty = ty >> 1;
            if (ptx < parent.WorkX0 || ptx >= parent.WorkX1)
            {
                return;
            }

            if (parent.PendingTy != pty)
            {
                FlushRow(byLevel, shallowest, parent, outputDirectory, result, threads, rawDirectory,
                    mobileDirectory, writeDxt1, writeEtc2);
                parent.PendingTy = pty;
            }

            int slot = ptx - parent.WorkX0;
            if (parent.Rgb[slot] == null)
            {
                parent.Rgb[slot] = new byte[TileSize * TileSize * 3];
                parent.Covered[slot] = new bool[TileSize * TileSize];
            }
            if (!parent.Touched[slot])
            {
                Array.Clear(parent.Rgb[slot], 0, parent.Rgb[slot].Length);
                Array.Clear(parent.Covered[slot], 0, parent.Covered[slot].Length);
                parent.Touched[slot] = true;
            }

            HalveInto(rgb, covered, parent.Rgb[slot], parent.Covered[slot], tx & 1, ty & 1);
        }

        private static void FlushRow(Dictionary<int, LevelBuffer> byLevel, int shallowest,
            LevelBuffer lb, string outputDirectory, TtcConversionResult result, int threads, string rawDirectory,
            string mobileDirectory, bool writeDxt1, bool writeEtc2)
        {
            if (lb.PendingTy == Int32.MinValue)
            {
                return;
            }

            int ty = lb.PendingTy;
            lb.PendingTy = Int32.MinValue;

            for (int slot = 0; slot < lb.Width; slot++)
            {
                if (!lb.Touched[slot])
                {
                    continue;
                }
                lb.Touched[slot] = false;

                EmitTile(byLevel, shallowest, lb.Level, lb.WorkX0 + slot, ty,
                    lb.Rgb[slot], lb.Covered[slot], outputDirectory, result, threads, rawDirectory,
                    mobileDirectory, writeDxt1, writeEtc2);
            }
        }

        /// <summary>
        /// Halves a child tile into one quadrant of its parent.
        ///
        /// quadX 0 is west, and quadY 1 is NORTH because grid y counts northward while row 0 of a
        /// tile is the north edge. Getting that pair the wrong way round assembles a parent out of
        /// correctly-drawn children in the wrong places, which looks like scrambled terrain rather
        /// than like a bug.
        ///
        /// Equivalent to the reference, which assembles a 4096 square from four children and halves
        /// the whole thing: the children sit on even boundaries, so no 2x2 block ever straddles two
        /// of them.
        /// </summary>
        private static void HalveInto(byte[] childRgb, bool[] childCovered,
            byte[] parentRgb, bool[] parentCovered, int quadX, int quadY)
        {
            int half = TileSize / 2;
            int rowOffset = (quadY == 1) ? 0 : half;
            int colOffset = quadX * half;

            for (int oy = 0; oy < half; oy++)
            {
                int sy0 = (2 * oy) * TileSize;
                int sy1 = (2 * oy + 1) * TileSize;
                int dy = (rowOffset + oy) * TileSize;

                for (int ox = 0; ox < half; ox++)
                {
                    int sx0 = 2 * ox;
                    int sx1 = 2 * ox + 1;

                    int a = (sy0 + sx0) * 3, b = (sy0 + sx1) * 3;
                    int c = (sy1 + sx0) * 3, d = (sy1 + sx1) * 3;
                    int o = (dy + colOffset + ox) * 3;

                    parentRgb[o] = (byte)((childRgb[a] + childRgb[b] + childRgb[c] + childRgb[d] + 2) / 4);
                    parentRgb[o + 1] = (byte)((childRgb[a + 1] + childRgb[b + 1] + childRgb[c + 1] + childRgb[d + 1] + 2) / 4);
                    parentRgb[o + 2] = (byte)((childRgb[a + 2] + childRgb[b + 2] + childRgb[c + 2] + childRgb[d + 2] + 2) / 4);

                    // Same rounding as the colour, applied to 0/1: two of the four child pixels
                    // covered is enough to call the parent pixel covered, one is not.
                    int sum = (childCovered[sy0 + sx0] ? 1 : 0) + (childCovered[sy0 + sx1] ? 1 : 0)
                            + (childCovered[sy1 + sx0] ? 1 : 0) + (childCovered[sy1 + sx1] ? 1 : 0);
                    parentCovered[dy + colOffset + ox] = ((sum + 2) / 4) != 0;
                }
            }
        }
    }
}

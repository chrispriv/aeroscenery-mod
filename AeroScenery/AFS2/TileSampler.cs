using System;
using System.Collections.Generic;

namespace AeroScenery.AFS2
{
    /// <summary>
    /// Resamples source imagery into one output tile.
    ///
    /// Port of sample_tile in tools/ttc/convert_tmc.py, and deliberately the same algorithm down to
    /// the rounding: nearest neighbour at pixel centres, first source to reach a pixel keeps it,
    /// GeoConvert's tone curve on the way out. Matching it exactly is what lets the two be compared
    /// by hash, which is the only way to know a georeferencing port is faithful rather than
    /// plausible - an off-by-half-pixel looks perfectly fine in isolation.
    ///
    /// Row 0 of what this produces is the NORTH edge, like the source and like GeoConvert's
    /// write_raw_files dump. Aerofly stores tiles bottom-up, so a flip happens later, once, on the
    /// way into the container. Do not move it here. See section 3 of docs/ttc-format.md.
    /// </summary>
    public static class TileSampler
    {
        /// <summary>
        /// Fills rgb (size*size*3) and covered (size*size) for one tile.
        ///
        /// Returns false when no source reached the tile at all. A tile nothing covers is not a
        /// black tile, it is no tile: GeoConvert omits those, and writing them would cost a full
        /// encode each and black out terrain Aerofly would otherwise draw from its own imagery.
        /// </summary>
        /// <param name="positionBands">
        /// When true this moves each source's band to whatever the tile needs, which touches the
        /// decoder and is therefore single threaded. Pass false when the caller has already placed
        /// the bands for the whole tile row - every tile in a row needs exactly the same rows, so
        /// one placement serves them all and the tiles can then be sampled in parallel. Sampling
        /// with a band that does not reach throws rather than quietly returning an empty tile.
        /// </param>
        /// <param name="blackIsMissing">
        /// When true a pure black source pixel is not taken and does not mark the pixel covered, so
        /// a later source in the folder can claim it and, failing that, the mask hands it back to
        /// Aerofly. Bing returns nothing at all where it has no imagery at the requested zoom - far
        /// offshore, that is most of the tile - and the stitcher leaves those areas at zero, so
        /// without this the converter writes black and calls it scenery.
        ///
        /// The test is exact black rather than a threshold, because that is what the failure
        /// actually produces: a cleared canvas. Real imagery reaching (0,0,0) in a shadow costs at
        /// most a 4 m dot of Aerofly's own terrain, and BuildMask's max pooling absorbs isolated
        /// ones anyway.
        ///
        /// Off by default: on it changes output, and the reference implementation in
        /// tools/ttc/convert_tmc.py does not do it, so the byte-for-byte cross-check would break.
        /// </param>
        /// <param name="coast">
        /// Where photoscenery stops at the sea, or null to keep every pixel a source reached.
        ///
        /// With a field, covered stops meaning "a source pixel existed" and starts meaning "a
        /// source pixel existed AND this point is within the margin of the land". What falls
        /// outside is not black, it is absent: the mask hands it back to Aerofly. See section 4 of
        /// docs/ttc-format.md.
        ///
        /// Off by default, for both of the reasons blackIsMissing is. It changes output, and the
        /// reference implementation knows nothing about a coastline, so the byte-for-byte
        /// cross-check only holds without it.
        /// </param>
        /// <param name="water">
        /// A per-square correction for the haze and the cloud Bing's own imagery carries over
        /// water, or null to take every source pixel as it comes. See WaterFixField.
        ///
        /// It is applied to the source value, before the tone curve, because the haze is additive
        /// in the units the stitched PNG carries and the curve is not linear.
        ///
        /// Off by default, for both of the reasons blackIsMissing and coast are.
        /// </param>
        public static bool Sample(List<SourceImage> sources, int level, int tx, int ty,
            byte[] rgb, bool[] covered, int size, bool positionBands = true,
            bool blackIsMissing = false, CoastlineField coast = null,
            WaterFixField water = null)
        {
            Array.Clear(rgb, 0, size * size * 3);
            Array.Clear(covered, 0, size * size);

            double lonW = AFS2World.LonOfGridX(tx, level);
            double lonE = AFS2World.LonOfGridX(tx + 1, level);
            double latS = AFS2World.LatOfGridY(ty, level);
            double latN = AFS2World.LatOfGridY(ty + 1, level);

            var lons = new double[size];
            for (int i = 0; i < size; i++)
            {
                lons[i] = lonW + (i + 0.5) * (lonE - lonW) / size;
            }

            // Row 0 is the north edge, so grid y runs down through the tile from ty+1 to ty.
            var lats = new double[size];
            for (int j = 0; j < size; j++)
            {
                lats[j] = AFS2World.LatOfGridY((ty + 1) - (j + 0.5) / size, level);
            }

            // Where the cut falls in this tile, decided before a single source is opened.
            //
            // Almost every tile is wholly one side of the line, and those cost one scan of a few
            // thousand field texels and nothing else: a tile out past the margin returns here,
            // having touched no source and moved no decoder; a tile wholly landward runs exactly
            // the loop it has always run. Only the thin band of tiles the line crosses pays for
            // the per-pixel test.
            bool[] allowed = null;
            if (coast != null)
            {
                double nearest, furthest;
                coast.RangeOver(Math.Min(lonW, lonE), Math.Max(lonW, lonE),
                                Math.Min(latS, latN), Math.Max(latS, latN),
                                out nearest, out furthest);

                if (nearest > coast.MarginKm)
                {
                    return false;
                }

                if (furthest > coast.MarginKm)
                {
                    allowed = new bool[size * size];
                    for (int j = 0; j < size; j++)
                    {
                        int allowedRow = j * size;
                        for (int i = 0; i < size; i++)
                        {
                            allowed[allowedRow + i] = coast.IsCovered(lats[j], lons[i]);
                        }
                    }
                }
            }

            // Whether the water correction reaches this tile, decided the same way and for the
            // same reason as the cut above: a tile wholly over land needs nothing, and asking
            // costs one scan of a few hundred texels against a per-pixel multiply and add.
            WaterFixField.Columns waterCols = null;
            float[] waterScale = null;
            float[] waterOffset = null;
            if (water != null
                && water.TouchesBox(Math.Min(lonW, lonE), Math.Max(lonW, lonE),
                                    Math.Min(latS, latN), Math.Max(latS, latN)))
            {
                waterCols = water.PlanColumns(lons);
                waterScale = new float[size];
                waterOffset = new float[size * 3];
            }

            var tone = TtcToneCurve.Table;
            var cols = new int[size];
            int anyCovered = 0;

            foreach (var s in sources)
            {
                if (!s.Covers(Math.Min(lonW, lonE), Math.Max(lonW, lonE),
                              Math.Min(latS, latN), Math.Max(latS, latN)))
                {
                    continue;
                }

                // The column a longitude lands in depends only on the column, so work it out once
                // for the whole tile rather than once per pixel.
                bool anyCol = false;
                for (int i = 0; i < size; i++)
                {
                    int cx = (int)Math.Floor(s.ColOfLon(lons[i]));
                    cols[i] = (cx >= 0 && cx < s.Width) ? cx : -1;
                    if (cols[i] >= 0)
                    {
                        anyCol = true;
                    }
                }
                if (!anyCol)
                {
                    continue;
                }

                // One contiguous run of source rows serves the whole tile, and the run only moves
                // south as ty descends - which is what keeps the decoder going forwards.
                double r0 = s.RowOfLat(lats[0]);
                double r1 = s.RowOfLat(lats[size - 1]);
                int rowFirst = (int)Math.Floor(Math.Min(r0, r1));
                int rowLast = (int)Math.Floor(Math.Max(r0, r1));

                if (positionBands)
                {
                    s.EnsureBand(rowFirst, rowLast + 1);
                }
                else if (!s.BandCovers(rowFirst, rowLast + 1))
                {
                    // Loud rather than empty. A tile sampled through a band that does not reach
                    // would come out blank, and a blank tile is indistinguishable from one no
                    // source covers - which is a legitimate result, so it would never be noticed.
                    throw new InvalidOperationException(String.Format(
                        "tile ({0}, {1}) at level {2} needs source rows {3}..{4}, but the band holds {5}..{6}",
                        tx, ty, level, rowFirst, rowLast, s.BandFirstRow, s.BandFirstRow + s.BandRows - 1));
                }

                if (s.BandRows <= 0)
                {
                    continue;
                }

                byte[] bandBytes = s.Band;
                int bandFirst = s.BandFirstRow;
                int bandRows = s.BandRows;
                int srcStride = s.Width * 3;

                for (int j = 0; j < size; j++)
                {
                    int cy = (int)Math.Floor(s.RowOfLat(lats[j]));
                    if (cy < 0 || cy >= s.Height)
                    {
                        continue;
                    }
                    int bandRow = cy - bandFirst;
                    if (bandRow < 0 || bandRow >= bandRows)
                    {
                        continue;
                    }

                    long srcRowOffset = (long)bandRow * srcStride;
                    int dstRowOffset = j * size * 3;
                    int covRowOffset = j * size;

                    if (waterCols != null)
                    {
                        water.RowFactors(lats[j], waterCols, waterScale, waterOffset);
                    }

                    for (int i = 0; i < size; i++)
                    {
                        int cx = cols[i];
                        if (cx < 0 || covered[covRowOffset + i])
                        {
                            continue;
                        }

                        if (allowed != null && !allowed[covRowOffset + i])
                        {
                            // Past the cut. Left uncovered rather than left black, so the mask
                            // gives the pixel back to Aerofly instead of painting over it.
                            continue;
                        }

                        long si = srcRowOffset + (long)cx * 3;
                        int di = dstRowOffset + i * 3;

                        if (blackIsMissing
                            && bandBytes[si] == 0 && bandBytes[si + 1] == 0 && bandBytes[si + 2] == 0)
                        {
                            // Not covered, so the next source gets its turn at this pixel. Skipping
                            // before the write is what makes the loop a compositor rather than a
                            // first-come-first-served copy.
                            continue;
                        }

                        // The tone curve is applied here rather than in a second pass over the
                        // tile. Same result: every pixel is written exactly once because the first
                        // source to reach it keeps it, and an untouched pixel stays 0, which the
                        // curve maps to 0 anyway.
                        //
                        // The water correction goes in front of the curve, not after it. The haze
                        // is additive in the source's own units, and the curve is steepest in the
                        // shadows - which is exactly where water sits - so correcting on the far
                        // side of it would take off the wrong amount.
                        if (waterScale != null)
                        {
                            int wi = i * 3;
                            rgb[di] = tone[WaterFixField.Apply(bandBytes[si], waterScale[i], waterOffset[wi])];
                            rgb[di + 1] = tone[WaterFixField.Apply(bandBytes[si + 1], waterScale[i], waterOffset[wi + 1])];
                            rgb[di + 2] = tone[WaterFixField.Apply(bandBytes[si + 2], waterScale[i], waterOffset[wi + 2])];
                        }
                        else
                        {
                            rgb[di] = tone[bandBytes[si]];
                            rgb[di + 1] = tone[bandBytes[si + 1]];
                            rgb[di + 2] = tone[bandBytes[si + 2]];
                        }

                        covered[covRowOffset + i] = true;
                        anyCovered++;
                    }
                }
            }

            return anyCovered > 0;
        }

        /// <summary>
        /// The run of source rows a whole tile row needs, so a caller can position the band once
        /// for every tile at that ty instead of per tile.
        /// </summary>
        public static void RowRangeFor(SourceImage source, int level, int ty, int size,
            out int rowFirst, out int rowEndExclusive)
        {
            double latNorth = AFS2World.LatOfGridY((ty + 1) - 0.5 / size, level);
            double latSouth = AFS2World.LatOfGridY(ty + (0.5 / size), level);

            double a = source.RowOfLat(latNorth);
            double b = source.RowOfLat(latSouth);

            rowFirst = (int)Math.Floor(Math.Min(a, b));
            rowEndExclusive = (int)Math.Floor(Math.Max(a, b)) + 1;
        }
    }
}

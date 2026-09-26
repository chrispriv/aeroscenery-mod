using System;
using System.Collections.Generic;
using System.IO;

namespace AeroScenery.AFS2
{
    /// <summary>One level of a .tmc, with its bounds already the right way round.</summary>
    public class TmcRegion
    {
        public int Level { get; set; }

        public double West { get; set; }
        public double East { get; set; }
        public double South { get; set; }
        public double North { get; set; }

        public bool WriteImagesWithMask { get; set; }
    }

    /// <summary>What a .tmc asks for, as the converter needs it.</summary>
    public class TmcDocument
    {
        public string FolderSourceFiles { get; set; }
        public string FolderDestinationTtc { get; set; }
        public string FolderDestinationRaw { get; set; }

        public bool WriteTtcFiles { get; set; }
        public bool WriteRawFiles { get; set; }
        public bool WriteImagesWithMask { get; set; }

        public List<TmcRegion> Regions { get; set; }

        public TmcDocument()
        {
            Regions = new List<TmcRegion>();
            WriteTtcFiles = true;
            WriteImagesWithMask = true;
        }
    }

    /// <summary>
    /// Reads a .tmc, so the converter can be handed the same file GeoConvert would have been.
    ///
    /// Separate from TMCFile on purpose. TMCFile writes, and writing reaches into
    /// AeroSceneryManager.Instance.Settings for the shrink margin; a reader that inherited that
    /// could not be built or tested without standing the whole app up. The converter's inputs stay
    /// free of the app singleton, which is what lets tools/ttc/csharp compile and check them on
    /// their own.
    ///
    /// Reading from disk rather than taking the TMCFile the app just built is also deliberate: the
    /// file is the contract, it lets the converter run over squares prepared by an earlier version,
    /// and there is then one code path whoever wrote the .tmc.
    /// </summary>
    public static class TmcReader
    {
        public static TmcDocument Parse(string path)
        {
            return ParseText(File.ReadAllText(path, System.Text.Encoding.UTF8),
                Path.GetFileName(path));
        }

        public static TmcDocument ParseText(string text, string nameForErrors = "tmc")
        {
            var doc = new TmcDocument();
            TmcRegion region = null;

            foreach (var f in TmFields.Parse(text))
            {
                switch (f.Name)
                {
                    case "folder_source_files":
                        doc.FolderSourceFiles = f.Value;
                        break;
                    case "folder_destination_ttc":
                        doc.FolderDestinationTtc = f.Value;
                        break;
                    case "folder_destination_raw":
                        doc.FolderDestinationRaw = f.Value;
                        break;
                    case "write_ttc_files":
                        doc.WriteTtcFiles = TmFields.Bool(f.Value);
                        break;
                    case "write_raw_files":
                        doc.WriteRawFiles = TmFields.Bool(f.Value);
                        break;

                    case "level":
                    {
                        // A level opens a region, and the fields after it belong to that region
                        // until the next level. This is the only reason field order matters.
                        int level;
                        if (Int32.TryParse(f.Value, out level))
                        {
                            region = new TmcRegion
                            {
                                Level = level,
                                WriteImagesWithMask = doc.WriteImagesWithMask
                            };
                            doc.Regions.Add(region);
                        }
                        break;
                    }

                    case "write_images_with_mask":
                    {
                        bool v = TmFields.Bool(f.Value);
                        if (region == null)
                        {
                            doc.WriteImagesWithMask = v;
                        }
                        else
                        {
                            region.WriteImagesWithMask = v;
                        }
                        break;
                    }

                    case "lonlat_min":
                    case "lonlat_max":
                    {
                        double lon, lat;
                        if (region != null && TmFields.TryPair(f.Value, out lon, out lat))
                        {
                            // lonlat_min is the NORTH west corner and lonlat_max the SOUTH east
                            // one, so taking the names literally puts the region upside down.
                            // Normalise here, once, and let nothing downstream worry about it.
                            if (f.Name == "lonlat_min")
                            {
                                region.West = lon;
                                region.North = lat;
                            }
                            else
                            {
                                region.East = lon;
                                region.South = lat;
                            }
                        }
                        break;
                    }
                }
            }

            foreach (var r in doc.Regions)
            {
                if (r.West > r.East)
                {
                    double t = r.West; r.West = r.East; r.East = t;
                }
                if (r.South > r.North)
                {
                    double t = r.South; r.South = r.North; r.North = t;
                }
            }

            if (doc.Regions.Count == 0)
            {
                throw new InvalidDataException(nameForErrors + " declares no regions");
            }

            return doc;
        }
    }
}

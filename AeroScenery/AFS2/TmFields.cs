using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace AeroScenery.AFS2
{
    /// <summary>
    /// Reader for the flat `&lt;[type][name][value]&gt;` syntax IPACS use for .tmc and .aid files.
    ///
    /// The app has always been able to write these - see AIDFile.ToString and TMCFile.ToString -
    /// but never to read them, because GeoConvert was the only thing that consumed them. The
    /// in-app converter has to read them too, and it has to read the ones already on disk rather
    /// than only the ones this session happened to write.
    ///
    /// Deliberately not a real parser for the nested tree. Every field the converter needs is
    /// uniquely named, so a flat scan is enough, and it is the same shape as the reference
    /// implementation in tools/ttc/convert_tmc.py. What it does have to preserve is order, because
    /// a .tmc repeats `level` once per region and the fields after each one belong to it.
    /// </summary>
    public static class TmFields
    {
        private static readonly Regex Field = new Regex(
            @"<\[([a-z0-9_]+)\]\s*\[([a-z0-9_]*)\]\s*\[([^\]]*)\]>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public struct Field3
        {
            public string Type;
            public string Name;
            public string Value;
        }

        public static List<Field3> Parse(string text)
        {
            var list = new List<Field3>();
            foreach (Match m in Field.Matches(text))
            {
                list.Add(new Field3
                {
                    Type = m.Groups[1].Value,
                    Name = m.Groups[2].Value,
                    Value = m.Groups[3].Value.Trim()
                });
            }
            return list;
        }

        public static List<Field3> ParseFile(string path)
        {
            // utf-8 with a BOM is what the app writes; encoding errors must not be fatal, since a
            // file that came from somewhere else still parses fine as far as these fields go.
            return Parse(File.ReadAllText(path, System.Text.Encoding.UTF8));
        }

        /// <summary>Parses one number. These files are written invariant, so read them that way.</summary>
        public static double Number(string s)
        {
            return Double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        /// <summary>Parses a `x y` pair such as steps_per_pixel or lonlat_min.</summary>
        public static bool TryPair(string s, out double a, out double b)
        {
            a = b = 0;
            var parts = s.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                return false;
            }
            return Double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out a)
                && Double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out b);
        }

        public static bool Bool(string s)
        {
            return String.Equals(s, "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}

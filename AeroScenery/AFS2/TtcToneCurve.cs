namespace AeroScenery.AFS2
{
    /// <summary>
    /// GeoConvert's tone curve, measured rather than guessed.
    ///
    /// GeoConvert does not resample the source straight into the tile: it brightens it on the way.
    /// Comparing our resampler against its own write_raw_files output over 15 million pixels from
    /// five tiles across three levels, the difference was a single curve, identical in all three
    /// channels, and applying it drops the mean absolute error from 33.07 to 1.79 out of 255.
    ///
    /// Not a plain gamma - a gamma fit is decent in the midtones and badly wrong in the shadows
    /// (16 measured to 30, predicted 49), so the table is the honest representation.
    ///
    /// Matching it matters because the engine is the consumer and IPACS wrote both ends: a tile
    /// built without this curve renders visibly darker than every tile the user already has.
    ///
    /// Mirror of tools/ttc/tone_curve.py - change neither without the other, or the cross-language
    /// hashes stop matching.
    /// </summary>
    public static class TtcToneCurve
    {
        /// <summary>Index is the source value, entry is what GeoConvert writes.</summary>
        public static readonly byte[] Table =
        {
              0,  10,  11,  11,  12,  13,  13,  15,  18,  20,  22,  24,  24,  26,  27,  28,
             30,  30,  32,  35,  36,  38,  40,  42,  43,  44,  47,  48,  50,  52,  52,  55,
             56,  58,  60,  60,  63,  64,  65,  67,  69,  70,  72,  73,  75,  76,  77,  79,
             80,  81,  83,  85,  86,  87,  89,  89,  91,  93,  94,  95,  97,  97,  99, 101,
            102, 103, 104, 105, 107, 108, 109, 111, 112, 113, 113, 115, 117, 118, 119, 120,
            121, 121, 124, 125, 126, 127, 128, 129, 130, 131, 133, 134, 135, 136, 137, 138,
            139, 140, 141, 142, 143, 144, 145, 146, 147, 147, 149, 150, 151, 152, 153, 154,
            155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 165, 166, 167, 168, 169,
            170, 171, 172, 173, 173, 174, 176, 176, 177, 178, 179, 179, 180, 181, 183, 184,
            185, 185, 187, 187, 189, 189, 190, 191, 192, 193, 194, 194, 194, 196, 197, 197,
            197, 199, 200, 200, 200, 202, 202, 202, 203, 204, 205, 205, 206, 207, 207, 208,
            209, 209, 210, 211, 212, 212, 214, 214, 214, 216, 217, 217, 217, 218, 218, 219,
            220, 220, 221, 222, 222, 223, 223, 224, 225, 225, 226, 227, 227, 228, 228, 229,
            231, 230, 231, 232, 232, 233, 233, 234, 235, 235, 236, 237, 237, 237, 239, 239,
            240, 242, 241, 243, 243, 243, 243, 245, 245, 245, 247, 247, 247, 247, 247, 247,
            247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247,
        };
    }
}

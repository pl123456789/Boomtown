namespace Boomtown.WorldGeneration
{
    /// <summary>
    /// Describes the interpreted landscape environment around a river sample.
    ///
    /// This is not a texture, material, tree, gold deposit, or gameplay rule.
    /// It is the shared environmental classification that those systems
    /// will read later.
    /// </summary>
    public enum RiverLandscapeType
    {
        /// <summary>
        /// The main flowing-water corridor.
        /// </summary>
        ActiveChannel,

        /// <summary>
        /// Depositional gravel ground, usually on the inside of a bend
        /// or in a wider, slower river reach.
        /// </summary>
        GravelBar,

        /// <summary>
        /// A steep, eroding outside bank with little deposition.
        /// </summary>
        CutBank,

        /// <summary>
        /// Low ground beside the river that may flood seasonally.
        /// </summary>
        Floodplain,

        /// <summary>
        /// An older raised river surface above the active floodplain.
        /// </summary>
        Terrace,

        /// <summary>
        /// Exposed or shallow bedrock bordering the river.
        /// </summary>
        BedrockMargin
    }
}
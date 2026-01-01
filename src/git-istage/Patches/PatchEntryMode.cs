namespace GitIStage.Patches;

// Taken from http://stackoverflow.com/a/8347325/335418
public enum PatchEntryMode
{
    Nonexistent                            = 0x0000,

    /// <summary>
    /// Octal: 040000
    /// </summary>
    Directory                              = 0x4000,

    /// <summary>
    /// Octal: 100644
    /// </summary>
    RegularNonExecutableFile               = 0x81A4,

    /// <summary>
    /// Octal: 100664
    /// </summary>
    RegularNonExecutableGroupWriteableFile = 0x81B4,

    /// <summary>
    /// Octal: 100755
    /// </summary>
    RegularExecutableFile                  = 0x81ED,

    /// <summary>
    /// Octal: 120000
    /// </summary>
    SymbolicLink                           = 0xA000,

    /// <summary>
    /// Octal: 160000
    /// </summary>
    Gitlink                                = 0xE000,
}
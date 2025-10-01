namespace MinaGroup.Backend.Enums
{
    [Flags]
    public enum WeekDays
    {
        None = 0,
        Mandag = 1 << 0,    // 1
        Tirsdag = 1 << 1,   // 2
        Onsdag = 1 << 2, // 4
        Torsdag = 1 << 3,  // 8
        Fredag = 1 << 4,    // 16
        Lørdag = 1 << 5,  // 32
        Søndag = 1 << 6     // 64
    }
}

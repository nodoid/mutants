namespace Mutants.Core;

/// <summary>
/// The five districts of the fallen town. Layouts are original designs; the
/// adventure structure (keys, gate, lair, portal) is shared across them.
/// </summary>
public static class LevelCatalog
{
    public const int Count = 5;

    public static IReadOnlyList<LevelDefinition> Definitions { get; } = new[]
    {
        new LevelDefinition("Hollow Hill", "Where the gate first rusted open.", LevelTheme.Graveyard, new[]
        {
            "########################",
            "#....T......C..#.......#",
            "#.S............#..X....#",
            "#.......K......#.......#",
            "#.....E........#.R.....#",
            "#..............#.......#",
            "#..............#....H..#",
            "#........C.....#.......#",
            "#...........B..#.......#",
            "#..............#....P..#",
            "#...G..........#..E....#",
            "#..K...........#T......#",
            "#..............D.......#",
            "#..C...........#.......#",
            "#........K.....#...H...#",
            "########################",
        }),
        new LevelDefinition("Sunken Mire", "The water remembers every footprint.", LevelTheme.Mire, new[]
        {
            "########################",
            "#....W.........#.......#",
            "#.S..W.E.......#...X...#",
            "#..K.W......C..#...E...#",
            "#....W.........#.R.....#",
            "#..............#.......#",
            "#.C........K...#..H....#",
            "#..............#.......#",
            "#.........B....#.......#",
            "#..............#....P..#",
            "#...W..........#.......#",
            "#...W.......G..#.T.....#",
            "#...W..........D.......#",
            "#...W...K......#.......#",
            "#...W..........#...H...#",
            "########################",
        }),
        new LevelDefinition("Ashen Foundry", "The furnaces still breathe.", LevelTheme.Foundry, new[]
        {
            "########################",
            "#..K........C..#.......#",
            "#.S............#...X...#",
            "#........B.....#.......#",
            "#...###........#R......#",
            "#..............#.......#",
            "#....E.........#...H...#",
            "#...###C...G...#.......#",
            "#..H...........#..E....#",
            "#.........R....#.......#",
            "#..............#....P..#",
            "#......R.......#.......#",
            "#.K............D.......#",
            "#.........K....#.......#",
            "#..............#.......#",
            "########################",
        }),
        new LevelDefinition("Frostbitten Chapel", "Something prays beneath the ice.", LevelTheme.Chapel, new[]
        {
            "########################",
            "#..............#.......#",
            "#.S............#...X...#",
            "#.K..G.........#.......#",
            "#...R....R.....#.......#",
            "#...........K..#.......#",
            "#..............#..H....#",
            "#...R.C..R.....#.......#",
            "#.........B....#.......#",
            "#..............#....P..#",
            "#...R....R.....#...B...#",
            "#.......K......#.......#",
            "#..........E...D.......#",
            "#..........C...#.......#",
            "#..H...........#.......#",
            "########################",
        }),
        new LevelDefinition("The Gears of Night", "Midnight is a machine, and it is winding down.", LevelTheme.Clocktower, new[]
        {
            "########################",
            "#........K....E#.......#",
            "#.S.........#..#.......#",
            "#...#.G........#...X...#",
            "#..........C...#.......#",
            "#........B.....#..H....#",
            "#..C....###....#.......#",
            "#......C.......#.......#",
            "#..............#..G....#",
            "#...#.......K..#.......#",
            "#.........G....#....P..#",
            "#..............#.......#",
            "#...........#..D.B.....#",
            "#.K.........E..#.......#",
            "#..............#...H...#",
            "########################",
        }),
    };

    /// <summary>Parses all levels. Throws if any authored map is malformed.</summary>
    public static IReadOnlyList<LevelMap> LoadAll()
        => Definitions.Select(LevelMap.Parse).ToArray();

    public static LevelMap Load(int index)
    {
        if (index < 0 || index >= Definitions.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return LevelMap.Parse(Definitions[index]);
    }
}

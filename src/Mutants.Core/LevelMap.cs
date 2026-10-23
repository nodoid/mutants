using System.Numerics;

namespace Mutants.Core;

public enum LevelTheme
{
    Graveyard,
    Mire,
    Foundry,
    Chapel,
    Clocktower,
}

public enum TileKind
{
    Floor,
    Wall,
    Tree,
    Rock,
    Water,
    Gate,
    Portal,
}

public enum EnemyKind
{
    Crawler,
    Bat,
    Ghoul,
}

public enum PickupKind
{
    Key,
    Coin,
    Heart,
}

public readonly record struct EnemySpawn(EnemyKind Kind, int Column, int Row);

public readonly record struct PickupSpawn(PickupKind Kind, int Column, int Row);

/// <summary>
/// A parsed level: rectangular tile grid plus entity spawn markers.
/// Gate is the only barrier that opens during play.
/// </summary>
public sealed class LevelMap
{
    public const int TileSize = 96;

    private readonly TileKind[,] _tiles;

    private LevelMap(
        string name,
        string subtitle,
        LevelTheme theme,
        TileKind[,] tiles,
        int columns,
        int rows,
        int spawnColumn,
        int spawnRow,
        int gateColumn,
        int gateRow,
        int bossColumn,
        int bossRow,
        int portalColumn,
        int portalRow,
        IReadOnlyList<EnemySpawn> enemies,
        IReadOnlyList<PickupSpawn> pickups)
    {
        Name = name;
        Subtitle = subtitle;
        Theme = theme;
        _tiles = tiles;
        Columns = columns;
        Rows = rows;
        SpawnCell = new Cell(spawnColumn, spawnRow);
        GateCell = new Cell(gateColumn, gateRow);
        BossCell = new Cell(bossColumn, bossRow);
        PortalCell = new Cell(portalColumn, portalRow);
        Enemies = enemies;
        Pickups = pickups;
        RequiredKeys = pickups.Count(p => p.Kind == PickupKind.Key);
    }

    public string Name { get; }

    public string Subtitle { get; }

    public LevelTheme Theme { get; }

    public int Columns { get; }

    public int Rows { get; }

    public int Width => Columns * TileSize;

    public int Height => Rows * TileSize;

    public Cell SpawnCell { get; }

    public Cell GateCell { get; }

    public Cell BossCell { get; }

    public Cell PortalCell { get; }

    public IReadOnlyList<EnemySpawn> Enemies { get; }

    public IReadOnlyList<PickupSpawn> Pickups { get; }

    public int RequiredKeys { get; }

    public Vector2 SpawnPosition => CellCenter(SpawnCell);

    public Vector2 GatePosition => CellCenter(GateCell);

    public Vector2 BossPosition => CellCenter(BossCell);

    public Vector2 PortalPosition => CellCenter(PortalCell);

    public TileKind this[int column, int row] => _tiles[column, row];

    public static Vector2 CellCenter(Cell cell)
        => new((cell.Column + 0.5f) * TileSize, (cell.Row + 0.5f) * TileSize);

    public static Vector2 CellCenter(int column, int row) => CellCenter(new Cell(column, row));

    public bool IsInside(int column, int row) => column >= 0 && row >= 0 && column < Columns && row < Rows;

    public bool IsWalkable(int column, int row, bool gateOpen)
    {
        if (!IsInside(column, row))
        {
            return false;
        }

        return _tiles[column, row] switch
        {
            TileKind.Floor or TileKind.Portal => true,
            TileKind.Gate => gateOpen,
            _ => false,
        };
    }

    /// <summary>Circle-versus-tile collision against blocked tiles.</summary>
    public bool IsBlocked(Vector2 position, float radius, bool gateOpen)
    {
        var minColumn = (int)MathF.Floor((position.X - radius) / TileSize);
        var maxColumn = (int)MathF.Floor((position.X + radius) / TileSize);
        var minRow = (int)MathF.Floor((position.Y - radius) / TileSize);
        var maxRow = (int)MathF.Floor((position.Y + radius) / TileSize);

        for (var row = minRow; row <= maxRow; row++)
        {
            for (var column = minColumn; column <= maxColumn; column++)
            {
                if (IsWalkable(column, row, gateOpen))
                {
                    continue;
                }

                if (Collision.CircleTouchesRect(
                        position,
                        radius,
                        column * TileSize,
                        row * TileSize,
                        TileSize,
                        TileSize))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Cells reachable from the spawn, used for authoring sanity checks.</summary>
    public HashSet<Cell> FloodFill(bool gateOpen)
    {
        var visited = new HashSet<Cell> { SpawnCell };
        var queue = new Queue<Cell>();
        queue.Enqueue(SpawnCell);

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            foreach (var next in Neighbours(cell))
            {
                if (!IsWalkable(next.Column, next.Row, gateOpen) || !visited.Add(next))
                {
                    continue;
                }

                queue.Enqueue(next);
            }
        }

        return visited;
    }

    public IEnumerable<Cell> Neighbours(Cell cell)
    {
        yield return new Cell(cell.Column + 1, cell.Row);
        yield return new Cell(cell.Column - 1, cell.Row);
        yield return new Cell(cell.Column, cell.Row + 1);
        yield return new Cell(cell.Column, cell.Row - 1);
    }

    public static LevelMap Parse(LevelDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var rows = definition.Rows;
        if (rows.Count < 3)
        {
            throw new ArgumentException("A level needs at least three rows.", nameof(definition));
        }

        var columns = rows[0].Length;
        if (columns < 3 || rows.Any(r => r.Length != columns))
        {
            throw new ArgumentException("Every level row must have the same width.", nameof(definition));
        }

        var tiles = new TileKind[columns, rows.Count];
        var enemies = new List<EnemySpawn>();
        var pickups = new List<PickupSpawn>();
        var spawns = new List<Cell>();
        var gates = new List<Cell>();
        var bosses = new List<Cell>();
        var portals = new List<Cell>();

        for (var row = 0; row < rows.Count; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                var tile = TileKind.Floor;
                switch (rows[row][column])
                {
                    case '#':
                        tile = TileKind.Wall;
                        break;
                    case 'T':
                        tile = TileKind.Tree;
                        break;
                    case 'R':
                        tile = TileKind.Rock;
                        break;
                    case 'W':
                        tile = TileKind.Water;
                        break;
                    case 'D':
                        tile = TileKind.Gate;
                        gates.Add(new Cell(column, row));
                        break;
                    case 'P':
                        tile = TileKind.Portal;
                        portals.Add(new Cell(column, row));
                        break;
                    case '.':
                        break;
                    case 'S':
                        spawns.Add(new Cell(column, row));
                        break;
                    case 'K':
                        pickups.Add(new PickupSpawn(PickupKind.Key, column, row));
                        break;
                    case 'C':
                        pickups.Add(new PickupSpawn(PickupKind.Coin, column, row));
                        break;
                    case 'H':
                        pickups.Add(new PickupSpawn(PickupKind.Heart, column, row));
                        break;
                    case 'E':
                        enemies.Add(new EnemySpawn(EnemyKind.Crawler, column, row));
                        break;
                    case 'B':
                        enemies.Add(new EnemySpawn(EnemyKind.Bat, column, row));
                        break;
                    case 'G':
                        enemies.Add(new EnemySpawn(EnemyKind.Ghoul, column, row));
                        break;
                    case 'X':
                        bosses.Add(new Cell(column, row));
                        break;
                    default:
                        throw new FormatException($"Unknown tile '{rows[row][column]}' at {column},{row} in {definition.Name}.");
                }

                tiles[column, row] = tile;
            }
        }

        if (spawns.Count != 1)
        {
            throw new FormatException($"{definition.Name} must have exactly one spawn (found {spawns.Count}).");
        }

        if (gates.Count != 1)
        {
            throw new FormatException($"{definition.Name} must have exactly one gate (found {gates.Count}).");
        }

        if (bosses.Count != 1)
        {
            throw new FormatException($"{definition.Name} must have exactly one boss (found {bosses.Count}).");
        }

        if (portals.Count != 1)
        {
            throw new FormatException($"{definition.Name} must have exactly one portal (found {portals.Count}).");
        }

        if (pickups.Count(p => p.Kind == PickupKind.Key) < 1)
        {
            throw new FormatException($"{definition.Name} needs at least one key.");
        }

        // Border safety: the player must never be able to walk out of the world.
        for (var column = 0; column < columns; column++)
        {
            if (tiles[column, 0] != TileKind.Wall || tiles[column, rows.Count - 1] != TileKind.Wall)
            {
                throw new FormatException($"{definition.Name} must be walled along the top and bottom.");
            }
        }

        for (var row = 0; row < rows.Count; row++)
        {
            if (tiles[0, row] != TileKind.Wall || tiles[columns - 1, row] != TileKind.Wall)
            {
                throw new FormatException($"{definition.Name} must be walled along the left and right.");
            }
        }

        return new LevelMap(
            definition.Name,
            definition.Subtitle,
            definition.Theme,
            tiles,
            columns,
            rows.Count,
            spawns[0].Column,
            spawns[0].Row,
            gates[0].Column,
            gates[0].Row,
            bosses[0].Column,
            bosses[0].Row,
            portals[0].Column,
            portals[0].Row,
            enemies,
            pickups);
    }
}

/// <summary>Raw level description: a name, a theme and an ASCII map.</summary>
public sealed record LevelDefinition(string Name, string Subtitle, LevelTheme Theme, IReadOnlyList<string> Rows);

/// <summary>Integer tile coordinate.</summary>
public readonly record struct Cell(int Column, int Row);

public static class Collision
{
    public static bool CircleTouchesRect(Vector2 center, float radius, float x, float y, float width, float height)
    {
        var closestX = Math.Clamp(center.X, x, x + width);
        var closestY = Math.Clamp(center.Y, y, y + height);
        var dx = center.X - closestX;
        var dy = center.Y - closestY;
        return (dx * dx) + (dy * dy) < radius * radius;
    }
}

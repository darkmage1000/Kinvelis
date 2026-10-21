namespace Wildbond
{
    /// <summary>
    /// What holds an upper floor up (user, 2026-10-21: "we can't place anything unless floor and walls are below, how would
    /// I make a balcony"). An upper floor used to need a floor directly under it on the same square. Now a floor at
    /// storey L stands if:
    ///   - there is a floor under it (as before), or
    ///   - a wall, doorway, window or archway stands under one of its edges (an open room built on walls), or
    ///   - it reaches one square out from a floor at the same storey that is itself held up one of those two ways: a
    ///     balcony. A balcony doesn't hold up another balcony, so they stick out one square at most.
    /// The Wooden Railing (a low wall) closes a balcony's open edges. Removing a floor or wall that a balcony or an
    /// open upper floor rests on is refused until that floor comes off.
    /// </summary>
    public partial class Housing
    {
        /// <summary>A floor under this square at L - 1, or an edge piece standing under one of its edges. y = where a floor at L would sit.</summary>
        bool HeldFromBelow(int cx, int cz, int L, out float y, Piece skip = null)
        {
            y = 0f;
            if (L <= 0) return false;
            if (floors.TryGetValue(LKey(cx, cz, L - 1), out var f) && f != skip) { y = f.Y + LevelH; return true; }
            if (pendingFloors.TryGetValue(LKey(cx, cz, L - 1), out float py)) { y = py + LevelH; return true; }
            for (int dir = 0; dir < 4; dir++)
            {
                EdgeOf(cx, cz, dir, out int ex, out int ez, out int side);
                var w = EdgePiece(ex, ez, side, L - 1);
                if (w == null || w == skip || w.def.kind == HouseKind.Steps) continue;
                if (w.level + Span(w.def) != L) continue;   // only a piece whose top is this storey's floor holds it
                y = w.Y + LevelH * (L - w.level);
                return true;
            }
            return false;
        }

        /// <summary>A held floor at the same storey on a square next to this one: this square can be a balcony off it.</summary>
        bool Overhang(int cx, int cz, int L, out float y, Piece skip = null)
        {
            y = 0f;
            if (L <= 0) return false;
            for (int dir = 0; dir < 4; dir++)
            {
                int nx = cx + (dir == 1 ? 1 : dir == 3 ? -1 : 0), nz = cz + (dir == 0 ? 1 : dir == 2 ? -1 : 0);
                bool built = floors.TryGetValue(LKey(nx, nz, L), out var n);
                if (built && n == skip) continue;
                float ny;
                if (built) ny = n.Y;
                else if (!pendingFloors.TryGetValue(LKey(nx, nz, L), out ny)) continue;
                if (!HeldFromBelow(nx, nz, L, out _, skip)) continue;
                y = ny;
                return true;
            }
            return false;
        }

        /// <summary>Does this upper floor still stand with `skip` gone?</summary>
        bool Stands(Piece f, Piece skip) => f.level <= 0 || HeldFromBelow(f.cx, f.cz, f.level, out _, skip) || Overhang(f.cx, f.cz, f.level, out _, skip);

        /// <summary>
        /// Refuses taking away a piece that an upper floor rests on: a floor a balcony juts out from, or a wall an open
        /// upper floor stands on. Checks the floors at the storey it could hold (the floor's own, or the one a wall tops out at).
        /// </summary>
        string HeldUpBy(Piece pc)
        {
            int L; int x0, x1, z0, z1;
            if (pc.def.kind == HouseKind.Floor) { L = pc.level; x0 = pc.cx - 2; x1 = pc.cx + 2; z0 = pc.cz - 2; z1 = pc.cz + 2; }
            else if (pc.def.kind == HouseKind.Wall || pc.def.kind == HouseKind.Doorway || pc.def.kind == HouseKind.Window)
            {
                L = pc.level + Span(pc.def);
                x0 = pc.cx - 1; x1 = pc.cx + 2; z0 = pc.cz - 1; z1 = pc.cz + 2;
            }
            else return null;
            if (L <= 0 || L > MaxLevel) return null;
            for (int x = x0; x <= x1; x++)
                for (int z = z0; z <= z1; z++)
                {
                    if (!floors.TryGetValue(LKey(x, z, L), out var f) || f == pc) continue;
                    if (!Stands(f, null)) continue;   // already unsupported (an old save): don't block on it
                    if (!Stands(f, pc)) return pc.def.kind == HouseKind.Floor ? "A balcony juts out from this floor - take it off first" : "An upper floor rests on this wall - take that floor off first";
                }
            return null;
        }
    }
}

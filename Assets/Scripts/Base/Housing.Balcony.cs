namespace Wildbond
{
    /// <summary>
    /// What holds an upper floor up (user, 2026-10-21: "we can't place anything unless floor and walls are below, how would
    /// I make a balcony"; 2026-10-22: "building shouldn't need a floor underneath, I don't mind breaking physics laws...
    /// let a ceiling that will become the floor of the 2nd floor float connected to another"). A floor at storey L stands if:
    ///   - there is a floor under it, or
    ///   - a wall, doorway, window or archway stands under one of its edges (an open room built on walls), or
    ///   - it touches another floor at the same storey on one of its four sides - any floor, floating or not, so an
    ///     upper storey can spread out over open air as far as you like.
    /// The Wooden Railing (a low wall) closes open edges. Removing a floor or wall is refused only if it would leave an
    /// upper floor touching nothing at all.
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

        /// <summary>Another floor (built or being built) at the same storey on a square next to this one; y = its height.</summary>
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
                    if (!Stands(f, pc)) return pc.def.kind == HouseKind.Floor ? "A floor beside this one would be left touching nothing - take it off first" : "An upper floor rests on this wall - take that floor off first";
                }
            return null;
        }
    }
}

/**
 * Name: John Deardorff
 * Date: 06/03/26
 * Project ARDO
 *
 * Class description:
 * `HitResult.cs` is a value type describing one hero attack: the damage dealt,
    whether it was a critical hit or a miss, and any extra effect text.
 */
namespace ARDO.Core.Entities
{
    public readonly record struct HitResult(int Damage, bool Critical, bool Missed, string Notes)
    {
        public static HitResult Miss => new(0, false, true, string.Empty);
    }
}

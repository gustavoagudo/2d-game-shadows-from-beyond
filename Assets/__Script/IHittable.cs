// Anything the player's sword can hit (regular enemies, bosses).
public interface IHittable
{
    // Returns true if the hit landed.
    bool TakeSwordHit();
}

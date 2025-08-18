using Godot;

public interface IDamageable
{
    void ApplyDamage(int amount, Vector3 hitPos, Vector3 hitNormal);
}

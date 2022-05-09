using System;

public class RandomNumbers
{
    public uint seed;

    public RandomNumbers(int seed = 1)
    {
        this.seed = (uint)seed + 2147483647;
    }

    public int Range(int min, int max)
    {
        if (max <= min) throw new ArgumentException("MinValue must be less than MaxValue");

        return GetInt(min, max);
    }
    public float Range(float min, float max)
    {
        if (max <= min) throw new ArgumentException("MinValue must be less than MaxValue");

        return (float)GetDouble(min, max);
    }

    public bool Bool()
    {
        return Range(-10, 11) > 0;
    }


    private int GetInt(int minValue, int maxValue)
    {
        return ConvertToIntRange(randomLong(), minValue, maxValue);
    }
    private double GetDouble(double minValue, double maxValue)
    {
        return ConvertToDoubleRange(randomLong(), minValue, maxValue);
    }

    private int ConvertToIntRange(uint val, int minValue, int maxValue)
    {
        return (int)(val % (maxValue - minValue) + minValue);
    }
    private double ConvertToDoubleRange(uint val, double minValue, double maxValue)
    {
        return (double)val / uint.MaxValue * (maxValue - minValue) + minValue;
    }

    private uint Rnd()
    {
        seed += 0xe120fc15;
        ulong tmp = (ulong)seed * 0x4a39b70d;
        uint m1 = (uint)((tmp >> 32) ^ tmp);
        tmp = (ulong)m1 * 0x12fad5c9;
        return (uint)((tmp >> 32) ^ tmp);
    }

    private uint randomLong()
    {
        seed ^= (seed << 21);
        seed ^= (seed >> 35);
        seed ^= (seed << 4);
        return seed;
    }
}

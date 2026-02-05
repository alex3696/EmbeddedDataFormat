namespace NetEdf.src;

// Fractional Numbers
// https://bob.cs.sonoma.edu/IntroCompOrg-x64/bookch14.html

/// <summary>
/// Represents a rational number
/// </summary>
public struct Fraction
{
    public int Numerator;
    public int Denominator;

    public Fraction(int numerator, int denominator)
    {
        Numerator = numerator;
        Denominator = denominator;
    }

    /// <summary>
    /// Approximates a fraction from the provided double
    /// </summary>
    public static Fraction Parse(double d)
    {
        return ApproximateFraction(d);
    }

    /// <summary>
    /// Returns this fraction expressed as a double, rounded to the specified number of decimal places.
    /// Returns double.NaN if denominator is zero
    /// </summary>
    public double ToDouble(int decimalPlaces)
    {
        if (Denominator == 0)
            return double.NaN;

        return Math.Round(
            Numerator / (double)Denominator,
            decimalPlaces
        );
    }


    /// <summary>
    /// Approximates the provided value to a fraction.
    /// http://stackoverflow.com/questions/95727/how-to-convert-floats-to-human-readable-fractions
    /// </summary>
    private static Fraction ApproximateFraction(double value)
    {
        const double EPSILON = .000001d;

        int n = 1;  // numerator
        int d = 1;  // denominator
        double fraction = n / d;

        while (Math.Abs(fraction - value) > EPSILON)
        {
            if (fraction < value)
                n++;
            else
            {
                d++;
                n = (int)Math.Round(value * d);
            }

            fraction = n / (double)d;
        }

        return new Fraction(n, d);
    }
}

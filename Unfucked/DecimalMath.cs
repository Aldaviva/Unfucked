namespace Unfucked;

/// <summary>
/// <para>Analogy of <see cref="Math"/> class for decimal types </para>
/// </summary>
/// <remarks>
/// <para>By Ramin Rahimzada: <see href="https://github.com/raminrahimzada/CSharp-Helper-Classes/blob/8f77a2b048a598d02053b7118a7fd63edf6c99cd/Math/DecimalMath/DecimalMath.cs"/></para>
/// </remarks>
[ExcludeFromCodeCoverage]
public static class DecimalMath {

    /// <summary>
    /// represents PI
    /// </summary>
    public const decimal PI = 3.14159265358979323846264338327950288419716939937510M;

    /// <summary>
    /// represents a very small number
    /// </summary>
    public const decimal EPSILON = 0.0000000000000000001M;

    /// <summary>
    /// represents 2*PI
    /// </summary>
    private const decimal P_IX2 = 6.28318530717958647692528676655900576839433879875021M;

    /// <summary>
    /// represents E
    /// </summary>
    public const decimal E = 2.7182818284590452353602874713526624977572470936999595749M;

    /// <summary>
    /// represents PI/2
    /// </summary>
    private const decimal P_IDIV2 = 1.570796326794896619231321691639751442098584699687552910487M;

    /// <summary>
    /// represents PI/4
    /// </summary>
    private const decimal P_IDIV4 = 0.785398163397448309615660845819875721049292349843776455243M;

    /// <summary>
    /// represents 1.0/E
    /// </summary>
    private const decimal EINV = 0.3678794411714423215955237701614608674458111310317678M;

    /// <summary>
    /// log(10,E) factor
    /// </summary>
    private const decimal LOG10_INV = 0.434294481903251827651128918916605082294397005803666566114M;

    /// <summary>
    /// Zero
    /// </summary>
    public const decimal ZERO = 0.0M;

    /// <summary>
    /// One
    /// </summary>
    public const decimal ONE = 1.0M;

    /// <summary>
    /// Represents 0.5M
    /// </summary>
    private const decimal HALF = 0.5M;

    /// <summary>
    /// Max iterations count in Taylor series
    /// </summary>
    private const int MAX_ITERATION = 100;

    /// <summary>
    /// <para>Analogy of <see cref="System.Math.Exp"/> method</para>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static decimal Exp(decimal x) {
        int count = 0;

        if (x > ONE) {
            count =  decimal.ToInt32(decimal.Truncate(x));
            x     -= decimal.Truncate(x);
        }

        if (x < ZERO) {
            count = decimal.ToInt32(decimal.Truncate(x) - 1);
            x     = ONE + (x - decimal.Truncate(x));
        }

        int     iteration = 1;
        decimal result    = ONE;
        decimal factorial = ONE;
        decimal cachedResult;
        do {
            cachedResult =  result;
            factorial    *= x / iteration++;
            result       += factorial;
        } while (cachedResult != result);

        if (count == 0) {
            return result;
        }

        return result * PowerN(E, count);
    }

    /// <summary>
    /// <para>Analogy of <see cref="System.Math.Pow"/> method</para>
    /// <inheritdoc cref="System.Math.Pow"/>
    /// </summary>
    /// <param name="value"></param>
    /// <param name="pow"></param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is 0 and <paramref name="pow"/> is negative, or <paramref name="value"/> is negative and <paramref name="pow"/> is not an integer</exception>
    /// <returns></returns>
    public static decimal Power(decimal value, decimal pow) {
        switch (pow) {
            case ZERO:
                return ONE;
            case ONE:
                return value;
        }

        if (value == ONE) return ONE;

        if (value == ZERO) {
            if (pow > ZERO) {
                return ZERO;
            }

            throw new ArgumentOutOfRangeException(nameof(pow), pow, "zero base and negative power");
        }

        if (pow == -ONE) return ONE / value;

        bool isPowerInteger = IsInteger(pow);
        if (value < ZERO && !isPowerInteger) {
            throw new ArgumentOutOfRangeException(nameof(value), value, "negative base and non-integer power");
        }

        switch (isPowerInteger) {
            case true when value > ZERO: {
                int powerInt = (int) pow;
                return PowerN(value, powerInt);
            }
            case true when value < ZERO: {
                int powerInt = (int) pow;
                if (powerInt % 2 == 0) {
                    return Exp(pow * Log(-value));
                }

                return -Exp(pow * Log(-value));
            }
            default:
                return Exp(pow * Log(value));
        }

    }

    private static bool IsInteger(decimal value) {
        long longValue = (long) value;
        return Math.Abs(value - longValue) <= EPSILON;
    }

    /// <summary>
    /// Power to the integer value
    /// </summary>
    /// <param name="value"></param>
    /// <param name="power"></param>
    /// <returns></returns>
    public static decimal PowerN(decimal value, int power) {
        while (true) {
            if (power == ZERO) return ONE;
            if (power < ZERO) {
                value = ONE / value;
                power = -power;
                continue;
            }

            int     q       = power;
            decimal prod    = ONE;
            decimal current = value;
            while (q > 0) {
                if (q % 2 == 1) {
                    // detects the 1s in the binary expression of power
                    prod = current * prod; // picks up the relevant power
                    q--;
                }

                current *=  current; // value^i -> value^(2*i)
                q       >>= 1;
            }

            return prod;
        }
    }

    /// <summary>
    /// <para>Analogy of <see cref="System.Math.Log10"/></para>
    /// <inheritdoc cref="System.Math.Log10"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static decimal Log10(decimal x) {
        return Log(x) * LOG10_INV;
    }

    /// <summary>
    /// <para>Analogy of <see cref="System.Math.Log(double)"/></para>
    /// <inheritdoc cref="System.Math.Log(double)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <exception cref="ArgumentOutOfRangeException">if <paramref name="x"/> is not positive</exception>
    /// <returns></returns>
    public static decimal Log(decimal x) {
        if (x <= ZERO) {
            throw new ArgumentOutOfRangeException(nameof(x), x, "x must be greater than zero");
        }

        int count = 0;
        while (x >= ONE) {
            x *= EINV;
            count++;
        }

        while (x <= EINV) {
            x *= E;
            count--;
        }

        x--;
        if (x == ZERO) return count;
        decimal result      = ZERO;
        int     iteration   = 0;
        decimal y           = ONE;
        decimal cacheResult = result - ONE;
        while (cacheResult != result && iteration < MAX_ITERATION) {
            iteration++;
            cacheResult =  result;
            y           *= -x;
            result      += y / iteration;
        }

        return count - result;
    }

    /// <summary>
    /// <para>Analogy of <see cref="System.Math.Cos"/></para>
    /// <inheritdoc cref="System.Math.Cos"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static decimal Cos(decimal x) {
        //truncating to  [-2*PI;2*PI]
        TruncateToPeriodicInterval(ref x);

        // now x in (-2pi,2pi)
        switch (x) {
            case >= PI and <= P_IX2:
                return -Cos(x - PI);
            case >= -P_IX2 and <= -PI:
                return -Cos(x + PI);
        }

        x *= x;
        //y=1-x/2!+x^2/4!-x^3/6!...
        decimal xx      = -x * HALF;
        decimal y       = ONE + xx;
        decimal cachedY = y - ONE; //init cache  with different value
        for (int i = 1; cachedY != y && i < MAX_ITERATION; i++) {
            cachedY = y;
            decimal factor = i * ((i << 1) + 3) + 1; //2i^2+2i+i+1=2i^2+3i+1
            factor =  -HALF / factor;
            xx     *= x * factor;
            y      += xx;
        }

        return y;
    }

    /// <summary>
    /// <para>Analogy of <see cref="System.Math.Tan"/></para>
    /// <inheritdoc cref="System.Math.Tan"/>
    /// </summary>
    /// <param name="x"></param>
    /// <exception cref="ArgumentOutOfRangeException">if <c>cos(x)</c> is 0</exception>
    /// <returns></returns>
    public static decimal Tan(decimal x) {
        decimal cos = Cos(x);
        if (cos == ZERO) throw new ArgumentOutOfRangeException(nameof(x), x, "Tan(Pi/2) is undefined");
        //calculate sin using cos
        decimal sin = CalculateSinFromCos(x, cos);
        return sin / cos;
    }

    /// <summary>
    /// Helper function for calculating sin(x) from cos(x)
    /// </summary>
    /// <param name="x"></param>
    /// <param name="cos"></param>
    /// <returns></returns>
    private static decimal CalculateSinFromCos(decimal x, decimal cos) {
        decimal moduleOfSin    = Sqrt(ONE - cos * cos);
        bool    sineIsPositive = IsSignOfSinePositive(x);
        if (sineIsPositive) return moduleOfSin;
        return -moduleOfSin;
    }

    /// <summary>
    /// <para>Analogy of <see cref="System.Math.Sin"/></para>
    /// <inheritdoc cref="System.Math.Sin"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static decimal Sin(decimal x) {
        decimal cos = Cos(x);
        return CalculateSinFromCos(x, cos);
    }

    /// <summary>
    /// Truncates to  [-2*PI;2*PI]
    /// </summary>
    /// <param name="x"></param>
    private static void TruncateToPeriodicInterval(ref decimal x) {
        while (x >= P_IX2) {
            int divide = Math.Abs(decimal.ToInt32(x / P_IX2));
            x -= divide * P_IX2;
        }

        while (x <= -P_IX2) {
            int divide = Math.Abs(decimal.ToInt32(x / P_IX2));
            x += divide * P_IX2;
        }
    }

    private static bool IsSignOfSinePositive(decimal x) {
        //truncating to  [-2*PI;2*PI]
        TruncateToPeriodicInterval(ref x);

        //now x in [-2*PI;2*PI]
        return x switch {
            >= -P_IX2 and <= -PI => true,
            >= -PI and <= ZERO   => false,
            >= ZERO and <= PI    => true,
            >= PI and <= P_IX2   => false,
            //will not be reached
            _ => true
        };
    }

    /// <summary>
    /// <para>Analogy of <see cref="System.Math.Sqrt"/></para>
    /// <inheritdoc cref="System.Math.Sqrt"/>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="epsilon">lasts iteration while error less than this epsilon</param>
    /// <exception cref="ArgumentOutOfRangeException">if <paramref name="x"/> is negative</exception>
    /// <returns></returns>
    public static decimal Sqrt(decimal x, decimal epsilon = ZERO) {
        if (x < ZERO) throw new ArgumentOutOfRangeException(nameof(x), x, "Cannot calculate square root from a negative number");
        //initial approximation
        decimal current = (decimal) Math.Sqrt((double) x), previous;
        do {
            previous = current;
            if (previous == ZERO) return ZERO;
            current = (previous + x / previous) * HALF;
        } while (Math.Abs(previous - current) > epsilon);

        return current;
    }

    /// <summary>
    /// <para>Analogy of <see cref="System.Math.Sinh"/></para>
    /// <inheritdoc cref="System.Math.Sinh"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static decimal Sinh(decimal x) {
        decimal y  = Exp(x);
        decimal yy = ONE / y;
        return (y - yy) * HALF;
    }

    /// <summary>
    /// <para>Analogy of <see cref="System.Math.Cosh"/></para>
    /// <inheritdoc cref="System.Math.Cosh"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static decimal Cosh(decimal x) {
        decimal y  = Exp(x);
        decimal yy = ONE / y;
        return (y + yy) * HALF;
    }

    // Provided by System.Math.Sign(decimal)
    /*
    /// <summary>
    /// Analogy of <see cref="Math.Sign"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static int Sign(decimal x) {
        return x < Zero ? -1 : x > Zero ? 1 : 0;
    }
    */

    /// <summary>
    /// <para>Analogy of <see cref="System.Math.Tanh"/></para>
    /// <inheritdoc cref="System.Math.Tanh"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static decimal Tanh(decimal x) {
        decimal y  = Exp(x);
        decimal yy = ONE / y;
        return (y - yy) / (y + yy);
    }

    // Provided by System.Math.Abs(decimal)
    /*
    /// <summary>
    /// Analogy of <see cref="Math.Abs"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static decimal Abs(decimal x) {
        if (x <= Zero) {
            return -x;
        }

        return x;
    }
    */

    /// <summary>
    /// <para>Analogy of <see cref="System.Math.Asin"/></para>
    /// <inheritdoc cref="System.Math.Asin"/>
    /// </summary>
    /// <param name="x"></param>
    /// <exception cref="ArgumentOutOfRangeException">if x is less than -1 or greater than 1</exception>
    /// <returns></returns>
    public static decimal Asin(decimal x) {
        switch (x) {
            case > ONE:
            case < -ONE:
                throw new ArgumentOutOfRangeException(nameof(x), x, "x must be in [-1,1]");
            //known values
            case ZERO:
                return ZERO;
            case ONE:
                return P_IDIV2;
            //asin function is odd function
            case < ZERO:
                return -Asin(-x);
        }

        //my optimize trick here

        // used a math formula to speed up :
        // asin(x)=0.5*(pi/2-asin(1-2*x*x)) 
        // if x>=0 is true

        decimal newX = ONE - 2 * x * x;

        //for calculating new value near to zero than current
        //because we gain more speed with values near to zero
        if (Math.Abs(x) > Math.Abs(newX)) {
            decimal t = Asin(newX);
            return HALF * (P_IDIV2 - t);
        }

        decimal y      = ZERO;
        decimal result = x;
        decimal cachedResult;
        int     i = 1;
        y += result;
        decimal xx = x * x;
        do {
            cachedResult =  result;
            result       *= xx * (ONE - HALF / i);
            y            += result / ((i << 1) + 1);
            i++;
        } while (cachedResult != result);

        return y;
    }

    /// <summary>
    /// <para>Analogy of <see cref="System.Math.Atan"/></para>
    /// <inheritdoc cref="System.Math.Atan"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static decimal ATan(decimal x) {
        return x switch {
            ZERO => ZERO,
            ONE  => P_IDIV4,
            _    => Asin(x / Sqrt(ONE + x * x))
        };
    }

    /// <summary>
    /// <para>Analogy of <see cref="System.Math.Acos"/></para>
    /// <inheritdoc cref="System.Math.Acos"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static decimal Acos(decimal x) {
        return x switch {
            ZERO   => P_IDIV2,
            ONE    => ZERO,
            < ZERO => PI - Acos(-x),
            _      => P_IDIV2 - Asin(x)
        };
    }

    /// <summary>
    /// <para>Analogy of <see cref="System.Math.Atan2"/></para>
    /// <para>for more see this
    /// <see href="https://i.imgur.com/TRLjs8R.png"/></para>
    /// <inheritdoc cref="System.Math.Atan2"/>
    /// </summary>
    /// <param name="y"></param>
    /// <param name="x"></param>
    /// <exception cref="ArgumentException"></exception>
    /// <returns></returns>
    public static decimal Atan2(decimal y, decimal x) {
        return x switch {
            ZERO when y > ZERO    => P_IDIV2,
            ZERO when y < ZERO    => -P_IDIV2,
            > ZERO                => ATan(y / x),
            < ZERO when y >= ZERO => ATan(y / x) + PI,
            < ZERO when y < ZERO  => ATan(y / x) - PI,
            _                     => throw new ArgumentException("invalid atan2 arguments")
        };

    }

}
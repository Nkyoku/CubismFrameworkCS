using System;
using System.Collections.Generic;
using System.Text;

namespace CubismFramework
{
    static class CubismMath
    {
        /// <summary>
        /// イージング処理されたサインを求める
        /// </summary>
        /// <param name="t">イージングを行う値(0～1の間でイージング)</param>
        /// <returns>イージング処理されたサイン値</returns>
        public static double EaseSine(double t)
        {
            if (t < 0.0)
            {
                return 0.0;
            }   
            else if (1.0 < t)
            {
                return 1.0;
            }
            else
            {
                return 0.5 - 0.5 * Math.Cos(Math.PI * t);
            }
        }
    }
}

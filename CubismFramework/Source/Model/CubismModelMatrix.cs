using System;
//using System.Numerics;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra.Single;

namespace CubismFramework
{
    internal class CubismModelMatrix
    {
        public CubismModelMatrix(double width, double height)
        {
            Width = width;
            Height = height;
            if (Height < Width)
            {
                SetWidth(Height / Width);
            }
            else
            {
                SetHeight(1.0);
            }
        }

        public void SetWidth(double w)
        {
            Matrix[0, 0] = (float)(w / Width);
            Matrix[1, 1] = (float)(w / Width);
        }

        public void SetHeight(double h)
        {
            Matrix[0, 0] = (float)(h / Height);
            Matrix[1, 1] = (float)(h / Height);
        }

        void SetCenterPosition(double x, double y)
        {
            CenterX(x);
            CenterY(y);
        }

        public void CenterX(double x)
        {
            double w = Width * Matrix[0, 0];
            Matrix[3, 0] = (float)(x - (w / 2.0));
        }

        public void CenterY(double y)
        {
            double h = Height * Matrix[1, 1];
            Matrix[3, 1] = (float)(y - (h / 2.0));
        }

        public void Bottom(double y)
        {
            double h = Height * Matrix[1, 1];
            Matrix[3, 1] = (float)(y - h);
        }

        public void Right(double x)
        {
            double w = Width * Matrix[0, 0];
            Matrix[3, 0] = (float)(x - w);
        }

        public void SetPosition(double x, double y)
        {
            Matrix[3, 0] = (float)x;
            Matrix[3, 1] = (float)y;
        }

        public void Top(double y)
        {
            SetY(y);
        }

        public void Left(double x)
        {
            SetX(x);
        }

        public void SetX(double x)
        {
            Matrix[3, 0] = (float)x;
        }

        public void SetY(double y)
        {
            Matrix[3, 1] = (float)y;
        }

        public void SetupFromLayout(Dictionary<string, double> layout)
        {
            if (layout == null)
            {
                return;
            }
            foreach(var item in layout)
            {
                if (item.Key == "width")
                {
                    SetWidth(item.Value);
                }
                else if (item.Key == "height")
                {
                    SetHeight(item.Value);
                }
            }
            foreach (var item in layout)
            {
                if (item.Key == "x")
                {
                    SetX(item.Value);
                }
                else if (item.Key == "y")
                {
                    SetY(item.Value);
                }
                else if (item.Key == "center_x")
                {
                    CenterX(item.Value);
                }
                else if (item.Key == "center_y")
                {
                    CenterY(item.Value);
                }
                else if (item.Key == "top")
                {
                    Top(item.Value);
                }
                else if (item.Key == "bottom")
                {
                    Bottom(item.Value);
                }
                else if (item.Key == "left")
                {
                    Left(item.Value);
                }
                else if (item.Key == "right")
                {
                    Right(item.Value);
                }
            }
        }

        private double Width = 0.0;

        private double Height = 0.0;

        public Matrix Matrix = DenseMatrix.CreateIdentity(4);
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IPA;

public static class IpaService
{
    private class Point
    {
        public int X { get; set; }

        public int Y { get; set; }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    public static void FindPath(Bitmap image)
    {
        var biImage = (Bitmap)image.Clone();
        var start = FindStart(image);
        ImageToBinary(biImage);

        var labels = HighlightRelatedAreas(biImage, out var t);

        var massCenters = GetMassCenter(labels);
        var currentLabel = labels[start.X, start.Y];
        var currentCenter = massCenters[currentLabel];

        const int stepSize = 10;
        while (true)
        {
            var tita = GetOrientation(labels, currentLabel);
            var x = currentCenter.X + (int)(stepSize * Math.Cos(tita));
            var y = currentCenter.Y + (int)(stepSize * Math.Sin(tita));

            var endPoint = new Point(x, y);

            var label = labels[endPoint.X, endPoint.Y];

            if (label != currentLabel && label != 0)
            {
                var prevLabel = currentLabel;
                currentLabel = label;
                currentCenter = massCenters[currentLabel];
                var prevCenter = GetMassCenter(labels)[prevLabel];
                if (IsArrow(labels, label))
                {
                    DrawLine(image, prevCenter, currentCenter);
                }
                else
                {
                    DrawLine(image, prevCenter, currentCenter);
                    break;
                }
            }
            else
            {
                currentCenter.X = endPoint.X + 1;
                currentCenter.Y = endPoint.Y + 1;
            }
        }
    }

    private static void DrawLine(Bitmap image, Point startPoint, Point endPoint)
    {
        using var graphics = Graphics.FromImage(image);
        using var pen = new Pen(Color.Red, 2);
        graphics.DrawLine(pen, startPoint.X, startPoint.Y, endPoint.X, endPoint.Y);
    }

    private static void ImageToBinary(Bitmap image)
    {
        ToGrayScale(image);
        AdaptiveThreshold(image);
    }

    #region Binarization

    private static void ToGrayScale(Bitmap image)
    {
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                var pixel = image.GetPixel(x, y);
                var gray = (byte)(0.2125 * pixel.R + 0.7154 * pixel.G + 0.0721 * pixel.B);
                image.SetPixel(x, y, Color.FromArgb(gray, gray, gray));
            }
        }
    }

    private static int[,] GetIntegralImage(Bitmap image)
    {
        var integralImage = new int[image.Width, image.Height];

        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                if (x == 0)
                {
                    integralImage[x, 0] = image.GetPixel(x, 0).R;
                    continue;
                }

                if (y == 0)
                {
                    integralImage[0, y] = image.GetPixel(0, y).R;
                    continue;
                }

                var sum = image.GetPixel(x, y).R +
                          integralImage[x - 1, y] +
                          integralImage[x, y - 1] -
                          integralImage[x - 1, y - 1];

                integralImage[x, y] = sum;
            }
        }

        return integralImage;
    }

    /// <summary>
    /// https://people.scs.carleton.ca/~roth/iit-publications-iti/docs/gerh-50002.pdf
    /// </summary>
    /// <param name="grayImg">image turned to gray</param>
    private static void AdaptiveThreshold(Bitmap grayImg)
    {
        // as article says: "Wellner uses 1/8th
        //of the image width for the value of s and 15 for the value of t"
        // why? her znaet!
        var s = grayImg.Width / 8;
        var t = 15;

        var intgImage = GetIntegralImage(grayImg);
        for (int i = 0; i < grayImg.Width; i++)
        {
            int sum = 0;
            for (int j = 0; j < grayImg.Height; j++)
            {
                sum += grayImg.GetPixel(i, j).R;
                if (i == 0)
                    intgImage[i, j] = sum;
                else
                    intgImage[i, j] = intgImage[i - 1, j] + sum;
            }
        }

        for (int i = 0; i < grayImg.Width; i++)
        {
            for (int j = 0; j < grayImg.Height; j++)
            {
                var x1 = Math.Max(1, i - s / 2);
                var x2 = Math.Min(grayImg.Width - 1, i + s / 2);
                var y1 = Math.Max(1, j - s / 2);
                var y2 = Math.Min(grayImg.Height - 1, j + s / 2);

                var count = (x2 - x1) * (y2 - y1);

                var sum = intgImage[x2, y2] -
                          intgImage[x2, y1 - 1] -
                          intgImage[x1 - 1, y2] +
                          intgImage[x1 - 1, y1 - 1];

                if (grayImg.GetPixel(i, j).R * count <= sum * (100 - t) / 100)
                    grayImg.SetPixel(i, j, Color.Black);
                else
                    grayImg.SetPixel(i, j, Color.White);
            }
        }
    }

    #endregion

    #region Find Related Areas

    private static int[,] HighlightRelatedAreas(Bitmap biImage, out int totalLabels)
    {
        var labelMatrix = new int[biImage.Width, biImage.Height];
        var label = 0;
        for (int i = 0; i < biImage.Width; i++)
        {
            for (int j = 0; j < biImage.Height; j++)
            {
                if (biImage.GetPixel(i, j).IsForeground() && labelMatrix[i, j] == 0)
                {
                    label++;
                    Dfs(biImage, labelMatrix, label, i, j);
                }
            }
        }

        totalLabels = label;
        return labelMatrix;
    }

    private static void Dfs(Bitmap biImage, int[,] labeledImage, int label, int x, int y)
    {
        if (x >= 0 && x < biImage.Width &&
            y >= 0 && y < biImage.Height &&
            biImage.GetPixel(x, y).IsForeground() &&
            labeledImage[x, y] == 0)
        {
            labeledImage[x, y] = label;
            Dfs(biImage, labeledImage, label, x - 1, y);
            Dfs(biImage, labeledImage, label, x + 1, y);
            Dfs(biImage, labeledImage, label, x, y - 1);
            Dfs(biImage, labeledImage, label, x, y + 1);
        }
    }

    #endregion

    /// <summary>
    /// for label area counting
    /// </summary>
    /// <param name="labels"> matrix of related areas</param>
    /// <returns></returns>
    private static Dictionary<int, int> CountLabelAreas(int[,] labels)
    {
        var labelAreas = new Dictionary<int, int>();

        for (int x = 0; x < labels.GetLength(0); x++)
        {
            for (int y = 0; y < labels.GetLength(1); y++)
            {
                var label = labels[x, y];
                if (label != 0)
                {
                    if (!labelAreas.ContainsKey(label))
                    {
                        labelAreas[label] = 0;
                    }

                    labelAreas[label]++;
                }
            }
        }

        return labelAreas;
    }

    /// <summary>
    /// Method for central mass counting
    /// </summary>
    /// <param name="labels">matrix of related areas</param>
    private static Dictionary<int, Point> GetMassCenter(int[,] labels)
    {
        var labelsArea = CountLabelAreas(labels);
        var labelMassCenters = new Dictionary<int, Point>();

        for (int x = 0; x < labels.GetLength(0); x++)
        {
            for (int y = 0; y < labels.GetLength(1); y++)
            {
                var label = labels[x, y];

                if (label != 0)
                {
                    if (!labelMassCenters.ContainsKey(label))
                    {
                        labelMassCenters[label] = new Point(0, 0);
                    }

                    labelMassCenters[label].X += x;
                    labelMassCenters[label].Y += y;
                }
            }
        }

        foreach (var massCenter in labelMassCenters)
        {
            int area = labelsArea[massCenter.Key];
            labelMassCenters[massCenter.Key].X /= area;
            labelMassCenters[massCenter.Key].Y /= area;
        }

        return labelMassCenters;
    }

    /// <summary>
    /// Central moment of labeled area
    /// </summary>
    /// <param name="labels"></param>
    /// <param name="label"></param>
    /// <param name="i"></param>
    /// <param name="j"></param>
    /// <returns></returns>
    private static double DiscreteCentralMoment(int[,] labels, int label, int i, int j)
    {
        double mij = 0;
        var massCentral = GetMassCenter(labels)[label];
        for (int x = 0; x < labels.GetUpperBound(0); x++)
        {
            for (int y = 0; y < labels.GetUpperBound(1); y++)
            {
                if (labels[x, y] == label)
                {
                    mij += Math.Pow(x - massCentral.X, i) * Math.Pow(y - massCentral.Y, j);
                }
            }
        }

        return mij;
    }

    /// <summary>
    /// Метод определяющйи  ориентацию области и направление стрелки (путем наговнокоживания всего, что ниже tita)
    /// </summary>
    /// <param name="labels"></param>
    /// <param name="label"></param>
    /// <returns></returns>
    private static double GetOrientation(int[,] labels, int label)
    {
        var m11 = DiscreteCentralMoment(labels, label, 1, 1);
        var m20 = DiscreteCentralMoment(labels, label, 2, 0);
        var m02 = DiscreteCentralMoment(labels, label, 0, 2);

        var tita = 0.5 * Math.Atan2(2 * m11, m20 - m02);
        var mc = GetMassCenter(labels)[label];
        const int arrowHalf = 28;

        //one side of tita line
        var x1 = mc.X + (int)(arrowHalf * Math.Cos(tita));
        var y1 = mc.Y + (int)(arrowHalf * Math.Sin(tita));
        var len1 = 0;


        //another of tita line
        var x2 = mc.X - (int)(arrowHalf * Math.Cos(tita));
        var y2 = mc.Y - (int)(arrowHalf * Math.Sin(tita));
        var len2 = 0;

        for (int i = 0; i < labels.GetLength(1); i++)
        {
            var px = x1 + (int)(i * Math.Cos(tita + Math.PI / 2));
            var py = y1 + (int)(i * Math.Sin(tita + Math.PI / 2));
            if (labels[px, py] == 0)
            {
                len1 = (int)Math.Sqrt(Math.Pow(px - x1, 2) + Math.Pow(py - y1, 2));
                break;
            }
        }

        for (int i = 0; i < labels.GetLength(1); i++)
        {
            var px = x2 + (int)(i * Math.Cos(tita - Math.PI / 2));
            var py = y2 + (int)(i * Math.Sin(tita - Math.PI / 2));
            if (labels[px, py] == 0)
            {
                len2 = (int)Math.Sqrt(Math.Pow(px - x2, 2) + Math.Pow(py - y2, 2));
                break;
            }
        }

        if (len1 > len2)
            return tita + Math.PI;

        return tita;
    }

    private static Point FindStart(Bitmap image)
    {
        var labels = new int[image.Width, image.Height];
        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                var p = image.GetPixel(x, y);
                if (p is { R: >= 220, G: 0, B: 0 })
                {
                    labels[x, y]++;
                }
            }
        }

        var center = GetMassCenter(labels).First();

        return center.Value;
    }

    /// <summary>
    /// Arrows have elongation from 3 to 4+;
    /// </summary>
    /// <returns></returns>
    private static bool IsArrow(int[,] labels, int label)
    {
        var elongation = GetElongation(labels, label);
        return Math.Round(elongation) is >= 3 and <= 4;
    }

    private static double GetElongation(int[,] labels, int label)
    {
        var m11 = DiscreteCentralMoment(labels, label, 1, 1);
        var m20 = DiscreteCentralMoment(labels, label, 2, 0);
        var m02 = DiscreteCentralMoment(labels, label, 0, 2);

        var c = m20 + m02 + Math.Sqrt(Math.Pow(m20 - m02, 2) + 4 * m11 * m11);
        var z = m20 + m02 - Math.Sqrt(Math.Pow(m20 - m02, 2) + 4 * m11 * m11);
        return c / z;
    }
}
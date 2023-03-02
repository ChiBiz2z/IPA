using System;
using System.Drawing;

namespace IPA;

public static class IpaService
{
    public static void ImageToBinary(Bitmap image)
    {
        ToGrayScale(image);
        AdaptiveThreshold(image);
    }

    private static void ToGrayScale(Bitmap image)
    {
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                var pixel = image.GetPixel(x, y);
                // The green circles on the arrows disappear. By randomness, I found that at values (wrong multipliers)
                //
                //             var gray = (byte)(0.7125 * pixel.R + 1.2154 * pixel.G + 0.721 * pixel.B);
                //
                // they appear, but the image will slightly affect
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
}